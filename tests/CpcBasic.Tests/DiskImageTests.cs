// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

public class DiskImageTests
{
    const int TrackBytes = DiskImage.SectorsPerTrack * DiskImage.SectorSize;   // 4608 octets utiles par piste
    const int FirstFileOffset = 2 * DiskImage.BlockSize;                       // le bloc 2 démarre à 2048

    // Lit un octet de la disquette « logique » (pistes à la suite) à l'intérieur du fichier .dsk
    static byte At(byte[] dsk, int logicalOffset)
    {
        int track = logicalOffset / TrackBytes;
        int inTrack = logicalOffset % TrackBytes;
        return dsk[256 + track * DiskImage.DskTrackSize + 256 + inTrack];
    }

    static string TextAt(byte[] dsk, int logicalOffset, int length)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < length; i++) sb.Append((char)At(dsk, logicalOffset + i));
        return sb.ToString();
    }

    static byte[] DiskWithOneFile(byte[] content)
    {
        var disk = new DiskImage();
        disk.AddFile("hello", "bin", AmsdosFileType.Binary, 0x4000, 0x4000, content);
        return disk.ToDskBytes();
    }

    [Fact]
    public void Dsk_has_standard_signature_and_size()
    {
        byte[] dsk = new DiskImage().ToDskBytes();

        Assert.Equal("MV - CPCEMU Disk-File", Encoding.ASCII.GetString(dsk, 0, 21));
        Assert.Equal(DiskImage.DskFileSize, dsk.Length);
        Assert.Equal(40, dsk[0x30]);       // nombre de pistes
        Assert.Equal(1, dsk[0x31]);        // une face
        Assert.Equal(0x00, dsk[0x32]);     // taille d'une piste : &1300
        Assert.Equal(0x13, dsk[0x33]);
    }

    [Fact]
    public void Empty_disk_is_filled_with_E5()
    {
        byte[] dsk = new DiskImage().ToDskBytes();

        Assert.Equal(0xE5, At(dsk, 0));
        Assert.Equal(0xE5, At(dsk, FirstFileOffset));
    }

    [Fact]
    public void Directory_entry_describes_the_file()
    {
        byte[] dsk = DiskWithOneFile([1, 2, 3, 4, 5]);

        Assert.Equal(0, At(dsk, 0));                         // utilisateur 0
        Assert.Equal("HELLO   BIN", TextAt(dsk, 1, 11));     // nom en majuscules, complété par des espaces
        Assert.Equal(2, At(dsk, 15));                        // 133 octets = 2 enregistrements de 128
        Assert.Equal(2, At(dsk, 16));                        // premier bloc libre = bloc 2
        Assert.Equal(0, At(dsk, 17));                        // un seul bloc utilisé
    }

    [Fact]
    public void Amsdos_header_is_correct()
    {
        byte[] content = [1, 2, 3, 4, 5];
        byte[] dsk = DiskWithOneFile(content);

        Assert.Equal(2, At(dsk, FirstFileOffset + 0x12));    // type binaire
        Assert.Equal(0x00, At(dsk, FirstFileOffset + 0x15)); // adresse de chargement &4000
        Assert.Equal(0x40, At(dsk, FirstFileOffset + 0x16));
        Assert.Equal(0x00, At(dsk, FirstFileOffset + 0x1A)); // adresse d'exécution &4000
        Assert.Equal(0x40, At(dsk, FirstFileOffset + 0x1B));
        Assert.Equal(5, At(dsk, FirstFileOffset + 0x40));    // longueur réelle

        int sum = 0;
        for (int i = 0; i < 0x43; i++) sum += At(dsk, FirstFileOffset + i);
        Assert.Equal(sum & 0xFF, At(dsk, FirstFileOffset + 0x43));
        Assert.Equal((sum >> 8) & 0xFF, At(dsk, FirstFileOffset + 0x44));
    }

    [Fact]
    public void File_content_follows_the_header()
    {
        byte[] dsk = DiskWithOneFile([10, 20, 30, 40, 50]);

        for (int i = 0; i < 5; i++)
            Assert.Equal((i + 1) * 10, At(dsk, FirstFileOffset + DiskImage.FileHeaderSize + i));
    }

    [Fact]
    public void Second_file_starts_in_the_next_block()
    {
        var disk = new DiskImage();
        disk.AddFile("ONE", "BIN", AmsdosFileType.Binary, 0x4000, 0x4000, [1]);
        disk.AddFile("TWO", "BAS", AmsdosFileType.Basic, 0x0170, 0x0000, [2]);
        byte[] dsk = disk.ToDskBytes();

        Assert.Equal("TWO     BAS", TextAt(dsk, 32 + 1, 11));   // deuxième entrée du répertoire
        Assert.Equal(3, At(dsk, 32 + 16));                      // bloc 3
        Assert.Equal(0, At(dsk, 3 * DiskImage.BlockSize + 0x12)); // type BASIC
    }

    [Fact]
    public void File_larger_than_16_KB_is_refused()
    {
        var disk = new DiskImage();

        Assert.Throws<InvalidOperationException>(
            () => disk.AddFile("BIG", "BIN", AmsdosFileType.Binary, 0x4000, 0x4000, new byte[20000]));
    }

    [Fact]
    public void Too_long_name_is_refused()
    {
        var disk = new DiskImage();

        Assert.Throws<ArgumentException>(
            () => disk.AddFile("TOOLONGNAME", "BIN", AmsdosFileType.Binary, 0x4000, 0x4000, [1]));
        Assert.Throws<ArgumentException>(
            () => disk.AddFile("OK", "TOOLONG", AmsdosFileType.Binary, 0x4000, 0x4000, [1]));
    }

    [Fact]
    public void Basic_loader_starts_with_MEMORY_line()
    {
        byte[] loader = BasicLoader.Build("HELLO.BIN", 0x3FFF, 0x4000);

        // 10 MEMORY &3FFF : longueur 10, numéro 10, MEMORY, espace, constante &1C &FF &3F, fin de ligne
        byte[] expected = [0x0A, 0x00, 0x0A, 0x00, 0xAA, 0x20, 0x1C, 0xFF, 0x3F, 0x00];
        Assert.Equal(expected, loader[..10]);
    }

    [Fact]
    public void Basic_loader_has_three_lines_and_end_marker()
    {
        byte[] loader = BasicLoader.Build("HELLO.BIN", 0x3FFF, 0x4000);

        Assert.Equal(39, loader.Length);          // 10 + 17 + 10 octets de lignes + 2 octets de fin
        Assert.Equal(0x11, loader[10]);           // ligne 20 : longueur 17
        Assert.Equal(20, loader[12]);             // numéro de ligne 20
        Assert.Equal(0xA8, loader[14]);           // LOAD
        Assert.Equal(0x83, loader[27 + 4]);       // ligne 30 : CALL
        Assert.Equal(0x00, loader[^1]);
        Assert.Equal(0x00, loader[^2]);
    }
}
