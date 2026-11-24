// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// ACCEPT permis [, longueur] , ["question" ; ou ,] variable (D223, nom et forme choisis par l'utilisateur) :
/// saisie limitée à certains caractères. examples\accept.bas.
/// </summary>
public class AcceptTests
{
    const char Del = (char)127;

    static BasicProgram Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis.Program;
    }

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    /// <summary>Lance le programme en tapant <paramref name="typed"/> (le caractère \r est la touche Entrée).</summary>
    static string Run(string source, string typed)
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

    [Fact]
    public void ACCEPT_est_un_INPUT_avec_les_caracteres_permis()
    {
        var input = Assert.IsType<InputStatement>(Parse("10 ACCEPT \"0123456789\", \"AGE : \"; A").Statements[0]);

        Assert.IsType<StringLiteralExpression>(input.Allowed);
        Assert.Null(input.MaxLength);
        Assert.Equal("AGE : ", input.Prompt);
        Assert.Equal(new[] { "A" }, input.Variables.ToArray());
    }

    [Fact]
    public void Les_touches_qui_ne_sont_pas_permises_sont_ignorees()
    {
        Assert.Equal("AGE : ? 123\r\n 123 \r\n", Run("10 ACCEPT \"0123456789\", \"AGE : \"; A\n20 PRINT A", "1a2b3\r"));
    }

    [Fact]
    public void La_longueur_arrete_la_saisie()
    {
        Assert.Equal("N12\r\n 12 \r\n", Run("10 ACCEPT \"0123456789\", 2, \"N\", A\n20 PRINT A", "1234\r"));
    }

    [Fact]
    public void Sans_question_et_avec_un_texte()
    {
        Assert.Equal("? O\r\nO\r\n", Run("10 ACCEPT \"ONon\", R$\n20 PRINT R$", "xyO\r"));
        Assert.Equal("? 5\r\n 5 \r\n", Run("10 ACCEPT \"0123456789\"; N\n20 PRINT N", "x5\r"));
    }

    [Fact]
    public void DEL_et_ENTREE_marchent_toujours()
    {
        Assert.EndsWith("\r\nAA\r\n", Run("10 ACCEPT \"AB\", 2, X$\n20 PRINT X$", "AB" + Del + "A\r"));
    }

    [Fact]
    public void Les_caracteres_permis_peuvent_etre_calcules()
    {
        Assert.EndsWith("\r\nXZ\r\n", Run("10 C$=\"XY\"\n20 ACCEPT C$+\"Z\", A$\n30 PRINT A$", "AXZQ\r"));
    }

    [Fact]
    public void INPUT_apres_ACCEPT_accepte_tout()
    {
        Assert.EndsWith("\r\nHELLO\r\n", Run("10 ACCEPT \"1\", 1, A\n20 INPUT B$\n30 PRINT B$", "1\rHELLO\r"));
    }

    [Fact]
    public void Redo_from_start_garde_les_caracteres_permis()
    {
        Assert.Equal("? --\r\n?Redo from start\r\n? 5\r\n 5 \r\n",
                     Run("10 ACCEPT \"-0123456789\", A\n20 PRINT A", "--\rx5\r"));
    }

    [Theory]
    [InlineData("10 ACCEPT")]
    [InlineData("10 ACCEPT \"12\"")]
    [InlineData("10 ACCEPT 5, A")]
    [InlineData("10 ACCEPT \"12\", 0, A")]
    [InlineData("10 ACCEPT \"12\", 256, A")]
    [InlineData("10 ACCEPT \"12\", 3, A, B")]
    public void Un_mauvais_ACCEPT_est_refuse(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 L=0:ACCEPT \"12\", L, A")]
    [InlineData("10 L=300:ACCEPT \"12\", L, A")]
    public void Une_longueur_calculee_hors_des_limites_arrete_le_programme(string source)
    {
        Assert.Contains("Improper argument", Run(source, ""));
    }

    [Fact]
    public void ACCEPT_va_aussi_sur_la_cartouche()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 ACCEPT \"ON\", 1, \"OK\"; R$");

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
    }
}
