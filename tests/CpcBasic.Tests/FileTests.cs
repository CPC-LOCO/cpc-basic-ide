// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot J1 (D084) : OPENOUT, PRINT #9, WRITE #9, CLOSEOUT, OPENIN, INPUT #9, LINE INPUT #9, EOF, CLOSEIN.
/// Référence : vrai BASIC (SugarBox, 16 nov. 2026) ; examples\fichiers.bas.
/// </summary>
public class FileTests
{
    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static (string Output, MiniZ80 Cpu) Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    static readonly string Line1 = "BONJOUR" + new string(' ', 6) + " 12 " + new string(' ', 9) + " 3.5 ";

    [Fact]
    public void Ecrire_puis_relire_comme_le_vrai_BASIC()
    {
        string program = string.Join("\n",
            "20 OPENOUT \"TEST\"", "30 PRINT #9,\"BONJOUR\",12,3.5", "40 PRINT #9,\"FIN\";-7", "50 WRITE #9,\"A,B\",42",
            "60 CLOSEOUT", "70 OPENIN \"TEST\"", "80 WHILE NOT EOF", "90 LINE INPUT #9,L$", "100 PRINT \"[\";L$;\"]\"",
            "110 WEND", "120 CLOSEIN", "130 OPENIN \"TEST\":INPUT #9,A$,B,C:PRINT A$;B;C:CLOSEIN");

        (string output, MiniZ80 cpu) = Run(program);

        Assert.Equal(Line1 + "\r\nFIN-7 \r\n\"A,B\",42\r\n", Encoding.ASCII.GetString(cpu.Files["TEST"].ToArray()));
        Assert.Equal("[" + Line1 + "]\r\n[FIN-7 ]\r\n[\"A,B\",42]\r\n" + Line1 + " 0  0 \r\n", output);
    }

    [Fact]
    public void Nombres_separes_par_des_virgules_ou_des_espaces()
    {
        (string output, _) = Run("10 OPENOUT \"N\":PRINT #9,1;-2;3:WRITE #9,4,\"X\",5.5:CLOSEOUT\n" +
                                 "20 OPENIN \"N\":INPUT #9,A,B,C,D,E$,F!:CLOSEIN:PRINT A;B;C;D;E$;F!");

        Assert.Equal(" 1 -2  3  4 X 5.5 \r\n", output);
    }

    [Fact]
    public void Tableaux_et_lignes_entieres()
    {
        (string output, _) = Run("10 DIM T(2),N$(2)\n20 OPENOUT \"T\":FOR I=0 TO 2:PRINT #9,I*10:NEXT:PRINT #9,\"UN, DEUX\":CLOSEOUT\n" +
                                 "30 OPENIN \"T\":FOR I=0 TO 2:INPUT #9,T(I):NEXT:LINE INPUT #9,N$(1):CLOSEIN\n" +
                                 "40 PRINT T(0);T(1);T(2);N$(1)");

        Assert.Equal(" 0  10  20 UN, DEUX\r\n", output);
    }

    [Fact]
    public void L_ecran_revient_apres_PRINT_9()
    {
        (string output, MiniZ80 cpu) = Run("10 OPENOUT \"F\":PRINT #9,\"DANS\":PRINT \"ECRAN\":CLOSEOUT");

        Assert.Equal("ECRAN\r\n", output);
        Assert.Equal("DANS\r\n", Encoding.ASCII.GetString(cpu.Files["F"].ToArray()));
    }

    [Theory]
    [InlineData("10 INPUT #9,A$", "File not open in 10\r\n")]
    [InlineData("10 PRINT #9,\"X\"", "File not open in 10\r\n")]
    [InlineData("10 OPENOUT \"T2\":PRINT #9,\"X\":CLOSEOUT:OPENIN \"T2\":INPUT #9,A$,B$", "EOF met in 10\r\n")]
    [InlineData("10 OPENOUT \"T3\":OPENOUT \"T4\"", "File already open in 10\r\n")]
    [InlineData("10 OPENIN \"ABSENT\"\n20 PRINT \"X\"", "")]
    [InlineData("10 ON ERROR GOTO 100\n20 OPENIN \"ABSENT\"\n30 END\n100 PRINT ERR", " 32 \r\n")]
    [InlineData("10 PRINT EOF", "-1 \r\n")]
    public void Erreurs_comme_le_vrai_BASIC(string source, string expected)
    {
        Assert.Equal(expected, Run(source).Output);
    }

    [Theory]
    [InlineData("10 OPENOUT 5")]
    [InlineData("10 CLS #9")]
    [InlineData("10 INPUT #8,A")]          // #8 (imprimante) : seulement PRINT, WRITE et POS (D100)
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 N$=\"SCORES\":OPENOUT N$:CLOSEOUT")]
    [InlineData("10 OPENIN \"A\"+\"B\":WHILE NOT EOF:LINE INPUT #9,L$:WEND:CLOSEIN")]
    [InlineData("10 IF EOF THEN PRINT 1")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }

    [Fact]
    public void Sans_fichier_rien_n_est_ajoute()
    {
        GeneratedProgram program = CodeGenerator.Generate(BasicAnalyzer.Analyze("10 PRINT #1,\"A\"").Program, 0x4000);

        Assert.DoesNotContain(program.Modules, m => m.StartsWith("File"));
    }
}
