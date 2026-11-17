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
/// dans une petite fenêtre graphique (0 à 20) en MODE 1 : un pixel = 2 unités. Exemple : examples\remplir.bas.
/// </summary>
public class FillTests
{
    const string Window = "10 ORIGIN 0,0,0,20,20,0\n";

    static (string Output, MiniZ80 Cpu) Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    [Fact]
    public void PAINT_remplit_toute_la_fenetre_vide_et_laisse_le_curseur()
    {
        (string output, MiniZ80 cpu) = Run(Window + "20 MOVE 10,10:PAINT 3:PRINT XPOS;YPOS");

        Assert.Equal(" 10  10 \r\n", output);
        Assert.Equal(11 * 11, cpu.Points.Count(p => p.Value == 3));     // x et y de 0 à 20, de 2 en 2
        Assert.DoesNotContain(cpu.Points, p => p.Key.Item1 > 20 || p.Key.Item2 > 20);
    }

    [Fact]
    public void PAINT_s_arrete_sur_toute_autre_encre()
    {
        (string output, MiniZ80 cpu) = Run(Window + "20 FOR Y=0 TO 20 STEP 2:PLOT 10,Y,2:NEXT\n" +
                                           "30 MOVE 4,4:PAINT 3:PRINT TEST(4,4);TEST(10,4);TEST(16,4)");

        Assert.Equal(" 3  2  0 \r\n", output);
        Assert.Equal(5 * 11, cpu.Points.Count(p => p.Value == 3));      // seulement à gauche du mur
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
