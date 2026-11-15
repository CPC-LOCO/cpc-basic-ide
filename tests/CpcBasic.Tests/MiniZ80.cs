// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Tests;

/// <summary>
/// Mini-émulateur Z80 pour les tests : il ne connaît que les instructions utilisées par les modules
/// de la bibliothèque (PrintInt, Mul16, Div16, Pixel, Box...). Seuls les indicateurs Z (zéro) et C (retenue)
/// sont gérés. Un appel au firmware &amp;BB5A (afficher un caractère) est capté dans Output.
/// Si un module utilise une instruction inconnue, l'émulateur le dit : il suffit de l'ajouter ici.
/// </summary>
internal sealed class MiniZ80
{
    public byte A, B, C, D, E, H, L;
    public bool CarryFlag, ZeroFlag;

    /// <summary>Indicateur de débordement signé (P/V), mis à jour par ADC HL,DE ; lu par JP PE (boucles FOR, D058).</summary>
    public bool OverflowFlag;
    public readonly byte[] Memory = new byte[65536];
    public readonly StringBuilder Output = new();

    /// <summary>Temps rendu par KL TIME PLEASE (&BD0D) dans DEHL.</summary>
    public uint Clock;

    /// <summary>Mode d'écran rendu par SCR GET MODE (&BC11) dans A : 0, 1 ou 2.</summary>
    public byte ScreenMode = 1;

    /// <summary>Touches appuyées : numéro de touche -> état de Shift (bit 5) et Ctrl (bit 7) rendu dans C.</summary>
    public readonly Dictionary<byte, byte> Keys = new();
    public byte Joystick0, Joystick1;

    /// <summary>Valeurs lues par IN A,(C) (port -> valeur), et écritures faites par OUT (C),A (port, valeur).</summary>
    public readonly Dictionary<ushort, byte> Ports = new();
    public readonly List<(ushort Port, byte Value)> PortWrites = new();
    public readonly Queue<byte> KeyQueue = new();

    /// <summary>Version rendue par KL PROBE ROM (&amp;B915) dans H : 0 = 464, 1 = 664, 2 = 6128 (D068).
    /// Elle choisit aussi les adresses des routines de calcul à virgule simulées (464 ou 664/6128).</summary>
    public byte FirmwareVersion = 2;

    /// <summary>Angles en degrés (SET ANGLE MODE avec A différent de 0), sinon en radians.</summary>
    public bool AngleDegrees;

    /// <summary>Lit un nombre à virgule de 5 octets à cette adresse.</summary>
    public double ReadReal(int address) => LocoReal.Decode(Memory.AsSpan(address & 0xFFFF, 5));

    /// <summary>Range un nombre à virgule de 5 octets ; faux s'il est trop grand (Overflow).</summary>
    public bool WriteReal(int address, double value)
    {
        byte[]? bytes = LocoReal.Encode(value);
        if (bytes is null)
            return false;
        Array.Copy(bytes, 0, Memory, address & 0xFFFF, 5);
        return true;
    }

    /// <summary>
    /// Vrai si la ROM haute (celle du BASIC) est active : le Z80 lit alors la ROM de &C000 à &FFFF au lieu de l'écran,
    /// comme sur un vrai CPC (les écritures vont toujours dans la RAM). KL U ROM DISABLE (&B903) la coupe.
    /// </summary>
    public bool UpperRomEnabled = true;

    /// <summary>Lecture d'un octet de la mémoire, avec le masque de la ROM haute.</summary>
    byte ReadMemory(int address) =>
        UpperRomEnabled && address >= 0xC000 ? (byte)(0xA5 ^ address) : Memory[address & 0xFFFF];

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
        sp = 0xBF00;           // sous l'écran (&C000) : la pile ne doit pas se mélanger aux tests de tracé
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

