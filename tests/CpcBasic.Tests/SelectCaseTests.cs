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

/// <summary>SELECT, CASE, CASE ELSE, END SELECT (D206).</summary>
public class SelectCaseTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static List<DiagnosticCode> Errors(string source) =>
        BasicAnalyzer.Analyze(source).Diagnostics.Select(d => d.Code).ToList();

    static string Run(string source)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Fact]
    public void Le_premier_CASE_qui_convient_est_execute()
    {
        string output = Run("""
            10 FOR N=0 TO 9
            20 SELECT N
            30 CASE 1
            40 PRINT "a";
            50 CASE 2,3
            60 PRINT "b";
            70 CASE 4 TO 6
            80 PRINT "c";
            90 CASE > 7
            100 PRINT "d";
            110 CASE ELSE
            120 PRINT "e";
            130 END SELECT
            140 NEXT
            """);

        Assert.Equal("eabbcccedd", output);                  // 0 à 9 : e a b b c c c e d d
    }

    [Fact]
    public void Sans_CASE_ELSE_rien_n_est_fait_si_aucun_cas_ne_convient()
    {
        Assert.Equal("[]", Run("10 PRINT \"[\";\n20 SELECT 5\n30 CASE 1:PRINT \"x\";\n40 END SELECT\n50 PRINT \"]\";"));
    }

    [Fact]
    public void Les_elements_se_melangent_et_les_comparaisons_marchent()
    {
        string output = Run("""
            10 FOR N=-2 TO 12 STEP 2
            20 SELECT N*1
            30 CASE <= 0, 5 TO 6, <> 10:PRINT "o";
            40 CASE ELSE:PRINT "n";
            50 END SELECT
            60 NEXT
            """);

        Assert.Equal("oooooono", output);                    // -2 0 2 4 6 8 : o ; 10 : n ; 12 : o
    }

    [Fact]
    public void SELECT_d_un_texte()
    {
        string output = Run("""
            10 FOR I=1 TO 4
            20 R$=MID$("OUINONPEUTOI",I*3-2,3)
            30 SELECT R$
            40 CASE "OUI":PRINT "1";
            50 CASE "NON","PEU":PRINT "2";
            60 CASE ELSE:PRINT "3";
            70 END SELECT
            80 NEXT
            """);

        Assert.Equal("1223", output);
    }

    [Fact]
    public void SELECT_dans_un_SELECT()
    {
        string output = Run("""
            10 FOR A=1 TO 2:FOR B=1 TO 2
            20 SELECT A
            30 CASE 1
            40 SELECT B
            50 CASE 1:PRINT "w";
            60 CASE 2:PRINT "x";
            70 END SELECT
            80 CASE 2
            90 SELECT B
            100 CASE 1:PRINT "y";
            110 CASE ELSE:PRINT "z";
            120 END SELECT
            130 END SELECT
            140 NEXT:NEXT
            """);

        Assert.Equal("wxyz", output);
    }

    [Fact]
    public void SELECT_CASE_est_accepte_et_SELECT_d_un_nombre_a_virgule()
    {
        string output = Run("""
            10 FOR I=1 TO 3:X!=I:X!=X!/2
            20 SELECT CASE X!
            30 CASE 0.5:PRINT "a";
            40 CASE 0.9 TO 1.1:PRINT "b";
            50 CASE > 1.2:PRINT "c";
            60 END SELECT
            70 NEXT
            """);

        Assert.Equal("abc", output);
    }

    [Theory]
    [InlineData("10 CASE 1", DiagnosticCode.CaseWithoutSelect)]
    [InlineData("10 CASE ELSE", DiagnosticCode.CaseWithoutSelect)]
    [InlineData("10 END SELECT", DiagnosticCode.EndSelectWithoutSelect)]
    [InlineData("10 SELECT 1\n20 CASE 1", DiagnosticCode.SelectWithoutEndSelect)]
    [InlineData("10 SELECT 1\n20 CASE ELSE\n30 CASE 2\n40 END SELECT", DiagnosticCode.CaseAfterCaseElse)]
    [InlineData("10 SELECT 1\n20 PRINT 2\n30 CASE 1\n40 END SELECT", DiagnosticCode.CaseExpected)]
    [InlineData("10 SELECT\n20 CASE 1\n30 END SELECT", DiagnosticCode.SelectSyntax)]
    [InlineData("10 SELECT 1\n20 CASE\n30 END SELECT", DiagnosticCode.SelectSyntax)]
    [InlineData("10 SELECT 1\n20 CASE 1 TO\n30 END SELECT", DiagnosticCode.SelectSyntax)]
    [InlineData("10 SELECT A$\n20 CASE 1\n30 END SELECT", DiagnosticCode.TypeMismatch)]
    [InlineData("10 SELECT 1\n20 CASE 1\n30 FOR I=1 TO 2\n40 CASE 2\n50 NEXT\n60 END SELECT", DiagnosticCode.BlocksNotNested)]
    public void Les_erreurs_de_SELECT(string source, DiagnosticCode expected)
    {
        Assert.Contains(expected, Errors(source));
    }
}
