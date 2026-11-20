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
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Libraries;

namespace CpcIde;

/// <summary>
/// Éditeur d'assembleur d'une commande de bibliothèque (D159). On n'y écrit que le code : la fiche (lignes « ;@ » du haut
/// du fichier : nom, valeurs, aide) est gardée telle quelle et rappelée en haut de la fenêtre. « Vérifier » (F5) passe le
/// fichier à l'assembleur du compilateur et liste les fautes (Entrée ou clic : aller à la ligne) ; « Enregistrer » (Ctrl+S)
/// écrit le fichier. Commande du projet (protégée) : lecture seule, on peut seulement vérifier.
/// </summary>
public sealed class AsmEditorWindow : Window
{
    readonly LibraryCommand command;
    readonly LibraryCatalog catalog;
    readonly string header;                   // les lignes « ;@ » du haut du fichier, avec leur fin de ligne
    readonly int headerLines;
    readonly TextEditor editor;
    readonly ListBox problems;
    readonly TextBlock status;
    readonly Action saved;
    bool modified;

    sealed record Problem(int Line, string Text)
    {
        public override string ToString() => Line > 0 ? $"ligne {Line} : {Text}" : Text;
    }

    /// <param name="saved">Appelé après un enregistrement (pour relire les bibliothèques).</param>
    public AsmEditorWindow(LibraryCommand command, LibraryCatalog catalog, int textSize, int codeSize, Action saved)
    {
        this.command = command;
        this.catalog = catalog;
        this.saved = saved;
        Title = (command.Protected ? "Code de " : "Modifier le code de ") + command.Name;
        Width = 1100;
        Height = 760;
        MinWidth = 700;
        MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontSize = textSize;

        (header, string body) = Split(command.Code);
        headerLines = header.Count(c => c == '\n');

        // Fiche rappelée en haut : écriture en BASIC, cases des valeurs, ce que doit faire le code
        var about = new StringBuilder();
        about.Append(command.Kind == LibraryKind.Routine
            ? $"Routine {command.Name} (bibliothèque {command.Library}) : appelée en assembleur par CALL {command.Name}."
            : $"{LibraryCommandFile.Signature(command)}   (bibliothèque {command.Library})");
        if (command.Parameters.Count > 0)
            about.Append("\nCases des valeurs : ").Append(string.Join(", ", command.Parameters.Select(p => p.Name)))
                 .Append($"   (par exemple LD HL,({command.Parameters[0].Name}))");
        about.Append(command.Kind switch
        {
            LibraryKind.Function => "\nUne fonction rend son résultat dans HL ; elle finit par RET.",
            LibraryKind.Routine => "\nUne routine reçoit ses valeurs dans les registres ; elle finit par RET.",
            _ => "\nLa commande finit par RET."
        });
        about.Append(command.Protected
            ? "\nCommande du projet : protégée, lecture seule (on peut la vérifier, pas l'enregistrer)."
            : "\nLa fiche (nom, valeurs, aide) se change par « Modifier la fiche » dans la fenêtre des bibliothèques.");
        var info = new TextBlock { Text = about.ToString(), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };

        editor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monospace"),
            FontSize = codeSize,
            Background = new SolidColorBrush(Color.Parse("#000080")),
            Foreground = new SolidColorBrush(Color.Parse("#FFFF00")),
            IsReadOnly = command.Protected,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Document = new TextDocument(body)
        };
        AutomationProperties.SetName(editor, "Code assembleur de " + command.Name);
        editor.TextArea.TextView.LineTransformers.Add(new AsmColorizer());
        editor.TextChanged += (_, _) =>
        {
            if (!modified)
            {
                modified = true;
                Title = "* " + Title;
            }
        };

