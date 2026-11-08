// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CpcIde;

/// <summary>
/// Colore chaque ligne du programme BASIC : numéros de ligne, mots-clés du Locomotive BASIC,
/// nouvelles commandes du projet, nombres, chaînes de caractères et commentaires.
/// Les listes de mots ci-dessous sont des exemples pour l'essai, pas la liste définitive.
/// </summary>
public class BasicColorizer : DocumentColorizingTransformer
{
    static IBrush Make(string hex) => new SolidColorBrush(Color.Parse(hex));

    static readonly IBrush LineNumberBrush = Make("#8080FF");
    static readonly IBrush KeywordBrush = Make("#00FFFF");   // mots-clés classiques
    static readonly IBrush NewCommandBrush = Make("#FF80FF"); // nouvelles commandes du projet
    static readonly IBrush NumberBrush = Make("#FFFFFF");
    static readonly IBrush StringBrush = Make("#00FF00");
    static readonly IBrush CommentBrush = Make("#A0A0A0");

    static readonly HashSet<string> Classic = new(StringComparer.OrdinalIgnoreCase)
    {
        "ABS", "AFTER", "AND", "ASC", "ATN", "AUTO", "BIN$", "BORDER", "CALL", "CAT", "CHAIN",
        "CHR$", "CINT", "CLEAR", "CLG", "CLOSEIN", "CLOSEOUT", "CLS", "CONT", "COPYCHR$", "COS",
        "CREAL", "CURSOR", "DATA", "DEF", "DEFINT", "DEFREAL", "DEFSTR", "DEG", "DELETE", "DERR",
        "DI", "DIM", "DRAW", "DRAWR", "EDIT", "EI", "ELSE", "END", "ENT", "ENV", "EOF", "ERASE",
        "ERL", "ERR", "ERROR", "EVERY", "EXP", "FILL", "FIX", "FN", "FOR", "FRAME", "FRE", "GOSUB",
        "GOTO", "GRAPHICS", "HEX$", "HIMEM", "IF", "INK", "INKEY", "INKEY$", "INP", "INPUT",
        "INSTR", "INT", "JOY", "KEY", "LEFT$", "LEN", "LET", "LINE", "LIST", "LOAD", "LOCATE",
        "LOG", "LOG10", "LOWER$", "MASK", "MAX", "MEMORY", "MERGE", "MID$", "MIN", "MOD", "MODE",
        "MOVE", "MOVER", "NEW", "NEXT", "NOT", "ON", "OPENIN", "OPENOUT", "OR", "ORIGIN", "OUT",
        "PAPER", "PEEK", "PEN", "PI", "PLOT", "PLOTR", "POKE", "POS", "PRINT", "RAD", "RANDOMIZE",
        "READ", "RELEASE", "REMAIN", "RENUM", "RESTORE", "RESUME", "RETURN", "RIGHT$", "RND",
        "ROUND", "RUN", "SAVE", "SGN", "SIN", "SOUND", "SPACE$", "SPC", "SPEED", "SQ", "SQR",
        "STEP", "STOP", "STR$", "STRING$", "SWAP", "SYMBOL", "TAB", "TAG", "TAGOFF", "TAN", "TEST",
        "TESTR", "THEN", "TIME", "TO", "TROFF", "TRON", "UNT", "UPPER$", "USING", "VAL", "VPOS",
        "WAIT", "WEND", "WHILE", "WIDTH", "WINDOW", "WRITE", "XOR", "XPOS", "YPOS", "ZONE"
    };

    static readonly HashSet<string> NewCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "CIRCLE", "ELLIPSE", "STAR", "PAINT", "BOX", "POLYGON",
        "SPRITE", "COLLIDE", "SCROLL",
        "SELECT", "CASE", "BYTE",
        "VROOM", "BOOM", "SHOT", "FALL", "WAVE"
    };

    protected override void ColorizeLine(DocumentLine line)
    {
        string text = CurrentContext.Document.GetText(line);
        int offset = line.Offset;
        int i = 0;
        bool firstToken = true;

        while (i < text.Length)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Commentaire commençant par une apostrophe : jusqu'à la fin de la ligne
            if (c == '\'')
            {
                Paint(offset + i, offset + text.Length, CommentBrush);
                return;
            }

            // Chaîne de caractères entre guillemets (non fermée : jusqu'à la fin de la ligne)
            if (c == '"')
            {
                int end = text.IndexOf('"', i + 1);
                end = end < 0 ? text.Length : end + 1;
                Paint(offset + i, offset + end, StringBrush);
                i = end;
                firstToken = false;
                continue;
            }

            // Nombre : décimal, &hexadécimal ou &Xbinaire. Le premier nombre d'une ligne est son numéro.
            if (char.IsDigit(c) || (c == '&' && i + 1 < text.Length && char.IsLetterOrDigit(text[i + 1])))
            {
                int start = i;
                i++;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.'))
                    i++;
                Paint(offset + start, offset + i, firstToken ? LineNumberBrush : NumberBrush);
                firstToken = false;
                continue;
            }

            // Mot : mot-clé, nouvelle commande ou variable
            if (char.IsLetter(c))
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '.'))
                    i++;
                if (i < text.Length && (text[i] == '$' || text[i] == '%' || text[i] == '!'))
                    i++;

                string word = text.Substring(start, i - start);

                if (word.Equals("REM", StringComparison.OrdinalIgnoreCase))
                {
                    Paint(offset + start, offset + text.Length, CommentBrush);
                    return;
                }

                if (NewCommands.Contains(word))
                    Paint(offset + start, offset + i, NewCommandBrush);
                else if (Classic.Contains(word))
                    Paint(offset + start, offset + i, KeywordBrush);

                firstToken = false;
                continue;
            }

            // « ? » est un raccourci de PRINT : coloré comme un mot-clé
            if (c == '?')
            {
                Paint(offset + i, offset + i + 1, KeywordBrush);
                firstToken = false;
                i++;
                continue;
            }

            // Tout autre caractère (opérateurs, virgules...) : couleur par défaut
            firstToken = false;
            i++;
        }
    }

    void Paint(int startOffset, int endOffset, IBrush brush)
    {
        ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetForegroundBrush(brush));
    }
}
