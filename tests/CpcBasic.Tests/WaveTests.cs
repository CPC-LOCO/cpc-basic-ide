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
/// D219 : WAVE son [, envers] (1 petite vague, 2 vague moyenne, 3 grosse vague, 4 gros plouf ; 1 : à l'envers), les sons
/// de examples\waveref.bas choisis à l'oreille par l'utilisateur. examples\wave.bas.
/// </summary>
public class WaveTests
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

    [Theory]
    [InlineData("10 WAVE 1", 0, 104, 4, 12, new byte[] { 2, 8, 1, 4, 12, 0xFF, 6 })]
    [InlineData("10 WAVE 2", 0, 170, 5, 18, new byte[] { 2, 10, 1, 5, 15, 0xFF, 8 })]
    [InlineData("10 WAVE 1,1", 0, 104, 0, 12, new byte[] { 2, 12, 1, 6, 8, 0xFF, 4 })]
    [InlineData("10 WAVE 2,1", 0, 170, 0, 18, new byte[] { 2, 15, 1, 8, 10, 0xFF, 5 })]
    public void Les_vagues_1_et_2_a_l_endroit_et_a_l_envers(string source, int period, int duration, byte volume, byte noise,
                                                           byte[] envelope)
    {
        MiniZ80 cpu = Run(source);

        Assert.Equal(Sound(1, 15, 0, period, noise, volume, duration), cpu.Sounds.Single());
        Assert.Equal(envelope, Envelope(cpu, false, 15)[..7]);
    }

    [Fact]
    public void La_grosse_vague_et_son_envers()
    {
        MiniZ80 cpu = Run("10 WAVE 3:WAVE 3,1");

        Assert.Equal(Sound(1, 15, 0, 0, 28, 3, 48), cpu.Sounds[0]);     // grondement qui monte
        Assert.Equal(Sound(1, 14, 0, 0, 16, 15, 105), cpu.Sounds[1]);   // elle se brise
        Assert.Equal(Sound(1, 13, 0, 0, 16, 0, 105), cpu.Sounds[2]);    // à l'envers : le souffle monte
        Assert.Equal(Sound(1, 12, 0, 0, 28, 15, 48), cpu.Sounds[3]);    // puis le grondement s'éteint
        Assert.Equal(new byte[] { 1, 15, 0xFF, 7 }, Envelope(cpu, false, 14)[..4]);
        Assert.Equal(new byte[] { 1, 15, 1, 7 }, Envelope(cpu, false, 13)[..4]);
    }

    [Fact]
    public void Le_gros_plouf_et_son_envers()
    {
        MiniZ80 cpu = Run("10 WAVE 4:D=1:WAVE 4,D");

        Assert.Equal(Sound(1, 15, 15, 60, 0, 14, 15), cpu.Sounds[0]);   // « bloup » qui plonge
        Assert.Equal(Sound(2, 14, 0, 0, 4, 15, 45), cpu.Sounds[1]);     // gerbe d'eau
        Assert.Equal(Sound(2, 13, 0, 0, 4, 0, 45), cpu.Sounds[2]);      // à l'envers : la gerbe monte
        Assert.Equal(Sound(1, 0, 0, 0, 0, 0, 30), cpu.Sounds[3]);       // silence, pour finir ensemble
        Assert.Equal(Sound(1, 12, 14, 300, 0, 0, 15), cpu.Sounds[4]);   // le « bloup » qui remonte
        Assert.Equal(new byte[] { 1, 15, 16, 1 }, Envelope(cpu, true, 15)[..4]);
        Assert.Equal(new byte[] { 1, 15, 0xF0, 1 }, Envelope(cpu, true, 14)[..4]);
    }

    [Fact]
    public void Des_vagues_au_hasard_pour_une_ile()
    {
        MiniZ80 cpu = Run("10 FOR A=1 TO 20:WAVE 1+INT(RND*3):NEXT");

        Assert.InRange(cpu.Sounds.Count, 20, 40);
        Assert.All(cpu.Sounds, s => Assert.Equal(0, s[2]));             // jamais de plouf (qui a une enveloppe de ton)
    }

    [Theory]
    [InlineData("10 WAVE 0")]
    [InlineData("10 WAVE 5")]
    [InlineData("10 WAVE")]
    [InlineData("10 WAVE 1, 2")]
    [InlineData("10 WAVE 1, 1, 1")]
    public void Un_mauvais_WAVE_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 N=5:WAVE N")]
    [InlineData("10 D=2:WAVE 1,D")]
    public void WAVE_calcule_hors_des_limites_arrete_le_programme(string source)
    {
        MiniZ80 cpu = Run(source);

        Assert.Empty(cpu.Sounds);
        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void WAVE_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 FOR A=1 TO 4:WAVE A:WAVE A,1:NEXT");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
