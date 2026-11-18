// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using System.Text.RegularExpressions;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// DEBUG (D136, solution C de la liste des variables) : avec le réglage « Mise au point » (F2), affiche les variables
/// et leurs valeurs, attend ESPACE (suite) ou S (stop : plus aucun DEBUG) ; sans le réglage, ignoré.
/// </summary>
public class DebugTests
{
    static readonly CompilerOptions On = new(Debug: true);

    static string Run(string source, string keys, byte width = 40)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, On);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, On);
        var cpu = new MiniZ80 { WindowWidth = width };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in keys)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    [Theory]
    [InlineData("10 DEBUG")]
    [InlineData("10 A=1:DEBUG:PRINT A")]
    [InlineData("10 IF A=0 THEN DEBUG ELSE PRINT")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Theory]
    [InlineData("10 DEBUG 5")]
    [InlineData("10 DEBUG=5")]              // nouveau mot réservé
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }

    [Fact]
    public void Sans_mise_au_point_DEBUG_n_ajoute_rien()
    {
        byte[] with = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 A=1:DEBUG:PRINT A").Program, 0x4000).Code;
        byte[] without = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 A=1:PRINT A").Program, 0x4000).Code;

        Assert.Equal(without, with);
    }

    [Fact]
    public void DEBUG_affiche_les_variables_puis_le_programme_continue()
    {
        string output = Run("10 A=5:B$=\"OK\":X!=1.5:DIM T(10):T(1)=2\n20 DEBUG\n30 PRINT \"FIN\"", " ");

        Assert.Contains("DEBUG ligne 20 ", output);
        Assert.Contains("A= 5 ", output);
        Assert.Contains("B$=\"OK\"", output);
        Assert.Contains("X!= 1.5 ", output);
        Assert.Contains("T() : 11 cases", output);
        Assert.Contains("ESPACE:suite S:stop", output);
        Assert.EndsWith("FIN\r\n", output);
        Assert.True(output.IndexOf("A= 5") < output.IndexOf("B$=") && output.IndexOf("B$=") < output.IndexOf("T()")
                    && output.IndexOf("T()") < output.IndexOf("X!="));   // ordre alphabétique
    }

    [Fact]
    public void S_arrete_tous_les_DEBUG_suivants()
    {
        // Une seule touche : s'il y avait un deuxième DEBUG, le mini-Z80 attendrait une touche qui n'existe pas
        string output = Run("10 FOR I=1 TO 3\n20 DEBUG\n30 NEXT\n40 PRINT \"FIN\"", "S");

        Assert.Single(Regex.Matches(output, "DEBUG ligne"));
        Assert.EndsWith("FIN\r\n", output);
    }

    [Fact]
    public void Les_autres_touches_sont_ignorees()
    {
        string output = Run("10 A=1\n20 DEBUG\n30 PRINT \"FIN\"", "xq ");

        Assert.EndsWith("FIN\r\n", output);
    }

    // V1=1 à V150=150, dix par ligne (lignes 10 à 150), puis DEBUG en ligne 1000
    static string ManyVariables() =>
        string.Join("\n", Enumerable.Range(0, 15).Select(l => $"{10 * (l + 1)} " +
            string.Join(":", Enumerable.Range(10 * l + 1, 10).Select(i => $"V{i}={i}"))))
        + "\n1000 DEBUG\n1010 PRINT \"FIN\"";

    [Fact]
    public void Beaucoup_de_variables_un_arret_a_chaque_ecran_plein()
    {
        // 150 variables, 3 par ligne dans une fenêtre de 20 colonnes : plus de deux écrans de 25 lignes
        string source = ManyVariables();
        string output = Run(source, "        ", 20);

        Assert.True(Regex.Matches(output, "ESPACE:suite S:stop").Count >= 3, output);
        Assert.Contains("V150= 150 ", output);
        Assert.EndsWith("FIN\r\n", output);
    }

    [Fact]
    public void S_pendant_un_ecran_plein_arrete_la_liste()
    {
        string source = ManyVariables();
        string output = Run(source, "S", 20);

        Assert.DoesNotContain("V99=", output);          // la liste s'arrête au premier écran
        Assert.EndsWith("FIN\r\n", output);
    }

    [Fact]
    public void Les_valeurs_d_une_fonction_DEF_FN_ne_sont_pas_montrees()
    {
        string output = Run("10 DEF FNDOUBLE(X)=X*2:K=FNDOUBLE(3)\n20 DEBUG", " ");

        Assert.Contains("K= 6 ", output);
        Assert.DoesNotContain("FNDOUBLE", output);
    }
}
