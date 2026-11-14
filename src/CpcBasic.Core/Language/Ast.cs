// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>Types de variables de J2 (décisions D026 et D033).</summary>
public enum VarType
{
    /// <summary>Entier signé 16 bits : A ou A%. Type par défaut, sans déclaration.</summary>
    Integer,
    /// <summary>Octet non signé 8 bits : variable déclarée avec BYTE.</summary>
    Byte,
    /// <summary>Texte : A$ (lot E, D062). Fiche de 3 octets comme le Locomotive : longueur, puis adresse des lettres.</summary>
    String
}

public static class VariableNames
{
    /// <summary>Le suffixe ! (réel) n'existe pas encore ; $ (texte) existe depuis le lot E (D062).</summary>
    public static bool IsSupported(string name) => name[^1] is not '!';

    /// <summary>Vrai pour une variable texte (A$).</summary>
    public static bool IsString(string name) => name[^1] == '$';

    /// <summary>Vrai si le nom se termine par un suffixe de type ($, % ou !).</summary>
    public static bool HasSuffix(string name) => name[^1] is '$' or '%' or '!';
}

/// <summary>Une instruction du programme. SourceLine est la ligne de l'éditeur (à partir de 1).</summary>
public abstract record Statement(int SourceLine);

/// <summary>PRINT "texte" : affiche un texte. NewLine est faux si l'instruction se termine par « ; ».</summary>
public sealed record PrintStatement(int SourceLine, string Text, bool NewLine) : Statement(SourceLine);

/// <summary>PRINT expression : affiche un nombre. NewLine est faux s'il est suivi de « ; ».</summary>
public sealed record PrintValueStatement(int SourceLine, Expression Value, bool NewLine) : Statement(SourceLine);

/// <summary>A = expression (avec ou sans LET). Variable : nom en majuscules, suffixe compris.</summary>
public sealed record AssignStatement(int SourceLine, string Variable, Expression Value) : Statement(SourceLine);

/// <summary>BYTE a, b : déclare des variables octet (0 à 255). Ne produit aucun code.</summary>
public sealed record DeclareStatement(int SourceLine, IReadOnlyList<string> Names) : Statement(SourceLine);

/// <summary>
/// FOR variable = départ TO limite [STEP pas]. La limite et le pas sont des nombres écrits dans
/// le programme (décision D033). Le corps de la boucle se termine au NEXT correspondant.
/// </summary>
public sealed record ForStatement(int SourceLine, string Variable, Expression Start, int Limit, int Step)
    : Statement(SourceLine);

/// <summary>NEXT [variable] : referme la boucle FOR ouverte la plus récente.</summary>
public sealed record NextStatement(int SourceLine, string? Variable) : Statement(SourceLine);

/// <summary>Types de boucles.</summary>
public enum LoopKind { For, While, Repeat, Do }

/// <summary>WHILE condition ... WEND : test en haut, la boucle continue tant que la condition est vraie.</summary>
public sealed record WhileStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>WEND : referme le WHILE ouvert.</summary>
public sealed record WendStatement(int SourceLine) : Statement(SourceLine);

/// <summary>REPEAT ... UNTIL condition : test en bas, la boucle s'arrête quand la condition devient vraie.</summary>
public sealed record RepeatStatement(int SourceLine) : Statement(SourceLine);

public sealed record UntilStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>
/// DO [WHILE c | UNTIL c] ... LOOP [WHILE c | UNTIL c]. Une seule des deux condition est permise.
/// UntilForm est vrai pour UNTIL, faux pour WHILE. Condition est null s'il n'y en a pas.
/// </summary>
public sealed record DoStatement(int SourceLine, Condition? Condition, bool UntilForm) : Statement(SourceLine);

public sealed record LoopStatement(int SourceLine, Condition? Condition, bool UntilForm) : Statement(SourceLine);

/// <summary>EXIT [FOR] : sort de la boucle. Kind est null pour « EXIT » seul (boucle la plus proche).</summary>
public sealed record ExitStatement(int SourceLine, LoopKind? Kind) : Statement(SourceLine);

/// <summary>CONTINUE [FOR] : passe à l'itération suivante. Kind est null pour « CONTINUE » seul.</summary>
public sealed record ContinueStatement(int SourceLine, LoopKind? Kind) : Statement(SourceLine);

