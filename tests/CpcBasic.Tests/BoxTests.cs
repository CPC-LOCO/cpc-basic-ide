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
/// Tracé direct dans la mémoire écran (D048) : les modules Pixel et Box, essayés sur le mini-Z80.
/// La façon dont le CPC range un point en mémoire est écrite à part (ReferencePixel), sans réutiliser
/// les tables du module, pour que les deux se contrôlent l'une l'autre.
/// </summary>
public class PixelModuleTests
{
    const int Origin = 0x4000;
    static readonly int[] Width = [160, 320, 640];

    /// <summary>Un CPC de test : les modules demandés (avec leurs dépendances), un écran rempli au hasard, le MODE choisi.</summary>
    sealed class Machine
    {
        public readonly MiniZ80 Cpu = new();
        public readonly Z80Emitter Emitter = new(Origin);

        public Machine(int mode, string module)
        {
            foreach (RuntimeModule m in Linker.Resolve([module]))
                m.Emit(Emitter);

            Cpu.Load(Emitter.ToBytes(), Origin);
            Cpu.ScreenMode = (byte)mode;
            Cpu.UpperRomEnabled = false;   // les essais de bas niveau jouent le rôle de la commande qui a coupé la ROM

            var random = new Random(7);
            for (int address = 0xC000; address < 0x10000; address++)
                Cpu.Memory[address] = (byte)random.Next(256);
        }

        public void Call(string label) => Cpu.Call(Emitter.AddressOf(label));

        public void SetWord(string label, int value)
        {
            int address = Emitter.AddressOf(label);
            Cpu.Memory[address] = (byte)(value & 0xFF);
            Cpu.Memory[address + 1] = (byte)((value >> 8) & 0xFF);
        }

        public void SetByte(string label, int value) => Cpu.Memory[Emitter.AddressOf(label)] = (byte)value;

        /// <summary>Copie de l'écran : l'octet 0 est à l'adresse &C000.</summary>
        public byte[] Screen() => Cpu.Memory[0xC000..];

        public void Setup(int pen)
        {
            Cpu.A = (byte)pen;
            Call("rt_px_setup");
        }
    }

