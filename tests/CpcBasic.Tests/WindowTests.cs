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

/// <summary>Lot H1 (D072) : WINDOW, WINDOW SWAP, numéro de fenêtre #n, POS et VPOS.</summary>
public class WindowTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static MiniZ80 Run(string source, string typed = "")
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        foreach (int routine in new[] { 0xBB18, 0xBB6C, 0xBB75, 0xBB90, 0xBB96 })
            cpu.Memory[routine] = 0xC9;            // routines du firmware sans effet ici : simple retour
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu;
    }

    // ---------- Analyse ----------

    [Fact]
    public void PRINT_dans_une_fenetre_est_entoure_du_choix_de_la_fenetre()
    {
        var statements = Parse("10 PRINT #1, \"A\"").Statements;

        Assert.Equal(3, statements.Count);
        var select = Assert.IsType<StreamStatement>(statements[0]);
        Assert.Equal(1, Assert.IsType<NumberExpression>(select.Stream).Value);
        Assert.IsType<PrintStatement>(statements[1]);
        Assert.Null(Assert.IsType<StreamStatement>(statements[2]).Stream);
    }

    [Theory]
    [InlineData("10 LOCATE #1, 3, 5")]
    [InlineData("10 CLS #2")]
    [InlineData("10 PEN #1, 3")]
    [InlineData("10 PAPER #7, 2")]
    [InlineData("10 WINDOW #1, 2, 19, 3, 10")]
    public void Les_commandes_d_ecran_acceptent_un_numero_de_fenetre(string source)
    {
        var statements = Parse(source).Statements;

        Assert.Equal(3, statements.Count);
        Assert.IsType<StreamStatement>(statements[0]);
        Assert.IsType<ScreenStatement>(statements[1]);
        Assert.IsType<StreamStatement>(statements[2]);
    }

    [Theory]
    [InlineData("10 INPUT #1, \"NOM\"; A$")]
    [InlineData("10 INPUT #1, A")]
    [InlineData("10 LINE INPUT #2, \"NOM\"; A$")]
    [InlineData("10 PRINT #1")]
    [InlineData("10 IF 1 THEN PRINT #1, \"A\" ELSE PRINT #2, \"B\"")]
    [InlineData("10 N = 3: PRINT #N, \"A\"; POS(#N); VPOS(#0)")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }

    [Theory]
    [InlineData("10 PRINT #8, \"A\"")]
    [InlineData("10 CLS #10")]
    [InlineData("10 A = POS(#9)")]
    [InlineData("10 WINDOW 0, 10, 1, 5")]
    [InlineData("10 WINDOW SWAP 8")]
    public void Valeurs_refusees(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Theory]
    [InlineData("10 PRINT #1 \"A\"")]
    [InlineData("10 LOCATE #1")]
    [InlineData("10 INPUT #1 A")]
    public void Sans_virgule_apres_le_numero_de_fenetre(string source)
    {
        Diagnostic error = Assert.Single(Errors(source));
        Assert.Equal(DiagnosticCode.ExpectedStreamComma, error.Code);
        Assert.Contains("virgule", error.Message);
    }

    [Fact]
    public void POS_sans_diese_est_refuse_avec_un_exemple()
    {
        Diagnostic error = Assert.Single(Errors("10 A = POS(0)"));
        Assert.Equal(DiagnosticCode.ExpectedStream, error.Code);
        Assert.Contains("POS(#0)", error.Message);
    }

    [Fact]
    public void WINDOW_et_SWAP_ne_peuvent_pas_etre_des_variables()
    {
        Assert.NotEmpty(Errors("10 WINDOW = 5"));
        Assert.NotEmpty(Errors("10 SWAP = 5"));
    }

    [Fact]
    public void Un_programme_sans_diese_ne_change_pas()
    {
        byte[] code = CodeGenerator.Generate(Parse("10 PRINT \"A\"\n20 CLS\n30 LOCATE 1, 2"), 0x4000).Code;

        // aucun appel à TXT STR SELECT (CALL &BBB4)
        for (int i = 0; i + 2 < code.Length; i++)
            Assert.False(code[i] == 0xCD && code[i + 1] == 0xB4 && code[i + 2] == 0xBB);
    }

    // ---------- Exécution ----------

    [Fact]
    public void PRINT_ecrit_dans_la_fenetre_puis_revient_a_la_fenetre_0()
    {
        MiniZ80 cpu = Run("10 PRINT #1, \"UN\"\n20 PRINT \"ZERO\"");

        Assert.Equal("UN", cpu.StreamText(1).TrimEnd());
        Assert.Equal("ZERO", cpu.StreamText(0).TrimEnd());
        Assert.Equal(new byte[] { 1, 0 }, cpu.StreamSelects);
    }

    [Fact]
    public void Numero_de_fenetre_calcule()
    {
        MiniZ80 cpu = Run("10 N = 1\n20 PRINT #N + 1, \"DEUX\"");

        Assert.Equal("DEUX", cpu.StreamText(2).TrimEnd());
        Assert.Equal(0, cpu.CurrentStream);
    }

    [Fact]
    public void Numero_de_fenetre_calcule_trop_grand_Improper_argument()
    {
        MiniZ80 cpu = Run("10 N = 8\n20 PRINT #N, \"A\"");

        Assert.Contains("Improper argument in 20", cpu.Output.ToString());
        Assert.DoesNotContain("A\r", cpu.Output.ToString());
    }

    [Fact]
    public void Une_erreur_pendant_l_execution_s_affiche_dans_la_fenetre_0()
    {
        MiniZ80 cpu = Run("10 PRINT #3, T(20)");

        Assert.Contains("Subscript out of range", cpu.StreamText(0));
        Assert.DoesNotContain("Subscript", cpu.StreamText(3));
    }

    [Fact]
    public void WINDOW_donne_au_firmware_les_bords_a_partir_de_0()
    {
        MiniZ80 cpu = Run("10 WINDOW #1, 2, 19, 3, 10\n20 WINDOW 30, 21, 12, 20");

        Assert.Equal(new[] { ((byte)1, (byte)1, (byte)18, (byte)2, (byte)9), ((byte)0, (byte)29, (byte)20, (byte)11, (byte)19) },
                     cpu.Windows);
    }

    [Fact]
    public void WINDOW_avec_des_valeurs_calculees()
    {
        MiniZ80 cpu = Run("10 G = 5\n20 WINDOW #2, G, G * 2, G - 3, G + 1");

        Assert.Equal(((byte)2, (byte)4, (byte)9, (byte)1, (byte)5), cpu.Windows.Single());
    }

    [Fact]
    public void WINDOW_SWAP_avec_une_ou_deux_fenetres()
    {
        MiniZ80 cpu = Run("10 WINDOW SWAP 1, 2\n20 WINDOW SWAP 3");

        Assert.Equal(new[] { ((byte)1, (byte)2), ((byte)3, (byte)0) }, cpu.Swaps);
    }

    [Fact]
    public void POS_et_VPOS_de_la_fenetre()
    {
        MiniZ80 cpu = Run("10 PRINT #1, \"ABC\";\n20 PRINT \"X\"\n30 PRINT \"Y\";\n40 PRINT POS(#1); VPOS(#0)");

        Assert.EndsWith(" 4  2", cpu.StreamText(0).TrimEnd());
    }

    [Fact]
    public void POS_garde_la_fenetre_de_la_commande()
    {
        // POS(#1) choisit la fenêtre 1 le temps de la lire, puis revient à la fenêtre 2 du PRINT
        MiniZ80 cpu = Run("10 PRINT #2, \"AB\"; POS(#1)");

        Assert.Equal("AB 1", cpu.StreamText(2).TrimEnd());
        Assert.Equal("", cpu.StreamText(1));
    }

    [Fact]
    public void INPUT_dans_une_fenetre()
    {
        MiniZ80 cpu = Run("10 INPUT #1, \"NOM\"; A$\n20 PRINT A$", "PAUL\r");

        Assert.StartsWith("NOM? PAUL", cpu.StreamText(1));
        Assert.Equal("PAUL", cpu.StreamText(0).TrimEnd());
    }

    [Fact]
    public void IF_avec_des_fenetres()
    {
        MiniZ80 cpu = Run("10 IF 1 THEN PRINT #1, \"OUI\" ELSE PRINT #2, \"NON\"");

        Assert.Equal("OUI", cpu.StreamText(1).TrimEnd());
        Assert.Equal("", cpu.StreamText(2));
        Assert.Equal(0, cpu.CurrentStream);
    }
}
