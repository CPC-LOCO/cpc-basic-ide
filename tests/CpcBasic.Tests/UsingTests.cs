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

/// <summary>
/// Lot H3 (D074) : PRINT USING. Les écrans attendus viennent du même programme tapé dans le vrai BASIC
/// (SugarBox, 15 nov. 2026) : voir examples\format.bas.
/// </summary>
public class UsingTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Écrans du vrai BASIC ----------

    [Theory]
    [InlineData("\"####\";12;-5;123456", "  12  -5%123456")]
    [InlineData("\"##.##\";3.14159;-2.5;0.5", " 3.14-2.50 0.50")]
    [InlineData("\"+##.#\";7;-7", " +7.0 -7.0")]
    [InlineData("\"##.#-\";7;-7", " 7.0  7.0-")]
    [InlineData("\"**###\";42", "***42")]
    [InlineData("\"$$###.##\";9.5", "   $9.50")]
    [InlineData("\"#,######\";1234567", "%1,234,567")]
    [InlineData("\"#.##^^^^\";12345", " .12E+05")]
    [InlineData("\"!\";\"BONJOUR\"", "B")]
    [InlineData("\"\\   \\\";\"BONJOUR\"", "BONJO")]
    [InlineData("\"&\";\"BONJOUR\"", "BONJOUR")]
    [InlineData("\"PRIX ###.## EUR\";19.9", "PRIX  19.90 EUR")]
    [InlineData("\"###\";1;2;3", "  1  2  3")]
    [InlineData("\"####\";99999", "%99999")]
    public void Comme_le_vrai_BASIC(string rest, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 PRINT USING " + rest));
    }

    // ---------- Autres cas ----------

    [Theory]
    [InlineData("\"##.##\";0", " 0.00")]
    [InlineData("\"##.#\";9.96", "10.0")]
    [InlineData("\"###\";2.5", "  3")]
    [InlineData("\"###.\";12", " 12.")]
    [InlineData("\"\\  \\\";\"AB\"", "AB  ")]
    [InlineData("\"#,###\";1000", "1,000")]
    public void Autres_formats(string rest, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 PRINT USING " + rest));
    }

    [Fact]
    public void Avec_des_variables_et_du_texte_avant()
    {
        Assert.Equal("TOTAL: 12.50\r\n", Run("10 A = 12 : B! = 0.5\n20 PRINT \"TOTAL:\"; USING \"###.##\"; A + B!"));
    }

    [Fact]
    public void Point_virgule_final_garde_la_ligne()
    {
        Assert.Equal("  1  2\r\n", Run("10 PRINT USING \"###\"; 1;\n20 PRINT USING \"###\"; 2"));
    }

    [Fact]
    public void Dans_une_fenetre()
    {
        Assert.Empty(Errors("10 PRINT #1, USING \"##\"; 5"));
    }

    // ---------- Erreurs ----------

    [Fact]
    public void Format_dans_une_variable_pas_encore_possible()
    {
        Diagnostic error = Assert.Single(Errors("10 F$ = \"##\"\n20 PRINT USING F$; 5"));
        Assert.Equal(DiagnosticCode.UsingFormatNotLiteral, error.Code);
    }

    [Fact]
    public void Format_sans_emplacement()
    {
        Diagnostic error = Assert.Single(Errors("10 PRINT USING \"ABC\"; 5"));
        Assert.Equal(DiagnosticCode.UsingWithoutField, error.Code);
    }

    [Theory]
    [InlineData("10 PRINT USING \"##\"; \"A\"")]
    [InlineData("10 PRINT USING \"&\"; 5")]
    public void Texte_et_nombre_melanges(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    [Fact]
    public void USING_sans_valeur()
    {
        Assert.Contains(Errors("10 PRINT USING \"##\";"), e => e.Code == DiagnosticCode.ExpectedExpression);
    }
}
