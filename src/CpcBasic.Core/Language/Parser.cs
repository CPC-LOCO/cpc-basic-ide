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
    static readonly HashSet<string> Commands = ["PRINT", "END", "LET", "BYTE", "FOR", "NEXT", "TO", "STEP", "EXIT", "CONTINUE", "IF", "THEN", "ELSE", "ENDIF", "AND", "OR",
        "WHILE", "WEND", "REPEAT", "UNTIL", "DO", "LOOP", "MOD",
        "ABS", "SGN", "MIN", "MAX", "HIBYTE", "LOBYTE", "CLAMP", "RANDOM", "RANDOMIZE", "TIME",
        "MODE", "CLS", "LOCATE", "PEN", "PAPER", "INK", "BORDER",
        "MOVE", "MOVER", "PLOT", "PLOTR", "DRAW", "DRAWR", "CLG", "ORIGIN", "BOX"];

    sealed record FunctionInfo(string Signature, int MinArguments, int MaxArguments);

    // Fonctions disponibles : nom, écriture à montrer dans les messages, nombre de valeurs
    static readonly Dictionary<string, FunctionInfo> Functions = new()
    {
        ["ABS"] = new("ABS(valeur)", 1, 1),
        ["SGN"] = new("SGN(valeur)", 1, 1),
        ["HIBYTE"] = new("HIBYTE(valeur)", 1, 1),
        ["LOBYTE"] = new("LOBYTE(valeur)", 1, 1),
        ["MIN"] = new("MIN(a, b)", 2, int.MaxValue),
        ["MAX"] = new("MAX(a, b)", 2, int.MaxValue),
        ["CLAMP"] = new("CLAMP(valeur, min, max)", 3, 3),
        ["RANDOM"] = new("RANDOM(min, max)", 2, 2)
    };

    /// <summary>Commande d'écran : écriture à montrer dans les messages, nombre de valeurs, valeurs permises pour chacune.</summary>
    sealed record ScreenCommandInfo(string Signature, int MinArguments, int[] Min, int[] Max);

    // Commandes d'écran (D046) : les valeurs écrites en nombres sont vérifiées à la compilation
    static readonly Dictionary<string, ScreenCommandInfo> ScreenCommands = new()
    {
        ["MODE"] = new("MODE mode (0, 1 ou 2)", 1, [0], [2]),
        ["CLS"] = new("CLS", 0, [], []),
        ["LOCATE"] = new("LOCATE colonne, ligne", 2, [1, 1], [80, 25]),
        ["PEN"] = new("PEN stylo (0 à 15)", 1, [0], [15]),
        ["PAPER"] = new("PAPER fond (0 à 15)", 1, [0], [15]),
        ["INK"] = new("INK stylo, couleur [, couleur2]", 2, [0, 0, 0], [15, 26, 26]),
        ["BORDER"] = new("BORDER couleur [, couleur2]", 1, [0, 0], [26, 26]),
        ["CLG"] = new("CLG [stylo]", 0, [0], [15]),
        ["ORIGIN"] = new("ORIGIN x, y", 2, [-32768, -32768], [32767, 32767]),
        // D048 : coordonnées natives du MODE (origine en haut à gauche) ; ce qui sort de l'écran est ignoré
        ["BOX"] = new("BOX x1, y1, x2, y2 [, stylo [, remplissage]]", 4, [-32768, -32768, -32768, -32768, 0, 0], [32767, 32767, 32767, 32767, 15, 15])
    };

    // Tracé (D047) : écriture à montrer dans les messages. Le stylo va de 0 à 15, le mode de 0 à 3.
    static readonly Dictionary<string, string> DrawCommands = new()
    {
        ["MOVE"] = "MOVE x, y [, [stylo] [, mode]]",
        ["MOVER"] = "MOVER écartx, écarty [, [stylo] [, mode]]",
        ["PLOT"] = "PLOT x, y [, [stylo] [, mode]]",
        ["PLOTR"] = "PLOTR écartx, écarty [, [stylo] [, mode]]",
        ["DRAW"] = "DRAW x, y [, [stylo] [, mode]]",
        ["DRAWR"] = "DRAWR écartx, écarty [, [stylo] [, mode]]"
    };

    public static BasicProgram Parse(IReadOnlyList<Token> tokens, List<Diagnostic> diagnostics)
    {
        var statements = new List<Statement>();
        int pos = 0;

        while (tokens[pos].Kind != TokenKind.EndOfFile)
            pos = ParseLine(tokens, pos, statements, diagnostics);

        CheckDeclarations(statements, diagnostics);
        CheckBlocks(statements, diagnostics);
        return new BasicProgram(statements);
    }

    static bool IsLineEnd(Token t) => t.Kind == TokenKind.EndOfLine || t.Kind == TokenKind.EndOfFile;

    static bool IsStatementEnd(Token t) => t.Kind == TokenKind.Colon || IsLineEnd(t);

    static bool IsElse(Token t) => t.Kind == TokenKind.Word && t.Text == "ELSE";

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

    // Chaque erreur trouvée dans une instruction reçoit le nom de la commande (PRINT, FOR, IF...), pour que
    // le message puisse dire « PRINT attend une valeur ». Une instruction placée dans une autre
    // (IF ... THEN PRINT ...) a déjà marqué ses propres erreurs : on ne touche qu'à celles sans commande.
    static int ParseStatement(IReadOnlyList<Token> tokens, int pos,
                              List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token first = tokens[pos];
        int before = diagnostics.Count;
        int next = ParseStatementCore(tokens, pos, statements, diagnostics);

        if (first.Kind == TokenKind.Word && Commands.Contains(first.Text))
        {
            for (int i = before; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Context.Length == 0)
                    diagnostics[i] = diagnostics[i] with { Context = first.Text };
            }
        }

        return next;
    }

    static int ParseStatementCore(IReadOnlyList<Token> tokens, int pos,
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
                if (tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "IF")
                {
                    statements.Add(new EndIfStatement(t.Line));   // END IF
                    return pos + 2;
                }
                statements.Add(new EndStatement(t.Line));
                return pos + 1;

            case "ENDIF":
                statements.Add(new EndIfStatement(t.Line));
                return pos + 1;

            case "ELSE":
                statements.Add(new ElseStatement(t.Line));
                return pos + 1;

            case "IF":
                return ParseIf(tokens, pos, statements, diagnostics);

            case "RANDOMIZE":
                return ParseRandomize(tokens, pos, statements, diagnostics);

            case "WHILE":
                return ParseConditionStatement(tokens, pos, statements, diagnostics, isWhile: true);

            case "UNTIL":
                return ParseConditionStatement(tokens, pos, statements, diagnostics, isWhile: false);

            case "WEND":
                statements.Add(new WendStatement(t.Line));
                return pos + 1;

            case "REPEAT":
                statements.Add(new RepeatStatement(t.Line));
                return pos + 1;

            case "DO":
                return ParseDoLoop(tokens, pos, statements, diagnostics, isLoop: false);

            case "LOOP":
                return ParseDoLoop(tokens, pos, statements, diagnostics, isLoop: true);

            case "FOR":
                return ParseFor(tokens, pos, statements, diagnostics);

            case "NEXT":
                return ParseNext(tokens, pos, statements);

            case "EXIT":
                return ParseLoopControl(tokens, pos, statements, isExit: true);

            case "CONTINUE":
                return ParseLoopControl(tokens, pos, statements, isExit: false);

            case "MODE" or "CLS" or "LOCATE" or "PEN" or "PAPER" or "INK" or "BORDER" or "CLG" or "ORIGIN" or "BOX":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands[t.Text]);

            case "MOVE" or "MOVER" or "PLOT" or "PLOTR" or "DRAW" or "DRAWR":
                return ParseDrawCommand(tokens, pos, statements, diagnostics, DrawCommands[t.Text]);

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

        while (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
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

    // MODE n | CLS | LOCATE colonne, ligne | PEN n | PAPER n | INK stylo, couleur [, couleur2] | BORDER couleur [, couleur2]
    static int ParseScreenCommand(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                                  List<Diagnostic> diagnostics, ScreenCommandInfo info)
    {
        Token name = tokens[pos];
        pos++;

        var arguments = new List<Expression>();
        if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            while (true)
            {
                Expression? argument = ParseExpression(tokens, ref pos, diagnostics);
                if (argument is null)
                    return SkipToStatementEnd(tokens, pos);
                arguments.Add(argument);

                if (tokens[pos].Kind != TokenKind.Comma)
                    break;
                pos++;
            }
        }

        if (arguments.Count < info.MinArguments || arguments.Count > info.Min.Length)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, info.Signature));
            return SkipToStatementEnd(tokens, pos);
        }

        // Une valeur écrite en nombre est vérifiée tout de suite ; une valeur calculée ne l'est pas
        for (int i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is NumberExpression number && (number.Value < info.Min[i] || number.Value > info.Max[i]))
                diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line,
                                               $"{number.Value}|{info.Min[i]} à {info.Max[i]}"));
        }

        statements.Add(new ScreenStatement(name.Line, name.Text, arguments));
        return pos;
    }

    // MOVE x, y [, [stylo] [, mode]]  (et MOVER, PLOT, PLOTR, DRAW, DRAWR). Le stylo peut rester vide : PLOT 1, 2, , 1
    static int ParseDrawCommand(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                                List<Diagnostic> diagnostics, string signature)
    {
        Token name = tokens[pos];
        pos++;

        Expression? x = ParseExpression(tokens, ref pos, diagnostics);
        if (x is null)
            return SkipToStatementEnd(tokens, pos);

        if (tokens[pos].Kind != TokenKind.Comma)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, signature));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Expression? y = ParseExpression(tokens, ref pos, diagnostics);
        if (y is null)
            return SkipToStatementEnd(tokens, pos);

        Expression? pen = null;
        Expression? mode = null;
        if (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            if (tokens[pos].Kind != TokenKind.Comma)
            {
                pen = ParseExpression(tokens, ref pos, diagnostics);
                if (pen is null)
                    return SkipToStatementEnd(tokens, pos);
            }

            if (tokens[pos].Kind == TokenKind.Comma)
            {
                pos++;
                mode = ParseExpression(tokens, ref pos, diagnostics);
                if (mode is null)
                    return SkipToStatementEnd(tokens, pos);
            }
        }

        CheckConstantRange(pen, 0, 15, name.Line, diagnostics);
        CheckConstantRange(mode, 0, 3, name.Line, diagnostics);

        statements.Add(new DrawStatement(name.Line, name.Text, x, y, pen, mode));
        return pos;
    }

    // Une valeur écrite en nombre est vérifiée tout de suite ; une valeur calculée ne l'est pas
    static void CheckConstantRange(Expression? value, int min, int max, int line, List<Diagnostic> diagnostics)
    {
        if (value is NumberExpression number && (number.Value < min || number.Value > max))
            diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, line, $"{number.Value}|{min} à {max}"));
    }

    // RANDOMIZE [graine]
    static int ParseRandomize(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                              List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        if (IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]))
        {
            statements.Add(new RandomizeStatement(line, null));
            return pos;
        }

        Expression? seed = ParseExpression(tokens, ref pos, diagnostics);
        if (seed is null)
            return SkipToStatementEnd(tokens, pos);

        statements.Add(new RandomizeStatement(line, seed));
        return pos;
    }

    // FOR variable = départ TO limite [STEP pas]
    // Si l'en-tête est incorrect, un FOR neutre est quand même conservé : le NEXT qui suit
    // n'est alors pas signalé en plus, ce qui évite des erreurs en cascade.
    static int ParseFor(IReadOnlyList<Token> tokens, int pos,
                        List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        Token name = tokens[pos];
        if (name.Kind != TokenKind.Word || Commands.Contains(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedVariableName, name.Line, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }
        if (!VariableNames.IsSupported(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnsupportedVariableType, name.Line, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Expression start = new NumberExpression(0);
        int limit = 0;
        int step = 1;
        bool ok = ParseForHeader(tokens, ref pos, diagnostics, ref start, ref limit, ref step);

        statements.Add(new ForStatement(line, name.Text, start, limit, step));
        return ok ? pos : SkipToStatementEnd(tokens, pos);
    }

    static bool ParseForHeader(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                               ref Expression start, ref int limit, ref int step)
    {
        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, tokens[pos].Line, tokens[pos].Text));
            return false;
        }
        pos++;

        Expression? first = ParseExpression(tokens, ref pos, diagnostics);
        if (first is null)
            return false;
        start = first;

        if (tokens[pos].Kind != TokenKind.Word || tokens[pos].Text != "TO")
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedTo, tokens[pos].Line, tokens[pos].Text));
            return false;
        }
        pos++;

        int? lastValue = ParseSignedNumber(tokens, ref pos, diagnostics);
        if (lastValue is null)
            return false;
        limit = lastValue.Value;

        if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "STEP")
        {
            pos++;
            int? stepValue = ParseSignedNumber(tokens, ref pos, diagnostics);
            if (stepValue is null)
                return false;
            if (stepValue.Value == 0)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.StepIsZero, tokens[pos - 1].Line));
                return false;
            }
            step = stepValue.Value;
        }

        return true;
    }

    // Un nombre écrit dans le programme, avec un signe facultatif (-32768 à 32767)
    static int? ParseSignedNumber(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        bool negative = false;
        if (tokens[pos].Kind == TokenKind.Minus)
        {
            negative = true;
            pos++;
        }
        else if (tokens[pos].Kind == TokenKind.Plus)
        {
            pos++;
        }

        Token t = tokens[pos];
        if (t.Kind != TokenKind.Number)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedNumber, t.Line, t.Text));
            return null;
        }

        return ParseNumber(tokens, ref pos, diagnostics, negative) is NumberExpression number ? number.Value : null;
    }

    // NEXT [variable]
    static int ParseNext(IReadOnlyList<Token> tokens, int pos, List<Statement> statements)
    {
        int line = tokens[pos].Line;
        pos++;

        string? name = null;
        if (tokens[pos].Kind == TokenKind.Word && !Commands.Contains(tokens[pos].Text))
        {
            name = tokens[pos].Text;
            pos++;
        }

        statements.Add(new NextStatement(line, name));
        return pos;
    }

    static LoopKind? LoopKindFromWord(string word) => word switch
    {
        "FOR" => LoopKind.For,
        "WHILE" => LoopKind.While,
        "REPEAT" => LoopKind.Repeat,
        "DO" => LoopKind.Do,
        _ => null
    };

    static Condition NeutralCondition() =>
        new ComparisonCondition(CompareOperator.Equal, new NumberExpression(0), new NumberExpression(0));

    // WHILE condition  /  UNTIL condition
    // Si la condition est incorrecte, une instruction neutre est conservée (pas d'erreur en cascade).
    static int ParseConditionStatement(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                                       List<Diagnostic> diagnostics, bool isWhile)
    {
        int line = tokens[pos].Line;
        pos++;

        Condition? condition = ParseCondition(tokens, ref pos, diagnostics);
        Condition used = condition ?? NeutralCondition();

        if (isWhile)
            statements.Add(new WhileStatement(line, used));
        else
            statements.Add(new UntilStatement(line, used));

        return condition is null ? SkipToStatementEnd(tokens, pos) : pos;
    }

    // DO [WHILE condition | UNTIL condition]  /  LOOP [WHILE condition | UNTIL condition]
    static int ParseDoLoop(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                           List<Diagnostic> diagnostics, bool isLoop)
    {
        int line = tokens[pos].Line;
        pos++;

        Condition? condition = null;
        bool untilForm = false;
        bool failed = false;

        if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text is "WHILE" or "UNTIL")
        {
            untilForm = tokens[pos].Text == "UNTIL";
            pos++;
            condition = ParseCondition(tokens, ref pos, diagnostics);
            if (condition is null)
            {
                condition = NeutralCondition();
                failed = true;
            }
        }

        if (isLoop)
            statements.Add(new LoopStatement(line, condition, untilForm));
        else
            statements.Add(new DoStatement(line, condition, untilForm));

        return failed ? SkipToStatementEnd(tokens, pos) : pos;
    }

    // EXIT [FOR | WHILE | REPEAT | DO] et CONTINUE [FOR | WHILE | REPEAT | DO]
    static int ParseLoopControl(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, bool isExit)
    {
        int line = tokens[pos].Line;
        pos++;

        LoopKind? kind = tokens[pos].Kind == TokenKind.Word ? LoopKindFromWord(tokens[pos].Text) : null;
        if (kind is not null)
            pos++;

        if (isExit)
            statements.Add(new ExitStatement(line, kind));
        else
            statements.Add(new ContinueStatement(line, kind));
        return pos;
    }

    // IF sur une ligne ou en bloc (fermé par END IF) :
    //   IF condition THEN instructions [ELSE instructions]
    //   IF condition THEN  (fin de ligne) ... [ELSE ...] END IF
    // Si la condition est incorrecte, un IF neutre est quand même conservé pour éviter
    // des erreurs en cascade, et le reste de la ligne est ignoré.
    static int ParseIf(IReadOnlyList<Token> tokens, int pos,
                       List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        Condition? condition = ParseCondition(tokens, ref pos, diagnostics);
        bool ok = condition is not null;

        if (ok)
        {
            if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "THEN")
            {
                pos++;
            }
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedThen, tokens[pos].Line, tokens[pos].Text));
                ok = false;
            }
        }

        condition ??= new ComparisonCondition(CompareOperator.Equal, new NumberExpression(0), new NumberExpression(0));
        statements.Add(new IfStatement(line, condition));

        if (!ok)
        {
            // On devine la forme : un THEN suivi d'instructions sur la même ligne = IF sur une ligne
            bool oneLine = false;
            for (int k = pos; !IsLineEnd(tokens[k]); k++)
            {
                if (tokens[k].Kind == TokenKind.Word && tokens[k].Text == "THEN" && !IsLineEnd(tokens[k + 1]))
                    oneLine = true;
            }

            while (!IsLineEnd(tokens[pos]))
                pos++;
            if (oneLine)
                statements.Add(new EndIfStatement(line));
            return pos;
        }

        if (IsLineEnd(tokens[pos]))
            return pos;   // forme bloc : refermée par END IF

        // Forme sur une ligne : tout le reste de la ligne appartient au IF
        while (!IsLineEnd(tokens[pos]))
        {
            if (IsElse(tokens[pos]))
            {
                statements.Add(new ElseStatement(tokens[pos].Line));
                pos++;
                continue;
            }

            pos = ParseStatement(tokens, pos, statements, diagnostics);

            if (tokens[pos].Kind == TokenKind.Colon)
            {
                pos++;
                continue;
            }

            if (!IsLineEnd(tokens[pos]) && !IsElse(tokens[pos]))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, tokens[pos].Line, tokens[pos].Text));
                pos = SkipToStatementEnd(tokens, pos);
                if (tokens[pos].Kind == TokenKind.Colon)
                    pos++;
            }
        }

        statements.Add(new EndIfStatement(line));
        return pos;
    }

    // condition := et { OR et }      et := comparaison { AND comparaison }
    static Condition? ParseCondition(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Condition? left = ParseAndCondition(tokens, ref pos, diagnostics);

        while (left is not null && tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "OR")
        {
            pos++;
            Condition? right = ParseAndCondition(tokens, ref pos, diagnostics);
            if (right is null)
                return null;
            left = new OrCondition(left, right);
        }

        return left;
    }

    static Condition? ParseAndCondition(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Condition? left = ParseComparison(tokens, ref pos, diagnostics);

        while (left is not null && tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "AND")
        {
            pos++;
            Condition? right = ParseComparison(tokens, ref pos, diagnostics);
            if (right is null)
                return null;
            left = new AndCondition(left, right);
        }

        return left;
    }

    static Condition? ParseComparison(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseExpression(tokens, ref pos, diagnostics);
        if (left is null)
            return null;

        CompareOperator? op = tokens[pos].Kind switch
        {
            TokenKind.Equals => CompareOperator.Equal,
            TokenKind.NotEqual => CompareOperator.NotEqual,
            TokenKind.Less => CompareOperator.Less,
            TokenKind.LessEqual => CompareOperator.LessOrEqual,
            TokenKind.Greater => CompareOperator.Greater,
            TokenKind.GreaterEqual => CompareOperator.GreaterOrEqual,
            _ => null
        };

        if (op is null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedComparison, tokens[pos].Line, tokens[pos].Text));
            return null;
        }
        pos++;

        Expression? right = ParseExpression(tokens, ref pos, diagnostics);
        return right is null ? null : new ComparisonCondition(op.Value, left, right);
    }

    sealed class OpenBlock(Statement node)
    {
        public Statement Node { get; } = node;
        public bool ElseSeen { get; set; }
    }

    static LoopKind? KindOf(Statement statement) => statement switch
    {
        ForStatement => LoopKind.For,
        WhileStatement => LoopKind.While,
        RepeatStatement => LoopKind.Repeat,
        DoStatement => LoopKind.Do,
        _ => null
    };

    static string KeywordOf(Statement statement) => statement switch
    {
        ForStatement => "FOR",
        WhileStatement => "WHILE",
        RepeatStatement => "REPEAT",
        DoStatement => "DO",
        _ => "IF"
    };

    // Une boucle (du type demandé, ou de n'importe quel type si kind est null) est-elle ouverte ?
    static bool HasLoop(Stack<OpenBlock> open, LoopKind? kind) =>
        open.Any(b => KindOf(b.Node) is LoopKind k && (kind is null || k == kind));

    static string ControlName(string word, LoopKind? kind) =>
        kind is null ? word : word + " " + kind.Value.ToString().ToUpperInvariant();

    // Vérifie que les blocs (FOR, WHILE, REPEAT, DO, IF) sont refermés et bien imbriqués
    static void CheckBlocks(IReadOnlyList<Statement> statements, List<Diagnostic> diagnostics)
    {
        var open = new Stack<OpenBlock>();

        // Referme le bloc le plus récent s'il est du bon type ; renvoie le bloc refermé, ou null
        Statement? Close(int line, string keyword, DiagnosticCode whenNothingOpen, Func<Statement, bool> matches)
        {
            if (open.Count == 0)
            {
                diagnostics.Add(new Diagnostic(whenNothingOpen, line, keyword));
                return null;
            }
            if (!matches(open.Peek().Node))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.BlocksNotNested, line, keyword));
                return null;
            }
            return open.Pop().Node;
        }

        foreach (Statement statement in statements)
        {
            switch (statement)
            {
                case ForStatement or IfStatement or WhileStatement or RepeatStatement or DoStatement:
                    open.Push(new OpenBlock(statement));
                    break;

                case NextStatement next:
                    if (Close(next.SourceLine, "NEXT", DiagnosticCode.NextWithoutFor, n => n is ForStatement)
                            is ForStatement closedFor
                        && next.Variable is not null && next.Variable != closedFor.Variable)
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.NextNameMismatch, next.SourceLine, closedFor.Variable));
                    }
                    break;

                case WendStatement:
                    Close(statement.SourceLine, "WEND", DiagnosticCode.ClosingWithoutOpening, n => n is WhileStatement);
                    break;

                case UntilStatement:
                    Close(statement.SourceLine, "UNTIL", DiagnosticCode.ClosingWithoutOpening, n => n is RepeatStatement);
                    break;

                case LoopStatement loopEnd:
                    if (Close(loopEnd.SourceLine, "LOOP", DiagnosticCode.ClosingWithoutOpening, n => n is DoStatement)
                            is DoStatement opener
                        && opener.Condition is not null && loopEnd.Condition is not null)
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.DoConditionTwice, loopEnd.SourceLine));
                    }
                    break;

                case EndIfStatement:
                    Close(statement.SourceLine, "END IF", DiagnosticCode.EndIfWithoutIf, n => n is IfStatement);
                    break;

                case ElseStatement:
                    if (open.Count == 0)
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ElseWithoutIf, statement.SourceLine));
                        break;
                    }
                    OpenBlock top = open.Peek();
                    if (top.Node is not IfStatement)
                        diagnostics.Add(new Diagnostic(DiagnosticCode.BlocksNotNested, statement.SourceLine, "ELSE"));
                    else if (top.ElseSeen)
                        diagnostics.Add(new Diagnostic(DiagnosticCode.DuplicateElse, statement.SourceLine));
                    else
                        top.ElseSeen = true;
                    break;

                case ExitStatement exit when !HasLoop(open, exit.Kind):
                    diagnostics.Add(new Diagnostic(DiagnosticCode.LoopControlWithoutLoop, exit.SourceLine,
                        ControlName("EXIT", exit.Kind)));
                    break;

                case ContinueStatement skip when !HasLoop(open, skip.Kind):
                    diagnostics.Add(new Diagnostic(DiagnosticCode.LoopControlWithoutLoop, skip.SourceLine,
                        ControlName("CONTINUE", skip.Kind)));
                    break;
            }
        }

        foreach (OpenBlock block in open)
        {
            switch (block.Node)
            {
                case ForStatement loop:
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ForWithoutNext, loop.SourceLine, loop.Variable));
                    break;
                case IfStatement:
                    diagnostics.Add(new Diagnostic(DiagnosticCode.IfWithoutEndIf, block.Node.SourceLine));
                    break;
                default:
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnclosedLoop, block.Node.SourceLine, KeywordOf(block.Node)));
                    break;
            }
        }
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
                case CallExpression call:
                    foreach (Expression argument in call.Arguments)
                        Uses(argument);
                    break;
            }
        }

        void UsesCondition(Condition condition)
        {
            switch (condition)
            {
                case ComparisonCondition comparison:
                    Uses(comparison.Left);
                    Uses(comparison.Right);
                    break;
                case AndCondition both:
                    UsesCondition(both.Left);
                    UsesCondition(both.Right);
                    break;
                case OrCondition either:
                    UsesCondition(either.Left);
                    UsesCondition(either.Right);
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

                case RandomizeStatement randomize when randomize.Seed is not null:
                    Uses(randomize.Seed);
                    break;

                case IfStatement ifStatement:
                    UsesCondition(ifStatement.Condition);
                    break;

                case WhileStatement whileStatement:
                    UsesCondition(whileStatement.Condition);
                    break;

                case UntilStatement untilStatement:
                    UsesCondition(untilStatement.Condition);
                    break;

                case DoStatement doStatement when doStatement.Condition is not null:
                    UsesCondition(doStatement.Condition);
                    break;

                case LoopStatement loopStatement when loopStatement.Condition is not null:
                    UsesCondition(loopStatement.Condition);
                    break;

                case ForStatement loop:
                    Uses(loop.Start);
                    used.Add(loop.Variable);
                    if (bytes.Contains(loop.Variable))
                    {
                        if (loop.Start is NumberExpression first && (first.Value < 0 || first.Value > 255))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, first.Value.ToString()));
                        if (loop.Limit < 0 || loop.Limit > 255)
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, loop.Limit.ToString()));
                        if (loop.Step < -255 || loop.Step > 255)
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, loop.Step.ToString()));
                    }
                    break;

                case PrintValueStatement print:
                    Uses(print.Value);
                    break;

                case ScreenStatement screen:
                    foreach (Expression argument in screen.Arguments)
                        Uses(argument);
                    break;

                case DrawStatement draw:
                    Uses(draw.X);
                    Uses(draw.Y);
                    if (draw.Pen is not null)
                        Uses(draw.Pen);
                    if (draw.Mode is not null)
                        Uses(draw.Mode);
                    break;
            }
        }
    }

    // ----- Expressions : renvoient null (après avoir noté l'erreur) si l'expression est incorrecte -----

    static Expression? ParseExpression(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseTerm(tokens, ref pos, diagnostics);

        while (left is not null && (tokens[pos].Kind == TokenKind.Plus || tokens[pos].Kind == TokenKind.Minus))
        {
            BinaryOperator op = tokens[pos].Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            pos++;

            Expression? right = ParseTerm(tokens, ref pos, diagnostics);
            if (right is null)
                return null;
            left = new BinaryExpression(op, left, right);
        }

        return left;
    }

    static bool IsMultiplicative(Token t) =>
        t.Kind == TokenKind.Star || t.Kind == TokenKind.Slash || (t.Kind == TokenKind.Word && t.Text == "MOD");

    // terme := opérande { ("*" | "/" | "\\" | MOD) opérande }   (passe avant + et -)
    static Expression? ParseTerm(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseOperand(tokens, ref pos, diagnostics);

        while (left is not null && IsMultiplicative(tokens[pos]))
        {
            Token opToken = tokens[pos];
            BinaryOperator op = opToken.Kind switch
            {
                TokenKind.Star => BinaryOperator.Multiply,
                TokenKind.Slash => BinaryOperator.Divide,
                _ => BinaryOperator.Modulo
            };
            pos++;

            Expression? right = ParseOperand(tokens, ref pos, diagnostics);
            if (right is null)
                return null;

            if (op != BinaryOperator.Multiply && right is NumberExpression { Value: 0 })
                diagnostics.Add(new Diagnostic(DiagnosticCode.DivisionByZero, opToken.Line, opToken.Text));

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
                if (t.Text == "TIME")
                {
                    pos++;                              // TIME s'écrit sans parenthèses, comme sur le CPC
                    return new CallExpression("TIME", []);
                }
                if (Functions.TryGetValue(t.Text, out FunctionInfo? function))
                    return ParseFunctionCall(tokens, ref pos, diagnostics, function);
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

    // NOM(valeur, ...) : MIN et MAX acceptent plus de deux valeurs (MIN(9, 4, 6) = MIN(MIN(9, 4), 6))
    static Expression? ParseFunctionCall(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                                         FunctionInfo function)
    {
        Token nameToken = tokens[pos];
        pos++;

        if (tokens[pos].Kind != TokenKind.LeftParen)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedOpenParenthesis, nameToken.Line, function.Signature));
            return null;
        }
        pos++;

        var arguments = new List<Expression>();
        if (tokens[pos].Kind != TokenKind.RightParen)
        {
            while (true)
            {
                Expression? argument = ParseExpression(tokens, ref pos, diagnostics);
                if (argument is null)
                    return null;
                arguments.Add(argument);

                if (tokens[pos].Kind != TokenKind.Comma)
                    break;
                pos++;
            }
        }

        if (tokens[pos].Kind != TokenKind.RightParen)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, nameToken.Line));
            return null;
        }
        pos++;

        if (arguments.Count < function.MinArguments || arguments.Count > function.MaxArguments)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, nameToken.Line, function.Signature));
            return null;
        }

        if (nameToken.Text is "MIN" or "MAX")
        {
            // Plus de deux valeurs : MIN(a, b, c) devient MIN(MIN(a, b), c)
            Expression result = new CallExpression(nameToken.Text, [arguments[0], arguments[1]]);
            for (int i = 2; i < arguments.Count; i++)
                result = new CallExpression(nameToken.Text, [result, arguments[i]]);
            return result;
        }

        return new CallExpression(nameToken.Text, arguments);
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
