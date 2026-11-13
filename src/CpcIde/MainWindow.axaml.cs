// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using CpcBasic.Core;

namespace CpcIde;

public partial class MainWindow : Window
{
    const string StartProgram =
        "10 REM Mon premier programme\r\n" +
        "20 PRINT \"HELLO CPC !\"\r\n" +
        "30 PRINT \"COMPILE DEPUIS MON EDITEUR\"\r\n" +
        "40 END\r\n";

    static readonly FilePickerFileType BasicFileType = new("Programmes BASIC")
    {
        Patterns = new[] { "*.bas" }
    };

    static IBrush Make(string hex) => new SolidColorBrush(Color.Parse(hex));

    // Une ligne du panneau de messages. Line vaut 0 quand le message n'est lié à aucune ligne.
    sealed class MessageItem
    {
        public MessageItem(string text, int line)
        {
            Text = text;
            Line = line;
        }

        public string Text { get; }
        public int Line { get; }
        public override string ToString() => Text;
    }

    readonly EditorSettings settings = EditorSettings.Load();
    readonly BasicColorizer colorizer = new();

    string? currentPath;
    bool modified;
    bool loading;
    double charWidth;        // largeur d'un caractère de l'éditeur, en points
    int visibleColumns;      // nombre de colonnes de caractères visibles dans la fenêtre d'édition
    bool closeConfirmed;   // vrai quand on a déjà demandé quoi faire des modifications avant de fermer

    public MainWindow()
    {
        InitializeComponent();

        Editor.TextArea.TextView.LineTransformers.Add(colorizer);

        // Un espace entre la colonne des numéros de l'éditeur et le texte du programme :
        // sans lui, « 1 » (éditeur) et « 10 » (numéro BASIC) se lisent comme « 110 ».
        Editor.TextArea.LeftMargins.Add(new Border { Width = 14 });

        // La largeur en colonnes change quand la fenêtre d'édition change de taille
        Editor.TextArea.TextView.PropertyChanged += (_, e) =>
        {
            if (e.Property == Avalonia.Visual.BoundsProperty)
                ComputeColumns();
        };
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        Editor.TextChanged += (_, _) =>
        {
            if (loading) return;
            modified = true;
            UpdateTitle();
        };

        // « Tunnel » : la fenêtre voit les touches et la molette avant l'éditeur,
        // donc Ctrl+molette et Ctrl+plus ne sont pas pris par l'éditeur.
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnWindowWheel, RoutingStrategies.Tunnel);

        // Au démarrage, le curseur est dans l'éditeur : on peut taper tout de suite.
        Opened += (_, _) =>
        {
            Editor.TextArea.Focus();
            ComputeColumns();
        };

