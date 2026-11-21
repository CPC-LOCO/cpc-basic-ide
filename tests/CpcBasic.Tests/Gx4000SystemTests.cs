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
        CartridgeResult result = Compiler.CompileToCartridge("10 MODE 1:LOCATE 5,5:PRINT \"BONJOUR\"");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
        Assert.Equal(1, result.Cartridge!.PageCount);
        Assert.True(result.BinarySize > 0);
        Assert.Empty(result.Notes);
    }

    // ---------- manettes (D183) ----------

    // ports lus par la routine : &F449 (ligne 9, manette 1) et &F446 (ligne 6, manette 2) ; bit à 0 = appuyé
    static MiniZ80 Pads(byte pad1, byte pad2 = 0, IEnumerable<Gx4000System.KeyLink>? extra = null)
    {
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Load(Gx4000System.SystemCode(0, extra), 0);
        cpu.Ports[0xF449] = (byte)~pad1;
        cpu.Ports[0xF446] = (byte)~pad2;
        return cpu;
    }

    static int EntryOf(int firmware, MiniZ80 cpu)
    {
        // l'adresse de la routine branchée sur cette adresse de la ROM : dans la table BRANCHE du système
        // la table commence par &BB5A suivi de l'adresse de TXT OUTPUT (celle du saut fixe en &0040)
        byte[] code = Gx4000System.SystemCode(0);
        int start = Enumerable.Range(0, code.Length - 4).First(i =>
            code[i] == 0x5A && code[i + 1] == 0xBB && code[i + 2] == code[0x41] && code[i + 3] == code[0x42]);
        for (int i = start; code[i] != 0 || code[i + 1] != 0; i += 4)
            if ((code[i] | code[i + 1] << 8) == firmware)
                return code[i + 2] | code[i + 3] << 8;
        throw new InvalidOperationException("adresse non branchée");
    }

    static bool TestKey(MiniZ80 cpu, int key)
    {
        cpu.A = (byte)key;
        cpu.Call(EntryOf(0xBB1E, cpu));
        return !cpu.ZeroFlag;
    }

    [Theory]
    [InlineData(0x20, 47, true)]                              // tir 1 = espace
    [InlineData(0x20, 77, true)]                              // tir 1 = touche 77 de la manette
    [InlineData(0x10, 18, true)]                              // tir 2 = ENTRÉE
    [InlineData(0x01, 0, true)]                               // haut = flèche haut
    [InlineData(0x00, 47, false)]                             // rien d'appuyé
    [InlineData(0x20, 67, false)]                             // Q : pas dans la table
    public void KM_TEST_KEY_lit_la_manette(byte pad, int key, bool pressed)
    {
        Assert.Equal(pressed, TestKey(Pads(pad), key));
    }

    [Fact]
    public void Une_touche_placee_par_le_compilateur_repond_a_ses_boutons()
    {
        var q = new[] { new Gx4000System.KeyLink(67, 0, 0x30) };   // Q = tirs 1 et 2 ensemble

        Assert.True(TestKey(Pads(0x30, extra: q), 67));
        Assert.False(TestKey(Pads(0x20, extra: q), 67));
    }

    [Fact]
    public void KM_GET_JOYSTICK_rend_les_deux_manettes()
    {
        MiniZ80 cpu = Pads(0x21, 0x04);                       // manette 1 : haut + tir 1 ; manette 2 : gauche
        cpu.Call(EntryOf(0xBB24, cpu));

        Assert.Equal(0x21, cpu.H);
        Assert.Equal(0x04, cpu.L);
    }

    [Fact]
    public void KM_READ_CHAR_donne_un_espace_au_tir_1_une_seule_fois()
    {
        MiniZ80 cpu = Pads(0x20);
        int entry = EntryOf(0xBB09, cpu);
        cpu.Call(entry);
        Assert.True(cpu.CarryFlag);
        Assert.Equal(32, cpu.A);

        cpu.Call(entry);                                      // toujours appuyé : pas de nouveau caractère
        Assert.False(cpu.CarryFlag);
    }

    [Fact]
    public void Le_compilateur_place_les_autres_touches_d_INKEY_et_le_dit()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 IF INKEY(67)=0 THEN PRINT \"Q\"\n20 IF INKEY(69)=0 THEN PRINT \"A\"\n30 IF INKEY(47)=0 THEN PRINT \"E\"");

        Assert.NotNull(result.Cartridge);
        Assert.Equal(2, result.Notes.Count);                  // 47 (espace) est déjà le tir 1
        Assert.Contains("correspondra au tir 2 de la manette 1", result.Notes[0]);
        Assert.Contains("ENTRÉE", result.Notes[0]);
        Assert.Contains("correspondra aux tirs 1 et 2 de la manette 1", result.Notes[1]);
    }

    [Fact]
    public void Trop_de_touches_pas_de_cartouche()
    {
        string source = string.Join("\n", Enumerable.Range(0, 10).Select(i => $"{10 * (i + 1)} IF INKEY({56 + i})=0 THEN PRINT {i}"));
        CartridgeResult result = Compiler.CompileToCartridge(source);

        Assert.Null(result.Cartridge);
        Assert.Single(result.Problems);
        Assert.Contains("INKEY(65)", result.Problems[0]);
    }

    [Fact]
    public void INKEY_calcule_et_INPUT_donnent_un_avertissement()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 K=47:IF INKEY(K)=0 THEN PRINT 1\n20 INPUT A");

        Assert.NotNull(result.Cartridge);
        Assert.Equal(2, result.Notes.Count);
        Assert.Contains("calculé", result.Notes[0]);
        Assert.Contains("INPUT", result.Notes[1]);
    }
}
