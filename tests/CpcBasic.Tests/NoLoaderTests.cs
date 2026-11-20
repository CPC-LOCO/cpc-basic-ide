// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D168 : programme BASIC compilé sans chargeur : NOM.BIN seul sur la disquette, et au départ le programme rebranche
/// la disquette (KL ROM WALK) si le firmware vient d'être remis à zéro.
/// </summary>
public class NoLoaderTests
{
    static byte[] Code(bool withLoader) =>
        CodeGenerator.Generate(BasicAnalyzer.Analyze("10 PRINT \"A\"").Program, Compiler.LoadAddress,
                               new CompilerOptions(WithLoader: withLoader)).Code;

    [Fact]
    public void Avec_chargeur_la_disquette_a_les_deux_fichiers()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"A\"", "jeu");

        Assert.Equal(new[] { "JEU.BAS", "JEU.BIN" }, result.Disk!.FileNames);
    }

    [Fact]
    public void Sans_chargeur_la_disquette_n_a_que_le_programme()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"A\"", "jeu", new CompilerOptions(WithLoader: false));

        Assert.Equal(new[] { "JEU.BIN" }, result.Disk!.FileNames);
    }

    [Fact]
    public void Sans_chargeur_le_programme_rebranche_la_disquette_au_depart()
    {
        byte[] code = Code(withLoader: false);

        // LD A,(&BC77) / CP &CF / JR NZ,+9 / LD DE,&0040 / LD HL,&ABFF / CALL &BCCB
        Assert.Equal(new byte[] { 0x3A, 0x77, 0xBC, 0xFE, 0xCF, 0x20, 0x09, 0x11, 0x40, 0x00, 0x21, 0xFF, 0xAB, 0xCD, 0xCB, 0xBC },
                     code[..16]);
    }

    [Fact]
    public void Deja_branchee_la_disquette_n_est_pas_rebranchee()
    {
        byte[] code = Code(withLoader: false);
        var cpu = new MiniZ80();
        cpu.Load(code, Compiler.LoadAddress);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Memory[0xBC77] = 0xDF;                          // AMSDOS déjà en place (programme lancé par RUN "nom")
        cpu.Call(Compiler.LoadAddress);

        Assert.Equal("A\r\n", cpu.Output.ToString());
    }

    [Fact]
    public void Avec_chargeur_rien_n_est_ajoute_au_depart()
    {
        Assert.Equal(Code(withLoader: false).Length - 16, Code(withLoader: true).Length);
    }
}
