// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// ELLIPSE (D139) : le module Ellipse essayé sur le mini-Z80 et comparé point par point à une référence écrite à part
/// (la méthode en nombres entiers de essais\ellipse\entiers.py, recopiée ici), dans les 3 MODES ; deux rayons égaux
/// donnent exactement les points de CIRCLE.
/// </summary>
public class EllipseTests
{
    const int Origin = 0x4000;
    static readonly int[] Width = [160, 320, 640];

    sealed class Machine
    {
        public readonly MiniZ80 Cpu = new();
        public readonly Z80Emitter Emitter = new(Origin);

        public Machine(int mode, bool upperRom = true, params string[] modules)
        {
            foreach (RuntimeModule m in Linker.Resolve(modules.Length > 0 ? modules : [RuntimeLibrary.EllipseName]))
                m.Emit(Emitter);
            Cpu.Load(Emitter.ToBytes(), Origin);
            Cpu.ScreenMode = (byte)mode;
            var random = new Random(11);
            for (int address = 0xC000; address < 0x10000; address++)
                Cpu.Memory[address] = (byte)random.Next(256);
            Cpu.UpperRomEnabled = upperRom;
        }

        void SetWord(string label, int value)
        {
            int address = Emitter.AddressOf(label);
            Cpu.Memory[address] = (byte)(value & 0xFF);
            Cpu.Memory[address + 1] = (byte)((value >> 8) & 0xFF);
        }

        public byte[] Screen() => Cpu.Memory[0xC000..];

