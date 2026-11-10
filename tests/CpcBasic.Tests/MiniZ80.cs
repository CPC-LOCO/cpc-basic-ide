// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;

namespace CpcBasic.Tests;

/// <summary>
/// Mini-émulateur Z80 pour les tests : il ne connaît que les instructions utilisées par les modules
/// de la bibliothèque (PrintInt, Mul16, Div16...). Seuls les indicateurs Z (zéro) et C (retenue)
/// sont gérés. Un appel au firmware &amp;BB5A (afficher un caractère) est capté dans Output.
/// Si un module utilise une instruction inconnue, l'émulateur le dit : il suffit de l'ajouter ici.
/// </summary>
internal sealed class MiniZ80
{
    public byte A, B, C, D, E, H, L;
    public bool CarryFlag, ZeroFlag;
    public readonly byte[] Memory = new byte[65536];
    public readonly StringBuilder Output = new();

    int pc;
    int sp;
    int steps;

    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
    }

    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
    }

    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
    }

    public void Load(byte[] code, int origin) => Array.Copy(code, 0, Memory, origin, code.Length);

    /// <summary>Appelle la routine à cette adresse et s'arrête quand elle fait RET.</summary>
    public void Call(int address)
    {
        sp = 0xF000;
        Push(0x0000);          // adresse de retour : 0 signifie « terminé »
        pc = address;
        steps = 0;

        while (pc != 0x0000)
        {
            if (++steps > 2_000_000)
                throw new InvalidOperationException("Le module tourne sans fin.");
            Step();
        }
    }

    byte Fetch() => Memory[pc++ & 0xFFFF];

    ushort Fetch16()
    {
        int low = Fetch();
        int high = Fetch();
        return (ushort)(low | (high << 8));
    }

    void Push(ushort value)
    {
        sp -= 2;
        Memory[sp] = (byte)(value & 0xFF);
        Memory[sp + 1] = (byte)(value >> 8);
    }

    ushort Pop()
    {
        ushort value = (ushort)(Memory[sp] | (Memory[sp + 1] << 8));
        sp += 2;
        return value;
    }

    void AddHl(ushort value)
    {
        int result = HL + value;
        CarryFlag = result > 0xFFFF;
        HL = (ushort)result;
    }

    void Sub(byte value)
    {
        CarryFlag = A < value;
        A = (byte)(A - value);
        ZeroFlag = A == 0;
    }

    void Jr(bool condition)
    {
        sbyte offset = (sbyte)Fetch();
        if (condition)
            pc += offset;
    }

    byte Rl(byte value)
    {
        bool carryOut = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (CarryFlag ? 1 : 0));
        CarryFlag = carryOut;
        ZeroFlag = result == 0;
        return result;
    }

    void Step()
    {
        int opcodeAddress = pc;
        byte op = Fetch();

        switch (op)
        {
            case 0x00: break;                                          // NOP

            // Chargements
            case 0x06: B = Fetch(); break;                             // LD B,n
            case 0x3E: A = Fetch(); break;                             // LD A,n
            case 0x21: HL = Fetch16(); break;                          // LD HL,nn
            case 0x11: DE = Fetch16(); break;                          // LD DE,nn
            case 0x44: B = H; break;                                   // LD B,H
            case 0x47: B = A; break;                                   // LD B,A
            case 0x4D: C = L; break;                                   // LD C,L
            case 0x4F: C = A; break;                                   // LD C,A
            case 0x57: D = A; break;                                   // LD D,A
            case 0x5F: E = A; break;                                   // LD E,A
            case 0x67: H = A; break;                                   // LD H,A
            case 0x69: L = C; break;                                   // LD L,C
            case 0x6F: L = A; break;                                   // LD L,A
            case 0x78: A = B; break;                                   // LD A,B
            case 0x79: A = C; break;                                   // LD A,C
            case 0x7A: A = D; break;                                   // LD A,D
            case 0x7C: A = H; break;                                   // LD A,H
            case 0x7D: A = L; break;                                   // LD A,L
            case 0xEB: (DE, HL) = (HL, DE); break;                     // EX DE,HL

            // Addition 16 bits
            case 0x09: AddHl(BC); break;                               // ADD HL,BC
            case 0x19: AddHl(DE); break;                               // ADD HL,DE
            case 0x29: AddHl(HL); break;                               // ADD HL,HL

            // Compteurs et opérations sur A
            case 0x3C: A++; ZeroFlag = A == 0; break;                  // INC A
            case 0x3D: A--; ZeroFlag = A == 0; break;                  // DEC A
            case 0x0D: C--; ZeroFlag = C == 0; break;                  // DEC C
            case 0xB7: ZeroFlag = A == 0; CarryFlag = false; break;    // OR A
            case 0xB5: A |= L; ZeroFlag = A == 0; CarryFlag = false; break;   // OR L
            case 0xB3: A |= E; ZeroFlag = A == 0; CarryFlag = false; break;   // OR E
            case 0xAF: A = 0; ZeroFlag = true; CarryFlag = false; break;      // XOR A
            case 0xEE: A ^= Fetch(); ZeroFlag = A == 0; CarryFlag = false; break;   // XOR n
            case 0xFE:                                                 // CP n
            {
                byte n = Fetch();
                CarryFlag = A < n;
                ZeroFlag = A == n;
                break;
            }
            case 0x92: Sub(D); break;                                  // SUB D
            case 0x93: Sub(E); break;                                  // SUB E
            case 0x94: Sub(H); break;                                  // SUB H
            case 0x95: Sub(L); break;                                  // SUB L
            case 0x9F:                                                 // SBC A,A
                A = (byte)(CarryFlag ? 0xFF : 0x00);
                ZeroFlag = A == 0;
                break;
            case 0x37: CarryFlag = true; break;                        // SCF
            case 0x17:                                                 // RLA
            {
                bool carryOut = (A & 0x80) != 0;
                A = (byte)((A << 1) | (CarryFlag ? 1 : 0));
                CarryFlag = carryOut;
                break;
            }

            // Pile et appels
            case 0xC5: Push(BC); break;                                // PUSH BC
            case 0xD5: Push(DE); break;                                // PUSH DE
            case 0xE5: Push(HL); break;                                // PUSH HL
            case 0xC1: BC = Pop(); break;                              // POP BC
            case 0xD1: DE = Pop(); break;                              // POP DE
            case 0xE1: HL = Pop(); break;                              // POP HL
            case 0xC9: pc = Pop(); break;                              // RET
            case 0xC8: if (ZeroFlag) pc = Pop(); break;                // RET Z
            case 0xD0: if (!CarryFlag) pc = Pop(); break;              // RET NC
            case 0xD8: if (CarryFlag) pc = Pop(); break;               // RET C
            case 0xC3: pc = Fetch16(); break;                          // JP nn
            case 0xCD:                                                 // CALL nn
            {
                ushort address = Fetch16();
                if (address == 0xBB5A)
                {
                    Output.Append((char)A);                            // TXT OUTPUT : caractère affiché
                }
                else
                {
                    Push((ushort)pc);
                    pc = address;
                }
                break;
            }

            // Sauts relatifs
            case 0x18: Jr(true); break;                                // JR
            case 0x20: Jr(!ZeroFlag); break;                           // JR NZ
            case 0x28: Jr(ZeroFlag); break;                            // JR Z
            case 0x30: Jr(!CarryFlag); break;                          // JR NC
            case 0x38: Jr(CarryFlag); break;                           // JR C
            case 0x10:                                                 // DJNZ
            {
                B--;
                Jr(B != 0);
                break;
            }

            case 0xCB:
                StepCb(opcodeAddress);
                break;

            case 0xED:
                StepEd(opcodeAddress);
                break;

            default:
                throw new NotSupportedException($"Instruction &{op:X2} à l'adresse &{opcodeAddress:X4} : à ajouter à MiniZ80.");
        }
    }

    void StepCb(int opcodeAddress)
    {
        byte op = Fetch();
        switch (op)
        {
            case 0x11: C = Rl(C); break;                               // RL C
            case 0x12: D = Rl(D); break;                               // RL D
            case 0x23:                                                 // SLA E
                CarryFlag = (E & 0x80) != 0;
                E = (byte)(E << 1);
                ZeroFlag = E == 0;
                break;
            case 0x40: ZeroFlag = (B & 0x01) == 0; break;              // BIT 0,B
            case 0x48: ZeroFlag = (B & 0x02) == 0; break;              // BIT 1,B
            case 0x7A: ZeroFlag = (D & 0x80) == 0; break;              // BIT 7,D
            case 0x7C: ZeroFlag = (H & 0x80) == 0; break;              // BIT 7,H
            default:
                throw new NotSupportedException($"Instruction &CB{op:X2} à l'adresse &{opcodeAddress:X4} : à ajouter à MiniZ80.");
        }
    }

    void StepEd(int opcodeAddress)
    {
        byte op = Fetch();
        switch (op)
        {
            case 0x52:                                                 // SBC HL,DE
            {
                int result = HL - DE - (CarryFlag ? 1 : 0);
                CarryFlag = result < 0;
                HL = (ushort)result;
                ZeroFlag = HL == 0;
                break;
            }
            case 0x6A:                                                 // ADC HL,HL
            {
                int result = HL + HL + (CarryFlag ? 1 : 0);
                CarryFlag = result > 0xFFFF;
                HL = (ushort)result;
                ZeroFlag = HL == 0;
                break;
            }
            default:
                throw new NotSupportedException($"Instruction &ED{op:X2} à l'adresse &{opcodeAddress:X4} : à ajouter à MiniZ80.");
        }
    }
}