        problems = new ListBox { MaxHeight = 160, Margin = new Thickness(0, 8, 0, 0) };
        problems.DoubleTapped += (_, _) => GoToProblem();
        problems.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                GoToProblem();
            }
        };
        status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

        var verify = MakeButton("Vérifier (F5)");
        verify.Click += (_, _) => Verify();
        var save = MakeButton("Enregistrer (Ctrl+S)");
        save.IsEnabled = !command.Protected;
        save.Click += (_, _) => Save();
        var close = MakeButton("Fermer (Échap)");
        close.Click += (_, _) => Close();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        buttons.Children.Add(verify);
        buttons.Children.Add(save);
        buttons.Children.Add(close);

        var bottom = new StackPanel();
        bottom.Children.Add(status);
        bottom.Children.Add(problems);
        bottom.Children.Add(buttons);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(info, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(info);
        root.Children.Add(bottom);
        root.Children.Add(editor);
        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                Verify();
            }
            else if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control) && !command.Protected)
            {
                e.Handled = true;
                Save();
            }
        };
        Closing += OnClosing;
        Opened += (_, _) => editor.TextArea.Focus();
    }

    Button MakeButton(string text) => new()
    {
        Content = text,
        MinHeight = 44,
        Padding = new Thickness(16, 6),
        Margin = new Thickness(8, 0, 0, 0)
    };

    // La fiche : les lignes « ;@ » du tout début du fichier ; le reste est le code
    static (string Header, string Body) Split(string text)
    {
        text = text.Replace("\r\n", "\n");
        int at = 0;
        while (at < text.Length)
        {
            int end = text.IndexOf('\n', at);
            string line = end < 0 ? text[at..] : text[at..end];
            if (!line.TrimStart().StartsWith(";@"))
                break;
            at = end < 0 ? text.Length : end + 1;
        }
        return (text[..at], text[at..]);
    }

    string FullText() => header + editor.Text.Replace("\r\n", "\n");

    // Comme le compilateur : la fiche relue, puis le code assemblé (valeurs et routines du catalogue reconnues)
    void Verify()
    {
        var list = new List<Problem>();
        string text = FullText();
        string fileName = Path.GetFileName(command.FilePath) ?? command.Name + LibraryCommandFile.Extension;
        var (_, fileErrors) = LibraryCommandFile.Parse(text, command.Library, fileName, command.Protected);
        list.AddRange(fileErrors.Select(e => new Problem(0, "fiche : " + e.Message)));

        var emitter = new Z80Emitter(0x4000);
        emitter.Label("entree");
        var used = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(text, emitter, "l:", name =>
        {
            string? label = command.Parameters.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ? "v:" + name
                          : name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) ? "entree"
                          : catalog.Find(name) is LibraryCommand other ? "x:" + other.Name : null;
            if (label is not null && label != "entree")
                used.Add(label);
            return label;
        });
        list.AddRange(errors.Select(e => new Problem(e.Line - headerLines, e.Message)));
        int size = emitter.Size;
        if (errors.Count == 0)
        {
            foreach (string label in used)
                emitter.Label(label);                     // valeurs et routines appelées : présentes à la liaison
            try
            {
                emitter.ToBytes();                        // sauts courts trop longs, par exemple
            }
            catch (InvalidOperationException ex)
            {
                list.Add(new Problem(0, ex.Message));
            }
        }

        problems.ItemsSource = list;
        if (list.Count == 0)
        {
            status.Text = $"Aucune faute. Code : {size} octets" +
                          (command.Parameters.Count > 0 ? $" (plus {2 * command.Parameters.Count} pour les cases des valeurs)." : ".") +
                          (used.Any(u => u.StartsWith("x:")) ? $" Appelle aussi : {string.Join(", ", used.Where(u => u.StartsWith("x:")).Select(u => u[2..]))}." : "");
            status.Foreground = new SolidColorBrush(Color.Parse("#008000"));
        }
        else
        {
            status.Text = $"{list.Count} faute(s) : Entrée ou double clic sur une faute pour aller à sa ligne.";
            status.Foreground = new SolidColorBrush(Color.Parse("#C00000"));
            problems.SelectedIndex = 0;
        }
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

    bool Save()
    {
        if (command.Protected || command.FilePath is null)
            return false;
        try
        {
            File.WriteAllText(command.FilePath, FullText(), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            status.Text = $"Le fichier n'a pas pu être écrit ({ex.Message}).";
            status.Foreground = new SolidColorBrush(Color.Parse("#C00000"));
            return false;
        }
        modified = false;
        Title = (Title ?? "").TrimStart('*', ' ');
        saved();
        Verify();
        status.Text = "Enregistré. " + status.Text;
        return true;
    }

    bool closingConfirmed;

    async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!modified || command.Protected || closingConfirmed)
            return;
        e.Cancel = true;
        SaveChangesDialog.Choice choice = await new SaveChangesDialog(command.Name + LibraryCommandFile.Extension)
            .ShowDialog<SaveChangesDialog.Choice>(this);
        if (choice == SaveChangesDialog.Choice.Cancel || (choice == SaveChangesDialog.Choice.Save && !Save()))
            return;
        closingConfirmed = true;
        Close();
    }
}
