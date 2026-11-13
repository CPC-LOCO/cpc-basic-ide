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

/// <summary>INPUT pour les nombres entiers : analyse, messages et saisie simulée sur le mini-Z80 (lot B, D055).</summary>
public class InputTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    /// <summary>Lance le programme en tapant <paramref name="typed"/> (le caractère \r est la touche Entrée).</summary>
    static string Run(string source, string typed)
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
    public void INPUT_sans_texte_affiche_le_point_d_interrogation()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 INPUT A").Statements[0]);

        Assert.Null(input.Prompt);
        Assert.True(input.Question);
        Assert.False(input.NoNewline);
        Assert.Equal(new[] { "A" }, input.Variables.ToArray());
    }

    [Fact]
    public void INPUT_avec_texte_et_point_virgule_garde_le_point_d_interrogation()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 INPUT \"AGE\"; A").Statements[0]);

        Assert.Equal("AGE", input.Prompt);
        Assert.True(input.Question);
    }

    [Fact]
    public void INPUT_avec_texte_et_virgule_n_affiche_pas_le_point_d_interrogation()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 INPUT \"AGE\", A").Statements[0]);

        Assert.Equal("AGE", input.Prompt);
        Assert.False(input.Question);
    }

    [Fact]
    public void INPUT_suivi_d_un_point_virgule_garde_le_curseur_sur_la_ligne()
    {
        Assert.True(Assert.IsType<InputStatement>(Parse("10 INPUT ; A").Statements[0]).NoNewline);
    }

    [Fact]
    public void INPUT_accepte_plusieurs_variables()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 INPUT \"XY\"; X, Y, Z").Statements[0]);

        Assert.Equal(new[] { "X", "Y", "Z" }, input.Variables.ToArray());
    }

    [Theory]
    [InlineData("10 INPUT", DiagnosticCode.ExpectedVariableName)]
    [InlineData("10 INPUT 5", DiagnosticCode.ExpectedVariableName)]
    [InlineData("10 INPUT \"X\" A", DiagnosticCode.UnexpectedToken)]
    public void INPUT_mal_ecrit_est_refuse(string source, DiagnosticCode expected)
    {
        Assert.Contains(Errors(source), e => e.Code == expected);
    }

    [Fact]
    public void Une_variable_de_INPUT_compte_comme_utilisee_avant_BYTE()
    {
        Assert.Contains(Errors("10 INPUT X\n20 BYTE X"), e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    // ---------- Saisie simulée ----------

    [Fact]
    public void INPUT_affiche_le_point_d_interrogation_fait_l_echo_et_range_le_nombre()
    {
        Assert.Equal("? 123\r\n 123 \r\n", Run("10 INPUT A\n20 PRINT A", "123\r"));
    }

    [Fact]
    public void Le_texte_suivi_d_un_point_virgule_est_suivi_du_point_d_interrogation()
    {
        Assert.Equal("AGE? 7\r\n 7 \r\n", Run("10 INPUT \"AGE\"; A\n20 PRINT A", "7\r"));
    }

    [Fact]
    public void Le_texte_suivi_d_une_virgule_n_est_pas_suivi_du_point_d_interrogation()
    {
        Assert.Equal("AGE7\r\n 7 \r\n", Run("10 INPUT \"AGE\", A\n20 PRINT A", "7\r"));
    }

    [Fact]
    public void INPUT_avec_point_virgule_ne_passe_pas_a_la_ligne()
    {
        Assert.Equal("? 5 5 \r\n", Run("10 INPUT ; A\n20 PRINT A", "5\r"));
    }

    [Theory]
    [InlineData("123", " 123 ")]
    [InlineData("-5", "-5 ")]
    [InlineData("+7", " 7 ")]
    [InlineData("  42  ", " 42 ")]
    [InlineData("32767", " 32767 ")]
    [InlineData("-32768", "-32768 ")]
    [InlineData("0", " 0 ")]
    public void Les_nombres_entiers_sont_lus_avec_leur_signe(string typed, string shown)
    {
        Assert.Equal("? " + typed + "\r\n" + shown + "\r\n", Run("10 INPUT A\n20 PRINT A", typed + "\r"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("12x")]
    [InlineData("32768")]
    [InlineData("-32769")]
    [InlineData("--5")]
    [InlineData("99999")]
    public void Un_texte_qui_n_est_pas_un_entier_donne_Redo_from_start_puis_on_recommence(string typed)
    {
        string expected = "? " + typed + "\r\n?Redo from start\r\n? 9\r\n 9 \r\n";

        Assert.Equal(expected, Run("10 INPUT A\n20 PRINT A", typed + "\r9\r"));
    }

    [Fact]
    public void Plusieurs_nombres_sur_la_meme_ligne_sont_separes_par_des_virgules()
    {
        Assert.Equal("? 3,4\r\n 3 \r\n 4 \r\n", Run("10 INPUT A, B\n20 PRINT A\n30 PRINT B", "3,4\r"));
    }

    [Fact]
    public void Quand_il_manque_un_nombre_deux_points_d_interrogation_demandent_la_suite()
    {
        Assert.Equal("? 3\r\n?? 4\r\n 3 \r\n 4 \r\n", Run("10 INPUT A, B\n20 PRINT A\n30 PRINT B", "3\r4\r"));
    }

    [Fact]
    public void Un_nombre_en_trop_est_signale_et_ignore()
    {
        Assert.Equal("? 1,2\r\n?Extra ignored\r\n 1 \r\n", Run("10 INPUT A\n20 PRINT A", "1,2\r"));
    }

    [Fact]
    public void La_touche_Suppr_efface_le_dernier_caractere()
    {
        // 1, 2, Suppr, 3 : le texte lu est « 13 »
        string output = Run("10 INPUT A\n20 PRINT A", "12\x7F" + "3\r");

        Assert.Equal("? 12\x7F" + "3\r\n 13 \r\n", output);
    }

    [Fact]
    public void Suppr_sur_une_ligne_vide_ne_fait_rien()
    {
        Assert.Equal("? 5\r\n 5 \r\n", Run("10 INPUT A\n20 PRINT A", "\x7F" + "5\r"));
    }

    [Fact]
    public void Les_caracteres_de_commande_sont_ignores()
    {
        Assert.Equal("? 5\r\n 5 \r\n", Run("10 INPUT A\n20 PRINT A", "\t5\r"));
    }

    [Fact]
    public void Une_variable_BYTE_accepte_jusqu_a_255()
    {
        Assert.Equal("? 255\r\n 255 \r\n", Run("10 BYTE B\n20 INPUT B\n30 PRINT B", "255\r"));
    }

    [Theory]
    [InlineData("256")]
    [InlineData("-1")]
    public void Une_variable_BYTE_refuse_ce_qui_ne_tient_pas_sur_un_octet(string typed)
    {
        string expected = "? " + typed + "\r\n?Redo from start\r\n? 7\r\n 7 \r\n";

        Assert.Equal(expected, Run("10 BYTE B\n20 INPUT B\n30 PRINT B", typed + "\r7\r"));
    }
}
