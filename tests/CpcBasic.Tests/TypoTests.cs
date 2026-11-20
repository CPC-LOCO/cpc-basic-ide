// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.IO;
using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Libraries;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Bibliothèque TEXTE (D170 à D173) : TYPO (écriture directe dans l'écran, d'après la RSX TYPO de l'utilisateur, 2020)
/// et DEFTYPO.
/// </summary>
public class TypoTests
{
    const int Origin = 0x4000;
    const int Matrix255 = 0x3800 + 255 * 8;                 // où MiniZ80 range le dessin de 255

    static string ProjectLibraries()
    {
        for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            string candidate = Path.Combine(folder.FullName, "bibliotheques");
            if (Directory.Exists(Path.Combine(candidate, "TEXTE")))
                return candidate;
        }
        throw new DirectoryNotFoundException("dossier bibliotheques introuvable");
    }

    static readonly LibraryCatalog Catalog = LibraryCatalog.Load(ProjectLibraries(), null);

    static MiniZ80 Run(string source, Action<MiniZ80>? setUp = null)
    {
        var options = new CompilerOptions(Libraries: Catalog);
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin, options);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, Origin);
        cpu.Memory[0xBB18] = 0xC9;
        setUp?.Invoke(cpu);
        cpu.Call(Origin);
        return cpu;
    }

    static (int, int) Position(MiniZ80 cpu) => (cpu.GraphicsX, cpu.GraphicsY);

    // « AB » : A = EE EA EA, B = CE AC AE ; chaque ligne prend la moitié gauche de A et la moitié droite de B
    static readonly byte[] AB = [0x00, 0xEC, 0xEE, 0xEA, 0xAC, 0xEA, 0xAE, 0x00];

    // les points d'un caractère, coin en haut à gauche au point x, y de l'écran
    // (points du firmware : x de 0 à 159 / 319 / 639 selon le MODE, y de 0 à 199 depuis le bas)
    static void AssertChar(MiniZ80 cpu, int x, int y, byte[] rows, byte pen = 1, byte paper = 0)
    {
        int unit = cpu.ScreenMode switch { 0 => 4, 1 => 2, _ => 1 };   // GetPixel prend les coordonnées du BASIC
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                Assert.True(cpu.GetPixel((x + c) * unit, (y - r) * 2) == ((rows[r] & (0x80 >> c)) != 0 ? pen : paper),
                            $"MODE {cpu.ScreenMode}, ligne {r}, point {c}");
    }

    [Fact]
    public void TYPO_est_au_catalogue_sans_erreur()
    {
        Assert.Empty(Catalog.Errors);
        LibraryCommand typo = Catalog.Find("TYPO")!;
        Assert.Equal("TEXTE", typo.Library);
        Assert.Equal("TYPO message$ [, x] [, y]", LibraryCommandFile.Signature(typo));
        Assert.Equal(LibraryKind.Routine, Catalog.Find("TYPO_FONTE")!.Kind);
        Assert.Equal(LibraryKind.Routine, Catalog.Find("TYPO_ADRESSE")!.Kind);
        Assert.Null(Catalog.Find("TYPOVITE"));                   // renommée TYPO (D173)
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Deux_petits_caracteres_font_un_caractere_dans_les_3_MODE(byte mode)
    {
        MiniZ80 cpu = Run("10 TYPO \"AB\", 0, 399", c => c.ScreenMode = mode);

        AssertChar(cpu, 0, 199, AB);
    }

    [Fact]
    public void Un_nombre_impair_finit_par_un_espace()
    {
        MiniZ80 cpu = Run("10 A$ = \"ABA\" : TYPO A$, 0, 399");

        AssertChar(cpu, 0, 199, AB);
        AssertChar(cpu, 8, 199, [0x00, 0xE0, 0xE0, 0xE0, 0xA0, 0xE0, 0xA0, 0x00]);
    }

    [Fact]
    public void Un_caractere_inconnu_devient_un_espace()
    {
        MiniZ80 cpu = Run("10 GRAPHICS PEN 1 : GRAPHICS PAPER 2 : TYPO CHR$(126) + CHR$(10), 0, 399");

        AssertChar(cpu, 0, 199, new byte[8], pen: 1, paper: 2);
    }

    [Fact]
    public void TYPO_prend_le_stylo_et_le_fond_du_trace()
    {
        MiniZ80 cpu = Run("10 GRAPHICS PEN 3 : GRAPHICS PAPER 2 : TYPO \"AB\", 0, 399");

        AssertChar(cpu, 0, 199, AB, pen: 3, paper: 2);
    }

    [Fact]
    public void TYPO_arrondit_x_au_debut_d_un_octet_et_avance_le_curseur()
    {
        MiniZ80 cpu = Run("10 TYPO \"ABAB\", 100, 201");          // MODE 1 : x 100 -> point 50 -> octet du point 48

        AssertChar(cpu, 48, 100, AB);                            // le caractère passe d'un rang de l'écran au suivant
        AssertChar(cpu, 56, 100, AB);
        Assert.Equal((132, 201), Position(cpu));                 // deux caractères de 16 : 100 + 32
    }

    [Fact]
    public void Sans_position_au_curseur_et_avec_en_x_y()
    {
        Assert.Equal((16, 0), Position(Run("10 TYPO \"AB\"")));          // trop bas pour être écrit : le curseur avance
        Assert.Equal((116, 200), Position(Run("10 TYPO \"AB\", 100, 200")));
        Assert.Equal((116, 0), Position(Run("10 TYPO \"AB\", 100")));
        Assert.Equal((16, 300), Position(Run("10 TYPO \"AB\", , 300")));
    }

    [Fact]
    public void TYPO_n_ecrit_pas_hors_de_l_ecran()
    {
        MiniZ80 cpu = Run("10 TYPO \"ABAB\", 600, 10");           // trop bas, et le 2e trop à droite

        Assert.All(cpu.Memory[0xC000..0x10000], b => Assert.Equal(0, b));
    }

    [Fact]
    public void Un_texte_vide_n_ecrit_rien()
    {
        MiniZ80 cpu = Run("10 TYPO \"\", 100, 200");

        Assert.All(cpu.Memory[0xC000..0x10000], b => Assert.Equal(0, b));
        Assert.Equal((100, 200), Position(cpu));
    }

    [Fact]
    public void TYPO_ne_touche_pas_au_caractere_255()
    {
        MiniZ80 cpu = Run("10 TYPO \"BONJOUR\", 0, 399", c =>
        {
            for (int i = 0; i < 8; i++)
                c.Memory[Matrix255 + i] = (byte)(0x11 * (i + 1));
        });

        Assert.Equal("11 22 33 44 55 66 77 88", string.Join(" ", cpu.Memory[Matrix255..(Matrix255 + 8)].Select(b => b.ToString("X2"))));
        Assert.Empty(cpu.GraphicChars);                         // sans GRA WR CHAR
        Assert.Equal(0, cpu.MatrixReads);
    }

    [Fact]
    public void La_place_de_TYPO_se_mesure()
    {
        var typo = LibraryLinker.Size(Catalog, "TYPO")!.Value;

        Assert.Equal(new[] { "TYPO_ADRESSE", "TYPO_FONTE" }, typo.Called);
        Assert.True(typo.Total - typo.Own >= 279);                // la fonte : 93 x 3 octets
    }

    // ---------- DEFTYPO (D171) ----------

    [Fact]
    public void DEFTYPO_est_au_catalogue()
    {
        Assert.Equal("DEFTYPO caractere$, l1, l2, l3", LibraryCommandFile.Signature(Catalog.Find("DEFTYPO")!));
    }

    [Fact]
    public void DEFTYPO_redessine_un_petit_caractere()
    {
        MiniZ80 cpu = Run("10 DEFTYPO \"A\", &12, &34, &56\n20 TYPO \"AA\", 0, 399");

        AssertChar(cpu, 0, 199, [0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x00]);
    }

    [Theory]
    [InlineData("10 DEFTYPO \"~\", &FF, &FF, &FF")]               // pas dans la fonte : l'espace ne change pas
    [InlineData("10 DEFTYPO \"\", &FF, &FF, &FF")]                // texte vide : rien
    public void DEFTYPO_ignore_ce_qui_n_est_pas_dans_la_fonte(string first)
    {
        MiniZ80 cpu = Run(first + "\n20 GRAPHICS PEN 1 : GRAPHICS PAPER 2 : TYPO \"  \", 0, 399");

        AssertChar(cpu, 0, 199, new byte[8], pen: 1, paper: 2);
    }
}
