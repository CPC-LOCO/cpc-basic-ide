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

/// <summary>DATA, READ, RESTORE, et INPUT dans une case de tableau (lot C, D059).</summary>
public class DataTests
{
    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    /// <summary>Lance le programme en tapant <paramref name="typed"/> (le caractère \r est la touche Entrée).</summary>
    static string Run(string source, string typed = "")
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse(source), 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Analyse ----------

    [Fact]
    public void DATA_garde_ses_nombres_sur_16_bits()
    {
        var data = Assert.IsType<DataStatement>(Parse("10 DATA 10, -5, 300, 40000, +7").Statements[0]);

        Assert.Equal(new[] { 10, -5, 300, -25536, 7 }, data.Values.ToArray());
    }

    [Theory]
    [InlineData("10 DATA")]
    [InlineData("10 DATA 1, , 2")]
    [InlineData("10 DATA 1,")]
    public void DATA_sans_nombre_est_signale(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedDataValue);
    }

    [Theory]
    [InlineData("10 DATA \"ABC\"")]
    [InlineData("10 DATA 1, BONJOUR")]
    public void Un_texte_dans_DATA_n_est_pas_encore_possible(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.DataTextNotYet);
    }

    [Theory]
    [InlineData("10 DATA 65536")]
    [InlineData("10 DATA -32769")]
    public void Un_nombre_trop_grand_dans_DATA_est_refuse(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.NumberOutOfRange);
    }

    [Fact]
    public void READ_lit_dans_des_variables_et_des_cases_de_tableau()
    {
        var read = Assert.IsType<ReadStatement>(Parse("10 READ A, T(2), B%").Statements[0]);

        Assert.Equal(3, read.Targets.Count);
        Assert.IsType<VariableExpression>(read.Targets[0]);
        Assert.IsType<ArrayElementExpression>(read.Targets[1]);
        Assert.Equal(new VariableExpression("B%"), read.Targets[2]);
    }

    [Theory]
    [InlineData("10 READ")]
    [InlineData("10 READ 5")]
    [InlineData("10 READ A,")]
    public void READ_sans_variable_est_signale(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedTarget);
    }

    [Fact]
    public void READ_dans_une_variable_texte_n_est_pas_encore_possible()
    {
        Assert.Contains(Errors("10 READ A$"), e => e.Code == DiagnosticCode.StringNotYetHere);   // étape E3 (D062)
    }

    [Fact]
    public void READ_garde_le_meme_nombre_de_dimensions_pour_un_tableau()
    {
        Assert.Contains(Errors("10 DIM T(5)\n20 READ T(1, 2)"), e => e.Code == DiagnosticCode.ArrayDimensionMismatch);
    }

    [Fact]
    public void RESTORE_sans_numero_et_avec_numero()
    {
        BasicProgram program = Parse("10 RESTORE\n20 RESTORE 30\n30 DATA 1");

        Assert.Null(Assert.IsType<RestoreStatement>(program.Statements[0]).Line);
        Assert.Equal(30, Assert.IsType<RestoreStatement>(program.Statements[1]).Line);
    }

    [Fact]
    public void RESTORE_vers_une_ligne_absente_est_refuse()
    {
        Diagnostic error = Assert.Single(Errors("10 RESTORE 99"));

        Assert.Equal(DiagnosticCode.LineNotFound, error.Code);
        Assert.Contains("RESTORE", error.Message);
    }

    [Fact]
    public void RESTORE_attend_un_numero_de_ligne()
    {
        Assert.Contains(Errors("10 RESTORE X"), e => e.Code == DiagnosticCode.ExpectedLineNumber);
    }

    [Fact]
    public void INPUT_accepte_une_case_de_tableau()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 INPUT A, T(2)").Statements[0]);

        Assert.Equal(new[] { "A", "T" }, input.Variables.ToArray());
        Assert.NotNull(input.Indexes);
        Assert.Null(input.Indexes![0]);
        Assert.NotNull(input.Indexes[1]);
    }

    // ---------- Exécution ----------

    [Fact]
    public void READ_lit_les_nombres_dans_l_ordre()
    {
        Assert.Equal(" 5 -7  300 \r\n", Run("10 DATA 5, -7, 300\n20 READ A, B, C\n30 PRINT A; B; C"));
    }

    [Fact]
    public void Les_DATA_peuvent_etre_n_importe_ou_et_sur_plusieurs_lignes()
    {
        Assert.Equal(" 3 \r\n", Run("10 READ A, B\n20 PRINT A + B\n30 DATA 1\n40 DATA 2"));
    }

    [Fact]
    public void READ_remplit_un_tableau()
    {
        Assert.Equal(" 10  50 \r\n",
                     Run("10 DIM T(4)\n20 FOR I = 0 TO 4 : READ T(I) : NEXT\n30 PRINT T(0); T(4)\n40 DATA 10, 20, 30, 40, 50"));
    }

    [Fact]
    public void Plus_rien_a_lire_donne_DATA_exhausted()
    {
        Assert.Equal("DATA exhausted in 10\r\n", Run("10 READ A, B\n20 DATA 1"));
    }

    [Fact]
    public void Sans_aucun_DATA_READ_donne_DATA_exhausted()
    {
        Assert.Equal(" 1 \r\nDATA exhausted in 20\r\n", Run("10 PRINT 1\n20 READ A"));
    }

    [Fact]
    public void RESTORE_fait_repartir_au_premier_DATA()
    {
        Assert.Equal(" 4  4 \r\n", Run("10 READ A : RESTORE : READ B\n20 PRINT A; B\n30 DATA 4, 5"));
    }

    [Fact]
    public void RESTORE_ligne_fait_repartir_au_DATA_de_cette_ligne()
    {
        Assert.Equal(" 3 \r\n", Run("10 RESTORE 40 : READ A\n20 PRINT A\n30 DATA 1, 2\n40 DATA 3, 4"));
    }

    [Fact]
    public void RESTORE_ligne_sans_DATA_part_du_DATA_suivant()
    {
        Assert.Equal(" 9 \r\n", Run("10 RESTORE 30 : READ A : PRINT A : END\n20 DATA 1\n30 PRINT 5\n40 DATA 9"));
    }

    [Fact]
    public void RESTORE_apres_le_dernier_DATA_ne_laisse_rien_a_lire()
    {
        Assert.Equal("DATA exhausted in 10\r\n", Run("10 RESTORE 30 : READ A\n20 DATA 1\n30 END"));
    }

    [Fact]
    public void Un_nombre_de_32768_a_65535_se_relit_comme_sur_le_CPC()
    {
        Assert.Equal("-25536 \r\n", Run("10 READ A : PRINT A\n20 DATA 40000"));
    }

    [Fact]
    public void READ_dans_une_variable_BYTE_garde_l_octet_de_poids_faible()
    {
        Assert.Equal(" 44 \r\n", Run("10 BYTE B\n20 READ B : PRINT B\n30 DATA 300"));
    }

    [Fact]
    public void READ_et_tableaux_partagent_le_message_d_erreur()
    {
        Assert.Equal("Subscript out of range in 20\r\n", Run("10 DIM T(1)\n20 READ T(5)\n30 DATA 7"));
    }

    [Fact]
    public void Un_programme_avec_DATA_sans_READ_n_ajoute_rien()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 DATA 1, 2, 3\n20 PRINT 1"), 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.DataName, program.Modules);
    }

    [Fact]
    public void INPUT_range_le_nombre_tape_dans_une_case()
    {
        Assert.Equal("? 42\r\n 42 \r\n", Run("10 DIM T(3)\n20 INPUT T(2)\n30 PRINT T(2)", "42\r"));
    }

    [Fact]
    public void INPUT_melange_variable_et_case()
    {
        Assert.Equal("? 1,2\r\n 1  2 \r\n", Run("10 INPUT A, T(I)\n20 PRINT A; T(0)", "1,2\r"));
    }

    [Fact]
    public void INPUT_dans_une_case_absente_donne_l_erreur_du_tableau()
    {
        Assert.Equal("? 5\r\nSubscript out of range in 20\r\n", Run("10 DIM T(3)\n20 INPUT T(9)", "5\r"));
    }
}
