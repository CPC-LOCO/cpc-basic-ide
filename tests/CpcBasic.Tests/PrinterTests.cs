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
/// Lot K12 (D100) : imprimante, PRINT #8, WRITE #8, WIDTH, POS(#8). Référence : vrai BASIC dans WinAPE, imprimante
/// envoyée dans un fichier (essais\imprimante.txt, 17 nov. 2026). Exemple : examples\imprime.bas.
/// </summary>
public class PrinterTests
{
    static (string Screen, string Printed) Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu.Printed.ToString());
    }

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        (string screen, string printed) = Run(
            "10 PRINT #8,\"BONJOUR\";\n20 PRINT #8,\"A\";TAB(10);\"B\"\n30 WIDTH 20\n40 PRINT #8,\"0123456789ABCDEFGHIJKLMNOP\"\n" +
            "50 PRINT #8,POS(#8)\n60 WRITE #8,\"X\",5\n70 WIDTH 255:PRINT #8,STRING$(30,\"-\")");

        Assert.Equal("BONJOURA B\r\n0123456789ABCDEFGHIJ\r\nKLMNOP\r\n 1 \r\n\"X\",5\r\n" + new string('-', 30) + "\r\n", printed);
        Assert.Equal("", screen);
    }

    [Fact]
    public void L_ecran_reste_l_ecran()
    {
        (string screen, string printed) = Run("10 PRINT \"A\":PRINT #8,\"B\":PRINT \"C\";POS(#8)");

        Assert.Equal("A\r\nC 1 \r\n", screen);
        Assert.Equal("B\r\n", printed);
    }

    [Fact]
    public void Zones_et_nombres()
    {
        Assert.Equal("1" + new string(' ', 12) + " 2 \r\n", Run("10 PRINT #8,\"1\",2").Printed);
    }

    [Theory]
    [InlineData("10 WIDTH 0")]
    [InlineData("10 WIDTH 256")]
    [InlineData("10 A=VPOS(#8)")]
    [InlineData("10 LINE INPUT #8,A$")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
