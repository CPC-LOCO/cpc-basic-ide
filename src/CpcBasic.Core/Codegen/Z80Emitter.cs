// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Codegen;

/// <summary>
/// Petit assembleur Z80 : on écrit les instructions une par une, avec des étiquettes (labels)
/// dont les adresses sont calculées à la fin. Seules les instructions utiles au compilateur
/// existent pour l'instant ; on en ajoutera au fil des besoins.
/// </summary>
public sealed class Z80Emitter
{
    readonly record struct Fixup(int Position, string Label, bool Relative);

    readonly List<byte> code = new();
    readonly Dictionary<string, int> labels = new();
    readonly List<Fixup> fixups = new();

    /// <summary>Adresse de la première instruction dans la mémoire du CPC.</summary>
    public int Origin { get; }

    /// <summary>Nombre d'octets écrits jusqu'ici.</summary>
    public int Size => code.Count;

    /// <summary>Adresse d'une étiquette déjà placée, dans la mémoire du CPC (utile pour les tests).</summary>
    public int AddressOf(string label) =>
        labels.TryGetValue(label, out int position)
            ? Origin + position
            : throw new InvalidOperationException($"Étiquette inconnue : {label}");

    public Z80Emitter(int origin) => Origin = origin;

    /// <summary>Place une étiquette à l'endroit actuel.</summary>
    public void Label(string name)
    {
        if (!labels.TryAdd(name, code.Count))
            throw new InvalidOperationException($"Étiquette en double : {name}");
    }

    /// <summary>Donne un second nom à l'adresse d'une étiquette déjà placée.</summary>
    public void AliasLabel(string alias, string existing)
    {
        if (!labels.TryGetValue(existing, out int position))
            throw new InvalidOperationException($"Étiquette inconnue : {existing}");
        if (!labels.TryAdd(alias, position))
            throw new InvalidOperationException($"Étiquette en double : {alias}");
    }

    /// <summary>Écrit des octets tels quels.</summary>
    public void Bytes(params byte[] values) => code.AddRange(values);

    /// <summary>Écrit un bloc de données.</summary>
    public void Data(byte[] data) => code.AddRange(data);

    /// <summary>Réserve des octets initialisés à 0 (variables).</summary>
    public void Reserve(int count) => code.AddRange(new byte[count]);

    // ----- Instructions -----

    public void LdHlLabel(string label) { code.Add(0x21); AddAbsolute(label); }   // LD HL,étiquette
    public void LdHlImm(int value) { code.Add(0x21); AddWord(value); }             // LD HL,nombre
    public void LdDeImm(int value) { code.Add(0x11); AddWord(value); }             // LD DE,nombre
    public void LdAImm(int value) { code.Add(0x3E); code.Add((byte)value); }       // LD A,n
    public void LdBImm(int value) { code.Add(0x06); code.Add((byte)value); }       // LD B,n
    public void CpImm(int value) { code.Add(0xFE); code.Add((byte)value); }        // CP n

    public void LdHlMem(string label) { code.Add(0x2A); AddAbsolute(label); }      // LD HL,(étiquette)
    public void LdMemHl(string label) { code.Add(0x22); AddAbsolute(label); }      // LD (étiquette),HL
    public void LdAMem(string label) { code.Add(0x3A); AddAbsolute(label); }       // LD A,(étiquette)
    public void LdMemA(string label) { code.Add(0x32); AddAbsolute(label); }       // LD (étiquette),A

    public void LdAHl() => code.Add(0x7E);                                         // LD A,(HL)
    public void LdLA() => code.Add(0x6F);                                          // LD L,A
    public void LdHA() => code.Add(0x67);                                          // LD H,A
    public void LdHZero() { code.Add(0x26); code.Add(0x00); }                      // LD H,0
    public void LdAL() => code.Add(0x7D);                                          // LD A,L
    public void LdCA() => code.Add(0x4F);                                          // LD C,A
    public void LdAB() => code.Add(0x78);                                          // LD A,B
    public void LdAC() => code.Add(0x79);                                          // LD A,C

    public void PushHl() => code.Add(0xE5);                                        // PUSH HL
    public void PopHl() => code.Add(0xE1);                                         // POP HL
    public void PushDe() => code.Add(0xD5);                                        // PUSH DE
    public void PopDe() => code.Add(0xD1);                                         // POP DE
    public void PushBc() => code.Add(0xC5);                                        // PUSH BC
    public void PopBc() => code.Add(0xC1);                                         // POP BC

    public void AddHlDe() => code.Add(0x19);                                       // ADD HL,DE
    public void SbcHlDe() { code.Add(0xED); code.Add(0x52); }                      // SBC HL,DE
    public void ExDeHl() => code.Add(0xEB);                                        // EX DE,HL
    public void Bit7H() { code.Add(0xCB); code.Add(0x7C); }                        // BIT 7,H

