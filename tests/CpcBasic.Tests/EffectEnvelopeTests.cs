// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D218 (remplace D216) : les effets sonores prennent leurs numéros d'enveloppes dans une réserve commune : tous ceux que le
/// programme n'écrit pas lui-même ; chaque effet prend les suivants, chacun son tour.
/// </summary>
public class EffectEnvelopeTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static MiniZ80 Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    [Fact]
    public void Les_numeros_du_programme_ne_sont_jamais_pris()
    {
        // le programme écrit ENV 15 et ENT -14 : les effets prennent 14, puis 13 (ENV) et 15, 13 (ENT)
        MiniZ80 cpu = Run("10 ENV 15,1,1,1:ENT -14,1,1,1:VROOM 1:VROOM 1");

        Assert.Equal(new byte[] { 14, 15 }, cpu.Sounds[0][1..3]);
        Assert.Equal(new byte[] { 13, 13 }, cpu.Sounds[1][1..3]);
    }

    [Fact]
    public void La_reserve_fait_le_tour_puis_recommence()
    {
        MiniZ80 cpu = Run("10 FOR I=1 TO 16:FALL 2:NEXT");

        var numbers = cpu.Sounds.Where(s => s[1] != 0).Select(s => (int)s[1]).ToList();   // la dernière note de chaque FALL 2
        Assert.Equal(Enumerable.Range(1, 15).Reverse().Append(15), numbers);
    }

    [Fact]
    public void Un_numero_ecrit_par_le_programme_est_accepte_avec_les_effets()
    {
        Assert.DoesNotContain(Errors("10 VROOM 1\n20 ENV 13,1,1,1"), d => d.Code == DiagnosticCode.EnvelopeTaken);
    }

    [Fact]
    public void Trop_de_numeros_ecrits_par_le_programme_est_refuse()
    {
        string envs = string.Join(":", Enumerable.Range(1, 13).Select(n => $"ENV {n},1,1,1"));
        Diagnostic error = Assert.Single(Errors($"10 {envs}\n20 VROOM 3"));

        Assert.Equal(DiagnosticCode.EnvelopeTaken, error.Code);
        Assert.Contains("ce programme écrit lui-même 13 numéros de ENV différents ; il n'en reste que 2", error.Message);
        Assert.Contains("VROOM en demande 3 à la fois", error.Message);
    }
}
