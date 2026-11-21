// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using Xunit;

namespace CpcBasic.Tests;

/// <summary>D176 : le Z80 complet de MiniZ80, sur quelques instructions et indicateurs connus.</summary>
public class Z80CoreTests
{
    static MiniZ80 Run(params byte[] code)
    {
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Load([.. code, 0xC9], 0x4000);                       // ... RET
        cpu.Call(0x4000);
        return cpu;
    }

    [Fact]
    public void ADD_donne_tous_les_indicateurs()
    {
        MiniZ80 cpu = Run(0x3E, 0x7F, 0xC6, 0x01);                // LD A,&7F : ADD A,1 -> &80, débordement
        Assert.Equal(0x80, cpu.A);
        Assert.Equal(0x80 | 0x10 | 0x04, cpu.F);                 // S, H, P/V

        cpu = Run(0x3E, 0xFF, 0xC6, 0x01);                       // &FF + 1 -> 0, retenue
        Assert.Equal(0, cpu.A);
        Assert.Equal(0x40 | 0x10 | 0x01, cpu.F);                 // Z, H, C
    }

    [Fact]
    public void DAA_corrige_une_addition_en_BCD()
    {
        MiniZ80 cpu = Run(0x3E, 0x19, 0xC6, 0x28, 0x27);          // &19 + &28 = &41, DAA -> &47
        Assert.Equal(0x47, cpu.A);
    }

    [Fact]
    public void SBC_HL_DE_et_le_debordement()
    {
        // LD HL,&8000 : LD DE,1 : OR A : SBC HL,DE -> &7FFF, débordement signé
        MiniZ80 cpu = Run(0x21, 0x00, 0x80, 0x11, 0x01, 0x00, 0xB7, 0xED, 0x52);
        Assert.Equal(0x7FFF, cpu.HL);
        Assert.True(cpu.OverflowFlag);
        Assert.False(cpu.CarryFlag);
    }

    [Fact]
    public void IX_IY_et_leurs_moities()
    {
        // LD IX,&1234 : LD IY,&5678 : LD A,IXH (DD 7C) : ADD A,IYL (FD 85) -> &12 + &78 = &8A
        MiniZ80 cpu = Run(0xDD, 0x21, 0x34, 0x12, 0xFD, 0x21, 0x78, 0x56, 0xDD, 0x7C, 0xFD, 0x85);
        Assert.Equal(0x8A, cpu.A);
    }

    [Fact]
    public void LD_IX_d_n_puis_SET_dans_IX_d()
    {
        // LD IX,&5000 : LD (IX+3),&10 : SET 0,(IX+3) : LD B,(IX+3)
        MiniZ80 cpu = Run(0xDD, 0x21, 0x00, 0x50, 0xDD, 0x36, 0x03, 0x10, 0xDD, 0xCB, 0x03, 0xC6, 0xDD, 0x46, 0x03);
        Assert.Equal(0x11, cpu.B);
        Assert.Equal(0x11, cpu.Memory[0x5003]);
    }

    [Fact]
    public void EXX_et_EX_AF_AF()
    {
        // LD BC,&1111 : EXX : LD BC,&2222 : EXX -> BC = &1111 ; LD A,5 : EX AF,AF' : LD A,6 : EX AF,AF' -> A = 5
        MiniZ80 cpu = Run(0x01, 0x11, 0x11, 0xD9, 0x01, 0x22, 0x22, 0xD9, 0x3E, 0x05, 0x08, 0x3E, 0x06, 0x08);
        Assert.Equal(0x1111, cpu.BC);
        Assert.Equal(5, cpu.A);
    }

    [Fact]
    public void CPIR_trouve_un_octet()
    {
        // &5000 : 1, 2, 3, 4 ; LD HL,&5000 : LD BC,4 : LD A,3 : CPIR -> trouvé, HL juste après
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Memory[0x5000] = 1;
        cpu.Memory[0x5001] = 2;
        cpu.Memory[0x5002] = 3;
        cpu.Memory[0x5003] = 4;
        cpu.Load([0x21, 0x00, 0x50, 0x01, 0x04, 0x00, 0x3E, 0x03, 0xED, 0xB1, 0xC9], 0x4000);
        cpu.Call(0x4000);
        Assert.True(cpu.ZeroFlag);
        Assert.Equal(0x5003, cpu.HL);
        Assert.Equal(1, cpu.BC);
    }

    [Fact]
    public void RLD_et_NEG()
    {
        // &5000 = &34 ; LD HL,&5000 : LD A,&12 : RLD -> A = &13, (HL) = &42 ; NEG -> &ED
        var cpu = new MiniZ80 { UpperRomEnabled = false };
        cpu.Memory[0x5000] = 0x34;
        cpu.Load([0x21, 0x00, 0x50, 0x3E, 0x12, 0xED, 0x6F, 0xED, 0x44, 0xC9], 0x4000);
        cpu.Call(0x4000);
        Assert.Equal(0x42, cpu.Memory[0x5000]);
        Assert.Equal(0xED, cpu.A);
        Assert.True(cpu.CarryFlag);
    }
}
