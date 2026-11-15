// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot F1b1 (D068) : nombres à virgule dans un programme. Les affichages attendus sont ceux relevés
/// sur le vrai BASIC (deux captures de l'utilisateur, 15 nov. 2026).
/// </summary>
public class RealProgramTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source, byte version = 2, TargetMachine target = TargetMachine.AllCpc)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000, new CompilerOptions(target));
        var cpu = new MiniZ80 { FirmwareVersion = version };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData(".5", 0.5)]
    [InlineData("1E5", 1e5)]
    [InlineData("2.5E-3", 0.0025)]
    [InlineData("1e+3", 1000.0)]
    [InlineData("40000", 40000.0)]
    public void Un_nombre_a_virgule_est_lu(string written, double value)
    {
        var print = Assert.IsType<PrintValueStatement>(Parse("10 PRINT " + written).Statements[0]);
        Assert.Equal(new RealNumberExpression(value), print.Value);
    }

    [Fact]
    public void Un_nombre_negatif_trop_grand_pour_un_entier_est_a_virgule()
    {
        var print = Assert.IsType<PrintValueStatement>(Parse("10 PRINT -40000").Statements[0]);
        Assert.Equal(new RealNumberExpression(-40000), print.Value);
    }

    [Fact]
    public void Un_entier_recoit_un_nombre_a_virgule_arrondi_des_la_compilation()
    {
        var assign = Assert.IsType<AssignStatement>(Parse("10 A% = 2.5").Statements[0]);
        Assert.Equal(new NumberExpression(3), assign.Value);
        assign = Assert.IsType<AssignStatement>(Parse("10 A% = -2.5").Statements[0]);
        Assert.Equal(new NumberExpression(-3), assign.Value);
    }

    [Fact]
    public void Un_nombre_trop_grand_est_refuse()
    {
        Assert.Contains(Errors("10 PRINT 1E39"), e => e.Code == DiagnosticCode.NumberOutOfRange);
        Assert.Contains(Errors("10 BOX 40000, 0, 1, 1"), e => e.Code == DiagnosticCode.NumberOutOfRange);
    }

    [Fact]
    public void Le_calcul_reste_entier_sans_nombre_a_virgule()
    {
        Assert.False(Types.IsReal(Parse("10 PRINT 7 / 2").Statements.OfType<PrintValueStatement>().First().Value));
        Assert.True(Types.IsReal(Parse("10 PRINT 7 / 2.0").Statements.OfType<PrintValueStatement>().First().Value));
        Assert.True(Types.IsReal(Parse("10 PRINT A! + 1").Statements.OfType<PrintValueStatement>().First().Value));
        Assert.False(Types.IsReal(Parse("10 PRINT 2.5 AND 1").Statements.OfType<PrintValueStatement>().First().Value));
    }

    // ---------- Affichage (règle relevée sur le vrai BASIC) ----------

    [Theory]
    [InlineData("1/3.0", " 0.333333333 ")]
    [InlineData("2/3.0", " 0.666666667 ")]
    [InlineData("10/3.0", " 3.33333333 ")]
    [InlineData("100/3.0", " 33.3333333 ")]
    [InlineData("1000000/3.0", " 333333.333 ")]
    [InlineData("1E8/3", " 33333333.3 ")]
    [InlineData("1/7.0", " 0.142857143 ")]
    [InlineData("0.5", " 0.5 ")]
    [InlineData("1E9", " 1E+09 ")]
    [InlineData("123456789", " 123456789 ")]
    [InlineData("1234567890", " 1.23457E+09 ")]
    [InlineData("0.1", " 0.1 ")]
    [InlineData("0.0001", " 0.0001 ")]
    [InlineData("-1.5", "-1.5 ")]
    [InlineData("1E-10", " 1E-10 ")]
    [InlineData("1E20", " 1E+20 ")]
    [InlineData("-2.5E-5", "-0.000025 ")]
    [InlineData("3.14159265358979", " 3.14159265 ")]
    [InlineData("2^0.5", " 1.41421356 ")]
    [InlineData("1E-9", " 0.000000001 ")]
    [InlineData("1.5E-6", " 0.0000015 ")]
    [InlineData("1.234E-7", " 1.234E-07 ")]
    [InlineData("0.000123456789", " 1.23457E-04 ")]
    [InlineData("999999999.6", " 1E+09 ")]
    [InlineData("123456789.4", " 123456789 ")]
    [InlineData("12345678.95", " 12345679 ")]
    [InlineData("1E10/3", " 3.33333E+09 ")]
    [InlineData("-1E15/7", "-1.42857E+14 ")]
    [InlineData("2^40.0", " 1.09951E+12 ")]
    [InlineData("1E6", " 1000000 ")]
    [InlineData("-0.000001", "-0.000001 ")]
    [InlineData("0.0", " 0 ")]
    [InlineData("-0.0", " 0 ")]
    public void PRINT_affiche_comme_le_Locomotive(string expression, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 PRINT " + expression));
    }

    [Fact]
    public void Overflow_affiche_un_message_et_le_programme_continue()
    {
        Assert.Equal("Overflow\r\n 1.70141E+38 SUITE\r\n", Run("10 PRINT 1E38*10;\"SUITE\""));
        Assert.Equal("Overflow\r\n-1.70141E+38 \r\n", Run("10 PRINT -1E38*10"));
    }

    [Fact]
    public void Division_by_zero_affiche_un_message_et_le_programme_continue()
    {
        Assert.Equal("Division by zero\r\n 1.70141E+38 SUITE\r\nFIN\r\n",
                     Run("10 PRINT 1/0.0;\"SUITE\"\n20 PRINT \"FIN\""));
    }

    // ---------- Variables et calculs ----------

    [Fact]
    public void Une_variable_a_virgule_garde_sa_valeur()
    {
        Assert.Equal(" 2.5 \r\n 3.75 \r\n", Run("10 B! = 2.5\n20 PRINT B!\n30 C! = B! * 1.5\n40 PRINT C!"));
    }

    [Fact]
    public void Entiers_et_nombres_a_virgule_se_melangent()
    {
        Assert.Equal(" 3 \r\n 0.5 \r\n-2 \r\n", Run("10 A = 2\n20 PRINT A * 1.5\n30 PRINT A - 1.5\n40 PRINT -A! - A"));
    }

    [Fact]
    public void Soustraction_et_signe_moins()
    {
        Assert.Equal(" 1.25 \r\n-1.25 \r\n-2.5 \r\n", Run("10 PRINT 3.5 - 2.25\n20 PRINT 2.25 - 3.5\n30 B! = 2.5\n40 PRINT -B!"));
    }

    [Theory]
    [InlineData("2.5", " 3 ")]
    [InlineData("-2.5", "-3 ")]
    [InlineData("3.5", " 4 ")]
    [InlineData("2.4", " 2 ")]
    [InlineData("32767.4", " 32767 ")]
    [InlineData("-32768", "-32768 ")]
    public void Un_entier_recoit_un_nombre_a_virgule_arrondi(string value, string shown)
    {
        Assert.Equal(shown + "\r\n", Run($"10 B! = {value}\n20 A% = B!\n30 PRINT A%"));
    }

    [Fact]
    public void Un_nombre_a_virgule_trop_grand_pour_un_entier_arrete_le_programme()
    {
        Assert.Equal("Overflow in 20\r\n", Run("10 B! = 40000\n20 A% = B!\n30 PRINT \"NON\""));
    }

    [Theory]
    [InlineData("0.5 < 1", true)]
    [InlineData("1 < 0.5", false)]
    [InlineData("2.5 = 2.5", true)]
    [InlineData("2.5 <> 2.5", false)]
    [InlineData("3 > 2.5", true)]
    [InlineData("-1.5 >= -1.5", true)]
    [InlineData("-1.5 <= -2", false)]
    public void Les_comparaisons_marchent_avec_les_nombres_a_virgule(string condition, bool expected)
    {
        Assert.Equal(expected ? "OUI\r\n" : "NON\r\n", Run($"10 IF {condition} THEN PRINT \"OUI\" ELSE PRINT \"NON\""));
        Assert.Equal(expected ? "-1 \r\n" : " 0 \r\n", Run($"10 PRINT ({condition})"));
    }

    [Fact]
    public void Un_calcul_entier_dans_un_calcul_a_virgule_ne_deborde_pas_sur_les_autres()
    {
        // (A! < 1.5 + A) est calculé au milieu du calcul à virgule : il ne doit pas écraser B! déjà rangé
        Assert.Equal(" 1.5 \r\n", Run("10 A = 2\n20 B! = 0.5\n30 PRINT B! - (A! < 1.5 + A) * 1"));
    }

    // ---------- Machines ----------

    [Theory]
    [InlineData(0, TargetMachine.AllCpc)]
    [InlineData(1, TargetMachine.AllCpc)]
    [InlineData(2, TargetMachine.AllCpc)]
    [InlineData(0, TargetMachine.Cpc464)]
    [InlineData(2, TargetMachine.Cpc664And6128)]
    public void Le_programme_marche_sur_chaque_machine(byte version, TargetMachine target)
    {
        Assert.Equal(" 0.333333333 \r\n", Run("10 PRINT 1/3.0", version, target));
    }

    [Fact]
    public void Sans_nombre_a_virgule_les_modules_de_calcul_ne_sont_pas_ajoutes()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 PRINT 7/2"), 0x4000);
        Assert.DoesNotContain(RuntimeLibrary.RealOpsName, program.Modules);
        Assert.DoesNotContain(RuntimeLibrary.MathsAllName, program.Modules);
    }
}
