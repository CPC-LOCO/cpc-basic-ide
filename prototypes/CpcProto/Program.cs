// ============================================================
// Prototype J0 (étape 2) : programme Z80 + chargeur BASIC -> hello.dsk
// Sur la disquette : HELLO.BAS (chargeur) et HELLO.BIN (programme Z80).
// Dans WinAPE : RUN"HELLO"
// ============================================================
using System.Text;

const int LoadAddress = 0x4000;   // adresse de chargement du programme Z80

// ---------- 1. Le programme Z80 (écrit à la main) ----------
// 4000: 21 0C 40    LD HL,message
// 4003: 7E          boucle: LD A,(HL)
// 4004: B7          OR A
// 4005: C8          RET Z            ; fin du texte -> retour au BASIC (via CALL)
// 4006: CD 5A BB    CALL &BB5A       ; TXT OUTPUT (routine du firmware)
// 4009: 23          INC HL
// 400A: 18 F7       JR boucle
// 400C: message, terminé par 0
byte[] code =
[
    0x21, 0x0C, 0x40,
    0x7E,
    0xB7,
    0xC8,
    0xCD, 0x5A, 0xBB,
    0x23,
    0x18, 0xF7
];
byte[] message = Encoding.ASCII.GetBytes("HELLO CPC !\r\n\0");
byte[] program = [.. code, .. message];

// ---------- 2. Le chargeur BASIC (format « tokenisé » du Locomotive BASIC) ----------
// 10 MEMORY &3FFF
// 20 LOAD"HELLO.BIN"
// 30 CALL &4000
// Codes utilisés : &AA = MEMORY, &A8 = LOAD, &83 = CALL,
//                  &1C = constante hexadécimale sur 2 octets, &22 = guillemet, &20 = espace
byte[] loader =
[
    .. BasicLine(10, [0xAA, 0x20, 0x1C, 0xFF, 0x3F]),
    .. BasicLine(20, [0xA8, 0x22, .. Encoding.ASCII.GetBytes("HELLO.BIN"), 0x22]),
    .. BasicLine(30, [0x83, 0x20, 0x1C, 0x00, 0x40]),
    0x00, 0x00                      // fin du programme BASIC
];

// ---------- 3. Construction de la disquette ----------
var disk = new DiskImage();
disk.AddFile("HELLO", "BAS", 0, 0x0170, 0x0000, loader);          // type 0 = BASIC
disk.AddFile("HELLO", "BIN", 2, LoadAddress, LoadAddress, program); // type 2 = binaire
disk.Save("hello.dsk");

Console.WriteLine($"hello.dsk créé : chargeur BASIC {loader.Length} octets, programme Z80 {program.Length} octets.");

// Une ligne de programme BASIC tokenisé : longueur (2 octets), numéro (2 octets), codes, 0
static byte[] BasicLine(int number, byte[] tokens)
{
    int length = 2 + 2 + tokens.Length + 1;
    byte[] line = new byte[length];
    DiskImage.SetWord(line, 0, length);
    DiskImage.SetWord(line, 2, number);
    tokens.CopyTo(line, 4);
    return line;                    // le dernier octet reste à 0 (fin de ligne)
}

// ============================================================
// Image de disquette CPC (format DATA, 40 pistes, simple face)
// ============================================================
class DiskImage
{
    const int Tracks = 40;
    const int SectorsPerTrack = 9;
    const int SectorSize = 512;
    const int BlockSize = 1024;
    const int MaxEntries = 64;

    readonly byte[] data = new byte[Tracks * SectorsPerTrack * SectorSize];
    int nextBlock = 2;              // les blocs 0 et 1 sont réservés au répertoire
    int nextEntry = 0;

    public DiskImage() => Array.Fill(data, (byte)0xE5);   // 0xE5 = octet « vide » des disquettes CPC

