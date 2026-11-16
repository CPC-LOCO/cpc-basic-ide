// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot K4 (D091) : XPOS, YPOS, TEST, TESTR, SPEED INK, SPEED KEY. Référence : vrai BASIC (SugarBox, 16 nov. 2026) ;
/// examples\points.bas.
/// </summary>
public class GraphicsQueryTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

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
    public void Comme_le_vrai_BASIC()
    {
        // TEST déplace le curseur graphique : TESTR(70,10) part de (100,50)
        (string output, _) = Run("10 PLOT 100,50,2:MOVE 30,40\n20 PRINT XPOS;YPOS\n30 PRINT TEST(100,50);XPOS;YPOS\n" +
                                 "40 PRINT TESTR(70,10);TEST(0,0)");

        Assert.Equal(" 30  40 \r\n 2  100  50 \r\n 0  0 \r\n", output);
    }

    [Fact]
    public void TESTR_trouve_un_point_relatif()
    {
        Assert.Equal(" 3 \r\n", Run("10 PLOT 10,10,3:MOVE 0,0:PRINT TESTR(10,10)").Output);
    }

    [Fact]
    public void SPEED_INK_et_SPEED_KEY()
    {
        (_, MiniZ80 cpu) = Run("10 SPEED INK 5,10:A=2:SPEED KEY 30,A");

        Assert.Equal(new[] { ("INK", (byte)5, (byte)10), ("KEY", (byte)30, (byte)2) }, cpu.Speeds);
    }

    [Theory]
    [InlineData("10 SPEED INK 0,5")]
    [InlineData("10 SPEED KEY 5")]
    [InlineData("10 SPEED FAST 5,5")]
    [InlineData("10 A=TEST(1)")]
    [InlineData("10 SWAP A,B")]                  // n'existe pas dans le Locomotive BASIC (SugarBox : Syntax error)
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(Errors(source));
    }
}
