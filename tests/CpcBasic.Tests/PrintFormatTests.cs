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
/// Lot H2 (D073) : virgule (zones), ZONE, TAB, SPC et WRITE. Les écrans attendus viennent du même programme tapé dans
/// le vrai BASIC (SugarBox, 15 nov. 2026), en MODE 1 (40 colonnes).
/// </summary>
public class PrintFormatTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source, byte width = 40)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = width };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    static string Spaces(int n) => new(' ', n);

    // ---------- Virgule et ZONE ----------

    [Fact]
    public void Textes_separes_par_des_virgules()
    {
        Assert.Equal("A" + Spaces(12) + "B" + Spaces(12) + "C\r\n", Run("10 PRINT \"A\",\"B\",\"C\""));
    }

    [Fact]
    public void Nombres_separes_par_des_virgules()
    {
        Assert.Equal(" 1 " + Spaces(10) + " 22 " + Spaces(9) + "-333 \r\n", Run("10 PRINT 1,22,-333"));
    }

    [Fact]
    public void Une_zone_qui_ne_tient_pas_passe_a_la_ligne()
    {
        // la 4e zone commencerait colonne 40 et finirait colonne 52 : à la ligne
        Assert.Equal("1" + Spaces(12) + "2" + Spaces(12) + "3\r\n4\r\n", Run("10 PRINT \"1\",\"2\",\"3\",\"4\""));
    }

    [Fact]
    public void Un_texte_plus_long_qu_une_zone_saute_a_la_zone_suivante()
    {
        Assert.Equal("ABCDEFGHIJKLMN" + Spaces(12) + "Z\r\n", Run("10 PRINT \"ABCDEFGHIJKLMN\",\"Z\""));
    }

    [Fact]
    public void ZONE_change_la_largeur_des_zones()
    {
        Assert.Equal("X    Y    Z    W\r\n", Run("10 ZONE 5:PRINT \"X\",\"Y\",\"Z\",\"W\""));
    }

    [Fact]
    public void Virgule_a_la_fin_puis_virgule_au_debut()
    {
        Assert.Equal(" 1 " + Spaces(10) + "SUITE\r\n" + Spaces(13) + "VIDE\r\n",
                     Run("10 PRINT 1,\n20 PRINT \"SUITE\"\n30 PRINT ,\"VIDE\""));
    }

    [Theory]
    [InlineData("10 ZONE 0")]
    [InlineData("10 ZONE 256")]
    public void ZONE_hors_limites(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Fact]
    public void ZONE_calculee_a_zero_Improper_argument()
    {
        Assert.Contains("Improper argument in 20", Run("10 Z = 0\n20 ZONE Z"));
    }

    // ---------- TAB et SPC ----------

    [Fact]
    public void TAB_en_avant_puis_en_arriere()
    {
        Assert.Equal(Spaces(9) + "T10\r\n" + Spaces(4) + "T5\r\n", Run("10 PRINT TAB(10);\"T10\";TAB(5);\"T5\""));
    }

    [Fact]
    public void TAB_jusqu_au_bord_droit()
    {
        Assert.Equal("X" + Spaces(36) + "YZW\r\n", Run("10 PRINT \"X\";TAB(38);\"YZW\""));
    }

    [Fact]
    public void TAB_sans_point_virgule_et_a_la_colonne_actuelle()
    {
        Assert.Equal(Spaces(4) + "A\r\nB\r\n", Run("10 PRINT TAB(5)\"A\"\n20 PRINT TAB(1)\"B\""));
    }

    [Fact]
    public void TAB_plus_grand_que_la_largeur_est_ramene_dans_la_ligne()
    {
        Assert.Equal(Spaces(4) + "A\r\n", Run("10 PRINT TAB(45);\"A\""));
    }

    [Fact]
    public void SPC_ajoute_des_espaces()
    {
        Assert.Equal("AB   CDEF\r\n", Run("10 PRINT \"AB\";SPC(3);\"CD\";SPC(0);\"EF\""));
    }

    [Fact]
    public void SPC_negatif_ou_trop_grand()
    {
        Assert.Equal("AB" + Spaces(2) + "C\r\n", Run("10 N = -4\n20 PRINT \"A\";SPC(N);\"B\";SPC(42);\"C\""));
    }

    [Fact]
    public void TAB_et_SPC_avec_des_valeurs_calculees()
    {
        Assert.Equal(Spaces(5) + "A" + Spaces(4) + "B\r\n", Run("10 N = 3\n20 PRINT TAB(N * 2);\"A\";SPC(N + 1);\"B\""));
    }

    [Fact]
    public void TAB_dans_une_fenetre_etroite()
    {
        Assert.Equal(Spaces(2) + "A\r\n", Run("10 PRINT TAB(13);\"A\"", width: 10));
    }

    [Fact]
    public void PRINT_fini_par_TAB_passe_a_la_ligne()
    {
        Assert.Equal(Spaces(3) + "\r\n", Run("10 PRINT TAB(4)"));
    }

    [Fact]
    public void TAB_sans_parenthese_fermante()
    {
        Assert.Contains(Errors("10 PRINT TAB(4;\"A\""), e => e.Code == DiagnosticCode.ExpectedClosingParenthesis);
    }

    // ---------- Élément qui ne tient pas dans la fin de la ligne (constaté dans SugarBox) ----------

    [Fact]
    public void Un_texte_qui_ne_tient_pas_passe_entier_a_la_ligne()
    {
        Assert.Equal("X" + Spaces(36) + "\r\nABCD\r\n", Run("10 PRINT \"X\";TAB(38);\"ABCD\""));
    }

    [Fact]
    public void Une_variable_texte_qui_ne_tient_pas_passe_a_la_ligne()
    {
        Assert.Equal(Spaces(38) + "\r\nABCD\r\n", Run("10 A$ = \"ABCD\"\n20 PRINT TAB(39);A$"));
    }

    [Fact]
    public void Un_entier_qui_ne_tient_pas_passe_a_la_ligne()
    {
        // « 12345 » fait 6 caractères avec son espace : colonnes 38 à 43
        Assert.Equal(Spaces(37) + "\r\n 12345 \r\n", Run("10 PRINT TAB(38);12345"));
    }

    [Fact]
    public void Un_nombre_a_virgule_qui_tient_juste_reste_sur_la_ligne()
    {
        Assert.Equal(Spaces(36) + " 1.5 \r\n", Run("10 PRINT TAB(37);1.5"));
        Assert.Equal(Spaces(37) + "\r\n 1.5 \r\n", Run("10 PRINT TAB(38);1.5"));
    }

    [Fact]
    public void En_colonne_1_un_texte_trop_long_n_ajoute_pas_de_ligne()
    {
        Assert.Equal(new string('A', 45) + "\r\n", Run("10 PRINT STRING$(45, \"A\")"));
    }

    [Fact]
    public void Apres_une_ligne_remplie_on_ne_saute_pas_de_ligne()
    {
        // 5 × « BONJOUR » remplissent les 40 colonnes : le 6e commence la ligne suivante, sans ligne vide
        Assert.Equal(string.Concat(Enumerable.Repeat("BONJOUR ", 6)),
                     Run("10 FOR I = 1 TO 6 : PRINT \"BONJOUR \"; : NEXT"));
    }

    // ---------- WRITE ----------

    [Fact]
    public void WRITE_textes_entre_guillemets_et_nombres_sans_espace()
    {
        Assert.Equal("\"BONJOUR\",12,-3,4.5\r\n", Run("10 WRITE \"BONJOUR\",12,-3,4.5"));
    }

    [Fact]
    public void WRITE_avec_des_variables_et_des_points_virgules()
    {
        Assert.Equal("\"PAUL\",7\r\n", Run("10 A$ = \"PAUL\":N = 7\n20 WRITE A$;N"));
    }

    [Fact]
    public void WRITE_seul_passe_a_la_ligne()
    {
        Assert.Equal("\r\n", Run("10 WRITE"));
    }

    [Fact]
    public void WRITE_dans_une_fenetre()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 WRITE #1, 5");
        Assert.Empty(analysis.Diagnostics);
        Assert.IsType<StreamStatement>(analysis.Program.Statements[0]);
    }

    // ---------- Ce qui ne change pas ----------

    [Fact]
    public void PRINT_simple_inchange()
    {
        Assert.Equal("A 5 B\r\n", Run("10 PRINT \"A\";5;\"B\""));
    }

    [Theory]
    [InlineData("10 ZONE = 5")]
    [InlineData("10 TAB = 5")]
    [InlineData("10 WRITE = 5")]
    public void Les_nouveaux_mots_ne_sont_pas_des_variables(string source)
    {
        Assert.NotEmpty(Errors(source));
    }
}