/// <summary>Comparaisons possibles dans une condition.</summary>
public enum CompareOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }

/// <summary>Une condition : une comparaison, ou plusieurs reliées par AND / OR (AND passe avant OR).</summary>
public abstract record Condition;

public sealed record ComparisonCondition(CompareOperator Operator, Expression Left, Expression Right) : Condition;

public sealed record AndCondition(Condition Left, Condition Right) : Condition;

public sealed record OrCondition(Condition Left, Condition Right) : Condition;

/// <summary>
/// IF condition THEN ... : début d'un test. Sur une ligne (IF c THEN a ELSE b), le parser
/// ajoute lui-même le EndIfStatement à la fin de la ligne ; en forme bloc, il vient de END IF.
/// </summary>
public sealed record IfStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>ELSE : début de la partie « sinon » du IF ouvert.</summary>
public sealed record ElseStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Fin du IF ouvert (END IF, ENDIF, ou fin de ligne pour un IF sur une ligne).</summary>
public sealed record EndIfStatement(int SourceLine) : Statement(SourceLine);

/// <summary>
/// RANDOMIZE [graine] : fixe le départ des nombres aléatoires (la même graine redonne la même suite).
/// Sans graine (Seed est null), le départ vient de l'horloge du CPC.
/// </summary>
public sealed record RandomizeStatement(int SourceLine, Expression? Seed) : Statement(SourceLine);

/// <summary>
/// Commandes d'écran : MODE, CLS, LOCATE, PEN, PAPER, INK, BORDER (décision D046), puis CLG, ORIGIN (D047) et BOX (D048).
/// Command est en majuscules ; Arguments sont les valeurs, dans l'ordre où elles sont écrites.
/// </summary>
public sealed record ScreenStatement(int SourceLine, string Command, IReadOnlyList<Expression> Arguments)
    : Statement(SourceLine);

/// <summary>
/// Tracé du Locomotive BASIC : MOVE, MOVER, PLOT, PLOTR, DRAW, DRAWR (décision D047).
/// X et Y sont des coordonnées (ou des écarts pour les commandes en « R »). Pen (le stylo) et Mode
/// (0 normal, 1 XOR, 2 AND, 3 OR) sont facultatifs : null s'ils sont omis.
/// </summary>
public sealed record DrawStatement(int SourceLine, string Command, Expression X, Expression Y,
                                   Expression? Pen, Expression? Mode) : Statement(SourceLine);

/// <summary>END : fin du programme.</summary>
public sealed record EndStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Une expression entière. Son résultat est toujours calculé sur 16 bits.</summary>
public abstract record Expression;

public sealed record NumberExpression(int Value) : Expression;

public sealed record VariableExpression(string Name) : Expression;

/// <summary>
/// Appel d'une fonction : ABS, SGN, MIN, MAX, HIBYTE, LOBYTE, CLAMP. Function est en majuscules.
/// MIN et MAX avec plus de deux valeurs sont découpés par le parser en appels à deux valeurs.
/// </summary>
public sealed record CallExpression(string Function, IReadOnlyList<Expression> Arguments) : Expression;

public enum BinaryOperator
{
    Add, Subtract, Multiply, Divide, Modulo,
    /// <summary>AND, OR, XOR : bit à bit sur 16 bits, comme le Locomotive BASIC (lot D, D060).</summary>
    And, Or, Xor,
    /// <summary>^ : puissance entière (lot D, D060).</summary>
    Power
}

/// <summary>Comparaison utilisée comme valeur : -1 si elle est vraie, 0 sinon, comme le Locomotive (A = B > 3) (lot D, D060).</summary>
public sealed record CompareExpression(CompareOperator Operator, Expression Left, Expression Right) : Expression;

/// <summary>
/// @A, @T(3) : adresse en mémoire où la variable (ou la case de tableau) est rangée, comme le Locomotive (D061).
/// Target est une VariableExpression ou une ArrayElementExpression.
/// </summary>
public sealed record AddressOfExpression(Expression Target) : Expression;

/// <summary>"BONJOUR" dans un calcul : un texte écrit dans le programme (lot E, D062).</summary>
public sealed record StringLiteralExpression(string Text) : Expression;

