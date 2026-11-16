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
/// Lot K7 (D095) : DEF FN et FN, fonctions du programmeur. Référence : vrai BASIC (SugarBox, 16 nov. 2026) ;
/// examples\fonctfn.bas.
/// </summary>
public class UserFunctionTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source, bool realByDefault = false)
    {
        var options = new CompilerOptions(RealByDefault: realByDefault);
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, options);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    const string Reference =
        "10 X=100\n20 DEF FNDOUBLE(X)=X*2\n30 DEF FNA$(N$)=N$+\"!\"\n40 DEF FNR=X+1\n50 PRINT FNDOUBLE(21);X\n" +
        "60 PRINT FNA$(\"BONJOUR\")\n70 PRINT FNR;FN R\n80 DEF FNM(A,B)=A*B+X\n90 PRINT FNM(2,3)\n";

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        // SugarBox : 42 100 / BONJOUR! / 101 101 / 106 (le X de FNDOUBLE ne touche pas le X du programme)
        Assert.Equal(" 42  100 \r\nBONJOUR!\r\n 101  101 \r\n 106 \r\n", Run(Reference));
    }

    [Fact]
    public void Avec_le_reglage_F8_FNDOUBLE_garde_la_virgule()
    {
        // Ligne 100 de la référence : 3, comme le vrai BASIC (X est alors à virgule)
        Assert.Equal(" 42  100 \r\nBONJOUR!\r\n 101  101 \r\n 106 \r\n 3 \r\n",
                     Run(Reference + "100 PRINT FNDOUBLE(1.5)", realByDefault: true));
    }

    [Fact]
    public void Avec_le_signe_point_d_exclamation()
    {
        Assert.Equal(" 3  0.5 \r\n", Run("10 DEF FNDOUBLE!(X!)=X!*2\n20 DEF FNH!(A!)=A!/2\n30 PRINT FNDOUBLE!(1.5);FNH!(1)"));
    }

    [Fact]
    public void Une_FN_peut_en_appeler_une_autre_et_s_appeler_dans_ses_valeurs()
    {
        // FNP(FNP(2,3), FNP(4,5)) = (2+3) + (4+5) : la seconde valeur appelle FNP sans abîmer la première
        Assert.Equal(" 14 \r\n 20 \r\n", Run("10 DEF FNP(A,B)=A+B\n20 DEF FNC(N)=FNP(N,N)*2\n30 PRINT FNP(FNP(2,3),FNP(4,5))\n40 PRINT FNC(5)"));
    }

    [Fact]
    public void La_definition_peut_venir_apres_l_appel()
    {
        Assert.Equal(" 9 \r\n", Run("10 PRINT FNC(3)\n20 END\n30 DEF FNC(V)=V*V"));
    }

    [Fact]
    public void Textes_et_comparaisons()
    {
        Assert.Equal("AB\r\nOUI\r\n",
                     Run("10 DEF FNJ$(A$,B$)=A$+B$\n20 PRINT FNJ$(\"A\",\"B\")\n30 IF FNJ$(\"X\",\"Y\")=\"XY\" THEN PRINT \"OUI\""));
    }

    [Theory]
    [InlineData("10 PRINT FNX(1)", DiagnosticCode.UserFunctionUnknown)]
    [InlineData("10 DEF FNA=1\n20 DEF FNA=2", DiagnosticCode.UserFunctionTwice)]
    [InlineData("10 DEF FNA(N)=FNA(N-1)", DiagnosticCode.UserFunctionRecursive)]
    [InlineData("10 DEF FNA(N)=FNB(N)\n20 DEF FNB(N)=FNA(N)", DiagnosticCode.UserFunctionRecursive)]
    [InlineData("10 DEF FNDOUBLE(X)=X*2\n20 PRINT FNDOUBLE(1.5)", DiagnosticCode.UserFunctionNeedsReal)]
    [InlineData("10 DEF FNH(X)=X/2.5", DiagnosticCode.UserFunctionNeedsReal)]
    [InlineData("10 DEF FNM(A,B)=A*B\n20 PRINT FNM(2)", DiagnosticCode.WrongArgumentCount)]
    [InlineData("10 DEF FNA$(N$)=N$\n20 PRINT FNA$(3)", DiagnosticCode.TypeMismatch)]
    [InlineData("10 DEF FNA$(N)=N", DiagnosticCode.TypeMismatch)]
    [InlineData("10 DEF FNM(A,3)=A", DiagnosticCode.UserFunctionParameter)]
    [InlineData("10 DEF X=3", DiagnosticCode.UserFunctionName)]
    public void Ecritures_refusees(string source, DiagnosticCode code)
    {
        Assert.Contains(Errors(source), d => d.Code == code);
    }

    [Fact]
    public void Le_message_donne_la_solution()
    {
        Diagnostic error = Errors("10 DEF FNDOUBLE(X)=X*2\n20 PRINT FNDOUBLE(1.5)").Single();

        Assert.Equal(2, error.Line);
        Assert.Contains("DEF FNDOUBLE(X!)", error.Message);
    }
}
