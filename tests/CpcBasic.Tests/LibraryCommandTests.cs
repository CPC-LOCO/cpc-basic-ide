// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;
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
    public void Une_option_a_sa_valeur_si_elle_est_sautee()
    {
        var (command, errors) = Read(";@NOM STAR\n;@GENRE COMMANDE\n;@VALEUR X, 0, 639\n;@OPTION STYLO, 0, 15, -1, couleur, du contour\n  RET");

        Assert.Empty(errors);
        Assert.False(command!.Parameters[0].IsOptional);
        Assert.Equal(new LibraryParameter("STYLO", 0, 15, "couleur, du contour", -1), command.Parameters[1]);
        Assert.True(command.Parameters[1].IsOptional);
    }

    [Theory]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@OPTION S, 0, 15, -1\n;@VALEUR X, 0, 9", "après une OPTION")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@OPTION S, 0, 15", "valeur si elle est sautée")]
    [InlineData(";@NOM STAR\n;@GENRE COMMANDE\n;@OPTION S, 0, 15, rien", "« rien »")]
    public void Une_option_mal_ecrite_est_refusee(string text, string word)
    {
        var (command, errors) = Read(text);

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
    [InlineData(LibraryKind.Command)]
    [InlineData(LibraryKind.Function)]
    public void Le_questionnaire_donne_un_fichier_qui_se_relit(LibraryKind kind)
    {
        var values = new[] { new LibraryParameter("X", 0, 639, "position, en points"), new LibraryParameter("STYLO", 0, 15, "couleur", -1) };
        string code = LibraryCommandFile.StarterCode("lune", kind, values);
        string text = LibraryCommandFile.Compose("lune", kind, values, "Dessine une lune.\r\n\r\nDeux lignes d'aide.", " Moi ", code);

        var (command, errors) = LibraryCommandFile.Parse(text, "decor", "LUNE.cmd.asm", false);

        Assert.Empty(errors);
        Assert.Equal("LUNE", command!.Name);
        Assert.Equal(kind, command.Kind);
        Assert.Equal(values, command.Parameters);
        Assert.Equal("Dessine une lune.\nDeux lignes d'aide.", command.Help);
        Assert.Equal("Moi", command.Author);
        Assert.Contains(";@OPTION STYLO, 0, 15, -1, couleur", text);
        Assert.Contains("LD HL,(X) LD HL,(STYLO)", text);
        Assert.EndsWith("        RET\n", text);
        Assert.Empty(Z80Assembler.Assemble(text, new Z80Emitter(0x4000), "t_", n => n is "X" or "STYLO" ? "case" : null));
    }

    [Fact]
    public void Une_routine_neuve_explique_les_registres()
    {
        string code = LibraryCommandFile.StarterCode("MA_ROUTINE", LibraryKind.Routine, []);
        string text = LibraryCommandFile.Compose("MA_ROUTINE", LibraryKind.Routine, [], "", "", code);

        Assert.Empty(LibraryCommandFile.Parse(text, "OUTILS", "MA_ROUTINE.cmd.asm", false).Errors);
        Assert.Contains("registres", text);
        Assert.DoesNotContain(";@AIDE", text);
        Assert.DoesNotContain(";@AUTEUR", text);
    }

    [Theory]
    [InlineData("STAR.cmd.asm", "STAR")]
    [InlineData("C:\\biblio\\GRAPHIQUE\\star.CMD.ASM", "star")]
    public void Le_nom_du_fichier_est_retrouve(string file, string stem)
    {
        Assert.Equal(stem, LibraryCommandFile.StemOf(file));
    }
}
