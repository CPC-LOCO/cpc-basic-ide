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
    DeclaredAfterUse
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
        _ => $"Ligne {Line} : problème inconnu ({Code})."
    };

    public override string ToString() => Message;
}
