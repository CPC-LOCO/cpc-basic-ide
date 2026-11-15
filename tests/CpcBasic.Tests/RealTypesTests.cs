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
/// Lot F1b2 (D068) : DEFINT / DEFREAL / DEFSTR, réglage « Sans signe : à virgule » (F8), FOR X!, INPUT A! et @A!.
/// </summary>
public class RealTypesTests
{
    static BasicProgram Parse(string source, bool realByDefault = false)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, new CompilerOptions(RealByDefault: realByDefault));
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source, bool realByDefault = false, string typed = "")
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source, realByDefault), 0x4000,
                                                          new CompilerOptions(RealByDefault: realByDefault));
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- DEFINT, DEFREAL, DEFSTR ----------

    [Fact]
    public void Apres_DEFINT_A_et_A_pourcent_sont_la_meme_variable()
    {
        Assert.Equal(" 5 \r\n", Run("10 DEFINT A-Z\n20 A = 5\n30 PRINT A%"));
    }

    [Fact]
    public void DEFREAL_rend_les_variables_a_virgule()
    {
        Assert.Equal(" 2.5  2.5 \r\n 3 \r\n", Run("10 DEFREAL A\n20 A = 2.5\n30 PRINT A;A!\n40 B = 2.5\n50 PRINT B"));
    }

    [Fact]
    public void DEFSTR_rend_les_variables_texte()
    {
        Assert.Equal("BONJOUR\r\n", Run("10 DEFSTR N\n20 N = \"BONJOUR\"\n30 PRINT N$"));
    }

    [Fact]
    public void Plusieurs_lettres_et_plages()
    {
        Assert.Equal("ABC 2.5 \r\n", Run("10 DEFSTR A-C, X : DEFREAL Y\n20 B = \"ABC\" : Y = 2.5\n30 PRINT B;Y"));
    }

    [Theory]
    [InlineData("10 DEFINT 5")]
    [InlineData("10 DEFINT Z-A")]
    [InlineData("10 DEFINT A-")]
    [InlineData("10 DEFINT")]
    [InlineData("10 DEFREAL AB")]
    public void Une_liste_de_lettres_fausse_est_signalee(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedLetterRange);
    }

    [Fact]
    public void Une_variable_BYTE_garde_son_type()
    {
        Assert.Equal(" 200 \r\n", Run("10 DEFREAL A-Z\n20 BYTE B\n30 B = 200\n40 PRINT B"));
    }

    // ---------- Réglage « Sans signe : à virgule » (F8) ----------

    [Fact]
    public void Avec_le_reglage_F8_les_variables_sans_signe_sont_a_virgule()
    {
        Assert.Equal(" 3.5 \r\n", Run("10 A = 7/2\n20 PRINT A", realByDefault: true));
        Assert.Equal(" 3.5 \r\n", Run("10 PRINT 7/2", realByDefault: true));
        Assert.Equal(" 3 \r\n", Run("10 PRINT 7/2"));
        Assert.Equal(" 1 \r\n", Run("10 A% = 7 : B% = 2 : PRINT A% MOD B%", realByDefault: true));
    }

    [Fact]
    public void Avec_le_reglage_F8_DEFINT_garde_des_entiers()
    {
        Assert.Equal(" 1  2  3 ", Run("10 DEFINT I\n20 FOR I = 1 TO 3 : PRINT I; : NEXT", realByDefault: true));
    }

    // ---------- FOR X! ----------

    [Fact]
    public void FOR_avec_une_variable_a_virgule()
    {
        Assert.Equal(" 0  0.25  0.5  0.75  1 ", Run("10 FOR X! = 0 TO 1 STEP 0.25 : PRINT X!; : NEXT"));
        Assert.Equal(" 1  0.5  0 ", Run("10 FOR X! = 1 TO 0 STEP -0.5 : PRINT X!; : NEXT X!"));
        Assert.Equal(" 1  2  3 ", Run("10 FOR I = 1 TO 3 : PRINT I; : NEXT", realByDefault: true));
    }

    // ---------- INPUT A! ----------

    [Theory]
    [InlineData("1.5", " 1.5 ")]
    [InlineData("-2.5E-3", "-0.0025 ")]
    [InlineData("1E3", " 1000 ")]
    [InlineData(".5", " 0.5 ")]
    [InlineData("  +7  ", " 7 ")]
    [InlineData("123456789", " 123456789 ")]
    [InlineData("0", " 0 ")]
    public void INPUT_lit_un_nombre_a_virgule(string typed, string shown)
    {
        Assert.Equal("? " + typed + "\r\n" + shown + "\r\n", Run("10 INPUT A!\n20 PRINT A!", typed: typed + "\r"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("1E")]
    [InlineData(".")]
    [InlineData("12x")]
    public void Un_texte_qui_n_est_pas_un_nombre_donne_Redo_from_start(string typed)
    {
        Assert.Equal("? " + typed + "\r\n?Redo from start\r\n? 2\r\n 2 \r\n",
                     Run("10 INPUT A!\n20 PRINT A!", typed: typed + "\r2\r"));
    }

    [Fact]
    public void INPUT_de_deux_nombres_a_virgule()
    {
        Assert.Equal("? 1.5, -2\r\n 1.5 \r\n-2 \r\n", Run("10 INPUT A!, B!\n20 PRINT A!\n30 PRINT B!", typed: "1.5, -2\r"));
    }

    // ---------- @A! ----------

    [Fact]
    public void Arobase_donne_l_adresse_des_5_octets()
    {
        Assert.Equal(" 129 \r\n", Run("10 A! = 1\n20 PRINT PEEK(@A! + 4)"));
    }
}
