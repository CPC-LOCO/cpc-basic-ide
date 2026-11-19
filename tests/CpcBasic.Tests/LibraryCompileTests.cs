// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>Commandes et fonctions des bibliothèques dans un programme BASIC (D148).</summary>
public class LibraryCompileTests
{
    const int Origin = 0x4000;

    // LETTRE code : affiche un caractère ; DOUBLE(n) : 2 × n, par la routine FOIS2 ; SOMME(a, b) ; DEUX : 2
    static readonly LibraryCatalog Catalog = LibraryCatalog.Build(
    [
        new CatalogSource("TEXTE", "LETTRE.cmd.asm", """
            ;@NOM LETTRE
            ;@GENRE COMMANDE
            ;@VALEUR CODE, 32, 255, code du caractère
                    LD A,(CODE)
                    CALL &BB5A
                    RET
            """, true),
        new CatalogSource("CALCUL", "DOUBLE.cmd.asm", """
            ;@NOM DOUBLE
            ;@GENRE FONCTION
            ;@VALEUR N, -16384, 16383
                    LD HL,(N)
                    CALL FOIS2
                    RET
            """, true),
        new CatalogSource("CALCUL", "FOIS2.cmd.asm", """
            ;@NOM FOIS2
            ;@GENRE ROUTINE
            ;@AIDE HL = HL * 2
                    ADD HL,HL
                    RET
            """, true),
        new CatalogSource("CALCUL", "SOMME.cmd.asm", """
            ;@NOM SOMME
            ;@GENRE FONCTION
            ;@VALEUR A1, -32768, 32767
            ;@VALEUR B1, -32768, 32767
                    LD HL,(A1)
                    LD DE,(B1)
                    ADD HL,DE
                    RET
            """, false),
        new CatalogSource("CALCUL", "DEUX.cmd.asm", ";@NOM DEUX\n;@GENRE FONCTION\n  LD HL,2\n  RET", false),
        new CatalogSource("CALCUL", "CASSE.cmd.asm", ";@NOM CASSE\n;@GENRE COMMANDE\n  LD A,\n  CALL NULLEPART\n  RET", false)
    ]);

    static readonly CompilerOptions Options = new(Libraries: Catalog);

    static AnalysisResult Analyze(string source, CompilerOptions? options = null) =>
        BasicAnalyzer.Analyze(source, options ?? Options);

    static (string Output, GeneratedProgram Program) Run(string source)
    {
        AnalysisResult analysis = Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin, Options);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, Origin);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(Origin);
        return (cpu.Output.ToString(), program);
    }

    static Diagnostic Error(string source, CompilerOptions? options = null) =>
        Assert.Single(Analyze(source, options).Diagnostics);

    // ---------- dans un programme ----------

    [Fact]
    public void Une_commande_de_bibliotheque_est_appelee()
    {
        Assert.Equal("AB", Run("10 LETTRE 65:X=66:LETTRE X").Output);
    }

    [Fact]
    public void Une_fonction_et_sa_routine_sont_appelees()
    {
        var (output, program) = Run("10 PRINT DOUBLE(21);DEUX;SOMME(5,DOUBLE(3))+1");

        Assert.Equal(" 42  2  12 \r\n", output);
        Assert.Contains("CALCUL:DOUBLE", program.Modules);
        Assert.Contains("CALCUL:FOIS2", program.Modules);       // appelée par DOUBLE : ajoutée aussi
        Assert.Contains("CALCUL:DEUX", program.Modules);
    }

    [Fact]
    public void Les_cases_ne_se_melangent_pas()
    {
        Assert.Equal(" 10 \r\n", Run("10 PRINT SOMME(1,SOMME(4,5))").Output);
    }

    [Fact]
    public void Seules_les_commandes_utilisees_sont_ajoutees()
    {
        var (_, program) = Run("10 LETTRE 65");

        Assert.Contains("TEXTE:LETTRE", program.Modules);
        Assert.DoesNotContain(program.Modules, m => m.StartsWith("CALCUL:"));
    }

    [Fact]
    public void Une_variable_peut_porter_le_nom_d_une_commande()
    {
        Assert.Equal(" 5 \r\n", Run("10 LETTRE=5:PRINT LETTRE").Output);
    }

    [Fact]
    public void Sans_le_catalogue_le_mot_reste_inconnu()
    {
        Assert.Equal(DiagnosticCode.UnknownCommand, Error("10 LETTRE 65", CompilerOptions.Default).Code);
    }

    // ---------- erreurs ----------

    [Theory]
    [InlineData("10 LETTRE", "LETTRE code")]
    [InlineData("10 LETTRE 65,66", "LETTRE code")]
    [InlineData("10 X=SOMME(1)", "SOMME(a1, b1)")]
    public void Le_nombre_de_valeurs_est_verifie(string source, string signature)
    {
        Diagnostic error = Error(source);

        Assert.Equal(DiagnosticCode.WrongArgumentCount, error.Code);
        Assert.Contains(signature, error.Message);
    }

    [Fact]
    public void Une_fonction_avec_valeurs_demande_des_parentheses()
    {
        Assert.Equal(DiagnosticCode.ExpectedOpenParenthesis, Error("10 X=DOUBLE").Code);
    }

    [Fact]
    public void Un_nombre_hors_des_limites_est_refuse()
    {
        Diagnostic error = Error("10 LETTRE 7");

        Assert.Equal(DiagnosticCode.ValueOutOfRange, error.Code);
        Assert.Contains("LETTRE, valeur code", error.Message);
        Assert.Contains("32 à 255", error.Message);
    }

    [Fact]
    public void Un_tableau_ne_peut_pas_porter_le_nom_d_une_fonction()
    {
        Diagnostic error = Error("10 DIM DOUBLE(5)");

        Assert.Equal(DiagnosticCode.LibraryArrayName, error.Code);
        Assert.Contains("bibliothèque CALCUL", error.Message);
    }

    [Fact]
    public void Une_faute_du_code_assembleur_est_signalee_a_la_ligne_d_appel()
    {
        var errors = Analyze("10 CLS\n20 CASSE").Diagnostics;

        Assert.Equal(2, errors.Count);                          // LD A, incomplet et NULLEPART inconnu
        Assert.All(errors, e => Assert.Equal(DiagnosticCode.LibraryCode, e.Code));
        Assert.All(errors, e => Assert.Equal(2, e.Line));          // ligne de l'éditeur
        Assert.Contains("CASSE (bibliothèque CALCUL), ligne 4", errors[1].Message);
        Assert.Contains("NULLEPART", errors[1].Message);
    }

    [Fact]
    public void Une_commande_fautive_non_utilisee_ne_gene_pas()
    {
        Assert.Empty(Analyze("10 LETTRE 65").Diagnostics);
    }
}
