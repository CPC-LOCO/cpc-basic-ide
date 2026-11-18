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
/// Lot K2 (D089) : |NOM, valeurs et CALL adresse, valeurs, avec la règle du firmware : A = nombre de valeurs, IX = la
/// liste (2 octets chacune, la dernière en premier).
/// </summary>
public class RsxTests
{
    // Routine d'essai en &9000 : range A en &9100, puis les deux premières valeurs de la liste (IX+0, IX+2) en &9102 et &9104
    static readonly byte[] Probe =
    [
        0x32, 0x00, 0x91,             // LD (&9100),A
        0xDD, 0x6E, 0x00,             // LD L,(IX+0)
        0xDD, 0x66, 0x01,             // LD H,(IX+1)
        0x22, 0x02, 0x91,             // LD (&9102),HL
        0xDD, 0x6E, 0x02,             // LD L,(IX+2)
        0xDD, 0x66, 0x03,             // LD H,(IX+3)
        0x22, 0x04, 0x91,             // LD (&9104),HL
        0xEB,                         // EX DE,HL
        0x22, 0x06, 0x91,             // LD (&9106),HL : DE à l'entrée
        0xC9                          // RET
    ];

    static IReadOnlyList<Diagnostic> Errors(string source) => BasicAnalyzer.Analyze(source).Diagnostics;

    static (string Output, MiniZ80 Cpu) Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Load(Probe, 0x9000);
        cpu.RsxCommands["TEST"] = 0x9000;
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return (cpu.Output.ToString(), cpu);
    }

    static int Word(MiniZ80 cpu, int at) => cpu.Memory[at] | (cpu.Memory[at + 1] << 8);

    [Theory]
    [InlineData("10 |TEST,5,7:PRINT \"FIN\"")]
    [InlineData("10 CALL &9000,5,7:PRINT \"FIN\"")]
    [InlineData("10 A=5:|TEST,A,A+2:PRINT \"FIN\"")]
    public void La_derniere_valeur_est_en_premier(string source)
    {
        (string output, MiniZ80 cpu) = Run(source);

        Assert.Equal("FIN\r\n", output);                 // la pile est remise en ordre au retour
        Assert.Equal(2, cpu.Memory[0x9100]);              // A = nombre de valeurs
        Assert.Equal(7, Word(cpu, 0x9102));               // IX+0 : la dernière
        Assert.Equal(5, Word(cpu, 0x9104));               // IX+2 : la première
        Assert.Equal(7, Word(cpu, 0x9106));               // DE = la dernière aussi (|GPRINT, |GPEN la lisent là)
    }

    [Fact]
    public void Sans_valeur_A_vaut_0()
    {
        Assert.Equal(0, Run("10 POKE &9100,9:|TEST").Cpu.Memory[0x9100]);
    }

    [Fact]
    public void Un_texte_passe_l_adresse_de_sa_fiche()
    {
        (_, MiniZ80 cpu) = Run("10 A$=\"XYZ\":|TEST,@A$:|TEST,A$");

        Assert.Equal(3, cpu.Memory[Word(cpu, 0x9102)]);   // la fiche : longueur, puis adresse des lettres
    }

    [Theory]
    // D124 (règle de l'utilisateur) : un texte passe la même adresse avec ou sans @, sur tous les CPC (sur le 464, le
    // Locomotive exige @ ; sur le 6128 les deux marchent) ; avec CALL comme avec une commande RSX
    [InlineData("|TEST,@A$", "|TEST,A$")]
    [InlineData("CALL &9000,@A$", "CALL &9000,A$")]
    public void Un_texte_avec_ou_sans_arobase_passe_la_meme_adresse(string with, string without)
    {
        (string output, _) = Run("10 A$=\"XYZ\":" + with + ":B=PEEK(&9102)+256*PEEK(&9103)\n" +
                                 "20 " + without + ":C=PEEK(&9102)+256*PEEK(&9103)\n" +
                                 "30 PRINT B=C;B=@A$;PEEK(B)");

        Assert.Equal("-1 -1  3 \r\n", output);   // même adresse, celle de la fiche, dont le premier octet est la longueur
    }

    [Theory]
    // D124 : une variable de nombre passe sa valeur ; avec @, son adresse
    [InlineData("|TEST,A", " 1234 ")]
    [InlineData("CALL &9000,A", " 1234 ")]
    [InlineData("|TEST,@A", "-1 ")]
    [InlineData("CALL &9000,@A", "-1 ")]
    public void Un_nombre_passe_sa_valeur_ou_avec_arobase_son_adresse(string call, string expected)
    {
        (string output, _) = Run("10 A=1234:" + call + ":B=PEEK(&9102)+256*PEEK(&9103)\n" +
                                 "20 IF B=@A THEN PRINT -1 ELSE PRINT B");

        Assert.Equal(expected + "\r\n", output);
    }

    [Theory]
    [InlineData("10 |INCONNUE,1", "Unknown command in 10\r\n")]
    [InlineData("10 ON ERROR GOTO 100\n20 |INCONNUE\n30 END\n100 PRINT ERR", " 28 \r\n")]
    public void Commande_inconnue(string source, string expected)
    {
        Assert.Equal(expected, Run(source).Output);
    }

    [Theory]
    [InlineData("10 |")]
    [InlineData("10 | 5")]
    [InlineData("10 |TEST,")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(Errors(source));
    }

    [Theory]
    [InlineData("10 |DISC")]
    [InlineData("10 |ERA,\"X.BAK\"")]
    [InlineData("10 a$=\"AB\":|GPRINT,@a$:|GPAS,-6,8")]
    [InlineData("10 IF 1 THEN |A ELSE |B")]
    // D129 : commandes AMSDOS essayées dans WinAPE (examples\amsdos.bas, examples\amsdos2.bas)
    [InlineData("10 |DIR,\"*.DAT\":|REN,\"AUTRE.DAT\",\"ESSAI.DAT\":|USER,1")]
    [InlineData("10 F$=\"A.DAT\":N$=\"B.DAT\":D$=\"A\":U=0:|REN,N$,F$:|ERA,@N$:|DRIVE,D$:|USER,U")]
    [InlineData("10 |TAPE.OUT:|DISC.OUT:|TAPE.IN:|DISC.IN:|TAPE:|DISC:|A")]
    [InlineData("10 CALL &BD19")]
    public void Ecritures_acceptees(string source)
    {
        Assert.Empty(Errors(source));
    }
}
