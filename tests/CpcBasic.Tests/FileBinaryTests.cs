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
/// Lot J2 (D085) : CAT, SAVE nom,B,..., LOAD nom[,adresse], SPEED WRITE. Référence : vrai BASIC (SugarBox, 16 nov. 2026) ;
/// examples\binaire.bas.
/// </summary>
public class FileBinaryTests
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
        string program = string.Join("\n",
            "15 MEMORY &7FFF", "20 FOR I=0 TO 15:POKE &8000+I,I*16:NEXT", "30 SAVE \"DONNEES\",B,&8000,16",
            "40 FOR I=0 TO 15:POKE &8000+I,0:NEXT", "50 LOAD \"DONNEES\",&9000",
            "60 FOR I=0 TO 15:PRINT PEEK(&9000+I);:NEXT:PRINT", "70 LOAD \"DONNEES\"", "80 PRINT PEEK(&8005)", "90 CAT");

        (string output, MiniZ80 cpu) = Run(program);

        Assert.Equal(string.Concat(Enumerable.Range(0, 16).Select(i => $" {i * 16} ")) + "\r\n 80 \r\n", output);
        Assert.Equal(((byte)2, 0x8000), cpu.FileHeaders["DONNEES"]);
        Assert.Equal(1, cpu.Catalogs);
    }

    [Fact]
    public void Un_ecran_se_charge_sans_MEMORY()
    {
        Assert.Equal(" 7 \r\n", Run("10 POKE &C000,7:SAVE \"E\",B,&C000,1:POKE &C000,0:LOAD \"E\":PRINT PEEK(&C000)").Output);
    }

    [Theory]
    [InlineData("10 SAVE \"D\",B,&C000,16\n20 LOAD \"D\",&9000", "Memory full in 20\r\n")]
    [InlineData("10 OPENOUT \"T\":PRINT #9,\"X\":CLOSEOUT:LOAD \"T\",&C000", "File type error in 10\r\n")]
    [InlineData("10 SAVE \"B\",B,&C000,4:OPENIN \"B\"", "File type error in 10\r\n")]
    [InlineData("10 LOAD \"ABSENT\",&C000\n20 PRINT \"X\"", "")]
    [InlineData("10 OPENOUT \"T\":CLOSEOUT:OPENIN \"T\":CAT", "File already open in 10\r\n")]
    [InlineData("10 N=2:SPEED WRITE N", "Improper argument in 10\r\n")]
    public void Erreurs(string source, string expected)
    {
        Assert.Equal(expected, Run(source).Output);
    }

    [Fact]
    public void SPEED_WRITE_regle_la_cassette()
    {
        Assert.Equal(new (int, byte)[] { (333, 25), (167, 50) }, Run("10 SPEED WRITE 0:SPEED WRITE 1").Cpu.TapeSpeeds);
    }

    [Theory]
    [InlineData("10 SAVE \"X\"", DiagnosticCode.SaveProgram)]
    [InlineData("10 SAVE \"X\",A", DiagnosticCode.SaveProgram)]
    [InlineData("10 SAVE \"X\",B,&C000", DiagnosticCode.ExpectedExpression)]
    [InlineData("10 SPEED WRITE 2", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 LOAD 5", DiagnosticCode.TypeMismatch)]
    public void Ecritures_refusees(string source, DiagnosticCode code)
    {
        Assert.Contains(Errors(source), e => e.Code == code);
    }

    [Theory]
    [InlineData("10 SAVE \"ECRAN\",B,&C000,&4000")]
    [InlineData("10 SAVE \"JEU\",B,&8000,100,&8000")]
    [InlineData("10 N$=\"ECRAN\":LOAD N$,&C000:CAT")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }
}