    public void AddFile(string name, string ext, byte type, int loadAddress, int execAddress, byte[] content)
    {
        // En-tête AMSDOS de 128 octets
        byte[] header = new byte[128];
        Encoding.ASCII.GetBytes(name.PadRight(8)).CopyTo(header, 0x01);
        Encoding.ASCII.GetBytes(ext.PadRight(3)).CopyTo(header, 0x09);
        header[0x12] = type;
        SetWord(header, 0x13, content.Length);
        SetWord(header, 0x15, loadAddress);
        SetWord(header, 0x18, content.Length);
        SetWord(header, 0x1A, execAddress);
        header[0x40] = (byte)(content.Length & 0xFF);      // longueur réelle sur 3 octets
        header[0x41] = (byte)((content.Length >> 8) & 0xFF);
        header[0x42] = 0;
        int checksum = 0;
        for (int i = 0; i < 0x43; i++) checksum += header[i];
        SetWord(header, 0x43, checksum);

        byte[] file = [.. header, .. content];
        int blocks = (file.Length + BlockSize - 1) / BlockSize;
        if (blocks > 16)
            throw new InvalidOperationException("Prototype : un fichier ne peut pas dépasser 16 Ko.");
        if (nextEntry >= MaxEntries || (nextBlock + blocks) * BlockSize > data.Length)
            throw new InvalidOperationException("Disquette pleine.");

        // Données du fichier
        file.CopyTo(data, nextBlock * BlockSize);

        // Entrée du répertoire (32 octets)
        int e = nextEntry * 32;
        Array.Clear(data, e, 32);
        Encoding.ASCII.GetBytes(name.PadRight(8)).CopyTo(data, e + 1);
        Encoding.ASCII.GetBytes(ext.PadRight(3)).CopyTo(data, e + 9);
        data[e + 15] = (byte)((file.Length + 127) / 128);  // nombre d'enregistrements de 128 octets
        for (int b = 0; b < blocks; b++)
            data[e + 16 + b] = (byte)(nextBlock + b);      // blocs occupés

        nextBlock += blocks;
        nextEntry++;
    }

    public void Save(string path)
    {
        int trackSize = 256 + SectorsPerTrack * SectorSize;   // 0x1300
        byte[] dskHeader = new byte[256];
        Encoding.ASCII.GetBytes("MV - CPCEMU Disk-File\r\nDisk-Info\r\n").CopyTo(dskHeader, 0);
        Encoding.ASCII.GetBytes("CpcProto").CopyTo(dskHeader, 0x22);
        dskHeader[0x30] = Tracks;
        dskHeader[0x31] = 1;                                  // une face
        SetWord(dskHeader, 0x32, trackSize);

        using var fs = File.Create(path);
        fs.Write(dskHeader);
        for (int t = 0; t < Tracks; t++)
        {
            byte[] info = new byte[256];
            Encoding.ASCII.GetBytes("Track-Info\r\n").CopyTo(info, 0);
            info[0x10] = (byte)t;
            info[0x11] = 0;
            info[0x14] = 2;                                   // taille secteur : 2 = 512 octets
            info[0x15] = SectorsPerTrack;
            info[0x16] = 0x4E;                                // GAP3
            info[0x17] = 0xE5;                                // octet de remplissage
            for (int s = 0; s < SectorsPerTrack; s++)
            {
                int p = 0x18 + s * 8;
                info[p + 0] = (byte)t;                        // piste
                info[p + 1] = 0;                              // face
                info[p + 2] = (byte)(0xC1 + s);               // secteur C1..C9
                info[p + 3] = 2;                              // taille 512
                SetWord(info, p + 6, SectorSize);
            }
            fs.Write(info);
            fs.Write(data, t * SectorsPerTrack * SectorSize, SectorsPerTrack * SectorSize);
        }
    }

    // Écrit un entier 16 bits octet faible d'abord, comme le Z80
    public static void SetWord(byte[] buffer, int position, int value)
    {
        buffer[position] = (byte)(value & 0xFF);
        buffer[position + 1] = (byte)((value >> 8) & 0xFF);
    }
}
