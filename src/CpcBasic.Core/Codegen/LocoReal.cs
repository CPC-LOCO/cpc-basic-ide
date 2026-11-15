// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

namespace CpcBasic.Core.Codegen;

/// <summary>
/// Nombres à virgule du Locomotive BASIC (5 octets, lot F1b, D068).
///   octets 0 à 3 : la mantisse sur 32 bits, l'octet de poids faible d'abord ;
///                  le bit 7 de l'octet 3 est le signe (le 1 de tête de la mantisse est sous-entendu) ;
///   octet 4      : l'exposant + 128 (0 = le nombre zéro).
/// Valeur = (mantisse avec le bit 31 à 1) / 2^32 × 2^(exposant − 128). Exemple : 1 = 00 00 00 00 81.
/// </summary>
public static class LocoReal
{
    /// <summary>Plus grand nombre possible (environ 1.7E+38).</summary>
    public static readonly double MaxValue = Decode([0xFF, 0xFF, 0xFF, 0x7F, 0xFF]);

    /// <summary>
    /// Range un nombre sur 5 octets, arrondi au plus près. Rend null s'il est trop grand (« Overflow »).
    /// Un nombre trop petit devient 0, comme sur le CPC.
    /// </summary>
    public static byte[]? Encode(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return null;
        if (value == 0)
            return [0, 0, 0, 0, 0];

        bool negative = value < 0;
        double magnitude = Math.Abs(value);

        // magnitude = m × 2^exponent, avec 0.5 <= m < 1
        int exponent = (int)Math.Floor(Math.Log2(magnitude)) + 1;
        double m = magnitude / Math.Pow(2, exponent);
        while (m >= 1) { m /= 2; exponent++; }
        while (m < 0.5) { m *= 2; exponent--; }

        double scaled = Math.Round(m * 4294967296.0, MidpointRounding.AwayFromZero);
        if (scaled >= 4294967296.0)
        {
            scaled /= 2;
            exponent++;
        }

        int stored = exponent + 128;
        if (stored > 255)
            return null;
        if (stored < 1)
            return [0, 0, 0, 0, 0];

        uint mantissa = (uint)scaled & 0x7FFFFFFF;
        if (negative)
            mantissa |= 0x80000000;

        return
        [
            (byte)(mantissa & 0xFF),
            (byte)((mantissa >> 8) & 0xFF),
            (byte)((mantissa >> 16) & 0xFF),
            (byte)(mantissa >> 24),
            (byte)stored
        ];
    }

    /// <summary>Lit un nombre de 5 octets.</summary>
    public static double Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes[4] == 0)
            return 0;

        uint mantissa = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
        bool negative = (mantissa & 0x80000000) != 0;
        double m = (mantissa | 0x80000000) / 4294967296.0;
        double value = m * Math.Pow(2, bytes[4] - 128);
        return negative ? -value : value;
    }
}

/// <summary>Une routine de calcul de la ROM du CPC : son nom et son adresse sur le 664/6128 et sur le 464.</summary>
public sealed record FirmwareMathsRoutine(string Name, int Address6128, int Address464)
{
    /// <summary>Étiquette de la case de la table de sauts en mémoire : rt_fm_add, rt_fm_mult...</summary>
    public string Label => "rt_fm_" + Name;
}