    static void ReferencePixel(byte[] screen, int mode, int x, int y, int pen)
    {
        if (x < 0 || x >= Width[mode] || y < 0 || y >= 200)
            return;

        int pixelsPerByte = 2 << mode;                           // 2, 4 ou 8
        int offset = (y / 8) * 80 + (y % 8) * 0x800 + x / pixelsPerByte;
        int p = x % pixelsPerByte;                               // rang du point dans l'octet, 0 = à gauche
        pen &= 15;

        // (bit de l'octet, bit du stylo) : le CPC entrelace les bits du stylo dans l'octet
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

    /// <summary>Intérieur avec le stylo de remplissage (s'il y en a un), puis contour : écrit à part, point par point.</summary>
    static void ReferenceBox(byte[] screen, int mode, int x1, int y1, int x2, int y2, int pen, int? fillPen = null)
    {
        int left = Math.Min(x1, x2), right = Math.Max(x1, x2);
        int top = Math.Min(y1, y2), bottom = Math.Max(y1, y2);

        if (fillPen is int fill)
        {
            for (int y = Math.Max(top + 1, 0); y <= Math.Min(bottom - 1, 199); y++)
                for (int x = Math.Max(left + 1, 0); x <= Math.Min(right - 1, Width[mode] - 1); x++)
                    ReferencePixel(screen, mode, x, y, fill);
        }

        for (int x = Math.Max(left, -1); x <= Math.Min(right, Width[mode]); x++)
        {
            ReferencePixel(screen, mode, x, top, pen);
            ReferencePixel(screen, mode, x, bottom, pen);
        }
        for (int y = Math.Max(top, -1); y <= Math.Min(bottom, 200); y++)
        {
            ReferencePixel(screen, mode, left, y, pen);
            ReferencePixel(screen, mode, right, y, pen);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_point_est_range_comme_le_fait_le_CPC(int mode)
    {
        int w = Width[mode];
        var points = new List<(int X, int Y)> { (0, 0), (w - 1, 0), (0, 199), (w - 1, 199), (1, 1), (w - 2, 198) };
        var random = new Random(1);
        for (int n = 0; n < 250; n++)
            points.Add((random.Next(w), random.Next(200)));

        foreach ((int x, int y) in points)
        {
            int pen = random.Next(16);
            var machine = new Machine(mode, RuntimeLibrary.PixelName);
            byte[] expected = machine.Screen();
            ReferencePixel(expected, mode, x, y, pen);

            machine.Setup(pen);
            machine.Cpu.HL = (ushort)x;
            machine.Cpu.DE = (ushort)y;
            machine.Call("rt_pixel");

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, point ({x}, {y}), stylo {pen}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_point_hors_de_l_ecran_ne_change_rien(int mode)
    {
        var points = new (int X, int Y)[]
        {
            (-1, 0), (0, -1), (Width[mode], 0), (0, 200), (0, 256), (32767, 5), (5, 32767), (-32768, -32768), (Width[mode] + 100, 300)
        };

        foreach ((int x, int y) in points)
        {
            var machine = new Machine(mode, RuntimeLibrary.PixelName);
            byte[] before = machine.Screen();

            machine.Setup(3);
            machine.Cpu.HL = unchecked((ushort)x);
            machine.Cpu.DE = unchecked((ushort)y);
            machine.Call("rt_pixel");

            Assert.True(before.SequenceEqual(machine.Screen()), $"MODE {mode}, point ({x}, {y})");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Une_ligne_horizontale_est_tracee_par_octets_entiers(int mode)
    {
        int w = Width[mode];
        var lines = new List<(int X1, int X2, int Y)> { (0, w - 1, 0), (0, 0, 5), (w - 1, w - 1, 199), (3, 4, 100), (0, w - 1, 199) };
        var random = new Random(3);
        for (int n = 0; n < 150; n++)
        {
            int x1 = random.Next(w);
            lines.Add((x1, random.Next(x1, w), random.Next(200)));
        }

        foreach ((int x1, int x2, int y) in lines)
        {
            int pen = random.Next(16);
            var machine = new Machine(mode, RuntimeLibrary.PixelName);
            byte[] expected = machine.Screen();
            for (int x = x1; x <= x2; x++)
                ReferencePixel(expected, mode, x, y, pen);

            machine.Setup(pen);
            machine.Cpu.DE = (ushort)x1;
            machine.Cpu.BC = (ushort)x2;
            machine.Cpu.HL = (ushort)y;
            machine.Call("rt_px_hline");

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, ligne de {x1} à {x2} en y = {y}, stylo {pen}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Une_ligne_verticale_descend_ligne_par_ligne_meme_entre_deux_lignes_de_caracteres(int mode)
    {
        int w = Width[mode];
        var lines = new List<(int X, int Y1, int Y2)> { (0, 0, 199), (0, 0, 0), (w - 1, 199, 199), (7, 7, 9), (w - 1, 0, 199) };
        var random = new Random(4);
        for (int n = 0; n < 150; n++)
        {
            int y1 = random.Next(200);
            lines.Add((random.Next(w), y1, random.Next(y1, 200)));
        }

        foreach ((int x, int y1, int y2) in lines)
        {
            int pen = random.Next(16);
            var machine = new Machine(mode, RuntimeLibrary.PixelName);
            byte[] expected = machine.Screen();
            for (int y = y1; y <= y2; y++)
                ReferencePixel(expected, mode, x, y, pen);

            machine.Setup(pen);
            machine.Cpu.DE = (ushort)x;
            machine.Cpu.HL = (ushort)y1;
            machine.Cpu.BC = (ushort)y2;
            machine.Call("rt_px_vline");

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, colonne {x}, de {y1} à {y2}, stylo {pen}");
        }
    }

    static Machine BoxMachine(int mode, int x1, int y1, int x2, int y2, int pen, bool upperRom, int? fillPen = null)
    {
        var machine = new Machine(mode, RuntimeLibrary.BoxName);
        machine.Cpu.UpperRomEnabled = upperRom;
        machine.SetWord(RuntimeLibrary.BoxX1Label, x1);
        machine.SetWord(RuntimeLibrary.BoxY1Label, y1);
        machine.SetWord(RuntimeLibrary.BoxX2Label, x2);
        machine.SetWord(RuntimeLibrary.BoxY2Label, y2);
        machine.SetByte(RuntimeLibrary.BoxPenLabel, pen);
        machine.SetByte(RuntimeLibrary.BoxFillPenLabel, fillPen ?? 0);
        machine.SetByte(RuntimeLibrary.BoxDoFillLabel, fillPen is null ? 0 : 1);
        return machine;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_rectangle_est_trace_meme_avec_les_coins_inverses_ou_hors_de_l_ecran(int mode)
    {
        int w = Width[mode];
        var boxes = new List<(int X1, int Y1, int X2, int Y2)>
        {
            (0, 0, w - 1, 199), (w - 1, 199, 0, 0), (5, 5, 5, 5), (3, 10, 9, 10), (10, 20, 10, 100),
            (-20, -20, w + 20, 220), (-50, 100, w + 50, 120), (-5, -5, 20, 30), (w - 20, 150, w + 30, 250)
        };
        var random = new Random(2);
        for (int n = 0; n < 40; n++)
            boxes.Add((random.Next(-10, w + 10), random.Next(-10, 210), random.Next(-10, w + 10), random.Next(-10, 210)));

        foreach ((int x1, int y1, int x2, int y2) in boxes)
        {
            int pen = random.Next(16);
            // La ROM haute est active au départ, comme dans un programme lancé depuis le BASIC : BOX doit la couper lui-même.
            Machine machine = BoxMachine(mode, x1, y1, x2, y2, pen, upperRom: true);
            byte[] expected = machine.Screen();
            ReferenceBox(expected, mode, x1, y1, x2, y2, pen);

            machine.Call(RuntimeLibrary.BoxLabel);

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, BOX {x1}, {y1}, {x2}, {y2}, stylo {pen}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Un_rectangle_avec_remplissage_a_son_interieur_dans_un_autre_stylo_meme_hors_de_l_ecran_ou_tres_fin(int mode)
    {
        int w = Width[mode];
        var boxes = new List<(int X1, int Y1, int X2, int Y2)>
        {
            (0, 0, w - 1, 199), (w - 1, 199, 0, 0), (5, 5, 5, 5), (3, 10, 9, 10), (10, 20, 10, 100), (3, 8, 4, 9),
            (10, 10, 11, 20), (10, 10, 12, 12), (10, 10, 13, 13), (0, 0, 2, 199), (0, 0, w - 1, 2),
            (-20, -20, w + 20, 220), (-50, 100, w + 50, 120), (-5, -5, 20, 30), (w - 20, 150, w + 30, 250),
            (-30, -30, -10, -10), (w + 5, 20, w + 40, 60), (20, 250, 30, 300), (-1, -1, w, 200), (-1, 0, w, 199),
            // coins aux limites des nombres de 16 bits : aucun dépassement
            (-32768, -32768, 32767, 32767), (32767, 32767, -32768, -32768), (32766, 5, 32767, 150), (-32768, -32767, -32767, 50)
        };
        var random = new Random(5);
        for (int n = 0; n < 40; n++)
            boxes.Add((random.Next(-10, w + 10), random.Next(-10, 210), random.Next(-10, w + 10), random.Next(-10, 210)));

        foreach ((int x1, int y1, int x2, int y2) in boxes)
        {
            int pen = random.Next(16);
            int fill = (pen + 1 + random.Next(15)) % 16;   // toujours un autre stylo que le contour
            Machine machine = BoxMachine(mode, x1, y1, x2, y2, pen, upperRom: true, fill);
            byte[] expected = machine.Screen();
            ReferenceBox(expected, mode, x1, y1, x2, y2, pen, fill);

            machine.Call(RuntimeLibrary.BoxLabel);

            Assert.True(expected.SequenceEqual(machine.Screen()), $"MODE {mode}, BOX {x1}, {y1}, {x2}, {y2}, stylo {pen}, remplissage {fill}");
        }
    }

    [Fact]
    public void BOX_avec_remplissage_remet_aussi_la_ROM_haute_dans_l_etat_ou_il_l_a_trouvee()
    {
        foreach (bool before in new[] { true, false })
        {
            Machine machine = BoxMachine(1, 0, 0, 319, 199, 1, upperRom: before, fillPen: 3);

            machine.Call(RuntimeLibrary.BoxLabel);

            Assert.Equal(before, machine.Cpu.UpperRomEnabled);
        }
    }

    [Fact]
    public void BOX_remet_la_ROM_haute_dans_l_etat_ou_il_l_a_trouvee()
    {
        foreach (bool before in new[] { true, false })
        {
            Machine machine = BoxMachine(1, 0, 0, 319, 199, 1, upperRom: before);

            machine.Call(RuntimeLibrary.BoxLabel);

            Assert.Equal(before, machine.Cpu.UpperRomEnabled);
        }
    }
}

/// <summary>BOX dans le langage : analyse, messages d'erreur et octets produits (D048).</summary>
public class BoxCommandTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult result = BasicAnalyzer.Analyze(source);
        Assert.Empty(result.Diagnostics);
        return result.Program;
    }

    static GeneratedProgram Generate(string source) => CodeGenerator.Generate(Parse(source), 0x4000);

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    [Fact]
    public void BOX_est_reconnu_avec_quatre_cinq_ou_six_valeurs()
    {
        BasicProgram program = Parse("10 BOX 1, 2, 3, 4\n20 BOX 1, 2, 3, 4, 2\n30 BOX 1, 2, 3, 4, 2, 1");

        var boxes = program.Statements.OfType<ScreenStatement>().ToList();
        Assert.All(boxes, s => Assert.Equal("BOX", s.Command));
        Assert.Equal(new[] { 4, 5, 6 }, boxes.Select(s => s.Arguments.Count).ToArray());
    }

    [Theory]
    [InlineData("10 BOX")]
    [InlineData("10 BOX 1, 2, 3")]
    [InlineData("10 BOX 1, 2, 3, 4, 5, 1, 1")]
    public void Un_mauvais_nombre_de_valeurs_est_refuse(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Fact]
    public void Un_stylo_hors_limites_est_refuse_avec_un_message_clair()
    {
        Diagnostic error = Assert.Single(Errors("10 BOX 1, 2, 3, 4, 16"));

        Assert.Equal("ERREUR en ligne 1 : BOX : la valeur 16 n'est pas permise. Valeurs possibles : 0 à 15.", error.Message);
    }

    [Fact]
    public void Un_stylo_de_remplissage_hors_limites_est_refuse_avec_un_message_clair()
    {
        Diagnostic error = Assert.Single(Errors("10 BOX 1, 2, 3, 4, 1, 16"));

        Assert.Equal("ERREUR en ligne 1 : BOX : la valeur 16 n'est pas permise. Valeurs possibles : 0 à 15.", error.Message);
    }

    [Fact]
    public void Un_coin_ecrit_en_nombre_doit_tenir_sur_16_bits()
    {
        Assert.Contains(Errors("10 BOX 40000, 0, 1, 1"), e => e.Code == DiagnosticCode.NumberOutOfRange);
    }

    [Fact]
    public void Les_valeurs_peuvent_etre_negatives_variables_ou_calculees()
    {
        Assert.Empty(Errors("10 X = 5\n20 BOX -10, -X, X * 2, 199 + X, X"));
    }

    [Fact]
    public void Une_variable_utilisee_dans_BOX_compte_comme_utilisee_avant_BYTE()
    {
        Assert.Contains(Errors("10 BOX X, 1, 2, 3\n20 BYTE X"), e => e.Code == DiagnosticCode.DeclaredAfterUse);
    }

    [Fact]
    public void BOX_a_quatre_valeurs_prend_le_stylo_courant_du_trace_et_ne_remplit_pas()
    {
        byte[] code = Generate("10 BOX 1, 2, 3, 4").Code;

        // LD HL,1 / LD (x1),HL / LD HL,2 / LD (y1),HL / LD HL,3 / LD (x2),HL / LD HL,4 / LD (y2),HL
        Assert.Equal(new byte[] { 0x21, 0x01, 0x00, 0x22 }, code.Skip(0).Take(4).ToArray());
        Assert.Equal(new byte[] { 0x21, 0x02, 0x00, 0x22 }, code.Skip(6).Take(4).ToArray());
        Assert.Equal(new byte[] { 0x21, 0x03, 0x00, 0x22 }, code.Skip(12).Take(4).ToArray());
        Assert.Equal(new byte[] { 0x21, 0x04, 0x00, 0x22 }, code.Skip(18).Take(4).ToArray());
        // CALL &BBE1 (GRA GET PEN) / LD (rt_box_pen),A
        Assert.Equal(new byte[] { 0xCD, 0xE1, 0xBB, 0x32 }, code.Skip(24).Take(4).ToArray());
        // LD HL,0 / LD A,L / LD (rt_box_dofill),A : pas de remplissage, puis CALL rt_box
        Assert.Equal(new byte[] { 0x21, 0x00, 0x00, 0x7D, 0x32 }, code.Skip(30).Take(5).ToArray());
        Assert.Equal(0xCD, code[37]);
    }

    [Fact]
    public void BOX_a_cinq_valeurs_charge_le_stylo_du_contour_et_ne_remplit_pas()
    {
        byte[] code = Generate("10 BOX 1, 2, 3, 4, 3").Code;

        // LD HL,3 / LD A,L / LD (rt_box_pen),A
        Assert.Equal(new byte[] { 0x21, 0x03, 0x00, 0x7D, 0x32 }, code.Skip(24).Take(5).ToArray());
        // LD HL,0 / LD A,L / LD (rt_box_dofill),A, puis CALL rt_box
        Assert.Equal(new byte[] { 0x21, 0x00, 0x00, 0x7D, 0x32 }, code.Skip(31).Take(5).ToArray());
        Assert.Equal(0xCD, code[38]);
    }

    [Fact]
    public void BOX_a_six_valeurs_range_les_deux_stylos_et_demande_le_remplissage()
    {
        byte[] code = Generate("10 BOX 1, 2, 3, 4, 3, 5").Code;

        // contour : LD HL,3 / LD A,L / LD (rt_box_pen),A
        Assert.Equal(new byte[] { 0x21, 0x03, 0x00, 0x7D, 0x32 }, code.Skip(24).Take(5).ToArray());
        // intérieur : LD HL,5 / LD A,L / LD (rt_box_fillpen),A
        Assert.Equal(new byte[] { 0x21, 0x05, 0x00, 0x7D, 0x32 }, code.Skip(31).Take(5).ToArray());
        // LD HL,1 / LD A,L / LD (rt_box_dofill),A, puis CALL rt_box
        Assert.Equal(new byte[] { 0x21, 0x01, 0x00, 0x7D, 0x32 }, code.Skip(38).Take(5).ToArray());
        Assert.Equal(0xCD, code[45]);
    }

    [Fact]
    public void BOX_lie_les_modules_Box_Pixel_Min16_et_Max16()
    {
        GeneratedProgram program = Generate("10 BOX 0, 0, 10, 10");

        Assert.Equal(new[] { "Box", "Max16", "Min16", "Pixel" }, program.Modules.ToArray());
    }

    [Fact]
    public void Sans_BOX_aucun_module_de_trace_direct_n_est_lie()
    {
        GeneratedProgram program = Generate("10 MODE 1\n20 PLOT 1, 1\n30 DRAW 5, 5");

        Assert.Empty(program.Modules);
    }
}
