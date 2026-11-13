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

/// <summary>POKE, PEEK, CALL, OUT et INP : accès direct à la machine (lot C, D056).</summary>
public class MachineTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static MiniZ80 Run(string source, System.Action<MiniZ80>? setup = null)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        setup?.Invoke(cpu);
        cpu.Call(0x4000);
        return cpu;
    }

    // ---------- Analyse ----------

    [Fact]
    public void POKE_OUT_et_CALL_sont_des_commandes_PEEK_et_INP_des_fonctions()
    {
        BasicProgram program = Parse("10 POKE 1, 2\n20 OUT 3, 4\n30 CALL 5\n40 A = PEEK(1) + INP(2)");

        Assert.Equal(4, program.Statements.Count);
        Assert.Equal(new[] { "POKE", "OUT", "CALL" },
                     program.Statements.OfType<ScreenStatement>().Select(s => s.Command).ToArray());
    }

    [Fact]
    public void Une_adresse_de_32768_a_65535_vaut_le_meme_mot_de_16_bits_que_sur_le_CPC()
    {
        var poke = Assert.IsType<ScreenStatement>(Parse("10 POKE 49152, 1").Statements[0]);

        Assert.Equal(new NumberExpression(-16384), poke.Arguments[0]);
    }

    [Fact]
    public void Une_adresse_au_dessus_de_65535_est_refusee()
    {
        Assert.Contains(Errors("10 POKE 65536, 1"), e => e.Code == DiagnosticCode.NumberOutOfRange);
    }

    [Theory]
    [InlineData("10 POKE 1, 256")]
    [InlineData("10 POKE 1, -1")]
    [InlineData("10 OUT 1, 300")]
    public void La_valeur_ecrite_doit_tenir_sur_un_octet(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    [Theory]
    [InlineData("10 POKE 1")]
    [InlineData("10 CALL")]
    [InlineData("10 CALL 1, 2")]
    [InlineData("10 OUT 1")]
    public void Le_mauvais_nombre_de_valeurs_est_signale(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Fact]
    public void PEEK_veut_une_seule_valeur_entre_parentheses()
    {
        Assert.NotEmpty(Errors("10 A = PEEK"));
        Assert.NotEmpty(Errors("10 A = PEEK(1, 2)"));
    }

    // ---------- Exécution ----------

    [Fact]
    public void POKE_ecrit_un_octet_et_PEEK_le_relit()
    {
        MiniZ80 cpu = Run("10 POKE 20000, 77\n20 PRINT PEEK(20000)");

        Assert.Equal(77, cpu.Memory[20000]);
        Assert.Equal(" 77 \r\n", cpu.Output.ToString());
    }

    [Fact]
    public void Les_adresses_au_dessus_de_32767_marchent_comme_sur_le_CPC()
    {
        MiniZ80 cpu = Run("10 POKE 40000, 5\n20 PRINT PEEK(40000)");

        Assert.Equal(5, cpu.Memory[40000]);
        Assert.Equal(" 5 \r\n", cpu.Output.ToString());
    }

    [Fact]
    public void Une_adresse_negative_designe_le_haut_de_la_memoire()
    {
        MiniZ80 cpu = Run("10 POKE -1, 9\n20 PRINT PEEK(65535)");

        Assert.Equal(9, cpu.Memory[0xFFFF]);
        Assert.Equal(" 9 \r\n", cpu.Output.ToString());
    }

    [Fact]
    public void PEEK_lit_la_memoire_et_pas_la_ROM_haute_a_partir_de_C000_puis_remet_la_ROM()
    {
        // Le mini-Z80 montre la ROM haute à partir de &C000 tant qu'on ne la coupe pas (comme un vrai CPC)
        MiniZ80 cpu = Run("10 POKE 49152, 33\n20 PRINT PEEK(49152)");

        Assert.Equal(" 33 \r\n", cpu.Output.ToString());
        Assert.True(cpu.UpperRomEnabled);
    }

    [Fact]
    public void L_adresse_et_la_valeur_peuvent_etre_calculees()
    {
        MiniZ80 cpu = Run("10 A = 100\n20 POKE A * 2 + 1, A + 5\n30 PRINT PEEK(201)");

        Assert.Equal(105, cpu.Memory[201]);
    }

    [Fact]
    public void POKE_n_ecrit_que_l_octet_de_poids_faible_d_une_valeur_calculee()
    {
        MiniZ80 cpu = Run("10 A = 300\n20 POKE 20000, A");

        Assert.Equal(300 & 0xFF, cpu.Memory[20000]);
    }

    [Fact]
    public void CALL_lance_un_sous_programme_en_langage_machine_puis_le_programme_continue()
    {
        // LD A,'Z' / CALL &BB5A / RET, rangé en 20480 (&5000)
        byte[] routine = [0x3E, 0x5A, 0xCD, 0x5A, 0xBB, 0xC9];

        MiniZ80 cpu = Run("10 CALL 20480\n20 PRINT \"A\"", c => routine.CopyTo(c.Memory, 0x5000));

        Assert.Equal("ZA\r\n", cpu.Output.ToString());
    }

    [Fact]
    public void CALL_accepte_une_adresse_au_dessus_de_32767()
    {
        byte[] routine = [0x3E, 0x59, 0xCD, 0x5A, 0xBB, 0xC9];

        MiniZ80 cpu = Run("10 CALL 40000", c => routine.CopyTo(c.Memory, 40000));

        Assert.Equal("Y", cpu.Output.ToString());
    }

    [Fact]
    public void INP_lit_un_port()
    {
        // 62720 = &F500
        MiniZ80 cpu = Run("10 PRINT INP(62720)", c => c.Ports[0xF500] = 85);

        Assert.Equal(" 85 \r\n", cpu.Output.ToString());
    }

    [Fact]
    public void OUT_ecrit_sur_un_port()
    {
        // 32512 = &7F00
        MiniZ80 cpu = Run("10 OUT 32512, 200");

        var write = Assert.Single(cpu.PortWrites);
        Assert.Equal(0x7F00, write.Port);
        Assert.Equal(200, write.Value);
    }
}
