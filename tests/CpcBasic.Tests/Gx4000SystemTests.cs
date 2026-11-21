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
        cpu.Memory[Gx4000System.Variables + 0x13] = 1;       // stylo 1, comme au démarrage de la cartouche (D186)
        cpu.Memory[Gx4000System.Variables + 0x48] = 1;       // stylo graphique 1 (D187)
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
    [InlineData(0x10, 47, true)]                              // tir 1 (bit 4) = espace
    [InlineData(0x10, 76, true)]                              // tir 1 = touche 76 de la manette
    [InlineData(0x20, 18, true)]                              // tir 2 (bit 5) = ENTRÉE
    [InlineData(0x01, 0, true)]                               // haut = flèche haut
    [InlineData(0x00, 47, false)]                             // rien d'appuyé
    [InlineData(0x10, 67, false)]                             // Q : pas dans la table
    public void KM_TEST_KEY_lit_la_manette(byte pad, int key, bool pressed)
    {
        Assert.Equal(pressed, TestKey(Pads(pad), key));
    }

    [Fact]
    public void Une_touche_placee_par_le_compilateur_repond_a_ses_boutons()
    {
        var q = new[] { new Gx4000System.KeyLink(67, 0, 0x30) };   // Q = tirs 1 et 2 ensemble

        Assert.True(TestKey(Pads(0x30, extra: q), 67));
        Assert.False(TestKey(Pads(0x10, extra: q), 67));
    }

    [Fact]
    public void KM_GET_JOYSTICK_rend_les_deux_manettes()
    {
        MiniZ80 cpu = Pads(0x21, 0x04);                       // manette 1 : haut + tir 2 ; manette 2 : gauche
        cpu.Call(EntryOf(0xBB24, cpu));

        Assert.Equal(0x21, cpu.H);
        Assert.Equal(0x04, cpu.L);
    }

    [Fact]
    public void KM_READ_CHAR_donne_un_espace_au_tir_1_une_seule_fois()
    {
        MiniZ80 cpu = Pads(0x10);
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

    // ---------- saisie à la manette pour INPUT (D184) ----------

    static MiniZ80 Wheel()
    {
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Load(Gx4000System.SystemCode(0, null, withInput: true), 0);
        cpu.Memory[Gx4000System.Variables + 0x13] = 1;       // stylo 1, comme au démarrage
        Mode(cpu, 1);
        return cpu;
    }

    // un passage de la boucle avec ces boutons de la manette 1 ; rend le caractère si la saisie avance, sinon null
    static char? Step(MiniZ80 cpu, byte pad)
    {
        cpu.Ports[0xF449] = (byte)~pad;
        cpu.Call(Gx4000System.WheelStepEntry);
        return cpu.CarryFlag ? (char)cpu.A : null;
    }

    static char Proposed(MiniZ80 cpu) =>
        (char)cpu.Memory[cpu.Memory[Gx4000System.WheelLetter] | cpu.Memory[Gx4000System.WheelLetter + 1] << 8];

    [Theory]
    [InlineData(0x08, ' ')]                                   // droite sur le carré vide du départ : un espace
    [InlineData(0x04, (char)127)]                             // gauche : DEL
    [InlineData(0x10, (char)13)]                              // tir 1 : la saisie est finie
    public void Droite_gauche_et_tir_1(byte pad, char expected)
    {
        Assert.Equal(expected, Step(Wheel(), pad));
    }

    [Fact]
    public void Bas_et_haut_changent_de_lettre_dans_le_groupe()
    {
        MiniZ80 cpu = Wheel();
        Assert.Null(Step(cpu, 0x02));                         // toute première fois : A montré sans bouger
        Assert.Equal('A', Proposed(cpu));
        Assert.Equal(1, cpu.Memory[Gx4000System.WheelShown]);
        Step(cpu, 0x00);
        Step(cpu, 0x02);                                      // bas : B
        Assert.Equal('B', Proposed(cpu));
        Step(cpu, 0x00);
        Step(cpu, 0x01);                                      // haut : A
        Step(cpu, 0x00);
        Step(cpu, 0x01);                                      // haut au début du groupe : Z
        Assert.Equal('Z', Proposed(cpu));
        Step(cpu, 0x00);
        Assert.Equal('Z', Step(cpu, 0x08));                   // droite : Z est écrite
        Assert.Equal(0, cpu.Memory[Gx4000System.WheelShown]); // ensuite : carré vide
        Step(cpu, 0x00);
        Assert.Equal(' ', Step(cpu, 0x08));                   // droite sur le carré vide : un espace
        Step(cpu, 0x00);
        Step(cpu, 0x01);                                      // haut repart de la dernière lettre : Y
        Assert.Equal('Y', Proposed(cpu));
    }

    [Fact]
    public void Tir_1_avec_une_lettre_montree_l_ecrit_puis_valide()
    {
        MiniZ80 cpu = Wheel();
        Step(cpu, 0x02);                                      // A montré
        Step(cpu, 0x00);

        Assert.Equal('A', Step(cpu, 0x10));                   // tir 1 : la lettre d'abord...
        Assert.Equal((char)13, Step(cpu, 0x10));              // ... puis la fin de la saisie, au passage suivant
        Assert.Equal(0, cpu.Memory[Gx4000System.WheelShown]);
    }

    [Fact]
    public void Le_tir_2_passe_au_groupe_suivant()
    {
        MiniZ80 cpu = Wheel();
        var firsts = new List<char>();
        for (int i = 0; i < 4; i++)
        {
            Step(cpu, 0x20);                                  // tir 2
            firsts.Add(Proposed(cpu));
            Step(cpu, 0x00);                                  // on relâche
        }

        Assert.Equal(new[] { 'a', '0', ' ', 'A' }, firsts);   // minuscules, chiffres, signes, puis retour
    }

    [Fact]
    public void La_lettre_proposee_est_montree_en_couleurs_inversees_puis_effacee()
    {
        MiniZ80 cpu = Wheel();
        Step(cpu, 0x02);                                      // A : première ligne ..###.. = &38, inversée &C7

        Assert.Equal(new byte[] { 0xC0, 0x70 }, cpu.Memory[0xC000..0xC002]);   // MODE 1 : stylo 1 autour de la lettre
        Step(cpu, 0x00);
        Step(cpu, 0x08);                                      // droite : effacée (INPUT écrira la lettre lui-même)
        Assert.Equal(new byte[] { 0x00, 0x00 }, cpu.Memory[0xC000..0xC002]);
        Assert.Equal(0, cpu.Memory[Gx4000System.Variables]);  // le curseur n'a pas bougé
    }

    [Fact]
    public void Sans_INPUT_pas_de_saisie_a_la_manette()
    {
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Load(Gx4000System.SystemCode(0), 0);
        cpu.Call(Gx4000System.WheelStepEntry);

        Assert.False(cpu.CarryFlag);
        Assert.True(Gx4000System.SystemCode(0).Length + 200 < Gx4000System.SystemCode(0, null, true).Length);
    }

    [Fact]
    public void Avec_INPUT_le_compilateur_ajoute_la_saisie_et_le_dit()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 INPUT \"NOM\";N$\n20 PRINT N$\n30 LINE INPUT A$");

        Assert.NotNull(result.Cartridge);
        Assert.Single(result.Notes);                          // un seul message, même avec deux saisies
        Assert.Contains("saisie à la manette", result.Notes[0]);
    }

    // ---------- couleurs (D186) ----------

    static void Firmware(MiniZ80 cpu, int address, byte a, byte b = 0)
    {
        cpu.A = a;
        cpu.B = b;
        cpu.Call(EntryOf(address, cpu));
    }

    [Fact]
    public void PEN_et_PAPER_changent_les_couleurs_du_texte()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Firmware(cpu, 0xBB90, 3);                             // PEN 3 : octet &FF
        Firmware(cpu, 0xBB96, 2);                             // PAPER 2 : octet &0F
        Print(cpu, "A");

        // A, ligne 1 : points 2, 3, 4 au stylo 3, les autres au papier 2
        Assert.Equal(new byte[] { 0x3F, 0x8F }, cpu.Memory[0xC000..0xC002]);
        Firmware(cpu, 0xBB93, 0);
        Assert.Equal(3, cpu.A);
        Firmware(cpu, 0xBB99, 0);
        Assert.Equal(2, cpu.A);
    }

    [Fact]
    public void CLS_remplit_l_ecran_avec_le_papier()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Firmware(cpu, 0xBB96, 2);
        cpu.A = 12;                                           // CLS (code 12)
        cpu.Call(Gx4000System.TxtOutputEntry);

        Assert.Equal(0x0F, cpu.Memory[0xC000]);
        Assert.Equal(0x0F, cpu.Memory[0xFFCF]);
    }

    [Fact]
    public void En_MODE_0_le_stylo_9_a_ses_quatre_bits()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 0);
        Firmware(cpu, 0xBB90, 9);                             // 9 = bits 0 et 3 : octet &C3
        Print(cpu, "A");

        // A, ligne 1, deuxième octet : les deux points allumés -> &C3
        Assert.Equal(0xC3, cpu.Memory[0xC001]);
    }

    [Fact]
    public void INK_et_BORDER_notent_la_couleur_et_la_rendent()
    {
        MiniZ80 cpu = Cpu();
        Firmware(cpu, 0xBC32, 1, 6);                          // INK 1,6
        Firmware(cpu, 0xBC35, 1);
        Assert.Equal(6, cpu.B);

        Firmware(cpu, 0xBC38, 0, 3);                          // BORDER 3
        Firmware(cpu, 0xBC3B, 0);
        Assert.Equal(3, cpu.B);
    }

    [Fact]
    public void Un_programme_avec_des_couleurs_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 MODE 0:INK 0,0:BORDER 3:PEN 2:PAPER 0:PRINT \"A\"");

        Assert.NotNull(result.Cartridge);
        Assert.Empty(result.Notes);
    }
    // ---------- dessin (D187) ----------

    static void Gra(MiniZ80 cpu, int address, int x = 0, int y = 0, byte a = 0)
    {
        cpu.D = (byte)(x >> 8);
        cpu.E = (byte)x;
        cpu.H = (byte)(y >> 8);
        cpu.L = (byte)y;
        cpu.A = a;
        cpu.Call(EntryOf(address, cpu));
    }

    [Fact]
    public void PLOT_0_0_est_en_bas_a_gauche()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBEA, 0, 0);                               // PLOT 0,0

        // ligne 199 depuis le haut : &C000 + 7 x &800 + 24 x 80 = &FF80 ; point de gauche, stylo 1 : &80
        Assert.Equal(0x80, cpu.Memory[0xFF80]);
    }

    [Fact]
    public void PLOT_639_399_en_MODE_2_est_en_haut_a_droite()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Gra(cpu, 0xBBEA, 639, 399);

        Assert.Equal(0x01, cpu.Memory[0xC04F]);
    }

    [Fact]
    public void Un_point_hors_de_l_ecran_n_est_pas_dessine()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBEA, -10, 0);
        Gra(cpu, 0xBBEA, 640, 0);
        Gra(cpu, 0xBBEA, 0, 400);

        Assert.All(cpu.Memory[0xC000..0x10000], b => Assert.Equal(0, b));
    }

    [Fact]
    public void DRAW_horizontal_en_MODE_1()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBF6, 20, 0);                              // DRAW 20,0 depuis 0,0 : points 0 à 10

        Assert.Equal(new byte[] { 0xF0, 0xF0, 0xE0, 0x00 }, cpu.Memory[0xFF80..0xFF84]);
        Gra(cpu, 0xBBC6);                                     // le curseur est au bout
        Assert.Equal(20, cpu.D << 8 | cpu.E);
        Assert.Equal(0, cpu.H << 8 | cpu.L);
    }

    [Fact]
    public void DRAW_vertical_en_MODE_2()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Gra(cpu, 0xBBF6, 0, 20);                              // points de base y 0 à 10

        Assert.Equal(0x80, cpu.Memory[0xFF80]);               // ligne 199
        Assert.Equal(0x80, cpu.Memory[0xEF30]);               // ligne 189
        Assert.Equal(0x00, cpu.Memory[0xE730]);               // ligne 188 : rien
    }

    [Fact]
    public void DRAW_en_biais_va_d_un_bout_a_l_autre()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Gra(cpu, 0xBBC0, 30, 20);                             // MOVE 30,20
        Gra(cpu, 0xBBF6, 0, 0);                               // DRAW 0,0 : de (30, 10) à (0, 0), à l'envers

        Assert.Equal(0x80, cpu.Memory[0xFF80] & 0x80);        // (0, 0) dessiné (le point voisin peut l'être aussi)
        Gra(cpu, 0xBBF0, 30, 20);                             // TEST 30,20 : l'autre bout est dessiné
        Assert.Equal(1, cpu.A);
    }

    [Fact]
    public void TEST_rend_l_encre_du_point()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBDE, a: 3);                               // GRAPHICS PEN 3
        Gra(cpu, 0xBBEA, 100, 100);
        Gra(cpu, 0xBBF0, 100, 100);
        Assert.Equal(3, cpu.A);
        Gra(cpu, 0xBBF0, 104, 100);
        Assert.Equal(0, cpu.A);
    }

    [Fact]
    public void ORIGIN_decale_les_points()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBC9, 100, 100);                           // ORIGIN 100,100
        Gra(cpu, 0xBBEA, 0, 0);                               // point de base (50, 50) : ligne 149, octet 12, point 2

        Assert.Equal(0x20, cpu.Memory[0xEDAC]);
    }

    [Fact]
    public void CLG_remplit_avec_le_papier_graphique()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBE4, a: 2);                               // GRAPHICS PAPER 2
        Gra(cpu, 0xBBDB);                                     // CLG

        Assert.Equal(0x0F, cpu.Memory[0xC000]);
        Assert.Equal(0x0F, cpu.Memory[0xFFFF]);
    }

    [Theory]
    [InlineData(0, true, false)]
    [InlineData(1, false, true)]
    [InlineData(2, false, false)]
    public void SCR_GET_MODE_rend_le_mode_et_ses_drapeaux(byte mode, bool carry, bool zero)
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, mode);
        Gra(cpu, 0xBC11);

        Assert.Equal(mode, cpu.A);
        Assert.Equal(carry, cpu.CarryFlag);
        Assert.Equal(zero, cpu.ZeroFlag);
    }

    [Fact]
    public void Un_programme_de_dessin_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 MODE 1:CLG 2:ORIGIN 320,200:PLOT 0,0,3:DRAW 100,100:MOVE -50,0:DRAWR 0,-80:BOX 10,10,60,60,1:CIRCLE 0,0,50,2");

        Assert.NotNull(result.Cartridge);
    }
}
