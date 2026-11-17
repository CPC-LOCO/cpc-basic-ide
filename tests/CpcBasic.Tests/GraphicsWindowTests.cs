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
/// Lot L3 (D104) : ORIGIN avec fenêtre graphique, PEN avec le mode transparent. Référence : vrai BASIC dans WinAPE
/// (essais\refl3.dsk, 17 nov. 2026) ; examples\fenetreg.bas.
/// </summary>
public class GraphicsWindowTests
{
    static MiniZ80 Run(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80 { WindowWidth = 80 };
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    [Fact]
    public void ORIGIN_avec_six_valeurs_regle_la_fenetre_graphique()
    {
        // GRA WIN WIDTH (&BBCF) : DE = gauche, HL = droite ; GRA WIN HEIGHT (&BBD2) : DE = haut, HL = bas
        MiniZ80 cpu = Run("10 G=100:ORIGIN 0,0,G,300,300,G");

        Assert.Equal(new[] { (0xBBCF, 100, 300), (0xBBD2, 300, 100) },
                     cpu.RegisterCalls.Select(c => (c.Address, (int)c.DE, (int)c.HL)).ToArray());
    }

    [Fact]
    public void ORIGIN_avec_deux_valeurs_garde_la_fenetre()
    {
        // WinAPE : après ORIGIN 320,200, la fenêtre de ORIGIN 0,0,100,300,300,100 reste en place
        Assert.Empty(Run("10 ORIGIN 320,200").RegisterCalls);
    }

    [Fact]
    public void PEN_transparent_puis_opaque()
    {
        // TXT SET BACK (&BB9F) : A = 1 transparent, 0 opaque
        MiniZ80 cpu = Run("10 PEN 3,1:PRINT \"O\":PEN 3,0:PEN 1");

        Assert.Equal(new[] { (0xBB9F, 1), (0xBB9F, 0) }, cpu.RegisterCalls.Select(c => (c.Address, (int)c.A)).ToArray());
    }

    [Theory]
    [InlineData("10 ORIGIN 1,2,3")]
    [InlineData("10 ORIGIN 1,2,3,4,5")]
    [InlineData("10 ORIGIN 1,2,3,4,5,6,7")]
    [InlineData("10 PEN 1,2")]
    [InlineData("10 PEN 1,0,1")]
    public void Ecritures_refusees(string source)
    {
        Assert.NotEmpty(BasicAnalyzer.Analyze(source).Diagnostics);
    }
}
