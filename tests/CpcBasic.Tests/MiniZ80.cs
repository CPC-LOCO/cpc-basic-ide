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

    /// <summary>Caractères envoyés à l'imprimante par MC PRINT CHAR (&BD2B, D100).</summary>
    public readonly StringBuilder Printed = new();

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

    /// <summary>Son (D071) : blocs de 9 octets reçus par SOUND QUEUE (&amp;BCAA), dans l'ordre.</summary>
    public readonly List<byte[]> Sounds = new();

    /// <summary>Enveloppes reçues : Tone = ENT (&amp;BCBF), sinon ENV (&amp;BCBC) ; numéro (A) et les 16 octets du bloc.</summary>
    public readonly List<(bool Tone, byte Number, byte[] Block)> Envelopes = new();

    /// <summary>Canaux libérés par SOUND RELEASE (&amp;BCB3).</summary>
    public readonly List<byte> Released = new();

    // Fenêtres (lot H1, D072) : fenêtre choisie (TXT STR SELECT), texte écrit dans chacune, WINDOW et WINDOW SWAP reçus
    public byte CurrentStream;
    public byte WindowWidth = 40;

    // Caractères (lot H4, D075) : premier caractère redessinable (240 au départ, comme le BASIC), dessins reçus,
    // tables données, TAG / TAGOFF
    public int FirstUserChar = 240;
    public readonly List<(byte Char, byte[] Rows)> Matrices = new();
    public readonly List<(int First, ushort Table)> MatrixTables = new();
    public readonly List<bool> Tags = new();
    public int MatrixReads;

    // Minuteries (lot I1, D079) : routine branchée par KL INIT EVENT puis KL ADD TICKER, appelée tous les TickSteps pas
    public ushort EventRoutine, TickRoutine;
    public int TickSteps = 300, Ticks;

    // Files sonores (ON SQ, D082) : si SoundQueueModel, chaque canal joue un son (durée en tops de 1/50 s) et en garde
    // 4 en attente ; SOUND ARM EVENT appelle la routine de l'événement dès qu'il y a une place, une seule fois.
    public bool SoundQueueModel;

    // Graphisme (D091) : curseur graphique, points tracés (couleur), réglages SPEED INK / SPEED KEY
    public short GraphicsX, GraphicsY;
    public byte GraphicsPen = 1;
    public readonly Dictionary<(int X, int Y), byte> Points = new();
    public readonly List<(string Kind, byte First, byte Second)> Speeds = new();
    // KEY, KEY DEF (D092) : appels du firmware (routine, A, B) et textes des touches de fonction
    public readonly List<(int Routine, byte A, byte B)> KeyCalls = new();
    public readonly Dictionary<int, string> Expansions = new();

    // Commandes RSX (D089) : nom -> adresse de la routine (KL FIND COMMAND)
    public readonly Dictionary<string, ushort> RsxCommands = new();
    public ushort IX;

    // Fichiers de données (D084) : le disque, rempli par CAS OUT et lu par CAS IN (noms en majuscules)
    public readonly Dictionary<string, List<byte>> Files = new();
    public readonly Dictionary<string, (byte Type, int Address)> FileHeaders = new();   // fichiers binaires (D085)
    public int Catalogs, SoundResets;
    public readonly List<(int HalfBit, byte Precompensation)> TapeSpeeds = new();
    string outFileName = "";
    string inFileName = "";
    List<byte>? fileOut;
    List<byte>? fileIn;
    int fileInPos;

    string FileName() => System.Text.Encoding.ASCII.GetString(Memory, HL, B).Trim().ToUpperInvariant();

    // Touches tapées à un moment donné (D083) : au top Tick, la touche arrive dans la file du clavier ; ESC (&FC) est
    // aussi enfoncée pendant 2 tops (KM TEST KEY 66). KM WAIT CHAR sans touche prête fait avancer le temps jusqu'à la
    // prochaine.
    public readonly List<(int Tick, byte Key)> TimedKeys = new();
    int escReleaseAt;

    void PressKey(byte key)
    {
        if (key == 0xFC)
        {
            Keys[66] = 1;
            escReleaseAt = Ticks + 2;
        }
        KeyQueue.Enqueue(key);
    }

    void TimedKeysTick()
    {
        if (escReleaseAt > 0 && Ticks >= escReleaseAt)
        {
            Keys.Remove(66);
            escReleaseAt = 0;
        }
        while (TimedKeys.Count > 0 && TimedKeys[0].Tick <= Ticks)
        {
            byte key = TimedKeys[0].Key;
            TimedKeys.RemoveAt(0);
            PressKey(key);
        }
    }
    readonly Dictionary<int, ushort> eventBlocks = new();
    readonly int[] soundPlaying = new int[3];
    readonly Queue<int>[] soundWaiting = [new(), new(), new()];
    readonly ushort[] soundArmed = new ushort[3];

    // Une place libre dans la file du canal armé : la routine de l'événement est appelée (comme un CALL)
    void KickSoundEvent(int channel)
    {
        if (soundArmed[channel] == 0 || soundWaiting[channel].Count >= 4)
            return;
        ushort routine = eventBlocks[soundArmed[channel]];
        soundArmed[channel] = 0;
        Push((ushort)pc);
        pc = routine;
    }

    void SoundTick()
    {
        for (int k = 0; k < 3; k++)
        {
            if (soundPlaying[k] > 0 && --soundPlaying[k] == 0 && soundWaiting[k].Count > 0)
                soundPlaying[k] = soundWaiting[k].Dequeue();
            KickSoundEvent(k);
        }
    }
    public bool InterruptsEnabled = true;

    // Éditeur de la ligne tapée (lot H5, D076) : positions données à TXT SET CURSOR, caractères lus par TXT RD CHAR
    public readonly List<(byte Col, byte Row)> CursorMoves = new();
    public readonly Dictionary<(byte Col, byte Row), byte> ScreenChars = new();                     // largeur rendue par TXT GET WINDOW (lot H2, D073)
    public readonly List<byte> StreamSelects = new();
    public readonly Dictionary<byte, StringBuilder> StreamOutput = new();
    public readonly List<(byte Stream, byte Left, byte Right, byte Top, byte Bottom)> Windows = new();
    public readonly List<(byte First, byte Second)> Swaps = new();

    /// <summary>Texte écrit dans la fenêtre n (#0 : tout ce qui n'est pas dans une autre fenêtre).</summary>
    public string StreamText(byte stream) => StreamOutput.TryGetValue(stream, out StringBuilder? text) ? text.ToString() : "";

    /// <summary>TXT STR SELECT : A = fenêtre choisie -> A = fenêtre d'avant.</summary>
    void SelectStream()
    {
        byte previous = CurrentStream;
        CurrentStream = A;
        StreamSelects.Add(A);
        A = previous;
    }

    void TextOutput(byte character)
    {
        Output.Append((char)character);
        if (!StreamOutput.TryGetValue(CurrentStream, out StringBuilder? text))
            StreamOutput[CurrentStream] = text = new StringBuilder();
        text.Append((char)character);
    }

    /// <summary>État rendu par SOUND CHECK (&amp;BCAD) : 4 places libres, canal arrêté.</summary>
    public byte SoundStatus = 4;

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
            if (TickRoutine != 0 && InterruptsEnabled && steps % TickSteps == 0)
            {
                Ticks++;
                TimedKeysTick();                    // touches tapées à ce moment (D083)
                Push((ushort)pc);                   // un « top » de 1/50 s : la routine du compteur est appelée (D079)
                pc = TickRoutine;
                if (SoundQueueModel)
                    SoundTick();                    // les sons avancent ; une place libérée appelle ON SQ (D082)
            }
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
            case 0x45: B = L; break;                                   // LD B,L (D092)
            case 0x48: C = B; break;                                   // LD C,B (D086)
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
            case 0xB4: A |= H; ZeroFlag = A == 0; CarryFlag = false; break;   // OR H (D092)
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
            case 0x63: H = E; break;                                   // LD H,E (D091)
            case 0x6C: L = H; break;                                   // LD L,H (POS, D072)
            case 0x24: H++; ZeroFlag = H == 0; break;                  // INC H (D076)
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
                if (target >= 0xB900 && Memory[target] == 0xC3)
                {
                    pc = target;                                       // entrée du firmware détournée (PRINT #9, D084)
                    break;
                }
                if (target == 0xB90C)
                {
                    UpperRomEnabled = (A & 1) != 0;                    // KL ROM RESTORE, puis retour à l'appelant
                    pc = Pop();
                }
                else if (target == 0xBB75)
                {
                    CursorMoves.Add((H, L));                           // JP TXT SET CURSOR (éditeur de ligne, D076)
                    pc = Pop();
                }
                else if (target == 0xBB8A || target == 0xBB8D)
                {
                    pc = Pop();                                        // JP TXT PLACE / REMOVE CURSOR (D076)
                }
                else if (target == 0xBCE9)
                {
                    TickRoutine = EventRoutine;                        // JP KL ADD TICKER (rt_ev_init, D079)
                    pc = Pop();
                }
                else if (target == 0xBBAB)
                {
                    FirstUserChar = DE;                                // JP TXT SET M TABLE (sortie de rt_symafter, D075)
                    MatrixTables.Add((DE, HL));
                    pc = Pop();
                }
                else if (target == 0xBBB4)
                {
                    SelectStream();                                    // JP TXT STR SELECT (sortie de rt_stream, D072)
                    pc = Pop();
                }
                else if (target == 0xBB5A)
                {
                    TextOutput(A);                                     // JP TXT OUTPUT (sortie de rt_fp_out, D070)
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
                if (address is 0xBB39 or 0xBB27 or 0xBB2D or 0xBB33)
                {
                    KeyCalls.Add((address, A, B));                     // KM SET REPEAT / TRANSLATE / SHIFT / CONTROL (D092)
                    break;
                }
                if (address == 0xBB0F)
                {
                    Expansions[B] = System.Text.Encoding.ASCII.GetString(Memory, HL, C);   // KM SET EXPAND (D092)
                    CarryFlag = true;
                    break;
                }
                if (address is 0xBBC0 or 0xBBC3 or 0xBBEA or 0xBBED or 0xBBF6 or 0xBBF9 or 0xBBF0 or 0xBBF3 or 0xBBC6
                    or 0xBBDE or 0xBC3E or 0xBB3F)
                {
                    bool relative = address is 0xBBC3 or 0xBBED or 0xBBF9 or 0xBBF3;   // graphisme (D091)
                    switch (address)
                    {
                        case 0xBBC6:                                   // GRA ASK CURSOR
                            DE = (ushort)GraphicsX;
                            HL = (ushort)GraphicsY;
                            break;
                        case 0xBBDE: GraphicsPen = A; break;           // GRA SET PEN
                        case 0xBC3E: Speeds.Add(("INK", H, L)); break; // SCR SET FLASHING
                        case 0xBB3F: Speeds.Add(("KEY", H, L)); break; // KM SET DELAY
                        default:                                       // MOVE, PLOT, LINE, TEST : le curseur va au point
                            GraphicsX = (short)((relative ? GraphicsX : 0) + (short)DE);
                            GraphicsY = (short)((relative ? GraphicsY : 0) + (short)HL);
                            if (address is 0xBBEA or 0xBBED)
                                Points[(GraphicsX, GraphicsY)] = GraphicsPen;
                            if (address is 0xBBF0 or 0xBBF3)
                                A = Points.TryGetValue((GraphicsX, GraphicsY), out byte ink) ? ink : (byte)0;
                            break;
                    }
                    break;
                }
                if (address == 0x001B)
                {
                    Push((ushort)pc);                                  // KL FAR PCHL : appel de HL (D089)
                    pc = HL;
                    break;
                }
                if (address == 0xBCD4)
                {
                    var name = new System.Text.StringBuilder();         // KL FIND COMMAND : dernière lettre avec le bit 7 (D089)
                    for (int at = HL; ; at++)
                    {
                        name.Append((char)(Memory[at] & 0x7F));
                        if ((Memory[at] & 0x80) != 0)
                            break;
                    }
                    CarryFlag = RsxCommands.TryGetValue(name.ToString(), out ushort routine);
                    if (CarryFlag)
                    {
                        HL = routine;
                        C = 0xFF;
                    }
                    break;
                }
                if (address >= 0xB900 && Memory[address] == 0xC3)
                {
                    Push((ushort)pc);                                  // entrée du firmware détournée (PRINT #9, D084)
                    pc = address;
                    break;
                }
                if (address is >= 0xBC77 and <= 0xBC9B or 0xBC68)
                {
                    switch (address)                                   // CAS ... (fichiers, D084) : retenue = réussi
                    {
                        case 0xBC8C:                                                                    // CAS OUT OPEN
                            outFileName = FileName();
                            fileOut = Files[outFileName] = new();
                            FileHeaders.Remove(outFileName);
                            CarryFlag = true;
                            break;
                        case 0xBC98:                                                                    // CAS OUT DIRECT (D085)
                            fileOut!.AddRange(Memory[HL..(HL + DE)]);
                            FileHeaders[outFileName] = (A, HL);
                            CarryFlag = true;
                            break;
                        case 0xBC83:                                                                    // CAS IN DIRECT (D085)
                            fileIn!.ToArray().CopyTo(Memory, HL);
                            CarryFlag = true;                          // HL = lancement : l'adresse de chargement ici
                            break;
                        case 0xBC7D: fileIn = null; CarryFlag = true; break;                            // CAS IN ABANDON
                        case 0xBC9B: Catalogs++; CarryFlag = true; break;                               // CAS CATALOG
                        case 0xBC68: TapeSpeeds.Add((HL, A)); break;                                    // CAS SET SPEED
                        case 0xBC95: fileOut!.Add(A); CarryFlag = true; break;                          // CAS OUT CHAR
                        case 0xBC8F: fileOut = null; CarryFlag = true; break;                           // CAS OUT CLOSE
                        case 0xBC77:                                                                    // CAS IN OPEN
                            inFileName = FileName();
                            CarryFlag = Files.TryGetValue(inFileName, out fileIn);
                            fileInPos = 0;
                            if (CarryFlag)
                            {
                                (byte type, int start) = FileHeaders.TryGetValue(inFileName, out var header) ? header : ((byte)0x16, 0);
                                A = type;                              // texte : &16
                                DE = (ushort)start;
                                BC = (ushort)fileIn!.Count;
                            }
                            else
                            {
                                A = 0x92;                              // AMSDOS : 146, fichier absent (DERR, D099)
                            }
                            break;
                        case 0xBC80:                                                                    // CAS IN CHAR
                            CarryFlag = fileInPos < fileIn!.Count;
                            if (CarryFlag)
                                A = fileIn[fileInPos++];
                            break;
                        case 0xBC86: fileInPos--; CarryFlag = true; break;                              // CAS RETURN
                        case 0xBC89: CarryFlag = fileInPos < fileIn!.Count; break;                      // CAS TEST EOF
                        case 0xBC7A: fileIn = null; CarryFlag = true; break;                            // CAS IN CLOSE
                        default: throw new InvalidOperationException($"Routine CAS non simulée : {address:X4}");
                    }
                    break;
                }
                if (address == 0xBB5A)
                {
                    TextOutput(A);                                     // TXT OUTPUT : caractère affiché
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
                    if (KeyQueue.Count == 0 && TimedKeys.Count > 0)
                    {
                        Ticks = Math.Max(Ticks, TimedKeys[0].Tick);   // on attend la prochaine touche tapée (D083)
                        byte key = TimedKeys[0].Key;
                        TimedKeys.RemoveAt(0);
                        PressKey(key);
                    }
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
                    string shown = StreamText(CurrentStream);          // TXT GET CURSOR : H = colonne, L = ligne
                    H = (byte)(shown.Length - shown.LastIndexOf('\n'));
                    L = (byte)(1 + shown.Count(c => c == '\n'));
                }
                else if (address == 0xBCEF)
                {
                    EventRoutine = DE;                                 // KL INIT EVENT (D079)
                    eventBlocks[HL] = DE;
                }
                else if (address == 0xBB75)
                {
                    CursorMoves.Add((H, L));                           // TXT SET CURSOR (D076)
                }
                else if (address == 0xBB8A || address == 0xBB8D)
                {
                    // TXT PLACE CURSOR / TXT REMOVE CURSOR : rien à simuler
                }
                else if (address == 0xBB60)
                {
                    (byte col, byte row) = CursorMoves.Count > 0 ? CursorMoves[^1] : ((byte)1, (byte)1);
                    CarryFlag = ScreenChars.TryGetValue((col, row), out byte shown);   // TXT RD CHAR (D076)
                    A = shown;
                }
                else if (address == 0xBBA8)
                {
                    CarryFlag = A >= FirstUserChar;                    // TXT SET MATRIX (D075)
                    if (CarryFlag)
                        Matrices.Add((A, Memory[HL..(HL + 8)]));
                }
                else if (address == 0xBBA5)
                {
                    HL = (ushort)(0x3800 + A * 8);                     // TXT GET MATRIX : dessin (mémoire vide ici)
                    MatrixReads++;
                }
                else if (address == 0xBBAB)
                {
                    FirstUserChar = DE;                                // TXT SET M TABLE (D075)
                    MatrixTables.Add((DE, HL));
                }
                else if (address == 0xB906)
                {
                    A = 0;                                             // KL L ROM ENABLE : état d'avant
                }
                else if (address == 0xBB63)
                {
                    Tags.Add(A != 0);                                  // TXT SET GRAPHIC (TAG, TAGOFF)
                }
                else if (address == 0xBB69)
                {
                    H = 0;                                             // TXT GET WINDOW : gauche, droite, haut, bas (D073)
                    D = (byte)(WindowWidth - 1);
                    L = 0;
                    E = 24;
                }
                else if (address == 0xBB87)
                {
                    if (H > WindowWidth)                               // TXT VALIDATE (D072) : au-delà du bord droit,
                    {                                                  // le caractère suivant ira à la ligne (D073)
                        L = (byte)(L + (H - 1) / WindowWidth);
                        H = (byte)((H - 1) % WindowWidth + 1);
                    }
                    CarryFlag = true;
                }
                else if (address == 0xBBB4)
                {
                    SelectStream();                                    // TXT STR SELECT (D072)
                }
                else if (address == 0xBB66)
                {
                    Windows.Add((CurrentStream, H, D, L, E));          // TXT WIN ENABLE (WINDOW, D072)
                }
                else if (address == 0xBBB7)
                {
                    Swaps.Add((B, C));                                 // TXT SWAP STREAMS (WINDOW SWAP, D072)
                }
                else if (address == 0xBCAA)
                {
                    if (SoundQueueModel)                               // SOUND QUEUE avec files (D082)
                    {
                        int channel = (Memory[HL] & 1) != 0 ? 0 : (Memory[HL] & 2) != 0 ? 1 : 2;
                        int ticks = Math.Max(1, (Memory[HL + 7] | (Memory[HL + 8] << 8)) / 2);
                        CarryFlag = soundWaiting[channel].Count < 4;
                        if (CarryFlag)
                        {
                            Sounds.Add(Memory[HL..(HL + 9)]);
                            if (soundPlaying[channel] == 0)
                                soundPlaying[channel] = ticks;
                            else
                                soundWaiting[channel].Enqueue(ticks);
                        }
                    }
                    else
                    {
                        Sounds.Add(Memory[HL..(HL + 9)]);              // SOUND QUEUE : toujours une place
                        CarryFlag = true;
                    }
                }
                else if (address == 0xBCB0)
                {
                    int channel = A == 1 ? 0 : A == 2 ? 1 : 2;         // SOUND ARM EVENT (ON SQ, D082)
                    soundArmed[channel] = HL;
                    KickSoundEvent(channel);
                }
                else if (address == 0xBCEC)
                {
                    TickRoutine = 0;                                   // KL DEL TICKER (RUN "nom", D086)
                }
                else if (address == 0xBCA7)
                {
                    SoundResets++;                                     // SOUND RESET (RUN "nom", D086)
                }
                else if (address == 0xBCAD)
                {
                    A = SoundStatus;                                   // SOUND CHECK
                }
                else if (address == 0xBCB3)
                {
                    Released.Add(A);                                   // SOUND RELEASE
                }
                else if (address == 0xBCBC || address == 0xBCBF)
                {
                    Envelopes.Add((address == 0xBCBF, A, Memory[HL..(HL + 16)]));   // ENV / ENT
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
                else if (address == 0xBD2B)
                {
                    Printed.Append((char)A);                           // MC PRINT CHAR : l'imprimante prend le caractère (D100)
                    CarryFlag = true;
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
            case 0x04: B++; ZeroFlag = B == 0; break;                  // INC B (D084)
            case 0x0E: C = Fetch(); break;                             // LD C,n
            case 0xB9: Compare(C); break;                              // CP C (D063)
            case 0x90: Sub(B); break;                                  // SUB B (D063)
            case 0x12: Memory[DE] = A; break;                          // LD (DE),A (D063)
            case 0xD6: Sub(Fetch()); break;                            // SUB n (D063)
            case 0x41: B = C; break;                                   // LD B,C (D063)
            case 0x60: H = B; break;                                   // LD H,B (D063)
            case 0x68: L = B; break;                                   // LD L,B (D065)
            case 0xC4:                                                 // CALL NZ,nn (D079)
            {
                ushort target = Fetch16();
                if (!ZeroFlag)
                {
                    Push((ushort)pc);
                    pc = target;
                }
                break;
            }
            case 0xF3: InterruptsEnabled = false; break;               // DI (D079)
            case 0xFB: InterruptsEnabled = true; break;                // EI (D079)
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
            case 0xDD:                                                 // IX (commandes RSX, D089)
            {
                byte next = Fetch();
                int At() => (IX + (sbyte)Fetch()) & 0xFFFF;
                switch (next)
                {
                    case 0x21: IX = Fetch16(); break;                  // LD IX,nn
                    case 0x39: IX = (ushort)(IX + sp); break;          // ADD IX,SP
                    case 0xE5: Push(IX); break;                        // PUSH IX
                    case 0xE1: IX = Pop(); break;                      // POP IX
                    case 0x7E: A = Memory[At()]; break;                // LD A,(IX+d)
                    case 0x6E: L = Memory[At()]; break;                // LD L,(IX+d)
                    case 0x66: H = Memory[At()]; break;                // LD H,(IX+d)
                    case 0x5E: E = Memory[At()]; break;                // LD E,(IX+d)
                    case 0x56: D = Memory[At()]; break;                // LD D,(IX+d)
                    default:
                        throw new NotSupportedException($"Instruction &DD &{next:X2} à l'adresse &{opcodeAddress:X4} : à ajouter à MiniZ80.");
                }
                break;
            }

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
            case 0x73:                                                 // LD (nn),SP (D080)
            {
                int at = Fetch16();
                Memory[at] = (byte)(sp & 0xFF);
                Memory[(at + 1) & 0xFFFF] = (byte)(sp >> 8);
                break;
            }
            case 0x7B:                                                 // LD SP,(nn) (D080)
            {
                int at = Fetch16();
                sp = Memory[at] | (Memory[(at + 1) & 0xFFFF] << 8);
                break;
            }
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
