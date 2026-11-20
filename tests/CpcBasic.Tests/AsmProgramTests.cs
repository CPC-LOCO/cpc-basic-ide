// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>« Assembleur seul » : ORG, RUN, WRITE, routines des bibliothèques, disquette (D163).</summary>
public class AsmProgramTests
{
    static readonly LibraryCatalog Catalog = LibraryCatalog.Build(
    [
        new CatalogSource("CALCUL", "FOIS2.cmd.asm", ";@NOM FOIS2\n;@GENRE ROUTINE\n  ADD HL,HL\n  RET", false),
        new CatalogSource("CALCUL", "FOIS4.cmd.asm", ";@NOM FOIS4\n;@GENRE ROUTINE\n  CALL FOIS2\n  JP FOIS2", false)
    ]);

    static AsmProgramResult Build(string source) => AsmProgram.Build(source, Catalog);

    static byte[] Hex(string text) => text.Split(' ').Select(h => Convert.ToByte(h, 16)).ToArray();

    [Fact]
    public void ORG_donne_l_adresse_de_chargement_et_de_lancement()
    {
        AsmProgramResult program = Build("        ORG &8000\n        LD A,(DONNEE)\n        RET\nDONNEE: DEFB 7");

        Assert.Empty(program.Errors);
        Assert.Equal(0x8000, program.Origin);
        Assert.Equal(0x8000, program.Entry);
        Assert.Equal(Hex("3A 04 80 C9 07"), program.Code);       // l'étiquette DONNEE est en &8004
    }

    [Fact]
    public void RUN_donne_l_adresse_de_lancement()
    {
        AsmProgramResult program = Build("ORG #9000\nTABLE DEFS 3\nDEBUT LD A,5\n RET\n RUN DEBUT");

        Assert.Empty(program.Errors);
        Assert.Equal(0x9003, program.Entry);
    }

    [Fact]
    public void Une_ligne_WRITE_de_WinAPE_est_acceptee()
    {
        AsmProgramResult program = Build("org &4000\nrun &4000\nwrite direct \"a:jeu.bin\"\n ret");

        Assert.Empty(program.Errors);
        Assert.Equal(Hex("C9"), program.Code);
    }

    [Fact]
    public void Les_routines_des_bibliotheques_sont_ajoutees_apres_le_code()
    {
        AsmProgramResult program = Build(" ORG &8000\n LD HL,5\n CALL FOIS4\n RET");

        Assert.Empty(program.Errors);
        Assert.Equal(new[] { "FOIS2", "FOIS4" }, program.Routines.Select(r => r.Name));   // FOIS4 appelle FOIS2
        var cpu = new MiniZ80();
        cpu.Load(program.Code!, program.Origin);
        cpu.Call(program.Entry);
        Assert.Equal(20, cpu.H << 8 | cpu.L);
    }

    [Theory]
    [InlineData(" LD A,1\n RET", "ORG manque")]
    [InlineData(" ORG &8000\n ORG &9000\n RET", "un seul ORG")]
    [InlineData(" NOP\n ORG &8000\n RET", "avant la première instruction")]
    [InlineData(" ORG &0100\n RET", "&0400")]
    [InlineData(" ORG &C000\n RET", "&A67B")]
    [InlineData(" ORG &A670\n DEFS 20\n RET", "dépasse &A67B")]
    [InlineData(" ORG &8000\n RUN 1\n RUN 2\n RET", "RUN est déjà donné")]
    [InlineData(" ORG &8000\n CALL INCONNUE\n RET", "INCONNUE")]
    public void Une_faute_donne_un_message_clair(string source, string word)
    {
        AsmProgramResult program = Build(source);

        Assert.False(program.Success);
        Assert.Contains(program.Errors, e => e.Message.Contains(word));
    }

    [Fact]
    public void Dans_une_commande_de_bibliotheque_ORG_RUN_et_WRITE_restent_refuses()
    {
        foreach (string line in new[] { " ORG &8000", " RUN &8000", " WRITE \"A.BIN\"" })
            Assert.Single(Z80Assembler.Assemble(line, new Z80Emitter(0x4000), "t_", _ => null));
    }

    [Fact]
    public void La_disquette_a_le_programme_et_son_chargeur()
    {
        AsmProgramResult program = Build(" ORG &8000\n RET\n RUN &8000");

        var disk = AsmProgram.ToDisk(program, "jeu");

        Assert.Equal(new[] { "JEU.BAS", "JEU.BIN" }, disk.FileNames);
    }
}