/// <summary>A$ + B$ : les deux textes mis bout à bout (lot E, D062).</summary>
public sealed record ConcatExpression(Expression Left, Expression Right) : Expression;

/// <summary>Le type d'une expression : texte ou nombre (lot E, D062).</summary>
public static class Types
{
    /// <summary>Vrai si l'expression donne un texte.</summary>
    public static bool IsString(Expression expression) => expression switch
    {
        StringLiteralExpression or ConcatExpression => true,
        VariableExpression variable => VariableNames.IsString(variable.Name),
        ArrayElementExpression element => VariableNames.IsString(element.Name),
        _ => false
    };
}

/// <summary>NOT valeur : inverse tous les bits (NOT 0 = -1, NOT -1 = 0), comme le Locomotive (lot D, D060).</summary>
public sealed record NotExpression(Expression Operand) : Expression;

public sealed record BinaryExpression(BinaryOperator Operator, Expression Left, Expression Right) : Expression;

/// <summary>Le programme complet : la suite de ses instructions.</summary>
/// <summary>
/// Le programme complet : la suite de ses instructions, et la table des lignes numérotées (numéro de ligne BASIC
/// -> rang de la première instruction qui la suit), utile pour GOTO, GOSUB et ON (D054).
/// </summary>
public sealed record BasicProgram(IReadOnlyList<Statement> Statements, IReadOnlyDictionary<int, int>? Lines = null)
{
    /// <summary>Les lignes numérotées du programme (vide si aucune).</summary>
    public IReadOnlyDictionary<int, int> LineTable => Lines ?? new Dictionary<int, int>();
}

/// <summary>GOTO ligne : saut à une ligne numérotée. Target est le numéro de ligne BASIC (D054).</summary>
public sealed record GotoStatement(int SourceLine, int Target) : Statement(SourceLine);

/// <summary>GOSUB ligne : appel d'un sous-programme qui commence à une ligne numérotée (D054).</summary>
public sealed record GosubStatement(int SourceLine, int Target) : Statement(SourceLine);

/// <summary>RETURN : fin d'un sous-programme appelé par GOSUB (D054).</summary>
public sealed record ReturnStatement(int SourceLine) : Statement(SourceLine);

/// <summary>STOP : arrête le programme, comme END (D054).</summary>
public sealed record StopStatement(int SourceLine) : Statement(SourceLine);

/// <summary>ON valeur GOTO / GOSUB ligne1, ligne2... : saut ou appel selon la valeur (1 = première ligne). Hors de 1 à N, on continue (D054).</summary>
public sealed record OnStatement(int SourceLine, Expression Selector, IReadOnlyList<int> Targets, bool IsGosub) : Statement(SourceLine);

/// <summary>CLEAR INPUT : vide les touches déjà tapées mais pas encore lues (lot B, D055).</summary>
public sealed record ClearInputStatement(int SourceLine) : Statement(SourceLine);

/// <summary>
/// INPUT [;] ["texte" ; ou ,] variable [, variable ...] : saisie de nombres entiers au clavier (lot B, D055).
/// Question : afficher « ? » après le texte (point-virgule, ou pas de texte). NoNewline : le « ; » juste après INPUT.
/// </summary>
public sealed record InputStatement(int SourceLine, string? Prompt, bool Question, bool NoNewline,
                                    IReadOnlyList<string> Variables,
                                    IReadOnlyList<IReadOnlyList<Expression>?>? Indexes = null) : Statement(SourceLine)
{
    /// <summary>
    /// Ce que reçoit chaque nombre tapé : une variable, ou une case de tableau (INPUT T(I), D059).
    /// Indexes[i] donne les numéros de case quand la i-ème cible est un tableau, sinon null.
    /// </summary>
    public IEnumerable<Expression> Targets => Variables.Select((name, i) =>
        Indexes?[i] is { } indexes ? new ArrayElementExpression(name, indexes) : (Expression)new VariableExpression(name));
}

/// <summary>DATA 10, -5, 300 : nombres rangés dans le programme, lus par READ (lot C, D059). Valeurs sur 16 bits.</summary>
public sealed record DataStatement(int SourceLine, IReadOnlyList<int> Values) : Statement(SourceLine);

