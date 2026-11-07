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
    Colon,
    Semicolon,
    Comma,
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

            // Mot : commande ou variable
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

            if (c == ':')
                tokens.Add(new Token(TokenKind.Colon, ":", line, column));
            else if (c == ';')
                tokens.Add(new Token(TokenKind.Semicolon, ";", line, column));
            else if (c == ',')
                tokens.Add(new Token(TokenKind.Comma, ",", line, column));
            else
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedCharacter, line, c.ToString()));

            i++;
            first = false;
        }
    }
}
