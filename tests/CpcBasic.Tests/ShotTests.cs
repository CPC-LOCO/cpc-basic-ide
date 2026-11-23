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
/// D215 : SHOT son (1 tir de laser, 2 missile, 3 rafale, 4 vitre qui casse), les sons de examples\shotref.bas choisis à
/// l'oreille par l'utilisateur. examples\shot.bas.
/// </summary>
public class ShotTests
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

    static byte[] Envelope(MiniZ80 cpu, bool tone, byte number) => cpu.Envelopes.Last(e => e.Tone == tone && e.Number == number).Block;

    [Fact]
    public void SHOT_1_le_laser()
    {
        MiniZ80 cpu = Run("10 SHOT 1");

        Assert.Equal(Sound(1, 8, 7, 30, 0, 15, 30), cpu.Sounds.Single());   // SOUND 1,30,30,15,8,7
        Assert.Equal(new byte[] { 1, 15, 0xFF, 2 }, Envelope(cpu, false, 8)[..4]);
        Assert.Equal(new byte[] { 1, 30, 9, 1 }, Envelope(cpu, true, 7)[..4]);
    }

    [Fact]
    public void SHOT_2_le_missile()
    {
        MiniZ80 cpu = Run("10 SHOT 2");

        Assert.Equal(Sound(1, 7, 0, 0, 3, 5, 100), cpu.Sounds[0]);
        Assert.Equal(Sound(2, 7, 6, 100, 0, 5, 100), cpu.Sounds[1]);
        Assert.Equal(new byte[] { 2, 10, 1, 4, 15, 0xFF, 4 }, Envelope(cpu, false, 7)[..7]);
        Assert.Equal(new byte[] { 1, 90, 3, 1 }, Envelope(cpu, true, 6)[..4]);
    }

    [Fact]
    public void SHOT_3_la_rafale()
    {
        MiniZ80 cpu = Run("10 N=3:SHOT N");

        Assert.Equal(6, cpu.Sounds.Count);
        Assert.All(cpu.Sounds, s => Assert.Equal(Sound(1, 6, 0, 0, 5, 14, 9), s));
        Assert.Equal(new byte[] { 1, 7, 0xFE, 1 }, Envelope(cpu, false, 6)[..4]);
    }

    [Fact]
    public void SHOT_4_la_vitre_qui_casse()
    {
        MiniZ80 cpu = Run("10 SHOT 4");

        Assert.Equal(7, cpu.Sounds.Count);
        Assert.Equal(Sound(1, 5, 0, 0, 1, 15, 60), cpu.Sounds[0]);
        int[] tints = [40, 55, 35, 60, 45, 50];
        for (int i = 0; i < 6; i++)
            Assert.Equal(Sound(2, 4, 0, tints[i], 0, 12, 7), cpu.Sounds[1 + i]);
        Assert.Equal(new byte[] { 1, 15, 0xFF, 4 }, Envelope(cpu, false, 5)[..4]);
        Assert.Equal(new byte[] { 1, 6, 0xFE, 1 }, Envelope(cpu, false, 4)[..4]);
    }

    [Theory]
    [InlineData("10 SHOT 0")]
    [InlineData("10 SHOT 5")]
    [InlineData("10 SHOT")]
    [InlineData("10 SHOT 1, 100")]
    public void Un_mauvais_SHOT_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Fact]
    public void SHOT_calcule_hors_de_1_a_4_arrete_le_programme()
    {
        MiniZ80 cpu = Run("10 N=5:SHOT N");

        Assert.Empty(cpu.Sounds);
        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void SHOT_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 SHOT 1:SHOT 2:SHOT 3:SHOT 4");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
