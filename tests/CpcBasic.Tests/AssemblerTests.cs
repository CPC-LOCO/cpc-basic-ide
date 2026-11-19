// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Tests;

/// <summary>Assembleur Z80 des bibliothèques (D145).</summary>
public class AssemblerTests
{
    const int Origin = 0x4000;

    static string? NoExternal(string name) => null;

    static byte[] Asm(string source, Func<string, string?>? resolve = null, Action<Z80Emitter>? after = null)
    {
        var emitter = new Z80Emitter(Origin);
        var errors = Z80Assembler.Assemble(source, emitter, "t_", resolve ?? NoExternal);
        Assert.Empty(errors);
        after?.Invoke(emitter);
        return emitter.ToBytes();
    }

    static IReadOnlyList<AsmError> Errors(string source)
    {
        var emitter = new Z80Emitter(Origin);
        return Z80Assembler.Assemble(source, emitter, "t_", NoExternal);
    }

    static byte[] Hex(string text) => text.Split(' ').Select(h => Convert.ToByte(h, 16)).ToArray();

    // ---------- codage des instructions ----------

    [Theory]
    [InlineData("LD A,5", "3E 05")]
    [InlineData("LD B,C", "41")]
    [InlineData("ld e,(hl)", "5E")]
    [InlineData("LD (HL),&FF", "36 FF")]
    [InlineData("LD A,(IX+3)", "DD 7E 03")]
    [InlineData("LD (IY-2),7", "FD 36 FE 07")]
    [InlineData("LD (IX),A", "DD 77 00")]
    [InlineData("LD HL,1234", "21 D2 04")]
    [InlineData("LD IX,&C000", "DD 21 00 C0")]
    [InlineData("LD DE,(&4000)", "ED 5B 00 40")]
    [InlineData("LD HL,(&4000)", "2A 00 40")]
    [InlineData("LD (&4000),HL", "22 00 40")]
    [InlineData("LD (&4000),A", "32 00 40")]
    [InlineData("LD A,(DE)", "1A")]
    [InlineData("LD SP,HL", "F9")]
    [InlineData("LD A,I", "ED 57")]
    [InlineData("PUSH AF", "F5")]
    [InlineData("POP IX", "DD E1")]
    [InlineData("EX AF,AF'", "08")]
    [InlineData("EX AF,AF' ; échange", "08")]
    [InlineData("EX DE,HL", "EB")]
    [InlineData("EX HL,DE", "EB")]
    [InlineData("EX (SP),HL", "E3")]
    [InlineData("ADD A,B", "80")]
    [InlineData("SUB 3", "D6 03")]
    [InlineData("SUB A,3", "D6 03")]
    [InlineData("CP (HL)", "BE")]
    [InlineData("XOR A", "AF")]
    [InlineData("AND (IX+5)", "DD A6 05")]
    [InlineData("ADD HL,DE", "19")]
    [InlineData("SBC HL,BC", "ED 42")]
    [InlineData("ADC HL,SP", "ED 7A")]
    [InlineData("ADD IX,IX", "DD 29")]
    [InlineData("ADD IX,BC", "DD 09")]
    [InlineData("INC (IX+1)", "DD 34 01")]
    [InlineData("DEC SP", "3B")]
    [InlineData("INC IY", "FD 23")]
    [InlineData("INC A", "3C")]
    [InlineData("BIT 7,H", "CB 7C")]
    [InlineData("SET 0,(HL)", "CB C6")]
    [InlineData("RES 3,(IX+4)", "DD CB 04 9E")]
    [InlineData("SRL A", "CB 3F")]
    [InlineData("RL E", "CB 13")]
    [InlineData("JP (HL)", "E9")]
    [InlineData("JP (IX)", "DD E9")]
    [InlineData("JP &BB5A", "C3 5A BB")]
    [InlineData("CALL NZ,&1234", "C4 34 12")]
    [InlineData("CALL &BB5A", "CD 5A BB")]
    [InlineData("RET", "C9")]
    [InlineData("RET C", "D8")]
    [InlineData("RST &38", "FF")]
    [InlineData("RST 7", "FF")]
    [InlineData("IN A,(&F5)", "DB F5")]
    [InlineData("IN B,(C)", "ED 40")]
    [InlineData("OUT (C),A", "ED 79")]
    [InlineData("IM 1", "ED 56")]
    [InlineData("LDIR", "ED B0")]
    [InlineData("NEG", "ED 44")]
    [InlineData("DI", "F3")]
    [InlineData("DEFB 1,\"AB\",'C'", "01 41 42 43")]
    [InlineData("DB 255,-1", "FF FF")]
    [InlineData("DEFW &1234,2", "34 12 02 00")]
    [InlineData("DEFS 3,&AA", "AA AA AA")]
    [InlineData("LD A,%1010", "3E 0A")]
    [InlineData("LD A,#1F", "3E 1F")]
    [InlineData("LD A,(2+3)*4", "3E 14")]
    [InlineData("LD A,-1", "3E FF")]
    [InlineData("LD A,'Z'-'A'", "3E 19")]
    [InlineData("JR $", "18 FE")]
    public void Une_instruction_donne_les_bons_octets(string instruction, string expected)
    {
        Assert.Equal(Hex(expected), Asm("    " + instruction));
    }

