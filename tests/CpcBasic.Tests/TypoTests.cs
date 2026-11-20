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

/// <summary>D170 : TYPO, bibliothèque TEXTE (d'après la RSX TYPO de l'utilisateur, 2020).</summary>
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

    static string Hex(byte[] rows) => string.Join(" ", rows.Select(b => b.ToString("X2")));

    [Fact]
    public void TYPO_est_au_catalogue_sans_erreur()
    {
        Assert.Empty(Catalog.Errors);
        LibraryCommand typo = Catalog.Find("TYPO")!;
        Assert.Equal("TEXTE", typo.Library);
        Assert.Equal("TYPO message$ [, x] [, y]", LibraryCommandFile.Signature(typo));
        Assert.Equal(LibraryKind.Routine, Catalog.Find("TYPO_FONTE")!.Kind);
        Assert.Equal(LibraryKind.Routine, Catalog.Find("TYPO_ADRESSE")!.Kind);
    }

    [Fact]
    public void Deux_petits_caracteres_font_un_caractere()
    {
        // A = EE EA EA, B = CE AC AE : chaque ligne prend la moitié gauche de A et la moitié droite de B
        MiniZ80 cpu = Run("10 TYPO \"AB\"");

        Assert.Equal("00 EC EE EA AC EA AE 00", Hex(Assert.Single(cpu.GraphicChars)));
    }

    [Fact]
    public void Un_nombre_impair_finit_par_un_espace()
    {
        MiniZ80 cpu = Run("10 A$ = \"ABA\" : TYPO A$");

        Assert.Equal(2, cpu.GraphicChars.Count);
        Assert.Equal("00 E0 E0 E0 A0 E0 A0 00", Hex(cpu.GraphicChars[1]));
    }

    [Fact]
    public void Un_caractere_inconnu_devient_un_espace()
    {
        MiniZ80 cpu = Run("10 TYPO CHR$(126) + CHR$(10)");

        Assert.Equal("00 00 00 00 00 00 00 00", Hex(Assert.Single(cpu.GraphicChars)));
    }

    [Fact]
    public void Le_dessin_de_255_est_remis_comme_avant()
    {
        MiniZ80 cpu = Run("10 TYPO \"BONJOUR\"", c =>
        {
            for (int i = 0; i < 8; i++)
                c.Memory[Matrix255 + i] = (byte)(0x11 * (i + 1));
        });

        Assert.Equal(4, cpu.GraphicChars.Count);
        Assert.Equal("11 22 33 44 55 66 77 88", Hex(cpu.Memory[Matrix255..(Matrix255 + 8)]));
    }

    [Fact]
    public void Sans_position_le_curseur_ne_bouge_pas_et_avec_il_y_va()
    {
        Assert.Equal((0, 0), Position(Run("10 TYPO \"AB\"")));
        Assert.Equal((100, 200), Position(Run("10 TYPO \"AB\", 100, 200")));
        Assert.Equal((100, 0), Position(Run("10 TYPO \"AB\", 100")));
        Assert.Equal((0, 300), Position(Run("10 TYPO \"AB\", , 300")));
    }

    static (int, int) Position(MiniZ80 cpu) => (cpu.GraphicsX, cpu.GraphicsY);

    [Fact]
    public void Un_texte_vide_n_ecrit_rien()
    {
        Assert.Empty(Run("10 TYPO \"\"").GraphicChars);
    }

    [Fact]
    public void Si_255_n_est_pas_redessinable_TYPO_lui_donne_une_table()
    {
        MiniZ80 cpu = Run("10 TYPO \"AB\"", c => c.FirstUserChar = 256);

        Assert.Contains(cpu.MatrixTables, t => t.First == 255);
        Assert.Single(cpu.GraphicChars);
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
        MiniZ80 cpu = Run("10 DEFTYPO \"A\", &12, &34, &56\n20 TYPO \"AA\"");

        Assert.Equal("00 11 22 33 44 55 66 00", Hex(Assert.Single(cpu.GraphicChars)));
    }

    [Theory]
    [InlineData("10 DEFTYPO \"~\", &FF, &FF, &FF\n20 TYPO \"  \"")]     // pas dans la fonte : l'espace ne change pas
    [InlineData("10 DEFTYPO \"\", &FF, &FF, &FF\n20 TYPO \"  \"")]      // texte vide : rien
    public void DEFTYPO_ignore_ce_qui_n_est_pas_dans_la_fonte(string source)
    {
        Assert.Equal("00 00 00 00 00 00 00 00", Hex(Assert.Single(Run(source).GraphicChars)));
    }
}
