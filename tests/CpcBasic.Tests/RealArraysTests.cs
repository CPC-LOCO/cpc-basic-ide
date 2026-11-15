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

/// <summary>Lot F3a (D070) : tableaux de nombres à virgule, nombres à virgule dans DATA, READ A!.</summary>
public class RealArraysTests
{
    static string Run(string source, bool realByDefault = false, string typed = "")
    {
        var options = new CompilerOptions(RealByDefault: realByDefault);
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, options);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Tableaux ----------

    [Fact]
    public void Un_tableau_de_nombres_a_virgule_garde_ses_valeurs()
    {
        Assert.Equal(" 1.5 -2.25  0 \r\n", Run("10 DIM T!(3)\n20 T!(0) = 1.5 : T!(3) = -2.25\n30 PRINT T!(0);T!(3);T!(1)"));
    }

    [Fact]
    public void Sans_DIM_le_tableau_va_de_0_a_10()
    {
        Assert.Equal(" 2.5 \r\nSubscript out of range in 30\r\n", Run("10 T!(10) = 2.5\n20 PRINT T!(10)\n30 T!(11) = 1"));
    }

    [Fact]
    public void Tableau_a_deux_dimensions()
    {
        Assert.Equal(" 3.5  0  7 \r\n", Run("10 DIM M!(2, 2)\n20 M!(1, 2) = 3.5 : M!(2, 2) = 7\n30 PRINT M!(1, 2);M!(2, 1);M!(2, 2)"));
    }

    [Fact]
    public void Tableaux_d_entiers_de_textes_et_de_nombres_a_virgule_ensemble()
    {
        Assert.Equal(" 7 X 0.5 \r\n", Run("10 DIM A(2), B$(2), C!(2)\n20 A(1) = 7 : B$(1) = \"X\" : C!(1) = 0.5\n30 PRINT A(1);B$(1);C!(1)"));
    }

    [Fact]
    public void Une_case_sert_dans_un_calcul_et_recoit_un_calcul()
    {
        Assert.Equal(" 4 \r\n 2 \r\n", Run("10 DIM T!(2)\n20 T!(1) = 1.5 : T!(2) = T!(1) + 2.5\n30 PRINT T!(2)\n40 A% = T!(1)\n50 PRINT A%"));
    }

    [Fact]
    public void Avec_le_reglage_F8_DIM_T_est_un_tableau_a_virgule()
    {
        Assert.Equal(" 3.5 \r\n", Run("10 DIM T(3)\n20 T(1) = 7/2\n30 PRINT T(1)", realByDefault: true));
    }

    [Fact]
    public void INPUT_dans_une_case()
    {
        Assert.Equal("? 1.5\r\n 1.5 \r\n", Run("10 INPUT T!(2)\n20 PRINT T!(2)", typed: "1.5\r"));
    }

    [Fact]
    public void Arobase_d_une_case_les_cases_font_5_octets()
    {
        Assert.Equal(" 5 \r\n", Run("10 DIM T!(3)\n20 PRINT @T!(1) - @T!(0)"));
    }

    // ---------- DATA et READ ----------

    [Fact]
    public void READ_lit_des_nombres_a_virgule()
    {
        Assert.Equal(" 3.14  2 -1000 \r\n", Run("10 DATA 3.14, 2, -1E3\n20 READ A!, B!, C!\n30 PRINT A!;B!;C!"));
    }

    [Fact]
    public void READ_d_un_entier_arrondit_un_nombre_a_virgule()
    {
        Assert.Equal(" 3  4 \r\n", Run("10 DATA 2.5, 3.5\n20 READ A%, B\n30 PRINT A%;B"));
    }

    [Fact]
    public void READ_d_un_entier_trop_grand_arrete_le_programme()
    {
        Assert.Equal("Overflow in 20\r\n", Run("10 DATA 70000\n20 READ A\n30 PRINT \"NON\""));
    }

    [Fact]
    public void READ_d_un_texte_rend_l_ecriture_du_nombre()
    {
        Assert.Equal("3.14 7\r\n", Run("10 DATA 3.14, 7\n20 READ A$, B$\n30 PRINT A$;\" \";B$"));
    }

    [Fact]
    public void READ_dans_des_cases_avec_RESTORE()
    {
        Assert.Equal(" 4 \r\n 1.5 \r\n",
                     Run("10 DATA 1.5, 2.5\n20 FOR I = 0 TO 1 : READ T!(I) : NEXT\n30 PRINT T!(0) + T!(1)\n40 RESTORE : READ X!\n50 PRINT X!"));
    }

    [Fact]
    public void READ_A_point_d_exclamation_sur_un_texte_donne_Type_mismatch()
    {
        Assert.Equal("Type mismatch in 20\r\n", Run("10 DATA BONJOUR\n20 READ A!"));
    }
}
