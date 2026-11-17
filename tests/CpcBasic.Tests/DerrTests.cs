// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot K11 (D099) : DERR, dernière erreur d'AMSDOS. Référence : vrai BASIC 1.1 (SugarBox, 17 nov. 2026) :
/// 0 au départ, puis ERR 32 et DERR 146 après OPENIN d'un fichier absent. Exemple : examples\derr.bas.
/// </summary>
public class DerrTests
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
        Assert.Equal(" 0 \r\n 32  146 \r\n",
                     Run("10 PRINT DERR\n20 ON ERROR GOTO 100\n30 OPENIN \"ABSENT\"\n40 END\n100 PRINT ERR;DERR\n110 END"));
    }

    [Fact]
    public void DERR_sans_fichier_vaut_0()
    {
        Assert.Equal(" 0 \r\n", Run("10 A=DERR:PRINT A"));
    }

    [Theory]
    [InlineData("10 DERR = 5")]
    [InlineData("10 A = DERR(1)")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
