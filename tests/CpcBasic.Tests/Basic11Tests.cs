// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot L1 (D102) : FRAME, CURSOR, COPYCHR$ (BASIC 1.1, routines du firmware présentes sur tous les CPC).
/// Référence : vrai BASIC 1.1 dans WinAPE (essais\refl1.dsk, 17 nov. 2026) ; examples\basic11.bas.
/// </summary>
public class Basic11Tests
{
    static (string Output, MiniZ80 Cpu) Run(string source, Action<MiniZ80>? setUp = null)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        setUp?.Invoke(cpu);
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    [Fact]
    public void COPYCHR_lit_le_caractere_a_la_place_du_curseur()
    {
        // WinAPE : « ABC », LOCATE 2,1 : COPYCHR$(#0) = « B » (66)
        (string output, _) = Run("10 LOCATE 2,1:A$=COPYCHR$(#0)\n20 PRINT \"[\";A$;\"]\";ASC(A$);LEN(A$)",
                                 cpu => cpu.ScreenChars[(2, 1)] = (byte)'B');

        Assert.Equal("[B] 66  1 \r\n", output);
    }

    [Fact]
    public void COPYCHR_d_un_caractere_non_reconnu_est_vide()
    {
        Assert.Equal("[] 0 \r\n", Run("10 LOCATE 9,9:A$=COPYCHR$(#0)\n20 PRINT \"[\";A$;\"]\";LEN(A$)").Output);
    }

    [Fact]
    public void FRAME_attend_le_balayage()
    {
        MiniZ80 cpu = Run("10 FOR I=1 TO 3:FRAME:NEXT").Cpu;

        Assert.Equal(new[] { 0xBD19, 0xBD19, 0xBD19 }, cpu.FirmwareCalls);
    }

    [Fact]
    public void CURSOR_allume_et_eteint()
    {
        // TXT CUR ON (&BB81) / OFF (&BB84) : interrupteur du système ; TXT CUR ENABLE (&BB7B) / DISABLE (&BB7E) : utilisateur
        MiniZ80 cpu = Run("10 CURSOR 1:CURSOR 0,1:A=0:CURSOR A,A+1").Cpu;

        Assert.Equal(new[] { 0xBB81, 0xBB84, 0xBB7B, 0xBB84, 0xBB7B }, cpu.FirmwareCalls);
    }

    [Fact]
    public void CURSOR_virgule_ne_touche_que_l_interrupteur_utilisateur()
    {
        // D123 : « CURSOR ,1 » accepté par le vrai BASIC 1.1 (SugarBox, 18 nov. 2026) ; seul ENABLE / DISABLE est appelé
        MiniZ80 cpu = Run("10 CURSOR ,1:CURSOR ,0:A=1:CURSOR ,A").Cpu;

        Assert.Equal(new[] { 0xBB7B, 0xBB7E, 0xBB7B }, cpu.FirmwareCalls);
    }

    [Theory]
    [InlineData("10 CURSOR ,")]
    [InlineData("10 CURSOR ,2")]
    [InlineData("10 CURSOR ,1,1")]
    [InlineData("10 CURSOR 2")]
    [InlineData("10 CURSOR")]
    [InlineData("10 FRAME 1")]
    [InlineData("10 A$=COPYCHR$(0)")]
    [InlineData("10 A$=COPYCHR$(#8)")]
    [InlineData("10 A=COPYCHR$(#0)")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