        public void Ellipse(int x, int y, int a, int b, int t, int pen, int? fill)
        {
            SetWord(RuntimeLibrary.EllipseXLabel, x);
            SetWord(RuntimeLibrary.EllipseYLabel, y);
            SetWord(RuntimeLibrary.EllipseRadiusXLabel, a);
            SetWord(RuntimeLibrary.EllipseRadiusYLabel, b);
            SetWord(RuntimeLibrary.EllipseAngleLabel, t);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.EllipsePenLabel)] = (byte)pen;
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.EllipseFillPenLabel)] = (byte)(fill ?? 0);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.EllipseDoFillLabel)] = (byte)(fill is null ? 0 : 1);
            Cpu.Call(Emitter.AddressOf(RuntimeLibrary.EllipseLabel));
        }

        public void Circle(int x, int y, int r, int pen, int? fill)
        {
            SetWord(RuntimeLibrary.CircleXLabel, x);
            SetWord(RuntimeLibrary.CircleYLabel, y);
            SetWord(RuntimeLibrary.CircleRadiusLabel, r);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CirclePenLabel)] = (byte)pen;
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CircleFillPenLabel)] = (byte)(fill ?? 0);
            Cpu.Memory[Emitter.AddressOf(RuntimeLibrary.CircleDoFillLabel)] = (byte)(fill is null ? 0 : 1);
            Cpu.Call(Emitter.AddressOf(RuntimeLibrary.CircleLabel));
        }
    }

    // Un point tel que le CPC le range en mémoire (même écriture que dans CircleTests)
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

    // ---------- Référence : la méthode en nombres entiers (essais\ellipse\entiers.py) ----------

    // sin(i degrés) x 16384, arrondi, i = 0 à 90
    static readonly long[] Sin = Enumerable.Range(0, 91)
        .Select(i => (long)Math.Round(Math.Sin(i * Math.PI / 180) * 16384)).ToArray();

    static long ISqrt(long n)
    {
        if (n <= 0)
            return 0;
        long r = (long)Math.Sqrt(n);
        while (r * r > n) r--;
        while ((r + 1) * (r + 1) <= n) r++;
        return r;
    }

    /// <summary>P, Q (T = P - Q d², 24 bits après la virgule), S16 (glissement par ligne), D (lignes de chaque côté).</summary>
    static (long P, long Q, long S16, int D) Setup(int a, int b, int t, int mode)
    {
        t = ((t % 180) + 180) % 180;
        if (a == b)
            t = 0;
        (long s, long c, bool neg) = t <= 90 ? (Sin[t], Sin[90 - t], false) : (Sin[180 - t], Sin[t - 90], true);
        long aS = a * s, bC = b * c;
        long a28 = aS * aS + bC * bC;
        long h14 = ISqrt(a28);
        long x = h14 == 0 ? ((a * c + b * s) >> 2) + 2048 : ((long)a * b << 26) / h14 + 2048;
        long y = (h14 >> 2) + 2048;
        long p = x * x;
        long q = (long)(((BigInteger)p << 24) / ((BigInteger)y * y));
        long s16 = 0;
        if (a28 != 0 && a != b)
        {
            BigInteger num = (BigInteger)Math.Abs((long)a * a - (long)b * b) * s * c << (15 + mode);
            s16 = (long)((2 * num + a28) / (2 * (BigInteger)a28));
            if ((a < b) != neg)
                s16 = -s16;
        }
        return (p, q, s16, (int)((h14 + 8192) >> 14));
    }

    /// <summary>Bords de chaque ligne (d compté vers le bas), par rapport au centre.</summary>
    static Dictionary<int, (int L, int R)> Spans(int a, int b, int t, int mode)
    {
        (long p, long q, long s16, int lines) = Setup(a, b, t, mode);
        BigInteger T = p - (BigInteger)q * lines * lines;
        long w = 0;
        var rows = new Dictionary<int, (int, int)>();
        for (int d = lines; d >= 0; d--)
        {
            long n = T > 0 ? (long)(T >> 16) : 0;
            while ((w + 1) * (w + 1) <= n)
                w++;
            long v = w << (11 + mode);
            long cd = s16 * d;
            int right = (int)((v - cd) >> 16);
            int left = -(int)((v + cd) >> 16);
            rows[d] = (left, right);
            rows[-d] = (-right, -left);
            T += (BigInteger)q * (2 * d - 1);
        }
        return rows;
    }

    /// <summary>L'ovale attendu : contour (prolongé jusqu'à la ligne voisine si elles ne se touchent pas) et intérieur.</summary>
    static void ReferenceEllipse(byte[] screen, int mode, int cx, int cy, int a, int b, int t, int pen, int? fill)
    {
        Dictionary<int, (int L, int R)> rows = Spans(a, b, t, mode);
        foreach ((int d, (int l, int r)) in rows)
        {
            bool hasUp = rows.TryGetValue(d - 1, out (int L, int R) up);
            bool hasDown = rows.TryGetValue(d + 1, out (int L, int R) down);
            int el = l, er = r, il = 1, ir = 0;
            foreach ((bool has, (int L, int R) n) in new[] { (hasUp, up), (hasDown, down) })
                if (has)
                {
                    el = Math.Min(el, n.R + 1);
                    er = Math.Max(er, n.L - 1);
                }
            if (hasUp && hasDown)
            {
                il = Math.Max(l + 1, Math.Max(up.L, down.L));
                ir = Math.Min(r - 1, Math.Min(up.R, down.R));
            }
            for (int x = el; x <= er; x++)
            {
                if (x >= il && x <= ir)
                {
                    if (fill is int f)
                        ReferencePixel(screen, mode, cx + x, cy + d, f);
                }
                else
                    ReferencePixel(screen, mode, cx + x, cy + d, pen);
            }
        }
    }

    static IEnumerable<(int X, int Y, int A, int B, int T)> Ellipses(int mode, int seed)
    {
        int w = Width[mode];
        var list = new List<(int, int, int, int, int)>
        {
            (w / 2, 100, 0, 0, 0), (w / 2, 100, 1, 1, 0), (w / 2, 100, 40, 20, 30), (w / 2, 100, 20, 40, 30),
            (w / 2, 100, 40, 20, 150), (w / 2, 100, 60, 25, 45), (w / 2, 100, 99, 99, 77), (w / 2, 100, 45, 0, 20),
            (w / 2, 100, 0, 45, 20), (w / 2, 100, 100, 1, 3), (w / 2, 100, 1000, 3, 1), (w / 2, 100, 1000, 1000, 45),
            (0, 0, 1000, 999, 89), (-32768, 100, 500, 400, 10), (32767, 100, 500, 400, 10), (100, -32768, 5, 5, 0),
            (-2900, 100, 1000, 800, 30), (w + 2900, 100, 1000, 800, 30), (100, -999, 800, 1000, 60), (100, 1198, 1000, 300, 120),
            (3, 3, 7, 2, -45), (w / 2, 100, 30, 15, -32768), (w / 2, 100, 30, 15, 32767), (w / 2, 100, 50, 30, 90),
            (w / 2, 100, 50, 30, 180), (w / 2, 100, 50, 30, 270), (w / 2, 100, 0, 0, 33)
        };
        var random = new Random(seed);
        for (int n = 0; n < 25; n++)
            list.Add((random.Next(-60, w + 60), random.Next(-60, 260), random.Next(0, 140), random.Next(0, 140), random.Next(-400, 400)));
        return list;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_ovale_est_trace_comme_la_reference_meme_hors_de_l_ecran(int mode)
    {
        var random = new Random(3 + mode);
        foreach ((int x, int y, int a, int b, int t) in Ellipses(mode, 20 + mode))
        {
            int pen = random.Next(16);
            var machine = new Machine(mode);
            byte[] expected = machine.Screen();
            ReferenceEllipse(expected, mode, x, y, a, b, t, pen, null);

            machine.Ellipse(x, y, a, b, t, pen, null);

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, ELLIPSE {x}, {y}, {a}, {b}, {pen}, , {t}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_ovale_rempli_a_son_interieur_dans_l_autre_stylo(int mode)
    {
        var random = new Random(9 + mode);
        foreach ((int x, int y, int a, int b, int t) in Ellipses(mode, 40 + mode))
        {
            int pen = random.Next(16);
            int fill = (pen + 1 + random.Next(15)) % 16;
            var machine = new Machine(mode);
            byte[] expected = machine.Screen();
            ReferenceEllipse(expected, mode, x, y, a, b, t, pen, fill);

            machine.Ellipse(x, y, a, b, t, pen, fill);

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, ELLIPSE {x}, {y}, {a}, {b}, {pen}, {fill}, {t}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Deux_rayons_egaux_donnent_exactement_le_rond_de_CIRCLE(int mode)
    {
        foreach (int r in new[] { 0, 1, 2, 3, 10, 37, 50, 99, 150, 1000 })
            foreach (int t in new[] { 0, 45, -100 })
                foreach (int? fill in new int?[] { null, 2 })
                {
                    var circle = new Machine(mode, true, RuntimeLibrary.CircleName, RuntimeLibrary.EllipseName);
                    circle.Circle(Width[mode] / 2, 100, r, 1, fill);
                    var ellipse = new Machine(mode, true, RuntimeLibrary.CircleName, RuntimeLibrary.EllipseName);
                    ellipse.Ellipse(Width[mode] / 2, 100, r, r, t, 1, fill);

                    Assert.True(circle.Screen().SequenceEqual(ellipse.Screen()), $"MODE {mode}, rayon {r}, angle {t}, {fill}");
                }
    }

    [Fact]
    public void Un_angle_et_le_meme_angle_plus_ou_moins_180_donnent_le_meme_ovale()
    {
        foreach ((int t1, int t2) in new[] { (10, 370), (10, 190), (-30, 150), (0, 180), (90, -90) })
        {
            var first = new Machine(1);
            first.Ellipse(160, 100, 60, 25, t1, 1, null);
            var second = new Machine(1);
            second.Ellipse(160, 100, 60, 25, t2, 1, null);

            Assert.True(first.Screen().SequenceEqual(second.Screen()), $"{t1} et {t2}");
        }
    }

    [Fact]
    public void Un_angle_positif_fait_monter_le_cote_droit()
    {
        Dictionary<int, (int L, int R)> rows = Spans(60, 20, 30, 1);
        int top = rows.Keys.Min();
        int bottom = rows.Keys.Max();
        Assert.True(rows[top].L > 0, "la ligne du haut est à droite du centre");
        Assert.True(rows[bottom].R < 0, "la ligne du bas est à gauche du centre");
    }

    [Fact]
    public void Le_meme_ovale_redessine_ailleurs_garde_son_calcul_et_reste_juste()
    {
        // Le calcul de départ est gardé quand les rayons, l'angle et le MODE ne changent pas : on vérifie l'enchaînement
        var machine = new Machine(1);
        byte[] expected = machine.Screen();
        foreach ((int x, int y, int a, int b, int t) in new[] { (100, 60, 50, 20, 30), (200, 120, 50, 20, 30), (150, 100, 50, 20, 31),
                                                                (60, 150, 50, 20, 30), (60, 150, 20, 50, 30), (250, 50, 20, 50, 30) })
        {
            ReferenceEllipse(expected, 1, x, y, a, b, t, 3, null);
            machine.Ellipse(x, y, a, b, t, 3, null);
            Assert.True(expected.SequenceEqual(machine.Screen()), $"ELLIPSE {x}, {y}, {a}, {b}, , , {t}");
        }
    }

    [Fact]
    public void Changer_de_MODE_refait_le_calcul()
    {
        var machine = new Machine(1);
        machine.Ellipse(80, 100, 50, 20, 30, 1, null);
        machine.Cpu.ScreenMode = 2;
        byte[] expected = machine.Screen();
        ReferenceEllipse(expected, 2, 300, 100, 50, 20, 30, 1, null);

        machine.Ellipse(300, 100, 50, 20, 30, 1, null);

        Assert.True(expected.SequenceEqual(machine.Screen()));
    }

    [Fact]
    public void ELLIPSE_remet_la_ROM_haute_dans_l_etat_ou_il_l_a_trouvee()
    {
        foreach (bool before in new[] { true, false })
        {
            var machine = new Machine(1, upperRom: before);

            machine.Ellipse(160, 100, 50, 20, 30, 1, 2);

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
    [InlineData("10 ELLIPSE 160,100,50,20")]
    [InlineData("10 ELLIPSE 160,100,50,20,1")]
    [InlineData("10 ELLIPSE 160,100,50,20,1,2")]
    [InlineData("10 ELLIPSE 160,100,50,20,1,2,30")]
    [InlineData("10 ELLIPSE 160,100,50,20,,,30")]
    [InlineData("10 ELLIPSE 160,100,50,20,,2")]
    [InlineData("10 ELLIPSE 160,100,50,20,1,,-45")]
    [InlineData("10 X=-5:R=0:ELLIPSE X*2,X+200,R,R+3,X+6,15,X*100")]
    [InlineData("10 ELLIPSE 0,0,1000,1000,,,32767")]
    [InlineData("10 IF 1 THEN ELLIPSE 1,2,3,4,,,5 ELSE PRINT")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Theory]
    [InlineData("10 ELLIPSE 160,100,50")]
    [InlineData("10 ELLIPSE 160,100,50,20,1,2,30,4")]
    [InlineData("10 ELLIPSE 160,100,50,20,")]
    [InlineData("10 ELLIPSE 160,100,50,20,,,")]
    [InlineData("10 ELLIPSE 160,100,1001,20")]
    [InlineData("10 ELLIPSE 160,100,50,-1")]
    [InlineData("10 ELLIPSE 160,100,50,20,16")]
    [InlineData("10 ELLIPSE 160,100,50,20,1,16")]
    [InlineData("10 ELLIPSE=5")]                     // nouveau mot réservé
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Theory]
    [InlineData("10 R=1001\n20 ELLIPSE 160,100,R,20")]
    [InlineData("10 R=-1\n20 ELLIPSE 160,100,20,R")]
    public void Un_rayon_calcule_hors_de_0_a_1000_donne_Improper_argument(string source)
    {
        Assert.Equal("Improper argument in 20\r\n", Run(source).Output.ToString());
    }

    [Fact]
    public void ELLIPSE_dans_un_programme_dessine_et_lie_le_module()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 R=40:ELLIPSE 160,100,R,20,1,2,30");
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin);
        Assert.Contains(RuntimeLibrary.EllipseName, program.Modules);
        Assert.Contains(RuntimeLibrary.PixelName, program.Modules);

        MiniZ80 cpu = Run("10 R=40:ELLIPSE 160,100,R,20,1,2,30");
        Assert.Equal("", cpu.Output.ToString());
        Assert.True(cpu.UpperRomEnabled);
    }

    [Fact]
    public void Sans_ELLIPSE_le_module_n_est_pas_lie()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 CIRCLE 50,50,5").Program, Origin);
        Assert.DoesNotContain(RuntimeLibrary.EllipseName, program.Modules);
    }
}