/// <summary>
/// READ A, T(I) : lit les nombres suivants des DATA (lot C, D059). Chaque cible est une VariableExpression
/// ou une ArrayElementExpression.
/// </summary>
public sealed record ReadStatement(int SourceLine, IReadOnlyList<Expression> Targets) : Statement(SourceLine);

/// <summary>RESTORE [ligne] : la lecture repart au premier DATA (ou au premier DATA de cette ligne ou après) (D059).</summary>
public sealed record RestoreStatement(int SourceLine, int? Line) : Statement(SourceLine);

/// <summary>Un tableau déclaré par DIM : son nom et la plus grande case de chaque dimension (DIM A(10, 5)) (lot C, D058).</summary>
public sealed record ArrayDeclaration(string Name, IReadOnlyList<Expression> Sizes);

/// <summary>DIM A(10) [, B(5, 5) ...] : crée des tableaux d'entiers, cases de 0 à la valeur écrite (lot C, D058).</summary>
public sealed record DimStatement(int SourceLine, IReadOnlyList<ArrayDeclaration> Arrays) : Statement(SourceLine);

/// <summary>Une case de tableau lue dans un calcul : A(3), T(I, J) (lot C, D058). Name est le nom sans parenthèses.</summary>
public sealed record ArrayElementExpression(string Name, IReadOnlyList<Expression> Indexes) : Expression;

/// <summary>A(3) = valeur : écrit dans une case de tableau (lot C, D058).</summary>
public sealed record ArrayAssignStatement(int SourceLine, string Name, IReadOnlyList<Expression> Indexes, Expression Value)
    : Statement(SourceLine);

/// <summary>Parcourt les expressions d'une instruction (pour les vérifications sur les tableaux, D058).</summary>
public static class AstWalker
{
    /// <summary>L'expression et toutes celles qu'elle contient.</summary>
    public static IEnumerable<Expression> Flatten(Expression expression)
    {
        yield return expression;
        IEnumerable<Expression> inner = expression switch
        {
            BinaryExpression binary => [binary.Left, binary.Right],
            CallExpression call => call.Arguments,
            ArrayElementExpression element => element.Indexes,
            CompareExpression compare => [compare.Left, compare.Right],
            NotExpression negation => [negation.Operand],
            AddressOfExpression address => [address.Target],
            ConcatExpression concat => [concat.Left, concat.Right],
            _ => []
        };
        foreach (Expression child in inner)
            foreach (Expression e in Flatten(child))
                yield return e;
    }

    /// <summary>Les expressions d'une condition.</summary>
    public static IEnumerable<Expression> Of(Condition? condition) => condition switch
    {
        ComparisonCondition c => [c.Left, c.Right],
        AndCondition a => Of(a.Left).Concat(Of(a.Right)),
        OrCondition o => Of(o.Left).Concat(Of(o.Right)),
        _ => []
    };

    /// <summary>Les expressions écrites directement dans l'instruction (sans descendre dedans).</summary>
    public static IEnumerable<Expression> TopLevel(Statement statement) => statement switch
    {
        PrintValueStatement p => [p.Value],
        AssignStatement a => [a.Value],
        ArrayAssignStatement a => a.Indexes.Append(a.Value),
        DimStatement d => d.Arrays.SelectMany(x => x.Sizes),
        ForStatement f => [f.Start],
        WhileStatement w => Of(w.Condition),
        UntilStatement u => Of(u.Condition),
        DoStatement d => Of(d.Condition),
        LoopStatement l => Of(l.Condition),
        IfStatement i => Of(i.Condition),
        RandomizeStatement r when r.Seed is not null => [r.Seed],
        ScreenStatement s => s.Arguments,
        DrawStatement d => new[] { d.X, d.Y, d.Pen, d.Mode }.OfType<Expression>(),
        OnStatement o => [o.Selector],
        ReadStatement r => r.Targets,
        InputStatement i => i.Targets,
        _ => []
    };

    /// <summary>Toutes les expressions de l'instruction, y compris celles qui sont à l'intérieur d'autres.</summary>
    public static IEnumerable<Expression> All(Statement statement) => TopLevel(statement).SelectMany(Flatten);

    /// <summary>Vrai si l'instruction crée, lit ou écrit un tableau.</summary>
    public static bool UsesArrays(Statement statement) =>
        statement is DimStatement or ArrayAssignStatement || All(statement).Any(e => e is ArrayElementExpression);
}
