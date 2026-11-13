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

/// <summary>GOTO, GOSUB, RETURN, STOP et ON ... GOTO / GOSUB : analyse, messages, octets produits et exécution (lot A, D054).</summary>
public class JumpTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static GeneratedProgram Generate(string source) => CodeGenerator.Generate(Parse(source), 0x4000);

    /// <summary>Lance le programme compilé sur le mini-Z80 et renvoie ce qu'il a affiché.</summary>
    static string Run(string source)
    {
        GeneratedProgram program = Generate(source);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;     // KM WAIT KEY : on rend la main tout de suite (RET)
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Fact]
    public void GOTO_et_GOSUB_gardent_le_numero_de_ligne_vise()
    {
        BasicProgram program = Parse("10 GOTO 30\n20 GOSUB 30\n30 RETURN");

        Assert.Equal(new GotoStatement(1, 30), program.Statements[0]);
        Assert.Equal(new GosubStatement(2, 30), program.Statements[1]);
        Assert.Equal(new ReturnStatement(3), program.Statements[2]);
    }

    [Fact]
    public void GO_TO_et_GO_SUB_s_ecrivent_aussi_en_deux_mots()
    {
        BasicProgram program = Parse("10 GO TO 30\n20 GO SUB 30\n30 RETURN");

        Assert.Equal(new GotoStatement(1, 30), program.Statements[0]);
        Assert.Equal(new GosubStatement(2, 30), program.Statements[1]);
    }

    [Fact]
    public void STOP_est_reconnu()
    {
        Assert.Equal(new StopStatement(1), Parse("10 STOP").Statements[0]);
    }

    [Fact]
    public void La_table_des_lignes_donne_le_rang_de_l_instruction_qui_suit()
    {
        BasicProgram program = Parse("10 PRINT \"A\"\n20 REM rien\n30 PRINT \"B\"\n40 REM fin");

        Assert.Equal(0, program.LineTable[10]);
        Assert.Equal(1, program.LineTable[20]);     // ligne sans instruction : vise la suivante
        Assert.Equal(1, program.LineTable[30]);
        Assert.Equal(2, program.LineTable[40]);     // après la dernière instruction
    }

    [Fact]
    public void ON_GOTO_garde_la_valeur_et_toutes_les_lignes()
    {
        BasicProgram program = Parse("10 ON X GOTO 10, 20, 30\n20 PRINT \"A\"\n30 PRINT \"B\"");

        var on = Assert.IsType<OnStatement>(program.Statements[0]);
        Assert.False(on.IsGosub);
        Assert.Equal(new[] { 10, 20, 30 }, on.Targets.ToArray());
    }

    [Fact]
    public void ON_GOSUB_est_reconnu_aussi_en_deux_mots()
    {
        BasicProgram program = Parse("10 ON X GO SUB 10, 20\n20 RETURN");

        var on = Assert.IsType<OnStatement>(program.Statements[0]);
        Assert.True(on.IsGosub);
        Assert.Equal(new[] { 10, 20 }, on.Targets.ToArray());
    }

    [Fact]
    public void Un_numero_de_ligne_apres_THEN_et_ELSE_veut_dire_GOTO()
    {
        BasicProgram program = Parse("10 IF X > 3 THEN 30 ELSE 40\n30 PRINT \"A\"\n40 PRINT \"B\"");

        Assert.IsType<IfStatement>(program.Statements[0]);
        Assert.Equal(new GotoStatement(1, 30), program.Statements[1]);
        Assert.IsType<ElseStatement>(program.Statements[2]);
        Assert.Equal(new GotoStatement(1, 40), program.Statements[3]);
        Assert.IsType<EndIfStatement>(program.Statements[4]);
    }

    [Fact]
    public void GOTO_apres_THEN_est_aussi_accepte()
    {
        BasicProgram program = Parse("10 IF X > 3 THEN GOTO 20\n20 END");

        Assert.Equal(new GotoStatement(1, 20), program.Statements[1]);
    }

    // ---------- Messages d'erreur ----------

    [Fact]
    public void Une_ligne_qui_n_existe_pas_est_signalee_avec_la_commande_et_le_numero()
    {
        Diagnostic error = Assert.Single(Errors("10 GOTO 500"));

        Assert.Equal(DiagnosticCode.LineNotFound, error.Code);
        Assert.Equal("ERREUR en ligne 1 : GOTO : la ligne 500 n'existe pas. Vérifiez le numéro de ligne.", error.Message);
    }

    [Fact]
    public void Chaque_ligne_manquante_d_un_ON_est_signalee()
    {
        IReadOnlyList<Diagnostic> errors = Errors("10 ON X GOSUB 100, 20, 200\n20 RETURN");

        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal(DiagnosticCode.LineNotFound, e.Code));
        Assert.Equal("ERREUR en ligne 1 : ON … GOSUB : la ligne 100 n'existe pas. Vérifiez le numéro de ligne.", errors[0].Message);
    }

    [Fact]
    public void Un_numero_de_ligne_utilise_deux_fois_est_signale()
    {
        Diagnostic error = Assert.Single(Errors("10 PRINT \"A\"\n10 PRINT \"B\""));

        Assert.Equal(DiagnosticCode.DuplicateLineNumber, error.Code);
        Assert.Equal("ERREUR en ligne 2 : le numéro de ligne 10 est déjà utilisé plus haut dans le programme.", error.Message);
    }

    [Theory]
    [InlineData("10 GOTO")]
    [InlineData("10 GOTO X")]
    [InlineData("10 GOSUB \"A\"")]
    public void GOTO_et_GOSUB_veulent_un_numero_de_ligne(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedLineNumber);
    }

    [Fact]
    public void Un_numero_de_ligne_trop_grand_est_refuse()
    {
        Assert.Contains(Errors("10 GOTO 70000"), e => e.Code == DiagnosticCode.NumberOutOfRange);
    }

    [Fact]
    public void ON_sans_GOTO_ni_GOSUB_est_refuse()
    {
        Assert.Contains(Errors("10 ON X PRINT 1"), e => e.Code == DiagnosticCode.ExpectedGoto);
    }

    [Theory]
    [InlineData("10 ON BREAK GOSUB 20\n20 RETURN", "ON BREAK")]
    [InlineData("10 ON ERROR GOTO 20\n20 END", "ON ERROR")]
    public void ON_BREAK_et_ON_ERROR_ne_sont_pas_encore_geres(string source, string name)
    {
        Diagnostic error = Assert.Single(Errors(source));

        Assert.Equal(DiagnosticCode.UnknownCommand, error.Code);
        Assert.Equal(name, error.Argument);
    }

    [Fact]
    public void Un_ON_utilise_ses_variables_comme_les_autres_instructions()
    {
        Assert.Contains(Errors("10 ON X GOTO 20\n20 BYTE X"), e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    // ---------- Octets produits ----------

    [Fact]
    public void GOTO_sur_la_premiere_ligne_saute_au_debut_du_programme()
    {
        byte[] code = Generate("10 GOTO 10").Code;

        // JP 0x4000
        Assert.Equal(new byte[] { 0xC3, 0x00, 0x40 }, code.Take(3).ToArray());
    }

    [Fact]
    public void GOSUB_appelle_la_ligne_et_RETURN_fait_RET()
    {
        byte[] code = Generate("10 GOSUB 30\n20 END\n30 RETURN").Code;

        // CALL 0x4006 / JP fin (0x4007) / RET
        Assert.Equal(new byte[] { 0xCD, 0x06, 0x40, 0xC3, 0x07, 0x40, 0xC9 }, code.Take(7).ToArray());
    }

    [Fact]
    public void STOP_saute_a_la_fin_du_programme_comme_END()
    {
        byte[] stop = Generate("10 STOP").Code;
        byte[] end = Generate("10 END").Code;

        Assert.Equal(end, stop);
    }

    [Fact]
    public void Les_sauts_ne_lient_aucun_module()
    {
        Assert.Empty(Generate("10 GOSUB 30\n20 END\n30 RETURN").Modules);
    }

    // ---------- Exécution sur le mini-Z80 ----------

    [Fact]
    public void GOTO_saute_par_dessus_des_instructions()
    {
        Assert.Equal("AC\r\n", Run("10 PRINT \"A\";\n20 GOTO 40\n30 PRINT \"B\";\n40 PRINT \"C\""));
    }

    [Fact]
    public void GOSUB_revient_apres_chaque_appel()
    {
        string output = Run("10 GOSUB 100\n20 GOSUB 100\n30 PRINT \"FIN\"\n40 END\n100 PRINT \"X\";\n110 RETURN");

        Assert.Equal("XXFIN\r\n", output);
    }

    [Fact]
    public void Les_GOSUB_peuvent_s_emboiter()
    {
        string output = Run("10 GOSUB 100\n20 PRINT \"Z\"\n30 END\n100 PRINT \"A\";\n110 GOSUB 200\n120 PRINT \"C\";\n130 RETURN\n200 PRINT \"B\";\n210 RETURN");

        Assert.Equal("ABCZ\r\n", output);
    }

    [Fact]
    public void STOP_arrete_le_programme()
    {
        Assert.Equal("A", Run("10 PRINT \"A\";\n20 STOP\n30 PRINT \"B\""));
    }

    [Theory]
    [InlineData(-1, "N")]
    [InlineData(0, "N")]
    [InlineData(1, "1")]
    [InlineData(2, "2")]
    [InlineData(3, "3")]
    [InlineData(4, "N")]
    [InlineData(32767, "N")]
    public void ON_GOTO_choisit_la_ligne_selon_la_valeur(int value, string expected)
    {
        string source = $"10 X = {value}\n20 ON X GOTO 100, 200, 300\n30 PRINT \"N\"\n40 END\n"
                      + "100 PRINT \"1\"\n110 END\n200 PRINT \"2\"\n210 END\n300 PRINT \"3\"\n310 END";

        Assert.Equal(expected + "\r\n", Run(source));
    }

    [Theory]
    [InlineData(0, "OK")]
    [InlineData(1, "1OK")]
    [InlineData(2, "2OK")]
    [InlineData(3, "OK")]
    public void ON_GOSUB_appelle_la_ligne_puis_continue_apres_le_ON(int value, string expected)
    {
        string source = $"10 X = {value}\n20 ON X GOSUB 100, 200\n30 PRINT \"OK\"\n40 END\n"
                      + "100 PRINT \"1\";\n110 RETURN\n200 PRINT \"2\";\n210 RETURN";

        Assert.Equal(expected + "\r\n", Run(source));
    }

    [Fact]
    public void ON_GOTO_avec_beaucoup_de_lignes_saute_bien_a_la_derniere()
    {
        // Plus de 9 lignes : le saut vers la fin du ON doit rester correct (les sauts relatifs ne suffiraient plus)
        var lines = new List<string> { "10 X = 12", "20 ON X GOSUB 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112", "30 PRINT \"OK\"", "40 END" };
        for (int i = 1; i <= 12; i++)
        {
            lines.Add($"{100 + i} PRINT \"{(char)('A' + i - 1)}\";");
            lines.Add($"{200 + i} RETURN");
        }

        Assert.Equal("LOK\r\n", Run(string.Join("\n", lines)));
    }
}
