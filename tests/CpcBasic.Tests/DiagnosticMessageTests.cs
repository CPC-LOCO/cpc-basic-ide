// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class DiagnosticMessageTests
{
    [Fact]
    public void Le_message_commence_par_ERREUR_en_ligne()
    {
        var diagnostic = new Diagnostic(DiagnosticCode.ExpectedExpression, 5, "");

        Assert.StartsWith("ERREUR en ligne 5 : ", diagnostic.Message);
    }

    [Fact]
    public void Ligne_incomplete_ne_montre_ni_guillemets_vides_ni_trouve()
    {
        var diagnostic = new Diagnostic(DiagnosticCode.ExpectedExpression, 5, "");

        Assert.Contains("La ligne est incomplète.", diagnostic.Message);
        Assert.DoesNotContain("«  »", diagnostic.Message);
        Assert.DoesNotContain("trouvé", diagnostic.Message);
    }

    [Fact]
    public void Element_qui_ne_convient_pas_est_cite()
    {
        var diagnostic = new Diagnostic(DiagnosticCode.ExpectedExpression, 5, ")", "PRINT");

        Assert.Contains("« ) » ne convient pas ici.", diagnostic.Message);
    }

    [Fact]
    public void Le_message_nomme_la_commande_concernee()
    {
        var diagnostic = new Diagnostic(DiagnosticCode.ExpectedExpression, 5, "", "PRINT");

        Assert.Contains("PRINT attend ici une valeur", diagnostic.Message);
    }

    [Fact]
    public void Print_parenthese_ouvrante_seule_nomme_PRINT_et_dit_ligne_incomplete()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT (");

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ExpectedExpression, error.Code);
        Assert.Equal("PRINT", error.Context);
        Assert.Contains("PRINT attend ici une valeur", error.Message);
        Assert.Contains("La ligne est incomplète.", error.Message);
    }

    [Fact]
    public void For_sans_valeur_de_depart_nomme_FOR()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 FOR I =\n20 NEXT");

        Diagnostic error = result.Diagnostics[0];
        Assert.Equal("FOR", error.Context);
        Assert.Contains("FOR attend ici une valeur", error.Message);
    }

    [Fact]
    public void Parenthese_non_fermee_nomme_la_commande()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT (1 + 2");

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ExpectedClosingParenthesis, error.Code);
        Assert.Contains("Dans PRINT, il manque une parenthèse fermante", error.Message);
    }

    [Fact]
    public void Texte_non_ferme_explique_le_guillemet()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"BONJOUR");

        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.UnterminatedString, error.Code);
        Assert.Contains("guillemet", error.Message);
        Assert.Contains("jamais refermé", error.Message);
    }

    [Fact]
    public void Tous_les_codes_a_element_attendu_gerent_la_fin_de_ligne()
    {
        DiagnosticCode[] codes =
        {
            DiagnosticCode.ExpectedExpression, DiagnosticCode.ExpectedEquals,
            DiagnosticCode.ExpectedVariableName, DiagnosticCode.ExpectedTo,
            DiagnosticCode.ExpectedNumber, DiagnosticCode.ExpectedThen,
            DiagnosticCode.ExpectedComparison
        };

        foreach (DiagnosticCode code in codes)
        {
            string message = new Diagnostic(code, 1, "").Message;
            Assert.DoesNotContain("«  »", message);
            Assert.Contains("La ligne est incomplète.", message);
        }
    }
}
