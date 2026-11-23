// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D216 : chaque effet sonore (VROOM, BOOM, SHOT) a ses numéros d'enveloppes ; un programme ne perd que ceux des effets
/// qu'il utilise, et ENV ou ENT avec un numéro pris est refusé avec les numéros libres.
/// </summary>
public class EffectEnvelopeTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    [Theory]
    [InlineData("10 ENV 13,1,1,1:SOUND 1,100")]                 // pas d'effet : tous les numéros sont libres
    [InlineData("10 ENV 13,1,1,1:BOOM 1")]                      // BOOM ne prend pas ENV 13
    [InlineData("10 ENT -5,1,1,1:VROOM 1")]
    [InlineData("10 N=13:ENV N,1,1,1:VROOM 1")]                 // numéro calculé : pas vérifié
    public void Un_numero_libre_est_accepte(string source)
    {
        Assert.DoesNotContain(Errors(source), d => d.Code == DiagnosticCode.EnvelopeTaken);
    }

    [Fact]
    public void Un_numero_pris_est_refuse_avec_les_numeros_libres()
    {
        Diagnostic error = Assert.Single(Errors("10 VROOM 1\n20 ENV 13,1,1,1"));

        Assert.Equal(DiagnosticCode.EnvelopeTaken, error.Code);
        Assert.Equal(2, error.Line);                           // la 2e ligne de l'éditeur (comme toutes les erreurs)
        Assert.Contains("ENV 13 : ce numéro est déjà pris par VROOM, qui se sert de ENV 12 à 15", error.Message);
        Assert.Contains("Choisissez un autre numéro : 1 à 11.", error.Message);
    }

    [Fact]
    public void Les_numeros_libres_tiennent_compte_de_tous_les_effets_du_programme()
    {
        Diagnostic error = Assert.Single(Errors("10 VROOM 1:BOOM 1:SHOT 1\n20 ENT -9,1,1,1"));

        Assert.Contains("ENT 9 : ce numéro est déjà pris par BOOM", error.Message);
        Assert.Contains("Choisissez un autre numéro : 1 à 5.", error.Message);
    }
}
