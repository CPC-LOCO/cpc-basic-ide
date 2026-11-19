// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>Catalogue des bibliothèques (D147).</summary>
public class LibraryCatalogTests
{
    static string File(string name, string kind = "COMMANDE") => $";@NOM {name}\n;@GENRE {kind}\n  RET";

    static CatalogSource Source(string library, string name, bool isProtected, string kind = "COMMANDE") =>
        new(library, name + LibraryCommandFile.Extension, File(name, kind), isProtected);

    [Fact]
    public void Les_commandes_sont_retrouvees_par_leur_nom()
    {
        var catalog = LibraryCatalog.Build(
        [
            Source("graphique", "STAR", true),
            Source("SON", "BIP", true),
            Source("GRAPHIQUE", "LUNE", false, "FONCTION")
        ]);

        Assert.Empty(catalog.Errors);
        Assert.Equal("GRAPHIQUE", catalog.Find("star")!.Library);
        Assert.Equal(LibraryKind.Function, catalog.Find("LUNE")!.Kind);
        Assert.Null(catalog.Find("SOLEIL"));
        Assert.Equal(new[] { "GRAPHIQUE", "SON" }, catalog.Libraries);
        Assert.Equal(new[] { "LUNE", "STAR", "BIP" }, catalog.All.Select(c => c.Name));
    }

    [Fact]
    public void Une_bibliotheque_du_projet_et_du_programmeur_n_en_font_qu_une()
    {
        var catalog = LibraryCatalog.Build([Source("GRAPHIQUE", "STAR", true), Source("GRAPHIQUE", "LUNE", false)]);

        Assert.Single(catalog.Libraries);
        Assert.True(catalog.Find("STAR")!.Protected);
        Assert.False(catalog.Find("LUNE")!.Protected);
    }

    [Fact]
    public void Un_nom_deja_pris_est_refuse()
    {
        var catalog = LibraryCatalog.Build([Source("GRAPHIQUE", "STAR", true), Source("DECOR", "STAR", false)]);

        Assert.True(catalog.Find("STAR")!.Protected);
        var error = Assert.Single(catalog.Errors);
        Assert.Contains("DECOR", error.FileName);
        Assert.Contains("GRAPHIQUE", error.Message);
    }

    [Fact]
    public void Un_fichier_faux_est_refuse_avec_sa_ligne()
    {
        var catalog = LibraryCatalog.Build(
        [
            new CatalogSource("GRAPHIQUE", "STAR.cmd.asm", ";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 9, 0", false)
        ]);

        Assert.Null(catalog.Find("STAR"));
        var error = Assert.Single(catalog.Errors);
        Assert.Equal(3, error.Line);
        Assert.Equal("GRAPHIQUE\\STAR.cmd.asm, ligne 3 : " + error.Message, error.ToString());
    }

    [Fact]
    public void Un_mauvais_nom_de_bibliotheque_est_refuse()
    {
        var catalog = LibraryCatalog.Build([Source("Mes outils", "STAR", false)]);

        Assert.Null(catalog.Find("STAR"));
        Assert.Contains("Mes outils", Assert.Single(catalog.Errors).Message);
    }

    [Fact]
    public void Les_dossiers_sont_lus()
    {
        string root = Path.Combine(Path.GetTempPath(), "cpcbasic-catalogue-" + Guid.NewGuid().ToString("N"));
        try
        {
            string project = Path.Combine(root, "projet"), user = Path.Combine(root, "perso");
            Directory.CreateDirectory(Path.Combine(project, "GRAPHIQUE"));
            Directory.CreateDirectory(Path.Combine(user, "GRAPHIQUE"));
            System.IO.File.WriteAllText(Path.Combine(project, "GRAPHIQUE", "STAR.cmd.asm"), File("STAR"));
            System.IO.File.WriteAllText(Path.Combine(user, "GRAPHIQUE", "LUNE.cmd.asm"), File("LUNE"));
            System.IO.File.WriteAllText(Path.Combine(user, "GRAPHIQUE", "notes.txt"), "pas une commande");
            System.IO.File.WriteAllText(Path.Combine(user, "PERDU.cmd.asm"), File("PERDU"));

            var catalog = LibraryCatalog.Load(project, user);

            Assert.True(catalog.Find("STAR")!.Protected);
            Assert.False(catalog.Find("LUNE")!.Protected);
            Assert.Null(catalog.Find("PERDU"));
            Assert.Contains("aucune bibliothèque", Assert.Single(catalog.Errors).Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Un_dossier_absent_donne_un_catalogue_vide()
    {
        var catalog = LibraryCatalog.Load(null, Path.Combine(Path.GetTempPath(), "dossier-qui-n-existe-pas-" + Guid.NewGuid()));

        Assert.Empty(catalog.All);
        Assert.Empty(catalog.Errors);
    }
}