    // ---------- étiquettes ----------

    [Fact]
    public void Etiquettes_et_decalages_sont_calcules()
    {
        string source = """
            DEBUT  LD HL,TABLE+1
                   JR SUITE
            TABLE  DEFB 1,2,3
            SUITE  LD A,(HL)
                   RET
            """;
        byte[] code = Asm(source.Replace("            ", ""));

        Assert.Equal(Hex("21 06 40 18 03 01 02 03 7E C9"), code);
    }

    [Fact]
    public void Une_routine_assemblee_tourne()
    {
        string source = "\tLD A,0\n\tLD B,5\nboucle: ADD A,3\n\tDJNZ boucle\n\tLD (resultat),A\n\tRET\nresultat DEFB 0";
        byte[] code = Asm(source);

        var cpu = new MiniZ80();
        cpu.Load(code, Origin);
        cpu.Call(Origin);

        Assert.Equal(15, cpu.A);
        Assert.Equal(15, cpu.Memory[Origin + code.Length - 1]);
    }

    [Fact]
    public void Equ_donne_un_nom_a_un_nombre()
    {
        Assert.Equal(Hex("06 14"), Asm("N EQU 10\n    LD B,N*2"));
    }

    [Fact]
    public void Un_nom_exterieur_est_demande_et_note()
    {
        var emitter = new Z80Emitter(Origin);
        var externals = new HashSet<string>();
        var errors = Z80Assembler.Assemble("    CALL plot\n    RET", emitter, "t_",
                                           name => name == "PLOT" ? "lib_plot" : null, externals);
        emitter.Label("lib_plot");
        emitter.Bytes(0xC9);

        Assert.Empty(errors);
        Assert.Equal(Hex("CD 04 40 C9 C9"), emitter.ToBytes());
        Assert.Equal(new[] { "PLOT" }, externals);
    }

    [Fact]
    public void Deux_routines_peuvent_avoir_la_meme_etiquette()
    {
        var emitter = new Z80Emitter(Origin);
        Assert.Empty(Z80Assembler.Assemble("BOUCLE JR BOUCLE", emitter, "a_", NoExternal));
        Assert.Empty(Z80Assembler.Assemble("BOUCLE JR BOUCLE", emitter, "b_", NoExternal));

        Assert.Equal(Hex("18 FE 18 FE"), emitter.ToBytes());
    }

    // ---------- erreurs ----------

    [Theory]
    [InlineData("    ORG &4000", "ORG")]
    [InlineData("    LD A,TABLE\nTABLE DEFB 0", "octet")]
    [InlineData("    CALL INCONNU", "INCONNU")]
    [InlineData("    JR &4000", "relatif")]
    [InlineData("    LD (HL),(HL)", "n'existe pas")]
    [InlineData("    FOO A", "FOO")]
    [InlineData("X  NOP\nX  NOP", "déjà")]
    [InlineData("    LD A,300", "255")]
    [InlineData("    LD A,(IX+200)", "127")]
    [InlineData("    BIT 8,A", "0 à 7")]
    [InlineData("    RST &39", "RST")]
    public void Une_faute_donne_un_message_clair(string source, string word)
    {
        var errors = Errors(source);

        Assert.Single(errors);
        Assert.Contains(word, errors[0].Message);
    }

    [Fact]
    public void Chaque_ligne_fausse_est_signalee()
    {
        var errors = Errors("    ORG 0\n    NOP\n    LD A,B,C\n    RET");

        Assert.Equal(new[] { 1, 3 }, errors.Select(e => e.Line));
    }
}
