// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>Lecture des fichiers de commande des bibliothèques (D146).</summary>
public class LibraryCommandTests
{
    const string Star = """
        ;@NOM STAR
        ;@GENRE COMMANDE
        ;@VALEUR X, 0, 639, position horizontale du centre
        ;@VALEUR Y, 0, 399, position verticale du centre
        ;@VALEUR rayon, 1, &FF
        ;@AIDE Dessine une étoile
        ;@AIDE à cinq branches.
        ;@AUTEUR RetroPoke
                LD HL,(X)
                RET
        """;

    static (LibraryCommand? Command, IReadOnlyList<LibraryError> Errors) Read(string text, string file = "STAR.cmd.asm") =>
        LibraryCommandFile.Parse(text, "graphique", file, isProtected: true);

    [Fact]
    public void Un_fichier_complet_est_lu()
    {
        var (command, errors) = Read(Star);

        Assert.Empty(errors);
        Assert.NotNull(command);
        Assert.Equal("STAR", command.Name);
        Assert.Equal(LibraryKind.Command, command.Kind);
        Assert.Equal("GRAPHIQUE", command.Library);
        Assert.True(command.Protected);
        Assert.Equal("Dessine une étoile\nà cinq branches.", command.Help);
        Assert.Equal("RetroPoke", command.Author);
        Assert.Equal(
            new[]
            {
                new LibraryParameter("X", 0, 639, "position horizontale du centre"),
                new LibraryParameter("Y", 0, 399, "position verticale du centre"),
                new LibraryParameter("RAYON", 1, 255, "")
            },
            command.Parameters);
        Assert.Contains("LD HL,(X)", command.Code);
    }

    [Fact]
    public void Une_fonction_et_une_routine_sont_lues()
    {
        var (function, e1) = Read(";@nom carre\n;@genre fonction\n;@valeur n, -181, 181\n  RET", "carre.cmd.asm");
        var (routine, e2) = Read(";@NOM POINT\n;@GENRE ROUTINE\n;@AIDE DE = x, HL = y\n  RET", "POINT.cmd.asm");

        Assert.Empty(e1);
        Assert.Equal(LibraryKind.Function, function!.Kind);
        Assert.Equal(-181, function.Parameters[0].Min);
        Assert.Empty(e2);
        Assert.Equal(LibraryKind.Routine, routine!.Kind);
        Assert.Empty(routine.Parameters);
    }

    [Theory]
    [InlineData(";@GENRE COMMANDE\n RET", "NOM")]
    [InlineData(";@NOM STAR\n RET", "GENRE")]
    [InlineData(";@NOM STAR\n;@GENRE MACHIN", "COMMANDE, FONCTION ou ROUTINE")]
    [InlineData(";@NOM ETOILE\n;@GENRE COMMANDE", "doivent être pareils")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@COULEUR 3", "COULEUR")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 0", "plus petite valeur")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 0, 9\n;@VALEUR x, 0, 9", "déjà déclarée")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 9, 0", "dépasse")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 0, 70000", "70000")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR HL, 0, 9", "registre")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR 2X, 0, 9", "une lettre au début")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR STAR, 0, 9", "comme la commande")]
    [InlineData(";@NOM STAR\n;@GENRE ROUTINE\n;@VALEUR X, 0, 9", "registres")]
    [InlineData(";@NOM STAR\n;@NOM STAR\n;@GENRE COMMANDE", "deux fois")]
    [InlineData(";@NOM PLOT\n;@GENRE COMMANDE", "déjà un mot du BASIC", "PLOT.cmd.asm")]
    [InlineData(";@NOM FNDOUBLE\n;@GENRE FONCTION", "DEF FN", "FNDOUBLE.cmd.asm")]
    [InlineData(";@NOM MA_STAR\n;@GENRE COMMANDE", "ne peut pas contenir _", "MA_STAR.cmd.asm")]
    public void Une_faute_donne_un_message_clair(string text, string word, string file = "STAR.cmd.asm")
    {
        var (command, errors) = Read(text, file);

        Assert.Null(command);
        Assert.Contains(errors, e => e.Message.Contains(word));
    }

    [Fact]
    public void Une_routine_peut_contenir_un_souligne()
    {
        var (routine, errors) = Read(";@NOM MA_ROUTINE_PLOT\n;@GENRE ROUTINE\n  RET", "MA_ROUTINE_PLOT.cmd.asm");

        Assert.Empty(errors);
        Assert.Equal("MA_ROUTINE_PLOT", routine!.Name);
    }

    [Fact]
    public void Le_numero_de_ligne_est_donne()
    {
        var (_, errors) = Read(";@NOM STAR\n;@GENRE COMMANDE\n\n;@VALEUR X, 9, 0");

        Assert.Equal(4, Assert.Single(errors).Line);
    }

    [Theory]
    [InlineData("STAR.cmd.asm", "STAR")]
    [InlineData("C:\\biblio\\GRAPHIQUE\\star.CMD.ASM", "star")]
    public void Le_nom_du_fichier_est_retrouve(string file, string stem)
    {
        Assert.Equal(stem, LibraryCommandFile.StemOf(file));
    }
}
