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
/// Lot L2 (D103) : DEC$(nombre, format), comme PRINT USING mais rendu comme un texte. Référence : vrai BASIC 1.1 dans
/// WinAPE (essais\refl1.dsk et essais\refl2.dsk, 17 nov. 2026) ; examples\decimal.bas.
/// </summary>
public class DecTests
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
        Assert.Equal(" 3.14/  -5/ $1,234.50\r\n[%123456]\r\n[2.3]\r\n",
                     Run("10 PRINT DEC$(3.14159,\"##.##\");\"/\";DEC$(-5,\"+###\");\"/\";DEC$(1234.5,\"$$#,###.##\")\n" +
                         "20 PRINT \"[\";DEC$(123456,\"##\");\"]\"\n30 F$=\"#.#\":PRINT \"[\";DEC$(2.25,F$);\"]\""));
    }

    [Theory]
    [InlineData("##.##", "3.14159")]
    [InlineData("+###", "-5")]
    [InlineData("$$#,###.##", "1234.5")]
    [InlineData("**$##.##", "12.5")]
    [InlineData("##.##^^^^", "12345")]
    [InlineData("##-", "-5")]
    [InlineData("##+", "5")]
    [InlineData(".##", "0.5")]
    [InlineData("###", "7")]
    public void Meme_texte_que_PRINT_USING(string format, string value)
    {
        string viaDec = Run($"10 PRINT \"[\";DEC$({value},\"{format}\");\"]\"");
        string viaUsing = Run($"10 PRINT \"[\";:PRINT USING \"{format}\";{value};:PRINT \"]\"");
        string viaVariable = Run($"10 F$=\"{format}\":PRINT \"[\";DEC$({value},F$);\"]\"");

        Assert.Equal(viaUsing, viaDec);
        Assert.Equal(viaUsing, viaVariable);
    }

    [Fact]
    public void Le_resultat_est_un_vrai_texte()
    {
        Assert.Equal(" 3 [  7]\r\n", Run("10 A$=DEC$(7,\"###\"):PRINT LEN(A$);\"[\";A$;\"]\""));
    }

    [Fact]
    public void Format_dans_une_variable_qui_ne_convient_pas()
    {
        // WinAPE : « A## » : erreur 5 (Improper argument)
        Assert.Contains("Improper argument in 10", Run("10 F$=\"A##\":PRINT DEC$(5,F$)"));
    }

    [Theory]
    [InlineData("10 PRINT DEC$(5,\"A##\")", DiagnosticCode.DecFormat)]
    [InlineData("10 PRINT DEC$(5,\"##A\")", DiagnosticCode.DecFormat)]
    [InlineData("10 PRINT DEC$(5,\"!\")", DiagnosticCode.DecFormat)]
    [InlineData("10 PRINT DEC$(5,\"\")", DiagnosticCode.DecFormat)]
    public void Formats_refuses_a_la_compilation(string source, DiagnosticCode code)
    {
        Assert.Contains(BasicAnalyzer.Analyze(source).Diagnostics, d => d.Code == code);
    }

    [Theory]
    [InlineData("10 PRINT DEC$(\"A\",\"##\")")]
    [InlineData("10 PRINT DEC$(5)")]
    [InlineData("10 A=DEC$(5,\"##\")")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
