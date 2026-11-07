// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;

namespace CpcBasic.Core.Output;

/// <summary>Type d'un fichier AMSDOS, tel qu'écrit dans son en-tête.</summary>
public enum AmsdosFileType : byte
{
    Basic = 0,
    Binary = 2
}

/// <summary>
/// Image de disquette CPC au format DATA : 40 pistes, une face, 9 secteurs de 512 octets.
/// Chaque fichier est précédé d'un en-tête AMSDOS de 128 octets.
/// </summary>
public sealed class DiskImage
{
    public const int Tracks = 40;
    public const int SectorsPerTrack = 9;
    public const int SectorSize = 512;
    public const int BlockSize = 1024;
    public const int MaxDirectoryEntries = 64;
    public const int FileHeaderSize = 128;

    /// <summary>Taille d'une piste dans le fichier .dsk : 256 octets d'en-tête + les secteurs.</summary>
    public const int DskTrackSize = 256 + SectorsPerTrack * SectorSize;

    /// <summary>Taille totale du fichier .dsk produit.</summary>
    public const int DskFileSize = 256 + Tracks * DskTrackSize;

    const int FirstFileBlock = 2;      // les blocs 0 et 1 sont réservés au répertoire
    const int MaxBlocksPerFile = 16;   // une seule entrée de répertoire par fichier : 16 Ko maximum

    readonly byte[] data = new byte[Tracks * SectorsPerTrack * SectorSize];
    int nextBlock = FirstFileBlock;
    int nextEntry;

    public DiskImage() => Array.Fill(data, (byte)0xE5);   // 0xE5 = octet « vide » des disquettes CPC

    /// <summary>Ajoute un fichier à la disquette.</summary>
    /// <param name="name">Nom, 8 caractères au plus (mis en majuscules).</param>
    /// <param name="extension">Extension, 3 caractères au plus (mise en majuscules).</param>
    public void AddFile(string name, string extension, AmsdosFileType type,
                        int loadAddress, int execAddress, byte[] content)
    {
        string cleanName = CleanName(name, 8, nameof(name));
        string cleanExt = CleanName(extension, 3, nameof(extension));

        byte[] header = BuildHeader(cleanName, cleanExt, type, loadAddress, execAddress, content.Length);
        byte[] file = [.. header, .. content];

        int blocks = (file.Length + BlockSize - 1) / BlockSize;
        if (blocks > MaxBlocksPerFile)
            throw new InvalidOperationException("Un fichier ne peut pas dépasser 16 Ko pour l'instant.");
        if (nextEntry >= MaxDirectoryEntries || (nextBlock + blocks) * BlockSize > data.Length)
            throw new InvalidOperationException("Disquette pleine.");

        // Données du fichier
        file.CopyTo(data, nextBlock * BlockSize);

        // Entrée du répertoire (32 octets)
        int e = nextEntry * 32;
        Array.Clear(data, e, 32);
        Encoding.ASCII.GetBytes(cleanName.PadRight(8)).CopyTo(data, e + 1);
        Encoding.ASCII.GetBytes(cleanExt.PadRight(3)).CopyTo(data, e + 9);
        data[e + 15] = (byte)((file.Length + 127) / 128);   // enregistrements de 128 octets
        for (int b = 0; b < blocks; b++)
            data[e + 16 + b] = (byte)(nextBlock + b);       // blocs occupés

        nextBlock += blocks;
        nextEntry++;
    }

    /// <summary>Produit le contenu complet du fichier .dsk.</summary>
    public byte[] ToDskBytes()
    {
        using var ms = new MemoryStream(DskFileSize);

        byte[] dskHeader = new byte[256];
        Encoding.ASCII.GetBytes("MV - CPCEMU Disk-File\r\nDisk-Info\r\n").CopyTo(dskHeader, 0);
        Encoding.ASCII.GetBytes("CpcBasic").CopyTo(dskHeader, 0x22);
        dskHeader[0x30] = Tracks;
        dskHeader[0x31] = 1;                                // une face
        ByteUtil.SetWord(dskHeader, 0x32, DskTrackSize);
        ms.Write(dskHeader);

        for (int t = 0; t < Tracks; t++)
        {
            byte[] info = new byte[256];
            Encoding.ASCII.GetBytes("Track-Info\r\n").CopyTo(info, 0);
            info[0x10] = (byte)t;
            info[0x11] = 0;
            info[0x14] = 2;                                 // taille secteur : 2 = 512 octets
            info[0x15] = SectorsPerTrack;
            info[0x16] = 0x4E;                              // GAP3
            info[0x17] = 0xE5;                              // octet de remplissage
            for (int s = 0; s < SectorsPerTrack; s++)
            {
                int p = 0x18 + s * 8;
                info[p + 0] = (byte)t;                      // piste
                info[p + 1] = 0;                            // face
                info[p + 2] = (byte)(0xC1 + s);             // secteur C1..C9
                info[p + 3] = 2;                            // taille 512
                ByteUtil.SetWord(info, p + 6, SectorSize);
            }
            ms.Write(info);
            ms.Write(data, t * SectorsPerTrack * SectorSize, SectorsPerTrack * SectorSize);
        }

        return ms.ToArray();
    }

    /// <summary>Écrit le fichier .dsk sur le disque.</summary>
    public void Save(string path) => File.WriteAllBytes(path, ToDskBytes());

    static string CleanName(string value, int maxLength, string parameter)
    {
        string clean = value.Trim().ToUpperInvariant();
        if (clean.Length == 0 && parameter == "name")
            throw new ArgumentException("Le nom du fichier est vide.", parameter);
        if (clean.Length > maxLength)
            throw new ArgumentException($"« {value} » dépasse {maxLength} caractères.", parameter);
        return clean;
    }

    static byte[] BuildHeader(string name, string ext, AmsdosFileType type,
                              int loadAddress, int execAddress, int length)
    {
        byte[] h = new byte[FileHeaderSize];
        Encoding.ASCII.GetBytes(name.PadRight(8)).CopyTo(h, 0x01);
        Encoding.ASCII.GetBytes(ext.PadRight(3)).CopyTo(h, 0x09);
        h[0x12] = (byte)type;
        ByteUtil.SetWord(h, 0x13, length);
        ByteUtil.SetWord(h, 0x15, loadAddress);
        ByteUtil.SetWord(h, 0x18, length);
        ByteUtil.SetWord(h, 0x1A, execAddress);
        h[0x40] = (byte)(length & 0xFF);                    // longueur réelle sur 3 octets
        h[0x41] = (byte)((length >> 8) & 0xFF);
        h[0x42] = (byte)((length >> 16) & 0xFF);

        int checksum = 0;
        for (int i = 0; i < 0x43; i++) checksum += h[i];
        ByteUtil.SetWord(h, 0x43, checksum);
        return h;
    }
}
