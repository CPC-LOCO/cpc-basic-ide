// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;

namespace CpcBasic.Core.Output;

/// <summary>
/// Fabrique un petit programme BASIC « tokenisé » (format du Locomotive BASIC) qui charge
/// et lance un fichier binaire :
///   10 MEMORY &amp;xxxx
///   20 LOAD"nom"
///   30 CALL &amp;xxxx
/// </summary>
public static class BasicLoader
{
    // Codes du format tokenisé
    const byte TokenMemory = 0xAA;
    const byte TokenLoad = 0xA8;
    const byte TokenCall = 0x83;
    const byte TokenHexConstant = 0x1C;   // constante hexadécimale sur 2 octets
    const byte Quote = 0x22;
    const byte Space = 0x20;

    /// <param name="binaryFileName">Nom complet du fichier à charger, par exemple « HELLO.BIN ».</param>
    /// <param name="memoryTop">Adresse passée à MEMORY (juste sous l'adresse de chargement).</param>
    /// <param name="callAddress">Adresse appelée par CALL (adresse d'exécution du programme).</param>
    public static byte[] Build(string binaryFileName, int memoryTop, int callAddress)
    {
        byte[] line10 = [TokenMemory, Space, .. HexConstant(memoryTop)];
        byte[] line20 = [TokenLoad, Quote, .. Encoding.ASCII.GetBytes(binaryFileName), Quote];
        byte[] line30 = [TokenCall, Space, .. HexConstant(callAddress)];

        return
        [
            .. Line(10, line10),
            .. Line(20, line20),
            .. Line(30, line30),
            0x00, 0x00                       // fin du programme
        ];
    }

    static byte[] HexConstant(int value) =>
        [TokenHexConstant, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF)];

    // Une ligne : longueur (2 octets), numéro (2 octets), codes, octet 0 de fin de ligne
    static byte[] Line(int number, byte[] tokens)
    {
        int length = 2 + 2 + tokens.Length + 1;
        byte[] line = new byte[length];
        ByteUtil.SetWord(line, 0, length);
        ByteUtil.SetWord(line, 2, number);
        tokens.CopyTo(line, 4);
        return line;
    }
}
