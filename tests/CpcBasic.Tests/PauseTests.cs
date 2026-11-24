// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// PAUSE [heure,][minute,] seconde et CLIC code (D222, idées de l'utilisateur notées en D049). examples\pause.bas.
/// </summary>
public class PauseTests
{
    const byte Esc = 0xFC;

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static MiniZ80 Run(string source, uint clockStep, params (int Tick, byte Key)[] keys)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80, ClockStep = clockStep };
        cpu.TimedKeys.AddRange(keys);
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    // Le temps avance de 300 (une seconde) à chaque lecture de l'horloge : une lecture au départ, puis une par seconde
    [Theory]
    [InlineData("10 PAUSE 0", 0)]
    [InlineData("10 PAUSE 2", 2)]
    [InlineData("10 PAUSE 1,30", 90)]
    [InlineData("10 PAUSE 1,0,0", 3600)]
    [InlineData("10 PAUSE 2,3,4", 7384)]
    [InlineData("10 H=1:M=2:S=3:PAUSE H,M,S", 3723)]
    public void PAUSE_attend_le_bon_nombre_de_secondes(string source, int seconds)
    {
        MiniZ80 cpu = Run(source + ":PRINT \"FIN\"", 300);

        Assert.Equal("FIN\r\n", cpu.Output.ToString());
        Assert.Equal((uint)(300 * (seconds + 1)), cpu.Clock);
    }

    [Fact]
    public void PAUSE_finit_juste_apres_le_temps_demande()
    {
        MiniZ80 cpu = Run("10 PAUSE 2:PRINT \"FIN\"", 7);

        Assert.Equal("FIN\r\n", cpu.Output.ToString());
        Assert.InRange(cpu.Clock, 600u, 620u);
    }

    [Fact]
    public void PAUSE_laisse_passer_les_minuteries()
    {
        MiniZ80 cpu = Run("10 EVERY 1 GOSUB 100\n20 PAUSE 1\n30 PRINT N>0\n40 END\n100 N=N+1:RETURN", 1);

        Assert.Equal("-1 \r\n", cpu.Output.ToString());
    }

    [Fact]
    public void CLIC_attend_la_touche_demandee()
    {
        MiniZ80 cpu = Run("10 ON BREAK CONT\n20 CLIC 32\n30 PRINT \"FIN\"", 0, (3, (byte)'A'), (6, (byte)'b'), (9, (byte)' '));

        Assert.Equal("FIN\r\n", cpu.Output.ToString());
        Assert.True(cpu.Ticks >= 9);
    }

    [Fact]
    public void CLIC_laisse_passer_ON_BREAK_GOSUB()
    {
        MiniZ80 cpu = Run("10 ON BREAK GOSUB 100\n20 CLIC 32\n30 PRINT B\n40 END\n100 B=B+1:RETURN", 0,
                          (3, Esc), (6, Esc), (10, (byte)' '));

        Assert.Equal(" 1 \r\n", cpu.Output.ToString());
    }

    [Theory]
    [InlineData("10 PAUSE")]
    [InlineData("10 PAUSE 1,2,3,4")]
    [InlineData("10 CLIC")]
    [InlineData("10 CLIC 30")]
    [InlineData("10 CLIC 256")]
    [InlineData("10 CLIC 32, 1")]
    public void Un_mauvais_PAUSE_ou_CLIC_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 S=-1:PAUSE S")]
    [InlineData("10 M=-5:PAUSE M,0")]
    [InlineData("10 C=13:CLIC C")]
    [InlineData("10 C=300:CLIC C")]
    public void PAUSE_ou_CLIC_calcule_hors_des_limites_arrete_le_programme(string source)
    {
        MiniZ80 cpu = Run(source, 300);

        Assert.Contains("Improper argument", cpu.Output.ToString());
    }

    [Fact]
    public void PAUSE_et_CLIC_vont_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 PAUSE 1:CLIC 32:PAUSE 0,1,0");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
