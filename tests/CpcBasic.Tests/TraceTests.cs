// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Lot K1 (D088) : TRON, TROFF. Référence : vrai BASIC (SugarBox, 16 nov. 2026) ; examples\trace.bas.</summary>
public class TraceTests
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
    public void Comme_le_vrai_BASIC()
    {
        // la ligne 30 ne s'affiche qu'une fois, même quand NEXT y revient
        Assert.Equal("[20]A[30] 1  2 [40][60]B\r\n",
                     Run("10 TRON\n20 PRINT \"A\";\n30 FOR I=1 TO 2:PRINT I;:NEXT\n40 GOTO 60\n50 PRINT \"JAMAIS\"\n60 TROFF:PRINT \"B\""));
    }

    [Fact]
    public void Une_ligne_est_affichee_a_chaque_passage()
    {
        Assert.Equal("[20][30][20][30][40]", Run("10 TRON\n20 I=I+1\n30 IF I<2 THEN 20\n40 TROFF:END"));
    }

    [Fact]
    public void Sans_TRON_rien_n_est_ajoute()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 PRINT 1\n20 PRINT 2").Program, 0x4000);

        Assert.DoesNotContain(RuntimeLibrary.TraceName, program.Modules);
    }
}
