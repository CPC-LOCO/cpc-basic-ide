// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D207 : fichiers rangés dans la cartouche GX4000 : RUN "nom" y trouve le programme suivant, le recopie et le lance ;
/// CAS IN OPEN, CAS IN DIRECT et CAS IN CLOSE de notre système lisent ces fichiers (LOAD suivra). examples\cartmenu.bas et cartjeu.bas.
/// </summary>
public class CartridgeFilesTests
{
    const int PageSize = CartridgeImage.PageSize;

    static byte[] Pattern(int length, int seed = 0) => Enumerable.Range(0, length).Select(i => (byte)((i + seed) % 251)).ToArray();

    // la cartouche dans le simulateur : la page 0 en &0000, les pages lues en &C000 par le port &DF
    static MiniZ80 Load(params Gx4000System.CartFile[] files)
    {
        CartridgeImage cartridge = Gx4000System.Build([0xC9], null, false, files);
        byte[] cpr = cartridge.ToCprBytes();
        int count = Math.Max(cartridge.PageCount, CartridgeImage.MinPages);
        byte[][] pages = Enumerable.Range(0, count)
            .Select(p => cpr[(20 + p * (8 + PageSize))..(20 + p * (8 + PageSize) + PageSize)]).ToArray();
        var cpu = new MiniZ80 { UpperRomEnabled = false, CartridgePages = pages };
        cpu.Load(pages[0], 0);
        return cpu;
    }

    // l'adresse de notre routine branchée sur une adresse de la ROM (table BRANCHE, qui commence par &BB5A)
    static int EntryOf(int firmware)
    {
        byte[] code = Gx4000System.SystemCode(0);
        int start = Enumerable.Range(0, code.Length - 4).First(i =>
            code[i] == 0x5A && code[i + 1] == 0xBB && code[i + 2] == code[0x41] && code[i + 3] == code[0x42]);
        for (int i = start; code[i] != 0 || code[i + 1] != 0; i += 4)
            if ((code[i] | code[i + 1] << 8) == firmware)
                return code[i + 2] | code[i + 3] << 8;
        throw new InvalidOperationException("adresse non branchée");
    }

    static void Open(MiniZ80 cpu, string name)
    {
        byte[] letters = Encoding.ASCII.GetBytes(name);
        cpu.Load(letters, 0x5000);
        cpu.HL = 0x5000;
        cpu.B = (byte)letters.Length;
        cpu.DE = 0x3000;
        cpu.Call(EntryOf(0xBC77));
    }

    static void Direct(MiniZ80 cpu, int address)
    {
        cpu.HL = (ushort)address;
        cpu.Call(EntryOf(0xBC83));
    }

    [Fact]
    public void Les_routines_des_fichiers_sont_branchees()
    {
        byte[] code = Gx4000System.SystemCode(0);

        Assert.Equal(0xC3, code[Gx4000System.RunFileEntry]);   // &004F : RUN "nom"
        foreach (int firmware in new[] { 0xBC77, 0xBC7A, 0xBC7D, 0xBC83 })
            Assert.InRange(EntryOf(firmware), 0x0050, 0x3FFF);
    }

    [Fact]
    public void Un_fichier_sans_extension_est_trouve_et_recopie_meme_sur_deux_pages()
    {
        byte[] data = Pattern(20000);
        MiniZ80 cpu = Load(new Gx4000System.CartFile("DONNEES.BIN", 2, 0x4000, 0x4321, data));

        Open(cpu, "donnees");                                    // minuscules, sans extension : .BIN trouvé, comme AMSDOS
        Assert.True(cpu.CarryFlag);
        Assert.Equal(2, cpu.A);
        Assert.Equal(0x4000, cpu.DE);
        Assert.Equal(20000, cpu.BC);

        Direct(cpu, 0x4000);
        Assert.True(cpu.CarryFlag);
        Assert.Equal(0x4321, cpu.HL);                            // adresse de lancement
        Assert.Equal(data, cpu.Memory[0x4000..(0x4000 + 20000)]);
        Assert.False(cpu.UpperRomEnabled);                       // ROM haute coupée de nouveau
        Assert.Equal(0, cpu.Memory[0x4000 + 20000]);             // rien de plus
    }

    [Fact]
    public void Un_second_fichier_commence_au_milieu_d_une_page_et_va_sur_l_ecran()
    {
        byte[] first = Pattern(1000, 7), screen = Pattern(16384, 3);
        MiniZ80 cpu = Load(new Gx4000System.CartFile("UN.BIN", 2, 0x4000, 0x4000, first),
                           new Gx4000System.CartFile("ECRAN.BIN", 2, 0xC000, 0xC000, screen));

        Open(cpu, "ECRAN.BIN");
        Assert.True(cpu.CarryFlag);
        Assert.Equal(0xC000, cpu.DE);
        Direct(cpu, 0xC000);                                     // comme LOAD "ECRAN",&C000 : dans la mémoire de l'écran

        Assert.Equal(screen, cpu.Memory[0xC000..0x10000]);
    }

    [Fact]
    public void Un_fichier_absent_n_est_pas_ouvert()
    {
        MiniZ80 cpu = Load(new Gx4000System.CartFile("UN.BIN", 2, 0x4000, 0x4000, Pattern(10)));

        Open(cpu, "ABSENT");
        Assert.False(cpu.CarryFlag);
        Assert.Equal(146, cpu.A);                                // le code d'AMSDOS, lu par DERR

        Open(cpu, "UN.BAS");                                     // l'extension écrite doit être la bonne
        Assert.False(cpu.CarryFlag);
    }

