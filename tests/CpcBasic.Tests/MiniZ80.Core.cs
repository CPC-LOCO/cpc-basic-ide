// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Tests;

/// <summary>
/// Le Z80 complet de MiniZ80 (idée de l'utilisateur, D176) : toutes les instructions, avec les préfixes CB, ED, DD, FD,
/// DDCB et FDCB (et les instructions non documentées : IXH, IXL, IYH, IYL, SLL, DDCB qui recopie dans un registre),
/// et tous les indicateurs (S, Z, H, P/V, N, C, et les bits 3 et 5). Les appels au firmware restent simulés par
/// <c>CallTo</c> et <c>JumpTo</c> (MiniZ80.cs). Les répétitions (LDIR, CPIR...) se font en une seule étape.
/// </summary>
internal sealed partial class MiniZ80
{
    const byte SF = 0x80, ZF = 0x40, YF = 0x20, HF = 0x10, XF = 0x08, PF = 0x04, NF = 0x02, CF = 0x01;

    /// <summary>Registre des indicateurs du Z80.</summary>
    public byte F;

    /// <summary>Registres de rechange (EX AF,AF' et EXX), registres I et R, mode d'interruption.</summary>
    public byte A2, F2, B2, C2, D2, E2, H2, L2, I, R, InterruptMode;
    public ushort IY;

    public bool CarryFlag
    {
        get => (F & CF) != 0;
        set => F = (byte)(value ? F | CF : F & ~CF);
    }

    public bool ZeroFlag
    {
        get => (F & ZF) != 0;
        set => F = (byte)(value ? F | ZF : F & ~ZF);
    }

    /// <summary>Indicateur P/V : débordement signé ou parité.</summary>
    public bool OverflowFlag
    {
        get => (F & PF) != 0;
        set => F = (byte)(value ? F | PF : F & ~PF);
    }

    bool SignFlag => (F & SF) != 0;

    int prefix;                 // 0 : HL ; 1 : IX (DD) ; 2 : IY (FD)
    int indexAddress = -1;      // (IX+d) déjà calculé pour cette instruction

    static bool EvenParity(int v)
    {
        v &= 0xFF;
        v ^= v >> 4;
        v ^= v >> 2;
        v ^= v >> 1;
        return (v & 1) == 0;
    }

    static byte Szxy(int v) => (byte)((v & (SF | YF | XF)) | ((v & 0xFF) == 0 ? ZF : 0));
    static byte Szxyp(int v) => (byte)(Szxy(v) | (EvenParity(v) ? PF : 0));

    // ----- mémoire

    ushort Read16(int address) =>
        (ushort)(ReadMemory(address & 0xFFFF) | (ReadMemory((address + 1) & 0xFFFF) << 8));

    void Write16(int address, ushort value)
    {
        Memory[address & 0xFFFF] = (byte)value;
        Memory[(address + 1) & 0xFFFF] = (byte)(value >> 8);
    }

    // ----- HL, IX ou IY selon le préfixe

    ushort HLX
    {
        get => prefix == 1 ? IX : prefix == 2 ? IY : HL;
        set
        {
            if (prefix == 1)
                IX = value;
            else if (prefix == 2)
                IY = value;
            else
                HL = value;
        }
    }

    // (HL), ou (IX+d) : le décalage d est lu une seule fois, juste après le code de l'instruction
    int MemoryAddress()
    {
        if (prefix == 0)
            return HL;
        if (indexAddress < 0)
            indexAddress = (HLX + (sbyte)Fetch()) & 0xFFFF;
        return indexAddress;
    }

    // registre numéro r (B, C, D, E, H, L, (HL), A) ; avec un préfixe, H et L deviennent IXH et IXL, sauf si
    // l'instruction se sert aussi de (IX+d) (plain = vrai)
    byte GetR(int r, bool plain = false) => r switch
    {
        0 => B,
        1 => C,
        2 => D,
        3 => E,
        4 => prefix == 0 || plain ? H : (byte)(HLX >> 8),
        5 => prefix == 0 || plain ? L : (byte)HLX,
        6 => ReadMemory(MemoryAddress()),
        _ => A
    };

