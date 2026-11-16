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
/// Lot I3a (D082) : ON SQ(canal) GOSUB ligne. Référence : vrai BASIC (SugarBox, 16 nov. 2026) : 5 appels tout de suite
/// (le son en cours et 4 en attente), puis un à chaque fin de son ; examples\sonfile.bas.
/// </summary>
public class OnSqTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static (string Output, MiniZ80 Cpu) Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80, SoundQueueModel = true };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    [Fact]
    public void Cinq_appels_tout_de_suite_puis_un_par_fin_de_son()
    {
        (string output, MiniZ80 cpu) = Run("10 ON SQ(1) GOSUB 100:PRINT C;\n20 IF C<7 THEN 20\n30 PRINT C\n40 END\n" +
                                           "100 C=C+1:SOUND 1,200,50:ON SQ(1) GOSUB 100:RETURN");

        Assert.Equal(" 5  7 \r\n", output);
        Assert.Equal(7, cpu.Sounds.Count);
    }

    [Fact]
    public void Sans_nouveau_ON_SQ_un_seul_appel()
    {
        (string output, _) = Run("10 ON SQ(2) GOSUB 100\n20 FOR I=1 TO 50:NEXT\n30 PRINT C\n40 END\n100 C=C+1:RETURN");

        Assert.Equal(" 1 \r\n", output);
    }

    [Fact]
    public void Canal_calcule_invalide_Improper_argument()
    {
        (string output, _) = Run("10 N=3:ON SQ(N) GOSUB 100\n20 END\n100 RETURN");

        Assert.Equal("Improper argument in 10\r\n", output);
    }

    [Fact]
    public void Le_module_des_canaux_sonores_remplace_celui_des_minuteries()
    {
        GeneratedProgram sq = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 ON SQ(1) GOSUB 20\n20 RETURN").Program, 0x4000);
        GeneratedProgram timer = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 AFTER 5 GOSUB 20\n20 RETURN").Program, 0x4000);

        Assert.Contains(RuntimeLibrary.EventsSoundName, sq.Modules);
        Assert.DoesNotContain(RuntimeLibrary.EventsName, sq.Modules);
        Assert.Contains(RuntimeLibrary.EventsName, timer.Modules);
        Assert.DoesNotContain(RuntimeLibrary.EventsSoundName, timer.Modules);
    }

    [Theory]
    [InlineData("10 ON SQ(3) GOSUB 20\n20 RETURN", DiagnosticCode.ValueOutOfRange)]
    [InlineData("10 ON SQ(1) 20\n20 RETURN", DiagnosticCode.ExpectedGoto)]
    [InlineData("10 ON SQ 1 GOSUB 20\n20 RETURN", DiagnosticCode.ExpectedGoto)]
    [InlineData("10 ON SQ(1) GOSUB 99", DiagnosticCode.LineNotFound)]
    public void Ecritures_refusees(string source, DiagnosticCode code)
    {
        Assert.Contains(Errors(source), e => e.Code == code);
    }

    [Theory]
    [InlineData("10 ON SQ(1) GOSUB 20\n20 RETURN")]
    [InlineData("10 ON SQ(4) GO SUB 20\n20 RETURN")]
    [InlineData("10 C=2:ON SQ(C) GOSUB 20\n20 RETURN")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }
}
