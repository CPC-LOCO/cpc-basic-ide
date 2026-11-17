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
/// Lot K10 (D098) : PRINT USING avec un format dans une variable ou un calcul, lu pendant l'exécution.
/// Il doit donner exactement le même affichage que le format écrit entre guillemets (D074, vérifié sur le vrai BASIC).
/// Référence : SugarBox, 17 nov. 2026 ; examples\formats.bas.
/// </summary>
public class UsingDynamicTests
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

    [Theory]
    [InlineData("###.##", "3.14159")]
    [InlineData("PRIX ##.# EUR", "5;-7.25")]
    [InlineData("!", "\"BONJOUR\"")]
    [InlineData("\\  \\", "\"ABCDEF\"")]
    [InlineData("&", "\"A\"+\"B\"")]
    [InlineData("+##.##", "-3.5;2")]
    [InlineData("**$##.##", "12.5")]
    [InlineData("$$#,###.##", "1234.5")]
    [InlineData("##.##^^^^", "12345")]
    [InlineData("##-", "-5;5")]
    [InlineData("##+", "-5;5")]
    [InlineData(".##", "0.5")]
    [InlineData("A ## B ## C", "1;2;3")]
    [InlineData("A ## B", "1;")]
    [InlineData("NOM : & AGE : ##", "\"LEA\";12")]
    [InlineData("+ $ . TEXTE ##", "7")]
    public void Meme_affichage_qu_avec_le_format_entre_guillemets(string format, string values)
    {
        string written = Run($"10 PRINT USING \"{format}\";{values}\n20 PRINT \"/\"");
        string fromVariable = Run($"10 F$=\"{format}\":PRINT USING F$;{values}\n20 PRINT \"/\"");

        Assert.Equal(written, fromVariable);
    }

    [Fact]
    public void Comme_le_vrai_BASIC()
    {
        // SugarBox : « PRIX  5.0 EURPRIX -7.3 EUR » : le format est repris pour la seconde valeur, sans passer à la ligne
        Assert.Equal("PRIX  5.0 EURPRIX -7.3 EUR\r\n", Run("10 F$=\"PRIX ##.# EUR\":PRINT USING F$;5;-7.25"));
    }

    [Fact]
    public void Le_format_peut_etre_un_calcul()
    {
        Assert.Equal(Run("10 PRINT USING \"##.#\";2.25"), Run("10 PRINT USING \"##\"+\".#\";2.25"));
    }

    [Fact]
    public void Format_sans_emplacement()
    {
        // SugarBox : le texte du format est écrit, puis « Improper argument in 60 »
        string output = Run("10 F$=\"X\":PRINT USING F$;5");

        Assert.StartsWith("X", output);
        Assert.Contains("Improper argument in 10", output);
    }

    [Fact]
    public void Un_texte_dans_un_emplacement_de_nombre()
    {
        Assert.Contains("Type mismatch in 10", Run("10 F$=\"##\":PRINT USING F$;\"A\""));
    }

    [Theory]
    [InlineData("10 PRINT USING 5;5")]
    [InlineData("10 PRINT USING F$")]
    [InlineData("10 PRINT USING F$;")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