    void SetR(int r, byte value, bool plain = false)
    {
        switch (r)
        {
            case 0: B = value; break;
            case 1: C = value; break;
            case 2: D = value; break;
            case 3: E = value; break;
            case 4:
                if (prefix == 0 || plain) H = value;
                else HLX = (ushort)((HLX & 0x00FF) | (value << 8));
                break;
            case 5:
                if (prefix == 0 || plain) L = value;
                else HLX = (ushort)((HLX & 0xFF00) | value);
                break;
            case 6: Memory[MemoryAddress()] = value; break;
            default: A = value; break;
        }
    }

    ushort GetRp(int p) => p switch { 0 => BC, 1 => DE, 2 => HLX, _ => (ushort)sp };

    void SetRp(int p, ushort value)
    {
        switch (p)
        {
            case 0: BC = value; break;
            case 1: DE = value; break;
            case 2: HLX = value; break;
            default: sp = value; break;
        }
    }

    ushort GetRp2(int p) => p == 3 ? (ushort)((A << 8) | F) : GetRp(p);

    void SetRp2(int p, ushort value)
    {
        if (p == 3)
        {
            A = (byte)(value >> 8);
            F = (byte)value;
        }
        else
            SetRp(p, value);
    }

    bool Condition(int y) => y switch
    {
        0 => !ZeroFlag,
        1 => ZeroFlag,
        2 => !CarryFlag,
        3 => CarryFlag,
        4 => !OverflowFlag,             // PO
        5 => OverflowFlag,              // PE
        6 => !SignFlag,                 // P
        _ => SignFlag                   // M
    };

    // ----- calculs sur 8 bits

    void Alu(int operation, byte v)
    {
        switch (operation)
        {
            case 0: AddA(v, 0); break;                              // ADD
            case 1: AddA(v, CarryFlag ? 1 : 0); break;              // ADC
            case 2: SubA(v, 0, true); break;                        // SUB
            case 3: SubA(v, CarryFlag ? 1 : 0, true); break;        // SBC
            case 4: A &= v; F = (byte)(Szxyp(A) | HF); break;       // AND
            case 5: A ^= v; F = Szxyp(A); break;                    // XOR
            case 6: A |= v; F = Szxyp(A); break;                    // OR
            default: SubA(v, 0, false); break;                      // CP
        }
    }

    void AddA(byte v, int carry)
    {
        int r = A + v + carry;
        F = (byte)(Szxy(r) | ((A ^ v ^ r) & HF) | (((A ^ ~v) & (A ^ r) & 0x80) != 0 ? PF : 0) | (r > 0xFF ? CF : 0));
        A = (byte)r;
    }

    void SubA(byte v, int carry, bool store)
    {
        int r = A - v - carry;
        byte flags = (byte)(((r & 0xFF) == 0 ? ZF : 0) | (r & SF) | NF | ((A ^ v ^ r) & HF)
                            | (((A ^ v) & (A ^ r) & 0x80) != 0 ? PF : 0) | (r < 0 ? CF : 0));
        // bits 3 et 5 : du résultat, sauf pour CP (de la valeur comparée)
        F = (byte)(flags | ((store ? r : v) & (YF | XF)));
        if (store)
            A = (byte)r;
    }

    byte Inc8(byte v)
    {
        byte r = (byte)(v + 1);
        F = (byte)((F & CF) | Szxy(r) | ((v & 0x0F) == 0x0F ? HF : 0) | (v == 0x7F ? PF : 0));
        return r;
    }

    byte Dec8(byte v)
    {
        byte r = (byte)(v - 1);
        F = (byte)((F & CF) | Szxy(r) | NF | ((v & 0x0F) == 0 ? HF : 0) | (v == 0x80 ? PF : 0));
        return r;
    }

    // ----- calculs sur 16 bits

    ushort Add16(ushort a, ushort b)
    {
        int r = a + b;
        F = (byte)((F & (SF | ZF | PF)) | (((a ^ b ^ r) >> 8) & HF) | ((r >> 8) & (YF | XF)) | (r > 0xFFFF ? CF : 0));
        return (ushort)r;
    }

    void Adc16(ushort v)
    {
        int hl = HL, r = hl + v + (CarryFlag ? 1 : 0);
        F = (byte)(((r >> 8) & (SF | YF | XF)) | ((r & 0xFFFF) == 0 ? ZF : 0) | (((hl ^ v ^ r) >> 8) & HF)
                   | (((hl ^ ~v) & (hl ^ r) & 0x8000) != 0 ? PF : 0) | (r > 0xFFFF ? CF : 0));
        HL = (ushort)r;
    }

