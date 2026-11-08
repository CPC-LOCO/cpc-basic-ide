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
    Byte
}

public static class VariableNames
{
    /// <summary>Les suffixes $ (texte) et ! (réel) n'existent pas encore.</summary>
    public static bool IsSupported(string name) => name[^1] is not ('$' or '!');

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

/// <summary>Types de boucles. DO, REPEAT et WHILE s'ajouteront à l'étape 2d.</summary>
public enum LoopKind { For }

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

/// <summary>END : fin du programme.</summary>
public sealed record EndStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Une expression entière. Son résultat est toujours calculé sur 16 bits.</summary>
public abstract record Expression;

public sealed record NumberExpression(int Value) : Expression;

public sealed record VariableExpression(string Name) : Expression;

public enum BinaryOperator { Add, Subtract }

public sealed record BinaryExpression(BinaryOperator Operator, Expression Left, Expression Right) : Expression;

/// <summary>Le programme complet : la suite de ses instructions.</summary>
public sealed record BasicProgram(IReadOnlyList<Statement> Statements);
