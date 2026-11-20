// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>Répertoire de distribution : ce qu'il faut donner avec un programme (D161).</summary>
public class DistributionTests
{
    // PROJET : commande du projet (protégée) ; LUNE (commande) appelle la routine AIDE ; DOUBLE (fonction) ; INUTILE
    static readonly LibraryCatalog Catalog = LibraryCatalog.Build(
    [
        new CatalogSource("GRAPHIQUE", "PROJET.cmd.asm", ";@NOM PROJET\n;@GENRE COMMANDE\n  RET", true),
        new CatalogSource("DECOR", "LUNE.cmd.asm", ";@NOM LUNE\n;@GENRE COMMANDE\n;@VALEUR N, 0, 9\n  CALL AIDE\n  RET", false),
        new CatalogSource("DECOR", "AIDE.cmd.asm", ";@NOM AIDE\n;@GENRE ROUTINE\n  RET", false),
        new CatalogSource("CALCUL", "DOUBLE.cmd.asm", ";@NOM DOUBLE\n;@GENRE FONCTION\n;@VALEUR N, 0, 9\n  RET", false),
        new CatalogSource("CALCUL", "INUTILE.cmd.asm", ";@NOM INUTILE\n;@GENRE COMMANDE\n  RET", false)
    ]);

    static readonly CompilerOptions Options = new(Libraries: Catalog);

    [Fact]
    public void Les_commandes_utilisees_et_leurs_routines_sont_donnees()
    {
        DistributionPlan plan = Distribution.Plan("jeu", "10 LUNE 3:PROJET:PRINT DOUBLE(2)", Options, _ => null);

        Assert.Equal(new[] { "JEU" }, plan.Programs);
        Assert.Equal(new[] { "AIDE", "DOUBLE", "LUNE" }, plan.Commands.Select(c => c.Name));
        Assert.Equal(new[] { "PROJET" }, plan.ProjectCommands.Select(c => c.Name));
        Assert.Empty(plan.Missing);
    }

    [Fact]
    public void Les_programmes_lances_par_RUN_sont_donnes_aussi()
    {
        DistributionPlan plan = Distribution.Plan("menu", "10 RUN \"NIVEAU1\":RUN \"NIVEAU2\"", Options,
            name => name == "NIVEAU1" ? new ProgramSource("10 X=DOUBLE(4)", CompilerOptions.Default) : null);

        Assert.Equal(new[] { "MENU", "NIVEAU1" }, plan.Programs);
        Assert.Equal(new[] { "DOUBLE" }, plan.Commands.Select(c => c.Name));     // vue dans NIVEAU1, avec le catalogue
        Assert.Equal(new[] { "NIVEAU2" }, plan.Missing);
    }

    [Fact]
    public void Un_programme_sans_commande_ajoutee_ne_donne_que_lui_meme()
    {
        DistributionPlan plan = Distribution.Plan("simple", "10 PRINT 1", Options, _ => null);

        Assert.Equal(new[] { "SIMPLE" }, plan.Programs);
        Assert.Empty(plan.Commands);
        Assert.Empty(plan.ProjectCommands);
    }
}
