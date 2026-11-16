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
/// D078 : FOR avec une limite et un pas calculés, et test au début de la boucle. Les écrans attendus viennent du même
/// programme tapé dans le vrai BASIC (SugarBox, 16 nov. 2026) : voir examples\boucle2.bas.
/// </summary>
public class ForTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static string Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000, new CompilerOptions(TargetMachine.AllCpc));
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu.Output.ToString();
    }

    // ---------- Écrans du vrai BASIC ----------

    [Theory]
    [InlineData("N=5:FOR I=1 TO N:PRINT I;:NEXT:PRINT \"/\";I", " 1  2  3  4  5 / 6 ")]
    [InlineData("FOR I=10 TO 1 STEP -3:PRINT I;:NEXT:PRINT \"/\";I", " 10  7  4  1 /-2 ")]
    [InlineData("S=2:FOR I=1 TO 7 STEP S:PRINT I;:NEXT:PRINT \"/\";I", " 1  3  5  7 / 9 ")]
    [InlineData("FOR I=5 TO 1:PRINT \"JAMAIS\";:NEXT:PRINT \"/\";I", "/ 5 ")]
    [InlineData("N=3:FOR I=1 TO N:N=10:PRINT I;:NEXT:PRINT \"/\";I", " 1  2  3 / 4 ")]
    [InlineData("FOR I=1 TO 3:PRINT I;:I=I+1:NEXT:PRINT \"/\";I", " 1  3 / 5 ")]
    [InlineData("FOR X!=0 TO 1 STEP 0.25:PRINT X!;:NEXT:PRINT \"/\";X!", " 0  0.25  0.5  0.75  1 / 1.25 ")]
    public void Comme_le_vrai_BASIC(string line, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 " + line));
    }

    // ---------- Autres cas ----------

    [Theory]
    [InlineData("S=-2:FOR I=10 TO 5 STEP S:PRINT I;:NEXT:PRINT \"/\";I", " 10  8  6 / 4 ")]       // pas calculé négatif
    [InlineData("A=4:FOR I=A TO 2:PRINT I;:NEXT:PRINT \"/\";I", "/ 4 ")]                      // départ calculé, déjà fini
    [InlineData("A=1:FOR I=A TO 2:PRINT I;:NEXT:PRINT \"/\";I", " 1  2 / 3 ")]
    [InlineData("N=0:FOR I=1 TO N:PRINT I;:NEXT:PRINT \"/\";I", "/ 1 ")]                      // limite calculée, déjà fini
    [InlineData("S=-1:FOR I=1 TO 3 STEP S:PRINT I;:NEXT:PRINT \"/\";I", "/ 1 ")]              // pas négatif, déjà fini
    [InlineData("FOR I=1 TO 2:FOR J=I TO 2:PRINT J;:NEXT:NEXT:PRINT", " 1  2  2 ")]                  // boucles imbriquées
    [InlineData("L!=1:FOR X!=0 TO L! STEP 0.5:PRINT X!;:NEXT:PRINT", " 0  0.5  1 ")]               // limite à virgule calculée
    [InlineData("P!=-0.5:FOR X!=1 TO 0 STEP P!:PRINT X!;:NEXT:PRINT", " 1  0.5  0 ")]              // pas à virgule calculé négatif
    [InlineData("FOR X!=2 TO 1:PRINT X!;:NEXT:PRINT \"/\";X!", "/ 2 ")]                       // à virgule, déjà fini
    [InlineData("T=3:FOR I=1 TO T*2 STEP T-1:PRINT I;:NEXT:PRINT", " 1  3  5 ")]                   // calculs
    public void Autres_boucles(string line, string shown)
    {
        Assert.Equal(shown + "\r\n", Run("10 " + line));
    }

    [Fact]
    public void Boucle_sur_plusieurs_lignes_avec_EXIT()
    {
        Assert.Equal(" 1  2 \r\n", Run("10 N=9\n20 FOR I=1 TO N\n30 IF I=3 THEN EXIT FOR\n40 PRINT I;\n50 NEXT\n60 PRINT"));
    }

    [Fact]
    public void Pas_zero_ecrit_reste_refuse()
    {
        Assert.Contains(Errors("10 FOR I = 1 TO 5 STEP 0\n20 NEXT"), e => e.Code == DiagnosticCode.StepIsZero);
    }

    [Fact]
    public void FOR_d_un_octet_avec_limite_calculee()
    {
        Assert.Equal(" 1  2  3 \r\n", Run("10 BYTE B\n20 N = 3\n30 FOR B = 1 TO N : PRINT B; : NEXT\n40 PRINT"));
    }
}
