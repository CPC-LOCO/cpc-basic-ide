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
/// D217 : FALL son (1 chute dans le vide puis le sol, 2 vie perdue), les sons 2 et 4 de examples\fallref.bas choisis à
/// l'oreille par l'utilisateur. examples\fall.bas.
/// </summary>
public class FallTests
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
    static byte[] Sound(byte env, byte ent, int period, byte noise, byte volume, int duration) =>
        [1, env, ent, (byte)period, (byte)(period >> 8), noise, volume, (byte)duration, (byte)(duration >> 8)];

    static byte[] Envelope(MiniZ80 cpu, bool tone, byte number) => cpu.Envelopes.Last(e => e.Tone == tone && e.Number == number).Block;

    [Fact]
    public void FALL_1_la_chute_puis_le_sol()
    {
        MiniZ80 cpu = Run("10 FALL 1");

        Assert.Equal(Sound(0, 5, 100, 0, 12, 100), cpu.Sounds[0]);      // SOUND 1,100,100,12,0,5 (ENT 100,6,1)
        Assert.Equal(Sound(2, 0, 2000, 25, 15, 15), cpu.Sounds[1]);     // SOUND 1,2000,15,15,2,0,25 (ENV 15,-1,1)
        Assert.Equal(new byte[] { 1, 100, 6, 1 }, Envelope(cpu, true, 5)[..4]);
        Assert.Equal(new byte[] { 1, 15, 0xFF, 1 }, Envelope(cpu, false, 2)[..4]);
    }

    [Fact]
    public void FALL_2_la_vie_perdue()
    {
        MiniZ80 cpu = Run("10 N=2:FALL N");

        Assert.Equal(4, cpu.Sounds.Count);
        Assert.Equal(Sound(0, 0, 239, 0, 12, 25), cpu.Sounds[0]);
        Assert.Equal(Sound(0, 0, 253, 0, 12, 25), cpu.Sounds[1]);
        Assert.Equal(Sound(0, 0, 268, 0, 12, 25), cpu.Sounds[2]);
        Assert.Equal(Sound(3, 4, 284, 0, 12, 60), cpu.Sounds[3]);
        Assert.Equal(new byte[] { 1, 12, 0xFF, 5 }, Envelope(cpu, false, 3)[..4]);
        Assert.Equal(new byte[] { 0x82, 2, 2, 1, 2, 0xFE, 1 }, Envelope(cpu, true, 4)[..7]);
    }

    [Theory]
    [InlineData("10 FALL 0")]
    [InlineData("10 FALL 3")]
    [InlineData("10 FALL")]
    [InlineData("10 FALL 1, 100")]
    [InlineData("10 FALL 1:ENV 3,1,1,1")]                         // ENV 3 est pris par FALL (D216)
    public void Un_mauvais_FALL_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Fact]
    public void FALL_calcule_hors_de_1_a_2_arrete_le_programme()
    {
        MiniZ80 cpu = Run("10 N=3:FALL N");

        Assert.Empty(cpu.Sounds);
        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void FALL_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 FALL 1:FALL 2");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
