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
/// Lot I2a (D080) : ON ERROR GOTO, RESUME, RESUME NEXT, RESUME ligne, ERROR, ERR, ERL. Référence : examples\erreurs.bas
/// tapé dans le vrai BASIC (SugarBox, 16 nov. 2026).
/// </summary>
public class OnErrorTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        string program = string.Join("\n",
            "20 ON ERROR GOTO 1000", "30 DIM T(5)", "40 T(9)=1", "50 PRINT \"APRES 40\"", "55 Z=0", "60 A=1/Z",
            "70 PRINT \"APRES 60\"", "80 ERROR 200", "90 PRINT \"APRES 80\"", "100 N=0", "110 PRINT 10/N", "120 PRINT \"FIN\"",
            "130 ON ERROR GOTO 0", "140 ERROR 5",
            "1000 PRINT \"ERR=\";ERR;\"ERL=\";ERL", "1010 IF ERL=110 THEN N=2:RESUME", "1020 IF ERL=60 THEN RESUME 70",
            "1030 RESUME NEXT");

        Assert.Equal("ERR= 9 ERL= 40 \r\nAPRES 40\r\nERR= 11 ERL= 60 \r\nAPRES 60\r\nERR= 200 ERL= 80 \r\nAPRES 80\r\n" +
                     "ERR= 11 ERL= 110 \r\n 5 \r\nFIN\r\nImproper argument in 140\r\n", Run(program));
    }

    [Fact]
    public void RESUME_NEXT_continue_apres_le_deux_points()
    {
        Assert.Equal("EB\r\n", Run("10 ON ERROR GOTO 100\n20 Z=0:A=1/Z:PRINT \"B\"\n30 END\n100 PRINT \"E\";:RESUME NEXT"));
    }

    [Fact]
    public void Erreur_dans_un_sous_programme()
    {
        Assert.Equal("ESFIN\r\n", Run("10 ON ERROR GOTO 100\n20 GOSUB 50\n30 PRINT \"FIN\"\n40 END\n" +
                                      "50 ERROR 7:PRINT \"S\";\n60 RETURN\n100 PRINT \"E\";:RESUME NEXT"));
    }

    [Fact]
    public void Erreur_pendant_le_traitement_d_une_erreur_arrete_le_programme()
    {
        Assert.Equal("Type mismatch in 100\r\n", Run("10 ON ERROR GOTO 100\n20 ERROR 5\n30 END\n100 ERROR 13"));
    }

    [Fact]
    public void RESUME_sans_erreur()
    {
        Assert.Equal("Unexpected RESUME in 10\r\n", Run("10 RESUME NEXT"));
    }

    [Fact]
    public void ERROR_d_un_numero_sans_message()
    {
        Assert.Equal("Unknown error in 10\r\n", Run("10 ERROR 99"));
    }

    [Fact]
    public void ERR_et_ERL_sans_erreur()
    {
        Assert.Equal(" 0  0 \r\n", Run("10 PRINT ERR;ERL"));
    }

    [Fact]
    public void Division_par_zero_sans_ON_ERROR_le_programme_continue()
    {
        Assert.Equal("Division by zero\r\n 0 \r\n", Run("10 Z=0:A=5/Z:PRINT A"));
    }

    [Theory]
    [InlineData("10 ON ERROR 100\n100 RETURN", DiagnosticCode.ExpectedGoto)]
    [InlineData("10 ERROR 0", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 RESUME 999", DiagnosticCode.LineNotFound)]
    [InlineData("10 ON ERROR GOTO 999", DiagnosticCode.LineNotFound)]
    public void Ecritures_refusees(string source, DiagnosticCode code)
    {
        Assert.Contains(Errors(source), e => e.Code == code);
    }

    [Theory]
    [InlineData("10 ON ERROR GOTO 0")]
    [InlineData("10 ON ERROR GO TO 20\n20 RESUME")]
    [InlineData("10 RESUME 0")]
    [InlineData("10 A = ERR + ERL")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }

    [Fact]
    public void Sans_ON_ERROR_rien_n_est_ajoute()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 PRINT 1\n20 PRINT 2").Program, 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.OnErrorName, program.Modules);
    }
}
