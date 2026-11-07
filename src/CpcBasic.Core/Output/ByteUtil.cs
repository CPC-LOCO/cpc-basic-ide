// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Output;

/// <summary>Petits outils pour écrire des octets comme le fait le Z80.</summary>
internal static class ByteUtil
{
    /// <summary>Écrit un entier 16 bits, octet faible d'abord (« petit boutiste »).</summary>
    public static void SetWord(byte[] buffer, int position, int value)
    {
        buffer[position] = (byte)(value & 0xFF);
        buffer[position + 1] = (byte)((value >> 8) & 0xFF);
    }
}
