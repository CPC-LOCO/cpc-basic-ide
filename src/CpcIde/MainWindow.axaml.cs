// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
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

    string? currentPath;
    bool modified;
    bool loading;

    public MainWindow()
    {
        InitializeComponent();

        Editor.LineNumbersForeground = new SolidColorBrush(Color.Parse("#8080FF"));
        Editor.TextArea.TextView.LineTransformers.Add(new BasicColorizer());
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        Editor.TextChanged += (_, _) =>
        {
            if (loading) return;
            modified = true;
            UpdateTitle();
        };
        KeyDown += OnWindowKeyDown;

        SetText(StartProgram);
        UpdateTitle();
        UpdateStatus();
    }

    // ---------- Boutons et raccourcis ----------

    async void OnOpenClick(object? sender, RoutedEventArgs e) => await OpenFileAsync();
    async void OnSaveClick(object? sender, RoutedEventArgs e) => await SaveAsync();
    async void OnCompileClick(object? sender, RoutedEventArgs e) => await CompileAsync();

    async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await CompileAsync();
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
    }

    // ---------- Ouvrir et enregistrer ----------

    async Task OpenFileAsync()
    {
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
            ShowMessage($"Impossible d'écrire le fichier .dsk : {ex.Message}");
            return;
        }

        string modules = result.Modules.Count == 0 ? "aucun" : string.Join(", ", result.Modules);
        ShowMessages(new[]
        {
            new MessageItem($"Compilation réussie : {dskPath}", 0),
            new MessageItem($"Programme Z80 : {result.BinarySize} octets. Modules liés : {modules}.", 0),
            new MessageItem($"Dans l'émulateur : insérez le disque dans le lecteur A, puis tapez RUN\"{name}\"", 0)
        });
    }

    // ---------- Panneau de messages ----------

    void ShowMessage(string text) => ShowMessages(new[] { new MessageItem(text, 0) });

    void ShowMessages(IEnumerable<MessageItem> items)
    {
        MessageList.ItemsSource = items.ToList();
        MessageList.SelectedItem = null;
    }

    // Un clic sur une erreur place le curseur sur la ligne concernée
    void OnMessageSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (MessageList.SelectedItem is MessageItem { Line: > 0 } item)
        {
            int line = Math.Min(item.Line, Editor.Document.LineCount);
            Editor.TextArea.Caret.Position = new TextViewPosition(line, 1);
            Editor.ScrollToLine(line);
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
        StatusText.Text = $"Ligne {caret.Line}, colonne {caret.Column}";
    }
}
