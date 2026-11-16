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
/// Lot K8 (D096) : ERASE. Référence : vrai BASIC (SugarBox, 16 nov. 2026) ; examples\efface.bas.
/// </summary>
public class EraseTests
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
        // SugarBox : la place revient (FRE monte), U garde 33, T se recrée, N$(1) est vide, ERASE U deux fois :
        // « Improper argument in 90 »
        string[] lines = Run("10 DIM T(5),N$(2),U(3)\n20 T(1)=11:N$(1)=\"ABC\":U(1)=33\n30 PRINT FRE(0)\n40 ERASE T,N$\n" +
                             "50 PRINT FRE(0)\n60 PRINT U(1)\n70 DIM T(10):T(10)=7:PRINT T(10)\n80 PRINT N$(1);\"/\"\n" +
                             "90 ERASE U:ERASE U")
            .Split("\r\n");

        Assert.Equal(12 + 9, int.Parse(lines[1]) - int.Parse(lines[0]));   // T : 6 cases de 2 octets ; N$ : 3 fiches de 3
        Assert.Equal(new[] { " 33 ", " 7 ", "/", "Improper argument in 90", "" }, lines.Skip(2).ToArray());
    }

    [Fact]
    public void Les_textes_des_tableaux_qui_descendent_suivent_apres_le_menage()
    {
        string output = Run("10 G$=\"AA\"+\"BB\":G$=\"\"\n20 DIM A(3),B$(2),C$(1)\n" +
                            "30 B$(1)=\"UN\":C$(0)=\"DEUX\"+\"X\":C$(1)=\"TRO\"+\"IS\"\n40 ERASE A\n" +
                            "50 Q=FRE(\"\")\n60 PRINT B$(1);C$(0);C$(1)");

        Assert.Equal("UNDEUXXTROIS\r\n", output);
    }

    [Fact]
    public void Les_textes_d_un_tableau_efface_sont_jetes_au_menage()
    {
        string output = Run("10 DIM N$(2),M$(1)\n20 N$(1)=\"AB\"+\"C\":M$(1)=\"D\"+\"E\"\n30 ERASE N$\n" +
                            "40 Q=FRE(\"\")\n50 PRINT M$(1);N$(1);\"/\"\n60 X$=\"Z\"+\"Z\":PRINT M$(1);X$");

        Assert.Equal("DE/\r\nDEZZ\r\n", output);
    }

    [Fact]
    public void Tableaux_a_virgule_et_a_deux_dimensions()
    {
        Assert.Equal(" 5 \r\n 0 \r\n", Run("10 DIM R!(2,2),T(1)\n20 T(1)=5:R!(1,1)=1.5\n30 ERASE R!\n40 PRINT T(1)\n50 DIM R!(1,3):PRINT R!(1,3)"));
    }

    [Fact]
    public void Un_tableau_jamais_cree_ne_peut_pas_etre_efface()
    {
        Assert.Equal("Improper argument in 10\r\n", Run("10 ERASE Z"));
    }

    [Theory]
    [InlineData("10 ERASE")]
    [InlineData("10 ERASE 3")]
    [InlineData("10 ERASE T(1)")]
    [InlineData("10 ERASE T,")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
