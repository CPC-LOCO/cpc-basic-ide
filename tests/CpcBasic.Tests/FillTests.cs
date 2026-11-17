// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot L4b (D107) : FILL (règle du vrai BASIC) et PAINT (commande nouvelle), notre propre remplissage. Les essais se font
/// dans une petite fenêtre graphique (0 à 20) en MODE 1 : un pixel = 2 unités ; D109 : lecture et écriture directes dans
/// la mémoire de l'écran (MiniZ80 la simule). Exemple : examples\remplir.bas.
/// </summary>
public class FillTests
{
    const string Window = "10 ORIGIN 0,0,0,20,20,0\n";

    static (string Output, MiniZ80 Cpu) Run(string source, byte mode = 1)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80, ScreenMode = mode };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    // Points d'encre « ink » dans le carré 0 à « size » (un pixel = 2 unités en MODE 1), lus dans la mémoire de l'écran
    static int Count(MiniZ80 cpu, int ink, int size)
    {
        int count = 0;
        for (int x = 0; x <= size; x += 2)
            for (int y = 0; y <= size; y += 2)
                if (cpu.GetPixel(x, y) == ink)
                    count++;
        return count;
    }

    [Fact]
    public void PAINT_remplit_toute_la_fenetre_vide_et_laisse_le_curseur()
    {
        (string output, MiniZ80 cpu) = Run(Window + "20 MOVE 10,10:PAINT 3:PRINT XPOS;YPOS");

        Assert.Equal(" 10  10 \r\n", output);
        Assert.Equal(11 * 11, Count(cpu, 3, 20));                          // x et y de 0 à 20, de 2 en 2
        Assert.Equal(11 * 11, Count(cpu, 3, 40));                          // rien en dehors de la fenêtre
    }

    [Theory]
    [InlineData(0, 4, 6)]       // MODE 0 : un pixel = 4 unités, 2 par octet
    [InlineData(2, 1, 21)]      // MODE 2 : un pixel = 1 unité, 8 par octet
    public void PAINT_dans_les_autres_modes(byte mode, int step, int columns)
    {
        (_, MiniZ80 cpu) = Run(Window + "20 MOVE 8,10:PAINT 1", mode);

        int count = 0;
        for (int x = 0; x <= 40; x += step)
            for (int y = 0; y <= 40; y += 2)
                if (cpu.GetPixel(x, y) == 1)
                    count++;
        Assert.Equal(columns * 11, count);
    }

    [Fact]
    public void PAINT_s_arrete_sur_toute_autre_encre()
    {
        (string output, MiniZ80 cpu) = Run(Window + "20 FOR Y=0 TO 20 STEP 2:PLOT 10,Y,2:NEXT\n" +
                                           "30 MOVE 4,4:PAINT 3:PRINT TEST(4,4);TEST(10,4);TEST(16,4)");

        Assert.Equal(" 3  2  0 \r\n", output);
        Assert.Equal(5 * 11, Count(cpu, 3, 20));                           // seulement à gauche du mur
    }

    [Fact]
    public void PAINT_ne_passe_pas_par_les_coins()
    {
        // Un trait en diagonale (un point par ligne) ferme la zone : on ne passe que par le haut, le bas, la gauche, la droite
        (string output, _) = Run(Window + "20 FOR I=0 TO 20 STEP 2:PLOT I,I,1:NEXT\n" +
                                 "30 MOVE 2,10:PAINT 3:PRINT TEST(2,10);TEST(10,2)");

        Assert.Equal(" 3  0 \r\n", output);
    }

    [Fact]
    public void PAINT_avec_la_meme_encre_ne_fait_rien()
    {
        (string output, _) = Run(Window + "20 PLOT 4,4,3:MOVE 4,4:PAINT 3:PRINT TEST(4,4);TEST(6,4)");

        Assert.Equal(" 3  0 \r\n", output);
    }

    [Fact]
    public void FILL_traverse_une_autre_encre_comme_le_vrai_BASIC()
    {
        // WinAPE (D106) : FILL ne s'arrête que sur l'encre du stylo graphique et sur l'encre du remplissage
        (string output, _) = Run(Window + "20 FOR Y=0 TO 20 STEP 2:PLOT 10,Y,2:NEXT\n" +
                                 "30 GRAPHICS PEN 1:MOVE 4,4:FILL 3:PRINT TEST(4,4);TEST(10,4);TEST(16,4)");

        Assert.Equal(" 3  3  3 \r\n", output);
    }

    [Fact]
    public void FILL_s_arrete_sur_le_stylo_graphique()
    {
        (string output, MiniZ80 cpu) = Run(Window + "20 FOR Y=0 TO 20 STEP 2:PLOT 10,Y,1:NEXT\n" +
                                           "30 MOVE 4,4:FILL 3:PRINT TEST(4,4);TEST(10,4);TEST(16,4)");

        Assert.Equal(" 3  1  0 \r\n", output);
        Assert.Equal(1, cpu.GraphicsPen);                                 // le stylo graphique est remis
    }

    [Theory]
    [InlineData("10 PAINT")]
    [InlineData("10 PAINT 16")]
    [InlineData("10 FILL 1,2")]
    [InlineData("10 PAINT=5")]                   // PAINT est un mot réservé (D107)
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
