// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot J4 (D087, idée de l'utilisateur) : un RUN "nom" écrit en clair fait compiler aussi le programme « nom », mis sur
/// la même disquette. examples\partie1.bas et examples\partie2.bas.
/// </summary>
public class LinkedCompileTests
{
    static CompileResult Compile(string main, Dictionary<string, string> folder) =>
        Compiler.CompileWithLinked(main, "jeu", null,
            name => folder.TryGetValue(name, out string? text) ? new ProgramSource(text, CompilerOptions.Default) : null);

    [Fact]
    public void Les_programmes_lances_vont_sur_la_meme_disquette()
    {
        var folder = new Dictionary<string, string>
        {
            ["PARTIE2"] = "10 PRINT 2:RUN \"PARTIE3.BIN\"",
            ["PARTIE3"] = "10 PRINT 3:RUN \"JEU\""          // retour au premier : pas compilé deux fois
        };

        CompileResult result = Compile("10 PRINT 1:RUN \"partie2\"", folder);

        Assert.True(result.Success);
        Assert.Equal(new[] { "JEU.BAS", "JEU.BIN", "PARTIE2.BAS", "PARTIE2.BIN", "PARTIE3.BAS", "PARTIE3.BIN" },
                     result.Disk!.FileNames);
        Assert.Equal(new[] { "PARTIE2", "PARTIE3" }, result.Linked.Select(l => l.Name));
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void Un_programme_absent_est_seulement_signale()
    {
        CompileResult result = Compile("10 RUN \"AILLEURS\"", new());

        Assert.True(result.Success);
        Assert.Equal(new[] { "AILLEURS" }, result.Missing);
        Assert.Equal(new[] { "JEU.BAS", "JEU.BIN" }, result.Disk!.FileNames);
    }

    [Fact]
    public void Une_erreur_dans_l_autre_programme_donne_son_nom()
    {
        CompileResult result = Compile("10 RUN \"PARTIE2\"", new() { ["PARTIE2"] = "10 PRINT \"A\" \"B\"" });

        Assert.False(result.Success);
        Diagnostic error = result.Diagnostics[0];
        Assert.Equal("PARTIE2.BAS", error.File);
        Assert.StartsWith("ERREUR dans PARTIE2.BAS, ligne 1 :", error.Message);   // ligne du fichier, comme dans l'éditeur
    }

    [Theory]
    [InlineData("10 N$=\"PARTIE2\":RUN N$")]       // nom calculé : le compilateur ne peut pas le deviner
    [InlineData("10 RUN \"PARTIE2.BAS\"")]          // pas un programme compilé
    public void Seulement_les_noms_ecrits_en_clair(string main)
    {
        CompileResult result = Compile(main, new() { ["PARTIE2"] = "10 PRINT 2" });

        Assert.Empty(result.Linked);
    }
}
