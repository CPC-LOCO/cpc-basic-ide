// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CpcIde;

/// <summary>
/// Couleurs de l'éditeur d'assembleur (D159), sur le même papier bleu que l'éditeur BASIC : étiquette (au début de la
/// ligne) en rose, instruction en bleu clair, nombres en blanc, textes en vert, commentaires (après « ; ») en gris.
/// </summary>
public sealed class AsmColorizer : DocumentColorizingTransformer
{
    static IBrush Make(string hex) => new SolidColorBrush(Color.Parse(hex));

    static readonly IBrush Label = Make("#FF80FF");
    static readonly IBrush Mnemonic = Make("#00FFFF");
    static readonly IBrush Number = Make("#FFFFFF");
    static readonly IBrush Text = Make("#00FF00");
    static readonly IBrush Comment = Make("#A0A0A0");

    protected override void ColorizeLine(DocumentLine line)
    {
        string text = CurrentContext.Document.GetText(line);
        int offset = line.Offset;
        int i = 0;
        bool sawWord = false;

        // une étiquette commence au tout début de la ligne
        if (text.Length > 0 && (char.IsLetter(text[0]) || text[0] is '_' or '.'))
        {
            int end = Word(text, 0);
            if (end < text.Length && text[end] == ':')
                end++;
            Paint(offset, offset + end, Label);
            i = end;
        }

        while (i < text.Length)
        {
            char c = text[i];
            if (c == ';')
            {
                Paint(offset + i, offset + text.Length, Comment);
                return;
            }
            if (c is '"' or '\'' && !(c == '\'' && i >= 2 && char.ToUpperInvariant(text[i - 1]) == 'F'
                                      && char.ToUpperInvariant(text[i - 2]) == 'A'))
            {
                int close = text.IndexOf(c, i + 1);
                int end = close < 0 ? text.Length : close + 1;
                Paint(offset + i, offset + end, Text);
                i = end;
                continue;
            }
            if (char.IsDigit(c) || (c is '&' or '#' or '%' or '$' && i + 1 < text.Length && char.IsLetterOrDigit(text[i + 1])))
            {
                int end = i + 1;
                while (end < text.Length && char.IsLetterOrDigit(text[end]))
                    end++;
                Paint(offset + i, offset + end, Number);
                i = end;
                continue;
            }
            if (char.IsLetter(c) || c is '_' or '.')
            {
                int end = Word(text, i);
                if (!sawWord)
                    Paint(offset + i, offset + end, Mnemonic);    // le premier mot après l'étiquette : l'instruction
                sawWord = true;
                i = end;
                continue;
            }
            i++;
        }
    }

    static int Word(string text, int start)
    {
        int end = start;
        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] is '_' or '.'))
            end++;
        return end;
    }

    void Paint(int start, int end, IBrush brush)
    {
        if (end > start)
            ChangeLinePart(start, end, element => element.TextRunProperties.SetForegroundBrush(brush));
    }
}
