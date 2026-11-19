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
    LibraryCatalog? shown;

    /// <param name="size">Taille du texte (réglage de l'éditeur) ; <paramref name="remember"/> garde la nouvelle taille.</param>
    public LibraryWindow(int size, Action<int> remember)
    {
        TextSize = size;
        this.remember = remember;
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
        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 12, 0, 0) };
        bottom.Children.Add(folders);
        Grid.SetColumn(close, 1);
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
                tabs.FontSize = close.FontSize = next;
                folders.FontSize = next - 2;
                if (shown is not null)
                    Fill(shown);
            }
        };
    }

    readonly TextBlock folders;

    /// <summary>Remplit la fenêtre avec le catalogue (appelé à l'ouverture et chaque fois qu'on la redemande).</summary>
    public void Fill(LibraryCatalog catalog)
    {
        shown = catalog;
        string? current = (tabs.SelectedItem as TabItem)?.Header as string;
        var pages = new List<TabItem>();
        foreach (string library in catalog.Libraries)
            pages.Add(new TabItem { Header = library, Content = LibraryPage(catalog.All.Where(c => c.Library == library).ToList()) });
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
    static Control LibraryPage(IReadOnlyList<LibraryCommand> commands)
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
        };
        list.SelectedIndex = commands.Count > 0 ? 0 : -1;
        if (commands.Count > 0)
            details.Text = Describe(commands[0]);

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
