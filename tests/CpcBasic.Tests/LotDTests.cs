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
/// Lot D (D060) : nombres &amp;C9, &amp;HC9, &amp;X1010 ; AND, OR, XOR bit à bit ; NOT ; ^ ; comparaisons utilisées
/// comme valeurs (-1 ou 0) ; une valeur seule comme condition.
/// </summary>
public class LotDTests
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

    static Expression ValueOf(string expression) =>
        Assert.IsType<AssignStatement>(Parse("10 A = " + expression).Statements[0]).Value;

    // ---------- Nombres hexadécimaux et binaires ----------

    [Theory]
    [InlineData("&C9", 201)]
    [InlineData("&HC9", 201)]
    [InlineData("&c9", 201)]
    [InlineData("&X1010", 10)]
    [InlineData("&x11111111", 255)]
    [InlineData("&0", 0)]
    [InlineData("&7FFF", 32767)]
    [InlineData("&8000", -32768)]
    [InlineData("&FFFF", -1)]
    [InlineData("&A400", -23552)]
    public void Un_nombre_avec_et_commercial_vaut_un_mot_de_16_bits(string written, int value)
    {
        Assert.Equal(new NumberExpression(value), ValueOf(written));
    }

    [Theory]
    [InlineData("10 A = &")]
    [InlineData("10 A = &G")]
    [InlineData("10 A = &X2")]
    [InlineData("10 A = &H")]
    public void Un_et_commercial_sans_chiffres_est_signale(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedHexDigits);
    }

    [Theory]
    [InlineData("10 A = &10000")]
    [InlineData("10 A = &X11111111111111111")]
    public void Un_nombre_au_dela_de_FFFF_est_refuse(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.HexNumberOutOfRange);
    }

    [Fact]
    public void PRINT_affiche_la_valeur_des_nombres_hexadecimaux()
    {
        Assert.Equal(" 255  5 -1 \r\n", Run("10 PRINT &FF; &X101; &FFFF"));
    }

    [Fact]
    public void Une_adresse_au_dessus_de_32767_se_calcule_avec_un_nombre_hexadecimal()
    {
        Assert.Equal(" 9 \r\n", Run("10 P = &A400\n20 POKE P + 1, 9 : PRINT PEEK(&A401)"));
    }

    [Fact]
    public void FOR_accepte_une_limite_hexadecimale()
    {
        Assert.Equal(" 0  1  2  3 \r\n", Run("10 FOR I = 0 TO &3 : PRINT I; : NEXT : PRINT"));
    }

    [Fact]
    public void DATA_accepte_les_nombres_hexadecimaux()
    {
        Assert.Equal(" 255 -16 \r\n", Run("10 READ A, B : PRINT A; B\n20 DATA &FF, -&10"));
    }

    [Fact]
    public void Une_commande_d_ecran_verifie_aussi_un_nombre_hexadecimal()
    {
        Assert.Empty(Errors("10 BORDER &1A"));
        Assert.Contains(Errors("10 BORDER &1B"), e => e.Code == DiagnosticCode.ValueOutOfRange);
    }

    // ---------- AND, OR, XOR, NOT ----------

    [Fact]
    public void AND_OR_XOR_NOT_travaillent_bit_a_bit()
    {
        Assert.Equal(" 8  15  6 -1 -6 \r\n", Run("10 PRINT 12 AND 10; 12 OR 3; 12 XOR 10; NOT 0; NOT 5"));
    }

    [Fact]
    public void AND_OR_XOR_avec_des_variables()
    {
        Assert.Equal(" 15  255 -256 \r\n", Run("10 A = &FF : B = &F0F\n20 PRINT A AND B; A OR B AND 0; NOT A"));
    }

    [Theory]
    [InlineData("10 PRINT 1 OR 2 AND 0", " 1 \r\n")]       // AND passe avant OR
    [InlineData("10 PRINT 3 XOR 1 OR 2", " 0 \r\n")]       // OR passe avant XOR
    [InlineData("10 PRINT NOT 1 = 2", "-1 \r\n")]          // la comparaison passe avant NOT
    [InlineData("10 PRINT 1 + 2 * 3 = 7", "-1 \r\n")]      // le calcul passe avant la comparaison
    public void L_ordre_des_operations_est_celui_du_Locomotive(string source, string output)
    {
        Assert.Equal(output, Run(source));
    }

    [Fact]
    public void XOR_et_NOT_sont_des_mots_reserves()
    {
        Assert.NotEmpty(Errors("10 XOR = 1"));
        Assert.NotEmpty(Errors("10 NOT = 1"));
    }

    // ---------- Comparaisons comme valeurs ----------

    [Fact]
    public void Une_comparaison_vaut_moins_1_si_elle_est_vraie_et_0_sinon()
    {
        Assert.Equal("-1  0 \r\n", Run("10 A = 5 > 3 : B = 5 < 3 : PRINT A; B"));
    }

    [Fact]
    public void Toutes_les_comparaisons_donnent_une_valeur()
    {
        Assert.Equal("-1  0 -1 -1  0 -1 \r\n",
                     Run("10 A = 2 : B = -3\n20 PRINT A = 2; A <> 2; B < A; A >= A; B > A; B <= A"));
    }

    [Fact]
    public void Une_comparaison_sert_dans_un_calcul()
    {
        Assert.Equal(" 3 \r\n", Run("10 X = 5\n20 PRINT -(X > 2) - (X < 2) * 2 + (X = 5) * -2"));
    }

    [Fact]
    public void Une_comparaison_est_rangee_comme_valeur()
    {
        var compare = Assert.IsType<CompareExpression>(ValueOf("B > 3"));

        Assert.Equal(CompareOperator.Greater, compare.Operator);
    }

    // ---------- Conditions ----------

    [Fact]
    public void Une_valeur_seule_est_une_condition_vraie_si_elle_n_est_pas_0()
    {
        Assert.Equal(" 1 \r\n 3 \r\n", Run("10 A = 0 : B = 7\n20 IF B THEN PRINT 1\n30 IF A THEN PRINT 2\n40 IF NOT A THEN PRINT 3"));
    }

    [Fact]
    public void IF_avec_AND_entre_deux_nombres_est_bit_a_bit_comme_le_Locomotive()
    {
        Assert.Equal(" 0 \r\n", Run("10 IF 1 AND 2 THEN PRINT 1 ELSE PRINT 0"));
    }

    [Fact]
    public void Des_comparaisons_reliees_par_AND_restent_des_sauts_rapides()
    {
        var test = Assert.IsType<IfStatement>(Parse("10 IF A > 1 AND B < 2 THEN PRINT 1").Statements[0]);

        Assert.IsType<AndCondition>(test.Condition);
    }

    [Fact]
    public void NOT_devant_des_comparaisons_est_retourne()
    {
        var test = Assert.IsType<IfStatement>(Parse("10 IF NOT (A = 1 OR A = 3) THEN PRINT 1").Statements[0]);

        var both = Assert.IsType<AndCondition>(test.Condition);
        Assert.Equal(CompareOperator.NotEqual, Assert.IsType<ComparisonCondition>(both.Left).Operator);
        Assert.Equal(" 1 \r\n", Run("10 A = 2\n20 IF NOT (A = 1 OR A = 3) THEN PRINT 1"));
    }

    [Fact]
    public void Une_valeur_seule_devient_un_test_different_de_0()
    {
        var test = Assert.IsType<IfStatement>(Parse("10 IF A THEN PRINT 1").Statements[0]);

        var comparison = Assert.IsType<ComparisonCondition>(test.Condition);
        Assert.Equal(CompareOperator.NotEqual, comparison.Operator);
        Assert.Equal(new NumberExpression(0), comparison.Right);
    }

    [Fact]
    public void WHILE_accepte_une_valeur_seule()
    {
        Assert.Equal(" 3  2  1 \r\n", Run("10 A = 3\n20 WHILE A\n30 PRINT A;\n40 A = A - 1\n50 WEND\n60 PRINT"));
    }

    // ---------- Puissance ----------

    [Fact]
    public void La_puissance_entiere()
    {
        Assert.Equal(" 1024  1 -8 -4  64  81 \r\n",
                     Run("10 A = 3 : B = 4\n20 PRINT 2 ^ 10; 3 ^ 0; (-2) ^ 3; -2 ^ 2; 2 ^ 3 ^ 2; A ^ B"));
    }

    [Fact]
    public void La_puissance_avec_un_exposant_negatif()
    {
        Assert.Equal(" 0  1 -1  1  1 \r\n", Run("10 PRINT 2 ^ -1; 1 ^ -3; (-1) ^ -3; (-1) ^ -2; 0 ^ 0"));
    }

    [Fact]
    public void La_puissance_passe_avant_la_multiplication()
    {
        Assert.Equal(" 18 \r\n", Run("10 PRINT 2 * 3 ^ 2"));
    }
}
