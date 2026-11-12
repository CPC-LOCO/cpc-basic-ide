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

/// <summary>Tracé du Locomotive BASIC (D047) : MOVE, MOVER, PLOT, PLOTR, DRAW, DRAWR, CLG, ORIGIN.</summary>
public class DrawCommandTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult result = BasicAnalyzer.Analyze(source);
        Assert.Empty(result.Diagnostics);
        return result.Program;
    }

    static byte[] Code(string source) => CodeGenerator.Generate(Parse(source), 0x4000).Code;

    static void AssertStartsWith(byte[] code, params byte[] expected) =>
        Assert.Equal(expected, code.Take(expected.Length).ToArray());

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    // ----- Analyse -----

    [Fact]
    public void Les_six_commandes_de_trace_sont_reconnues()
    {
        BasicProgram program = Parse("10 MOVE 1, 2\n20 MOVER 1, 2\n30 PLOT 1, 2\n40 PLOTR 1, 2\n50 DRAW 1, 2\n60 DRAWR 1, 2");

        var commands = program.Statements.OfType<DrawStatement>().Select(s => s.Command).ToList();
        Assert.Equal(new[] { "MOVE", "MOVER", "PLOT", "PLOTR", "DRAW", "DRAWR" }, commands);
    }

    [Fact]
    public void Le_stylo_et_le_mode_sont_facultatifs()
    {
        BasicProgram program = Parse("10 PLOT 1, 2\n20 PLOT 1, 2, 3\n30 PLOT 1, 2, 3, 1\n40 PLOT 1, 2, , 1");

        var plots = program.Statements.OfType<DrawStatement>().ToList();
        Assert.Null(plots[0].Pen);
        Assert.Null(plots[0].Mode);
        Assert.NotNull(plots[1].Pen);
        Assert.Null(plots[1].Mode);
        Assert.NotNull(plots[2].Pen);
        Assert.NotNull(plots[2].Mode);
        Assert.Null(plots[3].Pen);
        Assert.NotNull(plots[3].Mode);
    }

    [Fact]
    public void CLG_et_ORIGIN_sont_reconnus()
    {
        BasicProgram program = Parse("10 CLG\n20 CLG 2\n30 ORIGIN 320, 200");

        var commands = program.Statements.OfType<ScreenStatement>().Select(s => s.Command).ToList();
        Assert.Equal(new[] { "CLG", "CLG", "ORIGIN" }, commands);
    }

    [Fact]
    public void Les_valeurs_peuvent_etre_negatives_variables_ou_calculees()
    {
        Assert.Empty(Errors("10 X = 5\n20 ORIGIN 320, 200\n30 MOVE -100, -X\n40 DRAWR X * 2, X + 1, X"));
    }

    [Fact]
    public void Une_variable_utilisee_dans_un_trace_compte_comme_utilisee_avant_BYTE()
    {
        Assert.Contains(Errors("10 PLOT X, 5\n20 BYTE X"), e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    // ----- Erreurs -----

    [Theory]
    [InlineData("10 PLOT 5")]
    [InlineData("10 DRAW 5 6")]
    [InlineData("10 CLS 5")]
    [InlineData("10 ORIGIN 5")]
    [InlineData("10 CLG 1, 2")]
    public void Un_mauvais_nombre_de_valeurs_est_refuse(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Fact]
    public void Un_stylo_hors_limites_est_refuse_avec_un_message_clair()
    {
        Diagnostic error = Assert.Single(Errors("10 DRAW 1, 2, 16"));

        Assert.Equal("ERREUR en ligne 1 : DRAW : la valeur 16 n'est pas permise. Valeurs possibles : 0 à 15.", error.Message);
    }

    [Theory]
    [InlineData("10 PLOT 1, 2, 3, 4")]
    [InlineData("10 PLOT 1, 2, , -1")]
    [InlineData("10 CLG 16")]
    public void Un_mode_ou_un_stylo_hors_limites_est_refuse(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Fact]
    public void Une_virgule_de_fin_est_refusee()
    {
        Assert.NotEmpty(Errors("10 PLOT 1, 2,"));
    }

    // ----- Octets Z80 -----

    [Fact]
    public void MOVE_met_x_dans_DE_et_y_dans_HL()
    {
        // LD HL,10 / PUSH HL / LD HL,20 / PUSH HL / POP HL / POP DE / CALL &BBC0
        AssertStartsWith(Code("10 MOVE 10, 20"),
            0x21, 0x0A, 0x00, 0xE5, 0x21, 0x14, 0x00, 0xE5, 0xE1, 0xD1, 0xCD, 0xC0, 0xBB);
    }

    [Theory]
    [InlineData("MOVER", 0xC3, 0xBB)]
    [InlineData("PLOT", 0xEA, 0xBB)]
    [InlineData("PLOTR", 0xED, 0xBB)]
    [InlineData("DRAW", 0xF6, 0xBB)]
    [InlineData("DRAWR", 0xF9, 0xBB)]
    public void Chaque_commande_appelle_sa_routine_du_firmware(string command, byte low, byte high)
    {
        AssertStartsWith(Code($"10 {command} 1, 2"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x02, 0x00, 0xE5, 0xE1, 0xD1, 0xCD, low, high);
    }

    [Fact]
    public void Le_stylo_est_regle_avant_le_trace()
    {
        // ... PUSH HL / LD HL,3 / LD A,L / CALL &BBDE (GRA SET PEN) / POP HL / POP DE / CALL &BBEA
        AssertStartsWith(Code("10 PLOT 1, 2, 3"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x02, 0x00, 0xE5,
            0x21, 0x03, 0x00, 0x7D, 0xCD, 0xDE, 0xBB,
            0xE1, 0xD1, 0xCD, 0xEA, 0xBB);
    }

    [Fact]
    public void Le_mode_est_regle_par_SCR_ACCESS_et_le_stylo_peut_rester_vide()
    {
        // ... LD HL,1 / LD A,L / CALL &BC59 (SCR ACCESS) / POP HL / POP DE / CALL &BBF6
        AssertStartsWith(Code("10 DRAW 1, 2, , 1"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x02, 0x00, 0xE5,
            0x21, 0x01, 0x00, 0x7D, 0xCD, 0x59, 0xBC,
            0xE1, 0xD1, 0xCD, 0xF6, 0xBB);
    }

    [Fact]
    public void Le_stylo_puis_le_mode_sont_regles_dans_cet_ordre()
    {
        AssertStartsWith(Code("10 DRAW 1, 2, 3, 1"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x02, 0x00, 0xE5,
            0x21, 0x03, 0x00, 0x7D, 0xCD, 0xDE, 0xBB,
            0x21, 0x01, 0x00, 0x7D, 0xCD, 0x59, 0xBC,
            0xE1, 0xD1, 0xCD, 0xF6, 0xBB);
    }

    [Fact]
    public void CLG_efface_la_fenetre_graphique()
    {
        AssertStartsWith(Code("10 CLG"), 0xCD, 0xDB, 0xBB);
    }

    [Fact]
    public void CLG_avec_un_stylo_regle_d_abord_le_fond_graphique()
    {
        // LD HL,2 / LD A,L / CALL &BBE4 (GRA SET PAPER) / CALL &BBDB (GRA CLEAR WINDOW)
        AssertStartsWith(Code("10 CLG 2"), 0x21, 0x02, 0x00, 0x7D, 0xCD, 0xE4, 0xBB, 0xCD, 0xDB, 0xBB);
    }

    [Fact]
    public void ORIGIN_met_x_dans_DE_et_y_dans_HL()
    {
        // LD HL,320 / PUSH HL / LD HL,200 / POP DE / CALL &BBC9
        AssertStartsWith(Code("10 ORIGIN 320, 200"),
            0x21, 0x40, 0x01, 0xE5, 0x21, 0xC8, 0x00, 0xD1, 0xCD, 0xC9, 0xBB);
    }

    [Fact]
    public void Un_programme_de_trace_sans_texte_ne_lie_aucun_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 CLG\n20 MOVE 0, 0\n30 DRAW 639, 399, 1"), 0x4000);

        Assert.Empty(program.Modules);
    }
}
