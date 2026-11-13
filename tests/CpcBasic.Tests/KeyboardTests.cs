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

/// <summary>INKEY, JOY et CLEAR INPUT : analyse, messages et exécution sur le mini-Z80 (lot B, D055).</summary>
public class KeyboardTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    /// <summary>Lance le programme sur le mini-Z80 préparé par <paramref name="setup"/> et renvoie ce qu'il a affiché.</summary>
    static string Run(string source, System.Action<MiniZ80>? setup = null)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        setup?.Invoke(cpu);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Fact]
    public void INKEY_et_JOY_sont_des_fonctions_a_une_valeur()
    {
        BasicProgram program = Parse("10 A = INKEY(47)\n20 B = JOY(1)");

        Assert.Equal(2, program.Statements.Count);
    }

    [Theory]
    [InlineData("10 A = INKEY")]
    [InlineData("10 A = INKEY()")]
    [InlineData("10 A = JOY(1, 2)")]
    public void INKEY_et_JOY_veulent_une_seule_valeur_entre_parentheses(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Fact]
    public void CLEAR_INPUT_est_reconnu()
    {
        Assert.Equal(new ClearInputStatement(1), Parse("10 CLEAR INPUT").Statements[0]);
    }

    [Fact]
    public void CLEAR_seul_est_refuse()
    {
        Diagnostic error = Assert.Single(Errors("10 CLEAR"));

        Assert.Equal(DiagnosticCode.UnknownCommand, error.Code);
    }

    [Fact]
    public void Une_variable_utilisee_dans_INKEY_compte_comme_utilisee_avant_BYTE()
    {
        Assert.Contains(Errors("10 A = INKEY(X)\n20 BYTE X"), e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    // ---------- Exécution ----------

    [Theory]
    [InlineData(0, 0, "-1")]       // touche non appuyée
    [InlineData(1, 0, "0")]        // touche appuyée seule
    [InlineData(1, 0x20, "32")]    // avec Shift
    [InlineData(1, 0x80, "128")]   // avec Ctrl
    [InlineData(1, 0xA0, "160")]   // avec les deux
    public void INKEY_donne_moins_un_ou_l_etat_de_Shift_et_Ctrl(int pressed, int state, string expected)
    {
        string output = Run("10 A = INKEY(47)\n20 PRINT A", cpu =>
        {
            if (pressed == 1)
                cpu.Keys[47] = (byte)(state | 0x1F);   // les autres bits ne doivent pas compter
        });

        // PRINT d'un nombre : espace devant s'il est positif (« - » s'il est négatif), puis un espace
        string shown = expected.StartsWith('-') ? expected : " " + expected;
        Assert.Equal(shown + " \r\n", output);
    }

    [Fact]
    public void INKEY_ne_regarde_que_la_touche_demandee()
    {
        string output = Run("10 PRINT INKEY(10)\n20 PRINT INKEY(11)", cpu => cpu.Keys[11] = 0);

        Assert.Equal("-1 \r\n 0 \r\n", output);
    }

    [Theory]
    [InlineData(0, 0x25, 0x0A, " 37 \r\n")]   // manette 0 : haut + gauche + tir 1
    [InlineData(1, 0x25, 0x0A, " 10 \r\n")]   // manette 1 : bas + droite
    [InlineData(0, 0, 0, " 0 \r\n")]
    public void JOY_donne_l_etat_de_la_manette_demandee(int number, int joy0, int joy1, string expected)
    {
        string output = Run($"10 PRINT JOY({number})", cpu =>
        {
            cpu.Joystick0 = (byte)joy0;
            cpu.Joystick1 = (byte)joy1;
        });

        Assert.Equal(expected, output);
    }

    [Fact]
    public void CLEAR_INPUT_vide_les_touches_en_attente()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 CLEAR INPUT"), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (byte key in new byte[] { 65, 66, 67 })
            cpu.KeyQueue.Enqueue(key);

        cpu.Call(0x4000);

        Assert.Empty(cpu.KeyQueue);
    }

    [Fact]
    public void CLEAR_INPUT_ne_gene_pas_quand_il_n_y_a_rien_a_vider()
    {
        Assert.Equal("OK\r\n", Run("10 CLEAR INPUT\n20 PRINT \"OK\""));
    }
}