    /// <summary>
    /// Simule une routine de calcul à virgule de la ROM (voir FirmwareMaths), aux adresses du 464 si
    /// FirmwareVersion vaut 0, sinon à celles du 664/6128. Rend faux si l'adresse n'en est pas une.
    /// Les calculs se font en double puis sont arrondis sur 5 octets, ce qui suffit pour les tests.
    /// </summary>
    bool FirmwareMathsCall(ushort address)
    {
        FirmwareMathsRoutine? routine = FirmwareMaths.All.FirstOrDefault(
            r => (FirmwareVersion == 0 ? r.Address464 : r.Address6128) == address);
        if (routine is null)
            return false;

        ushort first = HL;
        bool ok = true;
        double Angle(double x) => AngleDegrees ? x * Math.PI / 180 : x;
        bool Result(double value) => !double.IsNaN(value) && WriteReal(first, value);

        switch (routine.Name)
        {
            case "int2real":
            {
                double value = HL;
                if ((A & 0x80) != 0)
                    value = -value;
                ok = WriteReal(DE, value);
                HL = DE;
                break;
            }
            case "real2int":
            {
                double rounded = Math.Floor(Math.Abs(ReadReal(first)) + 0.5);
                ok = rounded <= 65535;
                HL = ok ? (ushort)rounded : (ushort)0;
                break;
            }
            case "fix":                                        // comme le CPC (D069) : entier de 4 octets en place, signe dans B
            case "int":
            {
                double value = ReadReal(first);
                double whole = Math.Abs(routine.Name == "fix" ? Math.Truncate(value) : Math.Floor(value));
                ok = whole < 4294967296.0;
                if (ok)
                {
                    uint binary = (uint)whole;
                    for (int k = 0; k < 4; k++)
                        Memory[(first + k) & 0xFFFF] = (byte)(binary >> (8 * k));
                }
                B = (byte)(value < 0 ? 0x80 : 0);
                break;
            }
            case "bin2real":
            {
                uint binary = (uint)(Memory[first] | (Memory[(first + 1) & 0xFFFF] << 8)
                                     | (Memory[(first + 2) & 0xFFFF] << 16) | (Memory[(first + 3) & 0xFFFF] << 24));
                ok = WriteReal(first, (A & 0x80) != 0 ? -(double)binary : binary);
                break;
            }
            case "x10a": ok = Result(ReadReal(first) * Math.Pow(10, (sbyte)A)); break;
            case "add": ok = Result(ReadReal(first) + ReadReal(DE)); break;
            case "rsub": ok = Result(ReadReal(DE) - ReadReal(first)); break;
            case "mult": ok = Result(ReadReal(first) * ReadReal(DE)); break;
            case "div": ok = ReadReal(DE) != 0 && Result(ReadReal(first) / ReadReal(DE)); break;
            case "cmp":
            {
                double a = ReadReal(first), b = ReadReal(DE);
                A = (byte)(a > b ? 1 : a == b ? 0 : 0xFF);
                ZeroFlag = A == 0;
                break;
            }
            case "neg": ok = Result(-ReadReal(first)); break;
            case "sgn":
            {
                double a = ReadReal(first);
                A = (byte)(a > 0 ? 1 : a == 0 ? 0 : 0xFF);
                ZeroFlag = A == 0;
                break;
            }
            case "angle": AngleDegrees = A != 0; break;
            case "pi": ok = Result(Math.PI); break;
            case "sqr": ok = ReadReal(first) >= 0 && Result(Math.Sqrt(ReadReal(first))); break;
            case "pow": ok = Result(Math.Pow(ReadReal(first), ReadReal(DE))); break;
            case "log": ok = ReadReal(first) > 0 && Result(Math.Log(ReadReal(first))); break;
            case "log10": ok = ReadReal(first) > 0 && Result(Math.Log10(ReadReal(first))); break;
            case "exp": ok = Result(Math.Exp(ReadReal(first))); break;
            case "sin": ok = Result(Math.Sin(Angle(ReadReal(first)))); break;
            case "cos": ok = Result(Math.Cos(Angle(ReadReal(first)))); break;
            case "tan": ok = Result(Math.Tan(Angle(ReadReal(first)))); break;
            case "atn":
            {
                double value = Math.Atan(ReadReal(first));
                ok = Result(AngleDegrees ? value * 180 / Math.PI : value);
                break;
            }
            default:
                throw new InvalidOperationException($"Routine de calcul non simulée : {routine.Name}");
        }

        if (routine.Name != "int2real" && routine.Name != "real2int")
            HL = first;
        CarryFlag = ok;
        return true;
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

    void Add(byte value, bool withCarry = false)
    {
        int total = A + value + (withCarry && CarryFlag ? 1 : 0);
        CarryFlag = total > 0xFF;
        A = (byte)total;
        ZeroFlag = A == 0;
    }

    void And(byte value)
    {
        A &= value;
        ZeroFlag = A == 0;
        CarryFlag = false;
    }

    void Compare(byte value)
    {
        CarryFlag = A < value;
        ZeroFlag = A == value;
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

    byte Rl_Right(byte value)
    {
        bool carryOut = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (CarryFlag ? 0x80 : 0));
        CarryFlag = carryOut;
        ZeroFlag = result == 0;
        return result;
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
            case 0x16: D = Fetch(); break;                             // LD D,n
            case 0x26: H = Fetch(); break;                             // LD H,n
            case 0x2E: L = Fetch(); break;                             // LD L,n
            case 0x3A: A = Memory[Fetch16()]; break;                   // LD A,(nn)
            case 0x32: Memory[Fetch16()] = A; break;                   // LD (nn),A
            case 0x7E: A = ReadMemory(HL); break;                      // LD A,(HL)
            case 0x77: Memory[HL] = A; break;                          // LD (HL),A
            case 0x7B: A = E; break;                                   // LD A,E
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
            case 0x23: HL = (ushort)(HL + 1); break;                   // INC HL
            case 0x2A:                                                 // LD HL,(nn)
            {
                int at = Fetch16();
                HL = (ushort)(Memory[at] | (Memory[(at + 1) & 0xFFFF] << 8));
                break;
            }
            case 0x22:                                                 // LD (nn),HL
            {
                int at = Fetch16();
                Memory[at] = L;
                Memory[(at + 1) & 0xFFFF] = H;
                break;
            }
            case 0xAA: A ^= D; ZeroFlag = A == 0; CarryFlag = false; break;   // XOR D
            case 0xAB: A ^= E; ZeroFlag = A == 0; CarryFlag = false; break;   // XOR E
            case 0x3C: A++; ZeroFlag = A == 0; break;                  // INC A
            case 0x3D: A--; ZeroFlag = A == 0; break;                  // DEC A
            case 0x0D: C--; ZeroFlag = C == 0; break;                  // DEC C
            case 0xB7: ZeroFlag = A == 0; CarryFlag = false; break;    // OR A
            case 0x2F: A ^= 0xFF; break;                               // CPL
            case 0xC2: { ushort jumpTo = Fetch16(); if (!ZeroFlag) pc = jumpTo; break; }    // JP NZ,nn
            case 0xCA: { ushort jumpTo = Fetch16(); if (ZeroFlag) pc = jumpTo; break; }     // JP Z,nn
            case 0xD2: { ushort jumpTo = Fetch16(); if (!CarryFlag) pc = jumpTo; break; }   // JP NC,nn
            case 0xDA: { ushort jumpTo = Fetch16(); if (CarryFlag) pc = jumpTo; break; }    // JP C,nn
            case 0x80: Add(B); break;                                  // ADD A,B
            case 0x81: Add(C); break;                                  // ADD A,C
            case 0x85: Add(L); break;                                  // ADD A,L
            case 0x87: Add(A); break;                                  // ADD A,A
            case 0xC6: Add(Fetch()); break;                            // ADD A,n
            case 0xCE: Add(Fetch(), withCarry: true); break;          // ADC A,n
            case 0x91: Sub(C); break;                                  // SUB C
            case 0xA2: And(D); break;                                  // AND D
            case 0xA3: And(E); break;                                  // AND E
            case 0xA6: And(ReadMemory(HL)); break;                     // AND (HL)
            case 0xE6: And(Fetch()); break;                            // AND n
            case 0xB8: Compare(B); break;                              // CP B
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
            case 0x6B: L = E; break;                                   // LD L,E (D069)
            case 0x82: Add(D); break;                                  // ADD A,D (D068)
            case 0x83: Add(E); break;                                  // ADD A,E (D068)
            case 0x34: Memory[HL]++; ZeroFlag = Memory[HL] == 0; break;            // INC (HL) (D068)
            case 0x35: Memory[HL]--; ZeroFlag = Memory[HL] == 0; break;            // DEC (HL) (D068)
            case 0xAE: A ^= ReadMemory(HL); ZeroFlag = A == 0; CarryFlag = false; break;   // XOR (HL) (D068)
            case 0xF6: A |= Fetch(); ZeroFlag = A == 0; CarryFlag = false; break;          // OR n (D068)
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
            case 0xF5:                                                 // PUSH AF
                Push((ushort)((A << 8) | (ZeroFlag ? 0x40 : 0) | (CarryFlag ? 0x01 : 0)));
                break;
            case 0xF1:                                                 // POP AF
            {
                ushort af = Pop();
                A = (byte)(af >> 8);
                ZeroFlag = (af & 0x40) != 0;
                CarryFlag = (af & 0x01) != 0;
                break;
            }
            case 0xC0: if (!ZeroFlag) pc = Pop(); break;               // RET NZ
            case 0xD1: DE = Pop(); break;                              // POP DE
            case 0xE1: HL = Pop(); break;                              // POP HL
            case 0xC9: pc = Pop(); break;                              // RET
            case 0xC8: if (ZeroFlag) pc = Pop(); break;                // RET Z
            case 0xD0: if (!CarryFlag) pc = Pop(); break;              // RET NC
            case 0xD8: if (CarryFlag) pc = Pop(); break;               // RET C
            case 0xC3:                                                 // JP nn
            {
                ushort target = Fetch16();
                if (target == 0xB90C)
                {
                    UpperRomEnabled = (A & 1) != 0;                    // KL ROM RESTORE, puis retour à l'appelant
                    pc = Pop();
                }
                else if (target == 0xBB5A)
                {
                    Output.Append((char)A);                            // JP TXT OUTPUT (sortie de rt_fp_out, D070)
                    pc = Pop();
                }
                else if (FirmwareMathsCall(target))
                {
                    pc = Pop();                                        // routine de calcul : retour à l'appelant
                }
                else
                {
                    pc = target;
                }
                break;
            }
            case 0xCD:                                                 // CALL nn
            {
                ushort address = Fetch16();
                if (address == 0xBB5A)
                {
                    Output.Append((char)A);                            // TXT OUTPUT : caractère affiché
                }
                else if (address == 0xBC11)
                {
                    A = ScreenMode;                                    // SCR GET MODE
                }
                else if (address == 0xB903)
                {
                    A = (byte)(UpperRomEnabled ? 1 : 0);               // KL U ROM DISABLE : A = état précédent
                    UpperRomEnabled = false;
                }
                else if (address == 0xB90C)
                {
                    UpperRomEnabled = (A & 1) != 0;                    // KL ROM RESTORE
                }
                else if (address == 0xBB1E)
                {
                    ZeroFlag = !Keys.TryGetValue(A, out byte state);   // KM TEST KEY
                    C = state;
                }
                else if (address == 0xBB24)
                {
                    H = Joystick0;                                     // KM GET JOYSTICK
                    L = Joystick1;
                }
                else if (address == 0xBB06)
                {
                    if (KeyQueue.Count == 0)
                        throw new InvalidOperationException("Le programme attend une touche, mais il n'y en a plus de prévues.");
                    A = KeyQueue.Dequeue();                            // KM WAIT CHAR
                }
                else if (address == 0xBB09)
                {
                    CarryFlag = KeyQueue.Count > 0;                    // KM READ CHAR
                    if (CarryFlag)
                        A = KeyQueue.Dequeue();
                }
                else if (address == 0xBB78)
                {
                    string shown = Output.ToString();                  // TXT GET CURSOR : H = colonne, L = ligne
                    H = (byte)(shown.Length - shown.LastIndexOf('\n'));
                    L = 1;
                }
                else if (address == FirmwareMaths.KlProbeRom)
                {
                    A = 0x80;                                          // KL PROBE ROM : ROM 0 = BASIC
                    H = FirmwareVersion;
                    L = 0;
                }
                else if (FirmwareMathsCall(address))
                {
                    // routine de calcul à virgule simulée
                }
                else if (address == 0xBD0D)
                {
                    DE = (ushort)(Clock >> 16);                        // KL TIME PLEASE : temps dans DEHL
                    HL = (ushort)(Clock & 0xFFFF);
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

            case 0xE9: pc = HL; break;                                 // JP (HL)
            case 0xEA: { ushort jumpTo = Fetch16(); if (OverflowFlag) pc = jumpTo; break; }  // JP PE,nn (débordement)

            // Tableaux (D058)
            case 0x01: BC = Fetch16(); break;                          // LD BC,nn
            case 0x1E: E = Fetch(); break;                             // LD E,n
            case 0x13: DE = (ushort)(DE + 1); break;                   // INC DE
            case 0x1B: DE = (ushort)(DE - 1); break;                   // DEC DE
            case 0x0B: BC = (ushort)(BC - 1); break;                   // DEC BC
            case 0x2B: HL = (ushort)(HL - 1); break;                   // DEC HL
            case 0x1A: A = ReadMemory(DE); break;                      // LD A,(DE)
            case 0x36: Memory[HL] = Fetch(); break;                    // LD (HL),n
            case 0x72: Memory[HL] = D; break;                          // LD (HL),D
            case 0x73: Memory[HL] = E; break;                          // LD (HL),E
            case 0x46: B = ReadMemory(HL); break;                      // LD B,(HL)
            case 0x4E: C = ReadMemory(HL); break;                      // LD C,(HL)
            case 0x66: H = ReadMemory(HL); break;                      // LD H,(HL)
            case 0x6E: L = ReadMemory(HL); break;                      // LD L,(HL)
            case 0x56: D = ReadMemory(HL); break;                      // LD D,(HL) (D059)
            case 0x5E: E = ReadMemory(HL); break;                      // LD E,(HL) (D059)
            case 0x54: D = H; break;                                   // LD D,H
            case 0x5D: E = L; break;                                   // LD E,L
            case 0xB6: A |= ReadMemory(HL); ZeroFlag = A == 0; CarryFlag = false; break;   // OR (HL)
            case 0xB0: A |= B; ZeroFlag = A == 0; CarryFlag = false; break;   // OR B
            case 0xB1: A |= C; ZeroFlag = A == 0; CarryFlag = false; break;   // OR C
            case 0xB2: A |= D; ZeroFlag = A == 0; CarryFlag = false; break;   // OR D (D060)
            // Textes (lot E, D062)
            case 0x71: Memory[HL] = C; break;                          // LD (HL),C
            case 0x86: Add(ReadMemory(HL)); break;                     // ADD A,(HL)
            case 0xBE: Compare(ReadMemory(HL)); break;                 // CP (HL)
            case 0x05: B--; ZeroFlag = B == 0; break;                  // DEC B
            case 0x0C: C++; ZeroFlag = C == 0; break;                  // INC C
            case 0x0E: C = Fetch(); break;                             // LD C,n
            case 0xB9: Compare(C); break;                              // CP C (D063)
            case 0x90: Sub(B); break;                                  // SUB B (D063)
            case 0x12: Memory[DE] = A; break;                          // LD (DE),A (D063)
            case 0xD6: Sub(Fetch()); break;                            // SUB n (D063)
            case 0x41: B = C; break;                                   // LD B,C (D063)
            case 0x60: H = B; break;                                   // LD H,B (D063)
            case 0x68: L = B; break;                                   // LD L,B (D065)
            case 0xCC:                                                 // CALL Z,nn
            {
                ushort target = Fetch16();
                if (ZeroFlag)
                {
                    Push((ushort)pc);
                    pc = target;
                }
                break;
            }
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
            case 0x1B: E = Rl_Right(E); break;                         // RR E
            case 0x3A: CarryFlag = (D & 0x01) != 0; D = (byte)(D >> 1); ZeroFlag = D == 0; break;   // SRL D
            case 0x3F: CarryFlag = (A & 0x01) != 0; A = (byte)(A >> 1); ZeroFlag = A == 0; break;   // SRL A
            case 0x12: D = Rl(D); break;                               // RL D
            case 0x23:                                                 // SLA E
                CarryFlag = (E & 0x80) != 0;
                E = (byte)(E << 1);
                ZeroFlag = E == 0;
                break;
            case 0x1D: L = Rl_Right(L); break;                         // RR L
            case 0x3C: CarryFlag = (H & 0x01) != 0; H = (byte)(H >> 1); ZeroFlag = H == 0; break;   // SRL H
            // Nombres à virgule (lot F1b, D068) : décalages d'un octet en mémoire
            case 0x26:                                                 // SLA (HL)
                CarryFlag = (Memory[HL] & 0x80) != 0;
                Memory[HL] = (byte)(Memory[HL] << 1);
                ZeroFlag = Memory[HL] == 0;
                break;
            case 0x16: Memory[HL] = Rl(Memory[HL]); break;             // RL (HL)
            case 0x3E:                                                 // SRL (HL)
                CarryFlag = (Memory[HL] & 0x01) != 0;
                Memory[HL] = (byte)(Memory[HL] >> 1);
                ZeroFlag = Memory[HL] == 0;
                break;
            case 0x1E: Memory[HL] = Rl_Right(Memory[HL]); break;       // RR (HL)
            case 0xBC: H = (byte)(H & 0x7F); break;                    // RES 7,H
            case 0x40: ZeroFlag = (B & 0x01) == 0; break;              // BIT 0,B
            case 0x43: ZeroFlag = (E & 0x01) == 0; break;              // BIT 0,E (D060)
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
            case 0x78: A = Ports.TryGetValue(BC, out byte read) ? read : (byte)0xFF; break;   // IN A,(C)
            case 0x79: PortWrites.Add((BC, A)); break;                                       // OUT (C),A
            case 0x5B:                                                 // LD DE,(nn)
            {
                int at = Fetch16();
                DE = (ushort)(Memory[at] | (Memory[(at + 1) & 0xFFFF] << 8));
                break;
            }
            case 0x52:                                                 // SBC HL,DE
            {
                int result = HL - DE - (CarryFlag ? 1 : 0);
                CarryFlag = result < 0;
                HL = (ushort)result;
                ZeroFlag = HL == 0;
                break;
            }
            case 0x42:                                                 // SBC HL,BC
            {
                int result = HL - BC - (CarryFlag ? 1 : 0);
                CarryFlag = result < 0;
                HL = (ushort)result;
                ZeroFlag = HL == 0;
                break;
            }
            case 0xB0:                                                 // LDIR
                do
                {
                    Memory[DE] = ReadMemory(HL);
                    HL = (ushort)(HL + 1);
                    DE = (ushort)(DE + 1);
                    BC = (ushort)(BC - 1);
                }
                while (BC != 0);
                break;
            case 0x5A:                                                 // ADC HL,DE
            {
                int result = HL + DE + (CarryFlag ? 1 : 0);
                int signed = (short)HL + (short)DE + (CarryFlag ? 1 : 0);
                CarryFlag = result > 0xFFFF;
                OverflowFlag = signed is < -32768 or > 32767;
                HL = (ushort)result;
                ZeroFlag = HL == 0;
                break;
            }
            case 0x53:                                                 // LD (nn),DE (D062)
            {
                int at = Fetch16();
                Memory[at] = E;
                Memory[(at + 1) & 0xFFFF] = D;
                break;
            }
            case 0xB8:                                                 // LDDR (D062)
                do
                {
                    Memory[DE] = ReadMemory(HL);
                    HL = (ushort)(HL - 1);
                    DE = (ushort)(DE - 1);
                    BC = (ushort)(BC - 1);
                }
                while (BC != 0);
                break;
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
