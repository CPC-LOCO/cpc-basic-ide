// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;

namespace CpcBasic.Core.Output;

/// <summary>
/// Image de cassette CPC au format .cdt (le format TZX, lu par WinAPE, SugarBox et les lecteurs de cassette pour PC).
/// Chaque fichier est coupé en morceaux de 2 Ko, comme le fait le firmware du CPC. Chaque morceau donne deux
/// « enregistrements » sur la bande :
///   - l'en-tête (octet de reconnaissance &amp;2C) : nom, numéro du morceau, type, longueur, adresses ;
///   - les données (octet de reconnaissance &amp;16).
/// Un enregistrement = une amorce (2048 bits à 1), un bit à 0, l'octet de reconnaissance, les données par paquets de
/// 256 octets suivis chacun de leur somme de contrôle (CRC), puis 32 bits à 1 de fin.
/// </summary>
public sealed class TapeImage
{
    /// <summary>Taille d'un morceau de fichier sur la bande (comme le firmware).</summary>
    public const int BlockSize = 2048;

    /// <summary>Taille d'un paquet de données, suivi de sa somme de contrôle.</summary>
    public const int SegmentSize = 256;

    /// <summary>Longueur d'un nom de fichier sur la bande.</summary>
    public const int NameLength = 16;

    /// <summary>Horloge de référence du format TZX (3,5 MHz), pour la durée des impulsions.</summary>
    const int TzxClock = 3_500_000;

    const byte HeaderSync = 0x2C;
    const byte DataSync = 0x16;
    const int PilotPulses = 4096;              // 2048 bits à 1 = 4096 impulsions
    const int PauseAfterHeaderMs = 20;         // en-tête et données se lisent d'un trait, moteur en marche

    /// <summary>
    /// Silence avant chaque en-tête (D178, essai dans WinAPE) : entre deux morceaux, le CPC arrête le moteur, puis le
    /// remet en marche et attend environ 2 secondes que la bande prenne sa vitesse avant d'écouter. Sans ce silence,
    /// l'amorce du morceau suivant passe pendant cette attente et le morceau est perdu.
    /// </summary>
    public const int MotorPauseMs = 2500;

    readonly List<(string Name, AmsdosFileType Type, int Load, int Exec, byte[] Content)> files = new();

    /// <param name="baud">Vitesse d'écriture : 1000 (SPEED WRITE 0) ou 2000 (SPEED WRITE 1, par défaut). Le CPC
    /// reconnaît la vitesse tout seul en lisant l'amorce.</param>
    public TapeImage(int baud = 2000)
    {
        if (baud is < 500 or > 4000)
            throw new ArgumentOutOfRangeException(nameof(baud), "Vitesse de la cassette : de 500 à 4000 bauds.");
        Baud = baud;
    }

    /// <summary>Vitesse d'écriture en bauds.</summary>
    public int Baud { get; }

    /// <summary>Les fichiers de la cassette, dans l'ordre de la bande.</summary>
    public IReadOnlyList<string> FileNames => files.Select(f => f.Name).ToList();

    /// <summary>Ajoute un fichier à la fin de la bande.</summary>
    /// <param name="name">Nom sur la bande, 16 caractères au plus (mis en majuscules), par exemple « JEU » ou « JEU.BIN ».</param>
    public void AddFile(string name, AmsdosFileType type, int loadAddress, int execAddress, byte[] content)
    {
        string clean = name.Trim().ToUpperInvariant();
        if (clean.Length == 0)
            throw new ArgumentException("Le nom du fichier est vide.", nameof(name));
        if (clean.Length > NameLength)
            throw new ArgumentException($"« {name} » dépasse {NameLength} caractères.", nameof(name));
        if (content.Length == 0 || content.Length > 0xFFFF)
            throw new ArgumentException("Un fichier de cassette fait de 1 à 65535 octets.", nameof(content));
        files.Add((clean, type, loadAddress, execAddress, content));
    }

    /// <summary>Produit le contenu complet du fichier .cdt.</summary>
    public byte[] ToCdtBytes()
    {
        using var ms = new MemoryStream();
        ms.Write("ZXTape!"u8);
        ms.WriteByte(0x1A);
        ms.WriteByte(1);                                   // version 1.20 du format TZX
        ms.WriteByte(20);
        WritePause(ms, MotorPauseMs);                      // silence de départ (moteur qui démarre)

        foreach ((string name, AmsdosFileType type, int load, int exec, byte[] content) in files)
        {
            int count = (content.Length + BlockSize - 1) / BlockSize;
            for (int b = 0; b < count; b++)
            {
                int start = b * BlockSize;
                int length = Math.Min(BlockSize, content.Length - start);
                byte[] header = BlockHeader(name, b + 1, b == 0, b == count - 1, type, length,
                                            load + start, content.Length, exec);
                WriteTurboBlock(ms, Record(HeaderSync, header), PauseAfterHeaderMs);
                WriteTurboBlock(ms, Record(DataSync, content.AsSpan(start, length)),
                                MotorPauseMs);
            }
        }
        return ms.ToArray();
    }

