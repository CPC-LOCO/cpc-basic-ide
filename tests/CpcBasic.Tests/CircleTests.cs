// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// CIRCLE (D130) : le module Circle essayé sur le mini-Z80 et comparé point par point à une référence écrite à part
/// (calcul direct de chaque demi-largeur, sans les sommes du module), dans les 3 MODES.
/// </summary>
public class CircleTests
{
    const int Origin = 0x4000;
    static readonly int[] Width = [160, 320, 640];

    sealed class Machine
    {
        public readonly MiniZ80 Cpu = new();
        public readonly Z80Emitter Emitter = new(Origin);

        public Machine(int mode, int x, int y, int r, int pen, int? fill, bool upperRom = true)
        {
            foreach (RuntimeModule m in Linker.Resolve([RuntimeLibrary.CircleName]))
                m.Emit(Emitter);
            Cpu.Load(Emitter.ToBytes(), Origin);
            Cpu.ScreenMode = (byte)mode;
            var random = new Random(11);
            for (int address = 0xC000; address < 0x10000; address++)
                Cpu.Memory[address] = (byte)random.Next(256);
            Cpu.UpperRomEnabled = upperRom;
            SetWord(RuntimeLibrary.CircleXLabel, x);
            SetWord(RuntimeLibrary.CircleYLabel, y);
            SetWord(RuntimeLibrary.CircleRadiusLabel, r);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CirclePenLabel)] = (byte)pen;
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CircleFillPenLabel)] = (byte)(fill ?? 0);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CircleDoFillLabel)] = (byte)(fill is null ? 0 : 1);
        }

        void SetWord(string label, int value)
        {
            int address = Emitter.AddressOf(label);
            Cpu.Memory[address] = (byte)(value & 0xFF);
            Cpu.Memory[address + 1] = (byte)((value >> 8) & 0xFF);
        }

        public byte[] Screen() => Cpu.Memory[0xC000..];

        public void Draw() => Cpu.Call(Emitter.AddressOf(RuntimeLibrary.CircleLabel));
    }

    // Un point tel que le CPC le range en mémoire (même écriture que dans BoxTests, sans les tables du module)
    static void ReferencePixel(byte[] screen, int mode, int x, int y, int pen)
    {
        if (x < 0 || x >= Width[mode] || y < 0 || y >= 200)
            return;
        int pixelsPerByte = 2 << mode;
        int offset = (y / 8) * 80 + (y % 8) * 0x800 + x / pixelsPerByte;
        int p = x % pixelsPerByte;
        (int ByteBit, int PenBit)[] planes = mode switch
        {
            2 => new[] { (7 - p, 0) },
            1 => new[] { (7 - p, 0), (3 - p, 1) },
            _ => new[] { (7 - p, 0), (3 - p, 1), (5 - p, 2), (1 - p, 3) }
        };
        foreach ((int byteBit, int penBit) in planes)
        {
            int value = (pen >> penBit) & 1;
            screen[offset] = (byte)((screen[offset] & ~(1 << byteBit)) | (value << byteBit));
        }
    }

    // Demi-largeur de la ligne à la distance d du centre : la plus grande w avec w <= k * f * racine((r + 1/2)² - d²),
    // k = 0,5 / 1 / 2 selon le MODE, f = 69/64 (vrai moniteur Amstrad, D140 ; avant : f = 1, D130)
    static int HalfWidth(int mode, int r, int d)
    {
        long k4 = mode switch { 0 => 1, 1 => 4, _ => 16 };          // 4 k²
        double exact = (1 << mode) / 2.0 * 69 / 64 * Math.Sqrt((r + 0.5) * (r + 0.5) - (double)d * d);
        int w = Math.Max(0, (int)Math.Floor(exact) - 1);
        while (65536L * (w + 1) * (w + 1) <= k4 * 4761 * ((2L * r + 1) * (2L * r + 1) - 4L * d * d))
            w++;
        return w;
    }

    /// <summary>Le rond attendu, ligne par ligne : contour qui se raccorde à la ligne voisine, intérieur entre les deux.</summary>
    static void ReferenceCircle(byte[] screen, int mode, int cx, int cy, int r, int pen, int? fill)
    {
        for (int d = 0; d <= r; d++)
        {
            int outer = HalfWidth(mode, r, d);
            int inner = d == r ? -1 : HalfWidth(mode, r, d + 1);
            int s = Math.Min(inner + 1, outer);
            foreach (int y in d == 0 ? new[] { cy } : new[] { cy - d, cy + d })
            {
                if (s == 0)
                {
                    for (int x = -outer; x <= outer; x++)
                        ReferencePixel(screen, mode, cx + x, y, pen);
                    continue;
                }
                for (int x = s; x <= outer; x++)
                {
                    ReferencePixel(screen, mode, cx - x, y, pen);
                    ReferencePixel(screen, mode, cx + x, y, pen);
                }
                if (fill is int f)
                    for (int x = -(s - 1); x <= s - 1; x++)
                        ReferencePixel(screen, mode, cx + x, y, f);
            }
        }
    }

    static IEnumerable<(int X, int Y, int R)> Circles(int mode, int seed)
    {
        int w = Width[mode];
        var list = new List<(int, int, int)>
        {
            (w / 2, 100, 0), (w / 2, 100, 1), (w / 2, 100, 2), (w / 2, 100, 10), (w / 2, 100, 50), (w / 2, 100, 99),
            (w / 2, 100, 150), (0, 0, 30), (w - 1, 199, 30), (-20, 50, 30), (w + 20, 50, 30), (50, -20, 30), (50, 220, 30),
            (w / 2, 100, 1000), (-32768, 100, 1000), (32767, 100, 1000), (100, -32768, 1000), (100, 32767, 5),
            (-2399, 100, 1000), (w + 2399, 100, 1000), (100, -999, 1000), (100, 1198, 1000), (3, 3, 7)
        };
        var random = new Random(seed);
        for (int n = 0; n < 25; n++)
            list.Add((random.Next(-50, w + 50), random.Next(-50, 250), random.Next(0, 130)));
        return list;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_rond_est_trace_comme_la_reference_meme_hors_de_l_ecran(int mode)
    {
        var random = new Random(3 + mode);
        foreach ((int x, int y, int r) in Circles(mode, 20 + mode))
        {
            int pen = random.Next(16);
            var machine = new Machine(mode, x, y, r, pen, null);
            byte[] expected = machine.Screen();
            ReferenceCircle(expected, mode, x, y, r, pen, null);

            machine.Draw();

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, CIRCLE {x}, {y}, {r}, {pen}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_rond_rempli_a_son_interieur_dans_l_autre_stylo(int mode)
    {
        var random = new Random(9 + mode);
        foreach ((int x, int y, int r) in Circles(mode, 40 + mode))
        {
            int pen = random.Next(16);
            int fill = (pen + 1 + random.Next(15)) % 16;
            var machine = new Machine(mode, x, y, r, pen, fill);
            byte[] expected = machine.Screen();
            ReferenceCircle(expected, mode, x, y, r, pen, fill);

            machine.Draw();

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, CIRCLE {x}, {y}, {r}, {pen}, {fill}");
        }
    }

    [Theory]
    // Rayon 10 : au centre, demi-largeur 0,5 / 1 / 2 fois 10,5 x 69/64 ; en haut et en bas, une ligne plate (pas de point seul)
    [InlineData(0, 5, 1)]
    [InlineData(1, 11, 3)]
    [InlineData(2, 22, 6)]
    public void Le_rond_est_corrige_en_largeur_selon_le_MODE(int mode, int middle, int top)
    {
        Assert.Equal(middle, HalfWidth(mode, 10, 0));
        Assert.Equal(top, HalfWidth(mode, 10, 10));
    }

    [Fact]
    public void CIRCLE_remet_la_ROM_haute_dans_l_etat_ou_il_l_a_trouvee()
    {
        foreach (bool before in new[] { true, false })
        {
            var machine = new Machine(1, 160, 100, 50, 1, 2, upperRom: before);

            machine.Draw();

            Assert.Equal(before, machine.Cpu.UpperRomEnabled);
        }
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

    [Theory]
    [InlineData("10 CIRCLE 160,100,50")]
    [InlineData("10 CIRCLE 160,100,50,1")]
    [InlineData("10 CIRCLE 160,100,50,1,2")]
    [InlineData("10 X=-5:R=0:CIRCLE X*2,X+200,R,X+6,15")]
    [InlineData("10 CIRCLE 0,0,1000")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Theory]
    [InlineData("10 CIRCLE 160,100")]
    [InlineData("10 CIRCLE 160,100,50,1,2,3")]
    [InlineData("10 CIRCLE 160,100,1001")]
    [InlineData("10 CIRCLE 160,100,-1")]
    [InlineData("10 CIRCLE 160,100,50,16")]
    [InlineData("10 CIRCLE 160,100,50,1,16")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Theory]
    [InlineData("10 R=1001\n20 CIRCLE 160,100,R")]
    [InlineData("10 R=-1\n20 CIRCLE 160,100,R")]
    public void Un_rayon_calcule_hors_de_0_a_1000_donne_Improper_argument(string source)
    {
        Assert.Equal("Improper argument in 20\r\n", Run(source).Output.ToString());
    }

    [Fact]
    public void CIRCLE_dans_un_programme_dessine_et_lie_le_module()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 R=40:CIRCLE 160,100,R,1,2");
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin);
        Assert.Contains(RuntimeLibrary.CircleName, program.Modules);
        Assert.Contains(RuntimeLibrary.PixelName, program.Modules);

        MiniZ80 cpu = Run("10 R=40:CIRCLE 160,100,R,1,2");
        Assert.Equal("", cpu.Output.ToString());
        Assert.True(cpu.UpperRomEnabled);
    }

    [Fact]
    public void Sans_CIRCLE_le_module_n_est_pas_lie()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 BOX 1,1,5,5").Program, Origin);
        Assert.DoesNotContain(RuntimeLibrary.CircleName, program.Modules);
    }
}