    void Sbc16(ushort v)
    {
        int hl = HL, r = hl - v - (CarryFlag ? 1 : 0);
        F = (byte)(((r >> 8) & (SF | YF | XF)) | ((r & 0xFFFF) == 0 ? ZF : 0) | (((hl ^ v ^ r) >> 8) & HF) | NF
                   | (((hl ^ v) & (hl ^ r) & 0x8000) != 0 ? PF : 0) | (r < 0 ? CF : 0));
        HL = (ushort)r;
    }

    // ----- rotations et décalages (préfixe CB) : RLC, RRC, RL, RR, SLA, SRA, SLL, SRL

    byte Rotate(int y, byte v)
    {
        int c;
        int r;
        switch (y)
        {
            case 0: c = v >> 7; r = (v << 1) | c; break;
            case 1: c = v & 1; r = (v >> 1) | (c << 7); break;
            case 2: c = v >> 7; r = (v << 1) | (CarryFlag ? 1 : 0); break;
            case 3: c = v & 1; r = (v >> 1) | (CarryFlag ? 0x80 : 0); break;
            case 4: c = v >> 7; r = v << 1; break;
            case 5: c = v & 1; r = (v >> 1) | (v & 0x80); break;
            case 6: c = v >> 7; r = (v << 1) | 1; break;
            default: c = v & 1; r = v >> 1; break;
        }
        r &= 0xFF;
        F = (byte)(Szxyp(r) | (c != 0 ? CF : 0));
        return (byte)r;
    }

    void Bit(int y, byte v, int xy)
    {
        bool set = (v & (1 << y)) != 0;
        F = (byte)((F & CF) | HF | (set ? 0 : ZF | PF) | (y == 7 && set ? SF : 0) | (xy & (YF | XF)));
    }

    // ----- une instruction

    void Execute(int opcodeAddress)
    {
        prefix = 0;
        indexAddress = -1;
        R = (byte)((R & 0x80) | ((R + 1) & 0x7F));
        byte op = Fetch();
        while (op is 0xDD or 0xFD)
        {
            prefix = op == 0xDD ? 1 : 2;
            op = Fetch();
        }
        if (op == 0xCB)
        {
            if (prefix == 0)
                ExecuteCb();
            else
                ExecuteIndexCb();
            return;
        }
        if (op == 0xED)
        {
            prefix = 0;
            ExecuteEd();
            return;
        }
        ExecuteMain(op);
    }

