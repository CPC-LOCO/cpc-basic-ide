// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot K9 (D097) : TIME complet (32 bits, à virgule) avec le réglage F8 ; ON x GOTO / GOSUB hors de 0 à 255.
/// Référence : vrai BASIC (SugarBox, 16 nov. 2026) ; examples\temps.bas.
/// </summary>
public class TimeFullTests
{
    static string Run(string source, bool realByDefault, uint clock = 0)
    {
        var options = new CompilerOptions(RealByDefault: realByDefault);
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, options);
        var cpu = new MiniZ80 { WindowWidth = 80, Clock = clock };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Fact]
    public void Avec_F8_TIME_va_au_dela_de_32767()
    {
        Assert.Equal(" 100000 \r\n 0 \r\n", Run("10 PRINT TIME\n20 T=TIME:PRINT TIME-T", true, 100000));
    }

    [Fact]
    public void Avec_F8_RANDOMIZE_TIME_ne_deborde_pas()
    {
        Assert.Equal("OK\r\n", Run("10 RANDOMIZE TIME:PRINT \"OK\"", true, 100000));
    }

    [Fact]
    public void Sans_F8_TIME_reste_sur_16_bits()
    {
        // 100000 = &186A0 : les 16 bits du bas valent &86A0 = -31072 (comme avant, D026)
        Assert.Equal("-31072 \r\n", Run("10 PRINT TIME", false, 100000));
    }

    [Theory]
    [InlineData("10 ON 256 GOTO 20\n20 PRINT \"X\"", "Improper argument in 10\r\n")]
    [InlineData("10 X=-1:ON X GOSUB 20\n20 PRINT \"X\"", "Improper argument in 10\r\n")]
    [InlineData("10 ON 255 GOTO 20:PRINT \"255 OK\"\n20 PRINT \"X\"", "255 OK\r\nX\r\n")]
    [InlineData("10 FOR I=0 TO 3:ON I GOTO 30,30:PRINT \"SUITE\";I:NEXT\n30 PRINT \"SAUT\"", "SUITE 0 \r\nSAUT\r\n")]
    public void ON_comme_le_vrai_BASIC(string source, string expected)
    {
        Assert.Equal(expected, Run(source, false));
    }
}
