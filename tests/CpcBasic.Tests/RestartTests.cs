// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Lot K3 (D090) : CLEAR, RUN, RUN ligne. examples\relance.bas.</summary>
public class RestartTests
{
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
    public void CLEAR_efface_variables_textes_et_tableaux()
    {
        Assert.Equal(" 0 [] 0 \r\n",
                     Run("10 A=5:A$=\"X\":DIM T(3):T(1)=7\n20 CLEAR\n30 PRINT A;\"[\";A$;\"]\";\n40 DIM T(3):PRINT T(1)"));
    }

    [Fact]
    public void RUN_recommence_avec_les_DATA_au_debut()
    {
        Assert.Equal(" 7  0  7  1  7  2 FIN\r\n",
                     Run("10 READ X:PRINT X;PEEK(&9000);\n20 POKE &9000,PEEK(&9000)+1\n30 IF PEEK(&9000)<3 THEN RUN\n" +
                         "40 PRINT \"FIN\"\n50 DATA 7"));
    }

    [Fact]
    public void RUN_ligne_recommence_a_cette_ligne()
    {
        // RUN 20 saute la ligne 10 (« A » une seule fois) et remet N à 0 à chaque fois
        Assert.Equal("A[0][0][0]FIN\r\n",
                     Run("10 PRINT \"A\";\n20 PRINT \"[\";N;\"]\";:N=N+1:POKE &9000,PEEK(&9000)+1\n" +
                         "30 IF PEEK(&9000)<3 THEN RUN 20\n40 PRINT \"FIN\"").Replace(" ", ""));
    }

    [Fact]
    public void RUN_oublie_les_GOSUB_et_ON_ERROR()
    {
        Assert.Equal("Unexpected RETURN in 40\r\n",
                     Run("10 ON ERROR GOTO 90:POKE &9000,PEEK(&9000)+1\n20 IF PEEK(&9000)=1 THEN GOSUB 50\n30 ON ERROR GOTO 0\n40 RETURN\n" +
                         "50 RUN\n90 PRINT \"PIEGE\""));
    }

    [Fact]
    public void RUN_vers_une_ligne_absente()
    {
        Assert.Contains(BasicAnalyzer.Analyze("10 RUN 99").Diagnostics, e => e.Code == DiagnosticCode.LineNotFound);
    }
}
