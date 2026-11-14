// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

public enum TokenKind
{
    LineNumber,   // numéro au début d'une ligne
    Word,         // mot (commande ou variable), toujours en majuscules
    String,       // texte entre guillemets (sans les guillemets)
    Number,
    HexNumber,    // &C9, &HC9 (hexadécimal) ou &X1010 (binaire), de 0 à &FFFF ; Text garde l'écriture d'origine (lot D, D060)
    Caret,        // ^ : puissance (lot D, D060)
    Colon,
    Semicolon,
    Comma,
    Plus,
    Minus,
    Star,
    Slash,
    Equals,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    LeftParen,
    RightParen,
    EndOfLine,
    EndOfFile
}

/// <param name="Line">Ligne de l'éditeur, à partir de 1.</param>
/// <param name="Column">Colonne, à partir de 1.</param>
public readonly record struct Token(TokenKind Kind, string Text, int Line, int Column);

/// <summary>Découpe un programme BASIC en jetons. Chaque ligne se termine par un jeton EndOfLine.</summary>
public static class Lexer
{
    public static List<Token> Tokenize(string source, List<Diagnostic> diagnostics)
    {
        var tokens = new List<Token>();
        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int n = 0; n < lines.Length; n++)
        {
            TokenizeLine(lines[n], n + 1, tokens, diagnostics);
            tokens.Add(new Token(TokenKind.EndOfLine, "", n + 1, lines[n].Length + 1));
        }

        tokens.Add(new Token(TokenKind.EndOfFile, "", lines.Length, 1));
        return tokens;
    }

    static void TokenizeLine(string text, int line, List<Token> tokens, List<Diagnostic> diagnostics)
    {
        int i = 0;
        bool first = true;   // vrai tant qu'aucun jeton n'a été lu sur la ligne

        while (i < text.Length)
        {
            char c = text[i];
            int column = i + 1;

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Commentaire avec apostrophe : le reste de la ligne est ignoré
            if (c == '\'')
                return;

            // Texte entre guillemets
            if (c == '"')
            {
                int end = text.IndexOf('"', i + 1);
                if (end < 0)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnterminatedString, line));
                    tokens.Add(new Token(TokenKind.String, text[(i + 1)..], line, column));
                    return;
                }
                tokens.Add(new Token(TokenKind.String, text.Substring(i + 1, end - i - 1), line, column));
                i = end + 1;
                first = false;
                continue;
            }

            // Nombre : le premier nombre de la ligne est son numéro
            if (char.IsDigit(c))
            {
                int start = i;
                while (i < text.Length && char.IsDigit(text[i]))
                    i++;
                tokens.Add(new Token(first ? TokenKind.LineNumber : TokenKind.Number,
                                     text[start..i], line, column));
                first = false;
                continue;
            }

            // Nombre hexadécimal (&C9, &HC9) ou binaire (&X1010), comme le Locomotive BASIC (lot D, D060)
            if (c == '&')
            {
                int start = i;
                i++;
                int radix = 16;
                if (i < text.Length && char.ToUpperInvariant(text[i]) == 'H')
                {
                    i++;
                }
                else if (i < text.Length && char.ToUpperInvariant(text[i]) == 'X')
                {
                    radix = 2;
                    i++;
                }

                int digits = i;
                while (i < text.Length && (radix == 2 ? (text[i] is '0' or '1') : Uri.IsHexDigit(text[i])))
                    i++;

                string written = text[start..i].ToUpperInvariant();
                first = false;
                if (i == digits)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedHexDigits, line, written));
                    continue;
                }
                if (HexValue(written) is null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.HexNumberOutOfRange, line, written));
                    continue;
                }
                tokens.Add(new Token(TokenKind.HexNumber, written, line, column));
                continue;
            }

            // Mot : commande ou variable. Suffixes : $ chaîne, % entier, ! réel.
            if (char.IsLetter(c))
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.'))
                    i++;
                if (i < text.Length && (text[i] == '$' || text[i] == '%' || text[i] == '!'))
                    i++;

                string word = text[start..i].ToUpperInvariant();
                if (word == "REM")
                    return;   // commentaire : le reste de la ligne est ignoré

                tokens.Add(new Token(TokenKind.Word, word, line, column));
                first = false;
                continue;
            }

            // « ? » est un raccourci de PRINT, comme dans le Locomotive BASIC
            if (c == '?')
            {
                tokens.Add(new Token(TokenKind.Word, "PRINT", line, column));
                i++;
                first = false;
                continue;
            }

            // Comparaisons : <  <=  <>  >  >=
            if (c == '<' || c == '>')
            {
                string op = c.ToString();
                if (i + 1 < text.Length && (text[i + 1] == '=' || (c == '<' && text[i + 1] == '>')))
                    op += text[i + 1];

                TokenKind opKind = op switch
                {
                    "<" => TokenKind.Less,
                    "<=" => TokenKind.LessEqual,
                    "<>" => TokenKind.NotEqual,
                    ">" => TokenKind.Greater,
                    _ => TokenKind.GreaterEqual
                };
                tokens.Add(new Token(opKind, op, line, column));
                i += op.Length;
                first = false;
                continue;
            }

            TokenKind? kind = c switch
            {
                ':' => TokenKind.Colon,
                ';' => TokenKind.Semicolon,
                ',' => TokenKind.Comma,
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '\\' => TokenKind.Slash,   // « \ » : division entière du Locomotive BASIC
                '=' => TokenKind.Equals,
                '^' => TokenKind.Caret,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                _ => null
            };

            if (kind is TokenKind k)
                tokens.Add(new Token(k, c.ToString(), line, column));
            else
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedCharacter, line, c.ToString()));

            i++;
            first = false;
        }
    }

    /// <summary>
    /// Valeur d'un nombre écrit &amp;C9, &amp;HC9 ou &amp;X1010 : de 0 à 65535, ou null s'il est trop grand (D060).
    /// Comme sur le CPC, de &amp;8000 à &amp;FFFF il vaut le même mot de 16 bits qu'un entier négatif (&amp;FFFF = -1).
    /// </summary>
    public static int? HexValue(string written)
    {
        string digits = written.TrimStart('&');
        int radix = 16;
        if (digits.StartsWith('H'))
        {
            digits = digits[1..];
        }
        else if (digits.StartsWith('X'))
        {
            digits = digits[1..];
            radix = 2;
        }

        long value = 0;
        foreach (char d in digits)
        {
            value = value * radix + Convert.ToInt32(d.ToString(), 16);
            if (value > 0xFFFF)
                return null;
        }
        return (int)value;
    }
}
