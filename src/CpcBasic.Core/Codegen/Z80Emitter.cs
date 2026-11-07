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

    public Z80Emitter(int origin) => Origin = origin;

    /// <summary>Place une étiquette à l'endroit actuel.</summary>
    public void Label(string name)
    {
        if (!labels.TryAdd(name, code.Count))
            throw new InvalidOperationException($"Étiquette en double : {name}");
    }

    /// <summary>Écrit des octets tels quels.</summary>
    public void Bytes(params byte[] values) => code.AddRange(values);

    /// <summary>Écrit un bloc de données.</summary>
    public void Data(byte[] data) => code.AddRange(data);

    // ----- Instructions -----

    public void LdHlLabel(string label) { code.Add(0x21); AddAbsolute(label); }   // LD HL,étiquette
    public void LdAHl() => code.Add(0x7E);                                         // LD A,(HL)
    public void OrA() => code.Add(0xB7);                                           // OR A
    public void IncHl() => code.Add(0x23);                                         // INC HL
    public void Ret() => code.Add(0xC9);                                           // RET
    public void RetZ() => code.Add(0xC8);                                          // RET Z
    public void CallAddress(int address) { code.Add(0xCD); AddWord(address); }     // CALL adresse
    public void CallLabel(string label) { code.Add(0xCD); AddAbsolute(label); }    // CALL étiquette
    public void JpAddress(int address) { code.Add(0xC3); AddWord(address); }       // JP adresse
    public void JpLabel(string label) { code.Add(0xC3); AddAbsolute(label); }      // JP étiquette

    /// <summary>JR étiquette : saut relatif, de -128 à +127 octets.</summary>
    public void JrLabel(string label)
    {
        code.Add(0x18);
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
