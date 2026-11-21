// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D182 : notre système pour la cartouche GX4000 : jeu de caractères, TXT OUTPUT, LOCATE, MODE.</summary>
public class Gx4000SystemTests
{
    // la cartouche chargée en &0000 (lue comme de la mémoire), l'écran lisible (pas de ROM haute)
    static MiniZ80 Cpu(byte[]? program = null)
    {
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Load(Gx4000System.SystemCode(program?.Length ?? 0), 0);
        return cpu;
    }

    static void Mode(MiniZ80 cpu, byte mode)
    {
        cpu.A = mode;
        cpu.Call(Gx4000System.SetModeEntry);
    }

    static void Print(MiniZ80 cpu, string text)
    {
        foreach (char ch in text)
        {
            cpu.A = (byte)ch;
            cpu.Call(Gx4000System.TxtOutputEntry);
        }
    }

    [Fact]
    public void Le_jeu_de_caracteres_a_95_signes_et_une_colonne_vide_a_droite()
    {
        byte[] font = Gx4000Font.Bytes();

        Assert.Equal(95 * 8, font.Length);
        Assert.All(font, b => Assert.Equal(0, b & 1));          // 8e colonne toujours vide
        Assert.Equal(new byte[] { 0x38, 0x6C, 0xC6, 0xC6, 0xFE, 0xC6, 0xC6, 0x00 }, font.AsSpan((65 - 32) * 8, 8).ToArray());   // A
    }

    [Fact]
    public void Les_entrees_fixes_sont_des_sauts()
    {
        byte[] code = Gx4000System.SystemCode(0);

        Assert.Equal(0xF3, code[0]);                            // DI
        Assert.Equal(new byte[] { 0xFB, 0xC9 }, code[0x38..0x3A]);   // interruption : EI, RET
        foreach (int entry in new[] { Gx4000System.TxtOutputEntry, Gx4000System.SetModeEntry,
                                      Gx4000System.SetCursorEntry, Gx4000System.GetCursorEntry })
            Assert.Equal(0xC3, code[entry]);
    }

    [Fact]
    public void Un_A_en_MODE_1_en_haut_a_gauche()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Print(cpu, "A");

        // ligne 1 de A : ..###.. = &38 -> octets &30 et &80 (stylo 1) ; ligne 2 : .##.##. = &6C -> &60 et &C0
        Assert.Equal(new byte[] { 0x30, 0x80 }, cpu.Memory[0xC000..0xC002]);
        Assert.Equal(new byte[] { 0x60, 0xC0 }, cpu.Memory[0xC800..0xC802]);
        Assert.Equal(1, cpu.Memory[Gx4000System.Variables]);   // colonne suivante
    }

    [Fact]
    public void Un_A_en_MODE_2_et_en_MODE_0()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Print(cpu, "A");
        Assert.Equal(0x38, cpu.Memory[0xC000]);

        Mode(cpu, 0);
        Print(cpu, "A");
        // &38 = 0011 1000 -> points par paires : 00, 11, 10, 00 -> &00, &C0, &80, &00
        Assert.Equal(new byte[] { 0x00, 0xC0, 0x80, 0x00 }, cpu.Memory[0xC000..0xC004]);
    }

    [Fact]
    public void LOCATE_place_le_caractere_et_le_curseur_se_relit()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        cpu.H = 5;                                            // colonne 5, ligne 3
        cpu.L = 3;
        cpu.Call(Gx4000System.SetCursorEntry);
        Print(cpu, "A");

        Assert.Equal(0x30, cpu.Memory[0xC000 + 2 * 80 + 4 * 2]);
        cpu.Call(Gx4000System.GetCursorEntry);
        Assert.Equal(6, cpu.H);
        Assert.Equal(3, cpu.L);
    }

    [Fact]
    public void Retour_a_la_ligne_et_fin_de_ligne()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 0);                                         // 20 caractères par ligne
        Print(cpu, new string('A', 21));

        Assert.Equal((byte)1, cpu.Memory[Gx4000System.Variables + 1]);   // le 21e est sur la ligne 2
        Assert.Equal(0xC0, cpu.Memory[0xC000 + 80 + 1]);
        Print(cpu, "\r\n");
        Assert.Equal(0, cpu.Memory[Gx4000System.Variables]);
        Assert.Equal(2, cpu.Memory[Gx4000System.Variables + 1]);
    }

    [Fact]
    public void En_bas_de_l_ecran_tout_remonte_d_une_ligne()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Print(cpu, "A");                                      // ligne 1
        for (int i = 0; i < 25; i++)
            Print(cpu, "\r\n");

        Assert.Equal(0, cpu.Memory[0xC000]);                  // le A est sorti par le haut
        Assert.Equal((byte)24, cpu.Memory[Gx4000System.Variables + 1]);
    }

    [Fact]
    public void Un_programme_compile_va_sur_la_cartouche()
    {
        var (errors, cartridge, size) = Compiler.CompileToCartridge("10 MODE 1:LOCATE 5,5:PRINT \"BONJOUR\"");

        Assert.Empty(errors);
        Assert.NotNull(cartridge);
        Assert.Equal(1, cartridge!.PageCount);
        Assert.True(size > 0);
    }
}
