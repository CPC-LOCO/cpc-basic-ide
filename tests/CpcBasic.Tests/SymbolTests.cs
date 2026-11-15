// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Lot H4 (D075) : SYMBOL, SYMBOL AFTER, TAG, TAGOFF. Référence : examples\symboles.bas dans SugarBox.</summary>
public class SymbolTests
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

    // ---------- SYMBOL ----------

    [Fact]
    public void SYMBOL_donne_les_8_lignes_au_firmware()
    {
        MiniZ80 cpu = Run("10 SYMBOL 240, &18, &3C, &7E, &FF, &FF, &7E, &3C, &18");

        (byte character, byte[] rows) = Assert.Single(cpu.Matrices);
        Assert.Equal(240, character);
        Assert.Equal(new byte[] { 0x18, 0x3C, 0x7E, 0xFF, 0xFF, 0x7E, 0x3C, 0x18 }, rows);
    }

    [Fact]
    public void SYMBOL_lignes_absentes_a_zero_et_valeurs_calculees()
    {
        MiniZ80 cpu = Run("10 C = 241 : L = 15\n20 SYMBOL C, L * 2, 255");

        (byte character, byte[] rows) = Assert.Single(cpu.Matrices);
        Assert.Equal(241, character);
        Assert.Equal(new byte[] { 30, 255, 0, 0, 0, 0, 0, 0 }, rows);
    }

    [Fact]
    public void SYMBOL_d_un_caractere_non_redessinable_Improper_argument()
    {
        MiniZ80 cpu = Run("10 SYMBOL 65, 255");

        Assert.Contains("Improper argument in 10", cpu.Output.ToString());
        Assert.Empty(cpu.Matrices);
    }

    [Theory]
    [InlineData("10 SYMBOL 240")]
    [InlineData("10 SYMBOL 240, 1, 2, 3, 4, 5, 6, 7, 8, 9")]
    public void SYMBOL_avec_un_mauvais_nombre_de_valeurs(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Theory]
    [InlineData("10 SYMBOL 256, 1")]
    [InlineData("10 SYMBOL 240, 300")]
    [InlineData("10 SYMBOL AFTER 257")]
    public void Valeurs_hors_limites(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    // ---------- SYMBOL AFTER ----------

    [Fact]
    public void SYMBOL_AFTER_copie_les_dessins_puis_donne_la_table()
    {
        MiniZ80 cpu = Run("10 SYMBOL AFTER 200\n20 SYMBOL 65, 1");

        Assert.Equal(56, cpu.MatrixReads);                   // caractères 200 à 255
        (int first, ushort table) = Assert.Single(cpu.MatrixTables);
        Assert.Equal(200, first);
        Assert.True(table > 0x4000);
        Assert.Contains("Improper argument in 20", cpu.Output.ToString());   // 65 reste non redessinable
    }

    [Fact]
    public void SYMBOL_AFTER_puis_SYMBOL_d_un_caractere_devenu_redessinable()
    {
        MiniZ80 cpu = Run("10 SYMBOL AFTER 32\n20 SYMBOL 65, 255");

        Assert.Equal(65, Assert.Single(cpu.Matrices).Char);
    }

    [Fact]
    public void SYMBOL_AFTER_256_supprime_la_table()
    {
        MiniZ80 cpu = Run("10 SYMBOL AFTER 256");

        Assert.Equal(0, cpu.MatrixReads);
        Assert.Equal(256, Assert.Single(cpu.MatrixTables).First);
    }

    [Fact]
    public void SYMBOL_AFTER_calcule_hors_limites_Improper_argument()
    {
        MiniZ80 cpu = Run("10 N = 300\n20 SYMBOL AFTER N");

        Assert.Contains("Improper argument in 20", cpu.Output.ToString());
    }

    [Fact]
    public void La_table_prend_la_place_des_caracteres_demandes()
    {
        int Size(string source) => CodeGenerator.Generate(BasicAnalyzer.Analyze(source).Program, 0x4000).Code.Length;

        Assert.Equal(8 * 16, Size("10 SYMBOL AFTER 240") - Size("10 SYMBOL AFTER 256"));
    }

    // ---------- TAG et TAGOFF ----------

    [Fact]
    public void TAG_et_TAGOFF()
    {
        MiniZ80 cpu = Run("10 TAG\n20 PRINT \"A\";\n30 TAGOFF");

        Assert.Equal(new[] { true, false }, cpu.Tags);
    }

    [Fact]
    public void TAG_dans_une_fenetre()
    {
        MiniZ80 cpu = Run("10 TAG #1\n20 TAGOFF #1");

        Assert.Equal(new[] { true, false }, cpu.Tags);
        Assert.Equal(new byte[] { 1, 0, 1, 0 }, cpu.StreamSelects);
    }

    [Theory]
    [InlineData("10 SYMBOL = 5")]
    [InlineData("10 TAG = 5")]
    [InlineData("10 TAGOFF = 5")]
    public void Les_nouveaux_mots_ne_sont_pas_des_variables(string source)
    {
        Assert.NotEmpty(Errors(source));
    }
}
