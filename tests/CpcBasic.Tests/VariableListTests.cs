// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Editing;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Liste des variables, touche F4 de l'éditeur (D131, solution A de l'utilisateur).</summary>
public class VariableListTests
{
    // Solution A seule ; les adresses (solution B, D132) sont essayées à la fin
    static string[] Texts(string source) => VariableList.Build(source, withAddresses: false).Select(v => v.Text).ToArray();

    [Fact]
    public void Une_variable_qui_recoit_une_valeur_puis_est_utilisee()
    {
        Assert.Equal(new[] { "A : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20." }, Texts("10 A=5\n20 PRINT A"));
    }

    [Fact]
    public void Une_faute_de_frappe_est_signalee_avec_le_nom_proche()
    {
        var list = VariableList.Build("10 SCORE=0\n20 SCORE=SCORE+1\n30 PRINT SCOER", withAddresses: false);

        Assert.Equal(new[]
        {
            "⚠ SCOER : entier. Utilisée ligne 30, mais ne reçoit jamais de valeur (elle vaut toujours 0). Faute de frappe pour SCORE ?",
            "SCORE : entier. Reçoit une valeur lignes 10, 20 ; utilisée ligne 20."
        }, list.Select(v => v.Text).ToArray());
        Assert.True(list[0].Warning);
        Assert.Equal(3, list[0].Line);                 // Entrée : le curseur va à la ligne 3 de l'éditeur
        Assert.False(list[1].Warning);
        Assert.Equal(1, list[1].Line);
    }

    [Fact]
    public void Une_variable_jamais_utilisee_est_signalee()
    {
        Assert.Equal(new[] { "⚠ VIE : entier. Reçoit une valeur ligne 10, mais n'est jamais utilisée." }, Texts("10 VIE=3"));
    }

    [Fact]
    public void Un_nom_court_n_a_pas_de_nom_propose()
    {
        Assert.Equal(new[]
        {
            "⚠ A : entier. Reçoit une valeur ligne 10, mais n'est jamais utilisée.",
            "⚠ B : entier. Utilisée ligne 20, mais ne reçoit jamais de valeur (elle vaut toujours 0)."
        }, Texts("10 A=1\n20 PRINT B"));
    }

    [Fact]
    public void Un_texte_jamais_rempli_vaut_un_texte_vide()
    {
        Assert.Equal(new[] { "⚠ NOM$ : texte. Utilisée ligne 10, mais ne reçoit jamais de valeur (elle vaut toujours un texte vide)." },
                     Texts("10 PRINT NOM$"));
    }

    [Fact]
    public void Types_tableaux_et_boucles()
    {
        Assert.Equal(new[]
        {
            "I : entier. Reçoit une valeur ligne 20 ; utilisée ligne 20.",
            "N$ : texte. Reçoit une valeur ligne 10 ; utilisée ligne 30.",
            "T() : tableau d'entiers (10, 5). Reçoit une valeur lignes 10, 20 ; utilisée ligne 30.",
            "X! : nombre à virgule. Reçoit une valeur ligne 10 ; utilisée ligne 30."
        }, Texts("10 DIM T(10,5):N$=\"A\":X!=1.5\n20 FOR I=1 TO 10:T(I,1)=I:NEXT\n30 PRINT T(2,1);N$;X!"));
    }

    [Fact]
    public void Une_variable_et_un_tableau_du_meme_nom_sont_deux_variables()
    {
        Assert.Equal(new[]
        {
            "A : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20.",
            "A() : tableau d'entiers (3). Reçoit une valeur ligne 10 ; utilisée ligne 20."
        }, Texts("10 DIM A(3):A(1)=2:A=4\n20 PRINT A(1);A"));
    }

