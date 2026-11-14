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

/// <summary>Lot E, étape E2 (D063) : LEFT$, RIGHT$, MID$, CHR$, ASC, STR$, VAL, STRING$, SPACE$, UPPER$, LOWER$, INSTR, HEX$, BIN$.</summary>
public class StringFunctionsTests
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

    [Theory]
    [InlineData("10 A = LEFT$(\"A\", 1)")]
    [InlineData("10 A$ = LEFT$(1, 1)")]
    [InlineData("10 A$ = CHR$(\"A\")")]
    [InlineData("10 A = VAL(5)")]
    [InlineData("10 A = INSTR(1, 2, \"A\")")]
    [InlineData("10 A = ASC(65)")]
    public void Chaque_fonction_verifie_le_genre_de_ses_valeurs(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.TypeMismatch);
    }

    [Theory]
    [InlineData("10 A$ = MID$(\"A\")")]
    [InlineData("10 A$ = LEFT$(\"A\")")]
    [InlineData("10 A$ = HEX$(1, 2, 3)")]
    public void Le_nombre_de_valeurs_est_verifie(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.WrongArgumentCount);
    }

    [Fact]
    public void Un_programme_avec_une_fonction_de_texte_contient_le_module()
    {
        GeneratedProgram program = CodeGenerator.Generate(Parse("10 PRINT CHR$(65)"), 0x4000);

        Assert.Contains(RuntimeLibrary.StrFunctionsName, program.Modules);
    }

    // ---------- Exécution ----------

    [Fact]
    public void LEFT_RIGHT_MID()
    {
        Assert.Equal("BON-JOUR-JO-JOUR\r\n",
                     Run("10 A$ = \"BONJOUR\"\n20 PRINT LEFT$(A$, 3); \"-\"; RIGHT$(A$, 4); \"-\"; MID$(A$, 4, 2); \"-\"; MID$(A$, 4)"));
    }

    [Fact]
    public void LEFT_RIGHT_MID_aux_limites()
    {
        Assert.Equal("[][BONJOUR][][BONJOUR]\r\n",
                     Run("10 A$ = \"BONJOUR\"\n20 PRINT \"[\"; LEFT$(A$, 0); \"][\"; LEFT$(A$, 20); \"][\"; MID$(A$, 10); \"][\"; RIGHT$(A$, 20); \"]\""));
    }

    [Theory]
    [InlineData("10 PRINT MID$(\"ABC\", 0)")]
    [InlineData("10 PRINT LEFT$(\"ABC\", -1)")]
    [InlineData("10 PRINT CHR$(256)")]
    [InlineData("10 PRINT ASC(\"\")")]
    [InlineData("10 PRINT INSTR(0, \"A\", \"A\")")]
    [InlineData("10 PRINT HEX$(1, 17)")]
    public void Une_valeur_hors_limites_donne_Improper_argument(string source)
    {
        Assert.Equal("Improper argument in 10\r\n", Run(source));
    }

    [Fact]
    public void CHR_et_ASC()
    {
        Assert.Equal("A 66 D\r\n", Run("10 PRINT CHR$(65); ASC(\"B\"); CHR$(ASC(\"C\") + 1)"));
    }

    [Fact]
    public void STR_comme_le_Locomotive()
    {
        Assert.Equal(" 42/-7/-32768/ 0 2 \r\n",
                     Run("10 A$ = STR$(42) + \"/\" + STR$(-7) + \"/\" + STR$(-32768) + \"/\" + STR$(0)\n20 PRINT A$; LEN(STR$(5))"));
    }

    [Fact]
    public void VAL_lit_les_nombres_decimaux_hexadecimaux_et_binaires()
    {
        Assert.Equal(" 123 -45  255  31  5  0  0  255 \r\n",
                     Run("10 PRINT VAL(\"123\"); VAL(\" -45XY\"); VAL(\"&FF\"); VAL(\"&H1F\"); VAL(\"&X101\"); VAL(\"\"); VAL(\"ABC\"); VAL(\"&ff\")"));
    }

    [Fact]
    public void STRING_et_SPACE()
    {
        Assert.Equal("***AA[  ]\r\n", Run("10 PRINT STRING$(3, \"*\"); STRING$(2, 65); \"[\"; SPACE$(2); \"]\""));
    }

    [Fact]
    public void UPPER_et_LOWER()
    {
        Assert.Equal("BONJOUR 1!bonjour 1!Bonjour 1!\r\n",
                     Run("10 A$ = \"Bonjour 1!\" : PRINT UPPER$(A$); LOWER$(A$); A$"));
    }

    [Fact]
    public void INSTR_trouve_la_position()
    {
        Assert.Equal(" 4  2  5  0  1 \r\n",
                     Run("10 A$ = \"BONJOUR\"\n20 PRINT INSTR(A$, \"JOUR\"); INSTR(A$, \"O\"); INSTR(3, A$, \"O\"); INSTR(A$, \"Z\"); INSTR(A$, \"\")"));
    }

    [Fact]
    public void HEX_et_BIN()
    {
        Assert.Equal("FF FFFF 000A 101 00000101 0\r\n",
                     Run("10 PRINT HEX$(255); \" \"; HEX$(-1); \" \"; HEX$(10, 4); \" \"; BIN$(5); \" \"; BIN$(5, 8); \" \"; HEX$(0)"));
    }

    [Fact]
    public void Un_texte_retourne_lettre_par_lettre()
    {
        Assert.Equal("FEDCBA\r\n", Run("10 A$ = \"ABCDEF\" : FOR I = 1 TO 6 : B$ = B$ + MID$(A$, 7 - I, 1) : NEXT : PRINT B$"));
    }

    [Fact]
    public void Le_menage_avec_des_fonctions()
    {
        string source = "10 MEMORY 22000\n" +
                        "20 A$ = \"ABCDEFGHIJ\"\n" +
                        "30 FOR I = 1 TO 300 : B$ = LEFT$(A$, 5) + RIGHT$(A$, 5) + STR$(I) : NEXT\n" +
                        "40 PRINT B$";

        Assert.Equal("ABCDEFGHIJ 300\r\n", Run(source));
    }

    [Fact]
    public void La_ligne_du_programme_RSX_POKE_VAL_MID()
    {
        // Comme le programme de référence : POKE p+a, VAL("&"+MID$(a$, a*2+1, 2))
        string source = "10 A$ = \"3E2AC9\"\n" +
                        "20 FOR A = 0 TO 2 : POKE &9000 + A, VAL(\"&\" + MID$(A$, A * 2 + 1, 2)) : NEXT\n" +
                        "30 PRINT PEEK(&9000); PEEK(&9001); PEEK(&9002)";

        Assert.Equal(" 62  42  201 \r\n", Run(source));
    }
}
