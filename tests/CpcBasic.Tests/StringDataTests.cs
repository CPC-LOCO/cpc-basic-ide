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

/// <summary>Lot E, étape E3a (D064) : textes dans DATA et READ A$, INKEY$, LINE INPUT, @A$.</summary>
public class StringDataTests
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
    public void LINE_INPUT_avec_un_texte()
    {
        var input = Assert.IsType<LineInputStatement>(Parse("10 LINE INPUT \"NOM\"; A$").Statements[0]);

        Assert.Equal("NOM", input.Prompt);
        Assert.Equal("A$", input.Variable);
    }

    [Fact]
    public void LINE_INPUT_range_un_texte()
    {
        Assert.Contains(Errors("10 LINE INPUT A"), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void INKEY_dollar_donne_un_texte()
    {
        Assert.Empty(Errors("10 A$ = INKEY$"));
        Assert.Contains(Errors("10 A = INKEY$"), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    // ---------- Exécution ----------

    [Fact]
    public void READ_A_dollar_lit_textes_et_nombres_tels_qu_ils_sont_ecrits()
    {
        Assert.Equal("BONJOUR/A, B/012/ 7 \r\n",
                     Run("10 READ A$, B$, C$, D\n20 PRINT A$; \"/\"; B$; \"/\"; C$; \"/\"; D\n30 DATA BONJOUR, \"A, B\", 012, 7"));
    }

    [Fact]
    public void READ_d_un_nombre_sur_un_texte_donne_Type_mismatch()
    {
        Assert.Equal("Type mismatch in 10\r\n", Run("10 READ A\n20 DATA ABC"));
    }

    [Fact]
    public void Un_nombre_entre_guillemets_se_lit_comme_un_nombre()
    {
        Assert.Equal(" 42 \r\n", Run("10 READ A : PRINT A\n20 DATA \"42\""));
    }

    [Fact]
    public void Un_element_vide_vaut_0_ou_un_texte_vide()
    {
        Assert.Equal(" 0 [] 5 \r\n", Run("10 READ A, B$, C : PRINT A; \"[\"; B$; \"]\"; C\n20 DATA , , 5"));
    }

    [Fact]
    public void RESTORE_avec_des_textes()
    {
        Assert.Equal("XYXY\r\n", Run("10 READ A$ : RESTORE : READ B$ : PRINT A$; B$\n20 DATA XY"));
    }

    [Fact]
    public void Un_nombre_hexadecimal_lu_comme_nombre_et_comme_texte()
    {
        Assert.Equal(" 255 &FF\r\n", Run("10 READ A, B$ : PRINT A; B$\n20 DATA &FF, &FF"));
    }

    [Fact]
    public void Beaucoup_de_READ_A_dollar_ne_remplissent_pas_les_fiches_temporaires()
    {
        Assert.Equal("OK\r\n", Run("10 FOR I = 1 TO 40 : RESTORE : READ A$ : NEXT : PRINT A$\n20 DATA OK"));
    }

    [Fact]
    public void Le_chemin_du_programme_RSX_READ_VAL_MID_POKE()
    {
        string source = "10 READ A$ : FOR A = 0 TO 2 : POKE &9000 + A, VAL(\"&\" + MID$(A$, A * 2 + 1, 2)) : NEXT\n" +
                        "20 PRINT PEEK(&9000); PEEK(&9001); PEEK(&9002)\n" +
                        "30 DATA \"3E2AC9\"";

        Assert.Equal(" 62  42  201 \r\n", Run(source));
    }

    [Fact]
    public void INKEY_dollar_avec_une_touche()
    {
        Assert.Equal(" 1 Z\r\n", Run("10 A$ = INKEY$ : PRINT LEN(A$); A$", "Z"));
    }

    [Fact]
    public void INKEY_dollar_sans_touche()
    {
        Assert.Equal(" 0 \r\n", Run("10 A$ = INKEY$ : PRINT LEN(A$); A$"));
    }

    [Fact]
    public void LINE_INPUT_garde_toute_la_ligne()
    {
        Assert.Equal("NOM   A, B\"C\r\n[  A, B\"C]\r\n",
                     Run("10 LINE INPUT \"NOM \"; A$\n20 PRINT \"[\"; A$; \"]\"", "  A, B\"C\r"));
    }

    [Fact]
    public void LINE_INPUT_ligne_vide()
    {
        Assert.Equal("\r\n[]\r\n", Run("10 LINE INPUT A$\n20 PRINT \"[\"; A$; \"]\"", "\r"));
    }

    [Fact]
    public void Arobase_A_dollar_donne_la_fiche_du_texte_comme_le_Locomotive()
    {
        Assert.Equal(" 3 X\r\n",
                     Run("10 A$ = \"XYZ\"\n20 P = @A$\n30 PRINT PEEK(P); CHR$(PEEK(PEEK(P + 1) + 256 * PEEK(P + 2)))"));
    }
}
