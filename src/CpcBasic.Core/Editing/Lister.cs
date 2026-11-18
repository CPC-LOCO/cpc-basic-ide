// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Language;

namespace CpcBasic.Core.Editing;

/// <summary>Résultat de LIST : le texte rangé, ou un refus (Error) ; Warnings : lignes sans numéro.</summary>
public sealed record ListResult(string? Text, string? Error, int LineCount, IReadOnlyList<string> Warnings, int? Duplicate = null);

/// <summary>
/// LIST dans l'éditeur (D122, choix 3 de l'utilisateur) : range les lignes par ordre de numéro et les écrit comme
/// le CPC : mots du langage en majuscules, « ? » écrit PRINT, nombres hexadécimaux en majuscules, un seul espace
/// après le numéro, numéro sans zéro devant. Les noms de variables, les textes entre guillemets, REM, ' et DATA
/// gardent leur écriture ; les autres espaces aussi. Les lignes vides disparaissent.
/// </summary>
public static class Lister
{
    public static ListResult Apply(string text)
    {
        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        bool finalNewLine = text.EndsWith('\n');
        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        var numbered = new List<(int Number, string Text)>();
        var others = new List<string>();
        var warnings = new List<string>();

        foreach (string raw in lines)
        {
            string line = raw.TrimStart();
            if (line.Trim().Length == 0)
                continue;
            int n = 0;
            while (n < line.Length && char.IsAsciiDigit(line[n]))
                n++;
            if (n > 0 && n <= 5 && int.Parse(line[..n]) is int number and >= 1 and <= 65535)
            {
                string rest = Format(line[n..].TrimStart());
                numbered.Add((number, rest.Length == 0 ? number.ToString() : $"{number} {rest}"));
            }
            else
            {
                others.Add(raw);
                warnings.Add($"Ligne sans numéro gardée à la fin : {Shorten(line.Trim())}");
            }
        }

        if (numbered.Count == 0)
            return new ListResult(null, "LIST : le programme n'a aucune ligne numérotée.", 0, []);
        var duplicate = numbered.GroupBy(l => l.Number).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            return new ListResult(null,
                $"LIST : plusieurs lignes ont le numéro {duplicate.Key} ; gardez-en une seule avant LIST.", 0, [], duplicate.Key);

        var result = numbered.OrderBy(l => l.Number).Select(l => l.Text).Concat(others);
        string joined = string.Join(newLine, result) + (finalNewLine ? newLine : "");
        return new ListResult(joined, null, numbered.Count, warnings);
    }

    /// <summary>Écrit une instruction (sans son numéro) comme le CPC.</summary>
    public static string Format(string line)
    {
        var sb = new StringBuilder(line.Length + 8);
        int i = 0;
        while (i < line.Length)
        {
            char c = line[i];

            // Texte entre guillemets : gardé tel quel (non fermé : jusqu'à la fin)
            if (c == '"')
            {
                int end = line.IndexOf('"', i + 1);
                end = end < 0 ? line.Length : end + 1;
                sb.Append(line, i, end - i);
                i = end;
                continue;
            }

            // Commentaire ' : le reste de la ligne est gardé
            if (c == '\'')
            {
                sb.Append(line, i, line.Length - i);
                break;
            }

            if (c == '?')
            {
                sb.Append("PRINT");
                i++;
                continue;
            }

            // Nombre hexadécimal ou binaire : &ff -> &FF, &x101 -> &X101
            if (c == '&')
            {
                int start = i++;
                while (i < line.Length && char.IsAsciiLetterOrDigit(line[i]))
                    i++;
                sb.Append(line[start..i].ToUpperInvariant());
                continue;
            }

            // Nombre : gardé (1e5 ne doit pas être lu comme le mot E5)
            if (char.IsAsciiDigit(c) || c == '.')
            {
                int start = i;
                while (i < line.Length && (char.IsAsciiLetterOrDigit(line[i]) || line[i] == '.'))
                    i++;
                sb.Append(line, start, i - start);
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '.'))
                    i++;
                if (i < line.Length && line[i] is '$' or '%' or '!')
                    i++;
                string written = line[start..i];
                string word = written.ToUpperInvariant();

                if (Parser.IsKeyword(word))
                {
                    sb.Append(word);
                    // REM et DATA : la suite de la ligne garde son écriture
                    if (word is "REM" or "DATA")
                    {
                        sb.Append(line, i, line.Length - i);
                        break;
                    }
                }
                else if (word.Length > 2 && word.StartsWith("FN", StringComparison.Ordinal))
                    sb.Append("FN").Append(written, 2, written.Length - 2);   // FNdouble : FN est un mot à part (D095)
                else
                    sb.Append(written);   // nom de variable : gardé comme il est écrit
                continue;
            }

            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    static string Shorten(string text) => text.Length <= 50 ? text : text[..50] + "...";
}
