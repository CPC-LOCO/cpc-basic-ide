// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CpcBasic.Core.Assembly;
using CpcBasic.Core.Libraries;

namespace CpcIde;

/// <summary>
/// « Assembleur seul » (idée de l'utilisateur, D163, D164) : une fenêtre pour écrire un programme entièrement en assembleur
/// (style WinAPE), l'enregistrer en .asm et le compiler sur une disquette (.dsk à côté du .asm : NOM.BIN et son chargeur
/// NOM.BAS). Menus Fichier et Compiler ; F5 compiler, Maj+F5 vérifier, Ctrl+N / Ctrl+O / Ctrl+S / Ctrl+Maj+S.
/// </summary>
public sealed class AsmProgramWindow : Window
{
    const string Starter = """
        ; Mon programme en assembleur (comme dans WinAPE)
        ; ORG : adresse où le programme est chargé ; RUN : adresse où il démarre
                ORG &8000
                RUN DEBUT

        DEBUT:  LD HL,TEXTE         ; affiche le texte, lettre par lettre
        BOUCLE: LD A,(HL)
                OR A
                RET Z               ; 0 : fin du texte, retour au BASIC
                CALL &BB5A          ; TXT OUTPUT : affiche la lettre de A
                INC HL
                JR BOUCLE

        TEXTE:  DEFM "BONJOUR DE L'ASSEMBLEUR !"
                DEFB 13,10,0
        """;

    static readonly FilePickerFileType AsmFileType = new("Programme assembleur (*.asm)") { Patterns = ["*.asm"] };

    readonly TextEditor editor;
    readonly ListBox problems;
    readonly TextBlock status;
    string? path;
    bool modified;
    bool closingConfirmed;

    sealed record Problem(int Line, string Text)
    {
        public override string ToString() => Line > 0 ? $"ligne {Line} : {Text}" : Text;
    }

    public AsmProgramWindow(int textSize, int codeSize, string? openPath = null)
    {
        Width = 1100;
        Height = 780;
        MinWidth = 700;
        MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontSize = textSize;

        var menu = new Menu { FontSize = textSize };
        menu.Items.Add(MenuOf("_Fichier",
            Item("_Nouveau", "Ctrl+N", () => _ = NewAsync()),
            Item("_Ouvrir...", "Ctrl+O", () => _ = OpenAsync()),
            Item("_Enregistrer", "Ctrl+S", () => _ = SaveAsync()),
            Item("Enregistrer _sous...", "Ctrl+Shift+S", () => _ = SaveAsAsync()),
            Item("_Fermer", "Échap", Close)));
        menu.Items.Add(MenuOf("_Compiler",
            Item("_Compiler (disquette)", "F5", () => _ = CompileAsync()),
            Item("_Vérifier sans enregistrer", "Shift+F5", () => Check(writeDisk: false))));

        editor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monospace"),
            FontSize = codeSize,
            Background = new SolidColorBrush(Color.Parse("#000080")),
            Foreground = new SolidColorBrush(Color.Parse("#FFFF00")),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Document = new TextDocument(Starter.Replace("\r\n", "\n") + "\n")
        };
        AutomationProperties.SetName(editor, "Programme assembleur");
        editor.TextArea.TextView.LineTransformers.Add(new AsmColorizer());
        editor.TextChanged += (_, _) =>
        {
            if (!modified)
            {
                modified = true;
                UpdateTitle();
            }
        };

