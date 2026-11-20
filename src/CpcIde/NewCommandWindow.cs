// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CpcBasic.Core.Libraries;

namespace CpcIde;

/// <summary>
/// Questionnaire d'une nouvelle commande (D144, D157) : nom, genre, bibliothèque, aide, auteur et valeurs (nom, limites,
/// facultative et ce que reçoit la routine quand elle est sautée, rôle). « Créer » écrit le fichier dans le dossier des
/// bibliothèques du programmeur, avec un code de départ ; la fenêtre rend alors (bibliothèque, nom), sinon null.
/// Le texte écrit est relu par le même lecteur que le compilateur : ses messages d'erreur sont montrés ici.
/// </summary>
public sealed class NewCommandWindow : Window
{
    readonly double size;
    readonly LibraryCatalog catalog;
    readonly TextBox name, library, help, author;
    readonly RadioButton isCommand, isFunction, isRoutine;
    readonly StackPanel rows = new() { Spacing = 6 };
    readonly TextBlock valuesTitle, problems, preview;
    readonly Grid header;
    readonly Button addValue;
    readonly List<ValueRow> values = [];
    readonly LibraryCommand? editing;      // « Modifier la fiche » (D160) : la commande d'avant ; null pour une nouvelle

    sealed record ValueRow(Grid Line, TextBox Name, TextBox Min, TextBox Max, CheckBox Optional, TextBox Skipped, TextBox Role);

