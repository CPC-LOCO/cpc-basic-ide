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

/// <summary>Lot G1 (D071) : SOUND, ENV, ENT, RELEASE et SQ, par les routines sonores de la ROM.</summary>
public class SoundTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static MiniZ80 Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    // ---------- SOUND ----------

    [Fact]
    public void SOUND_avec_les_valeurs_par_defaut()
    {
        // canal, env. volume, env. ton, période (2 octets), bruit, volume, durée (2 octets)
        Assert.Equal(new byte[] { 1, 0, 0, 200, 0, 0, 12, 20, 0 }, Run("10 SOUND 1, 200").Sounds.Single());
    }

    [Fact]
    public void SOUND_avec_toutes_les_valeurs()
    {
        Assert.Equal(new byte[] { 1, 1, 2, 0xDE, 0x01, 3, 15, 100, 0 }, Run("10 SOUND 1, 478, 100, 15, 1, 2, 3").Sounds.Single());
    }

    [Fact]
    public void SOUND_avec_des_valeurs_vides_au_milieu()
    {
        Assert.Equal(new byte[] { 2, 0, 0, 100, 0, 9, 5, 20, 0 }, Run("10 SOUND 2, 100, , 5, , , 9").Sounds.Single());
    }

    [Fact]
    public void SOUND_avec_une_duree_negative_et_des_calculs()
    {
        Assert.Equal(new byte[] { 4, 0, 0, 0x2C, 0x01, 0, 12, 0xFE, 0xFF }, Run("10 P = 150\n20 SOUND 4, P * 2, -2").Sounds.Single());
    }

    [Theory]
    [InlineData("10 SOUND 1")]
    [InlineData("10 SOUND")]
    [InlineData("10 SOUND , 100")]
    [InlineData("10 SOUND 1, 2, 3, 4, 5, 6, 7, 8")]
    public void SOUND_sans_canal_ou_periode_ou_avec_trop_de_valeurs(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Theory]
    [InlineData("10 SOUND 0, 100")]
    [InlineData("10 SOUND 1, 5000")]
    [InlineData("10 SOUND 1, 100, 20, 16")]
    [InlineData("10 SOUND 1, 100, 20, 12, 0, 0, 32")]
    public void SOUND_avec_une_valeur_impossible(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    // ---------- ENV et ENT ----------

    [Fact]
    public void ENV_range_ses_parties()
    {
        var (tone, number, block) = Run("10 ENV 1, 10, 1, 10, 10, -1, 10").Envelopes.Single();
        Assert.False(tone);
        Assert.Equal(1, (int)number);
        Assert.Equal(new byte[] { 2, 10, 1, 10, 10, 0xFF, 10 }, block[..7]);
    }

    [Fact]
    public void ENT_negatif_se_repete()
    {
        var (tone, number, block) = Run("10 ENT -2, 5, 10, 2").Envelopes.Single();
        Assert.True(tone);
        Assert.Equal(2, (int)number);
        Assert.Equal(new byte[] { 0x81, 5, 10, 2 }, block[..4]);
    }

    [Fact]
    public void Parties_avec_le_signe_egal()
    {
        MiniZ80 cpu = Run("10 ENV 3, =9, 2000\n20 ENT 1, =478, 5");
        Assert.Equal(new byte[] { 1, 0x89, 0xD0, 0x07 }, cpu.Envelopes[0].Block[..4]);
        Assert.Equal(new byte[] { 1, 0xF1, 0xDE, 5 }, cpu.Envelopes[1].Block[..4]);
    }

    [Theory]
    [InlineData("10 ENV 0, 1, 1, 1", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 ENT 0, 1, 1, 1", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 ENV 1, 200, 1, 1", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 ENV 1, 1, 1", DiagnosticCode.WrongArgumentCount)]
    [InlineData("10 ENV 1, 1,1,1, 1,1,1, 1,1,1, 1,1,1, 1,1,1, 1,1,1", DiagnosticCode.WrongArgumentCount)]
    public void Enveloppe_fausse(string source, DiagnosticCode code)
    {
        Assert.Contains(Errors(source), e => e.Code == code);
    }

    // ---------- RELEASE et SQ ----------

    [Fact]
    public void RELEASE_et_SQ()
    {
        MiniZ80 cpu = Run("10 RELEASE 3\n20 PRINT SQ(1)");
        Assert.Equal(new byte[] { 3 }, cpu.Released);
        Assert.Equal(" 4 \r\n", cpu.Output.ToString());
    }
}
