// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>Nature d'un problème détecté dans le programme BASIC.</summary>
public enum DiagnosticCode
{
    UnexpectedCharacter,
    UnterminatedString,
    UnknownCommand,
    ExpectedString,          // plus produit depuis J2 (PRINT accepte les expressions)
    UnexpectedToken,
    ExpectedExpression,
    ExpectedEquals,
    ExpectedClosingParenthesis,
    NumberOutOfRange,
    ByteOutOfRange,
    UnsupportedVariableType,
    ExpectedVariableName,
    SuffixNotAllowedInDeclaration,
    AlreadyDeclared,
    DeclaredAfterUse,
    ExpectedTo,
    ExpectedNumber,
    StepIsZero,
    NextWithoutFor,
    NextNameMismatch,
    ForWithoutNext,
    LoopControlWithoutLoop,
    ExpectedThen,
    ExpectedComparison,
    ElseWithoutIf,
    DuplicateElse,
    EndIfWithoutIf,
    IfWithoutEndIf,
    BlocksNotNested
}

/// <summary>
/// Un problème trouvé dans le programme : un code, la ligne de l'éditeur (à partir de 1)
/// et l'élément en cause. Le texte du message est provisoirement écrit en français ici ;
/// il rejoindra les fichiers de langue (français, anglais, espagnol) plus tard.
/// </summary>
public sealed record Diagnostic(DiagnosticCode Code, int Line, string Argument = "")
{
    public string Message => Code switch
    {
        DiagnosticCode.UnexpectedCharacter =>
            $"Ligne {Line} : caractère inattendu « {Argument} ». Ce signe n'est pas utilisé dans ce langage.",
        DiagnosticCode.UnterminatedString =>
            $"Ligne {Line} : il manque le guillemet de fin du texte. Ajoutez un guillemet à la fin du texte.",
        DiagnosticCode.UnknownCommand =>
            $"Ligne {Line} : commande inconnue « {Argument} ». Vérifiez l'orthographe ou cherchez-la dans la bible.",
        DiagnosticCode.ExpectedString =>
            $"Ligne {Line} : PRINT attend un texte entre guillemets, mais a trouvé « {Argument} ».",
        DiagnosticCode.UnexpectedToken =>
            $"Ligne {Line} : « {Argument} » n'est pas attendu ici. Séparez les instructions par « : ».",
        DiagnosticCode.ExpectedExpression =>
            $"Ligne {Line} : une valeur est attendue (un nombre, une variable ou un calcul), mais « {Argument} » a été trouvé.",
        DiagnosticCode.ExpectedEquals =>
            $"Ligne {Line} : le signe « = » est attendu après le nom de la variable, mais « {Argument} » a été trouvé.",
        DiagnosticCode.ExpectedClosingParenthesis =>
            $"Ligne {Line} : il manque une parenthèse fermante « ) ».",
        DiagnosticCode.NumberOutOfRange =>
            $"Ligne {Line} : le nombre {Argument} est hors limites. Un entier va de -32768 à 32767.",
        DiagnosticCode.ByteOutOfRange =>
            $"Ligne {Line} : {Argument} ne tient pas dans une variable BYTE, qui va de 0 à 255.",
        DiagnosticCode.UnsupportedVariableType =>
            $"Ligne {Line} : le type de « {Argument} » n'existe pas encore. Seuls les entiers (A ou A%) et les variables BYTE sont gérés.",
        DiagnosticCode.ExpectedVariableName =>
            $"Ligne {Line} : un nom de variable est attendu après BYTE (ou après une virgule), mais « {Argument} » a été trouvé.",
        DiagnosticCode.SuffixNotAllowedInDeclaration =>
            $"Ligne {Line} : « {Argument} » : une variable BYTE se déclare sans suffixe. Écrivez simplement le nom.",
        DiagnosticCode.AlreadyDeclared =>
            $"Ligne {Line} : la variable « {Argument} » est déjà déclarée BYTE.",
        DiagnosticCode.DeclaredAfterUse =>
            $"Ligne {Line} : « {Argument} » est déjà utilisée plus haut comme entier. Placez BYTE {Argument} avant sa première utilisation.",
        DiagnosticCode.ExpectedTo =>
            $"Ligne {Line} : « TO » est attendu après la valeur de départ de FOR, mais « {Argument} » a été trouvé.",
        DiagnosticCode.ExpectedNumber =>
            $"Ligne {Line} : la limite et le pas de FOR doivent être des nombres écrits dans le programme (pas des variables), mais « {Argument} » a été trouvé.",
        DiagnosticCode.StepIsZero =>
            $"Ligne {Line} : le pas de FOR ne peut pas être 0, la boucle ne finirait jamais.",
        DiagnosticCode.NextWithoutFor =>
            $"Ligne {Line} : NEXT sans FOR correspondant. Ajoutez un FOR avant, ou retirez ce NEXT.",
        DiagnosticCode.NextNameMismatch =>
            $"Ligne {Line} : ce NEXT ne correspond pas à la boucle ouverte, dont la variable est « {Argument} ».",
        DiagnosticCode.ForWithoutNext =>
            $"Ligne {Line} : la boucle FOR {Argument} n'est jamais refermée. Ajoutez un NEXT.",
        DiagnosticCode.LoopControlWithoutLoop =>
            $"Ligne {Line} : {Argument} est utilisé en dehors d'une boucle qu'il pourrait quitter ou poursuivre.",
        DiagnosticCode.ExpectedThen =>
            $"Ligne {Line} : « THEN » est attendu après la condition de IF, mais « {Argument} » a été trouvé.",
        DiagnosticCode.ExpectedComparison =>
            $"Ligne {Line} : une comparaison est attendue (=, <>, <, >, <=, >=), mais « {Argument} » a été trouvé. Une condition compare deux valeurs, par exemple A > 5.",
        DiagnosticCode.ElseWithoutIf =>
            $"Ligne {Line} : ELSE sans IF correspondant.",
        DiagnosticCode.DuplicateElse =>
            $"Ligne {Line} : ce IF a déjà un ELSE.",
        DiagnosticCode.EndIfWithoutIf =>
            $"Ligne {Line} : END IF sans IF correspondant.",
        DiagnosticCode.IfWithoutEndIf =>
            $"Ligne {Line} : ce IF sur plusieurs lignes n'est jamais refermé. Ajoutez END IF.",
        DiagnosticCode.BlocksNotNested =>
            $"Ligne {Line} : « {Argument} » arrive alors qu'un autre bloc (IF ou FOR) est encore ouvert. Refermez d'abord le bloc le plus récent.",
        _ => $"Ligne {Line} : problème inconnu ({Code})."
    };

    public override string ToString() => Message;
}