    /// <summary>Écrit le fichier .cdt sur le disque.</summary>
    public void Save(string path) => File.WriteAllBytes(path, ToCdtBytes());

    /// <summary>En-tête d'un morceau (64 octets), tel que le firmware l'écrit.</summary>
    public static byte[] BlockHeader(string name, int blockNumber, bool first, bool last, AmsdosFileType type,
                                       int blockLength, int blockAddress, int totalLength, int execAddress)
    {
        byte[] h = new byte[64];
        Encoding.ASCII.GetBytes(name).CopyTo(h, 0);         // nom complété par des 0
        h[16] = (byte)blockNumber;
        h[17] = (byte)(last ? 0xFF : 0x00);
        h[18] = (byte)type;
        ByteUtil.SetWord(h, 19, blockLength);
        ByteUtil.SetWord(h, 21, blockAddress);
        h[23] = (byte)(first ? 0xFF : 0x00);
        ByteUtil.SetWord(h, 24, totalLength);
        ByteUtil.SetWord(h, 26, execAddress);
        return h;
    }

    /// <summary>
    /// Un enregistrement : octet de reconnaissance, paquets de 256 octets (le dernier complété par des 0) suivis
    /// chacun de leur somme de contrôle (octet fort d'abord), puis 4 octets &amp;FF (32 bits à 1 de fin).
    /// </summary>
    public static byte[] Record(byte sync, ReadOnlySpan<byte> data)
    {
        int segments = Math.Max(1, (data.Length + SegmentSize - 1) / SegmentSize);
        var bytes = new List<byte>(1 + segments * (SegmentSize + 2) + 4) { sync };
        byte[] segment = new byte[SegmentSize];
        for (int s = 0; s < segments; s++)
        {
            Array.Clear(segment);
            int start = s * SegmentSize;
            if (start < data.Length)
                data[start..Math.Min(data.Length, start + SegmentSize)].CopyTo(segment);
            ushort crc = Crc(segment);
            bytes.AddRange(segment);
            bytes.Add((byte)(crc >> 8));
            bytes.Add((byte)(crc & 0xFF));
        }
        bytes.AddRange([0xFF, 0xFF, 0xFF, 0xFF]);
        return bytes.ToArray();
    }

    /// <summary>Somme de contrôle du firmware : CRC-16 CCITT (polynôme &amp;1021, départ &amp;FFFF), inversée à la fin.</summary>
    public static ushort Crc(ReadOnlySpan<byte> data)
    {
        int crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b << 8;
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? ((crc << 1) ^ 0x1021) & 0xFFFF : (crc << 1) & 0xFFFF;
        }
        return (ushort)(crc ^ 0xFFFF);
    }

    // Bloc TZX n° &11 (« vitesse au choix ») : durées des impulsions, amorce, puis les octets, bit fort d'abord.
    // Un bit à 0 dure deux impulsions courtes, un bit à 1 deux impulsions deux fois plus longues ;
    // à 1000 bauds, une impulsion courte dure 333 µs (comme SPEED WRITE 0).
    void WriteTurboBlock(Stream s, byte[] data, int pauseMs)
    {
        int zero = (int)Math.Round(TzxClock / (3.0 * Baud));
        int one = 2 * zero;
        s.WriteByte(0x11);
        WriteWord(s, one);                                 // amorce : bits à 1
        WriteWord(s, zero);                                // bit à 0 qui termine l'amorce (deux impulsions)
        WriteWord(s, zero);
        WriteWord(s, zero);                                // bit à 0
        WriteWord(s, one);                                 // bit à 1
        WriteWord(s, PilotPulses);
        s.WriteByte(8);                                    // tous les bits du dernier octet sont utilisés
        WriteWord(s, pauseMs);
        s.WriteByte((byte)(data.Length & 0xFF));
        s.WriteByte((byte)((data.Length >> 8) & 0xFF));
        s.WriteByte((byte)((data.Length >> 16) & 0xFF));
        s.Write(data);
    }

    // Bloc TZX n° &20 : un silence de la durée donnée
    static void WritePause(Stream s, int pauseMs)
    {
        s.WriteByte(0x20);
        WriteWord(s, pauseMs);
    }

    static void WriteWord(Stream s, int value)
    {
        s.WriteByte((byte)(value & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
