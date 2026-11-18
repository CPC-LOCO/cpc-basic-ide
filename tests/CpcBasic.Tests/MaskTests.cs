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
/// Lot L4c (D110) : MASK et GRAPHICS PEN stylo, mode. Référence : vrai BASIC 1.1 dans WinAPE (essais\refl5.dsk,
/// 17 nov. 2026) : le programme lit chaque point des lignes avec TEST (1 trait, 0 fond opaque, 2 fond laissé).
/// Exemple : examples\masque.bas.
/// </summary>
public class MaskTests
{
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

    // Le programme de la disquette de référence (essais\refl5.bas)
    const string Reference =
        "10 MODE 1:GRAPHICS PAPER 2:CLG:GRAPHICS PAPER 0:L=1\n" +
        "20 MASK &X11110000:MOVE 0,100:DRAW 62,100:Y=100:GOSUB 200\n" +
        "30 MASK &X10101010:MOVE 0,110:DRAW 62,110:Y=110:GOSUB 200\n" +
        "40 GRAPHICS PEN 1,1:MASK &X11001100:MOVE 0,120:DRAW 62,120:Y=120:GOSUB 200\n" +
        "50 GRAPHICS PEN 1,0:MOVE 0,130:DRAW 62,130:Y=130:GOSUB 200\n" +
        "60 MASK &X11110000,0:MOVE 0,140:DRAW 10,140:DRAW 20,140:DRAW 62,140:Y=140:GOSUB 200\n" +
        "70 MASK &X11110000,1:MOVE 0,150:DRAW 10,150:DRAW 20,150:DRAW 62,150:Y=150:GOSUB 200\n" +
        "80 MASK &X11100000:MOVE 100,100:DRAW 100,162:S$=\"\":FOR Y=100 TO 162 STEP 2:S$=S$+CHR$(48+TEST(100,Y)):NEXT:" +
        "LOCATE 1,L:PRINT S$:L=L+1\n" +
        "90 MASK &X11100000:PLOT 200,100:PRINT \"PLOT :\";TEST(200,100)\n" +
        "100 END\n" +
        "200 S$=\"\":FOR X=0 TO 62 STEP 2:S$=S$+CHR$(48+TEST(X,Y)):NEXT:LOCATE 1,L:PRINT S$:L=L+1:RETURN";

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        Assert.Equal(
            "11110000111100001111000011110000\r\n" +      // motif, fond opaque (GRAPHICS PAPER 0)
            "10101010101010101010101010101010\r\n" +
            "11221122112211221122112211221122\r\n" +      // GRAPHICS PEN 1,1 : fond transparent
            "11001100110011001100110011001100\r\n" +      // GRAPHICS PEN 1,0 : de nouveau opaque
            "21111000011110000111100001111000\r\n" +      // MASK …,0 : premier point de chaque ligne sauté
            "11110001110000111100001111000011\r\n" +      // MASK …,1 : le point commun est retracé
            "11100000111000001110000011100000\r\n" +      // ligne verticale
            "PLOT : 1 \r\n",
            Run(Reference).Output);
    }

    [Theory]
    // D123 : « MASK ,n » (accepté par le vrai BASIC 1.1, SugarBox, 18 nov. 2026) change le premier point, pas le motif :
    // mêmes points que les lignes 60 et 70 de la référence
    [InlineData("MASK &X11110000,1:MASK ,0", "21111000011110000111100001111000")]
    [InlineData("MASK &X11110000,0:MASK ,1", "11110001110000111100001111000011")]
    public void MASK_virgule_ne_change_que_le_premier_point(string masks, string expected)
    {
        string source =
            "10 MODE 1:GRAPHICS PAPER 2:CLG:GRAPHICS PAPER 0:L=1\n" +
            "20 " + masks + ":MOVE 0,140:DRAW 10,140:DRAW 20,140:DRAW 62,140:Y=140:GOSUB 200\n" +
            "30 END\n" +
            "200 S$=\"\":FOR X=0 TO 62 STEP 2:S$=S$+CHR$(48+TEST(X,Y)):NEXT:LOCATE 1,L:PRINT S$:L=L+1:RETURN";

        Assert.Equal(expected + "\r\n", Run(source).Output);
    }

    [Fact]
    public void Sans_MASK_les_lignes_ne_passent_pas_par_le_detournement()
    {
        MiniZ80 cpu = Run("10 MOVE 0,0:DRAW 100,100").Cpu;

        Assert.Equal(new byte[] { 0xC3, 0xFC, 0xBD }, cpu.Memory[0xBDE8..0xBDEB]);   // SCR WRITE n'est pas touché
    }

    [Theory]
    [InlineData("10 MASK 256")]
    [InlineData("10 MASK 1,2")]
    [InlineData("10 MASK")]
    [InlineData("10 MASK ,")]
    [InlineData("10 MASK ,2")]
    [InlineData("10 MASK ,1,0")]
    [InlineData("10 GRAPHICS PEN 1,2")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