    public void XorA() => code.Add(0xAF);                                          // XOR A
    public void SubL() => code.Add(0x95);                                          // SUB L
    public void SubH() => code.Add(0x94);                                          // SUB H
    public void SbcAA() => code.Add(0x9F);                                         // SBC A,A
    public void IncA() => code.Add(0x3C);                                          // INC A
    public void LdAH() => code.Add(0x7C);                                          // LD A,H
    public void LdLH() => code.Add(0x6C);                                          // LD L,H
    public void OrL() => code.Add(0xB5);                                           // OR L
    public void XorD() => code.Add(0xAA);                                          // XOR D
    public void XorE() => code.Add(0xAB);                                          // XOR E
    public void SrlH() { code.Add(0xCB); code.Add(0x3C); }                         // SRL H
    public void RrL() { code.Add(0xCB); code.Add(0x1D); }                          // RR L
    public void Res7H() { code.Add(0xCB); code.Add(0xBC); }                        // RES 7,H
    public void RetC() => code.Add(0xD8);                                          // RET C
    public void RetNc() => code.Add(0xD0);                                         // RET NC
    public void LdBH() => code.Add(0x44);                                          // LD B,H
    public void LdCL() => code.Add(0x4D);                                          // LD C,L
    public void LdLC() => code.Add(0x69);                                          // LD L,C
    public void LdBA() => code.Add(0x47);                                          // LD B,A
    public void LdEA() => code.Add(0x5F);                                          // LD E,A
    public void OrE() => code.Add(0xB3);                                           // OR E
    public void SubE() => code.Add(0x93);                                          // SUB E
    public void SubD() => code.Add(0x92);                                          // SUB D
    public void DecA() => code.Add(0x3D);                                          // DEC A
    public void DecC() => code.Add(0x0D);                                          // DEC C
    public void Scf() => code.Add(0x37);                                           // SCF
    public void Rla() => code.Add(0x17);                                           // RLA
    public void RlC() { code.Add(0xCB); code.Add(0x11); }                          // RL C
    public void RlD() { code.Add(0xCB); code.Add(0x12); }                          // RL D
    public void SlaE() { code.Add(0xCB); code.Add(0x23); }                         // SLA E
    public void Bit7D() { code.Add(0xCB); code.Add(0x7A); }                        // BIT 7,D
    public void Bit0B() { code.Add(0xCB); code.Add(0x40); }                        // BIT 0,B
    public void Bit1B() { code.Add(0xCB); code.Add(0x48); }                        // BIT 1,B
    public void AddHlHl() => code.Add(0x29);                                       // ADD HL,HL
    public void AddHlBc() => code.Add(0x09);                                       // ADD HL,BC
    public void AdcHlHl() { code.Add(0xED); code.Add(0x6A); }                      // ADC HL,HL
    public void LdAD() => code.Add(0x7A);                                          // LD A,D
    public void LdDA() => code.Add(0x57);                                          // LD D,A
    public void XorImm(int value) { code.Add(0xEE); code.Add((byte)value); }       // XOR n
    public void AdcHlDe() { code.Add(0xED); code.Add(0x5A); }                      // ADC HL,DE
    public void OrA() => code.Add(0xB7);                                           // OR A
    public void IncHl() => code.Add(0x23);                                         // INC HL

    public void Ret() => code.Add(0xC9);                                           // RET
    public void RetZ() => code.Add(0xC8);                                          // RET Z
    public void CallAddress(int address) { code.Add(0xCD); AddWord(address); }     // CALL adresse
    public void CallLabel(string label) { code.Add(0xCD); AddAbsolute(label); }    // CALL étiquette
    public void JpAddress(int address) { code.Add(0xC3); AddWord(address); }       // JP adresse
    public void JpLabel(string label) { code.Add(0xC3); AddAbsolute(label); }      // JP étiquette
    public void JpNcLabel(string label) { code.Add(0xD2); AddAbsolute(label); }    // JP NC,étiquette
    public void JpCLabel(string label) { code.Add(0xDA); AddAbsolute(label); }     // JP C,étiquette
    public void JpZLabel(string label) { code.Add(0xCA); AddAbsolute(label); }     // JP Z,étiquette
    public void JpNzLabel(string label) { code.Add(0xC2); AddAbsolute(label); }    // JP NZ,étiquette
    public void JpPeLabel(string label) { code.Add(0xEA); AddAbsolute(label); }    // JP PE,étiquette

    /// <summary>JR étiquette : saut relatif, de -128 à +127 octets.</summary>
    public void JrLabel(string label) => Relative(0x18, label);

    public void JrZLabel(string label) => Relative(0x28, label);                   // JR Z,étiquette
    public void JrNzLabel(string label) => Relative(0x20, label);                  // JR NZ,étiquette
    public void JrNcLabel(string label) => Relative(0x30, label);                  // JR NC,étiquette
    public void DjnzLabel(string label) => Relative(0x10, label);                  // DJNZ étiquette

    void Relative(byte opcode, string label)
    {
        code.Add(opcode);
        fixups.Add(new Fixup(code.Count, label, true));
        code.Add(0);
    }

    // ----- Résultat -----

    /// <summary>Calcule toutes les adresses et renvoie le code final.</summary>
    public byte[] ToBytes()
    {
        byte[] result = code.ToArray();

        foreach (Fixup f in fixups)
        {
            if (!labels.TryGetValue(f.Label, out int target))
                throw new InvalidOperationException($"Étiquette inconnue : {f.Label}");

            if (f.Relative)
            {
                int offset = target - (f.Position + 1);
                if (offset < -128 || offset > 127)
                    throw new InvalidOperationException($"Saut trop long vers {f.Label} ({offset} octets).");
                result[f.Position] = (byte)(sbyte)offset;
            }
            else
            {
                int address = Origin + target;
                result[f.Position] = (byte)(address & 0xFF);
                result[f.Position + 1] = (byte)((address >> 8) & 0xFF);
            }
        }

        return result;
    }

    void AddWord(int value)
    {
        code.Add((byte)(value & 0xFF));
        code.Add((byte)((value >> 8) & 0xFF));
    }

    void AddAbsolute(string label)
    {
        fixups.Add(new Fixup(code.Count, label, false));
        code.Add(0);
        code.Add(0);
    }
}
