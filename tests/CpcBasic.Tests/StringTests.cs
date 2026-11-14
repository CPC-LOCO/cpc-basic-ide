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

/// <summary>Lot E, étape E1 (D062) : variables texte, +, comparaisons, LEN, PRINT et INPUT de textes.</summary>
public class StringTests
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

    [Fact]
    public void Une_variable_texte_recoit_un_texte()
    {
        var assign = Assert.IsType<AssignStatement>(Parse("10 A$ = \"BONJOUR\"").Statements[0]);

        Assert.Equal(new StringLiteralExpression("BONJOUR"), assign.Value);
    }

    [Fact]
    public void Le_plus_entre_deux_textes_les_met_bout_a_bout()
    {
        var assign = Assert.IsType<AssignStatement>(Parse("10 A$ = B$ + \"!\"").Statements[0]);

        Assert.IsType<ConcatExpression>(assign.Value);
    }

    [Theory]
    [InlineData("10 A = \"X\"")]
    [InlineData("10 A$ = 1")]
    [InlineData("10 A$ = B$ + 1")]
    [InlineData("10 PRINT \"A\" * 2")]
    [InlineData("10 IF A$ THEN PRINT 1")]
    [InlineData("10 IF A$ = 1 THEN PRINT 1")]
    [InlineData("10 FOR A$ = 1 TO 2 : NEXT")]
    [InlineData("10 A = LEN(5)")]
    [InlineData("10 A = -\"X\"")]
    [InlineData("10 A = NOT A$")]
    [InlineData("10 POKE 1, A$")]
    public void Un_texte_et_un_nombre_ne_se_melangent_pas(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void Un_texte_de_plus_de_255_caracteres_est_refuse()
    {
        Assert.Contains(Errors("10 A$ = \"" + new string('X', 256) + "\""), e => e.Code == DiagnosticCode.StringTooLong);
    }

    [Fact]
    public void Les_tableaux_de_textes_viendront_plus_tard()
    {
        Assert.Contains(Errors("10 A$(1) = \"X\""), e => e.Code == DiagnosticCode.StringNotYetHere);
    }

    // ---------- Exécution ----------

    [Fact]
    public void PRINT_affiche_une_variable_texte()
    {
        Assert.Equal("BONJOUR\r\n", Run("10 A$ = \"BONJOUR\" : PRINT A$"));
    }

    [Fact]
    public void Deux_textes_bout_a_bout_et_leur_longueur()
    {
        Assert.Equal("BONJOUR 7 \r\n", Run("10 A$ = \"BON\" : B$ = A$ + \"JOUR\" : PRINT B$; LEN(B$)"));
    }

    [Fact]
    public void PRINT_d_un_calcul_de_textes()
    {
        Assert.Equal("AB\r\n", Run("10 PRINT \"A\" + \"B\""));
    }

    [Fact]
    public void Un_texte_vide()
    {
        Assert.Equal(" 0 []\r\n", Run("10 A$ = \"\" : PRINT LEN(A$); \"[\"; A$; \"]\""));
    }

    [Fact]
    public void Une_variable_texte_jamais_remplie_est_vide()
    {
        Assert.Equal(" 0 \r\n", Run("10 PRINT LEN(A$)"));
    }

    [Fact]
    public void Les_comparaisons_de_textes()
    {
        Assert.Equal("-1 -1 -1 -1  0 \r\n",
                     Run("10 A$ = \"ABC\"\n20 PRINT A$ = \"ABC\"; A$ < \"ABD\"; A$ > \"AB\"; \"\" < \"A\"; A$ <> \"ABC\""));
    }

    [Fact]
    public void IF_compare_deux_textes()
    {
        Assert.Equal(" 1 \r\n", Run("10 A$ = \"OUI\"\n20 IF A$ = \"OUI\" THEN PRINT 1 ELSE PRINT 2"));
    }

    [Fact]
    public void Chaque_variable_garde_son_propre_texte()
    {
        Assert.Equal("ZXY\r\n", Run("10 A$ = \"X\" + \"Y\" : B$ = A$ : A$ = \"Z\" : PRINT A$; B$"));
    }

    [Fact]
    public void Un_texte_construit_dans_une_boucle()
    {
        Assert.Equal("*****\r\n", Run("10 FOR I = 1 TO 5 : A$ = A$ + \"*\" : NEXT : PRINT A$"));
    }

    [Fact]
    public void Le_menage_garde_les_textes_utiles()
    {
        // Peu de place (MEMORY) et beaucoup de textes jetés : le ménage passe plusieurs fois, B$ doit survivre
        string source = "10 MEMORY 22000\n" +
                        "20 B$ = \"GARDE\" + \"!\"\n" +
                        "30 FOR I = 1 TO 200 : A$ = \"\" : FOR J = 1 TO 20 : A$ = A$ + \"AB\" : NEXT : NEXT\n" +
                        "40 PRINT B$; LEN(A$)";

        Assert.Equal("GARDE! 40 \r\n", Run(source));
    }

    [Fact]
    public void Un_texte_de_plus_de_255_caracteres_arrete_le_programme()
    {
        Assert.Equal("String too long in 20\r\n", Run("10 A$ = \"X\"\n20 FOR I = 1 TO 8 : A$ = A$ + A$ : NEXT"));
    }

    [Fact]
    public void Trop_de_textes_intermediaires_dans_une_instruction()
    {
        string sum = string.Join(" + ", Enumerable.Repeat("A$", 18));

        Assert.Equal("String expression too complex in 20\r\n", Run("10 A$ = \"X\"\n20 B$ = " + sum));
    }

    [Fact]
    public void La_variable_A_et_la_variable_A_texte_sont_differentes()
    {
        Assert.Equal(" 5 X\r\n", Run("10 A = 5 : A$ = \"X\" : PRINT A; A$"));
    }

    [Fact]
    public void Textes_et_tableaux_partagent_la_memoire_libre()
    {
        Assert.Equal("OK! 7 \r\n", Run("10 DIM T(100) : A$ = \"OK\" + \"!\" : T(100) = 7 : PRINT A$; T(100)"));
    }

    [Fact]
    public void MEMORY_avant_les_textes()
    {
        Assert.Equal("AB\r\n", Run("10 MEMORY 30000\n20 A$ = \"A\" + \"B\" : PRINT A$"));
    }

    [Fact]
    public void MEMORY_apres_des_textes_donne_Memory_full()
    {
        Assert.Equal("Memory full in 20\r\n", Run("10 A$ = \"A\" + \"B\"\n20 MEMORY 30000"));
    }

    [Fact]
    public void INPUT_lit_un_texte()
    {
        Assert.Equal("? BONJOUR\r\nBONJOUR\r\n", Run("10 INPUT A$\n20 PRINT A$", "BONJOUR\r"));
    }

    [Fact]
    public void INPUT_texte_entre_guillemets_avec_une_virgule()
    {
        Assert.Equal("? \"A,B\", 5\r\nA,B 5 \r\n", Run("10 INPUT A$, B\n20 PRINT A$; B", "\"A,B\", 5\r"));
    }

    [Fact]
    public void INPUT_texte_vide()
    {
        Assert.Equal("? \r\n 0 \r\n", Run("10 INPUT A$\n20 PRINT LEN(A$)", "\r"));
    }

    [Fact]
    public void Un_programme_sans_texte_n_ajoute_pas_le_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 PRINT \"BONJOUR\""), 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.StringsName, program.Modules);
    }
}
