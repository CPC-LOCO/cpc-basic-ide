// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D178 : la cassette (.cdt), écrite comme le firmware du CPC écrit une bande.</summary>
public class TapeImageTests
{
    // Les blocs TZX n° &11 du fichier : (durée d'une impulsion de bit à 0, pause après, octets)
    static List<(int Zero, int One, int Pause, byte[] Data)> Blocks(byte[] cdt)
    {
        var blocks = new List<(int, int, int, byte[])>();
        Assert.Equal(0x20, cdt[10]);                          // silence de départ
        Assert.Equal(TapeImage.MotorPauseMs, cdt[11] | cdt[12] << 8);
        int p = 13;
        while (p < cdt.Length)
        {
            Assert.Equal(0x11, cdt[p]);
            int zero = cdt[p + 7] | cdt[p + 8] << 8;
            int one = cdt[p + 9] | cdt[p + 10] << 8;
            int pause = cdt[p + 14] | cdt[p + 15] << 8;
            int length = cdt[p + 16] | cdt[p + 17] << 8 | cdt[p + 18] << 16;
            blocks.Add((zero, one, pause, cdt[(p + 19)..(p + 19 + length)]));
            p += 19 + length;
        }
        return blocks;
    }

    static byte[] Content(int length) => Enumerable.Range(0, length).Select(i => (byte)(i * 7)).ToArray();

    [Fact]
    public void Le_fichier_commence_par_la_signature_TZX()
    {
        byte[] cdt = new TapeImage().ToCdtBytes();

        Assert.Equal("ZXTape!", Encoding.ASCII.GetString(cdt, 0, 7));
        Assert.Equal(0x1A, cdt[7]);
        Assert.Equal(13, cdt.Length);                         // signature et silence de départ
    }

    [Fact]
    public void La_somme_de_controle_est_celle_du_firmware()
    {
        // CRC-16 CCITT, départ &FFFF, inversé : valeur connue pour « 123456789 »
        Assert.Equal(0xD64E, TapeImage.Crc("123456789"u8));
    }

    [Fact]
    public void Un_enregistrement_a_ses_paquets_de_256_octets_et_sa_fin()
    {
        byte[] record = TapeImage.Record(0x16, Content(300));

        Assert.Equal(1 + 2 * 258 + 4, record.Length);
        Assert.Equal(0x16, record[0]);
        Assert.Equal(Content(256), record[1..257]);
        ushort crc = TapeImage.Crc(record.AsSpan(1, 256));
        Assert.Equal(crc >> 8, record[257]);
        Assert.Equal(crc & 0xFF, record[258]);
        Assert.Equal(0, record[259 + 44]);                    // second paquet complété par des 0
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, record[^4..]);
    }

    [Fact]
    public void Un_fichier_de_5000_octets_fait_trois_morceaux()
    {
        var tape = new TapeImage();
        tape.AddFile("jeu.bin", AmsdosFileType.Binary, 0x4000, 0x4010, Content(5000));
        var blocks = Blocks(tape.ToCdtBytes());

        Assert.Equal(6, blocks.Count);                        // en-tête + données, trois fois
        int[] lengths = [2048, 2048, 904];
        for (int b = 0; b < 3; b++)
        {
            byte[] header = blocks[2 * b].Data;
            Assert.Equal(0x2C, header[0]);
            Assert.Equal("JEU.BIN", Encoding.ASCII.GetString(header, 1, 7));
            Assert.Equal(0, header[1 + 7]);                   // nom complété par des 0
            Assert.Equal(b + 1, header[1 + 16]);              // numéro du morceau
            Assert.Equal(b == 2 ? 0xFF : 0, header[1 + 17]);  // dernier morceau
            Assert.Equal(2, header[1 + 18]);                  // binaire
            Assert.Equal(lengths[b], header[1 + 19] | header[1 + 20] << 8);
            Assert.Equal(0x4000 + 2048 * b, header[1 + 21] | header[1 + 22] << 8);
            Assert.Equal(b == 0 ? 0xFF : 0, header[1 + 23]);  // premier morceau
            Assert.Equal(5000, header[1 + 24] | header[1 + 25] << 8);
            Assert.Equal(0x4010, header[1 + 26] | header[1 + 27] << 8);

            byte[] data = blocks[2 * b + 1].Data;
            Assert.Equal(0x16, data[0]);
            Assert.Equal(1 + (lengths[b] + 255) / 256 * 258 + 4, data.Length);
            Assert.Equal(Content(5000)[2048 * b], data[1]);
        }
        Assert.Equal(20, blocks[0].Pause);                    // en-tête puis données : d'un trait
        Assert.All(new[] { 1, 3, 5 }, i => Assert.Equal(TapeImage.MotorPauseMs, blocks[i].Pause));   // le moteur redémarre
    }

    [Fact]
    public void A_2000_bauds_les_impulsions_durent_comme_SPEED_WRITE_1()
    {
        var tape = new TapeImage();
        tape.AddFile("A", AmsdosFileType.Binary, 0x4000, 0x4000, [1]);
        var blocks = Blocks(tape.ToCdtBytes());

        Assert.All(blocks, b => Assert.Equal((583, 1166), (b.Zero, b.One)));
    }

    [Fact]
    public void Un_nom_de_plus_de_16_caracteres_est_refuse()
    {
        Assert.Throws<ArgumentException>(() =>
            new TapeImage().AddFile("UN-NOM-BEAUCOUP-TROP-LONG", AmsdosFileType.Binary, 0x4000, 0x4000, [1]));
    }

    [Fact]
    public void Avec_chargeur_la_cassette_a_le_chargeur_puis_le_programme()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"A\"", "jeu");

        Assert.Equal(new[] { "JEU", "JEU.BIN" }, result.Tape!.FileNames);
        byte[] firstHeader = Blocks(result.Tape.ToCdtBytes())[0].Data;
        Assert.Equal(0, firstHeader[1 + 18]);                 // le chargeur est un programme BASIC
        Assert.Equal(0x0170, firstHeader[1 + 21] | firstHeader[1 + 22] << 8);
    }

    [Fact]
    public void Sans_chargeur_la_cassette_n_a_que_le_programme()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"A\"", "jeu", new CompilerOptions(WithLoader: false));

        Assert.Equal(new[] { "JEU" }, result.Tape!.FileNames);
    }

    [Fact]
    public void En_cas_d_erreur_pas_de_cassette()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT (", "jeu");

        Assert.Null(result.Tape);
    }
}