    [Fact]
    public void RUN_recopie_le_programme_suivant_puis_le_lance()
    {
        // LD A,42 / LD (&9000),A / JP 0 (le simulateur s'arrête en &0000)
        byte[] next = [0x3E, 0x2A, 0x32, 0x00, 0x90, 0xC3, 0x00, 0x00];
        MiniZ80 cpu = Load(new Gx4000System.CartFile("PARTIE2.BIN", 2, 0x4000, 0x4000, next));
        cpu.Memory[0xBED8] = 0x12;                                // une minuterie de l'ancien programme

        Open(cpu, "PARTIE2.BIN");
        Assert.True(cpu.CarryFlag);
        cpu.Call(Gx4000System.RunFileEntry);

        Assert.Equal(42, cpu.Memory[0x9000]);
        Assert.Equal(next, cpu.Memory[0x4000..0x4008]);
        Assert.Equal(0, cpu.Memory[0xBED8]);                      // oubliée
        Assert.Equal(0, cpu.Memory[0xBEE0] | cpu.Memory[0xBEE1]); // fichier fermé
    }

    [Fact]
    public void Sur_la_cartouche_RUN_passe_par_notre_systeme()
    {
        const string source = "10 RUN \"PARTIE2\"";
        byte[] Code(bool cartridge)
        {
            var options = CompilerOptions.Default with { Cartridge = cartridge };
            AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
            Assert.False(analysis.HasErrors);
            return CodeGenerator.Generate(analysis.Program, 0x4000, options).Code;
        }
        static bool Has(byte[] code, params byte[] bytes) =>
            Enumerable.Range(0, code.Length - bytes.Length + 1).Any(i => code.AsSpan(i, bytes.Length).SequenceEqual(bytes));

        byte[] cpc = Code(false), cart = Code(true);
        // le petit chargeur du CPC (recopié en &3800) : CALL CAS IN DIRECT / JR NC / PUSH HL / CALL CAS IN CLOSE
        Assert.True(Has(cpc, 0xCD, 0x83, 0xBC, 0x30, 0x06, 0xE5, 0xCD, 0x7A, 0xBC));
        Assert.False(Has(cart, 0xCD, 0x83, 0xBC, 0x30, 0x06, 0xE5, 0xCD, 0x7A, 0xBC));
        Assert.True(Has(cart, 0xC3, (byte)Gx4000System.RunFileEntry, 0x00));
    }

    static ProgramSource? From(Dictionary<string, string> programs, string name) =>
        programs.TryGetValue(name, out string? text) ? new ProgramSource(text, CompilerOptions.Default) : null;

    static bool Contains(byte[] data, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        return Enumerable.Range(0, data.Length - bytes.Length + 1).Any(i => data.AsSpan(i, bytes.Length).SequenceEqual(bytes));
    }

    [Fact]
    public void Les_programmes_lances_par_RUN_sont_ranges_dans_la_cartouche()
    {
        var programs = new Dictionary<string, string> { ["PARTIE2"] = "10 PRINT \"DEUX\":RUN \"MENU\"" };

        CartridgeResult result = Compiler.CompileToCartridge("10 PRINT \"UN\":RUN \"PARTIE2\"", null, "menu",
                                                             n => From(programs, n));

        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Problems);
        Assert.Empty(result.Missing);
        Assert.Equal("PARTIE2", Assert.Single(result.Linked).Name);
        Assert.Equal(2, result.Cartridge!.PageCount);            // la page 0, puis une page pour les deux fichiers
        byte[] cpr = result.Cartridge.ToCprBytes();
        Assert.True(Contains(cpr, "PARTIE2 BIN"));
        Assert.True(Contains(cpr, "MENU    BIN"));                // PARTIE2 revient au menu : il est rangé aussi
    }

    [Fact]
    public void Un_programme_introuvable_est_signale_et_la_cartouche_est_faite()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 RUN \"PARTIE2\"", null, "MENU", _ => null);

        Assert.Equal("PARTIE2", Assert.Single(result.Missing));
        Assert.NotNull(result.Cartridge);
        Assert.Equal(1, result.Cartridge!.PageCount);
    }

    [Fact]
    public void Une_erreur_dans_un_programme_lance_nomme_son_fichier()
    {
        var programs = new Dictionary<string, string> { ["PARTIE2"] = "10 PRINT (" };

        CartridgeResult result = Compiler.CompileToCartridge("10 RUN \"PARTIE2\"", null, "MENU", n => From(programs, n));

        Assert.Null(result.Cartridge);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, d => Assert.Equal("PARTIE2.BAS", d.File));
    }

    [Fact]
    public void Une_cartouche_trop_pleine_est_refusee()
    {
        var files = Enumerable.Range(0, 20).Select(i => new Gx4000System.CartFile($"F{i}.BIN", 2, 0x4000, 0x4000, Pattern(30000))).ToList();

        var error = Assert.Throws<InvalidOperationException>(() => Gx4000System.Build([0xC9], null, false, files));
        Assert.Contains("512", error.Message);
    }
}