    [Fact]
    public void INPUT_READ_et_LINE_INPUT_donnent_une_valeur()
    {
        Assert.Equal(new[]
        {
            "A$ : texte. Reçoit une valeur ligne 10 ; utilisée ligne 40.",
            "B : entier. Reçoit une valeur ligne 20 ; utilisée ligne 40.",
            "C$ : texte. Reçoit une valeur ligne 30 ; utilisée ligne 40."
        }, Texts("10 INPUT A$\n20 READ B\n30 LINE INPUT C$\n40 PRINT A$;B;C$\n50 DATA 5"));
    }

    [Fact]
    public void BYTE_donne_un_octet()
    {
        Assert.Equal(new[] { "B : octet (0 à 255). Reçoit une valeur ligne 20 ; utilisée ligne 20." },
                     Texts("10 BYTE B\n20 B=5:PRINT B"));
    }

    [Fact]
    public void Les_valeurs_d_une_fonction_DEF_FN_ne_sont_pas_des_variables_du_programme()
    {
        Assert.Equal(new[] { "K : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20." },
                     Texts("10 K=3:DEF FNDOUBLE(X)=X*2\n20 PRINT FNDOUBLE(K)"));
    }

    [Fact]
    public void Sans_numeros_ce_sont_les_lignes_de_l_editeur()
    {
        Assert.Equal(new[] { "A : entier. Reçoit une valeur ligne 1 ; utilisée ligne 2." }, Texts("A=1\nPRINT A"));
    }

    [Fact]
    public void Un_programme_sans_variable_donne_une_liste_vide()
    {
        Assert.Empty(VariableList.Build("10 PRINT \"BONJOUR\"\n20 END"));
    }

    // ---------- Solution B (D132) : adresses dans la mémoire du CPC ----------

    [Fact]
    public void L_adresse_donnee_est_celle_ou_le_programme_range_la_valeur()
    {
        const string source = "10 A=1234:B$=\"XYZ\":X!=1.5\n20 PRINT A;B$";
        var lines = VariableList.Build(source);
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze(source).Program, 0x4000);
        int a = program.Addresses!["A"];

        Assert.Equal($"&{a:X4}  A : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20. (2 octets, poids faible en premier)",
                     lines[0].Text);
        Assert.Equal($"&{a:X4}", lines[0].Address);
        Assert.Equal($"&{program.Addresses["B$"]:X4}  B$ : texte. Reçoit une valeur ligne 10 ; utilisée ligne 20. (fiche de 3 octets : longueur, puis adresse des lettres)",
                     lines[1].Text);
        Assert.Equal($"⚠ &{program.Addresses["X!"]:X4}  X! : nombre à virgule. Reçoit une valeur ligne 10, mais n'est jamais utilisée. (5 octets, nombre à virgule du Locomotive)",
                     lines[2].Text);

        // Le programme lancé range bien 1234 à cette adresse, et la fiche de B$ commence par la longueur 3
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        Assert.Equal(1234, cpu.Memory[a] | (cpu.Memory[a + 1] << 8));
        Assert.Equal(3, cpu.Memory[program.Addresses["B$"]]);
    }

    [Fact]
    public void Un_tableau_et_un_octet_ont_leur_adresse()
    {
        const string source = "10 BYTE C\n20 DIM T(5):T(1)=C:C=2:PRINT T(1);C";
        var lines = VariableList.Build(source);
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze(source).Program, 0x4000);

        Assert.StartsWith($"&{program.Addresses!["C"]:X4}  C : octet (0 à 255).", lines[0].Text);
        Assert.EndsWith("(1 octet)", lines[0].Text);
        Assert.StartsWith($"&{program.Addresses["T()"]:X4}  T() : tableau d'entiers (5).", lines[1].Text);
        Assert.EndsWith("(fiche du tableau : adresse de ses cases, puis leur nombre)", lines[1].Text);
    }

    [Fact]
    public void Avec_une_erreur_la_liste_n_a_pas_d_adresse()
    {
        VariableLine line = VariableList.Build("10 A=5\n20 PRINT A\n30 GOTO 999")[0];
        Assert.Equal("A : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20.", line.Text);
        Assert.Null(line.Address);
    }
}
