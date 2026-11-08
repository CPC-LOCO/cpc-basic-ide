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
