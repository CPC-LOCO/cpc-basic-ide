// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Globalization;
using System.Text;

namespace CpcBasic.Core;

/// <summary>Version du CPC choisie dans l'éditeur (D126) : elle décide de la table des caractères. Français par défaut.</summary>
public enum CpcLanguage
{
    /// <summary>CPC français (clavier AZERTY) : à ç ^ é ù è ° à la place de @ \ ↑ { | } ¨.</summary>
    French,
    /// <summary>CPC anglais (clavier QWERTY) : la table d'origine du Locomotive.</summary>
    English
}

/// <summary>
/// Table des caractères du CPC (D126). Relevée dans les ROM système OS6128.ROM et OS6128-AZERTY.rom (dessins des
/// caractères en &amp;3800, 18 nov. 2026) : seuls 7 codes changent entre le CPC anglais et le CPC français.
/// Règles :
/// - un caractère de l'alphabet anglais (espace à ~) garde son code, comme une touche du clavier du CPC ;
/// - un autre caractère prend le code qui l'affiche sur le CPC choisi, sinon celui qui l'affiche sur un autre CPC
///   (é sur un CPC anglais : 123, qui affiche « { ») ;
/// - un caractère qui n'existe sur aucun CPC (ê, â…) n'a pas de code : erreur à la compilation.
/// </summary>
public static class CharacterSet
{
    // Codes de 160 à 243 identiques sur les deux ROM (dessins comparés un par un)
    static readonly Dictionary<int, char> Common = new()
    {
        [160] = '^', [161] = '´', [163] = '£', [164] = '©', [165] = '¶', [166] = '§',
        [168] = '¼', [169] = '½', [170] = '¾', [171] = '±', [172] = '÷', [173] = '¬', [174] = '¿', [175] = '¡',
        [176] = 'α', [177] = 'β', [179] = 'δ', [180] = 'ε', [181] = 'θ', [182] = 'λ', [183] = 'μ', [184] = 'π',
        [185] = 'σ', [189] = 'ω', [190] = 'Σ', [191] = 'Ω',
        [224] = '☺', [225] = '☻', [226] = '♣', [227] = '♦', [228] = '♥', [229] = '♠', [230] = '○', [231] = '●',
        [232] = '□', [233] = '■', [240] = '↑', [241] = '↓', [242] = '←', [243] = '→'
    };

    static readonly Dictionary<int, char> FrenchOnly = new()
    {
        [64] = 'à', [92] = 'ç', [94] = '^', [123] = 'é', [124] = 'ù', [125] = 'è', [162] = '°'
    };

    static readonly Dictionary<int, char> EnglishOnly = new()
    {
        [94] = '↑', [162] = '¨'
    };

    static Dictionary<int, char> Specific(CpcLanguage language) => language == CpcLanguage.English ? EnglishOnly : FrenchOnly;

    /// <summary>Nom de la version, tel qu'il est montré dans l'éditeur et dans les messages.</summary>
    public static string Name(CpcLanguage language) => language == CpcLanguage.English ? "anglais" : "français";

    /// <summary>Caractère affiché par le CPC choisi pour ce code, ou null si la table n'en donne pas (codes graphiques).</summary>
    public static char? Glyph(int code, CpcLanguage language)
    {
        if (Specific(language).TryGetValue(code, out char special))
            return special;
        if (code is >= 32 and <= 126)
            return (char)code;
        return Common.TryGetValue(code, out char common) ? common : null;
    }

    /// <summary>Code du CPC pour ce caractère (voir les règles plus haut), ou null s'il n'existe sur aucun CPC.</summary>
    public static int? CodeOf(char c, CpcLanguage language)
    {
        if (c is >= ' ' and <= '~')
            return c;
        return Find(c, language) ?? Find(c, language == CpcLanguage.English ? CpcLanguage.French : CpcLanguage.English);
    }

    static int? Find(char c, CpcLanguage language)
    {
        int? best = null;
        foreach (var (code, glyph) in Specific(language).Concat(Common))
        {
            if (glyph == c && Glyph(code, language) == c && (best is null || code < best))
                best = code;
        }
        return best;
    }

    /// <summary>Texte en octets du CPC ; un caractère inconnu devient « ? » (l'analyse l'a déjà signalé).</summary>
    public static byte[] Encode(string text, CpcLanguage language) =>
        text.Select(c => c is '\r' or '\n' ? (byte)c : (byte)(CodeOf(c, language) ?? '?')).ToArray();

    /// <summary>Lettre sans accent qui peut remplacer c (ê : e), ou null.</summary>
    public static char? Plain(char c)
    {
        string plain = new(c.ToString().Normalize(NormalizationForm.FormD)
                              .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray());
        return plain.Length == 1 && plain[0] is >= ' ' and <= '~' && plain[0] != c ? plain[0] : null;
    }
}
