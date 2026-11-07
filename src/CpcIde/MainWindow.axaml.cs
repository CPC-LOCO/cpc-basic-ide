// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CpcIde;

public partial class MainWindow : Window
{
    const string SampleProgram = """
        10 REM Essai de coloration : mon premier jeu
        20 MODE 1 : INK 0,1 : BORDER 1
        30 PRINT "HELLO CPC !"
        40 FOR i = 1 TO 10
        50   CIRCLE 320, 200, i * 10, 2
        60 NEXT i
        70 SELECT CASE score
        80   CASE 1 TO 5 : BOOM
        90   CASE ELSE : VROOM
        100 END SELECT
        110 ' Un commentaire court
        120 POKE &C000, &X1010 : a$ = LEFT$("AMSTRAD", 3)
        """;

    public MainWindow()
    {
        InitializeComponent();

        Editor.LineNumbersForeground = new SolidColorBrush(Color.Parse("#8080FF"));
        Editor.TextArea.TextView.LineTransformers.Add(new BasicColorizer());
        Editor.Text = SampleProgram;

        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        UpdateStatus();
    }

    void UpdateStatus()
    {
        var caret = Editor.TextArea.Caret;
        StatusText.Text = $"Ligne {caret.Line}, colonne {caret.Column}   |   " +
                          "Copier / couper / coller : Ctrl+C / Ctrl+X / Ctrl+V";
    }
}