    void ExecuteMain(byte op)
    {
        int x = op >> 6, y = (op >> 3) & 7, z = op & 7, p = y >> 1, q = y & 1;
        switch (x)
        {
            case 0:
                switch (z)
                {
                    case 0:
                        switch (y)
                        {
                            case 0: break;                                              // NOP
                            case 1:                                                     // EX AF,AF'
                                (A, A2) = (A2, A);
                                (F, F2) = (F2, F);
                                break;
                            case 2: B--; Jr(B != 0); break;                             // DJNZ
                            case 3: Jr(true); break;                                    // JR
                            default: Jr(Condition(y - 4)); break;                       // JR NZ / Z / NC / C
                        }
                        break;
                    case 1:
                        if (q == 0)
                            SetRp(p, Fetch16());                                        // LD rr,nn
                        else
                            HLX = Add16(HLX, GetRp(p));                                 // ADD HL,rr
                        break;
                    case 2:
                        switch (y)
                        {
                            case 0: Memory[BC] = A; break;                              // LD (BC),A
                            case 1: A = ReadMemory(BC); break;                          // LD A,(BC)
                            case 2: Memory[DE] = A; break;                              // LD (DE),A
                            case 3: A = ReadMemory(DE); break;                          // LD A,(DE)
                            case 4: Write16(Fetch16(), HLX); break;                     // LD (nn),HL
                            case 5: HLX = Read16(Fetch16()); break;                     // LD HL,(nn)
                            case 6: Memory[Fetch16()] = A; break;                       // LD (nn),A
                            default: A = ReadMemory(Fetch16()); break;                  // LD A,(nn)
                        }
                        break;
                    case 3:
                        SetRp(p, (ushort)(GetRp(p) + (q == 0 ? 1 : -1)));               // INC rr / DEC rr
                        break;
                    case 4:
                    {
                        if (y == 6)
                            MemoryAddress();
                        SetR(y, Inc8(GetR(y)));                                         // INC r
                        break;
                    }
                    case 5:
                    {
                        if (y == 6)
                            MemoryAddress();
                        SetR(y, Dec8(GetR(y)));                                         // DEC r
                        break;
                    }
                    case 6:
                    {
                        if (y == 6)
                            MemoryAddress();                                            // LD (IX+d),n : d avant n
                        SetR(y, Fetch());                                               // LD r,n
                        break;
                    }
                    default:
                        RotateA(y);
                        break;
                }
                break;

            case 1:
                if (y == 6 && z == 6)
                    break;                                                              // HALT : rien à attendre ici
                {
                    bool plain = y == 6 || z == 6;                                      // LD H,(IX+d) : le vrai H
                    SetR(y, GetR(z, plain), plain);                                     // LD r,r'
                }
                break;

            case 2:
                Alu(y, GetR(z));                                                        // ADD, ADC, SUB, SBC, AND, XOR, OR, CP
                break;

            default:
                switch (z)
                {
                    case 0:
                        if (Condition(y))
                            pc = Pop();                                                 // RET cc
                        break;
                    case 1:
                        if (q == 0)
                            SetRp2(p, Pop());                                           // POP rr
                        else
                            switch (p)
                            {
                                case 0: pc = Pop(); break;                              // RET
                                case 1:                                                 // EXX
                                    (B, B2) = (B2, B);
                                    (C, C2) = (C2, C);
                                    (D, D2) = (D2, D);
                                    (E, E2) = (E2, E);
                                    (H, H2) = (H2, H);
                                    (L, L2) = (L2, L);
                                    break;
                                case 2: pc = HLX; break;                                // JP (HL)
                                default: sp = HLX; break;                               // LD SP,HL
                            }
                        break;
                    case 2:
                    {
                        ushort target = Fetch16();
                        if (Condition(y))
                            pc = target;                                                // JP cc,nn
                        break;
                    }
                    case 3:
                        switch (y)
                        {
                            case 0: JumpTo(Fetch16()); break;                           // JP nn (firmware simulé)
                            case 2:                                                     // OUT (n),A
                            {
                                byte n = Fetch();
                                PortWrites.Add(((ushort)((A << 8) | n), A));
                                break;
                            }
                            case 3:                                                     // IN A,(n)
                            {
                                byte n = Fetch();
                                A = Ports.TryGetValue((ushort)((A << 8) | n), out byte read) ? read : (byte)0xFF;
                                break;
                            }
                            case 4:                                                     // EX (SP),HL
                            {
                                ushort top = (ushort)(Memory[sp & 0xFFFF] | (Memory[(sp + 1) & 0xFFFF] << 8));
                                Memory[sp & 0xFFFF] = (byte)HLX;
                                Memory[(sp + 1) & 0xFFFF] = (byte)(HLX >> 8);
                                HLX = top;
                                break;
                            }
                            case 5: (DE, HL) = (HL, DE); break;                         // EX DE,HL (jamais IX)
                            case 6: InterruptsEnabled = false; break;                   // DI
                            default: InterruptsEnabled = true; break;                   // EI
                        }
                        break;
                    case 4:
                    {
                        ushort target = Fetch16();
                        if (Condition(y))
                            CallTo(target);                                             // CALL cc,nn
                        break;
                    }
                    case 5:
                        if (q == 0)
                            Push(GetRp2(p));                                            // PUSH rr
                        else
                            CallTo(Fetch16());                                          // CALL nn (firmware simulé)
                        break;
                    case 6:
                        Alu(y, Fetch());                                                // ADD A,n ... CP n
                        break;
                    default:
                        Push((ushort)pc);                                               // RST
                        pc = y * 8;
                        break;
                }
                break;
        }
    }

