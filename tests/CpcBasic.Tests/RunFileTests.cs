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

/// <summary>Lot J3 (D086) : RUN "nom" lance un autre programme compilé ; CHAIN et MERGE sont refusés. examples\enchaine.bas.</summary>
public class RunFileTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static byte[] Compile(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return CodeGenerator.Generate(analysis.Program, 0x4000).Code;
    }

    static (string Output, MiniZ80 Cpu) Run(string source, Dictionary<string, byte[]>? disk = null)
    {
        var cpu = new MiniZ80 { WindowWidth = 80 };
        foreach ((string name, byte[] code) in disk ?? new())
        {
            cpu.Files[name] = code.ToList();
            cpu.FileHeaders[name] = (2, 0x4000);
        }
        cpu.Load(Compile(source), 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    [Fact]
    public void RUN_lance_l_autre_programme()
    {
        var disk = new Dictionary<string, byte[]> { ["PARTIE2.BIN"] = Compile("10 PRINT \"PARTIE 2\"") };

        Assert.Equal("PARTIE 1\r\nPARTIE 2\r\n", Run("10 PRINT \"PARTIE 1\"\n20 RUN \"PARTIE2\"\n30 PRINT \"JAMAIS\"", disk).Output);
    }

    [Fact]
    public void RUN_avec_un_point_garde_le_nom()
    {
        var disk = new Dictionary<string, byte[]> { ["JEU.BIN"] = Compile("10 PRINT \"OK\"") };

        Assert.Equal("OK\r\n", Run("10 N$=\"JEU.BIN\":RUN N$", disk).Output);
    }

    [Fact]
    public void RUN_arrete_les_minuteries_et_les_sons()
    {
        var disk = new Dictionary<string, byte[]> { ["B.BIN"] = Compile("10 PRINT \"B\"") };

        (string output, MiniZ80 cpu) = Run("10 EVERY 50 GOSUB 100\n20 RUN \"B\"\n100 RETURN", disk);

        Assert.Equal("B\r\n", output);
        Assert.Equal(1, cpu.SoundResets);
    }

    [Theory]
    [InlineData("10 RUN \"ABSENT\"\n20 PRINT 1", "")]
    [InlineData("10 OPENOUT \"T.TXT\":PRINT #9,1:CLOSEOUT:RUN \"T.TXT\"", "File type error in 10\r\n")]
    [InlineData("10 ON ERROR GOTO 100\n20 RUN \"ABSENT\"\n30 END\n100 PRINT ERR", " 32 \r\n")]
    public void Erreurs(string source, string expected)
    {
        Assert.Equal(expected, Run(source).Output);
    }

    [Theory]
    [InlineData("10 CHAIN \"X\"")]
    [InlineData("10 CHAIN MERGE \"X\"")]
    [InlineData("10 MERGE \"X\"")]
    public void Refuses_a_la_compilation(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.NotForCompiled);
    }

    [Fact]
    public void RUN_attend_un_texte()
    {
        Assert.NotEmpty(Errors("10 RUN 5+A"));                 // RUN 5 : ligne 5 (D090), puis « +A » de trop
        Assert.Contains(Errors("10 RUN A"), e => e.Code == DiagnosticCode.TypeMismatch);
    }
}
