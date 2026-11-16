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

/// <summary>Lot K5 (D092) : KEY n,texte et KEY DEF. examples\touches.bas.</summary>
public class KeyDefinitionTests
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
    public void KEY_range_le_texte_de_la_touche()
    {
        MiniZ80 cpu = Run("10 KEY 128,\"BONJOUR\":KEY 11,\"RUN\"+CHR$(13)").Cpu;

        Assert.Equal("BONJOUR", cpu.Expansions[128]);
        Assert.Equal("RUN\r", cpu.Expansions[139]);            // 11 et 139 : la même touche
    }

    [Fact]
    public void KEY_DEF_appelle_les_quatre_routines()
    {
        MiniZ80 cpu = Run("10 KEY DEF 46,1,63,64,65").Cpu;

        Assert.Equal(new[] { (0xBB39, (byte)46, (byte)0xFF), (0xBB27, (byte)46, (byte)63), (0xBB2D, (byte)46, (byte)64),
                             (0xBB33, (byte)46, (byte)65) }, cpu.KeyCalls);
    }

    [Fact]
    public void KEY_DEF_sans_repetition()
    {
        Assert.Equal(new[] { (0xBB39, (byte)66, (byte)0) }, Run("10 KEY DEF 66,0").Cpu.KeyCalls);
    }

    [Theory]
    [InlineData("10 N=32:KEY N,\"X\"", "Improper argument in 10\r\n")]
    [InlineData("10 N=80:KEY DEF N,1", "Improper argument in 10\r\n")]
    public void Valeurs_calculees_hors_limites(string source, string expected)
    {
        Assert.Equal(expected, Run(source).Output);
    }

    [Theory]
    [InlineData("10 KEY 32,\"X\"")]
    [InlineData("10 KEY DEF 80,1")]
    [InlineData("10 KEY DEF 46,2")]
    [InlineData("10 KEY DEF 46")]
    [InlineData("10 KEY 128,5")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(Errors(source));
    }
}
