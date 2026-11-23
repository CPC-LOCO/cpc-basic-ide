// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Libraries;

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
        "MOVE", "MOVER", "PLOT", "PLOTR", "DRAW", "DRAWR", "CLG", "ORIGIN", "BOX", "CIRCLE", "ELLIPSE", "DEBUG",
        "GOTO", "GOSUB", "RETURN", "STOP", "ON", "INKEY", "JOY", "CLEAR", "INPUT", "POKE", "PEEK", "OUT", "INP", "CALL", "MEMORY", "DIM",
        "DATA", "READ", "RESTORE", "XOR", "NOT", "LEN",
        "LEFT$", "RIGHT$", "MID$", "CHR$", "ASC", "STR$", "VAL", "STRING$", "SPACE$", "UPPER$", "LOWER$", "INSTR", "HEX$", "BIN$",
        "INKEY$", "LINE", "DEFINT", "DEFREAL", "DEFSTR",
        "SIN", "COS", "TAN", "ATN", "SQR", "EXP", "LOG", "LOG10", "PI", "INT", "FIX", "CINT", "CREAL", "ROUND", "RND",
        "DEG", "RAD", "SOUND", "ENV", "ENT", "RELEASE", "SQ", "WINDOW", "SWAP", "POS", "VPOS", "ZONE", "TAB", "SPC", "WRITE", "USING", "SYMBOL", "TAG", "TAGOFF", "AFTER", "EVERY", "REMAIN", "DI", "EI", "ERROR", "RESUME", "ERR", "ERL",
        "OPENOUT", "OPENIN", "CLOSEOUT", "CLOSEIN", "EOF", "CAT", "LOAD", "SAVE", "SPEED",
        "RUN", "CHAIN", "MERGE", "TRON", "TROFF",
        "XPOS", "YPOS", "TEST", "TESTR", "KEY", "HIMEM", "FRE", "UNT", "WAIT", "DEF", "FN", "ERASE", "DERR", "WIDTH", "FRAME", "CURSOR", "COPYCHR$", "DEC$", "GRAPHICS", "FILL", "PAINT", "MASK",
        "DEFSPR", "SPRITE", "SELECT", "CASE", "VROOM", "BOOM", "SHOT"];

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
    // StreamFirst (lot H1, D072) : la première valeur est un numéro de fenêtre écrit avec « # », comme POS(#0).
    sealed record FunctionInfo(string Signature, int MinArguments, int MaxArguments, bool FirstIsAddress = false,
                               string[]? Patterns = null, bool StreamFirst = false);

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
        // D142 : sinus et cosinus rapides en entiers, angle en degrés (table au degré près)
        ["ISIN"] = new("ISIN(angle en degrés, échelle)", 2, 2),
        ["ICOS"] = new("ICOS(angle en degrés, échelle)", 2, 2),
        // D200 : -1 si les deux sprites sont montrés et se touchent (rectangles de leurs dessins), sinon 0
        ["COLLIDE"] = new("COLLIDE(sprite, sprite)", 2, 2),
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
        ["INP"] = new("INP(port)", 1, 1, true),
        // Lot F2 (D069) : fonctions mathématiques, calculées par la ROM
        ["SIN"] = new("SIN(angle)", 1, 1),
        ["COS"] = new("COS(angle)", 1, 1),
        ["TAN"] = new("TAN(angle)", 1, 1),
        ["ATN"] = new("ATN(valeur)", 1, 1),
        ["SQR"] = new("SQR(valeur)", 1, 1),
        ["EXP"] = new("EXP(valeur)", 1, 1),
        ["LOG"] = new("LOG(valeur)", 1, 1),
        ["LOG10"] = new("LOG10(valeur)", 1, 1),
        ["INT"] = new("INT(valeur)", 1, 1),
        ["FIX"] = new("FIX(valeur)", 1, 1),
        ["CINT"] = new("CINT(valeur)", 1, 1),
        ["CREAL"] = new("CREAL(valeur)", 1, 1),
        ["ROUND"] = new("ROUND(valeur [, décimales])", 1, 2),
        ["RND"] = new("RND[(valeur)]", 0, 1),
        // Lot G1 (D071) : état d'un canal sonore
        ["SQ"] = new("SQ(canal : 1, 2 ou 4)", 1, 1),
        // Lot K4 (D091) : couleur d'un point (le curseur graphique y va, comme le vrai BASIC)
        ["TEST"] = new("TEST(x, y)", 2, 2),
        ["TESTR"] = new("TESTR(dx, dy)", 2, 2),
        // Lot H1 (D072) : position du curseur de texte dans une fenêtre
        ["POS"] = new("POS(#fenêtre)", 1, 1, StreamFirst: true),
        ["VPOS"] = new("VPOS(#fenêtre)", 1, 1, StreamFirst: true),
        // Lot I1 (D079) : temps restant d'une minuterie, qui est arrêtée
        ["REMAIN"] = new("REMAIN(minuterie : 0 à 3)", 1, 1),
        // Lot L1 (D102) : caractère lu à l'écran, à la place du curseur de la fenêtre
        ["COPYCHR$"] = new("COPYCHR$(#fenêtre)", 1, 1, StreamFirst: true),
        // Lot L2 (D103) : nombre mis en forme comme PRINT USING, rendu comme un texte
        ["DEC$"] = new("DEC$(nombre, format)", 2, 2, Patterns: ["NS"]),
        // Lot K6 (D094) : mémoire libre (FRE("") fait d'abord le ménage des textes) ; adresse en entier signé
        ["FRE"] = new("FRE(0) ou FRE(\"\")", 1, 1, Patterns: ["A"]),
        ["UNT"] = new("UNT(adresse)", 1, 1, true)
    };

    /// <summary>Commande d'écran : écriture à montrer dans les messages, nombre de valeurs, valeurs permises pour chacune.</summary>
    /// <remarks>Command : nom rangé dans l'instruction quand il diffère du mot lu (« WINDOW SWAP », D072).</remarks>
    sealed record ScreenCommandInfo(string Signature, int MinArguments, int[] Min, int[] Max, bool FirstIsAddress = false,
                                    string? Command = null);

    // Commandes d'écran (D046) : les valeurs écrites en nombres sont vérifiées à la compilation
    /// <summary>Plus haute adresse permise pour MEMORY : le HIMEM de départ (vrai BASIC 1.1, SugarBox, 18 nov. 2026, D125).</summary>
    const int MemoryHighest = 0xA67B;

    static readonly Dictionary<string, ScreenCommandInfo> ScreenCommands = new()
    {
        ["MODE"] = new("MODE mode (0, 1 ou 2)", 1, [0], [2]),
        ["CLS"] = new("CLS", 0, [], []),
        ["DEG"] = new("DEG", 0, [], []),                // angles en degrés (D069)
        ["RELEASE"] = new("RELEASE canaux (0 à 7)", 1, [0], [7]),   // libère les canaux sonores en attente (D071)
        // D213 : bruit de moteur, sur le canal A (enveloppes ENV 12 à 15, ENT 11 à 15)
        ["VROOM"] = new("VROOM son (1 moteur qui monte, 2 ralenti, 3 moteur qui descend)", 1, [1], [3]),
        // D214 : explosions et chocs, durée en centièmes de seconde (enveloppes ENV 9 et 10, ENT 8 à 10)
        // D215 : tirs et bris, sans durée (enveloppes ENV 4 à 8, ENT 6 et 7)
        ["SHOT"] = new("SHOT son (1 laser, 2 missile, 3 rafale, 4 vitre qui casse)", 1, [1], [4]),
        ["BOOM"] = new("BOOM son (1 voiture, 2 feu d'artifice, 3 pétard, 4 marteau, 5 malus) [, durée en centièmes de seconde]", 1, [1, 1], [5, 32767]),
        ["RAD"] = new("RAD", 0, [], []),                // angles en radians (D069)
        ["LOCATE"] = new("LOCATE colonne, ligne", 2, [1, 1], [80, 25]),
        ["PEN"] = new("PEN stylo (0 à 15) [, mode : 0 opaque, 1 transparent]", 1, [0, 0], [15, 1]),   // mode : D104
        ["PAPER"] = new("PAPER fond (0 à 15)", 1, [0], [15]),
        ["INK"] = new("INK stylo, couleur [, couleur2]", 2, [0, 0, 0], [15, 26, 26]),
        ["BORDER"] = new("BORDER couleur [, couleur2]", 1, [0, 0], [26, 26]),
        ["CLG"] = new("CLG [stylo]", 0, [0], [15]),
        // Lot L3 (D104) : fenêtre graphique facultative (gauche, droite, haut, bas)
        ["ORIGIN"] = new("ORIGIN x, y [, gauche, droite, haut, bas]", 2,
                         [-32768, -32768, -32768, -32768, -32768, -32768], [32767, 32767, 32767, 32767, 32767, 32767]),
        // Lot H1 (D072) : fenêtres de texte, colonnes et lignes à partir de 1 (le firmware ramène ce qui dépasse dans l'écran)
        ["WINDOW"] = new("WINDOW [#fenêtre,] gauche, droite, haut, bas", 4, [1, 1, 1, 1], [255, 255, 255, 255]),
        ["WINDOW SWAP"] = new("WINDOW SWAP fenêtre [, fenêtre]", 1, [0, 0], [7, 7], Command: "WINDOW SWAP"),
        // Lot H4 (D075) : caractères redessinés et texte à la position graphique
        ["SYMBOL"] = new("SYMBOL caractère, ligne1 [, ligne2, ... ligne8] (de 0 à 255)", 2,
                         [0, 0, 0, 0, 0, 0, 0, 0, 0], [255, 255, 255, 255, 255, 255, 255, 255, 255]),
        ["SYMBOL AFTER"] = new("SYMBOL AFTER premier caractère (0 à 256)", 1, [0], [256], Command: "SYMBOL AFTER"),
        // Lot L4a (D106) : stylo et fond des tracés ; le mode transparent de GRAPHICS PEN viendra avec MASK (L4c)
        ["GRAPHICS PEN"] = new("GRAPHICS PEN stylo (0 à 15) [, mode : 0 opaque, 1 transparent]", 1, [0, 0], [15, 1],
                               Command: "GRAPHICS PEN"),
        // Lot L4c (D110) : motif des lignes
        ["MASK"] = new("MASK motif (0 à 255) [, premier point (0 ou 1)]", 1, [0, 0], [255, 1]),
        // D123 : « MASK ,1 » sans motif (accepté par le vrai BASIC 1.1 ; « MASK » seul : Operand missing)
        ["MASK FIRST"] = new("MASK [motif (0 à 255)], premier point (0 ou 1)", 1, [0], [1], Command: "MASK FIRST"),
        // Lot L4b (D107) : remplissage ; PAINT est une commande nouvelle (seulement l'encre trouvée sous le curseur)
        ["FILL"] = new("FILL encre (0 à 15)", 1, [0], [15]),
        ["PAINT"] = new("PAINT encre (0 à 15)", 1, [0], [15]),
        ["GRAPHICS PAPER"] = new("GRAPHICS PAPER fond (0 à 15)", 1, [0], [15], Command: "GRAPHICS PAPER"),
        ["TAG"] = new("TAG [#fenêtre]", 0, [], []),
        // Lot I1 (D079) : coupe / remet les appels des minuteries
        ["DI"] = new("DI", 0, [], []),
        ["EI"] = new("EI", 0, [], []),
        ["TAGOFF"] = new("TAGOFF [#fenêtre]", 0, [], []),
        // Lot H2 (D073) : largeur des zones de PRINT (13 au départ)
        ["ZONE"] = new("ZONE largeur (1 à 255)", 1, [1], [255]),
        // D056 : accès à la machine
        ["POKE"] = new("POKE adresse, valeur (0 à 255)", 2, [-32768, 0], [32767, 255], true),
        ["OUT"] = new("OUT port, valeur (0 à 255)", 2, [-32768, 0], [32767, 255], true),
        ["CALL"] = new("CALL adresse", 1, [-32768], [32767], true),
        ["MEMORY"] = new("MEMORY adresse", 1, [-32768], [32767], true),
        // Lot K12 (D100) : largeur de ligne de l'imprimante (255 : pas de retour à la ligne automatique)
        ["WIDTH"] = new("WIDTH largeur (1 à 255)", 1, [1], [255]),
        // Lot L1 (D102) : BASIC 1.1, mais les routines du firmware existent aussi sur le 464
        ["FRAME"] = new("FRAME", 0, [], []),
        ["CURSOR"] = new("CURSOR système (0 ou 1) [, utilisateur (0 ou 1)]", 1, [0, 0], [1, 1]),
        // D123 : « CURSOR ,1 » sans interrupteur du système (accepté par le vrai BASIC 1.1 ; « CURSOR » seul : Operand missing)
        ["CURSOR USER"] = new("CURSOR [système (0 ou 1)], utilisateur (0 ou 1)", 1, [0], [1], Command: "CURSOR USER"),
        // Lot K6 (D094) : attend que (INP(port) XOR inversion) AND masque ne vaille plus 0
        ["WAIT"] = new("WAIT port, masque [, inversion]", 2, [-32768, 0, 0], [32767, 255, 255], true),
        // D048 : coordonnées natives du MODE (origine en haut à gauche) ; ce qui sort de l'écran est ignoré
        ["BOX"] = new("BOX x1, y1, x2, y2 [, stylo [, remplissage]]", 4, [-32768, -32768, -32768, -32768, 0, 0], [32767, 32767, 32767, 32767, 15, 15]),
        // D130 : rond, rayon compté en lignes (0 à 1000), largeur corrigée selon le MODE
        ["CIRCLE"] = new("CIRCLE x, y, rayon (0 à 1000) [, stylo [, remplissage]]", 3, [-32768, -32768, 0, 0, 0], [32767, 32767, 1000, 15, 15]),
        // D198 : sprite montré (ou déplacé) en x, y (coordonnées du MODE, coin en haut à gauche, comme BOX) ; seul : caché
        ["SPRITE"] = new("SPRITE numéro (1 à 16) [, x, y]", 1, [1, -32768, -32768], [16, 32767, 32767])
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

    /// <param name="realByDefault">Réglage « Sans signe : à virgule » (F8, D066) : A est alors A!.</param>
    /// <param name="catalog">Catalogue des bibliothèques (D148) : un mot inconnu y est cherché ; null : aucune bibliothèque.</param>
    public static BasicProgram Parse(IReadOnlyList<Token> tokens, List<Diagnostic> diagnostics, bool realByDefault = false,
                                     LibraryCatalog? catalog = null)
    {
        realDivision = realByDefault;
        libraries = catalog;
        libraryUses = new Dictionary<string, int>(StringComparer.Ordinal);
        tokens = ApplyDefaultTypes(tokens, realByDefault);
        var statements = new List<Statement>();
        statementStarts = new HashSet<Statement>(ReferenceEqualityComparer.Instance);
        userFunctions = new Dictionary<string, UserFunction>();
        var lines = new Dictionary<int, int>();   // numéro de ligne -> rang de l'instruction qui la suit
        int pos = 0;

        while (tokens[pos].Kind != TokenKind.EndOfFile)
            pos = ParseLine(tokens, pos, statements, diagnostics, lines);

        CheckUserFunctions(statements, diagnostics);    // d'abord : le calcul des FN est ensuite vérifié à chaque appel (D095)
        userFunctions = null;
        CheckDeclarations(statements, diagnostics);
        CheckArrays(statements, diagnostics);
        CheckDebugNames(statements, diagnostics);
        CheckBlocks(statements, diagnostics);
        CheckLineTargets(statements, lines, diagnostics);
        CheckLibraries(diagnostics);
        CheckSprites(statements, diagnostics);
        libraries = null;
        libraryUses = null;
        HashSet<Statement> starts = statementStarts!;
        statementStarts = null;
        return new BasicProgram(statements, lines, starts);
    }

    // Fonctions du programmeur (DEF FN, lot K7, D095) : une fiche par nom, créée au premier appel ou à DEF FN.
    // fnRename : pendant la lecture du calcul de DEF FN, les valeurs de la fonction prennent leur nom caché.
    [ThreadStatic] static Dictionary<string, UserFunction>? userFunctions;

    // Bibliothèques (D148) : le catalogue, et chaque commande utilisée avec la ligne de sa première utilisation
    [ThreadStatic] static LibraryCatalog? libraries;
    [ThreadStatic] static Dictionary<string, int>? libraryUses;
    [ThreadStatic] static Dictionary<string, string>? fnRename;

    static UserFunction GetUserFunction(string name)
    {
        userFunctions ??= new Dictionary<string, UserFunction>();
        if (!userFunctions.TryGetValue(name, out UserFunction? function))
        {
            function = new UserFunction(name);
            userFunctions[name] = function;
        }
        return function;
    }

    /// <summary>Vrai pour un mot du langage (commande, fonction, mot réservé), écrit en majuscules (LIST, D122).</summary>
    public static bool IsKeyword(string word) =>
        word == "REM" || Commands.Contains(word) || LocomotiveWords.Contains(word) || Functions.ContainsKey(word)
        || OtherKeywords.Contains(word) || DefWords.Contains(word);

    static bool IsUserName(Token t) =>
        t.Kind == TokenKind.Word && t.Text[0] is >= 'A' and <= 'Z' && !Commands.Contains(t.Text)
        && !LocomotiveWords.Contains(t.Text) && !Functions.ContainsKey(t.Text) && !OtherKeywords.Contains(t.Text);

    // DEF FNNOM[(valeur, ...)] = calcul (D095)
    static int ParseDefFn(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token def = tokens[pos];
        pos++;
        if (!IsWord(tokens[pos], "FN") || !IsUserName(tokens[pos + 1]))
        {
            Token wrong = IsWord(tokens[pos], "FN") ? tokens[pos + 1] : tokens[pos];
            diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionName, def.Line, IsLineEnd(wrong) ? "" : wrong.Text, "DEF FN"));
            return SkipToStatementEnd(tokens, pos);
        }
        string name = tokens[pos + 1].Text;
        pos += 2;

        var written = new List<string>();
        if (tokens[pos].Kind == TokenKind.LeftParen)
        {
            pos++;
            while (true)
            {
                if (!IsUserName(tokens[pos]) || written.Contains(tokens[pos].Text))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionParameter, def.Line,
                                                   IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
                    return SkipToStatementEnd(tokens, pos);
                }
                written.Add(tokens[pos].Text);
                pos++;
                if (tokens[pos].Kind == TokenKind.Comma)
                {
                    pos++;
                    continue;
                }
                if (tokens[pos].Kind != TokenKind.RightParen)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, def.Line, "", "DEF FN"));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos++;
                break;
            }
        }
        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, def.Line, IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        UserFunction function = GetUserFunction(name);
        if (function.Body is not null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionTwice, def.Line, name));
            return SkipToStatementEnd(tokens, pos);
        }

        // Les valeurs de la fonction ont un nom caché : le X de FNDOUBLE(X) n'est pas le X du programme
        List<string> hidden = written.Select(p => $"FN{name}.{p}").ToList();
        fnRename = written.Zip(hidden).ToDictionary(x => x.First, x => x.Second);
        Expression? body;
        try
        {
            body = ParseAnyExpression(tokens, ref pos, diagnostics);
        }
        finally
        {
            fnRename = null;
        }
        if (body is null)
            return SkipToStatementEnd(tokens, pos);

        if (VariableNames.IsString(name) != Types.IsString(body))
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, def.Line, $"FN{name}", "DEF FN"));
        else if (!VariableNames.IsReal(name) && !VariableNames.IsString(name) && Types.IsReal(body))
            diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionNeedsReal, def.Line, $"{name}|"));

        function.Body = body;
        function.Parameters = hidden;
        function.WrittenParameters = written;
        function.Line = def.Line;
        statements.Add(new DefFnStatement(def.Line, function));
        return pos;
    }

    // FNNOM[(valeur, ...)] dans un calcul (D095) ; « FN NOM » s'écrit aussi avec un espace
    static Expression? ParseUserFunctionCall(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
    {
        Token fn = tokens[pos];
        pos++;
        if (!IsUserName(tokens[pos]))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionName, fn.Line, IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return null;
        }
        string name = tokens[pos].Text;
        pos++;
        var arguments = new List<Expression>();
        if (tokens[pos].Kind == TokenKind.LeftParen)
        {
            pos++;
            while (true)
            {
                Expression? argument = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (argument is null)
                    return null;
                arguments.Add(argument);
                if (tokens[pos].Kind == TokenKind.Comma)
                {
                    pos++;
                    continue;
                }
                if (tokens[pos].Kind != TokenKind.RightParen)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, fn.Line, "", $"FN{name}"));
                    return null;
                }
                pos++;
                break;
            }
        }
        return new UserFunctionExpression(GetUserFunction(name), arguments);
    }

    // Après la lecture (D095) : fonctions définies, nombre et genre des valeurs, pas d'appel à soi-même
    static void CheckUserFunctions(List<Statement> statements, List<Diagnostic> diagnostics)
    {
        foreach (Statement statement in statements)
        {
            IEnumerable<Expression> all = AstWalker.All(statement);
            if (statement is DefFnStatement { Function.Body: not null } def)
                all = all.Concat(AstWalker.Flatten(def.Function.Body));
            foreach (UserFunctionExpression call in all.OfType<UserFunctionExpression>().ToList())
            {
                UserFunction f = call.Function;
                if (f.Body is null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionUnknown, statement.SourceLine, f.Name));
                    continue;
                }
                if (call.Arguments.Count != f.Parameters.Count)
                {
                    string signature = f.WrittenParameters.Count == 0 ? $"FN{f.Name}" : $"FN{f.Name}({string.Join(", ", f.WrittenParameters)})";
                    diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, statement.SourceLine, signature));
                    continue;
                }
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    string p = f.WrittenParameters[i];
                    Expression argument = call.Arguments[i];
                    if (VariableNames.IsString(p) != Types.IsString(argument))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, statement.SourceLine, $"{p} de FN{f.Name}"));
                    else if (!VariableNames.IsReal(p) && !VariableNames.IsString(p) && Types.IsReal(argument))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionNeedsReal, statement.SourceLine, $"{f.Name}|{p}"));
                }
            }
        }

        // Une fonction qui s'appelle elle-même, même par un détour, est refusée
        IEnumerable<UserFunction> Callees(UserFunction f) =>
            AstWalker.Flatten(f.Body!).OfType<UserFunctionExpression>().Select(c => c.Function).Where(c => c.Body is not null);

        bool Reaches(UserFunction from, UserFunction target, HashSet<UserFunction> seen)
        {
            foreach (UserFunction callee in Callees(from))
            {
                if (callee == target)
                    return true;
                if (seen.Add(callee) && Reaches(callee, target, seen))
                    return true;
            }
            return false;
        }

        List<UserFunction> defined = statements.OfType<DefFnStatement>().Select(d => d.Function).Distinct().ToList();
        var recursive = defined.Where(f => Reaches(f, f, [])).ToList();
        foreach (UserFunction f in recursive)
            diagnostics.Add(new Diagnostic(DiagnosticCode.UserFunctionRecursive, f.Line, f.Name));
        foreach (UserFunction f in defined.Except(recursive))
            f.Ready = true;
    }

    // Début de chaque instruction BASIC, noté pendant la lecture (D080). Une lecture par fil d'exécution.
    [ThreadStatic] static HashSet<Statement>? statementStarts;

    // Réglage « Sans signe : à virgule » (F8) : 7/2 donne 3.5 comme dans le Locomotive, même entre deux entiers (D066)
    [ThreadStatic] static bool realDivision;

    static readonly HashSet<string> DefWords = ["DEFINT", "DEFREAL", "DEFSTR"];

    // Mots du langage lus par le parser sans être des commandes (GO SUB, BREAK...) : jamais des noms de variables
    static readonly HashSet<string> OtherKeywords = ["BREAK", "GO", "SUB"];

    static int? Letter(Token t) =>
        t.Kind == TokenKind.Word && t.Text.Length == 1 && t.Text[0] is >= 'A' and <= 'Z' ? t.Text[0] - 'A' : null;

    // DEFINT A-Z, I : les lettres concernées (0 = A). null si l'écriture est fausse ; pos avance après la liste.
    static List<int>? ReadLetterRanges(IReadOnlyList<Token> tokens, ref int pos)
    {
        var letters = new List<int>();
        while (true)
        {
            int? first = Letter(tokens[pos]);
            if (first is null)
                return null;
            pos++;
            int last = first.Value;
            if (tokens[pos].Kind == TokenKind.Minus)
            {
                pos++;
                int? second = Letter(tokens[pos]);
                if (second is null || second < first)
                    return null;
                last = second.Value;
                pos++;
            }
            for (int l = first.Value; l <= last; l++)
                letters.Add(l);
            if (tokens[pos].Kind != TokenKind.Comma)
                return letters;
            pos++;
        }
    }

    static bool IsPlainVariableName(string word) =>
        word[0] is >= 'A' and <= 'Z' && !VariableNames.HasSuffix(word)
        && !Commands.Contains(word) && !LocomotiveWords.Contains(word) && !Functions.ContainsKey(word)
        && !OtherKeywords.Contains(word);

    /// <summary>
    /// Type des variables sans signe (D068) : DEFINT (%), DEFREAL (!) et DEFSTR ($) donnent le type selon la première
    /// lettre ; le réglage « Sans signe : à virgule » met toutes les lettres à « ! ». Ces réglages valent pour tout
    /// le programme (le dernier l'emporte pour une même lettre). Les noms sont alors écrits avec leur signe :
    /// après DEFINT A-Z, A et A% sont la même variable, comme dans le Locomotive. Sans réglage, rien ne change :
    /// A reste un entier à part (D026). Les variables BYTE gardent leur nom.
    /// </summary>
    static IReadOnlyList<Token> ApplyDefaultTypes(IReadOnlyList<Token> tokens, bool realByDefault)
    {
        var suffix = new char?[26];
        if (realByDefault)
            Array.Fill(suffix, '!');
        bool any = realByDefault;
        var letterPlaces = new HashSet<int>();
        var bytes = new HashSet<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            Token t = tokens[i];
            if (t.Kind != TokenKind.Word)
                continue;
            if (DefWords.Contains(t.Text))
            {
                int pos = i + 1;
                List<int>? letters = ReadLetterRanges(tokens, ref pos);
                for (int k = i + 1; k < pos; k++)
                    letterPlaces.Add(k);
                if (letters is null)
                    continue;                           // l'erreur est signalée à la lecture de l'instruction
                char mark = t.Text switch { "DEFINT" => '%', "DEFSTR" => '$', _ => '!' };
                foreach (int l in letters)
                    suffix[l] = mark;
                any = true;
            }
            else if (t.Text == "BYTE")
            {
                for (int k = i + 1; k < tokens.Count && !IsStatementEnd(tokens[k]); k++)
                    if (tokens[k].Kind == TokenKind.Word)
                        bytes.Add(tokens[k].Text);
            }
        }

        if (!any)
            return tokens;

        var result = new List<Token>(tokens.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            Token t = tokens[i];
            if (t.Kind == TokenKind.Word && !letterPlaces.Contains(i) && IsPlainVariableName(t.Text)
                && !bytes.Contains(t.Text) && suffix[t.Text[0] - 'A'] is char mark)
                t = t with { Text = t.Text + mark };
            result.Add(t);
        }
        return result;
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
        int firstStatement = statements.Count;
        int next = ParseStatementCore(tokens, pos, statements, diagnostics);
        if (statements.Count > firstStatement)
            statementStarts?.Add(statements[firstStatement]);

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

        if (t.Kind == TokenKind.Bar)
            return ParseRsx(tokens, pos, statements, diagnostics);   // |NOM, valeurs (lot K2, D089)

        if (t.Kind != TokenKind.Word)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, t.Line, t.Text));
            return SkipToStatementEnd(tokens, pos);
        }

        switch (t.Text)
        {
            case "PRINT":
                return ParseStreamCommand(tokens, pos, 1, statements, diagnostics, "PRINT", false,
                                          rest => ParsePrint(tokens, pos, statements, diagnostics, rest));

            case "OPENOUT" or "OPENIN":                 // lot J1 (D084)
            {
                pos++;
                Token nameStart = tokens[pos];
                Expression? fileName = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (fileName is null)
                    return SkipToStatementEnd(tokens, pos);
                if (!Types.IsString(fileName))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, nameStart.Line,
                                                   $"{t.Text} attend le nom du fichier, entre guillemets"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new FileStatement(t.Line, t.Text, fileName));
                return pos;
            }

            case "RUN":                                 // RUN "nom" (lot J3, D086)
            {
                pos++;
                if (IsLineEnd(tokens[pos]) || tokens[pos].Kind == TokenKind.Colon || IsWord(tokens[pos], "ELSE"))
                {
                    statements.Add(new RestartStatement(t.Line, null));       // RUN : le programme recommence (D090)
                    return pos;
                }
                if (tokens[pos].Kind == TokenKind.Number)
                {
                    if (!TryParseTarget(tokens, ref pos, diagnostics, out int runLine))
                        return SkipToStatementEnd(tokens, pos);
                    statements.Add(new RestartStatement(t.Line, runLine));    // RUN ligne (D090)
                    return pos;
                }
                Token nameStart = tokens[pos];
                Expression? program = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (program is null)
                    return SkipToStatementEnd(tokens, pos);
                if (!Types.IsString(program))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, nameStart.Line,
                                                   "RUN attend le nom du programme, entre guillemets"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new RunFileStatement(t.Line, program));
                return pos;
            }

            case "CHAIN":                               // lot J3 (D086) : impossible entre deux programmes compilés
                diagnostics.Add(new Diagnostic(DiagnosticCode.NotForCompiled, t.Line,
                    IsWord(tokens[pos + 1], "MERGE")
                        ? "CHAIN MERGE ajoute des lignes BASIC au programme : impossible une fois compilé."
                        : "CHAIN garde les variables du programme : impossible entre deux programmes compilés (chacun range ses variables à sa façon). Utilisez RUN \"nom\" ; les valeurs peuvent passer par un fichier (OPENOUT, puis OPENIN dans l'autre programme)."));
                return SkipToStatementEnd(tokens, pos);

            case "MERGE":                               // lot J3 (D086)
                diagnostics.Add(new Diagnostic(DiagnosticCode.NotForCompiled, t.Line,
                    "MERGE ajoute des lignes BASIC au programme : impossible une fois compilé."));
                return SkipToStatementEnd(tokens, pos);

            case "TRON" or "TROFF":                     // lot K1 (D088)
                statements.Add(new TraceStatement(t.Line, t.Text == "TRON"));
                return pos + 1;

            case "CAT":                                 // lot J2 (D085)
                statements.Add(new FileStatement(t.Line, "CAT"));
                return pos + 1;

            case "SAVE" or "LOAD":                      // lot J2 (D085)
            {
                pos++;
                Token nameStart = tokens[pos];
                Expression? fileName = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (fileName is null)
                    return SkipToStatementEnd(tokens, pos);
                if (!Types.IsString(fileName))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, nameStart.Line,
                                                   $"{t.Text} attend le nom du fichier, entre guillemets"));
                    return SkipToStatementEnd(tokens, pos);
                }
                if (t.Text == "LOAD")
                {
                    Expression? address = null;
                    if (tokens[pos].Kind == TokenKind.Comma)
                    {
                        pos++;
                        address = ParseExpression(tokens, ref pos, diagnostics);
                        if (address is null)
                            return SkipToStatementEnd(tokens, pos);
                    }
                    statements.Add(new LoadStatement(t.Line, fileName, address));
                    return pos;
                }
                // SAVE nom,B,début,longueur[,lancement] ; sans ,B (ou ,A ,P) : le programme BASIC, qui n'existe plus
                if (tokens[pos].Kind != TokenKind.Comma || !IsWord(tokens[pos + 1], "B"))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.SaveProgram, t.Line, "", "SAVE"));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos += 2;
                var values = new List<Expression>();
                while (tokens[pos].Kind == TokenKind.Comma && values.Count < 3)
                {
                    pos++;
                    Expression? value = ParseExpression(tokens, ref pos, diagnostics);
                    if (value is null)
                        return SkipToStatementEnd(tokens, pos);
                    values.Add(value);
                }
                if (values.Count < 2)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, tokens[pos].Text, "SAVE"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new SaveStatement(t.Line, fileName, values[0], values[1], values.Count > 2 ? values[2] : null));
                return pos;
            }

            case "KEY" when IsWord(tokens[pos + 1], "DEF"):          // KEY DEF (lot K5, D092)
            {
                pos += 2;
                var values = new List<Expression>();
                while (true)
                {
                    Expression? value = ParseExpression(tokens, ref pos, diagnostics);
                    if (value is null)
                        return SkipToStatementEnd(tokens, pos);
                    int max = values.Count == 0 ? 79 : values.Count == 1 ? 1 : 255;
                    if (value is NumberExpression number && (number.Value < 0 || number.Value > max))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, t.Line, $"{number.Value}|0 à {max}", "KEY DEF"));
                    values.Add(value);
                    if (tokens[pos].Kind != TokenKind.Comma || values.Count == 5)
                        break;
                    pos++;
                }
                if (values.Count < 2)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, tokens[pos].Text, "KEY DEF"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new KeyDefStatement(t.Line, values));
                return pos;
            }

            case "KEY":                                 // KEY n, texte (lot K5, D092)
            {
                pos++;
                Expression? number = ParseExpression(tokens, ref pos, diagnostics);
                if (number is null)
                    return SkipToStatementEnd(tokens, pos);
                if (number is NumberExpression literal && literal.Value is not (>= 0 and <= 31 or >= 128 and <= 159))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, t.Line, $"{literal.Value}|0 à 31 ou 128 à 159", "KEY"));
                if (tokens[pos].Kind != TokenKind.Comma)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, tokens[pos].Text, "KEY"));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos++;
                Token textStart = tokens[pos];
                Expression? text = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (text is null)
                    return SkipToStatementEnd(tokens, pos);
                if (!Types.IsString(text))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, textStart.Line, "KEY attend un texte après le numéro"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new KeyStatement(t.Line, number, text));
                return pos;
            }

            case "SPEED" when IsWord(tokens[pos + 1], "INK") || IsWord(tokens[pos + 1], "KEY"):   // lot K4 (D091)
            {
                string kind = tokens[pos + 1].Text;
                pos += 2;
                Expression? first = ParseExpression(tokens, ref pos, diagnostics);
                if (first is null)
                    return SkipToStatementEnd(tokens, pos);
                if (tokens[pos].Kind != TokenKind.Comma)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, tokens[pos].Text, "SPEED " + kind));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos++;
                Expression? second = ParseExpression(tokens, ref pos, diagnostics);
                if (second is null)
                    return SkipToStatementEnd(tokens, pos);
                foreach (Expression value in new[] { first, second })
                    if (value is NumberExpression number && (number.Value < 1 || number.Value > 255))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, t.Line, $"{number.Value}|1 à 255", "SPEED " + kind));
                statements.Add(new SpeedStatement(t.Line, kind, first, second));
                return pos;
            }

            case "SPEED":                               // SPEED WRITE (lot J2, D085)
            {
                if (!IsWord(tokens[pos + 1], "WRITE"))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, t.Line, "SPEED " + tokens[pos + 1].Text));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos += 2;
                Expression? speed = ParseExpression(tokens, ref pos, diagnostics);
                if (speed is null)
                    return SkipToStatementEnd(tokens, pos);
                if (speed is NumberExpression number && number.Value is not (0 or 1))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, t.Line, $"{number.Value}|0 ou 1", "SPEED WRITE"));
                statements.Add(new SpeedWriteStatement(t.Line, speed));
                return pos;
            }

            case "CLOSEOUT" or "CLOSEIN":               // lot J1 (D084)
                statements.Add(new FileStatement(t.Line, t.Text));
                return pos + 1;

            case "RESUME":                              // lot I2 (D080)
            {
                pos++;
                if (IsWord(tokens[pos], "NEXT"))
                {
                    statements.Add(new ResumeStatement(t.Line, ResumeKind.Next));
                    return pos + 1;
                }
                if (tokens[pos].Kind == TokenKind.Number && tokens[pos].Text == "0")
                {
                    statements.Add(new ResumeStatement(t.Line, ResumeKind.Same));   // RESUME 0 : comme RESUME
                    return pos + 1;
                }
                if (tokens[pos].Kind == TokenKind.Number)
                {
                    if (!TryParseTarget(tokens, ref pos, diagnostics, out int resumeLine))
                        return SkipToStatementEnd(tokens, pos);
                    statements.Add(new ResumeStatement(t.Line, ResumeKind.Line, resumeLine));
                    return pos;
                }
                statements.Add(new ResumeStatement(t.Line, ResumeKind.Same));
                return pos;
            }

            case "ERROR":                               // lot I2 (D080)
            {
                pos++;
                Expression? code = ParseExpression(tokens, ref pos, diagnostics);
                if (code is null)
                    return SkipToStatementEnd(tokens, pos);
                if (code is NumberExpression number && (number.Value < 1 || number.Value > 255))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, t.Line, $"{number.Value}|1 à 255", "ERROR"));
                statements.Add(new ErrorStatement(t.Line, code));
                return pos;
            }

            case "AFTER" or "EVERY":                    // lot I1 (D079)
                return ParseTimer(tokens, pos, statements, diagnostics);

            case "WRITE":                               // lot H2 (D073)
                return ParseStreamCommand(tokens, pos, 1, statements, diagnostics, "WRITE", false,
                                          rest => ParseWrite(tokens, pos, statements, diagnostics, rest));

            // Fenêtres (lot H1, D072)
            case "WINDOW" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "SWAP":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands["WINDOW SWAP"], pos + 2);

            // Stylo et fond des tracés (lot L4a, D106)
            case "GRAPHICS" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text is "PEN" or "PAPER":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands["GRAPHICS " + tokens[pos + 1].Text], pos + 2);

            // Deuxième valeur seule (D123) : « CURSOR ,1 » et « MASK ,1 », le premier réglage ne change pas
            case "CURSOR" or "MASK" when tokens[pos + 1].Kind == TokenKind.Comma:
                return ParseScreenCommand(tokens, pos, statements, diagnostics,
                                          ScreenCommands[t.Text == "CURSOR" ? "CURSOR USER" : "MASK FIRST"], pos + 2);

            // Caractères (lot H4, D075)
            case "SYMBOL" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "AFTER":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands["SYMBOL AFTER"], pos + 2);

            case "WINDOW" or "CLS" or "LOCATE" or "PEN" or "PAPER" or "TAG" or "TAGOFF":
                return ParseStreamCommand(tokens, pos, 1, statements, diagnostics, t.Text, t.Text is not ("CLS" or "TAG" or "TAGOFF"),
                                          rest => ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands[t.Text], rest));

            case "ERASE":                                   // ERASE T, N$ (lot K8, D096)
            {
                int line = t.Line;
                pos++;
                var names = new List<string>();
                while (true)
                {
                    if (!IsUserName(tokens[pos]))
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEraseName, line, IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
                        return SkipToStatementEnd(tokens, pos);
                    }
                    names.Add(tokens[pos].Text);
                    pos++;
                    if (tokens[pos].Kind != TokenKind.Comma)
                        break;
                    pos++;
                }
                statements.Add(new EraseStatement(line, names));
                return pos;
            }

            case "DEF":                                     // DEF FN (lot K7, D095)
                return ParseDefFn(tokens, pos, statements, diagnostics);

            case "DEFINT" or "DEFREAL" or "DEFSTR":
            {
                // Rien à exécuter : le type des variables a été réglé avant la lecture du programme (D068)
                int end = pos + 1;
                if (ReadLetterRanges(tokens, ref end) is null || !(IsStatementEnd(tokens[end]) || IsElse(tokens[end])))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedLetterRange, tokens[end].Line,
                                                   IsStatementEnd(tokens[end]) ? "" : tokens[end].Text));
                    return SkipToStatementEnd(tokens, end);
                }
                return end;
            }

            case "END":
                if (tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "IF")
                {
                    statements.Add(new EndIfStatement(t.Line));   // END IF
                    return pos + 2;
                }
                if (IsWord(tokens[pos + 1], "SELECT"))
                {
                    statements.Add(new EndSelectStatement(t.Line));   // END SELECT (D206)
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

            case "SOUND":
                return ParseSound(tokens, pos, statements, diagnostics);

            case "ENV" or "ENT":
                return ParseEnvelope(tokens, pos, statements, diagnostics);

            case "CALL" when HasTopLevelComma(tokens, pos + 1):       // CALL adresse, valeurs (lot K2, D089)
            {
                pos++;
                Expression? address = ParseExpression(tokens, ref pos, diagnostics);
                if (address is null)
                    return SkipToStatementEnd(tokens, pos);
                if (!TryParseExternalArguments(tokens, ref pos, diagnostics, "CALL", out List<Expression> callArguments))
                    return SkipToStatementEnd(tokens, pos);
                statements.Add(new ExternalCallStatement(t.Line, null, address, callArguments));
                return pos;
            }

            case "RELEASE" or "VROOM" or "BOOM" or "SHOT" or "DEG" or "RAD" or "MODE" or "ZONE" or "SYMBOL" or "DI" or "EI" or "INK" or "BORDER" or "CLG" or "ORIGIN" or "BOX" or "CIRCLE" or "POKE" or "OUT" or "CALL" or "MEMORY" or "WAIT" or "WIDTH" or "FRAME" or "CURSOR" or "FILL" or "PAINT" or "MASK" or "SPRITE":
                return ParseScreenCommand(tokens, pos, statements, diagnostics, ScreenCommands[t.Text]);

            case "MOVE" or "MOVER" or "PLOT" or "PLOTR" or "DRAW" or "DRAWR":
                return ParseDrawCommand(tokens, pos, statements, diagnostics, DrawCommands[t.Text]);

            case "ELLIPSE":                             // D139
                return ParseEllipse(tokens, pos, statements, diagnostics);

            case "DEFSPR":                              // D198
                return ParseSpriteDef(tokens, pos, statements, diagnostics);

            case "SELECT":                              // D206 : SELECT valeur (SELECT CASE valeur accepté aussi)
            {
                pos++;
                if (IsWord(tokens[pos], "CASE"))
                    pos++;
                Expression? value = IsStatementEnd(tokens[pos]) ? null : ParseAnyExpression(tokens, ref pos, diagnostics);
                if (value is null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.SelectSyntax, t.Line, "SELECT"));
                    return SkipToStatementEnd(tokens, pos);
                }
                statements.Add(new SelectStatement(t.Line, value));
                return pos;
            }

            case "CASE":                                // D206
                return ParseCase(tokens, pos, statements, diagnostics);

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
                return ParseStreamCommand(tokens, pos, 1, statements, diagnostics, "INPUT", true,
                                          rest => ParseInput(tokens, pos, statements, diagnostics, rest));

            case "MID$" when tokens[pos + 1].Kind == TokenKind.LeftParen:
                return ParseMidAssign(tokens, pos, statements, diagnostics);

            case "LINE" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "INPUT":
                return ParseStreamCommand(tokens, pos, 2, statements, diagnostics, "LINE INPUT", true,
                                          rest => ParseLineInput(tokens, pos, statements, diagnostics, rest));

            case "CLEAR" when tokens[pos + 1].Kind == TokenKind.Word && tokens[pos + 1].Text == "INPUT":
                statements.Add(new ClearInputStatement(t.Line));
                return pos + 2;

            case "DEBUG":                               // D136 : DEBUG seul ; D137 : DEBUG A,B$ ; D138 : DEBUG T(3), T(I,2)
            {
                int line = t.Line;
                pos++;
                if (IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]))
                {
                    statements.Add(new DebugStatement(line));
                    return pos;
                }
                int first = pos;
                var items = new List<Expression>();
                while (true)
                {
                    if (!IsUserName(tokens[pos])
                        || (tokens[pos + 1].Kind == TokenKind.LeftParen && tokens[pos + 2].Kind == TokenKind.RightParen))
                    {
                        Token bad = IsUserName(tokens[pos]) ? tokens[pos + 1] : tokens[pos];
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedDebugName, line, IsLineEnd(bad) ? "" : bad.Text));
                        return SkipToStatementEnd(tokens, pos);
                    }
                    Expression? item = ParseAnyExpression(tokens, ref pos, diagnostics);
                    if (item is null)
                        return SkipToStatementEnd(tokens, pos);
                    items.Add(item);
                    if (tokens[pos].Kind != TokenKind.Comma)
                        break;
                    pos++;
                }
                if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedDebugName, line, tokens[pos].Text));
                    return SkipToStatementEnd(tokens, pos);
                }
                var key = new System.Text.StringBuilder();
                for (int k = first; k < pos; k++)
                    key.Append(tokens[k].Text);
                statements.Add(new DebugStatement(line, items, key.ToString()));
                return pos;
            }

            case "CLEAR":                               // lot K3 (D090)
                statements.Add(new ClearStatement(t.Line));
                return pos + 1;

            case "LET":
                return ParseAssignment(tokens, pos + 1, t.Line, statements, diagnostics);

            default:
                // Commande d'une bibliothèque (D144, D148), cherchée avant de dire que le mot est inconnu ; STAR = 5 reste une variable
                if (tokens[pos + 1].Kind != TokenKind.Equals && libraries?.Find(t.Text) is { Kind: LibraryKind.Command } libraryCommand)
                    return ParseLibraryCommand(tokens, pos, libraryCommand, statements, diagnostics);

                // Un mot suivi de « = » est une affectation sans LET ; suivi de « ( », c'est une case de tableau : A(3) = 5 (D058)
                if (tokens[pos + 1].Kind is TokenKind.Equals or TokenKind.LeftParen)
                    return ParseAssignment(tokens, pos, t.Line, statements, diagnostics);

                diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCommand, t.Line, t.Text));
                return SkipToStatementEnd(tokens, pos);
        }
    }

    // ----- Bibliothèques (D144, D148) -----

    // STAR x, y, r1, r2, pointes [, stylo] [, remplissage] [, angle] : valeurs dans l'ordre du questionnaire ;
    // une OPTION peut être sautée (place vide « ,, » ou omise à la fin), comme pour ELLIPSE (D150)
    static int ParseLibraryCommand(IReadOnlyList<Token> tokens, int pos, LibraryCommand command, List<Statement> statements,
                                   List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;
        var written = new List<Expression?>();
        if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos])
            && !ParseLibraryValues(tokens, ref pos, written, diagnostics, out bool _))
            return SkipToStatementEnd(tokens, pos);
        if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, LibrarySignature(command)));
            return SkipToStatementEnd(tokens, pos);
        }
        List<Expression>? arguments = CompleteLibraryArguments(command, written, name.Line, diagnostics);
        if (arguments is null)
            return SkipToStatementEnd(tokens, pos);
        statements.Add(new LibraryCallStatement(name.Line, command.Name, arguments));
        return pos;
    }

    // LUNE(n [, option]) ; une fonction sans valeur obligatoire peut s'écrire sans parenthèses, comme TIME
    static Expression? ParseLibraryFunction(IReadOnlyList<Token> tokens, ref int pos, LibraryCommand function,
                                            List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;
        var written = new List<Expression?>();
        if (tokens[pos].Kind == TokenKind.LeftParen && function.Parameters.Count > 0)
        {
            pos++;
            if (!ParseLibraryValues(tokens, ref pos, written, diagnostics, out bool _))
                return null;
            if (tokens[pos].Kind != TokenKind.RightParen)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, name.Line));
                return null;
            }
            pos++;
        }
        else if (function.Parameters.Any(p => !p.IsOptional))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedOpenParenthesis, name.Line, LibrarySignature(function)));
            return null;
        }
        List<Expression>? arguments = CompleteLibraryArguments(function, written, name.Line, diagnostics);
        return arguments is null ? null : new LibraryCallExpression(function.Name, arguments);
    }

    // Valeurs séparées par des virgules ; une place vide (« 5,,2 ») donne null. Une virgule à la fin (« 5, ») est une faute.
    static bool ParseLibraryValues(IReadOnlyList<Token> tokens, ref int pos, List<Expression?> values,
                                   List<Diagnostic> diagnostics, out bool trailingComma)
    {
        trailingComma = false;
        while (true)
        {
            if (tokens[pos].Kind == TokenKind.Comma)
                values.Add(null);                                // place vide
            else if (IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]) || tokens[pos].Kind == TokenKind.RightParen)
            {
                if (values.Count > 0)
                {
                    trailingComma = true;
                    values.Add(null);                            // « STAR 1,2, » : repéré par CompleteLibraryArguments
                }
                return true;
            }
            else
            {
                // un texte est permis ici (valeur TEXTE, D169) : le genre est vérifié par CompleteLibraryArguments
                Token at = tokens[pos];
                Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
                if (value is not null && !Types.IsString(value))
                    value = IntegerConstant(value, at, diagnostics);
                if (value is null)
                    return false;
                values.Add(value);
            }
            if (tokens[pos].Kind != TokenKind.Comma)
                return true;
            pos++;
        }
    }

    // Vérifie les valeurs écrites et met à la place des OPTION sautées la valeur prévue ; null s'il y a une faute
    static List<Expression>? CompleteLibraryArguments(LibraryCommand command, List<Expression?> written, int line,
                                                      List<Diagnostic> diagnostics)
    {
        bool emptyAtEnd = written.Count > 0 && written[^1] is null;
        if (written.Count > command.Parameters.Count || emptyAtEnd
            || command.Parameters.Where((p, i) => !p.IsOptional && (i >= written.Count || written[i] is null)).Any())
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, line, LibrarySignature(command)));
            return null;
        }
        var arguments = new List<Expression>();
        for (int i = 0; i < command.Parameters.Count; i++)
        {
            LibraryParameter parameter = command.Parameters[i];
            Expression? value = i < written.Count ? written[i] : null;
            if (value is null)
            {
                arguments.Add(new NumberExpression(parameter.Default!.Value));     // OPTION sautée (D150)
                continue;
            }
            // un TEXTE veut un texte, une VALEUR un nombre (D169)
            if (parameter.IsText != Types.IsString(value))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, line,
                    $"{command.Name}, valeur {parameter.Name.ToLowerInvariant()}{(parameter.IsText ? "$" : "")}"));
                return null;
            }
            if (parameter.IsText)
            {
                arguments.Add(value);
                continue;
            }
            int before = diagnostics.Count;
            CheckConstantRange(value, parameter.Min, parameter.Max, line, diagnostics);
            if (diagnostics.Count > before)
                diagnostics[^1] = diagnostics[^1] with { Context = $"{command.Name}, valeur {parameter.Name.ToLowerInvariant()}" };
            arguments.Add(value);
        }
        libraryUses?.TryAdd(command.Name, line);
        return arguments;
    }

    static string LibrarySignature(LibraryCommand command) => LibraryCommandFile.Signature(command);   // D155

    // Le code assembleur des commandes utilisées (et des routines qu'elles appellent) est vérifié ici
    static void CheckLibraries(List<Diagnostic> diagnostics)
    {
        if (libraries is null || libraryUses is null || libraryUses.Count == 0)
            return;
        var (_, errors) = LibraryLinker.Collect(libraries, libraryUses.Keys);
        int firstUse = libraryUses.Values.Min();
        foreach (LibraryCodeError error in errors)
        {
            int line = libraryUses.TryGetValue(error.Command, out int used) ? used : firstUse;
            diagnostics.Add(new Diagnostic(DiagnosticCode.LibraryCode, line, error.ToString()));
        }
    }

    // |NOM [, valeurs] : commande RSX (lot K2, D089). Le nom est un mot (lettres et chiffres), comme |DISC ou |GPEN.
    static int ParseRsx(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;
        if (tokens[pos].Kind != TokenKind.Word)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, line, IsLineEnd(tokens[pos]) ? "|" : tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        string name = tokens[pos].Text;
        pos++;
        if (!TryParseExternalArguments(tokens, ref pos, diagnostics, "|" + name, out List<Expression> arguments))
            return SkipToStatementEnd(tokens, pos);
        statements.Add(new ExternalCallStatement(line, name, null, arguments));
        return pos;
    }

    // « , valeur » répété (32 au plus) : nombre, calcul, @variable ou texte (D089)
    static bool TryParseExternalArguments(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                                          string command, out List<Expression> arguments)
    {
        arguments = new List<Expression>();
        while (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
            if (value is null)
                return false;
            arguments.Add(value);
        }
        if (arguments.Count > 32)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, tokens[pos].Line, $"{arguments.Count}|32 valeurs au plus", command));
            return false;
        }
        return true;
    }

    // Une virgule avant la fin de l'instruction, hors des parenthèses (CALL adresse, valeurs)
    static bool HasTopLevelComma(IReadOnlyList<Token> tokens, int pos)
    {
        int depth = 0;
        for (; !IsLineEnd(tokens[pos]) && tokens[pos].Kind != TokenKind.Colon && !IsWord(tokens[pos], "ELSE"); pos++)
        {
            if (tokens[pos].Kind == TokenKind.LeftParen)
                depth++;
            else if (tokens[pos].Kind == TokenKind.RightParen)
                depth--;
            else if (tokens[pos].Kind == TokenKind.Comma && depth == 0)
                return true;
        }
        return false;
    }

    // INPUT [;] ["texte" ; ou ,] variable [, variable ...]
    static int ParseInput(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics, int? restStart = null)
    {
        int line = tokens[pos].Line;
        pos = restStart ?? pos + 1;

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
            if (!VariableNames.IsSupported(name.Text)
                && (!VariableNames.IsReal(name.Text) || tokens[pos + 1].Kind == TokenKind.LeftParen))
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

        // Un mot réservé ne peut pas recevoir de valeur ; une fonction non plus (ISIN = 5, D142)
        if (name.Kind != TokenKind.Word || Commands.Contains(name.Text) || Functions.ContainsKey(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, name.Line, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }

        // A! = valeur : nombre à virgule (D068) ; pas encore de tableau de nombres à virgule
        if (!VariableNames.IsSupported(name.Text)
            && (!VariableNames.IsReal(name.Text) || tokens[pos + 1].Kind == TokenKind.LeftParen))
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
        Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
        if (value is null)
            return SkipToStatementEnd(tokens, pos);

        // A$ et N$(I) reçoivent un texte, A et T(I) reçoivent un nombre (D062, D065)
        if (VariableNames.IsString(name.Text) != Types.IsString(value))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, valueStart.Line,
                                           VariableNames.IsString(name.Text) ? "un texte est attendu" : "un nombre est attendu"));
            return SkipToStatementEnd(tokens, pos);
        }

        if (!VariableNames.IsString(name.Text) && !VariableNames.IsReal(name.Text)
            && IntegerConstant(value, valueStart, diagnostics) is Expression whole)
            value = whole;                              // A% = 2.5 : 3 dès la compilation (D068)
        else if (!VariableNames.IsString(name.Text) && !VariableNames.IsReal(name.Text))
            return SkipToStatementEnd(tokens, pos);

        statements.Add(indexes is null
            ? new AssignStatement(line, name.Text, value)
            : (Statement)new ArrayAssignStatement(line, name.Text, indexes, value));
        return pos;
    }

    // LINE INPUT [;] ["texte" ; ou ,] A$ : toute la ligne tapée, sans « ? » (D064)
    static int ParseLineInput(IReadOnlyList<Token> tokens, int pos,
                              List<Statement> statements, List<Diagnostic> diagnostics, int? restStart = null)
    {
        int line = tokens[pos].Line;
        pos = restStart ?? pos + 2;

        bool noNewline = false;
        if (tokens[pos].Kind == TokenKind.Semicolon)
        {
            noNewline = true;
            pos++;
        }

        string? prompt = null;
        bool question = true;                           // « ? » comme INPUT, sauf « texte , variable » (D093)
        if (tokens[pos].Kind == TokenKind.String)
        {
            prompt = tokens[pos].Text;
            pos++;
            if (tokens[pos].Kind is not (TokenKind.Semicolon or TokenKind.Comma))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, tokens[pos].Line, tokens[pos].Text));
                return SkipToStatementEnd(tokens, pos);
            }
            question = tokens[pos].Kind == TokenKind.Semicolon;
            pos++;
        }

        Token name = tokens[pos];
        if (name.Kind != TokenKind.Word || Commands.Contains(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedVariableName, name.Line, IsStatementEnd(name) ? "" : name.Text));
            return SkipToStatementEnd(tokens, pos);
        }
        if (!VariableNames.IsString(name.Text))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, name.Line, "LINE INPUT range un texte dans une variable A$"));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;
        List<Expression>? cells = null;
        if (tokens[pos].Kind == TokenKind.LeftParen
            && (!CheckArrayName(name, diagnostics) || !TryParseIndexes(tokens, ref pos, diagnostics, out cells)))
            return SkipToStatementEnd(tokens, pos);   // LINE INPUT N$(I) : une case d'un tableau de textes (D065)

        statements.Add(new LineInputStatement(line, prompt, noNewline, name.Text, cells, question));
        return pos;
    }

    // MID$(A$, début [, longueur]) = texte (D065)
    static int ParseMidAssign(IReadOnlyList<Token> tokens, int pos,
                              List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos += 2;                                       // MID$ (

        Token targetStart = tokens[pos];
        Expression? target = ParseTarget(tokens, ref pos, diagnostics);
        if (target is null)
            return SkipToStatementEnd(tokens, pos);
        if (!Types.IsString(target))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, targetStart.Line, "MID$ change une variable texte, comme A$"));
            return SkipToStatementEnd(tokens, pos);
        }
        if (tokens[pos].Kind != TokenKind.Comma)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, tokens[pos].Line, "MID$(texte, début [, longueur]) = texte"));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Expression? start = ParseExpression(tokens, ref pos, diagnostics);
        if (start is null)
            return SkipToStatementEnd(tokens, pos);
        Expression? length = null;
        if (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            length = ParseExpression(tokens, ref pos, diagnostics);
            if (length is null)
                return SkipToStatementEnd(tokens, pos);
        }
        if (tokens[pos].Kind != TokenKind.RightParen)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, tokens[pos].Line));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;
        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, tokens[pos].Line, tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;

        Token valueStart = tokens[pos];
        Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
        if (value is null)
            return SkipToStatementEnd(tokens, pos);
        if (!Types.IsString(value))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, valueStart.Line, "un texte est attendu"));
            return SkipToStatementEnd(tokens, pos);
        }

        statements.Add(new MidAssignStatement(line, target, start, length, value));
        return pos;
    }

    // ----- DATA, READ, RESTORE (lot C, D059) -----

    // DATA élément [, élément ...] (D059, textes D064) : chaque élément arrive du lexique tel qu'il est écrit (DataItem).
    static int ParseData(IReadOnlyList<Token> tokens, int pos,
                         List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;

        var items = new List<DataItem>();
        while (tokens[pos].Kind == TokenKind.DataItem)
        {
            DataItem? item = DataItemOf(tokens[pos], diagnostics);
            if (item is null)
                return SkipToStatementEnd(tokens, pos);
            items.Add(item);
            pos++;

            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        statements.Add(new DataStatement(line, items));
        return pos;
    }

    // Un élément de DATA : un nombre (décimal de -32768 à 65535, ou &hexadécimal, &Xbinaire, avec un signe possible ;
    // entre guillemets aussi), sinon un texte (entre guillemets, ou tel qu'il est écrit). Vide : le nombre 0.
    static DataItem? DataItemOf(Token t, List<Diagnostic> diagnostics)
    {
        string inner = t.Text;
        if (inner.StartsWith('"'))
            inner = inner.Length >= 2 && inner.EndsWith('"') ? inner[1..^1] : inner[1..];

        string number = inner.Trim();
        if (number.Length == 0)
            return new DataItem(false, 0, inner);

        bool negative = number[0] == '-';
        string digits = number[0] is '-' or '+' ? number[1..].TrimStart() : number;

        if (digits.Length > 0 && digits.All(char.IsAsciiDigit))
        {
            if (long.TryParse(digits, out long value) && value <= (negative ? 32768 : 65535))
            {
                int word = (int)(negative ? -value : value);
                return new DataItem(false, Signed16(word & 0xFFFF), inner);
            }
        }

        // 3.14, .5, 1E5, ou un entier trop grand (70000) : nombre à virgule, comme le Locomotive (D070)
        if (System.Text.RegularExpressions.Regex.IsMatch(digits, @"^(\d+\.?\d*|\.\d+)([Ee][+-]?\d+)?$")
            && double.TryParse(digits, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out double real))
        {
            if (negative)
                real = -real;
            if (CpcBasic.Core.Codegen.LocoReal.Encode(real) is null)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, t.Line, number));
                return null;
            }
            return new DataItem(false, 0, inner, IsReal: true, Real: real);
        }

        if (TryHexText(digits, out int hex))
            return new DataItem(false, Signed16((negative ? -hex : hex) & 0xFFFF), inner);

        if (inner.Length > 255)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.StringTooLong, t.Line, inner.Length.ToString()));
            return null;
        }
        return new DataItem(true, 0, inner);
    }

    // « &C9 », « &HC9 », « &X1010 » écrit dans un texte (DATA) : vrai si c'est bien un nombre de 0 à &FFFF
    static bool TryHexText(string text, out int value)
    {
        value = 0;
        string upper = text.ToUpperInvariant();
        if (!upper.StartsWith('&'))
            return false;
        string digits = upper[1..];
        bool binary = digits.StartsWith('X');
        if (binary || digits.StartsWith('H'))
            digits = digits[1..];
        if (digits.Length == 0 || !digits.All(c => binary ? (c is '0' or '1') : Uri.IsHexDigit(c)))
            return false;
        int? parsed = Lexer.HexValue(upper);
        if (parsed is null)
            return false;
        value = parsed.Value;
        return true;
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
        if (libraries?.Find(name.Text) is { Kind: LibraryKind.Function } function)     // D144 : nom déjà pris par une fonction
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.LibraryArrayName, name.Line, $"{name.Text}|{function.Library}"));
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
    // DEBUG A,B$ (D137) et DEBUG T(3) (D138) : chaque variable doit être utilisée ailleurs dans le programme, chaque
    // tableau aussi, avec le bon nombre de numéros ; un numéro écrit en nombre doit exister quand DIM est écrit en nombres.
    static void CheckDebugNames(IReadOnlyList<Statement> statements, List<Diagnostic> diagnostics)
    {
        if (!statements.Any(s => s is DebugStatement { Items: not null }))
            return;
        (HashSet<string> names, Dictionary<string, string?> arrays) = DebugVariables.Collect(statements);

        var counts = new Dictionary<string, int>();                 // nombre de numéros de chaque tableau
        var sizes = new Dictionary<string, IReadOnlyList<int>>();   // DIM écrit en nombres
        foreach (Statement s in statements)
        {
            if (s is DimStatement dim)
                foreach (ArrayDeclaration array in dim.Arrays)
                {
                    counts.TryAdd(array.Name, array.Sizes.Count);
                    if (array.Sizes.All(x => x is NumberExpression))
                        sizes.TryAdd(array.Name, array.Sizes.Select(x => ((NumberExpression)x).Value).ToList());
                }
            if (s is ArrayAssignStatement assign)
                counts.TryAdd(assign.Name, assign.Indexes.Count);
            foreach (Expression e in AstWalker.All(s))
                if (e is ArrayElementExpression a)
                    counts.TryAdd(a.Name, a.Indexes.Count);
        }

        foreach (DebugStatement debug in statements.OfType<DebugStatement>())
            foreach (Expression item in debug.Items ?? [])
            {
                int line = debug.SourceLine;
                if (item is VariableExpression v)
                {
                    if (!names.Contains(v.Name))
                        diagnostics.Add(new Diagnostic(arrays.ContainsKey(v.Name) ? DiagnosticCode.DebugArrayName
                                                                                   : DiagnosticCode.DebugUnknownVariable, line, v.Name));
                    continue;
                }
                if (item is not ArrayElementExpression element)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedDebugName, line));
                    continue;
                }
                if (!arrays.ContainsKey(element.Name))
                {
                    diagnostics.Add(new Diagnostic(names.Contains(element.Name) ? DiagnosticCode.DebugNotArray
                                                                                : DiagnosticCode.DebugUnknownVariable, line, element.Name));
                    continue;
                }
                if (counts.TryGetValue(element.Name, out int count) && count != element.Indexes.Count)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ArrayDimensionMismatch, line,
                                                   $"{element.Name}|{element.Indexes.Count}|{count}"));
                    continue;
                }
                if (sizes.TryGetValue(element.Name, out IReadOnlyList<int>? max))
                    for (int k = 0; k < element.Indexes.Count; k++)
                        if (element.Indexes[k] is NumberExpression n && (n.Value < 0 || n.Value > max[k]))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.DebugIndexTooBig, line, $"{n.Value}|{max[k]}"));
                foreach (Expression inner in element.Indexes.SelectMany(AstWalker.Flatten))
                    if (inner is VariableExpression iv && !names.Contains(iv.Name))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.DebugUnknownVariable, line, iv.Name));
            }
    }

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

    // PRINT [#n,] éléments séparés par « ; » (collés) ou « , » (zone suivante, lot H2, D073). Éléments : texte, valeur,
    // TAB(colonne), SPC(nombre). Seul TAB(...) ou SPC(...) peut être suivi d'un élément sans séparateur (PRINT TAB(10)"X").
    // Sans « ; » ni « , » à la fin, on passe à la ligne.
    static int ParsePrint(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics, int? restStart = null)
    {
        int line = tokens[pos].Line;
        pos = restStart ?? pos + 1;

        var items = new List<Statement>();
        bool newLine = true;

        while (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            Token t = tokens[pos];
            if (t.Kind == TokenKind.Semicolon)
            {
                pos++;
                newLine = false;
                continue;
            }
            if (t.Kind == TokenKind.Comma)
            {
                items.Add(new PrintZoneStatement(line));
                pos++;
                newLine = false;
                continue;
            }

            if (t.Kind == TokenKind.Word && t.Text == "USING")   // la suite du PRINT est écrite avec le format (D074)
            {
                if (!ParseUsing(tokens, ref pos, line, items, diagnostics, out newLine))
                    return SkipToStatementEnd(tokens, pos);
                break;
            }

            newLine = true;
            Token after = tokens[pos + 1];
            if (t.Kind == TokenKind.Word && t.Text is "TAB" or "SPC" && after.Kind == TokenKind.LeftParen)
            {
                pos += 2;
                Expression? count = ParseExpression(tokens, ref pos, diagnostics);
                if (count is null)
                    return SkipToStatementEnd(tokens, pos);
                if (tokens[pos].Kind != TokenKind.RightParen)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedClosingParenthesis, line, "", t.Text));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos++;
                items.Add(new PrintTabStatement(line, count, t.Text == "SPC"));
                continue;                               // PRINT TAB(10)"X" : l'élément peut suivre directement
            }

            if (t.Kind == TokenKind.String
                && (IsPrintSeparatorOrEnd(after) || after.Kind == TokenKind.String))
            {
                items.Add(new PrintStatement(line, t.Text, false));
                pos++;
            }
            else
            {
                Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);   // nombre ou texte (A$, "A" + B$)
                if (value is null)
                    return SkipToStatementEnd(tokens, pos);
                items.Add(new PrintValueStatement(line, value, false));
            }

            if (!IsPrintSeparatorOrEnd(tokens[pos]))
                break;                                  // la suite (PRINT "A" "B") sera signalée comme inattendue
        }

        if (newLine)
        {
            if (items.Count > 0 && items[^1] is PrintStatement text)
                items[^1] = text with { NewLine = true };
            else if (items.Count > 0 && items[^1] is PrintValueStatement value)
                items[^1] = value with { NewLine = true };
            else
                items.Add(new PrintStatement(line, "", true));   // PRINT seul, ou fini par TAB(n) : saut de ligne
        }
        statements.AddRange(items);
        return pos;
    }

    /// <summary>Un morceau de format de PRINT USING : texte à écrire tel quel, ou emplacement (D074).</summary>
    sealed record UsingPart(string? Text, bool IsString = false, int Width = 0, int Before = 0, int Decimals = 0, int Flags = 0)
    {
        public bool IsField => Text is null;
    }

    // USING "format"; valeur {; ou , valeur} [; ou ,] (lot H3, D074). Le format est découpé à la compilation : le texte
    // autour des emplacements est écrit tel quel ; chaque valeur prend l'emplacement suivant ; à la fin du format, on
    // reprend au début ; après la dernière valeur, le texte qui suit est écrit jusqu'à l'emplacement suivant.
    static bool ParseUsing(IReadOnlyList<Token> tokens, ref int pos, int line, List<Statement> items,
                           List<Diagnostic> diagnostics, out bool newLine)
    {
        newLine = true;
        pos++;
        if (tokens[pos].Kind != TokenKind.String || tokens[pos + 1].Kind != TokenKind.Semicolon)
            return ParseUsingDynamic(tokens, ref pos, line, items, diagnostics, out newLine);
        string format = tokens[pos].Text;
        pos += 2;

        List<UsingPart> parts = ParseUsingFormat(format);
        if (!parts.Any(p => p.IsField))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UsingWithoutField, line, format));
            return false;
        }

        var values = new List<Expression>();
        while (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
            if (value is null)
                return false;
            values.Add(value);
            newLine = true;
            if (tokens[pos].Kind is not (TokenKind.Semicolon or TokenKind.Comma))
                break;
            pos++;
            newLine = false;
        }
        if (values.Count == 0)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, line, tokens[pos].Text, "PRINT USING"));
            return false;
        }

        int index = 0;
        foreach (Expression value in values)
        {
            while (true)
            {
                if (index == parts.Count)
                    index = 0;
                UsingPart part = parts[index++];
                if (!part.IsField)
                {
                    items.Add(new PrintStatement(line, part.Text!, false));
                    continue;
                }
                if (part.IsString != Types.IsString(value))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, line,
                                                   part.IsString ? "un texte est attendu" : "un nombre est attendu", "PRINT USING"));
                    return false;
                }
                items.Add(part.IsString
                    ? new PrintUsingStringStatement(line, value, part.Width)
                    : new PrintUsingNumberStatement(line, value, part.Width, part.Before, part.Decimals, part.Flags));
                break;
            }
        }
        while (index < parts.Count && !parts[index].IsField)
            items.Add(new PrintStatement(line, parts[index++].Text!, false));
        return true;
    }

    // Liste des valeurs de PRINT USING, séparées par « ; » ou « , » ; newLine faux si la liste finit par l'un d'eux
    static List<Expression>? ParseUsingValues(IReadOnlyList<Token> tokens, ref int pos, int line,
                                             List<Diagnostic> diagnostics, out bool newLine)
    {
        newLine = true;
        var values = new List<Expression>();
        while (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
            if (value is null)
                return null;
            values.Add(value);
            newLine = true;
            if (tokens[pos].Kind is not (TokenKind.Semicolon or TokenKind.Comma))
                break;
            pos++;
            newLine = false;
        }
        if (values.Count == 0)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, line, tokens[pos].Text, "PRINT USING"));
            return null;
        }
        return values;
    }

    // PRINT USING F$; valeurs (lot K10, D098) : format dans une variable ou un calcul, lu pendant l'exécution
    static bool ParseUsingDynamic(IReadOnlyList<Token> tokens, ref int pos, int line, List<Statement> items,
                                  List<Diagnostic> diagnostics, out bool newLine)
    {
        newLine = true;
        Expression? format = ParseAnyExpression(tokens, ref pos, diagnostics);
        if (format is null)
            return false;
        if (!Types.IsString(format))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, line, "le format doit être un texte", "PRINT USING"));
            return false;
        }
        if (tokens[pos].Kind != TokenKind.Semicolon)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UsingFormatNotLiteral, line, IsLineEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return false;
        }
        pos++;
        List<Expression>? values = ParseUsingValues(tokens, ref pos, line, diagnostics, out newLine);
        if (values is null)
            return false;
        items.Add(new PrintUsingDynamicStatement(line, format, values));
        return true;
    }

    /// <summary>
    /// Format de DEC$ (D103) : un seul emplacement de nombre, sans rien autour ; null sinon (« Improper argument »).
    /// </summary>
    internal static (int Width, int Before, int Decimals, int Flags)? DecField(string format)
    {
        List<UsingPart> parts = ParseUsingFormat(format);
        return parts is [{ IsField: true, IsString: false } field] ? (field.Width, field.Before, field.Decimals, field.Flags) : null;
    }

    // Découpe un format de PRINT USING (D074). Texte : « ! » (1re lettre), « & » (tout), « \  \ » (2 + espaces lettres).
    // Nombre : [+] [** [$] | $$] # et , [. #] [^^^^] [+ ou -], comme le Locomotive.
    static List<UsingPart> ParseUsingFormat(string f)
    {
        var parts = new List<UsingPart>();
        var literal = new System.Text.StringBuilder();
        void Flush()
        {
            if (literal.Length > 0)
                parts.Add(new UsingPart(literal.ToString()));
            literal.Clear();
        }

        int i = 0;
        while (i < f.Length)
        {
            char c = f[i];
            if (c is '!' or '&')
            {
                Flush();
                parts.Add(new UsingPart(null, IsString: true, Width: c == '!' ? 1 : 0));
                i++;
                continue;
            }
            if (c == '\\')
            {
                int j = i + 1;
                while (j < f.Length && f[j] == ' ')
                    j++;
                if (j < f.Length && f[j] == '\\')
                {
                    Flush();
                    parts.Add(new UsingPart(null, IsString: true, Width: j - i + 1));
                    i = j + 1;
                    continue;
                }
            }
            int end = TryUsingNumber(f, i, out UsingPart? field);
            if (field is not null)
            {
                Flush();
                parts.Add(field);
                i = end;
                continue;
            }
            literal.Append(c);
            i++;
        }
        Flush();
        return parts;
    }

    static int TryUsingNumber(string f, int start, out UsingPart? field)
    {
        bool At(int k, string s) => k + s.Length <= f.Length && string.CompareOrdinal(f, k, s, 0, s.Length) == 0;
        field = null;
        int i = start, flags = 0, before = 0, decimals = 0;
        if (At(i, "+"))
        {
            flags |= UsingFlags.PlusLead;
            i++;
        }
        if (At(i, "**"))
        {
            flags |= UsingFlags.Asterisk;
            before += 2;
            i += 2;
            if (At(i, "$"))
            {
                flags |= UsingFlags.Dollar;
                before++;
                i++;
            }
        }
        else if (At(i, "$$"))
        {
            flags |= UsingFlags.Dollar;
            before += 2;
            i += 2;
        }
        while (i < f.Length && (f[i] == '#' || (f[i] == ',' && before > 0)))
        {
            if (f[i] == ',')
                flags |= UsingFlags.Comma;
            before++;
            i++;
        }
        bool point = false;
        if (At(i, ".") && (before > 0 || At(i + 1, "#")))
        {
            point = true;
            flags |= UsingFlags.Point;
            i++;
            while (i < f.Length && f[i] == '#')
            {
                decimals++;
                i++;
            }
        }
        if (before == 0 && !point)
            return start;                               // pas un emplacement : « + » ou « $ » seul est du texte
        if (At(i, "^^^^"))
        {
            flags |= UsingFlags.Exponent;
            i += 4;
        }
        if ((flags & UsingFlags.PlusLead) == 0 && (At(i, "-") || At(i, "+")))
        {
            flags |= f[i] == '-' ? UsingFlags.TrailMinus : UsingFlags.TrailPlus;
            i++;
        }
        field = new UsingPart(null, Width: Math.Min(i - start, 255), Before: Math.Min(before, 255),
                              Decimals: Math.Min(decimals, 255), Flags: flags);
        return i;
    }

    static bool IsPrintSeparatorOrEnd(Token token) =>
        token.Kind is TokenKind.Semicolon or TokenKind.Comma || IsStatementEnd(token) || IsElse(token);

    // WRITE [#n,] [valeur {, valeur}] (lot H2, D073) : textes entre guillemets, nombres sans espace, séparés par des
    // virgules, puis passage à la ligne. « ; » est accepté comme séparateur, comme le Locomotive.
    static int ParseWrite(IReadOnlyList<Token> tokens, int pos,
                          List<Statement> statements, List<Diagnostic> diagnostics, int? restStart = null)
    {
        int line = tokens[pos].Line;
        pos = restStart ?? pos + 1;

        var items = new List<Statement>();
        while (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            Expression? value = ParseAnyExpression(tokens, ref pos, diagnostics);
            if (value is null)
                return SkipToStatementEnd(tokens, pos);

            if (items.Count > 0)
                items.Add(new PrintStatement(line, ",", false));
            if (value is StringLiteralExpression literal)
            {
                items.Add(new PrintStatement(line, "\"" + literal.Text + "\"", false));
            }
            else if (Types.IsString(value))
            {
                items.Add(new PrintStatement(line, "\"", false));
                items.Add(new PrintValueStatement(line, value, false));
                items.Add(new PrintStatement(line, "\"", false));
            }
            else
            {
                items.Add(new PrintValueStatement(line, new CallExpression("STR$", [value]), false, NoLeadingSpace: true));
            }

            if (tokens[pos].Kind is not (TokenKind.Comma or TokenKind.Semicolon))
                break;
            pos++;
        }

        items.Add(new PrintStatement(line, "", true));
        statements.AddRange(items);
        return pos;
    }

    // DEFSPR numéro, "ligne", "ligne"... (D198) : l'image d'un sprite, une ligne de points par texte ; chaque point est un
    // stylo (0 à 9, A à F) ou « . » (le décor reste visible)
    static int ParseSpriteDef(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;
        if (tokens[pos].Kind != TokenKind.Number || !int.TryParse(tokens[pos].Text, out int number))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteDefinition, name.Line, IsStatementEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        if (number < 1 || number > SpriteMaxNumber)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line, $"{number}|1 à {SpriteMaxNumber}"));
            return SkipToStatementEnd(tokens, pos);
        }
        pos++;
        var rows = new List<string>();
        while (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            if (tokens[pos].Kind != TokenKind.String)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteDefinition, name.Line, IsStatementEnd(tokens[pos]) ? "" : tokens[pos].Text));
                return SkipToStatementEnd(tokens, pos);
            }
            string row = tokens[pos].Text.ToUpperInvariant();
            if (row.Length == 0 || row.Length > SpriteMaxSize || row.Any(c => c != '.' && !char.IsAsciiHexDigit(c)))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteRow, name.Line, tokens[pos].Text));
                return SkipToStatementEnd(tokens, pos);
            }
            rows.Add(row);
            pos++;
        }
        if (rows.Count == 0 || !(IsStatementEnd(tokens[pos]) || IsElse(tokens[pos])))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteDefinition, name.Line, IsStatementEnd(tokens[pos]) ? "" : tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }
        statements.Add(new SpriteDefStatement(name.Line, number, rows));
        return pos;
    }

    const int SpriteMaxNumber = 16, SpriteMaxSize = 16;

    // Les DEFSPR du même numéro qui se suivent forment un seul dessin (D198) : la première reçoit toutes les lignes
    static void CheckSprites(List<Statement> statements, List<Diagnostic> diagnostics)
    {
        void Replace(int index, Statement statement)
        {
            if (statementStarts!.Remove(statements[index]))
                statementStarts.Add(statement);
            statements[index] = statement;
        }

        for (int i = 0; i < statements.Count; i++)
        {
            if (statements[i] is not SpriteDefStatement first)
                continue;
            var rows = new List<string>(first.Rows);
            int j = i + 1;
            while (j < statements.Count && statements[j] is SpriteDefStatement next && next.Number == first.Number)
            {
                rows.AddRange(next.Rows);
                j++;
            }
            if (rows.Any(r => r.Length != rows[0].Length))
                diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteSize, first.SourceLine, $"largeur|{first.Number}"));
            else if (rows.Count > SpriteMaxSize)
                diagnostics.Add(new Diagnostic(DiagnosticCode.SpriteSize, first.SourceLine, $"hauteur|{first.Number}"));
            Replace(i, first with { Rows = rows });
            for (int k = i + 1; k < j; k++)
                Replace(k, ((SpriteDefStatement)statements[k]) with { Rows = [], Continuation = true });
            i = j - 1;
        }
    }

    // MODE n | CLS | LOCATE colonne, ligne | PEN n | PAPER n | INK stylo, couleur [, couleur2] | BORDER couleur [, couleur2]
    static int ParseScreenCommand(IReadOnlyList<Token> tokens, int pos, List<Statement> statements,
                                  List<Diagnostic> diagnostics, ScreenCommandInfo info, int? restStart = null)
    {
        Token name = tokens[pos];
        pos = restStart ?? pos + 1;

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

        if (arguments.Count < info.MinArguments || arguments.Count > info.Min.Length
            || (name.Text == "ORIGIN" && arguments.Count is > 2 and < 6)    // ORIGIN : 2 ou 6 valeurs (D104)
            || (name.Text == "SPRITE" && arguments.Count == 2))             // SPRITE : 1 ou 3 valeurs (D198)
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

        // MEMORY plus haut que le HIMEM de départ (&A67B) : « Memory full » sur le CPC (D125)
        if (name.Text == "MEMORY" && arguments[0] is NumberExpression top && (top.Value & 0xFFFF) > MemoryHighest)
            diagnostics.Add(new Diagnostic(DiagnosticCode.MemoryTooHigh, name.Line, $"{top.Value & 0xFFFF:X4}"));

        statements.Add(new ScreenStatement(name.Line, info.Command ?? name.Text, arguments));
        return pos;
    }

    // Commande qui accepte un numéro de fenêtre « #n » juste après son nom (lot H1, D072) : PRINT #1, "A" ; LOCATE #2, 5, 3 ;
    // CLS #1 ; INPUT #1, A$... La commande est lue par parse (qui reçoit la position après « #n, ») ; elle est
    // entourée du choix de la fenêtre n puis du retour à la fenêtre #0, comme le fait le Locomotive après chaque commande.
    static int ParseStreamCommand(IReadOnlyList<Token> tokens, int pos, int nameLength, List<Statement> statements,
                                  List<Diagnostic> diagnostics, string command, bool commaRequired, Func<int, int> parse)
    {
        int line = tokens[pos].Line;
        int rest = pos + nameLength;
        int start = statements.Count;
        if (!ParseStreamPrefix(tokens, ref rest, diagnostics, command, commaRequired, out Expression? stream))
            return SkipToStatementEnd(tokens, rest);

        int end = parse(rest);
        if (stream is not null)
        {
            statements.Insert(start, new StreamStatement(line, stream));
            statements.Add(new StreamStatement(line, null));
        }
        return end;
    }

    // « #n » suivi d'une virgule (D072). Sans « # », rien n'est lu et stream reste null. Rend faux si l'écriture est fausse.
    // Les fenêtres vont de #0 à #7 ; #8 (imprimante) et #9 (disquette, cassette) viendront avec le lot J.
    static bool ParseStreamPrefix(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                                  string command, bool commaRequired, out Expression? stream)
    {
        stream = null;
        if (tokens[pos].Kind != TokenKind.Hash)
            return true;
        int line = tokens[pos].Line;
        pos++;

        stream = ParseExpression(tokens, ref pos, diagnostics);
        if (stream is null)
            return false;
        CheckStreamNumber(stream, line, command, diagnostics);

        if (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            return true;
        }
        if (commaRequired || !(IsStatementEnd(tokens[pos]) || IsElse(tokens[pos])))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedStreamComma, line, tokens[pos].Text, command));
            return false;
        }
        return true;
    }

    static void CheckStreamNumber(Expression stream, int line, string command, List<Diagnostic> diagnostics)
    {
        // #9 : le fichier ouvert par OPENOUT ou OPENIN, pour PRINT, WRITE, INPUT et LINE INPUT (D084)
        if (stream is NumberExpression { Value: 9 } && command is "PRINT" or "WRITE" or "INPUT" or "LINE INPUT")
            return;
        // #8 : l'imprimante, pour PRINT, WRITE et POS (lot K12, D100)
        if (stream is NumberExpression { Value: 8 } && command is "PRINT" or "WRITE" or "POS")
            return;
        if (stream is NumberExpression number && (number.Value < 0 || number.Value > 7))
            diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, line,
                                           $"#{number.Value}|#0 à #7, #8 (imprimante) avec PRINT, WRITE et POS, ou #9 avec PRINT, WRITE, INPUT et LINE INPUT", command));
    }

    const string SoundSignature = "SOUND canal, période [, durée [, volume [, env. volume [, env. ton [, bruit]]]]]";

    // SOUND (lot G1, D071) : les valeurs absentes (même au milieu : SOUND 1, 200, , 5) prennent la valeur du Locomotive :
    // durée 20 (centièmes de seconde), volume 12, enveloppes 0, bruit 0.
    static int ParseSound(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;

        var given = new List<Expression?>();
        while (true)
        {
            if (tokens[pos].Kind == TokenKind.Comma || IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]))
            {
                given.Add(null);
            }
            else
            {
                Expression? value = ParseExpression(tokens, ref pos, diagnostics);
                if (value is null)
                    return SkipToStatementEnd(tokens, pos);
                given.Add(value);
            }
            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }

        if (given.Count < 2 || given.Count > 7 || given[0] is null || given[1] is null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, SoundSignature));
            return SkipToStatementEnd(tokens, pos);
        }

        int[] defaults = [0, 0, 20, 12, 0, 0, 0];
        int[] min = [1, 0, -32768, 0, 0, 0, 0];
        int[] max = [255, 4095, 32767, 15, 15, 15, 31];
        var arguments = new List<Expression>();
        for (int i = 0; i < 7; i++)
        {
            Expression value = i < given.Count && given[i] is Expression e ? e : new NumberExpression(defaults[i]);
            if (value is NumberExpression number && (number.Value < min[i] || number.Value > max[i]))
                diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line, $"{number.Value}|{min[i]} à {max[i]}"));
            arguments.Add(value);
        }

        statements.Add(new ScreenStatement(name.Line, "SOUND", arguments));
        return pos;
    }

    // ENV numéro [, pas, taille, pause | , =forme, période] (5 parties au plus) ;
    // ENT numéro [, pas, taille, pause | , =période, pause] (numéro négatif : l'enveloppe se répète). Lot G1 (D071).
    static int ParseEnvelope(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        bool tone = name.Text == "ENT";
        string signature = tone
            ? "ENT numéro, pas, taille, pause [, ...] (ou =période, pause), 5 parties au plus"
            : "ENV numéro, pas, taille, pause [, ...] (ou =forme, période), 5 parties au plus";
        pos++;

        Expression? number = ParseExpression(tokens, ref pos, diagnostics);
        if (number is null)
            return SkipToStatementEnd(tokens, pos);
        if (number is NumberExpression n && (tone ? n.Value is < -15 or > 15 or 0 : n.Value is < 1 or > 15))
            diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line,
                                           $"{n.Value}|{(tone ? "-15 à 15, sauf 0" : "1 à 15")}"));

        var sections = new List<EnvelopeSection>();
        while (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            bool absolute = tokens[pos].Kind == TokenKind.Equals;
            if (absolute)
                pos++;
            var values = new List<Expression>();
            for (int k = 0; k < (absolute ? 2 : 3); k++)
            {
                if (k > 0)
                {
                    if (tokens[pos].Kind != TokenKind.Comma)
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, signature));
                        return SkipToStatementEnd(tokens, pos);
                    }
                    pos++;
                }
                Expression? value = ParseExpression(tokens, ref pos, diagnostics);
                if (value is null)
                    return SkipToStatementEnd(tokens, pos);
                values.Add(value);
            }

            // Valeurs permises (écrites en nombres) : pas 0 à 127 (ENV) ou 239 (ENT), taille -128 à 127, pause 0 à 255 ;
            // =forme 0 à 15, =période 0 à 65535 (ENV) ; =période 0 à 4095, pause 0 à 255 (ENT)
            int[] min = absolute ? (tone ? new[] { 0, 0 } : new[] { 0, -32768 }) : new[] { 0, -128, 0 };
            int[] max = absolute ? (tone ? new[] { 4095, 255 } : new[] { 15, 32767 }) : new[] { tone ? 239 : 127, 127, 255 };
            for (int k = 0; k < values.Count; k++)
            {
                if (values[k] is NumberExpression v && (v.Value < min[k] || v.Value > max[k]))
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line, $"{v.Value}|{min[k]} à {max[k]}"));
            }

            sections.Add(new EnvelopeSection(absolute, values[0], values[1], absolute ? null : values[2]));
            if (sections.Count > 5)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, signature));
                return SkipToStatementEnd(tokens, pos);
            }
        }

        if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.UnexpectedToken, tokens[pos].Line, tokens[pos].Text));
            return SkipToStatementEnd(tokens, pos);
        }

        statements.Add(new EnvelopeStatement(name.Line, tone, number, sections));
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

    // ELLIPSE x, y, rayon x, rayon y [, [stylo] [, [remplissage] [, angle]]] (D139) : c'est la place qui compte, et une
    // place peut rester vide (deux virgules), comme MOVE x, y, , mode : ELLIPSE 160, 100, 50, 20, , , 30.
    const string EllipseSignature = "ELLIPSE x, y, rayon x (0 à 1000), rayon y (0 à 1000) [, [stylo] [, [remplissage] [, angle]]]";

    static int ParseEllipse(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;

        var values = new Expression[4];
        for (int i = 0; i < 4; i++)
        {
            if (i > 0)
            {
                if (tokens[pos].Kind != TokenKind.Comma)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, EllipseSignature));
                    return SkipToStatementEnd(tokens, pos);
                }
                pos++;
            }
            Expression? value = ParseExpression(tokens, ref pos, diagnostics);
            if (value is null)
                return SkipToStatementEnd(tokens, pos);
            values[i] = value;
        }

        var options = new Expression?[3];                       // stylo, remplissage, angle
        for (int i = 0; i < 3 && tokens[pos].Kind == TokenKind.Comma; i++)
        {
            pos++;
            if (tokens[pos].Kind == TokenKind.Comma)
                continue;                                       // place vide
            if (IsStatementEnd(tokens[pos]) || IsElse(tokens[pos]))
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, EllipseSignature));
                return SkipToStatementEnd(tokens, pos);
            }
            options[i] = ParseExpression(tokens, ref pos, diagnostics);
            if (options[i] is null)
                return SkipToStatementEnd(tokens, pos);
        }
        if (!IsStatementEnd(tokens[pos]) && !IsElse(tokens[pos]))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.WrongArgumentCount, name.Line, EllipseSignature));
            return SkipToStatementEnd(tokens, pos);
        }

        CheckConstantRange(values[2], 0, 1000, name.Line, diagnostics);
        CheckConstantRange(values[3], 0, 1000, name.Line, diagnostics);
        CheckConstantRange(options[0], 0, 15, name.Line, diagnostics);
        CheckConstantRange(options[1], 0, 15, name.Line, diagnostics);
        statements.Add(new EllipseStatement(name.Line, values[0], values[1], values[2], values[3], options[0], options[1], options[2]));
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
        if (!VariableNames.IsSupported(name.Text) && !VariableNames.IsReal(name.Text))
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

        if (VariableNames.IsReal(name.Text))
        {
            // FOR X! = départ TO limite [STEP pas] : limite et pas peuvent être à virgule (D068)
            Expression realStart = new NumberExpression(0);
            double realLimit = 0, realStep = 1;
            Expression? realLimitValue = null, realStepValue = null;
            bool realOk = ParseRealForHeader(tokens, ref pos, diagnostics, ref realStart, ref realLimit, ref realStep,
                                             ref realLimitValue, ref realStepValue);
            statements.Add(new ForStatement(line, name.Text, realStart, 0, Math.Sign(realStep), realLimit, realStep,
                                            realLimitValue, realStepValue));
            return realOk ? pos : SkipToStatementEnd(tokens, pos);
        }

        Expression start = new NumberExpression(0);
        int limit = 0;
        int step = 1;
        Expression? limitValue = null, stepValue = null;
        bool ok = ParseForHeader(tokens, ref pos, diagnostics, ref start, ref limit, ref step, ref limitValue, ref stepValue);

        statements.Add(new ForStatement(line, name.Text, start, limit, step, LimitValue: limitValue, StepValue: stepValue));
        return ok ? pos : SkipToStatementEnd(tokens, pos);
    }

    static bool ParseRealForHeader(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                                   ref Expression start, ref double limit, ref double step,
                                   ref Expression? limitValue, ref Expression? stepValue)
    {
        if (tokens[pos].Kind != TokenKind.Equals)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedEquals, tokens[pos].Line, tokens[pos].Text));
            return false;
        }
        pos++;

        Token at = tokens[pos];
        Expression? first = ParseAnyExpression(tokens, ref pos, diagnostics);
        if (first is null)
            return false;
        if (Types.IsString(first))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, at.Line, "un nombre est attendu"));
            return false;
        }
        start = first;

        if (tokens[pos].Kind != TokenKind.Word || tokens[pos].Text != "TO")
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedTo, tokens[pos].Line, tokens[pos].Text));
            return false;
        }
        pos++;

        // Limite et pas : nombres écrits, ou valeurs calculées une fois au début de la boucle (D078)
        Expression? lastExpression = ParseExpressionKeepingReals(tokens, ref pos, diagnostics);
        if (lastExpression is null)
            return false;
        if (ConstantReal(lastExpression) is double lastConstant)
            limit = lastConstant;
        else
            limitValue = lastExpression;

        if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "STEP")
        {
            pos++;
            Token stepToken = tokens[pos];
            Expression? stepExpression = ParseExpressionKeepingReals(tokens, ref pos, diagnostics);
            if (stepExpression is null)
                return false;
            if (ConstantReal(stepExpression) is double stepConstant)
            {
                if (stepConstant == 0)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.StepIsZero, stepToken.Line));
                    return false;
                }
                step = stepConstant;
            }
            else
            {
                stepValue = stepExpression;
            }
        }
        return true;
    }

    // Un nombre (pas un texte), sans arrondir les nombres à virgule écrits (FOR X! = 0 TO 1 STEP 0.25)
    static Expression? ParseExpressionKeepingReals(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics)
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

    // Valeur d'un nombre écrit (5, -5, 0.25, -0.25), sinon null (D078)
    static double? ConstantReal(Expression value) => value switch
    {
        NumberExpression number => number.Value,
        RealNumberExpression real => real.Value,
        BinaryExpression { Operator: BinaryOperator.Subtract, Left: NumberExpression { Value: 0 } } minus
            => ConstantReal(minus.Right) is double inner ? -inner : null,
        _ => null
    };

    static int? ConstantInt(Expression value) => value switch
    {
        NumberExpression number => number.Value,
        BinaryExpression { Operator: BinaryOperator.Subtract, Left: NumberExpression { Value: 0 }, Right: NumberExpression right }
            => -right.Value,
        _ => null
    };

    static bool ParseForHeader(IReadOnlyList<Token> tokens, ref int pos, List<Diagnostic> diagnostics,
                               ref Expression start, ref int limit, ref int step,
                               ref Expression? limitValue, ref Expression? stepValue)
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

        // Limite et pas : nombres écrits, ou valeurs calculées une fois au début de la boucle (D078)
        Expression? lastExpression = ParseExpression(tokens, ref pos, diagnostics);
        if (lastExpression is null)
            return false;
        if (ConstantInt(lastExpression) is int lastConstant)
            limit = lastConstant;
        else
            limitValue = lastExpression;

        if (tokens[pos].Kind == TokenKind.Word && tokens[pos].Text == "STEP")
        {
            pos++;
            Token stepToken = tokens[pos];
            Expression? stepExpression = ParseExpression(tokens, ref pos, diagnostics);
            if (stepExpression is null)
                return false;
            if (ConstantInt(stepExpression) is int stepConstant)
            {
                if (stepConstant == 0)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.StepIsZero, stepToken.Line));
                    return false;
                }
                step = stepConstant;
            }
            else
            {
                stepValue = stepExpression;
            }
        }

        return true;
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

    // AFTER durée [, minuterie] GOSUB ligne | EVERY durée [, minuterie] GOSUB ligne (lot I1, D079)
    static int ParseTimer(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        Token name = tokens[pos];
        pos++;
        Expression? time = ParseExpression(tokens, ref pos, diagnostics);
        if (time is null)
            return SkipToStatementEnd(tokens, pos);
        Expression timer = new NumberExpression(0);
        if (tokens[pos].Kind == TokenKind.Comma)
        {
            pos++;
            Expression? given = ParseExpression(tokens, ref pos, diagnostics);
            if (given is null)
                return SkipToStatementEnd(tokens, pos);
            if (given is NumberExpression number && (number.Value < 0 || number.Value > 3))
                diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, name.Line, $"{number.Value}|0 à 3", name.Text));
            timer = given;
        }
        if (IsWord(tokens[pos], "GOSUB"))
            pos++;
        else if (IsWord(tokens[pos], "GO") && IsWord(tokens[pos + 1], "SUB"))
            pos += 2;
        else
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, name.Line, tokens[pos].Text, name.Text));
            return SkipToStatementEnd(tokens, pos);
        }
        if (!TryParseTarget(tokens, ref pos, diagnostics, out int target))
            return SkipToStatementEnd(tokens, pos);

        statements.Add(new TimerStatement(name.Line, name.Text == "EVERY", time, timer, target));
        return pos;
    }

    // ON valeur GOTO ligne1, ligne2... | ON valeur GOSUB ligne1, ligne2...
    static int ParseOn(IReadOnlyList<Token> tokens, int pos,
                       List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;

        // ON ERROR GOTO ligne | ON ERROR GOTO 0 (lot I2, D080)
        if (IsWord(tokens[pos + 1], "ERROR"))
        {
            pos += 2;
            if (IsWord(tokens[pos], "GOTO"))
                pos++;
            else if (IsWord(tokens[pos], "GO") && IsWord(tokens[pos + 1], "TO"))
                pos += 2;
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, line, tokens[pos].Text, "ON ERROR"));
                return SkipToStatementEnd(tokens, pos);
            }
            if (tokens[pos].Kind == TokenKind.Number && tokens[pos].Text == "0")
            {
                statements.Add(new OnErrorStatement(line, 0));
                return pos + 1;
            }
            if (!TryParseTarget(tokens, ref pos, diagnostics, out int handler))
                return SkipToStatementEnd(tokens, pos);
            statements.Add(new OnErrorStatement(line, handler));
            return pos;
        }

        // ON SQ(canal) GOSUB ligne (lot I3a, D082)
        if (IsWord(tokens[pos + 1], "SQ"))
        {
            pos += 2;
            if (tokens[pos].Kind != TokenKind.LeftParen)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, line, tokens[pos].Text, "ON SQ"));
                return SkipToStatementEnd(tokens, pos);
            }
            pos++;
            Expression? channel = ParseExpression(tokens, ref pos, diagnostics);
            if (channel is null)
                return SkipToStatementEnd(tokens, pos);
            if (tokens[pos].Kind != TokenKind.RightParen)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, line, tokens[pos].Text, "ON SQ"));
                return SkipToStatementEnd(tokens, pos);
            }
            pos++;
            if (channel is NumberExpression number && number.Value is not (1 or 2 or 4))
                diagnostics.Add(new Diagnostic(DiagnosticCode.ValueOutOfRange, line, $"{number.Value}|1, 2 ou 4", "ON SQ"));
            if (IsWord(tokens[pos], "GOSUB"))
                pos++;
            else if (IsWord(tokens[pos], "GO") && IsWord(tokens[pos + 1], "SUB"))
                pos += 2;
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, line, tokens[pos].Text, "ON SQ"));
                return SkipToStatementEnd(tokens, pos);
            }
            if (!TryParseTarget(tokens, ref pos, diagnostics, out int sqTarget))
                return SkipToStatementEnd(tokens, pos);
            statements.Add(new OnSqStatement(line, channel, sqTarget));
            return pos;
        }

        // ON BREAK CONT | ON BREAK STOP | ON BREAK GOSUB ligne (lot I3b, D083)
        if (IsWord(tokens[pos + 1], "BREAK"))
        {
            pos += 2;
            if (IsWord(tokens[pos], "CONT") || IsWord(tokens[pos], "STOP"))
            {
                statements.Add(new OnBreakStatement(line, tokens[pos].Text == "CONT" ? BreakMode.Cont : BreakMode.Stop));
                return pos + 1;
            }
            if (IsWord(tokens[pos], "GOSUB"))
                pos++;
            else if (IsWord(tokens[pos], "GO") && IsWord(tokens[pos + 1], "SUB"))
                pos += 2;
            else
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedGoto, line, tokens[pos].Text, "ON BREAK"));
                return SkipToStatementEnd(tokens, pos);
            }
            if (!TryParseTarget(tokens, ref pos, diagnostics, out int breakTarget))
                return SkipToStatementEnd(tokens, pos);
            statements.Add(new OnBreakStatement(line, BreakMode.Gosub, breakTarget));
            return pos;
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
                case TimerStatement timer:
                    Check(timer.SourceLine, timer.Target, timer.Every ? "EVERY" : "AFTER");
                    break;
                case OnSqStatement onSq:
                    Check(onSq.SourceLine, onSq.Target, "ON SQ");
                    break;
                case RestartStatement { Target: int restartLine } restart:
                    Check(restart.SourceLine, restartLine, "RUN");
                    break;
                case OnBreakStatement { Mode: BreakMode.Gosub } onBreak:
                    Check(onBreak.SourceLine, onBreak.Target, "ON BREAK GOSUB");
                    break;
                case OnErrorStatement { Target: > 0 } onError:
                    Check(onError.SourceLine, onError.Target, "ON ERROR GOTO");
                    break;
                case ResumeStatement { Kind: ResumeKind.Line } resume:
                    Check(resume.SourceLine, resume.Target, "RESUME");
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

        for (int index = 0; index < statements.Count; index++)
        {
            Statement statement = statements[index];
            switch (statement)
            {
                case ForStatement or IfStatement or WhileStatement or RepeatStatement or DoStatement:
                    open.Push(new OpenBlock(statement));
                    break;

                case SelectStatement:                   // D206 : le premier CASE doit suivre SELECT
                    open.Push(new OpenBlock(statement));
                    if (index + 1 < statements.Count && statements[index + 1] is not (CaseStatement or CaseElseStatement
                                                                                       or EndSelectStatement))
                        diagnostics.Add(new Diagnostic(DiagnosticCode.CaseExpected, statements[index + 1].SourceLine));
                    break;

                case CaseStatement or CaseElseStatement:
                {
                    string word = statement is CaseElseStatement ? "CASE ELSE" : "CASE";
                    if (open.Count == 0 || !open.Any(b => b.Node is SelectStatement))
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.CaseWithoutSelect, statement.SourceLine, word));
                        break;
                    }
                    OpenBlock current = open.Peek();
                    if (current.Node is not SelectStatement)
                        diagnostics.Add(new Diagnostic(DiagnosticCode.BlocksNotNested, statement.SourceLine, word));
                    else if (current.ElseSeen)
                        diagnostics.Add(new Diagnostic(DiagnosticCode.CaseAfterCaseElse, statement.SourceLine, word));
                    else if (statement is CaseElseStatement)
                        current.ElseSeen = true;
                    if (statement is CaseStatement cs && current.Node is SelectStatement sel)
                    {
                        bool text = Types.IsString(sel.Value);
                        foreach (CaseItem item in cs.Items)
                            foreach (Expression e in new[] { item.Value, item.Upper }.OfType<Expression>())
                                if (Types.IsString(e) != text)
                                    diagnostics.Add(new Diagnostic(DiagnosticCode.TypeMismatch, statement.SourceLine,
                                                                   text ? "SELECT d'un texte, CASE d'un nombre" : "SELECT d'un nombre, CASE d'un texte",
                                                                   "CASE"));
                    }
                    break;
                }

                case EndSelectStatement:
                    Close(statement.SourceLine, "END SELECT", DiagnosticCode.EndSelectWithoutSelect, n => n is SelectStatement);
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
                case SelectStatement:
                    diagnostics.Add(new Diagnostic(DiagnosticCode.SelectWithoutEndSelect, block.Node.SourceLine));
                    break;
                default:
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnclosedLoop, block.Node.SourceLine, KeywordOf(block.Node)));
                    break;
            }
        }
    }

    // CASE (D206) : CASE ELSE, ou des éléments séparés par des virgules : valeur, valeur TO valeur, ou comparaison
    // (> valeur, <= valeur, <> valeur...)
    static int ParseCase(IReadOnlyList<Token> tokens, int pos, List<Statement> statements, List<Diagnostic> diagnostics)
    {
        int line = tokens[pos].Line;
        pos++;
        if (IsElse(tokens[pos]))
        {
            statements.Add(new CaseElseStatement(line));
            return pos + 1;
        }
        var items = new List<CaseItem>();
        while (true)
        {
            CompareOperator? op = CompareOf(tokens[pos]);
            if (op is not null)
                pos++;
            Expression? value = IsStatementEnd(tokens[pos]) ? null : ParseAnyExpression(tokens, ref pos, diagnostics);
            if (value is null)
            {
                diagnostics.Add(new Diagnostic(DiagnosticCode.SelectSyntax, line, "CASE"));
                return SkipToStatementEnd(tokens, pos);
            }
            Expression? upper = null;
            if (op is null && IsWord(tokens[pos], "TO"))
            {
                pos++;
                upper = IsStatementEnd(tokens[pos]) ? null : ParseAnyExpression(tokens, ref pos, diagnostics);
                if (upper is null)
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.SelectSyntax, line, "CASE"));
                    return SkipToStatementEnd(tokens, pos);
                }
            }
            items.Add(new CaseItem(value, upper, op));
            if (tokens[pos].Kind != TokenKind.Comma)
                break;
            pos++;
        }
        if (!IsStatementEnd(tokens[pos]))
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.SelectSyntax, line, "CASE"));
            return SkipToStatementEnd(tokens, pos);
        }
        statements.Add(new CaseStatement(line, items));
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

                case DimStatement or ArrayAssignStatement or MidAssignStatement:
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

                case ErrorStatement error:
                    Uses(error.Code);
                    break;

                case OnSqStatement onSq:
                    Uses(onSq.Channel);
                    break;

                case FileStatement { Name: not null } file:
                    Uses(file.Name);
                    break;

                case SaveStatement save:
                    Uses(save.Name);
                    Uses(save.Start);
                    Uses(save.Length);
                    if (save.Entry is not null)
                        Uses(save.Entry);
                    break;

                case LoadStatement load:
                    Uses(load.Name);
                    if (load.Address is not null)
                        Uses(load.Address);
                    break;

                case SpeedWriteStatement speedWrite:
                    Uses(speedWrite.Value);
                    break;

                case RunFileStatement runFile:
                    Uses(runFile.Name);
                    break;

                case SpeedStatement speedChange:
                    Uses(speedChange.First);
                    Uses(speedChange.Second);
                    break;

                case KeyStatement keyText:
                    Uses(keyText.Number);
                    Uses(keyText.Text);
                    break;

                case KeyDefStatement keyDef:
                    foreach (Expression value in keyDef.Values)
                        Uses(value);
                    break;

                case ExternalCallStatement external:
                    if (external.Address is not null)
                        Uses(external.Address);
                    foreach (Expression argument in external.Arguments)
                        Uses(argument);
                    break;

                case TimerStatement timer:
                    Uses(timer.Time);
                    Uses(timer.Timer);
                    break;

                case ForStatement loop:
                    Uses(loop.Start);
                    if (loop.LimitValue is not null)
                        Uses(loop.LimitValue);
                    if (loop.StepValue is not null)
                        Uses(loop.StepValue);
                    used.Add(loop.Variable);
                    if (bytes.Contains(loop.Variable))
                    {
                        if (loop.Start is NumberExpression first && (first.Value < 0 || first.Value > 255))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, first.Value.ToString()));
                        if (loop.LimitValue is null && (loop.Limit < 0 || loop.Limit > 255))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, loop.Limit.ToString()));
                        if (loop.StepValue is null && (loop.Step < -255 || loop.Step > 255))
                            diagnostics.Add(new Diagnostic(DiagnosticCode.ByteOutOfRange, loop.SourceLine, loop.Step.ToString()));
                    }
                    break;

                case PrintValueStatement print:
                    Uses(print.Value);
                    break;

                case OnStatement on:
                    Uses(on.Selector);
                    break;

                case LineInputStatement lineInput:
                    used.Add(lineInput.Variable);
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
        return value is null ? null : IntegerConstant(value, at, diagnostics);
    }

    // Un nombre à virgule écrit dans le programme, là où il faut un entier (BOX 2.6, A% = 40000) : arrondi
    // comme le Locomotive (,5 s'éloigne de zéro) ; hors de -32768 à 32767, erreur dès la compilation (D068).
    static Expression? IntegerConstant(Expression value, Token at, List<Diagnostic> diagnostics)
    {
        if (value is not RealNumberExpression real)
            return value;
        double rounded = Math.Round(real.Value, MidpointRounding.AwayFromZero);
        if (rounded < -32768 || rounded > 32767)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, at.Line,
                                           real.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            return null;
        }
        return new NumberExpression((int)rounded);
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

            // Réglage F8 : « / » divise à virgule même entre deux entiers (7/2 = 3.5), comme le Locomotive (D066).
            // Le côté gauche est rendu à virgule (un nombre écrit le devient directement, sinon + 0.0).
            if (op == BinaryOperator.Divide && realDivision && !Types.IsReal(left) && !Types.IsReal(right))
                left = left is NumberExpression whole ? new RealNumberExpression(whole.Value)
                    : new BinaryExpression(BinaryOperator.Add, left, new RealNumberExpression(0));

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
                if (tokens[pos].Kind == TokenKind.Word && VariableNames.IsReal(tokens[pos].Text)
                    && tokens[pos + 1].Kind != TokenKind.LeftParen)
                {
                    pos++;                              // @A! : adresse des 5 octets du nombre (D068)
                    return new AddressOfExpression(new VariableExpression(tokens[pos - 1].Text));
                }
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
                if (t.Text == "INKEY$")
                {
                    pos++;                              // INKEY$ s'écrit sans parenthèses : touche tapée, ou "" (D064)
                    return new CallExpression("INKEY$", []);
                }
                if (t.Text == "PI" || (t.Text == "RND" && tokens[pos + 1].Kind != TokenKind.LeftParen))
                {
                    pos++;                              // PI et RND s'écrivent sans parenthèses (D069)
                    return new CallExpression(t.Text, []);
                }
                if (t.Text == "TIME" && realDivision)
                {
                    pos++;                              // réglage F8 : TIME complet, à virgule, comme le Locomotive (D097)
                    return new CallExpression("TIME!", []);
                }
                if (t.Text is "TIME" or "ERR" or "ERL" or "EOF" or "XPOS" or "YPOS" or "HIMEM" or "DERR")
                {
                    pos++;                              // TIME, ERR, ERL et EOF s'écrivent sans parenthèses, comme sur le CPC (D080, D084)
                    return new CallExpression(t.Text, []);
                }
                if (t.Text == "FN")
                    return ParseUserFunctionCall(tokens, ref pos, diagnostics);   // D095
                if (Functions.TryGetValue(t.Text, out FunctionInfo? function))
                {
                    Expression? called = ParseFunctionCall(tokens, ref pos, diagnostics, function);
                    // DEC$ avec un format écrit : vérifié ici, comme PRINT USING (D103)
                    if (called is CallExpression { Function: "DEC$", Arguments: [_, StringLiteralExpression format] }
                        && DecField(format.Text) is null)
                        diagnostics.Add(new Diagnostic(DiagnosticCode.DecFormat, t.Line, format.Text));
                    return called;
                }
                if (libraries?.Find(t.Text) is { Kind: LibraryKind.Function } libraryFunction)
                    return ParseLibraryFunction(tokens, ref pos, libraryFunction, diagnostics);   // D148
                if (Commands.Contains(t.Text))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedExpression, t.Line, t.Text));
                    return null;
                }
                if (!VariableNames.IsSupported(t.Text)
                    && (!VariableNames.IsReal(t.Text) || tokens[pos + 1].Kind == TokenKind.LeftParen))
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
                if (fnRename is not null && fnRename.TryGetValue(t.Text, out string? hiddenName))
                    return new VariableExpression(hiddenName);              // valeur de DEF FN (D095)
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
                if (function.StreamFirst && arguments.Count == 0)
                {
                    if (tokens[pos].Kind != TokenKind.Hash)   // POS(#0) : le « # » est obligatoire (D072)
                    {
                        diagnostics.Add(new Diagnostic(DiagnosticCode.ExpectedStream, nameToken.Line, tokens[pos].Text, nameToken.Text));
                        return null;
                    }
                    pos++;
                }
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

        if (function.StreamFirst)
            CheckStreamNumber(arguments[0], nameToken.Line, nameToken.Text, diagnostics);

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
        bool decimals = t.Text.Contains('.') || t.Text.Contains('E');

        if (!decimals && long.TryParse(t.Text, out long value) && value <= limit)
        {
            pos++;
            return new NumberExpression((int)(negative ? -value : value));
        }

        // 3.14, 1E5 ou un nombre trop grand pour un entier (40000) : nombre à virgule, comme le Locomotive (D068)
        if (!double.TryParse(t.Text, System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out double real)
            || CpcBasic.Core.Codegen.LocoReal.Encode(real) is null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCode.NumberOutOfRange, t.Line, (negative ? "-" : "") + t.Text));
            return null;
        }

        pos++;
        return new RealNumberExpression(negative ? -real : real);
    }

    // Avance jusqu'au prochain « : » ou à la fin de la ligne
    static int SkipToStatementEnd(IReadOnlyList<Token> tokens, int pos)
    {
        while (!IsStatementEnd(tokens[pos]))
            pos++;
        return pos;
    }
}