/// <summary>
/// Les routines de calcul à virgule de la ROM (D068). Les adresses changent entre le 464 et le 664/6128 :
/// le programme les appelle donc à travers une table de sauts en mémoire (rt_fm_...), remplie au départ.
/// Toutes rendent HL = adresse du résultat et la retenue à 1 si tout va bien (0 = Overflow ou erreur).
/// </summary>
public static class FirmwareMaths
{
    /// <summary>INTEGER TO REAL : HL = entier sans signe, A bit 7 = signe, DE = où ranger le nombre.</summary>
    public static readonly FirmwareMathsRoutine IntegerToReal = new("int2real", 0xBD64, 0xBD40);
    /// <summary>REAL TO INTEGER : HL = nombre -> HL = entier sans signe arrondi (le signe reste celui du nombre).</summary>
    public static readonly FirmwareMathsRoutine RealToInteger = new("real2int", 0xBD6A, 0xBD46);
    /// <summary>REAL FIX : (HL) = partie entière vers zéro.</summary>
    public static readonly FirmwareMathsRoutine Fix = new("fix", 0xBD70, 0xBD4C);
    /// <summary>REAL INT : (HL) = partie entière vers le bas.</summary>
    public static readonly FirmwareMathsRoutine Int = new("int", 0xBD73, 0xBD4F);
    /// <summary>REAL *10^A : (HL) = (HL) × 10^A, A signé.</summary>
    public static readonly FirmwareMathsRoutine Times10Power = new("x10a", 0xBD79, 0xBD55);
    /// <summary>REAL ADDITION : (HL) = (HL) + (DE).</summary>
    public static readonly FirmwareMathsRoutine Add = new("add", 0xBD7C, 0xBD58);
    /// <summary>REAL REVERSE SUBTRACTION : (HL) = (DE) − (HL).</summary>
    public static readonly FirmwareMathsRoutine ReverseSubtract = new("rsub", 0xBD82, 0xBD5E);
    /// <summary>REAL MULTIPLICATION : (HL) = (HL) × (DE).</summary>
    public static readonly FirmwareMathsRoutine Multiply = new("mult", 0xBD85, 0xBD61);
    /// <summary>REAL DIVISION : (HL) = (HL) / (DE).</summary>
    public static readonly FirmwareMathsRoutine Divide = new("div", 0xBD88, 0xBD64);
    /// <summary>REAL COMPARISON : A = 1 si (HL) &gt; (DE), 0 si égaux, &amp;FF si (HL) &lt; (DE).</summary>
    public static readonly FirmwareMathsRoutine Compare = new("cmp", 0xBD8E, 0xBD6A);
    /// <summary>REAL UNARY MINUS : (HL) = −(HL).</summary>
    public static readonly FirmwareMathsRoutine Negate = new("neg", 0xBD91, 0xBD6D);
    /// <summary>REAL SIGNUM : A = 1, 0 ou &amp;FF selon le signe de (HL).</summary>
    public static readonly FirmwareMathsRoutine Sign = new("sgn", 0xBD94, 0xBD70);
    /// <summary>SET ANGLE MODE : A = 0 radians, autre = degrés.</summary>
    public static readonly FirmwareMathsRoutine AngleMode = new("angle", 0xBD97, 0xBD73);
    public static readonly FirmwareMathsRoutine Pi = new("pi", 0xBD9A, 0xBD76);
    public static readonly FirmwareMathsRoutine Sqr = new("sqr", 0xBD9D, 0xBD79);
    /// <summary>REAL POWER : (HL) = (HL) ^ (DE).</summary>
    public static readonly FirmwareMathsRoutine Power = new("pow", 0xBDA0, 0xBD7C);
    public static readonly FirmwareMathsRoutine Log = new("log", 0xBDA3, 0xBD7F);
    public static readonly FirmwareMathsRoutine Log10 = new("log10", 0xBDA6, 0xBD82);
    public static readonly FirmwareMathsRoutine Exp = new("exp", 0xBDA9, 0xBD85);
    public static readonly FirmwareMathsRoutine Sin = new("sin", 0xBDAC, 0xBD88);
    public static readonly FirmwareMathsRoutine Cos = new("cos", 0xBDAF, 0xBD8B);
    public static readonly FirmwareMathsRoutine Tan = new("tan", 0xBDB2, 0xBD8E);
    public static readonly FirmwareMathsRoutine Atn = new("atn", 0xBDB5, 0xBD91);

    /// <summary>Toutes les routines, dans l'ordre de la table de sauts.</summary>
    public static readonly IReadOnlyList<FirmwareMathsRoutine> All =
    [
        IntegerToReal, RealToInteger, Fix, Int, Times10Power, Add, ReverseSubtract, Multiply, Divide,
        Compare, Negate, Sign, AngleMode, Pi, Sqr, Power, Log, Log10, Exp, Sin, Cos, Tan, Atn
    ];

    /// <summary>KL PROBE ROM : avec C = 0, rend H = 0 sur un 464, 1 sur un 664, 2 sur un 6128, 4 sur un Plus.</summary>
    public const int KlProbeRom = 0xB915;
}
