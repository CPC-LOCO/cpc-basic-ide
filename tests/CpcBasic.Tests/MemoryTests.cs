// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>MEMORY : le programme compilé doit tenir sous la limite demandée (lot C, D057).</summary>
public class MemoryTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void MEMORY_avec_un_nombre_est_retenu_comme_limite()
    {
        GeneratedProgram program = Generate("10 MEMORY 41983");

        Assert.Equal(41983, program.MemoryLimit);
        Assert.Equal(1, program.MemoryLine);
    }

    [Fact]
    public void MEMORY_accepte_une_adresse_au_dessus_de_32767()
    {
        Assert.Equal(40000, Generate("10 MEMORY 40000").MemoryLimit);
    }

    [Fact]
    public void Avec_plusieurs_MEMORY_la_limite_la_plus_basse_compte()
    {
        GeneratedProgram program = Generate("10 MEMORY 50000\n20 MEMORY 45000\n30 MEMORY 48000");

        Assert.Equal(45000, program.MemoryLimit);
        Assert.Equal(2, program.MemoryLine);
    }

    [Fact]
    public void MEMORY_avec_un_calcul_n_est_pas_verifie_a_la_compilation()
    {
        // p = &A400 : MEMORY p - 1 : la valeur n'est connue qu'à l'exécution
        Assert.Equal(-1, Generate("10 P = -23552\n20 MEMORY P - 1").MemoryLimit);
    }

    [Fact]
    public void Sans_MEMORY_il_n_y_a_pas_de_limite()
    {
        Assert.Equal(-1, Generate("10 PRINT \"A\"").MemoryLimit);
    }

    [Fact]
    public void MEMORY_n_ajoute_aucun_octet_au_programme()
    {
        Assert.Equal(Generate("10 REM rien").Code, Generate("10 MEMORY 41983").Code);
    }

    [Fact]
    public void Un_programme_qui_tient_sous_la_limite_se_compile()
    {
        CompileResult result = Compiler.CompileToDisk("10 MEMORY 41983\n20 PRINT \"A\"", "TEST");

        Assert.True(result.Success);
    }

    [Fact]
    public void Un_programme_qui_depasse_la_limite_est_refuse_avec_un_message_clair()
    {
        // &4010 : le programme commence en &4000 et fait plus de 17 octets
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"A\"\n20 MEMORY 16400", "TEST");

        Assert.False(result.Success);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ProgramTooLarge, error.Code);
        Assert.Equal(2, error.Line);
        Assert.Contains("&4010", error.Message);
        Assert.StartsWith("ERREUR en ligne 2 : MEMORY : le programme compilé occupe de &4000 à &", error.Message);
    }

    [Theory]
    [InlineData("10 MEMORY")]
    [InlineData("10 MEMORY 1, 2")]
    public void MEMORY_veut_une_seule_valeur(string source)
    {
        Assert.Contains(BasicAnalyzer.Analyze(source).Diagnostics, e => e.Code == DiagnosticCode.WrongArgumentCount);
    }
}
