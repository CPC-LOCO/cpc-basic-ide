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

/// <summary>DIM et les tableaux d'entiers (lot C, D058).</summary>
public class ArrayTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    // Lance le programme ; la mémoire après le programme est remplie de &AA, pour vérifier que DIM met les cases à 0
    static string Run(string source)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        for (int address = 0x4000 + program.Code.Length; address < 0xA700; address++)
            cpu.Memory[address] = 0xAA;
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Fact]
    public void DIM_garde_le_nom_et_la_plus_grande_case_de_chaque_dimension()
    {
        var dim = Assert.IsType<DimStatement>(Parse("10 DIM A(10), T(2, 3)").Statements[0]);

        Assert.Equal(2, dim.Arrays.Count);
        Assert.Equal("A", dim.Arrays[0].Name);
        Assert.Equal(new NumberExpression(10), dim.Arrays[0].Sizes[0]);
        Assert.Equal("T", dim.Arrays[1].Name);
        Assert.Equal(2, dim.Arrays[1].Sizes.Count);
    }

    [Fact]
    public void Une_case_se_lit_et_s_ecrit_avec_ou_sans_LET()
    {
        BasicProgram program = Parse("10 A(3) = 5\n20 LET B(1, 2) = A(3) + 1");

        var first = Assert.IsType<ArrayAssignStatement>(program.Statements[0]);
        Assert.Equal("A", first.Name);
        var second = Assert.IsType<ArrayAssignStatement>(program.Statements[1]);
        Assert.Equal(2, second.Indexes.Count);
        var sum = Assert.IsType<BinaryExpression>(second.Value);
        Assert.IsType<ArrayElementExpression>(sum.Left);
    }

    [Fact]
    public void La_variable_A_et_le_tableau_A_sont_deux_choses_differentes()
    {
        Assert.Equal(" 7  3 \r\n", Run("10 A = 7 : A(0) = 3\n20 PRINT A; A(0)"));
    }

    [Theory]
    [InlineData("10 DIM A")]
    [InlineData("10 DIM 5")]
    [InlineData("10 DIM")]
    [InlineData("10 DIM PRINT(3)")]
    public void DIM_sans_nom_ou_sans_parentheses_est_signale(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedArrayName);
    }

    [Theory]
    [InlineData("10 DIM A(-1)")]
    [InlineData("10 DIM A(32767)")]
    [InlineData("10 DIM A(5, -2)")]
    public void Une_taille_ecrite_hors_de_0_a_32766_est_refusee(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Theory]
    [InlineData("10 DIM A(5)\n20 A(1, 2) = 3")]
    [InlineData("10 A(1) = 2\n20 PRINT A(1, 1)")]
    [InlineData("10 DIM T(2, 2)\n20 DIM T(3)")]
    public void Un_tableau_garde_toujours_le_meme_nombre_de_dimensions(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ArrayDimensionMismatch);
    }

    [Fact]
    public void Un_nom_declare_BYTE_ne_peut_pas_encore_etre_un_tableau()
    {
        Assert.Contains(Errors("10 BYTE T\n20 T(1) = 2"), e => e.Code == DiagnosticCode.ByteArrayNotYet);
    }

    [Theory]
    [InlineData("10 A = SIN(3)")]
    [InlineData("10 DIM ERASE(3)")]
    [InlineData("10 READ(1) = 2")]
    public void Un_mot_du_Locomotive_pas_encore_disponible_ne_devient_pas_un_tableau(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.NotYetAvailable);
    }

    [Theory]
    [InlineData("10 DIM A$(5)")]
    [InlineData("10 B!(2) = 1")]
    public void Les_tableaux_de_textes_et_de_reels_n_existent_pas_encore(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.UnsupportedVariableType);
    }

    [Theory]
    [InlineData("10 A(1 = 2")]
    [InlineData("10 PRINT A(1, 2")]
    public void Une_parenthese_oubliee_est_signalee(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedClosingParenthesis);
    }

    [Fact]
    public void DIM_est_un_mot_reserve()
    {
        Assert.NotEmpty(Errors("10 DIM = 3"));
    }

    [Fact]
    public void Le_message_d_erreur_explique_le_nombre_de_dimensions()
    {
        Diagnostic error = Errors("10 DIM A(5)\n20 A(1, 2) = 3").First(e => e.Code == DiagnosticCode.ArrayDimensionMismatch);

        Assert.Contains("A()", error.Message);
        Assert.Contains("2 numéro(s)", error.Message);
    }

    // ---------- Exécution ----------

    [Fact]
    public void DIM_cree_les_cases_de_0_a_la_valeur_ecrite()
    {
        Assert.Equal(" 30 \r\n", Run("10 DIM A(5)\n20 FOR I = 0 TO 5 : A(I) = I * 10 : NEXT\n30 PRINT A(3)"));
    }

    [Fact]
    public void Les_cases_valent_0_au_depart()
    {
        Assert.Equal(" 0  0 \r\n", Run("10 DIM A(3), T(2, 2)\n20 PRINT A(2); T(1, 1)"));
    }

    [Fact]
    public void Une_valeur_negative_se_range_et_se_relit()
    {
        Assert.Equal("-1234 \r\n", Run("10 DIM A(2)\n20 A(1) = -1234\n30 PRINT A(1)"));
    }

    [Fact]
    public void Un_tableau_utilise_sans_DIM_a_les_cases_0_a_10()
    {
        Assert.Equal(" 4 \r\n", Run("10 A(10) = 4\n20 PRINT A(10)"));
    }

    [Fact]
    public void Sans_DIM_la_case_11_n_existe_pas()
    {
        Assert.Equal("Subscript out of range in 10\r\n", Run("10 A(11) = 4"));
    }

    [Fact]
    public void Une_case_au_dela_de_DIM_arrete_le_programme_avec_le_numero_de_ligne()
    {
        Assert.Equal(" 1 \r\nSubscript out of range in 30\r\n",
                     Run("10 DIM A(5)\n20 PRINT 1\n30 A(6) = 1\n40 PRINT 2"));
    }

    [Fact]
    public void Un_numero_de_case_negatif_est_refuse()
    {
        Assert.Equal("Subscript out of range in 30\r\n", Run("10 DIM A(5)\n20 I = -1\n30 PRINT A(I)"));
    }

    [Fact]
    public void Le_message_passe_a_la_ligne_si_le_curseur_n_est_pas_au_debut()
    {
        Assert.Equal("X\r\nSubscript out of range in 20\r\n", Run("10 PRINT \"X\";\n20 A(11) = 1"));
    }

    [Fact]
    public void Sans_numeros_de_ligne_le_message_donne_la_ligne_de_l_editeur()
    {
        Assert.Equal("Subscript out of range in 2\r\n", Run("DIM A(2)\nA(3) = 1"));
    }

    [Fact]
    public void Un_grand_numero_de_ligne_s_affiche_correctement()
    {
        Assert.Equal("Subscript out of range in 40000\r\n", Run("40000 A(11) = 1"));
    }

    [Fact]
    public void Un_tableau_a_deux_dimensions_range_chaque_case_a_part()
    {
        string source = "10 DIM T(2, 3)\n" +
                        "20 FOR I = 0 TO 2 : FOR J = 0 TO 3 : T(I, J) = I * 10 + J : NEXT : NEXT\n" +
                        "30 PRINT T(0, 0); T(0, 3); T(1, 0); T(2, 1); T(2, 3)";

        Assert.Equal(" 0  3  10  21  23 \r\n", Run(source));
    }

    [Theory]
    [InlineData("10 DIM T(2, 3)\n20 T(0, 4) = 1")]
    [InlineData("10 DIM T(2, 3)\n20 T(3, 0) = 1")]
    [InlineData("10 DIM T(2, 3)\n20 T(-1, 1) = 1")]
    public void Chaque_dimension_est_verifiee(string source)
    {
        Assert.Equal("Subscript out of range in 20\r\n", Run(source));
    }

    [Fact]
    public void Un_tableau_a_trois_dimensions_fonctionne()
    {
        Assert.Equal(" 8 \r\n", Run("10 DIM C(1, 2, 3)\n20 C(1, 2, 3) = 7 : C(0, 0, 0) = 1\n30 PRINT C(1, 2, 3) + C(0, 0, 0)"));
    }

    [Fact]
    public void Sans_DIM_un_tableau_a_deux_dimensions_a_aussi_les_cases_0_a_10()
    {
        Assert.Equal(" 5 \r\nSubscript out of range in 30\r\n", Run("10 T(10, 10) = 5\n20 PRINT T(10, 10)\n30 T(0, 11) = 1"));
    }

    [Fact]
    public void La_taille_peut_etre_calculee_comme_dans_le_Locomotive()
    {
        Assert.Equal(" 2 \r\nSubscript out of range in 40\r\n",
                     Run("10 N = 4\n20 DIM A(N)\n30 A(4) = 2 : PRINT A(4)\n40 A(5) = 1"));
    }

    [Fact]
    public void Une_case_peut_servir_de_numero_de_case()
    {
        Assert.Equal(" 7 \r\n", Run("10 DIM A(5), B(5)\n20 B(2) = 4 : A(B(2)) = 7\n30 PRINT A(4)"));
    }

    [Fact]
    public void Une_case_sert_dans_IF_et_dans_FOR()
    {
        Assert.Equal(" 1 \r\n 3  4 \r\n",
                     Run("10 A(1) = 3\n20 IF A(1) = 3 THEN PRINT 1\n30 FOR I = A(1) TO 4 : PRINT I; : NEXT : PRINT"));
    }

    [Fact]
    public void Un_second_DIM_du_meme_tableau_est_refuse()
    {
        Assert.Equal("Array already dimensioned in 20\r\n", Run("10 DIM A(3)\n20 DIM A(3)"));
    }

    [Fact]
    public void DIM_apres_une_utilisation_sans_DIM_est_refuse()
    {
        Assert.Equal("Array already dimensioned in 20\r\n", Run("10 A(1) = 1\n20 DIM A(5)"));
    }

    [Theory]
    [InlineData("10 DIM A(30000)")]
    [InlineData("10 DIM A(300, 300)")]
    [InlineData("10 DIM A(100, 100, 100)")]
    public void Un_tableau_trop_grand_donne_Memory_full(string source)
    {
        Assert.Equal("Memory full in 10\r\n", Run(source));
    }

    [Fact]
    public void Plusieurs_tableaux_ne_se_chevauchent_pas()
    {
        Assert.Equal(" 1  2  3 \r\n",
                     Run("10 DIM A(100), B(100), C(100)\n20 A(100) = 1 : B(0) = 2 : C(0) = 3\n30 PRINT A(100); B(0); C(0)"));
    }

    [Fact]
    public void MEMORY_limite_la_place_des_tableaux()
    {
        Assert.Equal("Memory full in 30\r\n", Run("10 M = 30000\n20 MEMORY M\n30 DIM A(10000)"));
    }

    [Fact]
    public void Sous_la_limite_de_MEMORY_le_tableau_est_cree()
    {
        Assert.Equal(" 9 \r\n", Run("10 M = 30000\n20 MEMORY M\n30 DIM A(1000)\n40 A(1000) = 9 : PRINT A(1000)"));
    }

    [Fact]
    public void MEMORY_sous_les_tableaux_deja_crees_donne_Memory_full_avec_sa_ligne()
    {
        Assert.Equal("Memory full in 20\r\n", Run("10 DIM A(10)\n20 MEMORY 16500"));
    }

    [Fact]
    public void Un_programme_sans_tableau_ne_contient_pas_le_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 PRINT 1"), 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.ArrayName, program.Modules);
    }

    [Fact]
    public void Un_programme_avec_un_tableau_contient_le_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 DIM A(3)"), 0x4000);

        Assert.Contains(RuntimeLibrary.ArrayName, program.Modules);
    }
}