    // RLCA, RRCA, RLA, RRA, DAA, CPL, SCF, CCF
    void RotateA(int y)
    {
        switch (y)
        {
            case 0:
            {
                int c = A >> 7;
                A = (byte)((A << 1) | c);
                F = (byte)((F & (SF | ZF | PF)) | (A & (YF | XF)) | c);
                break;
            }
            case 1:
            {
                int c = A & 1;
                A = (byte)((A >> 1) | (c << 7));
                F = (byte)((F & (SF | ZF | PF)) | (A & (YF | XF)) | c);
                break;
            }
            case 2:
            {
                int c = A >> 7;
                A = (byte)((A << 1) | (CarryFlag ? 1 : 0));
                F = (byte)((F & (SF | ZF | PF)) | (A & (YF | XF)) | c);
                break;
            }
            case 3:
            {
                int c = A & 1;
                A = (byte)((A >> 1) | (CarryFlag ? 0x80 : 0));
                F = (byte)((F & (SF | ZF | PF)) | (A & (YF | XF)) | c);
                break;
            }
            case 4:                                                                     // DAA
            {
                int a = A, correction = 0;
                bool carry = CarryFlag, subtract = (F & NF) != 0, half = (F & HF) != 0;
                if (half || (a & 0x0F) > 9)
                    correction |= 0x06;
                if (carry || a > 0x99)
                {
                    correction |= 0x60;
                    carry = true;
                }
                bool newHalf = subtract ? half && (a & 0x0F) < 6 : (a & 0x0F) > 9;
                a = subtract ? a - correction : a + correction;
                A = (byte)a;
                F = (byte)(Szxyp(A) | (subtract ? NF : 0) | (newHalf ? HF : 0) | (carry ? CF : 0));
                break;
            }
            case 5:                                                                     // CPL
                A = (byte)~A;
                F = (byte)((F & (SF | ZF | PF | CF)) | HF | NF | (A & (YF | XF)));
                break;
            case 6:                                                                     // SCF
                F = (byte)((F & (SF | ZF | PF)) | (A & (YF | XF)) | CF);
                break;
            default:                                                                    // CCF
                F = (byte)(((F & (SF | ZF | PF | CF)) | (CarryFlag ? HF : 0) | (A & (YF | XF))) ^ CF);
                break;
        }
    }

    void ExecuteCb()
    {
        byte op = Fetch();
        int x = op >> 6, y = (op >> 3) & 7, z = op & 7;
        byte v = GetR(z);
        switch (x)
        {
            case 0: SetR(z, Rotate(y, v)); break;
            case 1: Bit(y, v, z == 6 ? HL >> 8 : v); break;
            case 2: SetR(z, (byte)(v & ~(1 << y))); break;                              // RES
            default: SetR(z, (byte)(v | (1 << y))); break;                              // SET
        }
    }

    // DD CB d op / FD CB d op : sur (IX+d) ; sauf BIT, le résultat va aussi dans le registre z (non documenté)
    void ExecuteIndexCb()
    {
        int address = (HLX + (sbyte)Fetch()) & 0xFFFF;
        byte op = Fetch();
        int x = op >> 6, y = (op >> 3) & 7, z = op & 7;
        byte v = ReadMemory(address);
        byte result;
        switch (x)
        {
            case 0: result = Rotate(y, v); break;
            case 1: Bit(y, v, address >> 8); return;
            case 2: result = (byte)(v & ~(1 << y)); break;
            default: result = (byte)(v | (1 << y)); break;
        }
        Memory[address] = result;
        if (z != 6)
            SetR(z, result, plain: true);
    }

