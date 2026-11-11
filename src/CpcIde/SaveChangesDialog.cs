// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace CpcIde;

/// <summary>
/// Boîte de dialogue « Le programme a été modifié. Que faire ? » : Enregistrer, Ne pas enregistrer, Annuler.
/// Entrée = Enregistrer, N = Ne pas enregistrer, Échap = Annuler. Gros boutons et texte de 18 points.
/// Fermer la boîte avec la croix revient à Annuler : dans le doute, rien n'est perdu.
/// </summary>
public sealed class SaveChangesDialog : Window
{
    /// <summary>Cancel est la première valeur : c'est elle que l'on obtient si la boîte est fermée sans choix.</summary>
    public enum Choice { Cancel, Save, Discard }

    public SaveChangesDialog(string fileName)
    {
        Title = "Modifications non enregistrées";
        Width = 620;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var message = new TextBlock
        {
            Text = $"Le programme « {fileName} » a été modifié et n'est pas enregistré.\n\n" +
                   "Voulez-vous l'enregistrer avant de continuer ?",
            FontSize = 18,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 16)
        };

        Button save = MakeButton("Enregistrer (Entrée)", Choice.Save);
        save.IsDefault = true;   // la touche Entrée
        Button discard = MakeButton("Ne pas enregistrer (N)", Choice.Discard);
        Button cancel = MakeButton("Annuler (Échap)", Choice.Cancel);
        cancel.IsCancel = true;  // la touche Échap

        var buttons = new WrapPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(save);
        buttons.Children.Add(discard);
        buttons.Children.Add(cancel);

        var panel = new StackPanel { Margin = new Avalonia.Thickness(24) };
        panel.Children.Add(message);
        panel.Children.Add(buttons);
        Content = panel;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.N)
            {
                e.Handled = true;
                Close(Choice.Discard);
            }
        };

        // Le clavier est tout de suite sur « Enregistrer »
        Opened += (_, _) => save.Focus();
    }

    Button MakeButton(string text, Choice choice)
    {
        var button = new Button
        {
            Content = text,
            FontSize = 18,
            MinHeight = 48,
            Padding = new Avalonia.Thickness(16, 8),
            Margin = new Avalonia.Thickness(0, 0, 8, 8),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) => Close(choice);
        return button;
    }
}
