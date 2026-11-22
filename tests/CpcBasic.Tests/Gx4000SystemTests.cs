// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
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
        InstallScrWrite(cpu);
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
        Assert.Equal(0xC3, code[0x38]);                         // interruption : un saut (encres clignotantes, D191)
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Les_traits_tout_droits_rapides_donnent_les_memes_points_que_PLOT(byte mode)
    {
        // D202 : DRAW horizontal (octet par octet) ou vertical (ligne après ligne) comparé à PLOT point par point,
        // sur un décor au hasard, avec ou sans fenêtre graphique, en écriture normale ou XOR, bouts hors de l'écran
        var random = new Random(40 + mode);
        int unit = mode == 0 ? 4 : mode == 1 ? 2 : 1;           // unités du repère par point, en largeur
        MiniZ80 fast = Cpu(), slow = Cpu();                   // la console fabriquée une seule fois (essai rapide)
        Mode(fast, mode);
        Mode(slow, mode);
        var entries = new Dictionary<int, int>();             // adresses des routines cherchées une seule fois
        void Go(MiniZ80 cpu, int firmware, int x = 0, int y = 0, byte a = 0)
        {
            if (!entries.TryGetValue(firmware, out int entry))
                entries[firmware] = entry = EntryOf(firmware, cpu);
            cpu.D = (byte)(x >> 8);
            cpu.E = (byte)x;
            cpu.H = (byte)(y >> 8);
            cpu.L = (byte)y;
            cpu.A = a;
            cpu.Call(entry);
        }
        for (int round = 0; round < 60; round++)
        {
            byte pen = (byte)random.Next(1, 16);
            byte access = (byte)(round % 2);
            var decor = new byte[0x4000];
            random.NextBytes(decor);
            foreach (MiniZ80 c in new[] { fast, slow })
            {
                decor.CopyTo(c.Memory, 0xC000);
                bool window = round % 3 == 0;
                Go(c, 0xBBCF, window ? 100 : 0, window ? 500 : 639);   // fenêtre graphique, ou tout l'écran
                Go(c, 0xBBD2, window ? 350 : 399, window ? 50 : 0);
                Go(c, 0xBBDE, a: pen);
                Go(c, 0xBC59, a: access);
            }
            int x1 = random.Next(-40, 700) / unit * unit, y1 = random.Next(-40, 440) / 2 * 2;
            int x2 = random.Next(-40, 700) / unit * unit, y2 = random.Next(-40, 440) / 2 * 2;
            bool horizontal = random.Next(2) == 0;
            if (horizontal)
                y2 = y1;
            else
                x2 = x1;

            Go(fast, 0xBBC0, x1, y1);
            Go(fast, 0xBBF6, x2, y2);
            if (horizontal)
                for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x += unit)
                    Go(slow, 0xBBEA, x, y1);
            else
                for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y += 2)
                    Go(slow, 0xBBEA, x1, y);

            Assert.True(slow.Memory[0xC000..].SequenceEqual(fast.Memory[0xC000..]),
                        $"MODE {mode}, essai {round} : de ({x1}, {y1}) à ({x2}, {y2})");
            Go(fast, 0xBBC6);                                 // le curseur graphique est au bout
            Assert.Equal(x2, (short)(fast.D << 8 | fast.E));
            Assert.Equal(y2, (short)(fast.H << 8 | fast.L));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Les_traits_en_biais_rapides_donnent_les_memes_points_que_PLOT(byte mode)
    {
        // D203 : DRAW en biais comparé à PLOT des points du même calcul (celui du chemin normal), sur un décor au
        // hasard, avec ou sans fenêtre graphique, en écriture normale ou XOR, bouts dans l'écran ou dehors
        var random = new Random(70 + mode);
        int unit = mode == 0 ? 4 : mode == 1 ? 2 : 1;
        int width = 640 / unit;
        MiniZ80 fast = Cpu(), slow = Cpu();
        Mode(fast, mode);
        Mode(slow, mode);
        var entries = new Dictionary<int, int>();
        void Go(MiniZ80 cpu, int firmware, int x = 0, int y = 0, byte a = 0)
        {
            if (!entries.TryGetValue(firmware, out int entry))
                entries[firmware] = entry = EntryOf(firmware, cpu);
            cpu.D = (byte)(x >> 8);
            cpu.E = (byte)x;
            cpu.H = (byte)(y >> 8);
            cpu.L = (byte)y;
            cpu.A = a;
            cpu.Call(entry);
        }
        for (int round = 0; round < 60; round++)
        {
            byte pen = (byte)random.Next(1, 16);
            byte access = (byte)(round % 2);
            var decor = new byte[0x4000];
            random.NextBytes(decor);
            bool window = round % 4 == 0;
            foreach (MiniZ80 c in new[] { fast, slow })
            {
                decor.CopyTo(c.Memory, 0xC000);
                Go(c, 0xBBCF, window ? 100 : 0, window ? 500 : 639);
                Go(c, 0xBBD2, window ? 350 : 399, window ? 50 : 0);
                Go(c, 0xBBDE, a: pen);
                Go(c, 0xBC59, a: access);
            }
            bool outside = round % 5 == 0;                    // un bout hors de l'écran : le chemin normal
            int x1 = random.Next(outside ? -30 : 0, width), y1 = random.Next(0, 200);
            int x2 = random.Next(0, width + (outside ? 30 : 0)), y2 = random.Next(0, 200);

            Go(fast, 0xBBC0, x1 * unit, y1 * 2);
            Go(fast, 0xBBF6, x2 * unit, y2 * 2);
            int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1), sx = x2 < x1 ? -1 : 1, sy = y2 < y1 ? -1 : 1;
            int x = x1, y = y1;
            bool alongY = dy > dx;
            int count = alongY ? dy : dx, error = count >> 1;
            while (true)
            {
                Go(slow, 0xBBEA, x * unit, y * 2);
                if (count-- == 0)
                    break;
                if (alongY)
                {
                    y += sy;
                    error += dx - dy;
                    if (error >= 0)
                        x += sx;
                    else
                        error += dy;
                }
                else
                {
                    x += sx;
                    error += dy - dx;
                    if (error >= 0)
                        y += sy;
                    else
                        error += dx;
                }
            }

            Assert.True(slow.Memory[0xC000..].SequenceEqual(fast.Memory[0xC000..]),
                        $"MODE {mode}, essai {round} : de ({x1}, {y1}) à ({x2}, {y2}) en points");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Les_traits_rapides_du_CPC_donnent_les_memes_points_que_GRA_LINE(byte mode)
    {
        // D204 : le module LineRuntime (DRAW et DRAWR des programmes) appelle les routines du firmware ; ici, celles de
        // notre système de la cartouche (mêmes entrées). Comparé à GRA LINE de ce système, sur des traits au hasard :
        // origine, fenêtre graphique, XOR, bouts hors de l'écran (la ROM s'en charge), DRAWR.
        var random = new Random(90 + mode);
        var emitter = new Z80Emitter(0x4000);
        foreach (RuntimeModule m in Linker.Resolve([RuntimeLibrary.LineName]))
            m.Emit(emitter);
        int line = emitter.AddressOf(LineRuntime.LineLabel), relative = emitter.AddressOf(LineRuntime.RelativeLabel);
        MiniZ80 fast = Cpu(), slow = Cpu();
        var entries = new Dictionary<int, int>();
        int Entry(int firmware) => entries.TryGetValue(firmware, out int e) ? e : entries[firmware] = EntryOf(firmware, fast);
        foreach (MiniZ80 c in new[] { fast, slow })
        {
            c.UseInstalledFirmware = true;
            c.Load(emitter.ToBytes(), 0x4000);
            foreach (int firmware in new[] { 0xBBC6, 0xBBCC, 0xBC0B, 0xBC11, 0xBBD5, 0xBBD8, 0xBBE1, 0xBC2C, 0xBBC0, 0xBC1D,
                                             0xBBF6 })
            {
                c.Memory[firmware] = 0xC3;                    // les sauts du firmware, comme au démarrage de la cartouche
                c.Memory[firmware + 1] = (byte)Entry(firmware);
                c.Memory[firmware + 2] = (byte)(Entry(firmware) >> 8);
            }
            Mode(c, mode);
        }
        void Go(MiniZ80 cpu, int address, int x = 0, int y = 0, byte a = 0)
        {
            cpu.D = (byte)(x >> 8);
            cpu.E = (byte)x;
            cpu.H = (byte)(y >> 8);
            cpu.L = (byte)y;
            cpu.A = a;
            cpu.Call(address);
        }
        for (int round = 0; round < 80; round++)
        {
            byte pen = (byte)random.Next(1, 16);
            var decor = new byte[0x4000];
            random.NextBytes(decor);
            bool window = round % 4 == 0;
            int ox = round % 3 == 0 ? random.Next(-50, 300) : 0, oy = round % 3 == 0 ? random.Next(-50, 200) : 0;
            int x1 = random.Next(-60, 700), y1 = random.Next(-40, 440), x2 = random.Next(-60, 700), y2 = random.Next(-40, 440);
            if (round % 5 == 1)
                y2 = y1;                                       // horizontal
            if (round % 5 == 2)
                x2 = x1;                                       // vertical
            bool rel = round % 2 == 0;
            foreach (MiniZ80 c in new[] { fast, slow })
            {
                decor.CopyTo(c.Memory, 0xC000);
                Go(c, Entry(0xBBC9), ox, oy);                 // ORIGIN
                Go(c, Entry(0xBBCF), window ? 100 : 0, window ? 500 : 639);
                Go(c, Entry(0xBBD2), window ? 350 : 399, window ? 50 : 0);
                Go(c, Entry(0xBBDE), a: pen);
                Go(c, Entry(0xBC59), a: (byte)(round % 3 == 1 ? 1 : 0));
                Go(c, Entry(0xBBC0), x1, y1);
            }
            if (rel)
            {
                Go(fast, relative, x2 - x1, y2 - y1);
                Go(slow, Entry(0xBBF9), x2 - x1, y2 - y1);
            }
            else
            {
                Go(fast, line, x2, y2);
                Go(slow, Entry(0xBBF6), x2, y2);
            }

            string gaps = string.Join(" ", Enumerable.Range(0xC000, 0x4000).Where(i => slow.Memory[i] != fast.Memory[i]).Take(8)
                .Select(i => $"{i:X4}:{decor[i - 0xC000]:X2}/{slow.Memory[i]:X2}/{fast.Memory[i]:X2}"));
            Assert.True(gaps.Length == 0,
                        $"MODE {mode}, essai {round} : de ({x1}, {y1}) à ({x2}, {y2}), origine ({ox}, {oy}), " +
                        $"fenêtre {window}, relatif {rel}, stylo {pen} ; écarts (décor/ROM/rapide) : {gaps}");
            Go(fast, Entry(0xBBC6));
            Assert.Equal(x2, (short)(fast.D << 8 | fast.E));
            Assert.Equal(y2, (short)(fast.H << 8 | fast.L));
        }
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
        Assert.Equal(0x0F, cpu.Memory[0xFFCF]);               // dernier octet visible (D188 : seulement ce qui se voit)
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

    // ---------- XOR, AND, OR, fenêtres graphiques (D188) ----------

    // SCR WRITE (&BDE8) : comme au démarrage de la cartouche, un saut vers notre routine
    static void InstallScrWrite(MiniZ80 cpu)
    {
        int target = EntryOf(0xBDE8, cpu);
        cpu.Memory[0xBDE8] = 0xC3;
        cpu.Memory[0xBDE9] = (byte)target;
        cpu.Memory[0xBDEA] = (byte)(target >> 8);
    }

    [Fact]
    public void PLOT_en_XOR_efface_un_point_deja_allume()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 2);
        Gra(cpu, 0xBBEA, 0, 0);
        Assert.Equal(0x80, cpu.Memory[0xFF80]);
        Gra(cpu, 0xBC59, a: 1);                               // SCR ACCESS 1 : XOR
        Gra(cpu, 0xBBEA, 0, 0);
        Assert.Equal(0x00, cpu.Memory[0xFF80]);
    }

    [Fact]
    public void PLOT_en_AND_et_en_OR()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBEA, 0, 0);                               // encre 1 : &80
        Gra(cpu, 0xBC59, a: 3);                               // OR avec l'encre 2 : encre 3
        Gra(cpu, 0xBBDE, a: 2);
        Gra(cpu, 0xBBEA, 0, 0);
        Assert.Equal(0x88, cpu.Memory[0xFF80]);
        Gra(cpu, 0xBC59, a: 2);                               // AND avec l'encre 1 : encre 1
        Gra(cpu, 0xBBDE, a: 1);
        Gra(cpu, 0xBBEA, 0, 0);
        Assert.Equal(0x80, cpu.Memory[0xFF80]);
    }

    [Fact]
    public void MODE_remet_l_ecriture_normale()
    {
        MiniZ80 cpu = Cpu();
        Gra(cpu, 0xBC59, a: 1);
        Mode(cpu, 2);
        Gra(cpu, 0xBBEA, 0, 0);
        Gra(cpu, 0xBBEA, 0, 0);                               // normal : le point reste allumé

        Assert.Equal(0x80, cpu.Memory[0xFF80]);
    }

    // fenêtre graphique : x de 100 à 199, y de 100 à 299 (repère de l'écran), en MODE 1
    static MiniZ80 Window()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Gra(cpu, 0xBBCF, 199, 100);                           // dans le désordre : rangés par la routine
        Gra(cpu, 0xBBD2, 100, 299);
        return cpu;
    }

    [Fact]
    public void Une_fenetre_graphique_est_arrondie_a_l_octet()
    {
        MiniZ80 cpu = Window();
        Gra(cpu, 0xBBD5);                                     // GRA GET W WIDTH

        Assert.Equal(96, cpu.D << 8 | cpu.E);                 // point 50 -> début de son octet : 48
        Assert.Equal(198, cpu.H << 8 | cpu.L);                // point 99 : déjà en fin d'octet
        Gra(cpu, 0xBBD8);                                     // GRA GET W HEIGHT
        Assert.Equal(298, cpu.D << 8 | cpu.E);
        Assert.Equal(100, cpu.H << 8 | cpu.L);
    }

    [Fact]
    public void Rien_n_est_dessine_hors_de_la_fenetre()
    {
        MiniZ80 cpu = Window();
        Gra(cpu, 0xBBEA, 50, 150);                            // à gauche de la fenêtre
        Gra(cpu, 0xBBEA, 150, 150);                           // dedans : point de base (75, 75)

        Assert.Equal(0x00, cpu.Memory[0xE4B6]);
        Assert.Equal(0x10, cpu.Memory[0xE4C2]);
        Gra(cpu, 0xBBF0, 50, 150);                            // TEST hors de la fenêtre : le papier
        Assert.Equal(0, cpu.A);
    }

    [Fact]
    public void CLG_ne_remplit_que_la_fenetre()
    {
        MiniZ80 cpu = Window();
        Gra(cpu, 0xBBE4, a: 2);
        Gra(cpu, 0xBBDB);

        Assert.Equal(0x0F, cpu.Memory[0xE4BC]);               // ligne 124, octet 12 (points 48 à 51) : dedans
        Assert.Equal(0x00, cpu.Memory[0xE4BB]);               // octet 11 : dehors
        Assert.Equal(0x00, cpu.Memory[0xFF80]);               // en bas de l'écran : dehors
    }

    [Fact]
    public void Un_programme_avec_XOR_MASK_et_FILL_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 MODE 1:ORIGIN 0,0,100,500,350,50:CLG 2:MASK 170:DRAW 400,300,3,1:MOVE 200,200:FILL 1:PLOT 10,10,1,2:FRAME");

        Assert.NotNull(result.Cartridge);
    }

    // ---------- programmes de plus de 16 Ko (D189) ----------

    static byte[] BigProgram(int length) => Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();

    // le contenu d'une page dans le fichier .cpr
    static byte[] Page(byte[] cpr, int page) =>
        cpr[(20 + page * (8 + CartridgeImage.PageSize))..(20 + page * (8 + CartridgeImage.PageSize) + CartridgeImage.PageSize)];

    [Theory]
    [InlineData(3000, 0)]
    [InlineData(20000, 1)]
    [InlineData(30000, 2)]               // (40 000 : 3 pages depuis que le système dépasse 9 Ko, D203)
    public void Le_programme_est_range_sur_assez_de_pages(int length, int otherPages)
    {
        (int first, IReadOnlyList<int> others) = Gx4000System.SplitProgram(length);

        Assert.Equal(otherPages, others.Count);
        Assert.Equal(length, first + others.Sum());
        Assert.All(others, l => Assert.InRange(l, 1, CartridgeImage.PageSize));
    }

    [Fact]
    public void Les_pages_contiennent_tout_le_programme_dans_l_ordre()
    {
        byte[] program = BigProgram(30000);
        CartridgeImage cartridge = Gx4000System.Build(program);
        (int first, IReadOnlyList<int> others) = Gx4000System.SplitProgram(program.Length);
        int system = Gx4000System.SystemCode(first, null, false, others).Length;
        byte[] cpr = cartridge.ToCprBytes();

        Assert.Equal(3, cartridge.PageCount);
        var found = new List<byte>(Page(cpr, 0)[system..(system + first)]);
        for (int p = 0; p < others.Count; p++)
            found.AddRange(Page(cpr, p + 1)[..others[p]]);
        Assert.Equal(program, found.ToArray());
    }

    [Fact]
    public void Un_programme_trop_grand_pour_la_memoire_est_refuse()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Gx4000System.Build(new byte[Gx4000System.MaxProgram + 1]));

        Assert.Contains("mémoire", error.Message);
    }

    // ---------- fenêtres de texte (D190) ----------

    // TXT WIN ENABLE : gauche, droite, haut, bas (à partir de 0)
    static void Win(MiniZ80 cpu, byte left, byte right, byte top, byte bottom)
    {
        cpu.H = left;
        cpu.D = right;
        cpu.L = top;
        cpu.E = bottom;
        cpu.Call(EntryOf(0xBB66, cpu));
    }

    // TXT STR SELECT : rend le flux d'avant
    static byte Stream(MiniZ80 cpu, byte stream)
    {
        cpu.A = stream;
        cpu.Call(EntryOf(0xBBB4, cpu));
        return cpu.A;
    }

    static void Locate(MiniZ80 cpu, byte column, byte row)
    {
        cpu.H = column;
        cpu.L = row;
        cpu.Call(Gx4000System.SetCursorEntry);
    }

    static (int Left, int Right, int Top, int Bottom) GetWindow(MiniZ80 cpu)
    {
        cpu.Call(EntryOf(0xBB69, cpu));
        return (cpu.H, cpu.D, cpu.L, cpu.E);
    }

    // MODE 1, fenêtre colonnes 10 à 19, lignes 5 à 9 (WINDOW 10,19,5,9) : son coin en haut à gauche est en &C000 + 4 x 80 + 9 x 2
    const int WindowCorner = 0xC000 + 4 * 80 + 9 * 2;

    static MiniZ80 TextWindow()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Win(cpu, 18, 9, 8, 4);                                // dans le désordre : rangés par la routine
        return cpu;
    }

    [Fact]
    public void WINDOW_place_le_texte_dans_la_fenetre()
    {
        MiniZ80 cpu = TextWindow();
        Assert.Equal((9, 18, 4, 8), GetWindow(cpu));
        Print(cpu, "A");

        Assert.Equal(new byte[] { 0x30, 0x80 }, cpu.Memory[WindowCorner..(WindowCorner + 2)]);
        cpu.Call(Gx4000System.GetCursorEntry);
        Assert.Equal(2, cpu.H);                               // colonne et ligne comptées dans la fenêtre
        Assert.Equal(1, cpu.L);
    }

    [Fact]
    public void Au_bout_de_la_fenetre_on_passe_a_la_ligne()
    {
        MiniZ80 cpu = TextWindow();
        Print(cpu, new string('A', 11));                      // la fenêtre a 10 colonnes

        Assert.Equal(1, cpu.Memory[Gx4000System.Variables + 1]);
        Assert.Equal(0x30, cpu.Memory[WindowCorner + 80]);    // le 11e A : ligne suivante, au bord gauche de la fenêtre
        Assert.Equal(0x00, cpu.Memory[WindowCorner + 20]);    // rien à droite de la fenêtre
    }

    [Fact]
    public void Seule_la_fenetre_remonte()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Print(cpu, "A");                                      // hors de la fenêtre, en haut à gauche de l'écran
        Win(cpu, 9, 18, 4, 8);
        Locate(cpu, 1, 2);
        Print(cpu, "A");                                      // deuxième ligne de la fenêtre
        Locate(cpu, 1, 5);
        Print(cpu, "\n");                                     // en bas de la fenêtre : elle remonte d'une ligne

        Assert.Equal(0x30, cpu.Memory[WindowCorner]);         // le A est monté sur la première ligne
        Assert.Equal(0x00, cpu.Memory[WindowCorner + 80]);
        Assert.Equal(0x30, cpu.Memory[0xC000]);               // le A hors de la fenêtre n'a pas bougé
        Assert.Equal(4, cpu.Memory[Gx4000System.Variables + 1]);   // le curseur reste sur la dernière ligne
    }

    [Fact]
    public void CLS_n_efface_que_la_fenetre()
    {
        MiniZ80 cpu = TextWindow();
        Firmware(cpu, 0xBB96, 2);                             // PAPER 2 : octet &0F
        cpu.A = 12;
        cpu.Call(Gx4000System.TxtOutputEntry);

        Assert.Equal(0x0F, cpu.Memory[WindowCorner]);
        Assert.Equal(0x0F, cpu.Memory[WindowCorner + 19]);    // dernier octet de la ligne de la fenêtre
        Assert.Equal(0x00, cpu.Memory[WindowCorner + 20]);    // juste à droite
        Assert.Equal(0x00, cpu.Memory[WindowCorner - 1]);     // juste à gauche
        Assert.Equal(0x0F, cpu.Memory[WindowCorner + 4 * 80 + 0x3800]);   // dernière ligne de points de la fenêtre
        Assert.Equal(0x00, cpu.Memory[WindowCorner + 5 * 80 + 0x3800]);   // dessous
    }

    [Fact]
    public void Chaque_flux_garde_sa_fenetre_son_curseur_et_ses_couleurs()
    {
        MiniZ80 cpu = Cpu();
        Mode(cpu, 1);
        Assert.Equal(0, Stream(cpu, 1));                      // #1
        Win(cpu, 9, 18, 4, 8);
        Firmware(cpu, 0xBB90, 3);                             // PEN #1,3
        Print(cpu, "A");

        Assert.Equal(1, Stream(cpu, 0));                      // retour à #0 : tout l'écran, stylo 1
        Assert.Equal((0, 39, 0, 24), GetWindow(cpu));
        Firmware(cpu, 0xBB93, 0);
        Assert.Equal(1, cpu.A);
        Print(cpu, "A");
        Assert.Equal(0x30, cpu.Memory[0xC000]);

        Stream(cpu, 1);                                       // #1 retrouvé
        Assert.Equal((9, 18, 4, 8), GetWindow(cpu));
        Firmware(cpu, 0xBB93, 0);
        Assert.Equal(3, cpu.A);
        Assert.Equal(1, cpu.Memory[Gx4000System.Variables]);  // son curseur, après le A
    }

    [Fact]
    public void WINDOW_SWAP_echange_deux_flux_et_MODE_remet_tout_l_ecran()
    {
        MiniZ80 cpu = TextWindow();                           // #0 : la fenêtre
        cpu.B = 0;
        cpu.C = 1;
        cpu.Call(EntryOf(0xBBB7, cpu));                       // WINDOW SWAP 0,1

        Assert.Equal((0, 39, 0, 24), GetWindow(cpu));         // #0 a pris la fenêtre de #1 : tout l'écran
        Stream(cpu, 1);
        Assert.Equal((9, 18, 4, 8), GetWindow(cpu));
        Mode(cpu, 1);
        Assert.Equal((0, 39, 0, 24), GetWindow(cpu));
    }

    [Fact]
    public void TXT_VALIDATE_passe_a_la_ligne_apres_la_fin_de_la_fenetre()
    {
        MiniZ80 cpu = TextWindow();
        cpu.H = 11;                                           // juste après la 10e colonne
        cpu.L = 1;
        cpu.Call(EntryOf(0xBB87, cpu));

        Assert.Equal(1, cpu.H);
        Assert.Equal(2, cpu.L);
    }

    [Fact]
    public void Un_programme_avec_des_fenetres_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 MODE 1:WINDOW #1,10,30,5,15:PAPER #1,2:CLS #1:PRINT #1,\"BONJOUR\":WINDOW SWAP 0,1:PRINT \"FIN\"");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- encres clignotantes (D191) ----------

    [Fact]
    public void INK_note_les_deux_couleurs()
    {
        MiniZ80 cpu = Cpu();
        cpu.C = 24;
        Firmware(cpu, 0xBC32, 1, 6);                          // INK 1,6,24
        Firmware(cpu, 0xBC35, 1);

        Assert.Equal(6, cpu.B);
        Assert.Equal(24, cpu.C);
    }

    [Fact]
    public void L_interruption_change_de_couleurs_au_rythme_de_SPEED_INK()
    {
        MiniZ80 cpu = Cpu();
        cpu.H = 2;                                            // SPEED INK 2,3
        cpu.L = 3;
        cpu.Call(EntryOf(0xBC3E, cpu));
        cpu.Memory[0xBE33] = 1;                               // la prochaine image change les couleurs
        cpu.Ports[0xF500] = 0x01;                             // interruption pendant le retour du balayage

        cpu.Call(0x0038);
        Assert.Equal(1, cpu.Memory[0xBE32]);                  // secondes couleurs...
        Assert.Equal(3, cpu.Memory[0xBE33]);                  // ... pour 3 images

        cpu.Ports[0xF500] = 0x00;                             // les 5 autres interruptions de l'image : rien
        for (int i = 0; i < 5; i++)
            cpu.Call(0x0038);
        Assert.Equal(3, cpu.Memory[0xBE33]);

        cpu.Ports[0xF500] = 0x01;
        for (int i = 0; i < 3; i++)
            cpu.Call(0x0038);
        Assert.Equal(0, cpu.Memory[0xBE32]);                  // retour aux premières couleurs, pour 2 images
        Assert.Equal(2, cpu.Memory[0xBE33]);
        cpu.Call(EntryOf(0xBC41, cpu));
        Assert.Equal(0x0203, cpu.H << 8 | cpu.L);
    }

    [Fact]
    public void Un_programme_avec_des_encres_clignotantes_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 MODE 1:INK 1,6,24:BORDER 2,26:SPEED INK 5,20:PRINT \"A\"");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- son (D192) ----------

    const int SoundBlock = 0x9000;

    // SOUND QUEUE avec le bloc de 9 octets du compilateur : état (canaux), enveloppes, période, bruit, volume, durée
    static bool Queue(MiniZ80 cpu, byte state, int period, int duration, byte volume = 12, byte noise = 0,
                      byte volumeEnvelope = 0, byte toneEnvelope = 0)
    {
        byte[] block = [state, volumeEnvelope, toneEnvelope, (byte)period, (byte)(period >> 8), noise, volume, (byte)duration, (byte)(duration >> 8)];
        block.CopyTo(cpu.Memory, SoundBlock);
        cpu.H = SoundBlock >> 8;
        cpu.L = SoundBlock & 0xFF;
        cpu.Call(EntryOf(0xBCAA, cpu));
        return cpu.CarryFlag;
    }

    static byte Check(MiniZ80 cpu, byte channel)
    {
        cpu.A = channel;
        cpu.Call(EntryOf(0xBCAD, cpu));
        return cpu.A;
    }

    // un centième de seconde : 3 interruptions
    static void Hundredth(MiniZ80 cpu, int count = 1)
    {
        for (int i = 0; i < 3 * count; i++)
            cpu.Call(0x0038);
    }

    static MiniZ80 SoundCpu()
    {
        MiniZ80 cpu = Cpu();
        cpu.Call(EntryOf(0xBCA7, cpu));                       // SOUND RESET, comme au démarrage
        return cpu;
    }

    [Fact]
    public void La_file_d_un_canal_garde_4_sons()
    {
        MiniZ80 cpu = SoundCpu();
        for (int i = 0; i < 4; i++)
            Assert.True(Queue(cpu, 1, 478, 50));

        Assert.False(Queue(cpu, 1, 478, 50));                 // pleine : le compilateur attend une place
        Assert.Equal(0, Check(cpu, 1));                       // plus de place, pas encore de son en cours
        Assert.Equal(4, Check(cpu, 2));                       // le canal B est libre
    }

    [Fact]
    public void Un_son_dure_le_temps_demande_puis_le_suivant_commence()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 1, 478, 2);
        Queue(cpu, 1, 239, 3);

        Hundredth(cpu);                                       // le premier commence
        Assert.Equal(0x80 | 3, Check(cpu, 1));                // en cours, 3 places libres
        Hundredth(cpu, 2);                                    // 2 centièmes plus tard : le second commence
        Assert.Equal(0x80 | 4, Check(cpu, 1));
        Assert.Equal(3, cpu.Memory[0xB803]);                  // il lui reste 3 centièmes
        Hundredth(cpu, 3);
        Assert.Equal(4, Check(cpu, 1));                       // fini : tout se tait
        Assert.Equal(0x3F, cpu.Memory[0xB8BB]);               // ton et bruit coupés partout
    }

    [Fact]
    public void Un_son_sur_deux_canaux_et_le_bruit()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 1 | 4, 100, 10, noise: 15);                // canaux A et C, avec du bruit
        Hundredth(cpu);

        Assert.Equal(0x80 | 4, Check(cpu, 1));
        Assert.Equal(4, Check(cpu, 2));
        Assert.Equal(0x80 | 4, Check(cpu, 4));
        Assert.Equal(0x3F & ~(0x01 | 0x04 | 0x08 | 0x20), cpu.Memory[0xB8BB]);   // ton et bruit de A et C
    }

    [Fact]
    public void Le_bit_7_vide_la_file_d_abord()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 1, 478, 50);
        Queue(cpu, 1, 478, 50);
        Queue(cpu, 0x81, 239, 10);                            // le son passe devant tout

        Assert.Equal(3, Check(cpu, 1));
        Hundredth(cpu);
        Assert.Equal(10, cpu.Memory[0xB803]);
    }

    [Fact]
    public void Un_programme_avec_du_son_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 SOUND 1,478,50,12:SOUND 2,0,20,15,0,0,10\n20 IF SQ(1)<128 THEN PRINT \"FINI\"");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- enveloppes du son (D193) ----------

    // ENV ou ENT : le bloc de 16 octets du compilateur (nombre de sections, puis 3 octets par section), noté par le système
    static void Envelope(MiniZ80 cpu, int firmware, byte number, int address, params byte[] block)
    {
        block.CopyTo(cpu.Memory, address);
        cpu.A = number;
        cpu.H = (byte)(address >> 8);
        cpu.L = (byte)address;
        cpu.Call(EntryOf(firmware, cpu));
    }

    [Fact]
    public void ENV_change_le_volume_pas_a_pas_et_donne_la_duree()
    {
        MiniZ80 cpu = SoundCpu();
        Envelope(cpu, 0xBCBC, 1, 0x9100, 1, 3, 2, 1);        // ENV 1,3,2,1 : 3 pas de +2, un centième chacun
        Queue(cpu, 1, 478, 0, volume: 5, volumeEnvelope: 1);  // durée 0 : celle de l'enveloppe

        Hundredth(cpu);
        Assert.Equal(7, cpu.Memory[0xB805]);                  // premier pas dès le début
        Hundredth(cpu, 2);
        Assert.Equal(11, cpu.Memory[0xB805]);
        Assert.Equal(0x80 | 4, Check(cpu, 1));                // encore en cours
        Hundredth(cpu);
        Assert.Equal(4, Check(cpu, 1));                       // l'enveloppe est finie : le son aussi
    }

    [Fact]
    public void Avec_une_duree_donnee_le_volume_reste_a_la_fin_de_l_enveloppe()
    {
        MiniZ80 cpu = SoundCpu();
        Envelope(cpu, 0xBCBC, 2, 0x9100, 1, 2, 0xFF, 1);     // ENV 2,2,-1,1
        Queue(cpu, 1, 478, 10, volume: 10, volumeEnvelope: 2);

        Hundredth(cpu, 5);
        Assert.Equal(8, cpu.Memory[0xB805]);
        Assert.Equal(0x80 | 4, Check(cpu, 1));
        Hundredth(cpu, 6);
        Assert.Equal(4, Check(cpu, 1));                       // fini : les 10 centièmes demandés, après le départ
    }

    [Fact]
    public void ENT_fait_glisser_la_note()
    {
        MiniZ80 cpu = SoundCpu();
        Envelope(cpu, 0xBCBF, 1, 0x9200, 1, 2, 0xF6, 1);     // ENT 1,2,-10,1
        Queue(cpu, 1, 500, 20, toneEnvelope: 1);

        Hundredth(cpu);
        Assert.Equal(490, cpu.Memory[0xB818] | cpu.Memory[0xB819] << 8);
        Hundredth(cpu);
        Assert.Equal(480, cpu.Memory[0xB818] | cpu.Memory[0xB819] << 8);
        Hundredth(cpu, 3);
        Assert.Equal(480, cpu.Memory[0xB818] | cpu.Memory[0xB819] << 8);   // l'enveloppe finie, la note reste
    }

    [Fact]
    public void Un_programme_avec_ENV_et_ENT_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 ENV 1,10,1,5,10,-1,5:ENT -2,5,2,1,5,-2,1:ENT 3,=500,10\n20 SOUND 1,478,0,5,1,2:SOUND 2,200,-3,0,1,3");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- temps et événements (D194) ----------

    // une petite routine en &9400 qui met 1 en &9500 : la marque qu'un événement est arrivé
    static void EventRoutine(MiniZ80 cpu, int routine = 0x9400)
    {
        new byte[] { 0x3E, 0x01, 0x32, 0x00, 0x95, 0xC9 }.CopyTo(cpu.Memory, routine);   // LD A,1 : LD (&9500),A : RET
        cpu.Memory[0x9500] = 0;
    }

    static void InitEvent(MiniZ80 cpu, int block, int routine)
    {
        cpu.H = (byte)(block >> 8);
        cpu.L = (byte)block;
        cpu.D = (byte)(routine >> 8);
        cpu.E = (byte)routine;
        cpu.B = 0xC1;
        cpu.C = 0;
        cpu.Call(EntryOf(0xBCEF, cpu));
    }

    [Fact]
    public void TIME_compte_les_interruptions()
    {
        MiniZ80 cpu = Cpu();
        for (int i = 0; i < 6; i++)
            cpu.Call(0x0038);
        cpu.Call(EntryOf(0xBD0D, cpu));                       // KL TIME PLEASE

        Assert.Equal(6, cpu.H << 8 | cpu.L);
        Assert.Equal(0, cpu.D << 8 | cpu.E);
    }

    [Fact]
    public void Une_minuterie_appelle_son_evenement_puis_recommence()
    {
        MiniZ80 cpu = Cpu();
        EventRoutine(cpu);
        InitEvent(cpu, 0x9316, 0x9400);                       // le bloc d'événement suit le bloc de minuterie
        cpu.H = 0x93;
        cpu.L = 0x10;
        cpu.D = 0;
        cpu.E = 3;                                            // dans 3 cinquantièmes...
        cpu.B = 0;
        cpu.C = 5;                                            // ... puis tous les 5
        cpu.Call(EntryOf(0xBCE9, cpu));                       // KL ADD TICKER

        cpu.Call(0x0038);                                     // chaque interruption de ce test est un début d'image
        cpu.Call(0x0038);
        Assert.Equal(0, cpu.Memory[0x9500]);
        cpu.Call(0x0038);
        Assert.Equal(1, cpu.Memory[0x9500]);

        cpu.Memory[0x9500] = 0;
        for (int i = 0; i < 4; i++)
            cpu.Call(0x0038);
        Assert.Equal(0, cpu.Memory[0x9500]);
        cpu.Call(0x0038);
        Assert.Equal(1, cpu.Memory[0x9500]);

        cpu.H = 0x93;
        cpu.L = 0x10;
        cpu.Call(EntryOf(0xBCEC, cpu));                       // KL DEL TICKER
        cpu.Memory[0x9500] = 0;
        for (int i = 0; i < 10; i++)
            cpu.Call(0x0038);
        Assert.Equal(0, cpu.Memory[0x9500]);
    }

    [Fact]
    public void ON_SQ_arrive_une_fois_quand_la_file_a_une_place()
    {
        MiniZ80 cpu = SoundCpu();
        EventRoutine(cpu);
        InitEvent(cpu, 0x9300, 0x9400);
        cpu.A = 1;
        cpu.H = 0x93;
        cpu.L = 0x00;
        cpu.Call(EntryOf(0xBCB0, cpu));                       // SOUND ARM EVENT, canal A

        Hundredth(cpu);
        Assert.Equal(1, cpu.Memory[0x9500]);                  // la file est vide : tout de suite
        cpu.Memory[0x9500] = 0;
        Hundredth(cpu, 3);
        Assert.Equal(0, cpu.Memory[0x9500]);                  // une seule fois
    }

    [Fact]
    public void Un_programme_avec_TIME_AFTER_EVERY_et_ON_SQ_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 T=TIME:EVERY 50 GOSUB 100:AFTER 200,1 GOSUB 200:ON SQ(1) GOSUB 300\n20 GOTO 20\n100 PRINT \"E\":RETURN\n200 PRINT \"A\":RETURN\n300 SOUND 1,200,10:RETURN");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- sons retenus et rendez-vous (D195) ----------

    [Fact]
    public void Un_son_retenu_attend_RELEASE()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 1 | 0x40, 478, 10);                        // canal A, retenu

        Hundredth(cpu, 2);
        Assert.Equal(0x40 | 3, Check(cpu, 1));                // il attend : retenu, 3 places libres
        cpu.A = 1;
        cpu.Call(EntryOf(0xBCB3, cpu));                       // RELEASE 1
        Hundredth(cpu);
        Assert.Equal(0x80 | 4, Check(cpu, 1));
    }

    [Fact]
    public void Un_rendez_vous_attend_l_autre_canal_puis_ils_partent_ensemble()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 1 | 0x20, 478, 10);                        // canal A, rendez-vous avec C

        Hundredth(cpu, 2);
        Assert.Equal(0x20 | 3, Check(cpu, 1));                // il attend C
        Queue(cpu, 4 | 0x08, 239, 10);                        // canal C, rendez-vous avec A
        Hundredth(cpu);
        Assert.Equal(0x80 | 4, Check(cpu, 1));
        Assert.Equal(0x80 | 4, Check(cpu, 4));
    }

    [Fact]
    public void Un_son_sur_deux_canaux_attend_que_les_deux_soient_libres()
    {
        MiniZ80 cpu = SoundCpu();
        Queue(cpu, 2, 300, 20);                               // le canal B est occupé
        Hundredth(cpu);
        Queue(cpu, 1 | 2, 478, 10);                           // A et B ensemble

        Hundredth(cpu);
        Assert.Equal(0x10 | 3, Check(cpu, 1));                // A attend B
        Hundredth(cpu, 20);                                   // B a fini son premier son : les deux partent
        Assert.Equal(0x80 | 4, Check(cpu, 1));
        Assert.Equal(0x80 | 4, Check(cpu, 2));
    }

    [Fact]
    public void Un_programme_avec_RELEASE_et_des_rendez_vous_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge(
            "10 SOUND 1+16,478,50:SOUND 2+8,239,50:SOUND 4+64,319,50:RELEASE 4:PRINT SQ(4)");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }

    // ---------- nombres à virgule (D196) ----------

    const int RealA = 0x9000, RealB = 0x9010;

    static void PutReal(MiniZ80 cpu, int address, double value) => LocoReal.Encode(value)!.CopyTo(cpu.Memory, address);

    static double GetReal(MiniZ80 cpu, int address) => LocoReal.Decode(cpu.Memory.AsSpan(address, 5));

    // une routine de calcul : HL = RealA (résultat), DE = RealB
    static bool RealCall(MiniZ80 cpu, int firmware, double a, double b = 0, byte accumulator = 0)
    {
        PutReal(cpu, RealA, a);
        PutReal(cpu, RealB, b);
        cpu.H = RealA >> 8;
        cpu.L = RealA & 0xFF;
        cpu.D = RealB >> 8;
        cpu.E = RealB & 0xFF;
        cpu.A = accumulator;
        cpu.Call(EntryOf(firmware, cpu));
        return cpu.CarryFlag;
    }

    static void Close(double expected, double actual, double tolerance = 1e-8) =>
        Assert.True(Math.Abs(actual - expected) <= tolerance * Math.Max(1, Math.Abs(expected)), $"{actual} au lieu de {expected}");

    [Theory]
    [InlineData(0xBD7C, 1.5, 2.25, 3.75)]                    // addition
    [InlineData(0xBD7C, 1e10, -1e10, 0)]
    [InlineData(0xBD82, 1.5, 10, 8.5)]                        // soustraction à l'envers : (DE) - (HL)
    [InlineData(0xBD85, -3.5, 2, -7)]                         // multiplication
    [InlineData(0xBD85, 1e20, 1e-25, 1e-5)]
    [InlineData(0xBD88, 1, 3, 0.333333333333)]                // division
    [InlineData(0xBD88, -22, 7, -3.14285714286)]
    public void Les_quatre_operations(int firmware, double a, double b, double expected)
    {
        MiniZ80 cpu = Cpu();
        Assert.True(RealCall(cpu, firmware, a, b));
        Close(expected, GetReal(cpu, RealA));
    }

    [Fact]
    public void Division_par_zero_et_comparaison()
    {
        MiniZ80 cpu = Cpu();
        Assert.False(RealCall(cpu, 0xBD88, 1, 0));            // division par zéro : retenue à 0
        RealCall(cpu, 0xBD8E, 2.5, 2.4);
        Assert.Equal(1, cpu.A);
        RealCall(cpu, 0xBD8E, -2.5, 2.4);
        Assert.Equal(0xFF, cpu.A);
        RealCall(cpu, 0xBD8E, 7, 7);
        Assert.Equal(0, cpu.A);
    }

    [Fact]
    public void Entiers_et_nombres_a_virgule()
    {
        MiniZ80 cpu = Cpu();
        cpu.H = 0x30;                                         // INTEGER TO REAL : 12345, négatif
        cpu.L = 0x39;
        cpu.A = 0x80;
        cpu.D = RealA >> 8;
        cpu.E = RealA & 0xFF;
        cpu.Call(EntryOf(0xBD64, cpu));
        Assert.Equal(-12345, GetReal(cpu, RealA));

        Assert.True(RealCall(cpu, 0xBD6A, 2.5));              // REAL TO INTEGER : arrondi
        Assert.Equal(3, cpu.H << 8 | cpu.L);
        Assert.False(RealCall(cpu, 0xBD6A, 70000));           // plus de 65535

        Assert.True(RealCall(cpu, 0xBD79, 1.25, 0, 3));       // x 10^3
        Close(1250, GetReal(cpu, RealA));
        Assert.True(RealCall(cpu, 0xBD79, 1.25, 0, 0xFE));    // x 10^-2
        Close(0.0125, GetReal(cpu, RealA));
    }

    [Theory]
    [InlineData(0xBD9D, 2, 1.41421356237)]                    // SQR
    [InlineData(0xBDA3, 10, 2.30258509299)]                   // LOG
    [InlineData(0xBDA6, 1000, 3)]                             // LOG10
    [InlineData(0xBDA9, 1, 2.71828182846)]                    // EXP
    [InlineData(0xBDAC, 1, 0.841470984808)]                   // SIN
    [InlineData(0xBDAF, 3, -0.9899924966)]                    // COS
    [InlineData(0xBDB2, 0.5, 0.546302489844)]                 // TAN
    [InlineData(0xBDB5, 2, 1.10714871779)]                    // ATN
    public void Les_fonctions(int firmware, double x, double expected)
    {
        MiniZ80 cpu = Cpu();
        Assert.True(RealCall(cpu, firmware, x));
        Close(expected, GetReal(cpu, RealA));
    }

    [Fact]
    public void Les_degres_et_la_puissance()
    {
        MiniZ80 cpu = Cpu();
        cpu.A = 1;
        cpu.Call(EntryOf(0xBD97, cpu));                       // DEG
        Assert.True(RealCall(cpu, 0xBDAC, 30));
        Close(0.5, GetReal(cpu, RealA));
        Assert.True(RealCall(cpu, 0xBDB5, 1));
        Close(45, GetReal(cpu, RealA));

        Assert.True(RealCall(cpu, 0xBDA0, 2, 10));            // 2^10 : exact
        Assert.Equal(1024, GetReal(cpu, RealA));
        Assert.True(RealCall(cpu, 0xBDA0, -2, 3));
        Assert.Equal(-8, GetReal(cpu, RealA));
        Assert.True(RealCall(cpu, 0xBDA0, 9, 0.5));
        Close(3, GetReal(cpu, RealA));
        Assert.False(RealCall(cpu, 0xBDA0, -8, 0.5));         // racine d'un négatif : impossible
    }

    [Fact]
    public void Un_programme_avec_des_nombres_a_virgule_va_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 A=1.5:B=SIN(A)*SQR(2)/3:PRINT A,B;PI;2^0.5");

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Problems);
        Assert.NotNull(result.Cartridge);
    }
}
