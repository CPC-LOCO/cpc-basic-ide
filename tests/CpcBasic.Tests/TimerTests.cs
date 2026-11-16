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
/// Lot I1 (D079) : AFTER, EVERY, REMAIN, DI, EI. Le simulateur appelle la routine du compteur tous les 300 pas, comme
/// les « tops » de 1/50 s du CPC. Référence : examples\minuter.bas tapé dans le vrai BASIC (SugarBox, 16 nov. 2026).
/// </summary>
public class TimerTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static MiniZ80 Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    // ---------- Analyse ----------

    [Fact]
    public void AFTER_et_EVERY_sont_lus()
    {
        var statements = BasicAnalyzer.Analyze("10 AFTER 100, 2 GOSUB 200\n20 EVERY 25 GOSUB 200\n200 RETURN").Program.Statements;

        var after = Assert.IsType<TimerStatement>(statements[0]);
        Assert.False(after.Every);
        Assert.Equal(200, after.Target);
        Assert.Equal(2, Assert.IsType<NumberExpression>(after.Timer).Value);
        var every = Assert.IsType<TimerStatement>(statements[1]);
        Assert.True(every.Every);
        Assert.Equal(0, Assert.IsType<NumberExpression>(every.Timer).Value);   // minuterie 0 si elle est omise
    }

    [Fact]
    public void Minuterie_hors_de_0_a_3()
    {
        Assert.Contains(Errors("10 AFTER 50, 4 GOSUB 20\n20 RETURN"), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Fact]
    public void GOSUB_manquant()
    {
        Diagnostic error = Assert.Single(Errors("10 EVERY 50"));
        Assert.Equal(DiagnosticCode.ExpectedGoto, error.Code);
        Assert.Contains("EVERY attend GOSUB", error.Message);
    }

    [Fact]
    public void Ligne_qui_n_existe_pas()
    {
        Assert.Contains(Errors("10 AFTER 50 GOSUB 999"), e => e.Code == DiagnosticCode.LineNotFound);
    }

    [Theory]
    [InlineData("10 AFTER = 5")]
    [InlineData("10 EVERY = 5")]
    [InlineData("10 DI = 5")]
    public void Les_nouveaux_mots_ne_sont_pas_des_variables(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    // ---------- Exécution ----------

    [Fact]
    public void AFTER_appelle_une_seule_fois()
    {
        MiniZ80 cpu = Run("10 AFTER 1 GOSUB 100\n20 FOR K=1 TO 500:NEXT\n30 PRINT \"/\"\n40 END\n100 PRINT \"T\";:RETURN");

        Assert.Equal("T/\r\n", cpu.Output.ToString());
    }

    [Fact]
    public void EVERY_appelle_souvent_et_REMAIN_l_arrete()
    {
        MiniZ80 cpu = Run("10 EVERY 1,1 GOSUB 100\n20 FOR K=1 TO 500:NEXT\n30 R=REMAIN(1):A=C\n" +
                          "40 FOR K=1 TO 500:NEXT\n50 PRINT C>2;C=A\n60 END\n100 C=C+1:RETURN");

        Assert.Equal("-1 -1 \r\n", cpu.Output.ToString());   // vrai : -1
    }

    [Fact]
    public void DI_retient_les_appels_jusqu_a_EI()
    {
        MiniZ80 cpu = Run("10 AFTER 1 GOSUB 100\n20 DI\n30 FOR K=1 TO 300:NEXT\n40 PRINT \"X\";\n50 EI\n60 PRINT \"Y\"\n70 END\n" +
                          "100 PRINT \"T\";:RETURN");

        Assert.Equal("XTY\r\n", cpu.Output.ToString());
    }

    [Fact]
    public void La_minuterie_la_plus_haute_passe_d_abord()
    {
        MiniZ80 cpu = Run("10 DI\n20 AFTER 1,1 GOSUB 100\n30 AFTER 1,2 GOSUB 200\n40 FOR K=1 TO 300:NEXT\n50 EI\n60 PRINT\n70 END\n" +
                          "100 PRINT \"1\";:RETURN\n200 PRINT \"2\";:RETURN");

        Assert.Equal("21\r\n", cpu.Output.ToString());
    }

    [Fact]
    public void REMAIN_d_une_minuterie_arretee_vaut_0()
    {
        Assert.Equal(" 0 \r\n", Run("10 PRINT REMAIN(3)").Output.ToString());
    }

    [Fact]
    public void REMAIN_hors_limites_Improper_argument()
    {
        Assert.Contains("Improper argument in 20", Run("10 N = 4\n20 A = REMAIN(N)").Output.ToString());
    }

    [Fact]
    public void Sans_minuterie_rien_ne_change()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 FOR I=1 TO 3\n20 PRINT I\n30 NEXT").Program, 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.EventsName, program.Modules);
    }
}
