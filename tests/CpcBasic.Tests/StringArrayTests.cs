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

/// <summary>Lot E, étape E3b (D065) : tableaux de textes et MID$(A$, début [, longueur]) = texte.</summary>
public class StringArrayTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    /// <summary>Lance le programme en tapant <paramref name="typed"/> (le caractère \r est la touche Entrée).</summary>
    static string Run(string source, string typed = "")
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Theory]
    [InlineData("10 N$(1) = 5")]
    [InlineData("10 DIM N(2) : N(1) = \"X\"")]
    [InlineData("10 MID$(A, 1) = \"X\"")]
    [InlineData("10 MID$(A$, 1) = 5")]
    public void Un_texte_et_un_nombre_ne_se_melangent_pas(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void MID_dollar_egal_est_une_instruction()
    {
        var mid = Assert.IsType<MidAssignStatement>(Parse("10 MID$(A$, 2, 1) = \"X\"").Statements[0]);

        Assert.Equal(new VariableExpression("A$"), mid.Target);
        Assert.NotNull(mid.Length);
    }

    // ---------- Tableaux de textes ----------

    [Fact]
    public void Un_tableau_de_textes()
    {
        Assert.Equal("AD! 0 \r\n", Run("10 DIM N$(3)\n20 N$(0) = \"A\" : N$(3) = \"D\" + \"!\"\n30 PRINT N$(0); N$(3); LEN(N$(1))"));
    }

    [Fact]
    public void READ_remplit_un_tableau_de_textes()
    {
        Assert.Equal("MERLUN\r\n", Run("10 DIM J$(2)\n20 FOR I = 0 TO 2 : READ J$(I) : NEXT\n30 PRINT J$(2); J$(0)\n40 DATA LUN, MAR, MER"));
    }

    [Fact]
    public void Sans_DIM_un_tableau_de_textes_a_les_cases_0_a_10()
    {
        Assert.Equal("X 0 \r\n", Run("10 A$(10) = \"X\" : PRINT A$(10); LEN(A$(5))"));
    }

    [Fact]
    public void Un_tableau_de_textes_a_deux_dimensions()
    {
        Assert.Equal("AZ\r\n", Run("10 DIM G$(1, 2)\n20 G$(1, 2) = \"Z\" : G$(0, 0) = \"A\"\n30 PRINT G$(0, 0); G$(1, 2)"));
    }

    [Fact]
    public void Tableaux_de_nombres_et_de_textes_ensemble()
    {
        Assert.Equal(" 7 S 1 \r\n", Run("10 DIM T(3), N$(3)\n20 T(3) = 7 : N$(3) = \"S\" : T(0) = 1\n30 PRINT T(3); N$(3); T(0)"));
    }

    [Fact]
    public void Une_case_absente_d_un_tableau_de_textes()
    {
        Assert.Equal("Subscript out of range in 20\r\n", Run("10 DIM N$(2)\n20 N$(3) = \"X\""));
    }

    [Fact]
    public void Le_menage_garde_les_textes_des_tableaux()
    {
        string source = "10 MEMORY 22000\n" +
                        "20 DIM N$(9)\n" +
                        "30 FOR K = 1 TO 100 : FOR I = 0 TO 9 : N$(I) = STR$(K * 10 + I) : NEXT : NEXT\n" +
                        "40 PRINT N$(0); N$(9)";

        Assert.Equal(" 1000 1009\r\n", Run(source));
    }

    [Fact]
    public void INPUT_dans_une_case_d_un_tableau_de_textes()
    {
        Assert.Equal("? BOB\r\nBOB\r\n", Run("10 DIM N$(2)\n20 INPUT N$(1)\n30 PRINT N$(1)", "BOB\r"));
    }

    [Fact]
    public void LINE_INPUT_dans_une_case_d_un_tableau_de_textes()
    {
        Assert.Equal("A, B\r\nA, B\r\n", Run("10 LINE INPUT N$(0)\n20 PRINT N$(0)", "A, B\r"));
    }

    [Fact]
    public void Arobase_d_une_case_de_texte_donne_sa_fiche_de_3_octets()
    {
        Assert.Equal(" 2  3 \r\n", Run("10 DIM N$(2) : N$(1) = \"HI\"\n20 PRINT PEEK(@N$(1)); @N$(2) - @N$(1)"));
    }

    // ---------- MID$(...) = ----------

    [Fact]
    public void MID_dollar_remplace_des_lettres()
    {
        Assert.Equal("BONSOIR\r\n", Run("10 A$ = \"BONJOUR\" : MID$(A$, 4, 4) = \"SOIR\" : PRINT A$"));
    }

    [Fact]
    public void MID_dollar_ne_change_pas_le_texte_du_programme_ni_les_copies()
    {
        Assert.Equal("AXYDEABCDE\r\n", Run("10 A$ = \"ABCDE\" : B$ = A$ : MID$(A$, 2) = \"XY\" : PRINT A$; B$"));
    }

    [Fact]
    public void MID_dollar_ne_change_pas_la_longueur()
    {
        Assert.Equal("ABX\r\n", Run("10 A$ = \"ABC\" : MID$(A$, 3) = \"XYZ\" : PRINT A$"));
    }

    [Fact]
    public void MID_dollar_avec_une_longueur()
    {
        Assert.Equal("AXCDEF\r\n", Run("10 A$ = \"ABCDEF\" : MID$(A$, 2, 1) = \"XYZ\" : PRINT A$"));
    }

    [Fact]
    public void MID_dollar_dans_une_case_de_tableau()
    {
        Assert.Equal("ABA\r\n", Run("10 DIM N$(1) : N$(1) = \"AAA\" : MID$(N$(1), 2) = \"B\" : PRINT N$(1)"));
    }

    [Theory]
    [InlineData("10 A$ = \"AB\" : MID$(A$, 3) = \"X\"")]
    [InlineData("10 A$ = \"AB\" : MID$(A$, 0) = \"X\"")]
    public void MID_dollar_hors_du_texte_donne_Improper_argument(string source)
    {
        Assert.Equal("Improper argument in 10\r\n", Run(source));
    }
}
