// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot K6 (D094) : HIMEM, FRE, UNT, WAIT, et les adresses à virgule (MEMORY HIMEM-1000).
/// Référence : vrai BASIC (SugarBox, 16 nov. 2026) ; examples\memoire.bas.
/// </summary>
public class MemoryInfoTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static (string Output, int CodeLength) Run(string source, Action<MiniZ80>? setUp = null)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        setUp?.Invoke(cpu);
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), program.Code.Length);
    }

    static int[] Numbers(string output) =>
        output.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
              .Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        // SugarBox : 42619 / -1, -32768, 100 en zones de 13 / 40959 après MEMORY &9FFF
        (string output, _) = Run("10 PRINT HIMEM\n20 PRINT UNT(&FFFF),UNT(&8000),UNT(100)\n50 MEMORY &9FFF\n60 PRINT HIMEM");

        Assert.Equal(" 42619 \r\n" + "-1 " + new string(' ', 10) + "-32768 " + new string(' ', 6) + " 100 \r\n" + " 40959 \r\n",
                     output);
    }

    [Fact]
    public void FRE_donne_la_place_entre_le_programme_et_HIMEM()
    {
        (string output, int length) = Run("10 PRINT FRE(0)");

        Assert.Equal($" {0xA67C - (0x4000 + length)} \r\n", output);
    }

    [Fact]
    public void FRE_avec_un_texte_fait_le_menage()
    {
        int[] values = Numbers(Run("10 A$=\"BON\"+\"JOUR\"\n20 A$=\"\"\n30 PRINT FRE(0)\n40 PRINT FRE(\"\")").Output);

        Assert.Equal(2, values.Length);
        Assert.True(values[1] > values[0], $"{values[0]} puis {values[1]}");
    }

    [Fact]
    public void MEMORY_HIMEM_moins_1000_et_PEEK_POKE_au_dessus_de_32767()
    {
        (string output, _) = Run("10 MEMORY HIMEM-1000\n20 PRINT HIMEM\n30 POKE HIMEM,77:PRINT PEEK(HIMEM)");

        Assert.Equal(" 41619 \r\n 77 \r\n", output);
    }

    [Fact]
    public void UNT_d_un_nombre_a_virgule()
    {
        Assert.Equal("-22917 -5 \r\n", Run("10 PRINT UNT(HIMEM);UNT(-5.0)").Output);
    }

    [Fact]
    public void Sans_HIMEM_ni_FRE_MEMORY_n_ajoute_toujours_rien()
    {
        GeneratedProgram with = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 MEMORY &9FFF").Program, 0x4000);
        GeneratedProgram without = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 REM").Program, 0x4000);

        Assert.Equal(without.Code.Length, with.Code.Length);
    }

    [Theory]
    [InlineData("10 WAIT &F500,1:PRINT \"OK\"", 0x01)]          // le bit 0 vaut 1 : on passe
    [InlineData("10 WAIT &F500,1,1:PRINT \"OK\"", 0x00)]        // inversé : le bit 0 à 0 suffit
    [InlineData("10 P=&F500:M=3:WAIT P,M:PRINT \"OK\"", 0x02)]  // valeurs calculées
    public void WAIT_attend_l_etat_du_port(string source, int portValue)
    {
        (string output, _) = Run(source, cpu => cpu.Ports[0xF500] = (byte)portValue);

        Assert.Equal("OK\r\n", output);
    }

    [Theory]
    [InlineData("10 WAIT &F500")]
    [InlineData("10 WAIT &F500,256")]
    [InlineData("10 WAIT &F500,1,2,3")]
    [InlineData("10 A=FRE")]
    [InlineData("10 A=UNT(1,2)")]
    [InlineData("10 A=UNT")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(Errors(source));
    }
}