        SetText(StartProgram);
        UpdateTitle();
        ApplyAppearance();
    }

    // ---------- Boutons et raccourcis ----------

    async void OnOpenClick(object? sender, RoutedEventArgs e) => await OpenFileAsync();
    async void OnSaveClick(object? sender, RoutedEventArgs e) => await SaveAsync();
    async void OnCompileClick(object? sender, RoutedEventArgs e) => await CompileAsync();
    void OnSmallerClick(object? sender, RoutedEventArgs e) => ChangeFontSize(-2);
    void OnBiggerClick(object? sender, RoutedEventArgs e) => ChangeFontSize(+2);
    void OnContrastClick(object? sender, RoutedEventArgs e) => ToggleContrast();

    async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await CompileAsync();
        }
        else if (e.Key == Key.F6)
        {
            // F6 : passer de l'éditeur au panneau de messages, et inversement
            e.Handled = true;
            if (MessageList.IsKeyboardFocusWithin)
                Editor.TextArea.Focus();
            else
            {
                // Rien de choisi : on prend la première erreur liée à une ligne (sinon le premier message)
                if (MessageList.SelectedItem is null
                    && MessageList.ItemsSource is List<MessageItem> messages
                    && messages.Count > 0)
                {
                    int first = messages.FindIndex(m => m.Line > 0);
                    MessageList.SelectedIndex = first >= 0 ? first : 0;
                }

                // Le clavier se pose sur un message précis (comme après un clic), pas sur la liste entière
                Control? container = MessageList.SelectedIndex >= 0
                    ? MessageList.ContainerFromIndex(MessageList.SelectedIndex)
                    : null;
                if (container is not null)
                    container.Focus(NavigationMethod.Tab);
                else
                    MessageList.Focus();
            }
        }
        else if (e.Key == Key.Enter && MessageList.IsKeyboardFocusWithin)
        {
            // Entrée sur un message : la liste la garde pour elle, donc on la prend ici, avant la liste
            e.Handled = true;
            GoToSelectedMessage(true);
            Editor.TextArea.Focus();   // même pour un message qui n'est lié à aucune ligne
        }
        else if (ctrl && e.Key == Key.S)
        {
            e.Handled = true;
            await SaveAsync();
        }
        else if (ctrl && e.Key == Key.O)
        {
            e.Handled = true;
            await OpenFileAsync();
        }
        else if (ctrl && e.Key is Key.OemPlus or Key.Add)
        {
            e.Handled = true;
            ChangeFontSize(+2);
        }
        else if (ctrl && e.Key is Key.OemMinus or Key.Subtract)
        {
            e.Handled = true;
            ChangeFontSize(-2);
        }
        else if (ctrl && e.Key is Key.D0 or Key.NumPad0)
        {
            e.Handled = true;
            SetFontSize(EditorSettings.DefaultFontSize);
        }
        else if (ctrl && e.Key == Key.H)
        {
            e.Handled = true;
            ToggleContrast();
        }
    }

    // Ctrl + molette : taille du texte
    void OnWindowWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        e.Handled = true;
        if (e.Delta.Y > 0)
            ChangeFontSize(+2);
        else if (e.Delta.Y < 0)
            ChangeFontSize(-2);
    }

    // ---------- Apparence : taille du texte et contraste ----------

    void ChangeFontSize(int delta) => SetFontSize(settings.FontSize + delta);

    void SetFontSize(int size)
    {
        size = Math.Clamp(size, EditorSettings.MinFontSize, EditorSettings.MaxFontSize);
        if (size == settings.FontSize)
            return;

        settings.FontSize = size;
        settings.Save();
        ApplyAppearance();
    }

    void ToggleContrast()
    {
        settings.HighContrast = !settings.HighContrast;
        settings.Save();
        ApplyAppearance();
    }

    // Applique les réglages à l'éditeur, au panneau de messages et à la barre d'état
    void ApplyAppearance()
    {
        bool high = settings.HighContrast;

        Editor.FontSize = settings.FontSize;
        Editor.Background = Make(high ? "#000000" : "#000080");
        Editor.Foreground = Make(high ? "#FFFFFF" : "#FFFF00");
        // Numéros de l'éditeur en gris : jamais de la couleur des numéros BASIC (violet ou jaune)
        Editor.LineNumbersForeground = Make(high ? "#C0C0C0" : "#B0B0B0");

        colorizer.HighContrast = high;
        Editor.TextArea.TextView.Redraw();

        // Le panneau de messages et la barre d'état restent lisibles : au moins 16 et 14 points
        MessageList.FontSize = Math.Max(16, settings.FontSize);
        MessageList.Background = Make(high ? "#000000" : "#101010");
        MessageList.Foreground = Make(high ? "#FFFFFF" : "#E0E0E0");

        StatusText.FontSize = Math.Max(14, settings.FontSize - 2);
        StatusText.Foreground = Make(high ? "#FFFFFF" : "#C0C0C0");
        StatusBar.Background = Make(high ? "#000000" : "#202020");

        ContrastButton.Content = high
            ? "Contraste élevé : oui (Ctrl+H)"
            : "Contraste élevé : non (Ctrl+H)";

        UpdateColumns();
    }

    // Mesure la largeur d'un caractère de l'éditeur (la police est à chasse fixe), puis la largeur en colonnes
    void UpdateColumns()
    {
        var sample = new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                       new Typeface(Editor.FontFamily), Editor.FontSize, Brushes.Black);
        charWidth = sample.Width;
        ComputeColumns();
    }

    void ComputeColumns()
    {
        double width = Editor.TextArea.TextView.Bounds.Width;
        visibleColumns = charWidth > 0 ? (int)Math.Floor(width / charWidth) : 0;
        UpdateStatus();
    }

    // ---------- Ouvrir et enregistrer ----------

    // Si le programme a des modifications non enregistrées, demande quoi faire (Enregistrer, Ne pas enregistrer, Annuler).
    // Renvoie vrai si on peut continuer (enregistré ou abandonné), faux si la personne a annulé.
    async Task<bool> ConfirmDiscardOrSaveAsync()
    {
        if (!modified)
            return true;

        string file = currentPath is null ? "sans titre" : Path.GetFileName(currentPath);
        var dialog = new SaveChangesDialog(file);
        SaveChangesDialog.Choice choice = await dialog.ShowDialog<SaveChangesDialog.Choice>(this);

        return choice switch
        {
            SaveChangesDialog.Choice.Save => await SaveAsync(),   // faux si l'enregistrement échoue ou est annulé
            SaveChangesDialog.Choice.Discard => true,
            _ => false
        };
    }

    // Fermeture de la fenêtre : si le programme est modifié, on la retient et on demande d'abord quoi faire
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (closeConfirmed || !modified)
            return;

        e.Cancel = true;
        _ = CloseAfterConfirmAsync();
    }

    async Task CloseAfterConfirmAsync()
    {
        if (await ConfirmDiscardOrSaveAsync())
        {
            closeConfirmed = true;
            Close();
        }
    }

    async Task OpenFileAsync()
    {
        if (!await ConfirmDiscardOrSaveAsync())
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Ouvrir un programme BASIC",
            AllowMultiple = false,
            FileTypeFilter = new[] { BasicFileType, FilePickerFileTypes.All }
        });
        if (files.Count == 0)
            return;

        string? path = files[0].TryGetLocalPath();
        if (path is null)
        {
            ShowMessage("Impossible de lire ce fichier : son emplacement n'est pas accessible.");
            return;
        }

        try
        {
            string text = await File.ReadAllTextAsync(path);
            SetText(text);
            currentPath = path;
            UpdateTitle();
            ShowMessage($"Fichier ouvert : {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage($"Impossible de lire le fichier : {ex.Message}");
        }
    }

    // Enregistre le programme ; demande un nom si le fichier n'en a pas encore. Renvoie faux si annulé ou en échec.
    async Task<bool> SaveAsync()
    {
        if (currentPath is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer le programme",
                SuggestedFileName = "jeu.bas",
                DefaultExtension = "bas",
                FileTypeChoices = new[] { BasicFileType }
            });
            if (file is null)
                return false;

            string? chosen = file.TryGetLocalPath();
            if (chosen is null)
            {
                ShowMessage("Impossible d'enregistrer ici : emplacement non accessible.");
                return false;
            }
            currentPath = chosen;
        }

        try
        {
            await File.WriteAllTextAsync(currentPath, Editor.Text);
            modified = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage($"Impossible d'enregistrer le fichier : {ex.Message}");
            return false;
        }
    }

    // ---------- Compilation ----------

    async Task CompileAsync()
    {
        if (!await SaveAsync())
            return;

        string path = currentPath!;
        string name = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        if (name.Length is 0 or > 8)
        {
            ShowMessage($"Le nom « {name} » ne convient pas : une disquette CPC accepte 8 caractères au plus. " +
                        "Renommez le fichier (par exemple « jeu.bas ») puis recompilez.");
            return;
        }

        CompileResult result;
        try
        {
            result = Compiler.CompileToDisk(Editor.Text, name);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowMessage($"Compilation impossible : {ex.Message}");
            return;
        }

        if (!result.Success)
        {
            var items = result.Diagnostics.Select(d => new MessageItem(d.Message, d.Line)).ToList();
            items.Add(new MessageItem($"{result.Diagnostics.Count} erreur(s). Aucun fichier n'a été créé.", 0));
            ShowMessages(items);
            return;
        }

        string dskPath = Path.ChangeExtension(path, ".dsk");
        try
        {
            result.Disk!.Save(dskPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessages(new[]
            {
                new MessageItem("Impossible d'écrire le fichier .dsk : il est sans doute ouvert dans votre émulateur.", 0),
                new MessageItem("Fermez l'émulateur (ou éjectez la disquette), puis compilez de nouveau.", 0),
                new MessageItem($"Fichier : {dskPath}", 0),
                new MessageItem($"Détail de Windows : {ex.Message}", 0)
            });
            return;
        }

        string modules = result.Modules.Count == 0 ? "aucun" : string.Join(", ", result.Modules);
        ShowMessages(new[]
        {
            new MessageItem($"Compilation réussie : {dskPath}", 0),
            new MessageItem($"Programme Z80 : {result.BinarySize} octets. Modules liés : {modules}.", 0),
            new MessageItem($"Dans l'émulateur : insérez le disque dans le lecteur A, puis tapez RUN\"{name}\"", 0),
            new MessageItem("Avant de recompiler le même programme, fermez l'émulateur : il garde le fichier .dsk ouvert.", 0)
        });
    }

    // ---------- Panneau de messages ----------

    void ShowMessage(string text) => ShowMessages(new[] { new MessageItem(text, 0) });

    void ShowMessages(IEnumerable<MessageItem> items)
    {
        MessageList.ItemsSource = items.ToList();
        MessageList.SelectedItem = null;
    }

    // Place le curseur de l'éditeur sur la ligne du message choisi.
    // Avec focusEditor, le clavier passe aussi dans l'éditeur.
    void GoToSelectedMessage(bool focusEditor)
    {
        if (MessageList.SelectedItem is MessageItem { Line: > 0 } item)
        {
            int line = Math.Min(item.Line, Editor.Document.LineCount);
            Editor.TextArea.Caret.Position = new TextViewPosition(line, 1);
            Editor.ScrollToLine(line);
            if (focusEditor)
                Editor.TextArea.Focus();
        }
    }

    // Flèches haut/bas dans la liste : le curseur de l'éditeur suit, mais on reste dans la liste
    void OnMessageSelected(object? sender, SelectionChangedEventArgs e) => GoToSelectedMessage(false);

    // Clic sur un message : on va dans l'éditeur, sur la ligne concernée
    void OnMessageTapped(object? sender, TappedEventArgs e) => GoToSelectedMessage(true);

    // Entrée sur un message : on va dans l'éditeur, sur la ligne concernée s'il y en a une.
    // Échap : retour dans l'éditeur, sans changer de ligne.
    void OnMessageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            GoToSelectedMessage(true);
            Editor.TextArea.Focus();   // même pour un message qui n'est lié à aucune ligne
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Editor.TextArea.Focus();
        }
    }

    // ---------- Titre et barre d'état ----------

    void SetText(string text)
    {
        loading = true;
        Editor.Text = text;
        loading = false;
        modified = false;
    }

    void UpdateTitle()
    {
        string file = currentPath is null ? "sans titre" : Path.GetFileName(currentPath);
        Title = $"CPC BASIC IDE - {file}{(modified ? " *" : "")}";
    }

    void UpdateStatus()
    {
        var caret = Editor.TextArea.Caret;
        string contrast = settings.HighContrast ? "oui" : "non";
        StatusText.Text = $"Ligne {caret.Line}, colonne {caret.Column}   |   [Larg : {visibleColumns}]   |   " +
                          $"Taille du texte : {settings.FontSize}   |   Contraste élevé : {contrast}";
    }
}
