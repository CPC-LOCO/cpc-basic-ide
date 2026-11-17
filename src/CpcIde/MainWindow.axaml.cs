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
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using CpcBasic.Core;
using CpcBasic.Core.Editing;

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
    // Sorte de message (D114) : une erreur a un ✗ rouge devant, une réussite un ✓ vert, une information rien.
    enum MessageKind { Info, Success, Error }

    sealed class MessageItem
    {
        public MessageItem(string text, int line, MessageKind kind = MessageKind.Info)
        {
            Text = text;
            Line = line;
            Kind = kind;
        }

        public string Text { get; }
        public int Line { get; }
        public MessageKind Kind { get; }

        // Ce que lit un lecteur d'écran : le mot dit ce que l'icône montre
        public override string ToString() => Kind switch
        {
            MessageKind.Error => "Erreur : " + Text,
            MessageKind.Success => "Réussi : " + Text,
            _ => Text
        };
    }

    readonly EditorSettings settings = EditorSettings.Load();
    readonly BasicColorizer colorizer = new();

    string? currentPath;
    bool modified;
    bool loading;
    string savedText = "";   // texte au dernier Ouvrir / Enregistrer : une commande effacée ne compte pas comme une modification
    double charWidth;        // largeur d'un caractère de l'éditeur, en points
    int visibleColumns;      // nombre de colonnes de caractères visibles dans la fenêtre d'édition
    bool closeConfirmed;   // vrai quand on a déjà demandé quoi faire des modifications avant de fermer
    bool realByDefault;    // ce programme : variables sans signe à virgule, comme le Locomotive (D066)
    bool breakKey = true;  // ce programme : touche ESC comme le vrai BASIC (pause, « Break in N ») (D083)

    public MainWindow()
    {
        InitializeComponent();

        Editor.TextArea.TextView.LineTransformers.Add(colorizer);

        // Messages (D113) : une longue phrase passe à la ligne au lieu d'être coupée au bord du panneau
        // Une icône (D114) devant les erreurs (✗ rouge) et les réussites (✓ vert)
        MessageList.ItemTemplate = new FuncDataTemplate<MessageItem>((item, _) =>
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("28,*") };
            var icon = new TextBlock { FontWeight = FontWeight.Bold };
            if (item?.Kind == MessageKind.Error)
            {
                icon.Text = "✗";
                icon.Foreground = Make("#FF6060");
            }
            else if (item?.Kind == MessageKind.Success)
            {
                icon.Text = "✓";
                icon.Foreground = Make("#40E040");
            }
            var text = new TextBlock { Text = item?.Text ?? "", TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(text, 1);
            row.Children.Add(icon);
            row.Children.Add(text);
            return row;
        });

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
    void OnRealClick(object? sender, RoutedEventArgs e) => ToggleRealByDefault();
    void OnBreakClick(object? sender, RoutedEventArgs e) => ToggleBreakKey();
    void OnTextClick(object? sender, RoutedEventArgs e) => ToggleTextOnDisk();
    void OnWrapClick(object? sender, RoutedEventArgs e)
    {
        settings.WordWrap = !settings.WordWrap;
        settings.Save();
        ApplyAppearance();
        ShowMessage(settings.WordWrap
            ? "Lignes longues : elles continuent sur la ligne suivante de l'écran, comme sur le CPC (le numéro n'est affiché qu'une fois)."
            : "Lignes longues : sur une seule ligne, avec une barre de défilement en bas.");
    }

    void OnQuitClick(object? sender, RoutedEventArgs e) => Close();
    void OnNormalSizeClick(object? sender, RoutedEventArgs e) => SetFontSize(EditorSettings.DefaultFontSize);
    void OnShortcutsClick(object? sender, RoutedEventArgs e) => ShowShortcuts();
    void OnAboutClick(object? sender, RoutedEventArgs e) => ShowAbout();

    // Aide (D112) : les raccourcis, affichés dans le panneau des messages (lisibles par un lecteur d'écran)
    void ShowShortcuts()
    {
        ShowMessages(new[]
        {
            new MessageItem("Raccourcis clavier :", 0),
            new MessageItem("Ctrl+O : ouvrir un programme.   Ctrl+S : enregistrer.   F5 : compiler.", 0),
            new MessageItem("F6 : passer de l'éditeur aux messages, et inversement.   Entrée sur un message d'erreur : aller à la ligne.", 0),
            new MessageItem("F8 : variables sans signe entières ou à virgule.   F9 : touche ESC comme le CPC ou ignorée.", 0),
            new MessageItem("F11 : texte du programme sur la disquette (pour le vrai BASIC : LOAD\"NOM.TXT\" puis RUN).", 0),
            new MessageItem("Ctrl et + / Ctrl et - / Ctrl et 0 : taille du texte.   Ctrl+H : contraste élevé.", 0),
            new MessageItem("Alt : ouvrir la barre de menus (les lettres soulignées choisissent un menu).   F1 : cette aide.", 0)
        });
    }

    void ShowAbout()
    {
        string version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";
        ShowMessages(new[]
        {
            new MessageItem($"CPC BASIC IDE, version {version} : éditeur et compilateur de BASIC pour Amstrad CPC.", 0),
            new MessageItem("Logiciel libre (licence GPL 3), par RetroPoke : https://github.com/CPC-LOCO/cpc-basic-ide", 0)
        });
    }

    async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Entrée sur une commande du mode direct tapée sans numéro (NEW, EDIT 20, CLEAR...) : D077, D115
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && Editor.TextArea.IsKeyboardFocusWithin)
        {
            var line = Editor.Document.GetLineByNumber(Editor.TextArea.Caret.Line);
            var command = DirectCommands.Recognize(Editor.Document.GetText(line));
            if (command is not null)
            {
                e.Handled = true;
                RemoveCommandLine(line);
                await RunDirectCommandAsync(command);
            }
            return;
        }

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
        else if (e.Key == Key.F8)
        {
            e.Handled = true;
            ToggleRealByDefault();
        }
        else if (e.Key == Key.F9)
        {
            e.Handled = true;
            ToggleBreakKey();
        }
        else if (e.Key == Key.F1)
        {
            e.Handled = true;
            ShowShortcuts();
        }
        else if (e.Key == Key.F11)
        {
            e.Handled = true;
            ToggleTextOnDisk();
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

    // F8 : variables sans signe entières, ou à virgule comme le Locomotive ; mémorisé pour ce programme (D066)
    void ToggleRealByDefault()
    {
        realByDefault = !realByDefault;
        if (currentPath is not null)
        {
            settings.SetRealByDefault(currentPath, realByDefault);
            settings.Save();
        }
        UpdateOptionButtons();
        ShowMessage(realByDefault
            ? "Variables sans signe (A, B...) : nombres à virgule, comme le Locomotive BASIC. A% reste un entier."
            : "Variables sans signe (A, B...) : nombres entiers, plus rapides. A! sera un nombre à virgule.");
    }

    // F9 : touche ESC comme le vrai BASIC, ou ignorée ; mémorisé pour ce programme (D083)
    void ToggleBreakKey()
    {
        breakKey = !breakKey;
        if (currentPath is not null)
        {
            settings.SetBreakIgnored(currentPath, !breakKey);
            settings.Save();
        }
        UpdateOptionButtons();
        ShowMessage(breakKey
            ? "Touche ESC comme le vrai BASIC : une fois = pause, deux fois = « Break in … » et arrêt (sauf ON BREAK)."
            : "Touche ESC ignorée (sauf ON BREAK dans le programme) : programme un peu plus petit et plus rapide.");
    }

    // F11 : le texte du programme aussi sur la disquette (NOM.TXT), pour le vrai BASIC ; pour tous les programmes (D101)
    void ToggleTextOnDisk()
    {
        settings.TextOnDisk = !settings.TextOnDisk;
        settings.Save();
        UpdateOptionButtons();
        ShowMessage(settings.TextOnDisk
            ? "Texte sur la disquette : oui. F5 ajoute NOM.TXT ; dans l'émulateur : LOAD\"NOM.TXT\" puis RUN pour le vrai BASIC."
            : "Texte sur la disquette : non. F5 ne met que le programme compilé.");
    }

    void UpdateOptionButtons()
    {
        // Menu Options (D112) : coché quand le réglage est actif
        TextMenu.IsChecked = settings.TextOnDisk;
        BreakMenu.IsChecked = breakKey;
        RealMenu.Header = realByDefault
            ? "_Variables sans signe (A, X...) : à virgule"
            : "_Variables sans signe (A, X...) : entières";
        ContrastMenu.IsChecked = settings.HighContrast;
        WrapMenu.IsChecked = settings.WordWrap;
        UpdateStatus();
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
        // Lignes longues (D113) : à la ligne comme le CPC, ou une seule ligne avec défilement horizontal
        Editor.WordWrap = settings.WordWrap;
        Editor.HorizontalScrollBarVisibility = settings.WordWrap
            ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
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

        // Barre d'état (D113) : claire comme sous Windows ; en contraste élevé, blanc sur noir
        StatusText.FontSize = StatusRight.FontSize = Math.Max(14, settings.FontSize - 2);
        StatusText.Foreground = StatusRight.Foreground = Make(high ? "#FFFFFF" : "#000000");
        StatusBar.Background = Make(high ? "#000000" : "#F0F0F0");
        StatusBar.BorderBrush = Make(high ? "#FFFFFF" : "#C8C8C8");
        MessageSplitter.Background = Make(high ? "#FFFFFF" : "#808080");

        UpdateOptionButtons();

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
            ShowError("Impossible de lire ce fichier : son emplacement n'est pas accessible.");
            return;
        }

        try
        {
            string text = await File.ReadAllTextAsync(path);
            SetText(text);
            currentPath = path;
            realByDefault = settings.IsRealByDefault(path);   // réglage mémorisé pour ce programme (D066)
            breakKey = !settings.IsBreakIgnored(path);        // D083
            UpdateOptionButtons();
            UpdateTitle();
            ShowMessage($"Fichier ouvert : {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Impossible de lire le fichier : {ex.Message}");
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
                ShowError("Impossible d'enregistrer ici : emplacement non accessible.");
                return false;
            }
            currentPath = chosen;
            if (realByDefault || !breakKey)
            {
                settings.SetRealByDefault(chosen, realByDefault);   // les réglages suivent le programme (D066, D083)
                settings.SetBreakIgnored(chosen, !breakKey);
                settings.Save();
            }
        }

        try
        {
            await File.WriteAllTextAsync(currentPath, Editor.Text);
            savedText = Editor.Text;
            modified = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Impossible d'enregistrer le fichier : {ex.Message}");
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
            ShowError($"Le nom « {name} » ne convient pas : une disquette CPC accepte 8 caractères au plus. " +
                        "Renommez le fichier (par exemple « jeu.bas ») puis recompilez.");
            return;
        }

        CompileResult result;
        try
        {
            // RUN "nom" écrit en clair : nom.BAS du même dossier est compilé aussi, sur la même disquette (D087)
            string folder = Path.GetDirectoryName(path) ?? ".";
            ProgramSource? Find(string program)
            {
                string? file = Directory.EnumerateFiles(folder, "*.bas")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(program, StringComparison.OrdinalIgnoreCase));
                if (file is null)
                    return null;
                return new ProgramSource(File.ReadAllText(file),
                                         new CompilerOptions(TargetMachine.AllCpc, settings.IsRealByDefault(file), !settings.IsBreakIgnored(file)));
            }
            result = Compiler.CompileWithLinked(Editor.Text, name, new CompilerOptions(TargetMachine.AllCpc, realByDefault, breakKey), Find);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ShowError($"Compilation impossible : {ex.Message}");
            return;
        }

        if (!result.Success)
        {
            // une erreur dans un autre programme (RUN "nom", D087) ne mène pas à une ligne de celui-ci
            var items = result.Diagnostics.Select(d => new MessageItem(d.Message, d.File is null ? d.Line : 0, MessageKind.Error)).ToList();
            items.Add(new MessageItem($"{result.Diagnostics.Count} erreur(s). Aucun fichier n'a été créé.", 0));
            ShowMessages(items);
            return;
        }

        string dskPath = Path.ChangeExtension(path, ".dsk");
        string? textNote = null;
        if (settings.TextOnDisk)
        {
            try
            {
                result.Disk!.AddTextFile(name, "TXT", Editor.Text);   // pour le vrai BASIC : LOAD"NOM.TXT" (D101)
                textNote = $"Texte du programme aussi sur la disquette : {name}.TXT (dans l'émulateur : LOAD\"{name}.TXT\" puis RUN).";
            }
            catch (InvalidOperationException ex)
            {
                textNote = $"Le texte du programme n'a pas pu être mis sur la disquette : {ex.Message}";
            }
        }
        try
        {
            result.Disk!.Save(dskPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessages(new[]
            {
                new MessageItem("Impossible d'écrire le fichier .dsk : il est sans doute ouvert dans votre émulateur.", 0, MessageKind.Error),
                new MessageItem("Fermez l'émulateur (ou éjectez la disquette), puis compilez de nouveau.", 0),
                new MessageItem($"Fichier : {dskPath}", 0),
                new MessageItem($"Détail de Windows : {ex.Message}", 0)
            });
            return;
        }

        string modules = result.Modules.Count == 0 ? "aucun" : string.Join(", ", result.Modules);
        var done = new List<MessageItem>
        {
            new MessageItem($"Compilation réussie : {dskPath}", 0, MessageKind.Success),
            new MessageItem($"Programme Z80 : {result.BinarySize} octets. Modules liés : {modules}.", 0),
            new MessageItem("Machine : tous les CPC (le programme reconnaît la machine au démarrage). " +
                            $"Variables sans signe : {(realByDefault ? "à virgule" : "entières")}. " +
                            $"Touche ESC : {(breakKey ? "comme le CPC" : "ignorée")}.", 0)
        };
        if (textNote is not null)
            done.Add(new MessageItem(textNote, 0));
        foreach (LinkedProgram linked in result.Linked)   // D087
            done.Add(new MessageItem($"Aussi compilé et mis sur la disquette (RUN \"{linked.Name}\") : {linked.Name}.BAS, {linked.BinarySize} octets.", 0));
        foreach (string absent in result.Missing)
            done.Add(new MessageItem($"RUN \"{absent}\" : {absent}.BAS n'est pas dans le dossier ; ce programme n'est pas sur la disquette.", 0));
        done.Add(new MessageItem($"Dans l'émulateur : insérez le disque dans le lecteur A, puis tapez RUN\"{name}\"", 0));
        done.Add(new MessageItem("Avant de recompiler le même programme, fermez l'émulateur : il garde le fichier .dsk ouvert.", 0));
        ShowMessages(done);
    }

    // ---------- Panneau de messages ----------

    void ShowMessage(string text) => ShowMessages(new[] { new MessageItem(text, 0) });
    void ShowError(string text) => ShowMessages(new[] { new MessageItem(text, 0, MessageKind.Error) });

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

    // ---------- Commandes du mode direct (D077, D115) ----------

    // La ligne de la commande disparaît du programme ; si le reste est comme au dernier enregistrement, il n'est plus « modifié »
    void RemoveCommandLine(AvaloniaEdit.Document.DocumentLine line)
    {
        Editor.Document.Remove(line.Offset, line.TotalLength);
        modified = Editor.Text != savedText;
        UpdateTitle();
    }

    async Task RunDirectCommandAsync(DirectCommand command)
    {
        switch (command.Kind)
        {
            case DirectCommandKind.New:
                if (!await ConfirmDiscardOrSaveAsync())
                {
                    ShowMessage("NEW annulé : le programme est gardé.");
                    return;
                }
                SetText("");
                currentPath = null;
                UpdateTitle();
                ShowMessage("NEW : nouveau programme, sans titre. Ctrl+S l'enregistre sous un nom.");
                break;

            case DirectCommandKind.Edit:
                if (!int.TryParse(command.Argument, out int number) || number < 1 || number > 65535)
                {
                    ShowError("EDIT : il faut le numéro de la ligne, par exemple EDIT 20.");
                    return;
                }
                int? target = DirectCommands.FindLine(Editor.Text, number);
                if (target is null)
                {
                    ShowError($"EDIT {number} : cette ligne n'existe pas (Line does not exist).");
                    return;
                }
                var found = Editor.Document.GetLineByNumber(target.Value);
                Editor.TextArea.Caret.Offset = found.EndOffset;
                Editor.TextArea.Caret.BringCaretToView();
                ShowMessage($"EDIT {number} : le curseur est à la fin de la ligne {number}.");
                break;

            case DirectCommandKind.Explain:
                ShowMessage(command.Explanation!);
                break;

            default:
                ShowMessage($"{command.Name} : cette commande de l'éditeur arrive à une prochaine étape.");
                break;
        }
    }

    void SetText(string text)
    {
        loading = true;
        Editor.Text = text;
        loading = false;
        savedText = text;
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
        StatusText.Text = $"Ligne {caret.Line}, colonne {caret.Column}";
        StatusRight.Text = $"Largeur visible : {visibleColumns} colonnes";
    }
}
