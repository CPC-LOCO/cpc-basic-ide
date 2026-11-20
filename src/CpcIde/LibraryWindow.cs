// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CpcBasic.Core.Libraries;

namespace CpcIde;

/// <summary>
/// Fenêtre des bibliothèques (D156, idée de l'utilisateur) : un onglet par bibliothèque en haut ; à gauche ses commandes
/// (liste avec barre de défilement) ; à droite, la commande choisie : bonne écriture, aide, valeurs. Un onglet de plus
/// pour les fichiers refusés, s'il y en a. Fenêtre à part : l'éditeur reste utilisable pendant qu'elle est ouverte.
/// Échap ou « Fermer » : fermer. Texte de 15 points au départ ; Ctrl et + / Ctrl et - pour l'agrandir ou le réduire (gardé).
/// </summary>
public sealed class LibraryWindow : Window
{
    static double TextSize = 15;
    readonly TabControl tabs = new() { FontSize = TextSize };
    readonly Button close;
    readonly Action<int> remember;
    readonly Func<LibraryCatalog> reload;
    readonly int codeSize;
    readonly Button code;
    LibraryCatalog? shown;
    LibraryCommand? selected;              // commande choisie dans l'onglet ouvert (D159)

    /// <param name="size">Taille du texte (réglage de l'éditeur) ; <paramref name="remember"/> garde la nouvelle taille.</param>
    /// <param name="reload">Relit les bibliothèques (après une nouvelle commande) et rend le catalogue.</param>
    /// <param name="codeSize">Taille du texte de l'éditeur (pour l'éditeur d'assembleur).</param>
    public LibraryWindow(int size, Action<int> remember, Func<LibraryCatalog> reload, int codeSize)
    {
        this.codeSize = codeSize;
        TextSize = size;
        this.remember = remember;
        this.reload = reload;
        tabs.FontSize = TextSize;
        Title = "Bibliothèques";
        Width = 1050;
        Height = 650;
        MinWidth = 600;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        close = new Button
        {
            Content = "Fermer (Échap)",
            FontSize = TextSize,
            MinHeight = 44,
            Padding = new Thickness(16, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsCancel = true
        };
        close.Click += (_, _) => Close();

        folders = new TextBlock { FontSize = TextSize - 2, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 16, 0) };
        newCommand = new Button
        {
            Content = "Nouvelle commande...",
            FontSize = TextSize,
            MinHeight = 44,
            Padding = new Thickness(16, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        newCommand.Click += async (_, _) => await CreateCommandAsync();
        code = new Button
        {
            Content = "Modifier le code...",
            FontSize = TextSize,
            MinHeight = 44,
            Padding = new Thickness(16, 6),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };
        code.Click += (_, _) => OpenCode();
        tabs.SelectionChanged += (_, _) => RefreshSelected();
        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"), Margin = new Thickness(0, 12, 0, 0) };
        bottom.Children.Add(folders);
        Grid.SetColumn(code, 1);
        bottom.Children.Add(code);
        Grid.SetColumn(newCommand, 2);
        bottom.Children.Add(newCommand);
        Grid.SetColumn(close, 3);
        bottom.Children.Add(close);

        var root = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);
        root.Children.Add(tabs);
        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key is Key.OemPlus or Key.Add or Key.OemMinus or Key.Subtract)
            {
                e.Handled = true;
                int next = Math.Clamp((int)TextSize + (e.Key is Key.OemPlus or Key.Add ? 1 : -1), EditorSettings.MinFontSize, 30);
                TextSize = next;
                this.remember(next);
                tabs.FontSize = close.FontSize = newCommand.FontSize = code.FontSize = next;
                folders.FontSize = next - 2;
                if (shown is not null)
                    Fill(shown);
            }
        };
    }

    readonly TextBlock folders;
    readonly Button newCommand;

    // La commande choisie dans l'onglet ouvert ; le bouton du code dit « Voir » pour une commande protégée (D159)
    void RefreshSelected()
    {
        selected = ((tabs.SelectedItem as TabItem)?.Tag as Func<LibraryCommand?>)?.Invoke();
        code.IsEnabled = selected?.FilePath is not null;
        code.Content = selected?.Protected == true ? "Voir le code..." : "Modifier le code...";
    }

    // « Modifier le code... » (D159) : éditeur d'assembleur, à côté (on peut en ouvrir plusieurs)
    void OpenCode()
    {
        if (selected is null || shown is null)
            return;
        LibraryCommand command = selected;
        var editor = new AsmEditorWindow(command, shown, (int)TextSize, codeSize,
                                         () => Fill(reload(), command.Library, command.Name));
        editor.Show(this);
    }

    // « Nouvelle commande... » (D157) : questionnaire ; la commande créée est montrée dans son onglet
    async System.Threading.Tasks.Task CreateCommandAsync()
    {
        string suggested = (tabs.SelectedItem as TabItem)?.Header as string ?? "";
        if (shown is null || !shown.Libraries.Contains(suggested))
            suggested = shown?.Libraries.FirstOrDefault() ?? "";
        var dialog = new NewCommandWindow(shown ?? LibraryCatalog.Empty, suggested, (int)TextSize);
        (string Library, string Name)? created = await dialog.ShowDialog<(string, string)?>(this);
        if (created is null)
            return;
        Fill(reload(), created.Value.Library, created.Value.Name);
    }

    /// <summary>Remplit la fenêtre avec le catalogue (appelé à l'ouverture et chaque fois qu'on la redemande).</summary>
    public void Fill(LibraryCatalog catalog, string? selectLibrary = null, string? selectName = null)
    {
        shown = catalog;
        string? current = selectLibrary ?? (tabs.SelectedItem as TabItem)?.Header as string;
        var pages = new List<TabItem>();
        foreach (string library in catalog.Libraries)
        {
            var page = new TabItem { Header = library };
            page.Content = LibraryPage(catalog.All.Where(c => c.Library == library).ToList(), selectName, out Func<LibraryCommand?> pick);
            page.Tag = pick;
            pages.Add(page);
        }
        if (catalog.Errors.Count > 0)
            pages.Add(new TabItem { Header = $"Fichiers refusés ({catalog.Errors.Count})", Content = ErrorsPage(catalog.Errors) });
        if (pages.Count == 0)
            pages.Add(new TabItem { Header = "Aucune", Content = Words("Aucune bibliothèque trouvée.") });
        tabs.ItemsSource = pages;
        tabs.SelectedItem = pages.FirstOrDefault(p => (p.Header as string) == current) ?? pages[0];

        folders.Text = $"Bibliothèques du projet (protégées) : {LibraryFolders.Project}\n" +
                       $"Vos bibliothèques : {LibraryFolders.User}" +
                       (Directory.Exists(LibraryFolders.User) ? "" : " (dossier pas encore créé)");
    }

    // Une bibliothèque : ses commandes à gauche, la commande choisie à droite
    Control LibraryPage(IReadOnlyList<LibraryCommand> commands, string? selectName, out Func<LibraryCommand?> current)
    {
        var details = new TextBlock { FontSize = TextSize, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16, 8, 8, 8) };
        var list = new ListBox
        {
            FontSize = TextSize,
            ItemsSource = commands.Select(c => $"{c.Name}  ({KindName(c)})").ToList()
        };
        ScrollViewer.SetVerticalScrollBarVisibility(list, Avalonia.Controls.Primitives.ScrollBarVisibility.Visible);
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedIndex >= 0)
                details.Text = Describe(commands[list.SelectedIndex]);
            RefreshSelected();
        };
        current = () => list.SelectedIndex >= 0 && list.SelectedIndex < commands.Count ? commands[list.SelectedIndex] : null;
        int first = Math.Max(0, commands.ToList().FindIndex(c => c.Name == selectName));
        list.SelectedIndex = commands.Count > 0 ? first : -1;
        if (commands.Count > 0)
            details.Text = Describe(commands[first]);

        var page = new Grid { ColumnDefinitions = new ColumnDefinitions("320,*"), Margin = new Thickness(0, 8, 0, 0) };
        page.Children.Add(list);
        var right = new ScrollViewer { Content = details };
        Grid.SetColumn(right, 1);
        page.Children.Add(right);
        return page;
    }

    static string KindName(LibraryCommand c) => c.Kind switch
    {
        LibraryKind.Command => "commande",
        LibraryKind.Function => "fonction",
        _ => "routine"
    };

    // Ce qu'on sait d'une commande : bonne écriture, à qui elle est, aide, valeurs
    static string Describe(LibraryCommand c)
    {
        var lines = new List<string>
        {
            LibraryCommandFile.Signature(c),
            "",
            c.Kind switch
            {
                LibraryKind.Command => "Commande du BASIC.",
                LibraryKind.Function => "Fonction du BASIC : elle donne un nombre entier.",
                _ => "Routine : elle ne sert qu'en assembleur (CALL " + c.Name + "), pas en BASIC."
            },
            c.Protected ? "Commande du projet : protégée (ni modifiée ni effacée)." : "Commande à vous : vous pouvez la modifier ou l'effacer.",
        };
        if (c.Author.Length > 0)
            lines.Add("Auteur : " + c.Author);
        if (c.Help.Length > 0)
        {
            lines.Add("");
            lines.AddRange(c.Help.Split('\n'));
        }
        if (c.Parameters.Count > 0)
        {
            lines.Add("");
            lines.Add(c.Parameters.Count == 1 ? "Sa valeur :" : $"Ses {c.Parameters.Count} valeurs :");
            foreach (LibraryParameter p in c.Parameters)
            {
                string skipped = p.IsOptional ? $", facultative (sautée : la routine reçoit {p.Default})" : "";
                string role = p.Description.Length > 0 ? " : " + p.Description : "";
                lines.Add($"   {p.Name.ToLowerInvariant()}, de {p.Min} à {p.Max}{skipped}{role}");
            }
        }
        return string.Join("\n", lines);
    }

    static Control ErrorsPage(IReadOnlyList<CatalogError> errors) =>
        new ScrollViewer { Content = Words(string.Join("\n\n", errors.Select(e => e.ToString()))) };

    static TextBlock Words(string text) =>
        new() { Text = text, FontSize = TextSize, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
}
