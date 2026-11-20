// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D167 : la zone du programmeur, &amp;0400 à &amp;2FFF, est à lui pour toujours : le compilateur n'y met jamais rien.
/// &amp;3000 à &amp;3FFF : libre aussi, sauf avec les fichiers (OPENIN, OPENOUT) ou RUN"nom".
/// Si un de ces tests échoue un jour, c'est le compilateur qu'il faut changer, pas le test.
/// </summary>
public class ProgrammerZoneTests
{
    const int ZoneStart = 0x0400;
    const int ZoneEnd = 0x2FFF;

    [Fact]
    public void Le_programme_et_ses_zones_restent_au_dessus_de_la_zone()
    {
        Assert.True(Compiler.LoadAddress >= 0x4000);
        Assert.True(RuntimeLibrary.FileInBuffer > ZoneEnd);
        Assert.True(RuntimeLibrary.FileOutBuffer > ZoneEnd);
        Assert.True(RuntimeLibrary.RunStub > ZoneEnd);
    }

    [Theory]
    [InlineData("10 A$ = \"BON\"\n20 B$ = A$ + \"JOUR\"\n30 PRINT B$", "BONJOUR\r\n")]
    [InlineData("10 DIM T(20)\n20 FOR I = 0 TO 20\n30 T(I) = I * 2\n40 NEXT\n50 PRINT T(20)", " 40 \r\n")]
    [InlineData("10 GOSUB 100\n20 PRINT \"FIN\"\n30 END\n100 PRINT \"SOUS\"\n110 RETURN", "SOUS\r\nFIN\r\n")]
    public void Un_programme_qui_tourne_ne_touche_pas_la_zone(string source, string expected)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Compiler.LoadAddress);
        var cpu = new MiniZ80();
        for (int a = ZoneStart; a <= ZoneEnd; a++)
            cpu.Memory[a] = (byte)(a * 7 + 3);          // un motif connu
        cpu.Load(program.Code, Compiler.LoadAddress);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(Compiler.LoadAddress);

        Assert.Equal(expected, cpu.Output.ToString());
        for (int a = ZoneStart; a <= ZoneEnd; a++)
            Assert.True(cpu.Memory[a] == (byte)(a * 7 + 3), $"la zone du programmeur a changé en &{a:X4}");
    }
}
