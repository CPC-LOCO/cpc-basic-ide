// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D214 : BOOM son [, durée] (1 voiture qui explose, 2 feu d'artifice, 3 pétard, 4 marteau, 5 malus), les sons de
/// examples\boomref.bas choisis à l'oreille par l'utilisateur. examples\boom.bas.
/// </summary>
public class BoomTests
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

    // un son : canal, env. volume, env. ton, période, bruit, volume, durée
    static byte[] Sound(byte channel, byte env, byte ent, int period, byte noise, byte volume, int duration) =>
        [channel, env, ent, (byte)period, (byte)(period >> 8), noise, volume, (byte)duration, (byte)(duration >> 8)];

    // la dernière enveloppe posée sous ce numéro
    static byte[] Envelope(MiniZ80 cpu, bool tone, byte number) => cpu.Envelopes.Last(e => e.Tone == tone && e.Number == number).Block;

    [Fact]
    public void BOOM_1_la_voiture_qui_explose()
    {
        MiniZ80 cpu = Run("10 BOOM 1");

        // claquement, puis grondement de bruit sur les canaux A et B, éteint en 15 marches de 13 centièmes (ENV 15,-1,13)
        Assert.Equal(Sound(1, 0, 0, 0, 6, 15, 8), cpu.Sounds[0]);
        Assert.Equal(Sound(1, 9, 0, 0, 31, 15, 200), cpu.Sounds[1]);
        Assert.Equal(Sound(2, 9, 0, 3000, 31, 15, 200), cpu.Sounds[2]);
        Assert.Equal(new byte[] { 1, 15, 0xFF, 13 }, Envelope(cpu, false, 9)[..4]);
    }

    [Fact]
    public void BOOM_avec_une_duree_allonge_le_son()
    {
        MiniZ80 cpu = Run("10 BOOM 1, 300");

        Assert.Equal(Sound(1, 9, 0, 0, 31, 15, 300), cpu.Sounds[1]);
        Assert.Equal(new byte[] { 1, 15, 0xFF, 20 }, Envelope(cpu, false, 9)[..4]);   // 300 / 15
    }

    [Fact]
    public void BOOM_2_le_feu_d_artifice()
    {
        MiniZ80 cpu = Run("10 BOOM 2");

        Assert.Equal(Sound(1, 0, 8, 300, 0, 8, 60), cpu.Sounds[0]);   // sifflement (ENT 60,-3,1)
        Assert.Equal(Sound(1, 9, 0, 0, 15, 15, 30), cpu.Sounds[1]);   // boom
        Assert.Equal(Sound(1, 10, 0, 0, 4, 15, 45), cpu.Sounds[2]);   // crépitement
        Assert.Equal(new byte[] { 1, 60, 0xFD, 1 }, Envelope(cpu, true, 8)[..4]);
        Assert.Equal(new byte[] { 3, 5, 0xFD, 3, 5, 3, 3, 5, 0xFD, 3 }, Envelope(cpu, false, 10)[..10]);
    }

    [Fact]
    public void BOOM_3_le_petard()
    {
        MiniZ80 cpu = Run("10 BOOM 3");

        Assert.Equal(Sound(1, 9, 0, 0, 6, 15, 30), cpu.Sounds.Single());
        Assert.Equal(new byte[] { 1, 15, 0xFF, 2 }, Envelope(cpu, false, 9)[..4]);
    }

    [Fact]
    public void BOOM_4_le_marteau()
    {
        MiniZ80 cpu = Run("10 BOOM 4");

        Assert.Equal(Sound(1, 9, 9, 180, 10, 15, 15), cpu.Sounds[0]);
        Assert.Equal(Sound(2, 9, 9, 90, 0, 12, 15), cpu.Sounds[1]);
        Assert.Equal(new byte[] { 1, 15, 0xFF, 1 }, Envelope(cpu, false, 9)[..4]);
        Assert.Equal(new byte[] { 1, 20, 8, 1 }, Envelope(cpu, true, 9)[..4]);
    }

    [Fact]
    public void BOOM_5_le_malus_et_sa_descente_qui_s_allonge()
    {
        MiniZ80 cpu = Run("10 BOOM 5:D=300:BOOM 5,D");

        Assert.Equal(Sound(1, 9, 10, 200, 0, 13, 90), cpu.Sounds[0]);
        Assert.Equal(Sound(2, 9, 10, 203, 0, 10, 90), cpu.Sounds[1]);
        Assert.Equal(Sound(1, 9, 10, 200, 0, 13, 300), cpu.Sounds[2]);
        Assert.Equal(new byte[] { 1, 60, 6, 5 }, Envelope(cpu, true, 10)[..4]);    // 300 / 60
        Assert.Equal(new byte[] { 1, 15, 0xFF, 20 }, Envelope(cpu, false, 9)[..4]);
    }

    [Theory]
    [InlineData("10 BOOM 0")]
    [InlineData("10 BOOM 6")]
    [InlineData("10 BOOM")]
    [InlineData("10 BOOM 1, 0")]
    [InlineData("10 BOOM 1, 2, 3")]
    public void Un_mauvais_BOOM_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 N=6:BOOM N")]
    [InlineData("10 D=0:BOOM 1,D")]
    [InlineData("10 D=-5:BOOM 1,D")]
    public void BOOM_calcule_hors_des_limites_arrete_le_programme(string source)
    {
        MiniZ80 cpu = Run(source);

        Assert.Empty(cpu.Sounds);
        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void BOOM_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 BOOM 1:BOOM 2,100:BOOM 3:BOOM 4:BOOM 5");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
