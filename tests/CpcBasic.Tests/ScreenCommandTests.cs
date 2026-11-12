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

/// <summary>Commandes d'écran (D046) : MODE, CLS, LOCATE, PEN, PAPER, INK, BORDER.</summary>
public class ScreenCommandTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult result = BasicAnalyzer.Analyze(source);
        Assert.Empty(result.Diagnostics);
        return result.Program;
    }

    // Octets du programme compilé à l'adresse &4000
    static byte[] Code(string source) => CodeGenerator.Generate(Parse(source), 0x4000).Code;

    static void AssertStartsWith(byte[] code, params byte[] expected) =>
        Assert.Equal(expected, code.Take(expected.Length).ToArray());

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    // ----- Analyse -----

    [Fact]
    public void Les_sept_commandes_sont_reconnues()
    {
        BasicProgram program = Parse(
            "10 MODE 1\n20 CLS\n30 LOCATE 5, 3\n40 PEN 1\n50 PAPER 0\n60 INK 1, 24\n70 BORDER 0");

        var commands = program.Statements.OfType<ScreenStatement>().Select(s => s.Command).ToList();
        Assert.Equal(new[] { "MODE", "CLS", "LOCATE", "PEN", "PAPER", "INK", "BORDER" }, commands);
    }

    [Fact]
    public void Les_valeurs_sont_gardees_dans_l_ordre()
    {
        BasicProgram program = Parse("10 INK 1, 24, 6");

        var ink = Assert.IsType<ScreenStatement>(Assert.Single(program.Statements));
        Assert.Equal(new[] { 1, 24, 6 }, ink.Arguments.Cast<NumberExpression>().Select(n => n.Value).ToArray());
    }

    [Fact]
    public void Les_commandes_marchent_dans_un_IF_sur_une_ligne()
    {
        Assert.Empty(Errors("10 IF 1 = 1 THEN CLS ELSE MODE 1"));
    }

    [Fact]
    public void Les_valeurs_peuvent_etre_des_variables_et_des_calculs()
    {
        Assert.Empty(Errors("10 X = 5\n20 LOCATE X + 3, X * 2\n30 INK 1, X + 10"));
    }

    [Fact]
    public void Une_variable_utilisee_dans_une_commande_compte_comme_utilisee_avant_BYTE()
    {
        var errors = Errors("10 PEN X\n20 BYTE X");

        Assert.Contains(errors, e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    // ----- Erreurs -----

    [Fact]
    public void MODE_3_est_refuse_avec_un_message_clair()
    {
        Diagnostic error = Assert.Single(Errors("10 MODE 3"));

        Assert.Equal(DiagnosticCode.ValueOutOfRange, error.Code);
        Assert.Equal("ERREUR en ligne 1 : MODE : la valeur 3 n'est pas permise. Valeurs possibles : 0 à 2.", error.Message);
    }

    [Theory]
    [InlineData("10 PEN 16")]
    [InlineData("10 PAPER -1")]
    [InlineData("10 INK 16, 0")]
    [InlineData("10 INK 0, 27")]
    [InlineData("10 BORDER 27")]
    [InlineData("10 LOCATE 0, 1")]
    [InlineData("10 LOCATE 81, 1")]
    [InlineData("10 LOCATE 1, 26")]
    public void Les_valeurs_hors_limites_sont_refusees(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Theory]
    [InlineData("10 MODE")]
    [InlineData("10 CLS 5")]
    [InlineData("10 LOCATE 1")]
    [InlineData("10 INK 1")]
    [InlineData("10 INK 1, 2, 3, 4")]
    [InlineData("10 BORDER")]
    public void Un_mauvais_nombre_de_valeurs_est_refuse(string source)
    {
        Diagnostic error = Assert.Single(Errors(source));

        Assert.Equal(DiagnosticCode.WrongArgumentCount, error.Code);
        Assert.Contains("Écrivez", error.Message);
    }

    [Fact]
    public void MODE_egal_est_refuse_car_MODE_est_un_mot_reserve()
    {
        Assert.NotEmpty(Errors("10 MODE = 3"));
    }

    // ----- Octets Z80 -----

    [Fact]
    public void MODE_1_charge_A_et_appelle_SCR_SET_MODE()
    {
        // LD HL,1 / LD A,L / CALL &BC0E
        AssertStartsWith(Code("10 MODE 1"), 0x21, 0x01, 0x00, 0x7D, 0xCD, 0x0E, 0xBC);
    }

    [Fact]
    public void CLS_appelle_TXT_CLEAR_WINDOW()
    {
        AssertStartsWith(Code("10 CLS"), 0xCD, 0x6C, 0xBB);
    }

    [Fact]
    public void PEN_appelle_TXT_SET_PEN()
    {
        AssertStartsWith(Code("10 PEN 3"), 0x21, 0x03, 0x00, 0x7D, 0xCD, 0x90, 0xBB);
    }

    [Fact]
    public void PAPER_appelle_TXT_SET_PAPER()
    {
        AssertStartsWith(Code("10 PAPER 2"), 0x21, 0x02, 0x00, 0x7D, 0xCD, 0x96, 0xBB);
    }

    [Fact]
    public void LOCATE_met_la_colonne_dans_H_et_la_ligne_dans_L()
    {
        // LD HL,10 / PUSH HL / LD HL,5 / POP DE / LD H,E / CALL &BB75
        AssertStartsWith(Code("10 LOCATE 10, 5"),
            0x21, 0x0A, 0x00, 0xE5, 0x21, 0x05, 0x00, 0xD1, 0x63, 0xCD, 0x75, 0xBB);
    }

    [Fact]
    public void INK_a_deux_valeurs_repete_la_couleur()
    {
        // LD HL,0 / PUSH HL / LD HL,1 / LD A,L / LD B,A / LD C,A / POP HL / LD A,L / CALL &BC32
        AssertStartsWith(Code("10 INK 0, 1"),
            0x21, 0x00, 0x00, 0xE5, 0x21, 0x01, 0x00, 0x7D, 0x47, 0x4F, 0xE1, 0x7D, 0xCD, 0x32, 0xBC);
    }

    [Fact]
    public void INK_a_trois_valeurs_met_la_seconde_couleur_dans_C()
    {
        // LD HL,1 / PUSH / LD HL,24 / PUSH / LD HL,6 / LD A,L / LD C,A / POP HL / LD A,L / LD B,A / POP HL / LD A,L / CALL &BC32
        AssertStartsWith(Code("10 INK 1, 24, 6"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x18, 0x00, 0xE5, 0x21, 0x06, 0x00,
            0x7D, 0x4F, 0xE1, 0x7D, 0x47, 0xE1, 0x7D, 0xCD, 0x32, 0xBC);
    }

    [Fact]
    public void BORDER_a_une_valeur_repete_la_couleur()
    {
        // LD HL,0 / LD A,L / LD B,A / LD C,A / CALL &BC38
        AssertStartsWith(Code("10 BORDER 0"), 0x21, 0x00, 0x00, 0x7D, 0x47, 0x4F, 0xCD, 0x38, 0xBC);
    }

    [Fact]
    public void BORDER_a_deux_valeurs_met_la_seconde_dans_C()
    {
        // LD HL,1 / PUSH HL / LD HL,2 / LD A,L / LD C,A / POP HL / LD A,L / LD B,A / CALL &BC38
        AssertStartsWith(Code("10 BORDER 1, 2"),
            0x21, 0x01, 0x00, 0xE5, 0x21, 0x02, 0x00, 0x7D, 0x4F, 0xE1, 0x7D, 0x47, 0xCD, 0x38, 0xBC);
    }

    [Fact]
    public void Un_programme_d_ecran_sans_texte_ne_lie_aucun_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 MODE 1\n20 INK 0, 0\n30 CLS"), 0x4000);

        Assert.Empty(program.Modules);
    }

    [Fact]
    public void LdHE_est_l_instruction_LD_H_E()
    {
        var emitter = new Z80Emitter(0x4000);
        emitter.LdHE();

        Assert.Equal(new byte[] { 0x63 }, emitter.ToBytes());
    }
}
