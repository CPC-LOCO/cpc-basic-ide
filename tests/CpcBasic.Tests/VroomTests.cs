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
/// D213 : VROOM son (1 moteur qui monte, 2 ralenti, 3 moteur qui descend), les sons de examples\vroomref.bas choisis à
/// l'oreille par l'utilisateur. examples\vroom.bas.
/// </summary>
public class VroomTests
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
    static byte[] Sound(byte env, byte ent, int period, byte volume, int duration) =>
        [1, env, ent, (byte)period, (byte)(period >> 8), 25, volume, (byte)duration, (byte)(duration >> 8)];

    [Fact]
    public void VROOM_1_le_moteur_qui_monte()
    {
        MiniZ80 cpu = Run("10 VROOM 1");

        // comme SOUND 1,900,-13,12,15,15,25 avec ENV 15,2,-6,2,2,6,2 et ENT 15,100,-5,1
        Assert.Equal(Sound(15, 15, 900, 12, -13), cpu.Sounds.Single());
        Assert.Contains(cpu.Envelopes, e => !e.Tone && e.Number == 15 && e.Block[..7].SequenceEqual(new byte[] { 2, 2, 0xFA, 2, 2, 6, 2 }));
        Assert.Contains(cpu.Envelopes, e => e.Tone && e.Number == 15 && e.Block[..4].SequenceEqual(new byte[] { 1, 100, 0xFB, 1 }));
    }

    [Fact]
    public void VROOM_2_le_ralenti()
    {
        MiniZ80 cpu = Run("10 VROOM 2");

        // comme SOUND 1,900,-6,12,14,14,25 avec ENV 14,3,-4,4,3,4,4 et ENT -14,5,8,1,5,-8,1 (qui se répète)
        Assert.Equal(Sound(14, 14, 900, 12, -6), cpu.Sounds.Single());
        Assert.Contains(cpu.Envelopes, e => !e.Tone && e.Number == 14 && e.Block[..7].SequenceEqual(new byte[] { 2, 3, 0xFC, 4, 3, 4, 4 }));
        Assert.Contains(cpu.Envelopes, e => e.Tone && e.Number == 14 && e.Block[..7].SequenceEqual(new byte[] { 0x82, 5, 8, 1, 5, 0xF8, 1 }));
    }

    [Fact]
    public void VROOM_3_le_moteur_qui_descend_en_trois_morceaux()
    {
        MiniZ80 cpu = Run("10 N=3:VROOM N");

        Assert.Equal(3, cpu.Sounds.Count);
        Assert.Equal(Sound(15, 13, 400, 12, -4), cpu.Sounds[0]);
        Assert.Equal(Sound(13, 12, 565, 8, -4), cpu.Sounds[1]);
        Assert.Equal(Sound(12, 11, 730, 4, -5), cpu.Sounds[2]);
    }

    [Theory]
    [InlineData("10 VROOM 0")]
    [InlineData("10 VROOM 4")]
    [InlineData("10 VROOM")]
    [InlineData("10 VROOM 1, 2")]
    public void Un_mauvais_VROOM_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Fact]
    public void VROOM_calcule_hors_de_1_a_3_arrete_le_programme()
    {
        MiniZ80 cpu = Run("10 N=5:VROOM N");

        Assert.Empty(cpu.Sounds);
        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void VROOM_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 VROOM 1:VROOM 2:VROOM 3");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
