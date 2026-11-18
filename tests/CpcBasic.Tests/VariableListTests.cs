// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Editing;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Liste des variables, touche F4 de l'éditeur (D131, solution A de l'utilisateur).</summary>
public class VariableListTests
{
    static string[] Texts(string source) => VariableList.Build(source).Select(v => v.Text).ToArray();

    [Fact]
    public void Une_variable_qui_recoit_une_valeur_puis_est_utilisee()
    {
        Assert.Equal(new[] { "A : entier. Reçoit une valeur ligne 10 ; utilisée ligne 20." }, Texts("10 A=5\n20 PRINT A"));
    }

    [Fact]
    public void Une_faute_de_frappe_est_signalee_avec_le_nom_proche()
    {
        var list = VariableList.Build("10 SCORE=0\n20 SCORE=SCORE+1\n30 PRINT SCOER");

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
}
