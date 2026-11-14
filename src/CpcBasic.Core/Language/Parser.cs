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
        "MOVE", "MOVER", "PLOT", "PLOTR", "DRAW", "DRAWR", "CLG", "ORIGIN", "BOX",
        "GOTO", "GOSUB", "RETURN", "STOP", "ON", "INKEY", "JOY", "CLEAR", "INPUT", "POKE", "PEEK", "OUT", "INP", "CALL", "MEMORY", "DIM",
        "DATA", "READ", "RESTORE", "XOR", "NOT", "LEN",
        "LEFT$", "RIGHT$", "MID$", "CHR$", "ASC", "STR$", "VAL", "STRING$", "SPACE$", "UPPER$", "LOWER$", "INSTR", "HEX$", "BIN$"];

    // Tous les mots du Locomotive BASIC (D058) : un mot pas encore disponible ici ne peut pas devenir un nom de tableau
    // (sinon SIN(3) serait pris pour la case 3 d'un tableau SIN, sans aucun message).
    static readonly HashSet<string> LocomotiveWords =
    [
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
    ];

    // Patterns (D062, D063) : pour chaque nombre de valeurs permis, le genre de chacune : N nombre, S texte, A l'un ou l'autre.
    // Sans Patterns, toutes les valeurs sont des nombres.
    sealed record FunctionInfo(string Signature, int MinArguments, int MaxArguments, bool FirstIsAddress = false,
                               string[]? Patterns = null);

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
        ["RANDOM"] = new("RANDOM(min, max)", 2, 2),
        ["INKEY"] = new("INKEY(numéro de touche)", 1, 1),
        ["JOY"] = new("JOY(0 ou 1)", 1, 1),
        // D056 : accès à la machine ; l'adresse ou le port peut s'écrire de 0 à 65535 (comme sur le CPC) ou de -32768 à 32767
        ["PEEK"] = new("PEEK(adresse)", 1, 1, true),
        // Lot E (D062) : fonctions sur les textes
        ["LEN"] = new("LEN(texte)", 1, 1, Patterns: ["S"]),
        // Étape E2 (D063)
        ["LEFT$"] = new("LEFT$(texte, nombre)", 2, 2, Patterns: ["SN"]),
        ["RIGHT$"] = new("RIGHT$(texte, nombre)", 2, 2, Patterns: ["SN"]),
        ["MID$"] = new("MID$(texte, début [, longueur])", 2, 3, Patterns: ["SN", "SNN"]),
        ["CHR$"] = new("CHR$(code)", 1, 1),
        ["ASC"] = new("ASC(texte)", 1, 1, Patterns: ["S"]),
        ["STR$"] = new("STR$(nombre)", 1, 1),
        ["VAL"] = new("VAL(texte)", 1, 1, Patterns: ["S"]),
        ["STRING$"] = new("STRING$(nombre, caractère)", 2, 2, Patterns: ["NA"]),
        ["SPACE$"] = new("SPACE$(nombre)", 1, 1),
        ["UPPER$"] = new("UPPER$(texte)", 1, 1, Patterns: ["S"]),
        ["LOWER$"] = new("LOWER$(texte)", 1, 1, Patterns: ["S"]),
        ["INSTR"] = new("INSTR([début,] texte, texte cherché)", 2, 3, Patterns: ["SS", "NSS"]),
        ["HEX$"] = new("HEX$(nombre [, chiffres])", 1, 2),
        ["BIN$"] = new("BIN$(nombre [, chiffres])", 1, 2),
        ["INP"] = new("INP(port)", 1, 1, true)
    };

    /// <summary>Commande d'écran : écriture à montrer dans les messages, nombre de valeurs, valeurs permises pour chacune.</summary>
    sealed record ScreenCommandInfo(string Signature, int MinArguments, int[] Min, int[] Max, bool FirstIsAddress = false);

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
        // D056 : accès à la machine
        ["POKE"] = new("POKE adresse, valeur (0 à 255)", 2, [-32768, 0], [32767, 255], true),
        ["OUT"] = new("OUT port, valeur (0 à 255)", 2, [-32768, 0], [32767, 255], true),
        ["CALL"] = new("CALL adresse", 1, [-32768], [32767], true),
        ["MEMORY"] = new("MEMORY adresse", 1, [-32768], [32767], true),
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
        var lines = new Dictionary<int, int>();   // numéro de ligne -> rang de l'instruction qui la suit
        int pos = 0;

        while (tokens[pos].Kind != TokenKind.EndOfFile)
            pos = ParseLine(tokens, pos, statements, diagnostics, lines);

        CheckDeclarations(statements, diagnostics);
        CheckArrays(statements, diagnostics);
        CheckBlocks(statements, diagnostics);
        CheckLineTargets(statements, lines, diagnostics);
        return new BasicProgram(statements, lines);
    }

    const int MaxLineNumber = 65529;   // plus grand numéro de ligne du Locomotive BASIC

    static bool IsLineEnd(Token t) => t.Kind == TokenKind.EndOfLine || t.Kind == TokenKind.EndOfFile;

    static bool IsStatementEnd(Token t) => t.Kind == TokenKind.Colon || IsLineEnd(t);

    static bool IsElse(Token t) => t.Kind == TokenKind.Word && t.Text == "ELSE";

    static int ParseLine(IReadOnlyList<Token> tokens, int pos,
                         List<Statement> statements, List<Diagnostic> diagnostics,
                         Dictionary<int, int> lines)
    {
        if (tokens[pos].Kind == TokenKind.LineNumber)
        {
            // Le numéro de ligne devient une cible possible pour GOTO, GOSUB et ON (D054)
            if (int.TryParse(tokens[pos].Text, out int number) && number is >= 1 and <= MaxLineNumber)
            {
                if (!lines.TryAdd(number, statements.Count))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.DuplicateLineNumber, tokens[pos].Line, tokens[pos].Text));
            }
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, tokens[pos].Line, tokens[pos].Text));
            }
            pos++;
        }

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

            case "MODE" or "CLS" or "LOCATE" or "PEN" or "PAPER" or "INK" or "BORDER" or "CLG" or "ORIGIN" or "BOX" or "POKE" or "OUT" or "CALL" or "MEMORY":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands[t.Text]);

            case "MOVE" or "MOVER" or "PLOT" or "PLOTR" or "DRAW" or "DRAWR":
                return ParseDrawCommand(tokens, pos, statements, diagnostics, DrawCommands[t.Text]);

            case "BYTE":
                return ParseDeclaration(tokens, pos, statements, diagnostics);

            case "DIM":
                return ParseDim(tokens, pos, statements, diagnostics);

            case "DATA":
                return ParseData(tokens, pos, statements, diagnostics);

            case "READ":
                return ParseRead(tokens, pos, statements, diagnostics);

            case "RESTORE":
                return ParseRestore(tokens, pos, statements, diagnostics);

            case "GOTO":
                return ParseJump(tokens, pos, 1, false, statements, diagnostics);

            case "GOSUB":
                return ParseJump(tokens, pos, 1, true, statements, diagnostics);

            // « GO TO » et « GO SUB » s'écrivent aussi en deux mots ; « GO » seul reste un nom de variable possible
            case "GO" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text is "TO" or "SUB":
                return ParseJump(tokens, pos, 2, tokens[pos + 1].Text == "SUB", statements, diagnostics);

            case "RETURN":
                statements.Add(new ReturnStatement(t.Line));
                return pos + 1;

            case "STOP":
                statements.Add(new StopStatement(t.Line));
                return pos + 1;

            case "ON":
                return ParseOn(tokens, pos, statements, diagnostics);

            case "INPUT":
                return ParseInput(tokens, pos, statements, diagnostics);

            case "CLEAR" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "INPUT":
                statements.Add(new ClearInputStatement(t.Line));
                return pos + 2;

            case "LET":
                return ParseAssignment(tokens, pos + 1, t.Line, statements, diagnostics);

            default:
                // Un mot suivi de « = » est une affectation sans LET ; suivi de « ( », c'est une case de tableau : A(3) = 5 (D058)
                if (tokens[pos + 1].Kind is TokenKind.Equals or TokenKind.LeftParen)
                    return ParseAssignment(tokens, pos, t.Line, statements, diagnostics);

                diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, t.Line, t.Text));
                return SkipToStatementEnd(tokens, pos);
        }
    }

    // INPUT [;] ["texte" ; ou ,] variable [, variable ...]
    static int ParseInput(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        bool noNewline = false;
        string? prompt = null;
        bool question = true;

        if (tokens[pos].Kind == TokenKind.Semicolon)
        {
            noNewline = true;
            pos++;
        }

        if (tokens[pos].Kind == TokenKind.String)
        {
            prompt = tokens[pos].Text;
            pos++;
            if (tokens[pos].Kind == TokenKind.Semicolon)
            {
                pos++;                                  // texte ; variable : le « ? » suit le texte
            }
            else if (tokens[pos].Kind == TokenKind.Comma)
            {
                question = false;                       // texte , variable : pas de « ? »
                pos++;
            }
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, tokens[pos].Line, tokens[pos].Text));
                return SkipToStatementEnd(tokens, pos);
            }
        }

        var variables = new List<string>();
        var indexes = new List<IReadOnlyList<Expression>?>();
        while (true)
        {
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
            variables.Add(name.Text);
            pos++;

            // INPUT T(I) : le nombre tapé va dans une case de tableau (D059)
            if (tokens[pos].Kind == TokenKind.LeftParen)
            {
                if (!CheckArrayName(name, diagnostics) || !TryParseIndexes(tokens, ref pos, diagnostics, out List<Expression> cases))
                    return SkipToStatementEnd(tokens, pos);
                indexes.Add(cases);
            }
            else
            {
                indexes.Add(null);
            }

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new InputStatement(line, prompt, question, noNewline, variables,
                                          indexes.Any(i => i is not null) ? indexes : null));
        return pos;
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

        // A(3) = valeur : case de tableau (D058)
        List<Expression>? indexes = null;
        if (tokens[pos].Kind == TokenKind.LeftParen)
        {
            if (!CheckArrayName(name, diagnostics) || !TryParseIndexes(tokens, ref pos, diagnostics, out indexes))
                return SkipToStatementEnd(tokens, pos);
        }

        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, tokens[pos].Line, tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Token valueStart = tokens[pos];
        Expression? value = indexes is null
            ? ParseAnyExpression(tokens, ref pos, diagnostics)
            : ParseExpression(tokens, ref pos, diagnostics);
        if (value is null)
            return SkipToStatementEnd(tokens, pos);

        // A$ reçoit un texte, A reçoit un nombre (D062)
        if (indexes is null && VariableNames.IsString(name.Text) != Types.IsString(value))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, valueStart.Line,
                                           VariableNames.IsString(name.Text) ? "un texte est attendu" : "un nombre est attendu"));
            return SkipToStatementEnd(tokens, pos);
        }

        statements.Add(indexes is null
            ? new AssignStatement(line, name.Text, value)
            : (Statement)new ArrayAssignStatement(line, name.Text, indexes, value));
        return pos;
    }

    // ----- DATA, READ, RESTORE (lot C, D059) -----

    // DATA nombre [, nombre ...] : entiers de -32768 à 65535 (de 32768 à 65535, même mot de 16 bits que sur le CPC)
    static int ParseData(IReadOnlyList<Token> tokens, int pos,
                         List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var values = new List<int>();
        while (true)
        {
            bool negative = false;
            if (tokens[pos].Kind is TokenKind.Plus or TokenKind.Minus)
            {
                negative = tokens[pos].Kind == TokenKind.Minus;
                pos++;
            }

            Token t = tokens[pos];
            if (t.Kind == TokenKind.HexNumber)
            {
                int hex = Signed16(Lexer.HexValue(t.Text) ?? 0);
                values.Add(negative ? Signed16(-hex & 0xFFFF) : hex);
                pos++;
                if (tokens[pos].Kind != TokenKind.Comma)
                    break;
                pos++;
                continue;
            }
            if (t.Kind == TokenKind.String || (t.Kind == TokenKind.Word && !negative))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.DataTextNotYet, t.Line, t.Text));
                return SkipToStatementEnd(tokens, pos);
            }
            if (t.Kind != TokenKind.Number)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedDataValue, t.Line, IsStatementEnd(t) ? "" : t.Text));
                return SkipToStatementEnd(tokens, pos);
            }
            if (!long.TryParse(t.Text, out long value) || value > (negative ? 32768 : 65535))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, t.Line, (negative ? "-" : "") + t.Text));
                return SkipToStatementEnd(tokens, pos);
            }

            int word = (int)(negative ? -value : value);
            values.Add(word > 32767 ? word - 65536 : word);
            pos++;

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new DataStatement(line, values));
        return pos;
    }

    // Ce qui reçoit une valeur lue : une variable (A, A%) ou une case de tableau (T(I))
    static Expression? ParseTarget(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        if (name.Kind != TokenKind.Word || Commands.Contains(name.Text) || Functions.ContainsKey(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedTarget, name.Line, IsStatementEnd(name) ? "" : name.Text));
            return null;
        }
        if (!VariableNames.IsSupported(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnsupportedVariableType, name.Line, name.Text));
            return null;
        }
        if (VariableNames.IsString(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.StringNotYetHere, name.Line, name.Text));   // READ A$, @A$ : étape E3
            return null;
        }
        pos++;

        if (tokens[pos].Kind != TokenKind.LeftParen)
            return new VariableExpression(name.Text);

        if (!CheckArrayName(name, diagnostics) || !TryParseIndexes(tokens, ref pos, diagnostics, out List<Expression> indexes))
            return null;
        return new ArrayElementExpression(name.Text, indexes);
    }

    // READ cible [, cible ...]
    static int ParseRead(IReadOnlyList<Token> tokens, int pos,
                         List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var targets = new List<Expression>();
        while (true)
        {
            Expression? target = ParseTarget(tokens, ref pos, diagnostics);
            if (target is null)
                return SkipToStatementEnd(tokens, pos);
            targets.Add(target);

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new ReadStatement(line, targets));
        return pos;
    }

    // RESTORE [numéro de ligne]
    static int ParseRestore(IReadOnlyList<Token> tokens, int pos,
                            List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        if (IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]))
        {
            statements.Add(new RestoreStatement(line, null));
            return pos;
        }

        if (!TryParseTarget(tokens, ref pos, diagnostics, out int target))
            return SkipToStatementEnd(tokens, pos);

        statements.Add(new RestoreStatement(line, target));
        return pos;
    }

    // ----- Tableaux (lot C, D058) -----

    // Un nom de tableau ne peut pas être un mot du Locomotive BASIC
    static bool CheckArrayName(Token name, List<Diagnostic> diagnostics)
    {
        if (VariableNames.IsString(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.StringNotYetHere, name.Line, name.Text));   // tableaux de textes : étape E3
            return false;
        }
        if (LocomotiveWords.Contains(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.NotYetAvailable, name.Line, name.Text));
            return false;
        }
        return true;
    }

    // « ( valeur [, valeur ...] ) » : les numéros de case (ou les tailles pour DIM). pos est sur « ( ».
    static bool TryParseIndexes(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                                out List<Expression> indexes)
    {
        indexes = [];
        int line = tokens[pos].Line;
        pos++;

        while (true)
        {
            Expression? index = ParseExpression(tokens, ref pos, diagnostics);
            if (index is null)
                return false;
            indexes.Add(index);

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        if (tokens[pos].Kind != TokenKind.RightParen)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, line));
            return false;
        }
        pos++;
        return true;
    }

    // DIM A(10) [, B(5, 5) ...] : la plus grande case de chaque dimension, de 0 à 32766
    static int ParseDim(IReadOnlyList<Token> tokens, int pos,
                        List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var arrays = new List<ArrayDeclaration>();
        while (true)
        {
            Token name = tokens[pos];
            bool isName = name.Kind == TokenKind.Word && !Commands.Contains(name.Text) && !Functions.ContainsKey(name.Text);

            if (!isName || tokens[pos + 1].Kind != TokenKind.LeftParen)
            {
                Token wrong = isName ? tokens[pos + 1] : name;
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedArrayName, wrong.Line, wrong.Text));
                return SkipToStatementEnd(tokens, pos);
            }

            if (!VariableNames.IsSupported(name.Text))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnsupportedVariableType, name.Line, name.Text));
                return SkipToStatementEnd(tokens, pos);
            }

            if (!CheckArrayName(name, diagnostics))
                return SkipToStatementEnd(tokens, pos);

            pos++;
            if (!TryParseIndexes(tokens, ref pos, diagnostics, out List<Expression> sizes))
                return SkipToStatementEnd(tokens, pos);

            foreach (Expression size in sizes)
                CheckConstantRange(size, 0, MaxArrayIndex, line, diagnostics);

            arrays.Add(new ArrayDeclaration(name.Text, sizes));

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new DimStatement(line, arrays));
        return pos;
    }

    /// <summary>Un mot de 16 bits (0 à 65535) vu comme un entier signé (-32768 à 32767) : &amp;FFFF = -1.</summary>
    static int Signed16(int value) => value > 32767 ? value - 65536 : value;

    const int MaxArrayIndex = 32766;   // plus grande case possible : un tableau a au plus 32767 cases

    // Un même tableau garde toujours le même nombre de numéros de case (DIM compris), et un nom déclaré BYTE
    // ne peut pas encore être un tableau (les tableaux d'octets viendront plus tard).
    static void CheckArrays(IReadOnlyList<Statement> statements, List<Diagnostic> diagnostics)
    {
        var bytes = statements.OfType<DeclareStatement>().SelectMany(d => d.Names).ToHashSet();
        var dimensions = new Dictionary<string, int>();
        var reported = new HashSet<string>();

        void Use(string name, int count, int line)
        {
            if (bytes.Contains(name))
            {
                if (reported.Add(name))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ByteArrayNotYet, line, name));
                return;
            }

            if (!dimensions.TryGetValue(name, out int known))
                dimensions[name] = count;
            else if (known != count)
                diagnostics.Add(new Diagnostic(DiagnosticCode.ArrayDimensionMismatch, line, $"{name}|{count}|{known}"));
        }

        foreach (Statement statement in statements)
        {
            if (statement is DimStatement dim)
                foreach (ArrayDeclaration array in dim.Arrays)
                    Use(array.Name, array.Sizes.Count, dim.SourceLine);

            if (statement is ArrayAssignStatement assign)
                Use(assign.Name, assign.Indexes.Count, assign.SourceLine);

            foreach (Expression expression in AstWalker.All(statement))
                if (expression is ArrayElementExpression element)
                    Use(element.Name, element.Indexes.Count, statement.SourceLine);
        }
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
            Token after = tokens[pos + 1];
            if (tokens[pos].Kind == TokenKind.String
                && (after.Kind == TokenKind.Semicolon || IsStatementEnd(after) || IsElse(after) || after.Kind == TokenKind.String))
            {
                items.Add(tokens[pos].Text);
                pos++;
            }
            else
            {
                Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);   // nombre ou texte (A$, "A" + B$)
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
                Expression? argument = info.FirstIsAddress && arguments.Count == 0
                    ? ParseAddressArgument(tokens, ref pos, diagnostics)
                    : ParseExpression(tokens, ref pos, diagnostics);
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
        if (VariableNames.IsString(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, name.Line, name.Text));   // FOR A$ : impossible
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
        if (t.Kind == TokenKind.HexNumber)
        {
            pos++;
            int value = Signed16(Lexer.HexValue(t.Text) ?? 0);
            return negative ? -value : value;
        }
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

    // Lit un numéro de ligne (nombre entier de 1 à 65529) ; renvoie faux et signale l'erreur sinon
    static bool TryParseTarget(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics, out int target)
    {
        target = 0;
        Token t = tokens[pos];
        if (t.Kind != TokenKind.Number)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedLineNumber, t.Line, IsLineEnd(t) ? "" : t.Text));
            return false;
        }

        if (!int.TryParse(t.Text, out target) || target < 1 || target > MaxLineNumber)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, t.Line, t.Text));
            return false;
        }

        pos++;
        return true;
    }

    // GOTO ligne | GOSUB ligne | GO TO ligne | GO SUB ligne (wordCount = 1 ou 2 mots avant le numéro)
    static int ParseJump(IReadOnlyList<Token> tokens, int pos, int wordCount, bool isGosub,
                         List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos += wordCount;

        if (!TryParseTarget(tokens, ref pos, diagnostics, out int target))
            return SkipToStatementEnd(tokens, pos);

        statements.Add(isGosub ? new GosubStatement(line, target) : new GotoStatement(line, target));
        return pos;
    }

    // ON valeur GOTO ligne1, ligne2... | ON valeur GOSUB ligne1, ligne2...
    static int ParseOn(IReadOnlyList<Token> tokens, int pos,
                       List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;

        // ON BREAK, ON ERROR et ON SQ ne sont pas encore gérés
        if (tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text is "BREAK" or "ERROR" or "SQ")
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, line, "ON " + tokens[pos + 1].Text));
            return SkipToStatementEnd(tokens, pos);
        }

        pos++;
        Expression? selector = ParseExpression(tokens, ref pos, diagnostics);
        if (selector is null)
            return SkipToStatementEnd(tokens, pos);

        bool isGosub;
        if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text is "GOTO" or "GOSUB")
        {
            isGosub = tokens[pos].Text == "GOSUB";
            pos++;
        }
        else if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "GO"
                 && tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text is "TO" or "SUB")
        {
            isGosub = tokens[pos + 1].Text == "SUB";
            pos += 2;
        }
        else
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, tokens[pos].Line, IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }

        var targets = new List<int>();
        while (true)
        {
            if (!TryParseTarget(tokens, ref pos, diagnostics, out int target))
                return SkipToStatementEnd(tokens, pos);
            targets.Add(target);

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new OnStatement(line, selector, targets, isGosub));
        return pos;
    }

    // Chaque GOTO, GOSUB et ON doit viser une ligne qui existe
    static void CheckLineTargets(IReadOnlyList<Statement> statements, Dictionary<int, int> lines, List<Diagnostic> diagnostics)
    {
        void Check(int sourceLine, int target, string name)
        {
            if (!lines.ContainsKey(target))
                diagnostics.Add(new Diagnostic(DiagnosticCode.LineNotFound, sourceLine, target.ToString(), name));
        }

        foreach (Statement statement in statements)
        {
            switch (statement)
            {
                case GotoStatement jump:
                    Check(jump.SourceLine, jump.Target, "GOTO");
                    break;
                case GosubStatement call:
                    Check(call.SourceLine, call.Target, "GOSUB");
                    break;
                case OnStatement on:
                    foreach (int target in on.Targets)
                        Check(on.SourceLine, target, on.IsGosub ? "ON … GOSUB" : "ON … GOTO");
                    break;
                case RestoreStatement { Line: int restoreLine } restore:
                    Check(restore.SourceLine, restoreLine, "RESTORE");
                    break;
            }
        }
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

            if (tokens[pos].Kind == TokenKind.Number && tokens[pos - 1].Kind == TokenKind.Word
                && tokens[pos - 1].Text is "THEN" or "ELSE")
            {
                // IF condition THEN 100 ELSE 200 : un numéro de ligne seul veut dire GOTO
                int gotoLine = tokens[pos].Line;
                if (TryParseTarget(tokens, ref pos, diagnostics, out int gotoTarget))
                    statements.Add(new GotoStatement(gotoLine, gotoTarget));
                else
                    pos = SkipToStatementEnd(tokens, pos);
            }
            else
            {
                pos = ParseStatement(tokens, pos, statements, diagnostics);
            }

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
    // Une condition est une expression comme une autre (lot D, D060) : IF A THEN, IF A AND 4 THEN, IF A > 1 AND B < 2 THEN.
    // Les comparaisons reliées par AND, OR et NOT deviennent des sauts rapides ; le reste est testé « différent de 0 »,
    // comme le Locomotive (vrai = tout sauf 0).
    static Condition? ParseCondition(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? expression = ParseExpression(tokens, ref pos, diagnostics);
        return expression is null ? null : ToCondition(expression);
    }

    // Vrai si l'expression vaut toujours -1 ou 0 (une comparaison, ou des comparaisons reliées par AND, OR, NOT) :
    // alors AND et OR bit à bit donnent le même résultat que « et » et « ou ».
    static bool IsTruthValue(Expression expression) => expression switch
    {
        CompareExpression => true,
        BinaryExpression { Operator: BinaryOperator.And or BinaryOperator.Or } both => IsTruthValue(both.Left) && IsTruthValue(both.Right),
        NotExpression negation => IsTruthValue(negation.Operand),
        _ => false
    };

    static Condition ToCondition(Expression expression) => expression switch
    {
        CompareExpression compare => new ComparisonCondition(compare.Operator, compare.Left, compare.Right),
        BinaryExpression { Operator: BinaryOperator.And } both when IsTruthValue(both) =>
            new AndCondition(ToCondition(both.Left), ToCondition(both.Right)),
        BinaryExpression { Operator: BinaryOperator.Or } either when IsTruthValue(either) =>
            new OrCondition(ToCondition(either.Left), ToCondition(either.Right)),
        NotExpression negation when IsTruthValue(negation.Operand) => Negate(ToCondition(negation.Operand)),
        _ => new ComparisonCondition(CompareOperator.NotEqual, expression, new NumberExpression(0))
    };

    static Condition Negate(Condition condition) => condition switch
    {
        ComparisonCondition c => c with
        {
            Operator = c.Operator switch
            {
                CompareOperator.Equal => CompareOperator.NotEqual,
                CompareOperator.NotEqual => CompareOperator.Equal,
                CompareOperator.Less => CompareOperator.GreaterOrEqual,
                CompareOperator.GreaterOrEqual => CompareOperator.Less,
                CompareOperator.Greater => CompareOperator.LessOrEqual,
                _ => CompareOperator.Greater
            }
        },
        AndCondition a => new OrCondition(Negate(a.Left), Negate(a.Right)),
        OrCondition o => new AndCondition(Negate(o.Left), Negate(o.Right)),
        _ => throw new NotSupportedException()
    };

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
                case ArrayElementExpression element:
                    foreach (Expression index in element.Indexes)
                        Uses(index);
                    break;
                case CompareExpression compare:
                    Uses(compare.Left);
                    Uses(compare.Right);
                    break;
                case NotExpression negation:
                    Uses(negation.Operand);
                    break;
                case AddressOfExpression address:
                    Uses(address.Target);
                    break;
                case ConcatExpression concat:
                    Uses(concat.Left);
                    Uses(concat.Right);
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

                case DimStatement or ArrayAssignStatement:
                    foreach (Expression expression in AstWalker.TopLevel(statement))
                        Uses(expression);
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

                case OnStatement on:
                    Uses(on.Selector);
                    break;

                case InputStatement or ReadStatement:
                    foreach (Expression target in AstWalker.TopLevel(statement))
                    {
                        if (target is VariableExpression variable)
                            used.Add(variable.Name);
                        else
                            Uses(target);
                    }
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

    // expression := ou { XOR ou } ; ou := et { OR et } ; et := non { AND non } ; non := NOT non | comparaison ;
    // comparaison := somme { (= <> < > <= >=) somme } : l'ordre des opérations du Locomotive BASIC (lot D, D060).
    static bool IsWord(Token t, string word) => t.Kind == TokenKind.Word && t.Text == word;

    // Une valeur qui doit être un nombre (presque partout). Un texte donne « un texte et un nombre ne se mélangent pas ».
    static Expression? ParseExpression(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token at = tokens[pos];
        Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
        if (value is not null && Types.IsString(value))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, at.Line, "un nombre est attendu"));
            return null;
        }
        return value;
    }

    // Les deux côtés d'une opération sur les nombres (AND, *, ^...) doivent être des nombres (lot E, D062)
    static bool BothNumbers(Expression left, Expression right, Token op, List<Diagnostic> diagnostics)
    {
        if (!Types.IsString(left) && !Types.IsString(right))
            return true;
        diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, op.Line, op.Text));
        return false;
    }

    // Une valeur texte ou nombre (PRINT, affectation, comparaison, LEN...).
    static Expression? ParseAnyExpression(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseOrLevel(tokens, ref pos, diagnostics);
        while (left is not null && IsWord(tokens[pos], "XOR"))
        {
            Token op = tokens[pos];
            pos++;
            Expression? right = ParseOrLevel(tokens, ref pos, diagnostics);
            left = right is null || !BothNumbers(left, right, op, diagnostics) ? null : new BinaryExpression(BinaryOperator.Xor, left, right);
        }
        return left;
    }

    static Expression? ParseOrLevel(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseAndLevel(tokens, ref pos, diagnostics);
        while (left is not null && IsWord(tokens[pos], "OR"))
        {
            Token op = tokens[pos];
            pos++;
            Expression? right = ParseAndLevel(tokens, ref pos, diagnostics);
            left = right is null || !BothNumbers(left, right, op, diagnostics) ? null : new BinaryExpression(BinaryOperator.Or, left, right);
        }
        return left;
    }

    static Expression? ParseAndLevel(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseNotLevel(tokens, ref pos, diagnostics);
        while (left is not null && IsWord(tokens[pos], "AND"))
        {
            Token op = tokens[pos];
            pos++;
            Expression? right = ParseNotLevel(tokens, ref pos, diagnostics);
            left = right is null || !BothNumbers(left, right, op, diagnostics) ? null : new BinaryExpression(BinaryOperator.And, left, right);
        }
        return left;
    }

    static Expression? ParseNotLevel(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        if (!IsWord(tokens[pos], "NOT"))
            return ParseRelational(tokens, ref pos, diagnostics);

        Token op = tokens[pos];
        pos++;
        Expression? operand = ParseNotLevel(tokens, ref pos, diagnostics);
        return operand is null || !BothNumbers(operand, new NumberExpression(0), op, diagnostics) ? null : new NotExpression(operand);
    }

    static CompareOperator? CompareOf(Token t) => t.Kind switch
    {
        TokenKind.Equals => CompareOperator.Equal,
        TokenKind.NotEqual => CompareOperator.NotEqual,
        TokenKind.Less => CompareOperator.Less,
        TokenKind.LessEqual => CompareOperator.LessOrEqual,
        TokenKind.Greater => CompareOperator.Greater,
        TokenKind.GreaterEqual => CompareOperator.GreaterOrEqual,
        _ => null
    };

    static Expression? ParseRelational(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseSum(tokens, ref pos, diagnostics);
        while (left is not null && CompareOf(tokens[pos]) is CompareOperator op)
        {
            Token opToken = tokens[pos];
            pos++;
            Expression? right = ParseSum(tokens, ref pos, diagnostics);
            if (right is not null && Types.IsString(left) != Types.IsString(right))
            {
                // on compare deux nombres, ou deux textes (dans l'ordre des codes des caractères, D062)
                diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, opToken.Line, opToken.Text));
                return null;
            }
            left = right is null ? null : new CompareExpression(op, left, right);
        }
        return left;
    }

    // somme := terme { ("+" | "-") terme }
    static Expression? ParseSum(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParseTerm(tokens, ref pos, diagnostics);

        while (left is not null && (tokens[pos].Kind == TokenKind.Plus || tokens[pos].Kind == TokenKind.Minus))
        {
            Token opToken = tokens[pos];
            BinaryOperator op = opToken.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            pos++;

            Expression? right = ParseTerm(tokens, ref pos, diagnostics);
            if (right is null)
                return null;

            // A$ + B$ : les deux textes bout à bout (D062)
            if (op == BinaryOperator.Add && Types.IsString(left) && Types.IsString(right))
            {
                left = new ConcatExpression(left, right);
                continue;
            }
            if (!BothNumbers(left, right, opToken, diagnostics))
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
            if (right is null || !BothNumbers(left, right, opToken, diagnostics))
                return null;

            if (op != BinaryOperator.Multiply && right is NumberExpression { Value: 0 })
                diagnostics.Add(new Diagnostic(DiagnosticCode.DivisionByZero, opToken.Line, opToken.Text));

            left = new BinaryExpression(op, left, right);
        }

        return left;
    }

    // opérande := ["+" | "-"] puissance ; puissance := primaire { "^" ["+" | "-"] primaire }.
    // Le ^ passe avant le signe moins, comme le Locomotive : -2^2 = -4 (D060).
    static Expression? ParseOperand(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        if (tokens[pos].Kind == TokenKind.Plus)
        {
            pos++;
            return ParsePower(tokens, ref pos, diagnostics);
        }

        if (tokens[pos].Kind == TokenKind.Minus)
        {
            pos++;

            // -32768 est valide : le nombre est lu avec son signe (sauf s'il est suivi de ^)
            if (tokens[pos].Kind == TokenKind.Number && tokens[pos + 1].Kind != TokenKind.Caret)
                return ParseNumber(tokens, ref pos, diagnostics, negative: true);

            Token minus = tokens[pos - 1];
            Expression? operand = ParsePower(tokens, ref pos, diagnostics);
            return operand is null || !BothNumbers(operand, new NumberExpression(0), minus, diagnostics)
                ? null : new BinaryExpression(BinaryOperator.Subtract, new NumberExpression(0), operand);
        }

        return ParsePower(tokens, ref pos, diagnostics);
    }

    static Expression? ParsePower(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Expression? left = ParsePrimary(tokens, ref pos, diagnostics);
        while (left is not null && tokens[pos].Kind == TokenKind.Caret)
        {
            Token caret = tokens[pos];
            pos++;
            Expression? right;
            if (tokens[pos].Kind == TokenKind.Minus)
            {
                pos++;
                right = tokens[pos].Kind == TokenKind.Number
                    ? ParseNumber(tokens, ref pos, diagnostics, negative: true)
                    : ParsePrimary(tokens, ref pos, diagnostics) is Expression e ? new BinaryExpression(BinaryOperator.Subtract, new NumberExpression(0), e) : null;
            }
            else
            {
                if (tokens[pos].Kind == TokenKind.Plus)
                    pos++;
                right = ParsePrimary(tokens, ref pos, diagnostics);
            }
            left = right is null || !BothNumbers(left, right, caret, diagnostics) ? null : new BinaryExpression(BinaryOperator.Power, left, right);
        }
        return left;
    }

    static Expression? ParsePrimary(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token t = tokens[pos];

        switch (t.Kind)
        {
            case TokenKind.Number:
                return ParseNumber(tokens, ref pos, diagnostics, negative: false);

            case TokenKind.String:
            {
                // "BONJOUR" dans un calcul (D062) ; 255 caractères au plus, comme le Locomotive
                pos++;
                if (t.Text.Length > 255)
                    diagnostics.Add(new Diagnostic(DiagnosticCode.StringTooLong, t.Line, t.Text.Length.ToString()));
                return new StringLiteralExpression(t.Text);
            }

            case TokenKind.At:
            {
                // @A ou @T(3) : adresse de la variable ou de la case (D061)
                pos++;
                int before = diagnostics.Count;
                Expression? target = ParseTarget(tokens, ref pos, diagnostics);
                for (int i = before; i < diagnostics.Count; i++)
                    if (diagnostics[i].Code == DiagnosticCode.ExpectedTarget)
                        diagnostics[i] = diagnostics[i] with { Context = "@" };
                return target is null ? null : new AddressOfExpression(target);
            }

            case TokenKind.HexNumber:
                pos++;
                return new NumberExpression(Signed16(Lexer.HexValue(t.Text) ?? 0));

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
                if (tokens[pos + 1].Kind == TokenKind.LeftParen)
                {
                    // A(3) : case de tableau (D058)
                    if (!CheckArrayName(t, diagnostics))
                        return null;
                    pos++;
                    return TryParseIndexes(tokens, ref pos, diagnostics, out List<Expression> indexes)
                        ? new ArrayElementExpression(t.Text, indexes)
                        : null;
                }
                pos++;
                return new VariableExpression(t.Text);

            case TokenKind.LeftParen:
                pos++;
                Expression? inner = ParseAnyExpression(tokens, ref pos, diagnostics);
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
                Expression? argument = function.FirstIsAddress && arguments.Count == 0
                    ? ParseAddressArgument(tokens, ref pos, diagnostics)
                    : ParseAnyExpression(tokens, ref pos, diagnostics);   // le genre est vérifié plus bas
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

        // Chaque valeur doit être du bon genre : texte (S), nombre (N) ou l'un ou l'autre (A)
        string pattern = function.Patterns?.FirstOrDefault(p => p.Length == arguments.Count) ?? new string('N', arguments.Count);
        for (int i = 0; i < arguments.Count; i++)
        {
            bool isText = Types.IsString(arguments[i]);
            if ((pattern[i] == 'S' && !isText) || (pattern[i] == 'N' && isText))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, nameToken.Line,
                                               pattern[i] == 'S' ? "un texte est attendu" : "un nombre est attendu", nameToken.Text));
                return null;
            }
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

    // Adresse ou port : un nombre seul de 32768 à 65535 est accepté et vaut le même mot de 16 bits que sur le CPC
    // (49152 = -16384). Dans un calcul (49152 + 1), un nombre au-dessus de 32767 reste refusé.
    static Expression? ParseAddressArgument(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token t = tokens[pos];
        TokenKind next = tokens[pos + 1].Kind;
        bool alone = next is TokenKind.Comma or TokenKind.RightParen || IsStatementEnd(tokens[pos + 1]) || IsElse(tokens[pos + 1]);

        if (t.Kind == TokenKind.Number && alone && long.TryParse(t.Text, out long value) && value is >= 32768 and <= 65535)
        {
            pos++;
            return new NumberExpression((int)(value - 65536));
        }
        return ParseExpression(tokens, ref pos, diagnostics);
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