    /// <param name="editing">Pour « Modifier la fiche » (D160) : la commande à modifier (son code est gardé) ; null : nouvelle.</param>
    public NewCommandWindow(LibraryCatalog catalog, string suggestedLibrary, int fontSize, LibraryCommand? editing = null)
    {
        this.catalog = catalog;
        this.editing = editing;
        size = fontSize;
        Title = editing is null ? "Nouvelle commande" : $"Modifier la fiche de {editing.Name}";
        Width = 1000;
        Height = 720;
        MinWidth = 700;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontSize = size;

        name = Box("par exemple LUNE");
        library = Box("par exemple GRAPHIQUE");
        library.Text = suggestedLibrary;
        author = Box("votre nom (facultatif)");
        help = Box("ce que fait la commande, comment l'écrire (plusieurs lignes possibles)");
        help.AcceptsReturn = true;
        help.TextWrapping = TextWrapping.Wrap;
        help.Height = size * 5;

        isCommand = new RadioButton { Content = "commande (STAR x, y...)", GroupName = "genre", IsChecked = true };
        isFunction = new RadioButton { Content = "fonction (A = LUNE(x))", GroupName = "genre" };
        isRoutine = new RadioButton { Content = "routine (pour l'assembleur seulement)", GroupName = "genre" };
        isRoutine.IsCheckedChanged += (_, _) => UpdateValuesState();
        var kinds = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (RadioButton r in new[] { isCommand, isFunction, isRoutine })
        {
            r.Margin = new Thickness(0, 0, 24, 0);
            kinds.Children.Add(r);
        }

        string existing = catalog.Libraries.Count == 0 ? "aucune pour l'instant" : string.Join(", ", catalog.Libraries);
        var form = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto") };
        AddRow(form, 0, "Nom :", name);
        AddRow(form, 1, "Genre :", kinds);
        AddRow(form, 2, "Bibliothèque :", Stack(library, Note($"Bibliothèques existantes : {existing}. Un nom nouveau crée une bibliothèque.")));
        AddRow(form, 3, "Aide :", help);
        AddRow(form, 4, "Auteur :", author);

        valuesTitle = new TextBlock { Text = "Valeurs (dans l'ordre où on les écrit en BASIC ; les facultatives à la fin) :", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 12, 0, 4) };
        // Repère (idée de l'utilisateur, D158) : l'écriture en BASIC, mise à jour à chaque frappe
        preview = new TextBlock { FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };
        // Titres gris des cases d'une valeur, avec des exemples (idée de l'utilisateur, D158)
        header = new Grid { ColumnDefinitions = new ColumnDefinitions(ValueColumns), Margin = new Thickness(0, 6, 0, 0) };
        string[] titles = ["nom (ex : X, STYLO)", "de", "à", "", "si sautée", "rôle (ex : couleur du contour)"];
        for (int i = 0; i < titles.Length; i++)
        {
            var title = new TextBlock { Text = titles[i], FontSize = size - 3, Opacity = 0.7, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 6, 0) };
            Grid.SetColumn(title, i);
            header.Children.Add(title);
        }
        name.TextChanged += (_, _) => UpdatePreview();
        foreach (RadioButton r in new[] { isCommand, isFunction, isRoutine })
            r.IsCheckedChanged += (_, _) => UpdatePreview();
        addValue = new Button { Content = "Ajouter une valeur", Margin = new Thickness(0, 6, 0, 0) };
        addValue.Click += (_, _) => AddValue();

        problems = new TextBlock { Foreground = new SolidColorBrush(Color.Parse("#C00000")), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

        var create = new Button { Content = editing is null ? "Créer" : "Enregistrer", IsDefault = true, MinHeight = size * 2.4, Padding = new Thickness(20, 4), Margin = new Thickness(0, 0, 8, 0) };
        create.Click += (_, _) => Create();
        var cancel = new Button { Content = "Annuler (Échap)", IsCancel = true, MinHeight = size * 2.4, Padding = new Thickness(20, 4) };
        cancel.Click += (_, _) => Close(null);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(create);
        buttons.Children.Add(cancel);

        var body = new StackPanel { Spacing = 4 };
        body.Children.Add(form);
        body.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#C00000")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 4),
            Margin = new Thickness(0, 8, 0, 0),
            Child = preview
        });
        body.Children.Add(valuesTitle);
        body.Children.Add(Note("Pour chaque valeur : son nom (lettres sans accent, chiffres et _, sans espace : c'est aussi le nom de sa case " +
                               "en assembleur, par exemple LD HL,(X)), la plus petite et la plus grande valeur permises, et son rôle."));
        body.Children.Add(Note("Facultative : on peut la sauter en BASIC (STAR 160,100,30,80,5 sans le stylo). La case reçoit alors le nombre " +
                               "« si sautée ». Un nombre hors des limites, comme -1, dit à la routine que la valeur a été sautée (STAR prend alors " +
                               "le stylo du moment) ; un nombre dans les limites, comme 0, sert simplement de valeur par défaut."));
        body.Children.Add(header);
        body.Children.Add(rows);
        body.Children.Add(addValue);
        body.Children.Add(problems);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(new ScrollViewer { Content = body });
        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(null);
            }
        };
        Opened += (_, _) => name.Focus();
        if (editing is not null)
            FillFrom(editing);
        UpdatePreview();
    }

    // Fiche déjà remplie avec la commande à modifier (D160)
    void FillFrom(LibraryCommand command)
    {
        name.Text = command.Name;
        isCommand.IsChecked = command.Kind == LibraryKind.Command;
        isFunction.IsChecked = command.Kind == LibraryKind.Function;
        isRoutine.IsChecked = command.Kind == LibraryKind.Routine;
        library.Text = command.Library;
        help.Text = command.Help;
        author.Text = command.Author;
        foreach (LibraryParameter p in command.Parameters)
        {
            AddValue();
            ValueRow row = values[^1];
            row.Name.Text = p.Name;
            row.Min.Text = p.Min.ToString(CultureInfo.InvariantCulture);
            row.Max.Text = p.Max.ToString(CultureInfo.InvariantCulture);
            row.Optional.IsChecked = p.IsOptional;
            if (p.Default is int skipped)
                row.Skipped.Text = skipped.ToString(CultureInfo.InvariantCulture);
            row.Role.Text = p.Description;
        }
        UpdateValuesState();
    }

    TextBox Box(string watermark) => new() { PlaceholderText = watermark, FontSize = size };

    TextBlock Note(string text) => new() { Text = text, FontSize = size - 2, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };

    static StackPanel Stack(params Control[] controls)
    {
        var panel = new StackPanel { Spacing = 2 };
        foreach (Control c in controls)
            panel.Children.Add(c);
        return panel;
    }

    static void AddRow(Grid grid, int row, string label, Control control)
    {
        var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 12, 6) };
        Grid.SetRow(text, row);
        grid.Children.Add(text);
        control.Margin = new Thickness(0, 6, 0, 6);
        Grid.SetRow(control, row);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
    }

    // Une ligne de valeur : nom, de, à, facultative, si sautée, rôle, retirer
    void AddValue()
    {
        if (values.Count >= LibraryCommandFile.MaxParameters)
        {
            problems.Text = $"{LibraryCommandFile.MaxParameters} valeurs au plus.";
            return;
        }
        var line = new Grid { ColumnDefinitions = new ColumnDefinitions(ValueColumns) };
        TextBox valueName = Box("nom"), min = Box("de"), max = Box("à"), skipped = Box("si sautée"), role = Box("rôle");
        min.Text = "0";
        max.Text = "255";
        skipped.Text = "-1";
        skipped.IsEnabled = false;
        var optional = new CheckBox { Content = "facultative", VerticalAlignment = VerticalAlignment.Center };
        optional.IsCheckedChanged += (_, _) => skipped.IsEnabled = optional.IsChecked == true;
        var remove = new Button { Content = "Retirer" };
        Control[] cells = [valueName, min, max, optional, skipped, role, remove];
        for (int i = 0; i < cells.Length; i++)
        {
            Grid.SetColumn(cells[i], i);
            cells[i].Margin = new Thickness(0, 0, 6, 0);
            line.Children.Add(cells[i]);
        }
        var row = new ValueRow(line, valueName, min, max, optional, skipped, role);
        remove.Click += (_, _) =>
        {
            values.Remove(row);
            rows.Children.Remove(line);
            UpdatePreview();
        };
        valueName.TextChanged += (_, _) => UpdatePreview();
        optional.IsCheckedChanged += (_, _) => UpdatePreview();
        values.Add(row);
        rows.Children.Add(line);
        UpdatePreview();
        valueName.Focus();
    }

    const string ValueColumns = "160,90,90,Auto,90,*,Auto";

    // Écriture en BASIC de la commande telle qu'elle est remplie ; un nom qui ne convient pas est signalé tout de suite
    void UpdatePreview()
    {
        string commandName = (name.Text ?? "").Trim().ToUpperInvariant();
        string shownName = commandName.Length == 0 ? "NOM" : commandName;
        var notes = new List<string>();
        if (commandName.Length > 0 && LibraryCommandFile.CheckName(commandName, "le nom") is string nameError)
            notes.Add(nameError);
        if (isRoutine.IsChecked == true)
        {
            preview.Text = $"Écriture en assembleur : CALL {shownName}   (une routine ne s'écrit pas en BASIC)" + Notes(notes);
            return;
        }
        var text = new StringBuilder();
        foreach (ValueRow row in values)
        {
            string valueName = (row.Name.Text ?? "").Trim();
            string shown = valueName.Length == 0 ? "?" : valueName.ToLowerInvariant();
            if (valueName.Length > 0 && LibraryCommandFile.CheckName(valueName, "un nom de valeur") is string valueError)
            {
                shown = $"«{valueName}»?";
                notes.Add(valueError);
            }
            string separator = text.Length == 0 ? "" : ", ";
            text.Append(row.Optional.IsChecked == true ? $" [{separator}{shown}]" : separator + shown);
        }
        string names = text.ToString().Trim();
        string written = isFunction.IsChecked == true
            ? (values.Count == 0 ? $"A = {shownName}" : $"A = {shownName}({names})")
            : (values.Count == 0 ? shownName : $"{shownName} {names}");
        preview.Text = "Écriture en BASIC : " + written + Notes(notes);
    }

    static string Notes(List<string> notes) => notes.Count == 0 ? "" : "\n" + string.Join("\n", notes.Select(n => "À corriger : " + n));

    void UpdateValuesState()
    {
        bool routine = isRoutine.IsChecked == true;
        rows.IsEnabled = addValue.IsEnabled = !routine;
        valuesTitle.Text = routine
            ? "Valeurs : une routine n'en a pas, elle reçoit ses valeurs dans les registres (dites lesquels dans l'aide)."
            : "Valeurs (dans l'ordre où on les écrit en BASIC ; les facultatives à la fin) :";
    }

    void Create()
    {
        var errors = new List<string>();
        string commandName = (name.Text ?? "").Trim().ToUpperInvariant();
        string libraryName = (library.Text ?? "").Trim().ToUpperInvariant();
        LibraryKind kind = isRoutine.IsChecked == true ? LibraryKind.Routine
                         : isFunction.IsChecked == true ? LibraryKind.Function : LibraryKind.Command;

        if (commandName.Length == 0)
            errors.Add("Donnez un nom à la commande.");
        if (LibraryCatalog.CheckLibraryName(libraryName) is string libraryError)
            errors.Add(libraryName.Length == 0 ? "Donnez le nom de la bibliothèque (par exemple GRAPHIQUE)." : libraryError);
        if (commandName.Length > 0 && catalog.Find(commandName) is LibraryCommand other && other.Name != editing?.Name)
            errors.Add($"Le nom {commandName} existe déjà dans la bibliothèque {other.Library} : choisissez un autre nom.");

        var parameters = new List<LibraryParameter>();
        if (kind != LibraryKind.Routine)
        {
            foreach (ValueRow row in values)
            {
                string valueName = (row.Name.Text ?? "").Trim().ToUpperInvariant();
                string label = valueName.Length > 0 ? valueName : $"la valeur {values.IndexOf(row) + 1}";
                if (valueName.Length == 0)
                    errors.Add($"Donnez un nom à {label}.");
                int? min = Number(row.Min, $"{label} : la plus petite valeur", errors);
                int? max = Number(row.Max, $"{label} : la plus grande valeur", errors);
                int? skipped = row.Optional.IsChecked == true ? Number(row.Skipped, $"{label} : la valeur si elle est sautée", errors) : null;
                if (valueName.Length > 0 && min is not null && max is not null && (row.Optional.IsChecked != true || skipped is not null))
                    parameters.Add(new LibraryParameter(valueName, min.Value, max.Value, (row.Role.Text ?? "").Trim(), skipped));
            }
        }

        if (errors.Count == 0)
        {
            // le fichier est écrit puis relu par le lecteur du compilateur : mêmes vérifications que pour un fichier écrit à la main
            // modifier la fiche : le code d'avant est gardé tel quel (D160)
            string code = editing is null
                ? LibraryCommandFile.StarterCode(commandName, kind, parameters)
                : LibraryCommandFile.SplitHeader(editing.Code).Body.TrimStart('\n');
            string text = LibraryCommandFile.Compose(commandName, kind, parameters, help.Text ?? "", author.Text ?? "", code);
            string fileName = commandName + LibraryCommandFile.Extension;
            var (_, fileErrors) = LibraryCommandFile.Parse(text, libraryName, fileName, isProtected: false);
            errors.AddRange(fileErrors.Select(e => Explain(e.Message)));
            if (errors.Count == 0)
            {
                string folder = Path.Combine(LibraryFolders.User, libraryName);
                string path = Path.Combine(folder, fileName);
                string? before = editing?.FilePath;
                bool samePlace = before is not null && string.Equals(Path.GetFullPath(before), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
                if (File.Exists(path) && !samePlace)
                    errors.Add($"Le fichier existe déjà : {path}");
                else
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        File.WriteAllText(path, text, new UTF8Encoding(false));
                        if (before is not null && !samePlace)
                            File.Delete(before);            // nom ou bibliothèque changé : l'ancien fichier part (D160)
                        Close((libraryName, commandName));
                        return;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        errors.Add($"Le fichier n'a pas pu être écrit ({ex.Message}).");
                    }
                }
            }
        }
        problems.Text = string.Join("\n", errors);
    }

    // Un nombre entier de -32768 à 65535 (comme dans le fichier : décimal, &FF ou #FF)
    static int? Number(TextBox box, string what, List<string> errors)
    {
        string text = (box.Text ?? "").Trim();
        int value;
        bool ok = text.Length > 1 && text[0] is '&' or '#'
            ? int.TryParse(text[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value)
            : int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        if (ok && value is >= -32768 and <= 65535)
            return value;
        errors.Add($"{what} doit être un nombre entier de -32768 à 65535{(text.Length > 0 ? $" (« {text} »)" : "")}.");
        return null;
    }

    // Les messages du lecteur parlent du fichier (« ;@VALEUR ») : ici on parle du questionnaire
    static string Explain(string message) =>
        message.Replace("une VALEUR obligatoire ne peut pas venir après une OPTION : mettez les OPTION à la fin.",
                        "une valeur obligatoire ne peut pas venir après une valeur facultative : mettez les facultatives à la fin.");
}
