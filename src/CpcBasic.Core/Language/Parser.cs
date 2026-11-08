// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>
/// Transforme les jetons en instructions. Grammaire actuelle :
///   ligne        := [numéro] [instruction { ":" instruction }]
///   instruction  := PRINT [élément { ";" élément } [";"]]  |  END  |  BYTE nom { "," nom }
///                   |  [LET] variable "=" expression
///   élément      := texte | expression
///   expression   := opérande { ("+" | "-") opérande }
///   opérande     := ["+" | "-"] ( nombre | variable | "(" expression ")" )
/// Quand une instruction est incorrecte, le problème est noté et l'analyse reprend à l'instruction
/// suivante, pour signaler plusieurs erreurs d'un coup.
/// </summary>
public static class Parser
{
    static readonly HashSet<string> Commands = ["PRINT", "END", "LET", "BYTE"];

    public static BasicProgram Parse(IReadOnlyList<Token> tokens, List<Diagnostic> diagnostics)
    {
        var statements = new List<Statement>();
        int pos = 0;

        while (tokens[pos].Kind != TokenKind.EndOfFile)
            pos = ParseLine(tokens, pos, statements, diagnostics);

        CheckDeclarations(statements, diagnostics);
        return new BasicProgram(statements);
    }

    static bool IsLineEnd(Token t) => t.Kind == TokenKind.EndOfLine || t.Kind == TokenKind.EndOfFile;

    static bool IsStatementEnd(Token t) => t.Kind == TokenKind.Colon || IsLineEnd(t);

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

            case "BYTE":
                return ParseDeclaration(tokens, pos, statements, diagnostics);

            case "LET":
                return ParseAssignment(tokens, pos + 1, t.Line, statements, diagnostics);

            default:
                // Un mot suivi de « = » est une affectation sans LET
                if (tokens[pos + 1].Kind == TokenKind.Equals)
                    return ParseAssignment(tokens, pos, t.Line, statements, diagnostics);

                diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, t.Line, t.Text));
                return SkipToStatementEnd(tokens, pos);
        }
    }

    // pos désigne le nom de la variable
    static int ParseAssignment(IReadOnlyList<Token> tokens, int pos, int line,
                               List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];

        if (name.Kind != TokenKind.Word || Commands.Contains(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, name.Line, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }

        if (!VariableNames.IsSupported(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnsupportedVariableType, name.Line, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }

        pos++;
        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, tokens[pos].Line, tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Expression? value = ParseExpression(tokens, ref pos, diagnostics);
        if (value is null)
            return SkipToStatementEnd(tokens, pos);

        statements.Add(new AssignStatement(line, name.Text, value));
        return pos;
    }

    static int ParsePrint(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var items = new List<object>();   // string (texte) ou Expression
        bool trailingSemicolon = false;

        while (!IsStatementEnd(tokens[pos]))
        {
            if (tokens[pos].Kind == TokenKind.String)
            {
                items.Add(tokens[pos].Text);
                pos++;
            }
            else
            {
                Expression? value = ParseExpression(tokens, ref pos, diagnostics);
                if (value is null)
                    return SkipToStatementEnd(tokens, pos);
                items.Add(value);
            }

            if (tokens[pos].Kind == TokenKind.Semicolon)
            {
                pos++;
                trailingSemicolon = true;
            }
            else
            {
                trailingSemicolon = false;
                break;
            }
        }

        if (items.Count == 0)
        {
            statements.Add(new PrintStatement(line, "", true));   // PRINT seul : saut de ligne
            return pos;
        }

        for (int i = 0; i < items.Count; i++)
        {
            bool newLine = i == items.Count - 1 && !trailingSemicolon;
            statements.Add(items[i] is string text
                ? new PrintStatement(line, text, newLine)
                : new PrintValueStatement(line, (Expression)items[i], newLine));
        }
        return pos;
    }

    // BYTE a, b, c : les noms s'écrivent sans suffixe
    static int ParseDeclaration(IReadOnlyList<Token> tokens, int pos,
                                List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var names = new List<string>();
        while (true)
        {
            Token name = tokens[pos];

            if (name.Kind != TokenKind.Word || Commands.Contains(name.Text))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedVariableName, name.Line, name.Text));
                return SkipToStatementEnd(tokens, pos);
            }

            if (VariableNames.HasSuffix(name.Text))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.SuffixNotAllowedInDeclaration, name.Line, name.Text));
                return SkipToStatementEnd(tokens, pos);
            }

            names.Add(name.Text);
            pos++;

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new DeclareStatement(line, names));
        return pos;
    }

    // Vérifications dans l'ordre du programme : une variable BYTE se déclare une seule fois,
    // avant sa première utilisation, et un littéral affecté à un octet tient dans 0 à 255.
    static void CheckDeclarations(IReadOnlyList<Statement> statements, List<Diagnostic> diagnostics)
    {
        var bytes = new HashSet<string>();
        var used = new HashSet<string>();

        void Uses(Expression expression)
        {
            switch (expression)
            {
                case VariableExpression variable:
                    used.Add(variable.Name);
                    break;
                case BinaryExpression binary:
                    Uses(binary.Left);
                    Uses(binary.Right);
                    break;
            }
        }

        foreach (Statement statement in statements)
        {
            switch (statement)
            {
                case DeclareStatement declare:
                    foreach (string name in declare.Names)
                    {
                        if (bytes.Contains(name))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.AlreadyDeclared, declare.SourceLine, name));
                        else if (used.Contains(name))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.DeclaredAfterUse, declare.SourceLine, name));
                        else
                            bytes.Add(name);
                    }
                    break;

                case AssignStatement assign:
                    Uses(assign.Value);
                    used.Add(assign.Variable);
                    if (bytes.Contains(assign.Variable) && assign.Value is NumberExpression n && (n.Value < 0 || n.Value > 255))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, assign.SourceLine, n.Value.ToString()));
                    break;

                case PrintValueStatement print:
                    Uses(print.Value);
                    break;
            }
        }
    }

    // ----- Expressions : renvoient null (après avoir noté l'erreur) si l'expression est incorrecte -----

    static Expression? ParseExpression(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseOperand(tokens, ref pos, diagnostics);

        while (left is not null && (tokens[pos].Kind == TokenKind.Plus || tokens[pos].Kind == TokenKind.Minus))
        {
            BinaryOperator op = tokens[pos].Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            pos++;

            Expression? right = ParseOperand(tokens, ref pos, diagnostics);
            if (right is null)
                return null;
            left = new BinaryExpression(op, left, right);
        }

        return left;
    }

    static Expression? ParseOperand(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        if (tokens[pos].Kind == TokenKind.Plus)
        {
            pos++;
            return ParsePrimary(tokens, ref pos, diagnostics);
        }

        if (tokens[pos].Kind == TokenKind.Minus)
        {
            pos++;

            // -32768 est valide : le nombre est lu avec son signe
            if (tokens[pos].Kind == TokenKind.Number)
                return ParseNumber(tokens, ref pos, diagnostics, negative: true);

            Expression? operand = ParsePrimary(tokens, ref pos, diagnostics);
            return operand is null ? null : new BinaryExpression(BinaryOperator.Subtract, new NumberExpression(0), operand);
        }

        return ParsePrimary(tokens, ref pos, diagnostics);
    }

    static Expression? ParsePrimary(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token t = tokens[pos];

        switch (t.Kind)
        {
            case TokenKind.Number:
                return ParseNumber(tokens, ref pos, diagnostics, negative: false);

            case TokenKind.Word:
                if (Commands.Contains(t.Text))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, t.Text));
                    return null;
                }
                if (!VariableNames.IsSupported(t.Text))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnsupportedVariableType, t.Line, t.Text));
                    return null;
                }
                pos++;
                return new VariableExpression(t.Text);

            case TokenKind.LeftParen:
                pos++;
                Expression? inner = ParseExpression(tokens, ref pos, diagnostics);
                if (inner is null)
                    return null;
                if (tokens[pos].Kind != TokenKind.RightParen)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, t.Line));
                    return null;
                }
                pos++;
                return inner;

            default:
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, t.Text));
                return null;
        }
    }

    static Expression? ParseNumber(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics, bool negative)
    {
        Token t = tokens[pos];
        long limit = negative ? 32768 : 32767;

        if (!long.TryParse(t.Text, out long value) || value > limit)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, t.Line, (negative ? "-" : "") + t.Text));
            return null;
        }

        pos++;
        return new NumberExpression((int)(negative ? -value : value));
    }

    // Avance jusqu'au prochain « : » ou à la fin de la ligne
    static int SkipToStatementEnd(IReadOnlyList<Token> tokens, int pos)
    {
        while (!IsStatementEnd(tokens[pos]))
            pos++;
        return pos;
    }
}
