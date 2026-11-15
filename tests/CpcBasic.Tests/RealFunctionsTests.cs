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
/// Lot F2 (D069) : fonctions mathématiques. Les affichages attendus sont ceux relevés sur le vrai BASIC
/// (capture de l'utilisateur dans SugarBox, 15 nov. 2026).
/// </summary>
public class RealFunctionsTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static string Run(string source)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Theory]
    [InlineData("SIN(1);COS(1);TAN(1);ATN(1)", " 0.841470985  0.540302306  1.55740772  0.785398163 ")]
    [InlineData("SQR(2);EXP(1);LOG(10);LOG10(1000);PI", " 1.41421356  2.71828183  2.30258509  3  3.14159265 ")]
    [InlineData("INT(2.7);INT(-2.7);FIX(2.7);FIX(-2.7);CINT(2.5);CINT(-2.5)", " 2 -3  2 -2  3 -3 ")]
    [InlineData("ROUND(3.14159,2);ROUND(1234.5,-2);ROUND(2.5);ROUND(-2.5)", " 3.14  1200  3 -3 ")]
    [InlineData("ABS(-2.5);SGN(-2.5);MIN(1.5,2);MAX(1.5,2,0.5)", " 2.5 -1  1.5  2 ")]
    [InlineData("INT(5);ROUND(1234,-2);CREAL(5)/2", " 5  1200  2.5 ")]
    [InlineData("SIN(0);SGN(0.0);ABS(3);MIN(3,2.5)", " 0  0  3  2.5 ")]
    public void Les_fonctions_donnent_les_valeurs_du_Locomotive(string values, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 PRINT " + values));
    }

    [Fact]
    public void DEG_et_RAD_changent_l_unite_des_angles()
    {
        Assert.Equal(" 0.5  0.5  45 \r\n 0.785398163 \r\n", Run("10 DEG:PRINT SIN(30);COS(60);ATN(1):RAD\n20 PRINT ATN(1)"));
    }

    [Fact]
    public void EXP_trop_grand_donne_Overflow_et_le_programme_continue()
    {
        Assert.Equal("Overflow\r\n 1.70141E+38 \r\nSUITE\r\n", Run("10 PRINT EXP(100)\n20 PRINT \"SUITE\""));
    }

    [Theory]
    [InlineData("SQR(-1)")]
    [InlineData("LOG(0)")]
    [InlineData("LOG10(-5)")]
    public void Une_valeur_impossible_donne_Improper_argument_et_arrete_le_programme(string call)
    {
        Assert.Equal("Improper argument in 10\r\n", Run($"10 PRINT {call}\n20 PRINT \"NON\""));
    }

    [Fact]
    public void SQR_de_zero_est_permis()
    {
        Assert.Equal(" 0 \r\n", Run("10 PRINT SQR(0)"));
    }

    [Fact]
    public void Un_entier_recoit_le_resultat_d_une_fonction()
    {
        Assert.Equal(" 2 -3  3 \r\n", Run("10 A% = INT(2.7) : B% = FIX(-3.2) : C% = CINT(SQR(9))\n20 PRINT A%;B%;C%"));
    }

    [Fact]
    public void CINT_trop_grand_arrete_le_programme()
    {
        Assert.Equal("Overflow in 10\r\n", Run("10 PRINT CINT(40000.5)"));
    }

    [Fact]
    public void RND_donne_un_nombre_de_0_a_1()
    {
        Assert.Equal("-1 \r\n", Run("10 FOR I = 1 TO 50 : X! = RND : IF X! < 0 OR X! >= 1 THEN PRINT \"NON\"\n20 NEXT\n30 PRINT X! >= 0"));
    }

    [Fact]
    public void RND_0_redonne_le_meme_nombre_et_RND_negatif_repart()
    {
        Assert.Equal("-1 \r\n", Run("10 A! = RND : B! = RND(0) : PRINT A! = B!"));
        Assert.Equal("-1 \r\n", Run("10 A! = RND(-3) : B! = RND(-3) : PRINT A! = B!"));
        Assert.Equal(" 0 \r\n", Run("10 A! = RND : B! = RND : PRINT A! = B!"));
    }
}
