// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D198 : sprites (DEFSPR, SPRITE n, x, y, SPRITE n).</summary>
public class SpriteTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => Compiler.CompileToDisk(source, "SPRITE").Diagnostics;

    [Fact]
    public void Un_programme_avec_des_sprites_se_compile()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 MODE 1\n20 DEFSPR 1,\"..33..\",\".3333.\"\n30 DEFSPR 1,\"333333\",\"3.33.3\"\n40 SPRITE 1,100,50:SPRITE 1,104,52\n50 SPRITE 1", "SPRITE");

        Assert.True(result.Success);
        Assert.Contains(RuntimeLibrary.SpriteName, result.Modules);
    }

    [Theory]
    [InlineData("10 DEFSPR 0,\"3\"", DiagnosticCode.ValueOutOfRange)]                  // numéro de 1 à 16
    [InlineData("10 DEFSPR 1,\"3G\"", DiagnosticCode.SpriteRow)]                      // G n'est pas un stylo
    [InlineData("10 DEFSPR 1,\"33333333333333333\"", DiagnosticCode.SpriteRow)]       // 17 points
    [InlineData("10 DEFSPR 1", DiagnosticCode.SpriteDefinition)]                      // pas de dessin
    [InlineData("10 DEFSPR 1,A$", DiagnosticCode.SpriteDefinition)]                   // texte écrit en clair seulement
    [InlineData("10 DEFSPR 1,\"33\"\n20 DEFSPR 1,\"333\"", DiagnosticCode.SpriteSize)] // largeurs différentes
    [InlineData("10 SPRITE 1,2", DiagnosticCode.WrongArgumentCount)]                  // 1 ou 3 valeurs
    public void Les_erreurs_de_DEFSPR_et_SPRITE(string source, DiagnosticCode expected)
    {
        Assert.Contains(Errors(source), d => d.Code == expected);
    }

    [Fact]
    public void Plus_de_16_lignes_est_refuse()
    {
        string lines = string.Join("\n", Enumerable.Range(1, 9).Select(i => $"{i * 10} DEFSPR 2,\"3.\",\".3\""));

        Assert.Contains(Errors(lines), d => d.Code == DiagnosticCode.SpriteSize);
        Assert.Empty(Errors(string.Join("\n", Enumerable.Range(1, 8).Select(i => $"{i * 10} DEFSPR 2,\"3.\",\".3\""))));
    }

    [Fact]
    public void L_image_est_preparee_pour_les_trois_MODE()
    {
        (byte[] blocks, int background, int cache) = SpriteRuntime.Build(["3."]);

        // MODE 0 : 1 octet ; stylo 3 au point de gauche = &88, le décor garde le point de droite (&55)
        Assert.Equal(new byte[] { 1, 0x88, 0x55 }, blocks[0..3]);
        // MODE 1 : stylo 3 = &88, décor gardé &77 ; MODE 2 : stylo 3 -> 1 seul bit, &80, décor &7F
        Assert.Equal(new byte[] { 1, 0x88, 0x77 }, blocks[3..6]);
        Assert.Equal(new byte[] { 1, 0x80, 0x7F }, blocks[6..9]);
        Assert.Equal(2, background);                          // (1 octet + 1) x 1 ligne
        Assert.Equal(28, cache);                              // MODE 2 : 7 décalages x 2 octets x 1 ligne x 2
    }

    // le module seul, sur le mini-Z80, avec une description posée en &8000 (décor gardé en &8800)
    sealed class Machine
    {
        public readonly MiniZ80 Cpu = new() { UpperRomEnabled = false };
        public readonly Z80Emitter Emitter = new(0x4000);

        public Machine(byte mode, IReadOnlyList<string> rows)
        {
            foreach (RuntimeModule m in Linker.Resolve([RuntimeLibrary.SpriteName]))
                m.Emit(Emitter);
            Cpu.Load(Emitter.ToBytes(), 0x4000);
            Cpu.ScreenMode = mode;
            (byte[] blocks, _, _) = SpriteRuntime.Build(rows);
            byte[] description = [(byte)rows[0].Length, (byte)rows.Count, 0x00, 0x88, 0x00, 0x90, 0xFF, 0, .. blocks];
            description.CopyTo(Cpu.Memory, 0x8000);
            var random = new Random(5);
            for (int address = 0xC000; address < 0x10000; address++)
                Cpu.Memory[address] = (byte)random.Next(256);
            Cpu.A = 1;
            Cpu.H = 0x80;
            Cpu.L = 0x00;
            Cpu.Call(Emitter.AddressOf(SpriteRuntime.DefLabel));
        }

        void Word(string label, int value)
        {
            int address = Emitter.AddressOf(label);
            Cpu.Memory[address] = (byte)value;
            Cpu.Memory[address + 1] = (byte)(value >> 8);
        }

        // un autre sprite : sa description en « at », son décor en at + &800, ses images décalées en at + &900
        public void Define(int number, IReadOnlyList<string> rows, int at)
        {
            (byte[] blocks, _, _) = SpriteRuntime.Build(rows);
            byte[] description = [(byte)rows[0].Length, (byte)rows.Count, 0x00, (byte)((at >> 8) + 8), 0x00, (byte)((at >> 8) + 9), 0xFF, 0, .. blocks];
            description.CopyTo(Cpu.Memory, at);
            Cpu.A = (byte)number;
            Cpu.H = (byte)(at >> 8);
            Cpu.L = (byte)at;
            Cpu.Call(Emitter.AddressOf(SpriteRuntime.DefLabel));
        }

        public int Collide(int a, int b)
        {
            Cpu.H = 0;
            Cpu.L = (byte)a;
            Cpu.D = 0;
            Cpu.E = (byte)b;
            Cpu.Call(Emitter.AddressOf(SpriteRuntime.CollideLabel));
            return Cpu.H << 8 | Cpu.L;
        }

        public void Show(int x, int y, int number = 1)
        {
            Cpu.Memory[Emitter.AddressOf(SpriteRuntime.NumberLabel)] = (byte)number;
            Word(SpriteRuntime.XLabel, x);
            Word(SpriteRuntime.YLabel, y);
            Cpu.Call(Emitter.AddressOf(SpriteRuntime.ShowLabel));
        }

        public void Hide(int number = 1)
        {
            Cpu.Memory[Emitter.AddressOf(SpriteRuntime.NumberLabel)] = (byte)number;
            Cpu.Call(Emitter.AddressOf(SpriteRuntime.HideLabel));
        }

        public byte[] Screen() => Cpu.Memory[0xC000..];
    }

    static int Address(int xByte, int y) => 0xC000 + y / 8 * 80 + y % 8 * 0x800 + xByte;

    [Fact]
    public void Le_sprite_se_pose_sur_le_decor_puis_le_rend()
    {
        var machine = new Machine(1, ["3..3", ".33."]);
        byte[] before = machine.Screen();
        int first = Address(1, 9), second = Address(1, 10);
        byte decor1 = machine.Cpu.Memory[first], decor2 = machine.Cpu.Memory[second];

        machine.Show(4, 9);                                   // MODE 1 : x = 4 -> octet 1 ; y = 9 -> 2e ligne de caractères

        // ligne 1 : points 0 et 3 au stylo 3 (&88 et &11), les deux autres gardent le décor
        Assert.Equal((decor1 & 0x66) | 0x99, machine.Cpu.Memory[first]);
        Assert.Equal((decor2 & 0x99) | 0x66, machine.Cpu.Memory[second]);
        machine.Hide();
        Assert.Equal(before, machine.Screen());               // le décor revient, octet pour octet
    }

    [Fact]
    public void Deplacer_le_sprite_remet_le_decor_a_l_ancienne_place()
    {
        var machine = new Machine(0, ["FF"]);
        byte[] before = machine.Screen();
        machine.Show(0, 0);
        machine.Show(10, 100);                                // MODE 0 : x = 10 -> octet 5

        Assert.Equal(before[0], machine.Cpu.Memory[0xC000]);  // l'ancienne place est rendue
        Assert.Equal(0xFF, machine.Cpu.Memory[Address(5, 100)]);   // stylo 15 sur les deux points
        machine.Hide();
        Assert.Equal(before, machine.Screen());
    }

    [Fact]
    public void Un_sprite_qui_sort_de_l_ecran_n_est_pas_montre()
    {
        var machine = new Machine(1, ["33"]);
        byte[] before = machine.Screen();

        machine.Show(-4, 10);
        machine.Show(320, 10);                                // dépasse à droite (octet 80)
        machine.Show(10, 200);                                // dépasse en bas

        Assert.Equal(before, machine.Screen());
    }

    [Fact]
    public void Le_sprite_se_place_au_point_pres()
    {
        var machine = new Machine(1, ["33"]);
        byte[] before = machine.Screen();
        int first = Address(0, 0), second = Address(1, 0);
        byte decor1 = machine.Cpu.Memory[first], decor2 = machine.Cpu.Memory[second];

        machine.Show(3, 0);                                   // MODE 1 : points 3 et 4 -> à cheval sur deux octets

        Assert.Equal((decor1 & 0xEE) | 0x11, machine.Cpu.Memory[first]);    // dernier point du 1er octet
        Assert.Equal((decor2 & 0x77) | 0x88, machine.Cpu.Memory[second]);   // premier point du 2e octet
        machine.Show(5, 0);                                   // décalé d'un point : points 5 et 6 dans le 2e octet
        Assert.Equal(before[0], machine.Cpu.Memory[first]);
        Assert.Equal((decor2 & 0x99) | 0x66, machine.Cpu.Memory[second]);
        machine.Show(3, 0);                                   // de nouveau : l'image décalée gardée ressert
        Assert.Equal((decor1 & 0xEE) | 0x11, machine.Cpu.Memory[first]);
        machine.Hide();
        Assert.Equal(before, machine.Screen());
    }

    [Fact]
    public void COLLIDE_dit_si_deux_sprites_se_touchent()
    {
        var machine = new Machine(1, ["3333"]);               // sprite 1 : 4 points sur 1 ligne
        machine.Define(2, ["33"], 0xA000);                    // sprite 2 : 2 points
        machine.Show(100, 50);

        machine.Show(104, 50, 2);                             // juste à côté : ne touche pas
        Assert.Equal(0, machine.Collide(1, 2));
        machine.Show(103, 50, 2);                             // un point en commun
        Assert.Equal(0xFFFF, machine.Collide(1, 2));
        Assert.Equal(0xFFFF, machine.Collide(2, 1));
        machine.Show(103, 51, 2);                             // une ligne plus bas
        Assert.Equal(0, machine.Collide(1, 2));
        machine.Show(103, 50, 2);
        machine.Hide(2);                                      // caché : plus de choc
        Assert.Equal(0, machine.Collide(1, 2));
        Assert.Equal(0, machine.Collide(1, 3));               // jamais défini
    }

    [Fact]
    public void Un_programme_avec_COLLIDE_se_compile()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 DEFSPR 1,\"33\":DEFSPR 2,\"44\"\n20 SPRITE 1,10,10:SPRITE 2,11,10\n30 IF COLLIDE(1,2) THEN PRINT \"BOUM\"", "SPRITE");

        Assert.True(result.Success);
    }
}