    void ExecuteEd()
    {
        byte op = Fetch();
        int x = op >> 6, y = (op >> 3) & 7, z = op & 7, p = y >> 1, q = y & 1;
        if (x == 1)
        {
            switch (z)
            {
                case 0:                                                                 // IN r,(C)
                {
                    byte read = Ports.TryGetValue(BC, out byte value) ? value : (byte)0xFF;
                    F = (byte)((F & CF) | Szxyp(read));
                    if (y != 6)
                        SetR(y, read);
                    break;
                }
                case 1:                                                                 // OUT (C),r
                    PortWrites.Add((BC, y == 6 ? (byte)0 : GetR(y)));
                    CartridgeOut(BC, y == 6 ? (byte)0 : GetR(y));                      // D207
                    break;
                case 2:
                    if (q == 0)
                        Sbc16(GetRp(p));                                                // SBC HL,rr
                    else
                        Adc16(GetRp(p));                                                // ADC HL,rr
                    break;
                case 3:
                    if (q == 0)
                        Write16(Fetch16(), GetRp(p));                                   // LD (nn),rr
                    else
                        SetRp(p, Read16(Fetch16()));                                    // LD rr,(nn)
                    break;
                case 4:                                                                 // NEG
                {
                    byte value = A;
                    A = 0;
                    SubA(value, 0, true);
                    break;
                }
                case 5:
                    pc = Pop();                                                         // RETN, RETI
                    break;
                case 6:
                    InterruptMode = (byte)(y switch { 2 or 6 => 1, 3 or 7 => 2, _ => 0 });   // IM 0 / 1 / 2
                    break;
                default:
                    switch (y)
                    {
                        case 0: I = A; break;                                           // LD I,A
                        case 1: R = A; break;                                           // LD R,A
                        case 2:                                                         // LD A,I
                        case 3:                                                         // LD A,R
                            A = y == 2 ? I : R;
                            F = (byte)((F & CF) | Szxy(A) | (InterruptsEnabled ? PF : 0));
                            break;
                        case 4:                                                         // RRD
                        {
                            byte m = ReadMemory(HL);
                            Memory[HL] = (byte)((A << 4) | (m >> 4));
                            A = (byte)((A & 0xF0) | (m & 0x0F));
                            F = (byte)((F & CF) | Szxyp(A));
                            break;
                        }
                        case 5:                                                         // RLD
                        {
                            byte m = ReadMemory(HL);
                            Memory[HL] = (byte)((m << 4) | (A & 0x0F));
                            A = (byte)((A & 0xF0) | (m >> 4));
                            F = (byte)((F & CF) | Szxyp(A));
                            break;
                        }
                        default: break;                                                 // NOP
                    }
                    break;
            }
            return;
        }
        if (x == 2 && z <= 3 && y >= 4)
        {
            Block(y, z);
            return;
        }
        // les autres codes après ED ne font rien (comme le vrai Z80)
    }

    // LDI, LDD, LDIR, LDDR ; CPI, CPD, CPIR, CPDR ; INI, IND, INIR, INDR ; OUTI, OUTD, OTIR, OTDR
    void Block(int y, int z)
    {
        int step = (y & 1) == 0 ? 1 : -1;
        bool repeat = y >= 6;
        switch (z)
        {
            case 0:
            {
                byte value;
                do
                {
                    value = ReadMemory(HL);
                    Memory[DE] = value;
                    HL = (ushort)(HL + step);
                    DE = (ushort)(DE + step);
                    BC = (ushort)(BC - 1);
                }
                while (repeat && BC != 0);
                int n = value + A;
                F = (byte)((F & (SF | ZF | CF)) | (BC != 0 ? PF : 0) | (n & XF) | ((n << 4) & YF));
                break;
            }
            case 1:
            {
                bool carry = CarryFlag;
                byte value;
                int r;
                do
                {
                    value = ReadMemory(HL);
                    r = A - value;
                    HL = (ushort)(HL + step);
                    BC = (ushort)(BC - 1);
                }
                while (repeat && BC != 0 && (r & 0xFF) != 0);
                bool half = ((A ^ value ^ r) & HF) != 0;
                int n = r - (half ? 1 : 0);
                F = (byte)(((r & 0xFF) == 0 ? ZF : 0) | (r & SF) | NF | (half ? HF : 0) | (BC != 0 ? PF : 0)
                           | (carry ? CF : 0) | (n & XF) | ((n << 4) & YF));
                break;
            }
            case 2:
                do
                {
                    Memory[HL] = Ports.TryGetValue(BC, out byte read) ? read : (byte)0xFF;
                    HL = (ushort)(HL + step);
                    B--;
                }
                while (repeat && B != 0);
                F = (byte)((F & CF) | NF | (B == 0 ? ZF : 0) | (B & (SF | YF | XF)));
                break;
            default:
                do
                {
                    B--;
                    PortWrites.Add((BC, ReadMemory(HL)));
                    HL = (ushort)(HL + step);
                }
                while (repeat && B != 0);
                F = (byte)((F & CF) | NF | (B == 0 ? ZF : 0) | (B & (SF | YF | XF)));
                break;
        }
    }
}
