// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>
/// Transforme les jetons en instructions. Grammaire actuelle :
///   ligne        := [numéro] [instruction { ":" instruction }]
///   instruction  := PRINT [texte [";"]]  |  END
/// Quand une instruction est incorrecte, le problème est noté et l'analyse reprend à l'instruction
/// suivante, pour signaler plusieurs erreurs d'un coup.
/// </summary>
public static class Parser
{
    public static BasicProgram Parse(IReadOnlyList<Token> tokens, List<Diagnostic> diagnostics)
    {
        var statements = new List<Statement>();
        int pos = 0;

        while (tokens[pos].Kind != TokenKind.EndOfFile)
            pos = ParseLine(tokens, pos, statements, diagnostics);

        return new BasicProgram(statements);
    }

    static bool IsLineEnd(Token t) => t.Kind == TokenKind.EndOfLine || t.Kind == TokenKind.EndOfFile;

    static int ParseLine(IReadOnlyList<Token> tokens, int pos,
                         List<Statement> statements, List<Diagnostic> diagnostics)
    {
        if (tokens[pos].Kind == TokenKind.LineNumber)
            pos++;

        while (!IsLineEnd(tokens[pos]))
        {
            pos = ParseStatement(tokens, pos, statements, diagnostics);

            if (tokens[pos].Kind == TokenKind.Colon)
            {
                pos++;
                continue;
            }

            if (!IsLineEnd(tokens[pos]))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, tokens[pos].Line, tokens[pos].Text));
                pos = SkipToStatementEnd(tokens, pos);
                if (tokens[pos].Kind == TokenKind.Colon)
                    pos++;
            }
        }

        if (tokens[pos].Kind == TokenKind.EndOfLine)
            pos++;
        return pos;
    }

    static int ParseStatement(IReadOnlyList<Token> tokens, int pos,
                              List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token t = tokens[pos];

        if (t.Kind == TokenKind.Colon)
            return pos;   // instruction vide : le « : » est traité par l'appelant

        if (t.Kind != TokenKind.Word)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, t.Line, t.Text));
            return SkipToStatementEnd(tokens, pos);
        }

        switch (t.Text)
        {
            case "PRINT":
                return ParsePrint(tokens, pos, statements, diagnostics);

            case "END":
                statements.Add(new EndStatement(t.Line));
                return pos + 1;

            default:
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, t.Line, t.Text));
                return SkipToStatementEnd(tokens, pos);
        }
    }

    static int ParsePrint(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token print = tokens[pos];
        pos++;
        Token next = tokens[pos];

        if (next.Kind == TokenKind.String)
        {
            pos++;
            bool newLine = true;
            if (tokens[pos].Kind == TokenKind.Semicolon)
            {
                newLine = false;
                pos++;
            }
            statements.Add(new PrintStatement(print.Line, next.Text, newLine));
            return pos;
        }

        if (next.Kind == TokenKind.Colon || IsLineEnd(next))
        {
            statements.Add(new PrintStatement(print.Line, "", true));   // PRINT seul : saut de ligne
            return pos;
        }

        diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedString, next.Line, next.Text));
        return SkipToStatementEnd(tokens, pos);
    }

    // Avance jusqu'au prochain « : » ou à la fin de la ligne
    static int SkipToStatementEnd(IReadOnlyList<Token> tokens, int pos)
    {
        while (tokens[pos].Kind != TokenKind.Colon && !IsLineEnd(tokens[pos]))
            pos++;
        return pos;
    }
}
