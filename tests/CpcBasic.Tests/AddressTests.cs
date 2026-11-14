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

/// <summary>@A et @T(3) : adresse d'une variable ou d'une case de tableau (D061).</summary>
public class AddressTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source)
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Fact]
    public void Arobase_devant_une_variable_donne_son_adresse()
    {
        var assign = Assert.IsType<AssignStatement>(Parse("10 A = @B").Statements[0]);

        var address = Assert.IsType<AddressOfExpression>(assign.Value);
        Assert.Equal(new VariableExpression("B"), address.Target);
    }

    [Fact]
    public void Arobase_devant_une_case_de_tableau()
    {
        var assign = Assert.IsType<AssignStatement>(Parse("10 P = @T(3)").Statements[0]);

        var address = Assert.IsType<AddressOfExpression>(assign.Value);
        Assert.IsType<ArrayElementExpression>(address.Target);
    }

    [Theory]
    [InlineData("10 A = @")]
    [InlineData("10 A = @5")]
    [InlineData("10 A = @(B)")]
    public void Arobase_sans_variable_est_signale(string source)
    {
        Diagnostic error = Assert.Single(Errors(source), e => e.Code == DiagnosticCode.ExpectedTarget);

        Assert.StartsWith("ERREUR en ligne 1 : @ attend ici", error.Message);
    }

    [Fact]
    public void Arobase_devant_une_variable_texte_n_est_pas_encore_possible()
    {
        Assert.Contains(Errors("10 A = @B$"), e => e.Code == DiagnosticCode.UnsupportedVariableType);
    }

    // ---------- Exécution ----------

    [Fact]
    public void PEEK_a_l_adresse_relit_les_deux_octets_de_la_variable()
    {
        Assert.Equal(" 210  4  1234 \r\n",
                     Run("10 A = 1234\n20 P = @A\n30 PRINT PEEK(P); PEEK(P + 1); PEEK(P) + 256 * PEEK(P + 1)"));
    }

    [Fact]
    public void POKE_a_l_adresse_change_la_variable()
    {
        Assert.Equal(" 256 \r\n", Run("10 A = 5\n20 POKE @A, 0 : POKE @A + 1, 1\n30 PRINT A"));
    }

    [Fact]
    public void Deux_variables_ont_deux_adresses_differentes_et_fixes()
    {
        Assert.Equal("-1 -1 \r\n", Run("10 PRINT @A <> @B; @A = @A"));
    }

    [Fact]
    public void Les_cases_d_un_tableau_se_suivent_de_2_octets()
    {
        Assert.Equal(" 500  2 \r\n",
                     Run("10 DIM T(5) : T(3) = 500\n20 P = @T(3)\n30 PRINT PEEK(P) + 256 * PEEK(P + 1); @T(4) - @T(3)"));
    }

    [Fact]
    public void Dans_un_tableau_a_deux_dimensions_la_derniere_change_le_plus_vite()
    {
        Assert.Equal(" 2  8 \r\n", Run("10 DIM T(2, 3)\n20 PRINT @T(0, 1) - @T(0, 0); @T(1, 0) - @T(0, 0)"));
    }

    [Fact]
    public void Une_variable_BYTE_tient_sur_un_octet()
    {
        Assert.Equal(" 200 \r\n", Run("10 BYTE B\n20 B = 200\n30 PRINT PEEK(@B)"));
    }

    [Fact]
    public void Arobase_sur_une_case_absente_donne_l_erreur_du_tableau()
    {
        Assert.Equal("Subscript out of range in 20\r\n", Run("10 DIM T(2)\n20 P = @T(5)"));
    }
}
