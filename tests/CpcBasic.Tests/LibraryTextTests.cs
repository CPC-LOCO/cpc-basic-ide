// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Libraries;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D169 : une valeur TEXTE dans une commande ou une fonction de bibliothèque.</summary>
public class LibraryTextTests
{
    const int Origin = 0x4000;

    // AFFICHE message$ [, fin] : affiche le texte, puis le caractère « fin » (sauté : rien) ; LONGUEUR(t$) : sa longueur
    const string Affiche = """
        ;@NOM AFFICHE
        ;@GENRE COMMANDE
        ;@TEXTE MESSAGE, ce qui est affiché
        ;@OPTION FIN, 32, 255, 0, caractère ajouté à la fin
                LD HL,(MESSAGE)
                LD B,(HL)
                INC HL
                LD E,(HL)
                INC HL
                LD D,(HL)
                LD A,B
                OR A
                JR Z,SUITE
        BOUCLE: LD A,(DE)
                CALL &BB5A
                INC DE
                DJNZ BOUCLE
        SUITE:  LD A,(FIN)
                OR A
                RET Z
                CALL &BB5A
                RET
        """;

    static readonly LibraryCatalog Catalog = LibraryCatalog.Build(
    [
        new CatalogSource("TEXTE", "AFFICHE.cmd.asm", Affiche, false),
        new CatalogSource("TEXTE", "LONGUEUR.cmd.asm", """
            ;@NOM LONGUEUR
            ;@GENRE FONCTION
            ;@TEXTE T
                    LD HL,(T)
                    LD L,(HL)
                    LD H,0
                    RET
            """, false)
    ]);

    static readonly CompilerOptions Options = new(Libraries: Catalog);

    static string Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, Options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin, Options);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, Origin);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(Origin);
        return cpu.Output.ToString();
    }

    [Fact]
    public void La_ligne_TEXTE_donne_une_valeur_texte()
    {
        var (command, errors) = LibraryCommandFile.Parse(Affiche, "TEXTE", "AFFICHE.cmd.asm", false);

        Assert.Empty(errors);
        Assert.True(command!.Parameters[0].IsText);
        Assert.False(command.Parameters[1].IsText);
        Assert.Equal("AFFICHE message$ [, fin]", LibraryCommandFile.Signature(command));
    }

    [Fact]
    public void Le_questionnaire_reecrit_la_ligne_TEXTE()
    {
        var (command, _) = LibraryCommandFile.Parse(Affiche, "TEXTE", "AFFICHE.cmd.asm", false);
        string text = LibraryCommandFile.Compose("AFFICHE", LibraryKind.Command, command!.Parameters, "", "", "  RET");

        Assert.Contains(";@TEXTE MESSAGE, ce qui est affiché\n", text);
        var (again, errors) = LibraryCommandFile.Parse(text, "TEXTE", "AFFICHE.cmd.asm", false);
        Assert.Empty(errors);
        Assert.Equal(command.Parameters, again!.Parameters);
    }

    [Fact]
    public void Un_TEXTE_apres_une_OPTION_est_refuse()
    {
        var (_, errors) = LibraryCommandFile.Parse(";@NOM ZZ\n;@GENRE COMMANDE\n;@OPTION N, 0, 9, 0\n;@TEXTE T\n RET",
                                                   "TEXTE", "ZZ.cmd.asm", false);

        Assert.Contains(errors, e => e.Message.Contains("TEXTE ne peut pas venir après une OPTION"));
    }

    [Theory]
    [InlineData("10 AFFICHE \"BONJOUR\"", "BONJOUR")]
    [InlineData("10 A$ = \"SALUT\" : AFFICHE A$, 33", "SALUT!")]
    [InlineData("10 A$ = \"BON\" : AFFICHE A$ + \"JOUR\" + A$", "BONJOURBON")]
    [InlineData("10 AFFICHE LEFT$(\"ABCDEF\", 3)", "ABC")]
    [InlineData("10 AFFICHE \"\", 65", "A")]
    [InlineData("10 DIM T$(2) : T$(1) = \"CASE\" : AFFICHE T$(1)", "CASE")]
    public void Une_commande_recoit_un_texte(string source, string expected)
    {
        Assert.StartsWith(expected, Run(source));
    }

    [Fact]
    public void Une_fonction_recoit_un_texte()
    {
        Assert.StartsWith(" 7 ", Run("10 A$ = \"CPC\" : PRINT LONGUEUR(A$ + \"6128\")"));
    }

    [Theory]
    [InlineData("10 AFFICHE 5")]
    [InlineData("10 AFFICHE \"A\", \"B\"")]
    [InlineData("10 X = LONGUEUR(3)")]
    public void Un_texte_et_un_nombre_ne_se_melangent_pas(string source)
    {
        Diagnostic error = Assert.Single(BasicAnalyzer.Analyze(source, Options).Diagnostics);

        Assert.Equal(DiagnosticCode.TypeMismatch, error.Code);
    }
}
