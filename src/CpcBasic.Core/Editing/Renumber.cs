// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;

namespace CpcBasic.Core.Editing;

/// <summary>Résultat de RENUM : le nouveau texte, ou un refus (Error) ; Warnings : renvois vers des lignes absentes.</summary>
public sealed record RenumberResult(string? Text, string? Error, int LineCount, IReadOnlyList<string> Warnings);

/// <summary>
/// RENUM [nouveau][,[ancien][,pas]] comme le Locomotive BASIC (D117) : renumérote les lignes à partir de « ancien »
/// (la première ligne si absent), en commençant à « nouveau » (10) de « pas » en « pas » (10), et corrige les renvois :
/// GOTO, GOSUB (aussi ON ... GOTO/GOSUB, ON ERROR GOTO, ON BREAK GOSUB, EVERY/AFTER ... GOSUB), THEN, ELSE,
/// RESTORE, RESUME et RUN suivis d'un numéro. Les textes entre guillemets, REM, ' et DATA ne sont jamais touchés.
/// </summary>
public static class Renumber
{
    static readonly HashSet<string> ListKeywords = new() { "GOTO", "GOSUB" };
    static readonly HashSet<string> SingleKeywords = new() { "THEN", "ELSE", "RESTORE", "RESUME", "RUN" };

    /// <summary>Lit les valeurs de RENUM ; faux si elles ne conviennent pas.</summary>
    public static bool TryParse(string argument, out int newStart, out int? oldStart, out int step)
    {
        newStart = 10;
        oldStart = null;
        step = 10;
        string[] parts = argument.Split(',');
        if (parts.Length > 3)
            return false;
        if (parts[0].Trim().Length > 0 && !TryNumber(parts[0], out newStart))
            return false;
        if (parts.Length >= 2 && parts[1].Trim().Length > 0)
        {
            if (!TryNumber(parts[1], out int old))
                return false;
            oldStart = old;
        }
        if (parts.Length == 3 && !TryNumber(parts[2], out step))
            return false;
        return true;
    }

    static bool TryNumber(string text, out int value) =>
        int.TryParse(text.Trim(), System.Globalization.NumberStyles.None, null, out value) && value is >= 1 and <= 65535;

    public static RenumberResult Apply(string text, int newStart = 10, int? oldStart = null, int step = 10)
    {
        var tokens = Lexer.Tokenize(text, new List<Diagnostic>());

        // Les lignes du programme : numéro, et position du numéro dans l'éditeur
        var lineNumbers = tokens.Where(t => t.Kind == TokenKind.LineNumber && IsInteger(t.Text)).ToList();
        if (lineNumbers.Count == 0)
            return new RenumberResult(null, "RENUM : le programme n'a aucune ligne numérotée.", 0, []);

        var duplicate = lineNumbers.GroupBy(t => int.Parse(t.Text)).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            return new RenumberResult(null,
                $"RENUM : deux lignes ont le numéro {duplicate.Key} ; corrigez-en une avant de renuméroter.", 0, []);

        var sorted = lineNumbers.Select(t => int.Parse(t.Text)).Order().ToList();
        int from = oldStart ?? sorted[0];
        var moved = sorted.Where(n => n >= from).ToList();
        if (moved.Count == 0)
            return new RenumberResult(null, $"RENUM : aucune ligne à partir de la ligne {from} (Improper argument).", 0, []);

        // Les nouveaux numéros ne doivent ni dépasser 65535, ni rejoindre les lignes gardées avant « ancien »
        long last = newStart + (long)step * (moved.Count - 1);
        if (last > 65535)
            return new RenumberResult(null,
                $"RENUM : la dernière ligne aurait le numéro {last}, plus que 65535 (Improper argument). Prenez un pas plus petit.", 0, []);
        int kept = sorted.Where(n => n < from).DefaultIfEmpty(0).Max();
        if (kept > 0 && newStart <= kept)
            return new RenumberResult(null,
                $"RENUM : la ligne {kept} est gardée, la nouvelle numérotation doit commencer après elle (Improper argument).", 0, []);

        var map = new Dictionary<int, int>();
        for (int i = 0; i < moved.Count; i++)
            map[moved[i]] = newStart + step * i;

        // Ce qui change : (ligne de l'éditeur, colonne, longueur, nouveau texte)
        var changes = new List<(int Line, int Column, int Length, string Text)>();
        var warnings = new List<string>();
        foreach (var t in lineNumbers)
            if (map.TryGetValue(int.Parse(t.Text), out int n))
                changes.Add((t.Line, t.Column, t.Text.Length, n.ToString()));

        int currentLine = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Kind == TokenKind.LineNumber)
                currentLine = IsInteger(t.Text) ? int.Parse(t.Text) : 0;
            if (t.Kind != TokenKind.Word)
                continue;
            bool list = ListKeywords.Contains(t.Text);
            if (!list && !SingleKeywords.Contains(t.Text))
                continue;

            int j = i + 1;
            while (j < tokens.Count && tokens[j].Kind == TokenKind.Number && IsInteger(tokens[j].Text))
            {
                var reference = tokens[j];
                int target = int.Parse(reference.Text);
                if (map.TryGetValue(target, out int n))
                    changes.Add((reference.Line, reference.Column, reference.Text.Length, n.ToString()));
                else if (target != 0 && !sorted.Contains(target))
                    warnings.Add($"Ligne {currentLine} : {t.Text} {target}, mais la ligne {target} n'existe pas (Undefined line).");
                if (!list || j + 2 >= tokens.Count || tokens[j + 1].Kind != TokenKind.Comma)
                    break;
                j += 2;
            }
        }

        // Remplacement de la fin vers le début de chaque ligne : les colonnes restent justes
        string[] lines = text.Split('\n');
        foreach (var group in changes.GroupBy(c => c.Line))
        {
            string line = lines[group.Key - 1];
            foreach (var c in group.OrderByDescending(c => c.Column))
                line = line[..(c.Column - 1)] + c.Text + line[(c.Column - 1 + c.Length)..];
            lines[group.Key - 1] = line;
        }
        return new RenumberResult(string.Join('\n', lines), null, moved.Count, warnings);
    }

    static bool IsInteger(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);
}
