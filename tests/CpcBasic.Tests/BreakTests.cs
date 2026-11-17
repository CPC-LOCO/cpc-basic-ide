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
/// Lot I3b (D083) : ON BREAK CONT / STOP / GOSUB et la touche ESC (réglage F9). Référence : vrai BASIC (SugarBox,
/// 16 nov. 2026) : ESC = pause ; une autre touche : reprise ; ESC encore : « Break in N » ou appel ; ON BREAK CONT :
/// ESC sans effet.
/// </summary>
public class BreakTests
{
    const byte Esc = 0xFC;

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static GeneratedProgram Generate(string source, bool breakKey = false) =>
        CodeGenerator.Generate(BasicAnalyzer.Analyze(source).Program, 0x4000, new CompilerOptions(BreakKey: breakKey));

    static string Run(string source, bool breakKey, params (int Tick, byte Key)[] keys)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, new CompilerOptions(BreakKey: breakKey));
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.TimedKeys.AddRange(keys);
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Fact]
    public void ON_BREAK_GOSUB_a_chaque_double_ESC()
    {
        Assert.Equal(" 2 \r\n", Run("10 ON BREAK GOSUB 100\n20 IF B<2 THEN 20\n30 PRINT B\n40 END\n100 B=B+1:RETURN", false,
                                    (3, Esc), (6, Esc), (10, Esc), (13, Esc)));
    }

    [Fact]
    public void Pendant_la_pause_une_autre_touche_fait_continuer()
    {
        Assert.Equal(" 0 \r\n", Run("10 ON BREAK GOSUB 100\n20 FOR I=1 TO 2000:NEXT\n30 PRINT B\n40 END\n100 B=B+1:RETURN", false,
                                    (3, Esc), (5, (byte)' ')));
    }

    [Fact]
    public void ON_BREAK_CONT_ESC_sans_effet()
    {
        // une pause attendrait une touche : le mini-Z80 s'arrêterait faute de touche prévue
        Assert.Equal("FIN\r\n", Run("10 ON BREAK CONT\n20 FOR I=1 TO 2000:NEXT\n30 PRINT \"FIN\"", true, (3, Esc)));
    }

    [Fact]
    public void Reglage_ESC_comme_le_CPC_Break_in()
    {
        Assert.Equal("A\r\nBreak in 20\r\n", Run("10 PRINT \"A\";\n20 GOTO 20", true, (3, Esc), (5, Esc)));
    }

    [Fact]
    public void ON_BREAK_STOP_remet_Break_in()
    {
        Assert.Equal("Break in 30\r\n", Run("10 ON BREAK GOSUB 100\n20 ON BREAK STOP\n30 GOTO 30\n100 RETURN", false,
                                            (3, Esc), (5, Esc)));
    }

    [Fact]
    public void Sans_reglage_ni_ON_BREAK_rien_n_est_ajoute()
    {
        Assert.DoesNotContain(Generate("10 PRINT 1").Modules, m => m.StartsWith("Events"));
        Assert.Contains(RuntimeLibrary.EventsBreakName, Generate("10 PRINT 1", breakKey: true).Modules);
        Assert.Contains(RuntimeLibrary.EventsBreakName, Generate("10 ON BREAK CONT").Modules);
        Assert.Contains(RuntimeLibrary.EventsSoundBreakName,
                        Generate("10 ON BREAK CONT:ON SQ(1) GOSUB 20\n20 RETURN").Modules);
    }

    [Theory]
    [InlineData("10 ON BREAK")]
    [InlineData("10 ON BREAK 100\n100 RETURN")]
    [InlineData("10 ON BREAK GOTO 100\n100 RETURN")]
    public void ON_BREAK_mal_ecrit(string source)
    {
        Assert.Contains(Errors(source), e => e.Code == DiagnosticCode.ExpectedGoto);
    }

    [Fact]
    public void ON_BREAK_GOSUB_vers_une_ligne_absente()
    {
        Assert.Contains(Errors("10 ON BREAK GOSUB 99"), e => e.Code == DiagnosticCode.LineNotFound);
    }

    [Theory]
    [InlineData("10 ON BREAK CONT")]
    [InlineData("10 ON BREAK STOP")]
    [InlineData("10 ON BREAK GO SUB 20\n20 RETURN")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }
}
