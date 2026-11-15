// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot F3b (D070) : VAL et STR$ avec les nombres à virgule. Valeurs attendues relevées sur le vrai BASIC
/// (capture SugarBox de l'utilisateur, 15 nov. 2026).
/// </summary>
public class RealStrValTests
{
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

    [Theory]
    [InlineData("VAL(\"3.14\");VAL(\"  -2.5E3\");VAL(\".5\");VAL(\"1E2\")", " 3.14 -2500  0.5  100 ")]
    [InlineData("VAL(\"12ABC\");VAL(\"ABC\");VAL(\"\");VAL(\"1.2.3\")", " 12  0  0  1.2 ")]
    [InlineData("VAL(\"&FF\");VAL(\"&X101\");VAL(\"1E\");VAL(\"- 5\")", " 255  5  1 -5 ")]
    [InlineData("VAL(\"40000\")", " 40000 ")]
    public void VAL_lit_comme_le_Locomotive(string values, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 PRINT " + values));
    }

    [Fact]
    public void VAL_trop_grand_donne_Overflow_a_la_suite_de_la_ligne()
    {
        // Le vrai BASIC écrit « Overflow » là où est le curseur : « 40000 Overflow »
        Assert.Equal(" 40000 Overflow\r\n 1.70141E+38 \r\n", Run("10 PRINT VAL(\"40000\");VAL(\"1E39\")"));
    }

    [Fact]
    public void VAL_dans_un_entier_arrondit()
    {
        Assert.Equal(" 4 \r\n", Run("10 A% = VAL(\"3.7\")\n20 PRINT A%"));
    }

    [Fact]
    public void STR_de_nombres_a_virgule()
    {
        Assert.Equal("[ 3.14][-0.5][ 1E+20]\r\n[ 7][ 0.333333333]\r\n",
                     Run("10 PRINT \"[\";STR$(3.14);\"]\";\"[\";STR$(-0.5);\"]\";\"[\";STR$(1E20);\"]\"\n" +
                         "20 PRINT \"[\";STR$(7);\"]\";\"[\";STR$(1/3.0);\"]\""));
    }

    [Fact]
    public void STR_se_colle_a_un_texte_et_PRINT_revient_a_l_ecran()
    {
        Assert.Equal(" 2.5! 5 \r\n 1.5 2.5\r\n 0.25 \r\n",
                     Run("10 A$ = STR$(2.5) + \"!\" : PRINT A$;LEN(A$)\n20 B$ = STR$(1.5) + STR$(2.5) : PRINT B$\n30 PRINT 0.25"));
    }

    [Fact]
    public void STR_de_zero()
    {
        Assert.Equal("[ 0]\r\n", Run("10 PRINT \"[\";STR$(0.0);\"]\""));
    }
}
