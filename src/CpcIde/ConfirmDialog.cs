// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace CpcIde;

/// <summary>
/// Question « Oui / Non » avant une action qui efface (D118). Par prudence, le clavier est sur « Non » :
/// Entrée ou Échap ou N = Non ; O = Oui. Fermer avec la croix = Non. Gros boutons et texte de 18 points.
/// </summary>
public sealed class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string question, string yesText, string noText)
    {
        Title = title;
        Width = 620;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var message = new TextBlock
        {
            Text = question,
            FontSize = 18,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 16)
        };

        Button yes = MakeButton(yesText + " (O)", true);
        Button no = MakeButton(noText + " (N)", false);
        no.IsDefault = true;
        no.IsCancel = true;

        var buttons = new WrapPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(yes);
        buttons.Children.Add(no);

        var panel = new StackPanel { Margin = new Avalonia.Thickness(24) };
        panel.Children.Add(message);
        panel.Children.Add(buttons);
        Content = panel;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.O)
            {
                e.Handled = true;
                Close(true);
            }
            else if (e.Key == Key.N)
            {
                e.Handled = true;
                Close(false);
            }
        };

        Opened += (_, _) => no.Focus();
    }

    Button MakeButton(string text, bool result)
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
        button.Click += (_, _) => Close(result);
        return button;
    }
}