        status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        problems = new ListBox { MaxHeight = 160, Margin = new Thickness(0, 6, 0, 0) };
        problems.DoubleTapped += (_, _) => GoToProblem();
        problems.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                GoToProblem();
            }
        };
        status.Text = "F5 : compiler (enregistre le .asm et écrit la disquette .dsk à côté).   Maj+F5 : vérifier sans rien écrire.";

        var bottom = new StackPanel { Margin = new Thickness(12, 0, 12, 12) };
        bottom.Children.Add(status);
        bottom.Children.Add(problems);

        var root = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(menu);
        root.Children.Add(bottom);
        editor.Margin = new Thickness(12, 8, 12, 0);
        root.Children.Add(editor);
        Content = root;

        KeyDown += OnKeyDown;
        Closing += OnClosing;
        Opened += async (_, _) =>
        {
            if (openPath is not null)
                await LoadAsync(openPath);
            editor.TextArea.Focus();
        };
        modified = false;
        UpdateTitle();
    }

    static MenuItem MenuOf(string header, params MenuItem[] items)
    {
        var menu = new MenuItem { Header = header };
        foreach (MenuItem item in items)
            menu.Items.Add(item);
        return menu;
    }

    static MenuItem Item(string header, string gesture, Action action)
    {
        var item = new MenuItem { Header = header };
        if (gesture != "Échap")
            item.InputGesture = KeyGesture.Parse(gesture);
        item.Click += (_, _) => action();
        return item;
    }

    async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
        else if (e.Key == Key.F5)
        {
            e.Handled = true;
            if (shift)
                Check(writeDisk: false);
            else
                await CompileAsync();
        }
        else if (ctrl && e.Key == Key.S)
        {
            e.Handled = true;
            await (shift ? SaveAsAsync() : SaveAsync());
        }
        else if (ctrl && e.Key == Key.O)
        {
            e.Handled = true;
            await OpenAsync();
        }
        else if (ctrl && e.Key == Key.N)
        {
            e.Handled = true;
            await NewAsync();
        }
    }

    void UpdateTitle() =>
        Title = (modified ? "* " : "") + "Assembleur seul - " + (path is null ? "sans titre" : Path.GetFileName(path));

    void SetText(string text)
    {
        editor.Document = new TextDocument(text.Replace("\r\n", "\n"));
        modified = false;
        problems.ItemsSource = null;
        UpdateTitle();
    }

    // ----- fichiers

    async Task<bool> ConfirmDiscardAsync()
    {
        if (!modified)
            return true;
        SaveChangesDialog.Choice choice = await new SaveChangesDialog(path is null ? "sans titre.asm" : Path.GetFileName(path))
            .ShowDialog<SaveChangesDialog.Choice>(this);
        return choice switch
        {
            SaveChangesDialog.Choice.Save => await SaveAsync(),
            SaveChangesDialog.Choice.Discard => true,
            _ => false
        };
    }

    async Task NewAsync()
    {
        if (!await ConfirmDiscardAsync())
            return;
        path = null;
        SetText(Starter + "\n");
        status.Text = "Nouveau programme : un petit exemple est déjà écrit (il affiche un texte). F5 pour le compiler.";
    }

    async Task OpenAsync()
    {
        if (!await ConfirmDiscardAsync())
            return;
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Ouvrir un programme assembleur",
            AllowMultiple = false,
            FileTypeFilter = [AsmFileType, FilePickerFileTypes.All]
        });
        string? chosen = files.Count == 0 ? null : files[0].TryGetLocalPath();
        if (chosen is not null)
            await LoadAsync(chosen);
    }

    async Task LoadAsync(string file)
    {
        try
        {
            string text = await File.ReadAllTextAsync(file);
            path = file;
            SetText(text);
            status.Text = $"Ouvert : {file}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            status.Text = $"Le fichier n'a pas pu être lu ({ex.Message}).";
        }
    }

    async Task<bool> SaveAsync()
    {
        if (path is null)
            return await SaveAsAsync();
        try
        {
            await File.WriteAllTextAsync(path, editor.Text.Replace("\r\n", "\n"), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            status.Text = $"Le fichier n'a pas pu être enregistré ({ex.Message}).";
            return false;
        }
        modified = false;
        UpdateTitle();
        return true;
    }

    async Task<bool> SaveAsAsync()
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le programme assembleur",
            DefaultExtension = "asm",
            SuggestedFileName = path is null ? "jeu.asm" : Path.GetFileName(path),
            FileTypeChoices = [AsmFileType]
        });
        string? chosen = file?.TryGetLocalPath();
        if (chosen is null)
            return false;
        path = chosen;
        return await SaveAsync();
    }

    // ----- vérifier, compiler

    async Task CompileAsync()
    {
        if (!await SaveAsync() || path is null)
            return;
        Check(writeDisk: true);
    }

    void Check(bool writeDisk)
    {
        AsmProgramResult program = AsmProgram.Build(editor.Text.Replace("\r\n", "\n"), LibraryFolders.Load());
        var list = program.Errors.Select(e => new Problem(e.Line, e.Message)).ToList();
        problems.ItemsSource = list;
        if (!program.Success)
        {
            status.Text = $"{list.Count} faute(s) : Entrée ou double clic sur une faute pour aller à sa ligne. Rien n'a été écrit.";
            status.Foreground = new SolidColorBrush(Color.Parse("#C00000"));
            problems.SelectedIndex = 0;
            return;
        }

        int end = program.Origin + program.Code!.Length - 1;
        string routines = program.Routines.Count == 0 ? ""
            : $" Routines des bibliothèques ajoutées : {string.Join(", ", program.Routines.Select(r => $"{r.Name} ({r.Library})"))}.";
        string summary = $"de &{program.Origin:X4} à &{end:X4} ({program.Code.Length} octets), lancement en &{program.Entry:X4}.{routines}";
        status.Foreground = new SolidColorBrush(Color.Parse("#008000"));
        if (!writeDisk)
        {
            status.Text = $"Vérification réussie : aucune faute. Programme {summary} Rien n'a été écrit.";
            return;
        }

        string name = Path.GetFileNameWithoutExtension(path!).ToUpperInvariant();
        if (name.Length is 0 or > 8)
        {
            status.Text = $"Le nom « {name} » ne convient pas : une disquette CPC accepte 8 caractères au plus. Renommez le fichier (Enregistrer sous).";
            status.Foreground = new SolidColorBrush(Color.Parse("#C00000"));
            return;
        }
        string dsk = Path.ChangeExtension(path!, ".dsk");
        try
        {
            AsmProgram.ToDisk(program, name).Save(dsk);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            status.Text = $"Impossible d'écrire {dsk} : il est sans doute ouvert dans votre émulateur (fermez-le, puis recompilez). Détail : {ex.Message}";
            status.Foreground = new SolidColorBrush(Color.Parse("#C00000"));
            return;
        }
        status.Text = $"Compilation réussie : {dsk}. Programme {summary} Dans l'émulateur : insérez le disque dans le lecteur A, puis tapez RUN\"{name}\".";
    }

    void GoToProblem()
    {
        if (problems.SelectedItem is not Problem { Line: > 0 } problem)
            return;
        int line = Math.Min(problem.Line, editor.Document.LineCount);
        editor.TextArea.Caret.Position = new TextViewPosition(line, 1);
        editor.ScrollToLine(line);
        editor.TextArea.Focus();
    }

    async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!modified || closingConfirmed)
            return;
        e.Cancel = true;
        if (!await ConfirmDiscardAsync())
            return;
        closingConfirmed = true;
        Close();
    }
}
