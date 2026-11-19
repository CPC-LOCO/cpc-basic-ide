// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// ISIN(angle, échelle) et ICOS(angle, échelle) (D142) : sinus et cosinus rapides en entiers, avec une table au degré
/// près, comparés à une référence écrite à part, pour tous les angles de -720 à 720 et des échelles variées.
/// </summary>
public class ISinTests
{
    const int Origin = 0x4000;

    // sin(i degrés) x 16384, arrondi (la même règle que la table du module)
    static readonly long[] Sin = Enumerable.Range(0, 91)
        .Select(i => (long)Math.Round(Math.Sin(i * Math.PI / 180) * 16384)).ToArray();

    /// <summary>Référence : signe x ((sinus x |échelle| + 8192) / 16384), 32768 rendu comme 32767.</summary>
    static int Reference(int angle, int scale, bool cosine)
    {
        int a = ((angle % 360) + 360) % 360;
        if (cosine)
            a = (a + 90) % 360;
        int sign = 1;
        if (a >= 180)
        {
            a -= 180;
            sign = -1;
        }
        if (a > 90)
            a = 180 - a;
        if (scale < 0)
            sign = -sign;
        long magnitude = (Sin[a] * Math.Abs((long)scale) + 8192) >> 14;
        long result = sign * magnitude;
        return (int)Math.Min(result, 32767);
    }

    sealed class Machine
    {
        public readonly MiniZ80 Cpu = new();
        public readonly Z80Emitter Emitter = new(Origin);

        public Machine()
        {
            foreach (RuntimeModule m in Linker.Resolve([RuntimeLibrary.ISinName]))
                m.Emit(Emitter);
            Cpu.Load(Emitter.ToBytes(), Origin);
        }

        public int Call(string label, int angle, int scale)
        {
            Cpu.HL = (ushort)(angle & 0xFFFF);
            Cpu.DE = (ushort)(scale & 0xFFFF);
            Cpu.Call(Emitter.AddressOf(label));
            return (short)Cpu.HL;
        }
    }

    [Fact]
    public void La_table_du_module_est_celle_de_la_reference()
    {
        var machine = new Machine();
        int table = machine.Emitter.AddressOf(RuntimeLibrary.SinTableLabel);
        for (int i = 0; i <= 90; i++)
            Assert.Equal(Sin[i], machine.Cpu.Memory[table + 2 * i] | machine.Cpu.Memory[table + 2 * i + 1] << 8);
    }

    [Fact]
    public void ISIN_et_ICOS_pour_tous_les_angles_et_des_echelles_variees()
    {
        var machine = new Machine();
        int[] scales = [0, 1, -1, 7, 100, -100, 255, 1000, 12345, 16384, 32767, -32767, -32768];
        foreach (int scale in scales)
            for (int angle = -720; angle <= 720; angle++)
            {
                Assert.True(Reference(angle, scale, false) == machine.Call(RuntimeLibrary.ISinLabel, angle, scale),
                            $"ISIN({angle},{scale})");
                Assert.True(Reference(angle, scale, true) == machine.Call(RuntimeLibrary.ICosLabel, angle, scale),
                            $"ICOS({angle},{scale})");
            }
    }

    [Theory]
    [InlineData(-32768)]
    [InlineData(32767)]
    [InlineData(32760)]
    [InlineData(-32761)]
    [InlineData(23040)]
    [InlineData(-360)]
    public void Les_angles_extremes_sont_ramenes_de_0_a_359(int angle)
    {
        var machine = new Machine();
        Assert.Equal(Reference(angle, 1000, false), machine.Call(RuntimeLibrary.ISinLabel, angle, 1000));
        Assert.Equal(Reference(angle, 1000, true), machine.Call(RuntimeLibrary.ICosLabel, angle, 1000));
    }

    [Theory]
    [InlineData("ISIN", 30, 100, 50)]
    [InlineData("ICOS", 60, 100, 50)]
    [InlineData("ICOS", 0, 80, 80)]
    [InlineData("ISIN", 270, 80, -80)]
    [InlineData("ISIN", 370, 100, 17)]
    [InlineData("ISIN", -90, 100, -100)]
    [InlineData("ISIN", 90, 32767, 32767)]
    [InlineData("ISIN", 270, -32768, 32767)]       // 32768 : rendu comme 32767
    [InlineData("ISIN", 90, -32768, -32768)]
    [InlineData("ICOS", 90, 1000, 0)]
    public void Quelques_valeurs_connues(string function, int angle, int scale, int expected)
    {
        var machine = new Machine();
        Assert.Equal(expected, machine.Call(function == "ISIN" ? RuntimeLibrary.ISinLabel : RuntimeLibrary.ICosLabel, angle, scale));
    }

    // ---------- Analyse et programme complet ----------

    static MiniZ80 Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, Origin);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(Origin);
        return cpu;
    }

    [Fact]
    public void ISIN_et_ICOS_dans_un_programme()
    {
        Assert.Equal(" 50  50 -80 \r\n", Run("10 A=30:PRINT ISIN(A,100);ICOS(60,100);ISIN(A*9,80)").Output.ToString());
    }

    [Fact]
    public void ELLIPSE_et_ISIN_partagent_la_meme_table()
    {
        GeneratedProgram program = CodeGenerator.Generate(
            BasicAnalyzer.Analyze("10 X=ISIN(10,5):ELLIPSE 160,100,50,20,,,30").Program, Origin);
        Assert.Contains(RuntimeLibrary.SinTableName, program.Modules);
        Assert.Contains(RuntimeLibrary.ISinName, program.Modules);
        Assert.Contains(RuntimeLibrary.EllipseName, program.Modules);
        Assert.Single(program.Modules, m => m == RuntimeLibrary.SinTableName);
    }

    [Theory]
    [InlineData("10 X=ISIN(30)")]
    [InlineData("10 X=ICOS(30,100,5)")]
    [InlineData("10 ISIN=5")]                       // nouveaux mots réservés
    [InlineData("10 ICOS=5")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Fact]
    public void Sans_ISIN_ni_ELLIPSE_la_table_n_est_pas_liee()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 X=SIN(30)").Program, Origin);
        Assert.DoesNotContain(RuntimeLibrary.SinTableName, program.Modules);
    }
}
