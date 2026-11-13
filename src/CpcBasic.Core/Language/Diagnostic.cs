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
    BlocksNotNested,
    ClosingWithoutOpening,
    UnclosedLoop,
    DoConditionTwice,
    DivisionByZero,
    ExpectedOpenParenthesis,
    WrongArgumentCount,
    ValueOutOfRange,
    ExpectedLineNumber,
    LineNotFound,
    DuplicateLineNumber,
    ExpectedGoto,
    ProgramTooLarge,
    ExpectedArrayName,
    ArrayDimensionMismatch,
    ByteArrayNotYet,
    NotYetAvailable
}

/// <summary>
/// Un problème trouvé dans le programme : un code, la ligne de l'éditeur (à partir de 1),
/// l'élément en cause (Argument) et la commande dans laquelle l'erreur a été trouvée (Context : PRINT, FOR...). Le texte du message est provisoirement écrit en français ici ;
/// il rejoindra les fichiers de langue (français, anglais, espagnol) plus tard.
/// </summary>
public sealed record Diagnostic(DiagnosticCode Code, int Line, string Argument = "", string Context = "")
{
    /// <summary>
    /// Fin de phrase des messages « telle chose est attendue » : « La ligne est incomplète. » quand l'erreur
    /// est à la fin de la ligne (rien à citer), sinon l'élément qui ne convient pas.
    /// </summary>
    string Why => Argument.Length == 0
        ? " La ligne est incomplète."
        : $" « {Argument} » ne convient pas ici.";

    /// <summary>Le message complet, précédé de « ERREUR en ligne N : ».</summary>
    public string Message => $"ERREUR en ligne {Line} : {Body}";

    string Body => Code switch
    {
        DiagnosticCode.UnexpectedCharacter =>
            $"caractère inattendu « {Argument} ». Ce signe n'est pas utilisé dans ce langage.",
        DiagnosticCode.UnterminatedString =>
            $"un texte commence par un guillemet mais n'est jamais refermé. Ajoutez un guillemet à la fin du texte.",
        DiagnosticCode.UnknownCommand =>
            $"commande inconnue « {Argument} ». Vérifiez l'orthographe ou cherchez-la dans la bible.",
        DiagnosticCode.ExpectedString =>
            $"PRINT attend un texte entre guillemets, mais a trouvé « {Argument} ».",
        DiagnosticCode.UnexpectedToken =>
            $"« {Argument} » n'est pas attendu ici. Séparez les instructions par « : ».",
        DiagnosticCode.ExpectedExpression =>
            $"{(Context.Length == 0 ? "une valeur est attendue ici" : $"{Context} attend ici une valeur")} (un nombre, une variable ou un calcul).{Why}",
        DiagnosticCode.ExpectedEquals =>
            $"le signe « = » est attendu après le nom de la variable.{Why}",
        DiagnosticCode.ExpectedClosingParenthesis =>
            $"{(Context.Length == 0 ? "il" : $"Dans {Context}, il")} manque une parenthèse fermante « ) ».",
        DiagnosticCode.NumberOutOfRange =>
            $"le nombre {Argument} est hors limites. Un entier va de -32768 à 32767.",
        DiagnosticCode.ByteOutOfRange =>
            $"{Argument} ne tient pas dans une variable BYTE, qui va de 0 à 255.",
        DiagnosticCode.UnsupportedVariableType =>
            $"le type de « {Argument} » n'existe pas encore. Seuls les entiers (A ou A%) et les variables BYTE sont gérés.",
        DiagnosticCode.ExpectedVariableName =>
            $"un nom de variable est attendu après BYTE (ou après une virgule).{Why}",
        DiagnosticCode.SuffixNotAllowedInDeclaration =>
            $"« {Argument} » : une variable BYTE se déclare sans suffixe. Écrivez simplement le nom.",
        DiagnosticCode.AlreadyDeclared =>
            $"la variable « {Argument} » est déjà déclarée BYTE.",
        DiagnosticCode.DeclaredAfterUse =>
            $"« {Argument} » est déjà utilisée plus haut comme entier. Placez BYTE {Argument} avant sa première utilisation.",
        DiagnosticCode.ExpectedTo =>
            $"« TO » est attendu après la valeur de départ de FOR.{Why}",
        DiagnosticCode.ExpectedNumber =>
            $"la limite et le pas de FOR doivent être des nombres écrits dans le programme (pas des variables).{Why}",
        DiagnosticCode.StepIsZero =>
            $"le pas de FOR ne peut pas être 0, la boucle ne finirait jamais.",
        DiagnosticCode.NextWithoutFor =>
            $"NEXT sans FOR correspondant. Ajoutez un FOR avant, ou retirez ce NEXT.",
        DiagnosticCode.NextNameMismatch =>
            $"ce NEXT ne correspond pas à la boucle ouverte, dont la variable est « {Argument} ».",
        DiagnosticCode.ForWithoutNext =>
            $"la boucle FOR {Argument} n'est jamais refermée. Ajoutez un NEXT.",
        DiagnosticCode.LoopControlWithoutLoop =>
            $"{Argument} est utilisé en dehors d'une boucle qu'il pourrait quitter ou poursuivre.",
        DiagnosticCode.ExpectedThen =>
            $"« THEN » est attendu après la condition de IF.{Why}",
        DiagnosticCode.ExpectedComparison =>
            $"une comparaison est attendue (=, <>, <, >, <=, >=) : une condition compare deux valeurs, par exemple A > 5.{Why}",
        DiagnosticCode.ElseWithoutIf =>
            $"ELSE sans IF correspondant.",
        DiagnosticCode.DuplicateElse =>
            $"ce IF a déjà un ELSE.",
        DiagnosticCode.EndIfWithoutIf =>
            $"END IF sans IF correspondant.",
        DiagnosticCode.IfWithoutEndIf =>
            $"ce IF sur plusieurs lignes n'est jamais refermé. Ajoutez END IF.",
        DiagnosticCode.BlocksNotNested =>
            $"« {Argument} » arrive alors qu'un autre bloc (IF ou FOR) est encore ouvert. Refermez d'abord le bloc le plus récent.",
        DiagnosticCode.ClosingWithoutOpening =>
            $"{Argument} sans début de boucle correspondant (WHILE pour WEND, REPEAT pour UNTIL, DO pour LOOP).",
        DiagnosticCode.UnclosedLoop =>
            $"la boucle {Argument} n'est jamais refermée. Ajoutez {(Argument switch { "WHILE" => "WEND", "REPEAT" => "UNTIL", _ => "LOOP" })}.",
        DiagnosticCode.DoConditionTwice =>
            $"ce DO a déjà une condition (WHILE ou UNTIL) au début : LOOP ne peut pas en avoir une aussi.",
        DiagnosticCode.DivisionByZero =>
            $"division par zéro avec « {Argument} » : le diviseur ne peut pas être 0.",
        DiagnosticCode.ExpectedOpenParenthesis =>
            $"une fonction s'écrit avec des parenthèses, par exemple {Argument}.",
        DiagnosticCode.WrongArgumentCount =>
            $"le nombre de valeurs est incorrect. Écrivez {Argument}.",
        DiagnosticCode.ValueOutOfRange =>
            $"{(Context.Length == 0 ? "" : Context + " : ")}la valeur {Argument.Split('|')[0]} n'est pas permise. Valeurs possibles : {Argument.Split('|')[1]}.",
        DiagnosticCode.ExpectedLineNumber =>
            $"{(Context.Length == 0 ? "une instruction de saut" : Context)} attend ici un numéro de ligne (un nombre entier de 1 à 65529).{Why}",
        DiagnosticCode.LineNotFound =>
            $"{Context} : la ligne {Argument} n'existe pas. Vérifiez le numéro de ligne.",
        DiagnosticCode.DuplicateLineNumber =>
            $"le numéro de ligne {Argument} est déjà utilisé plus haut dans le programme.",
        DiagnosticCode.ExpectedGoto =>
            $"ON attend GOTO ou GOSUB après la valeur.{Why}",
        DiagnosticCode.ProgramTooLarge =>
            $"MEMORY : le programme compilé occupe de &4000 à &{Argument.Split('|')[0]}, ce qui dépasse la limite &{Argument.Split('|')[1]} demandée. Mettez une limite plus haute, ou raccourcissez le programme.",
        DiagnosticCode.ExpectedArrayName =>
            $"DIM attend un nom de tableau suivi de sa taille entre parenthèses, par exemple DIM A(10).{Why}",
        DiagnosticCode.ArrayDimensionMismatch =>
            $"le tableau {Argument.Split('|')[0]}() est utilisé ici avec {Argument.Split('|')[1]} numéro(s) de case, mais avec {Argument.Split('|')[2]} plus haut. Utilisez toujours le même nombre, par exemple {Argument.Split('|')[0]}(I) partout ou {Argument.Split('|')[0]}(I, J) partout.",
        DiagnosticCode.ByteArrayNotYet =>
            $"« {Argument} » est déclaré BYTE : les tableaux d'octets ne sont pas encore possibles. Donnez un autre nom au tableau.",
        DiagnosticCode.NotYetAvailable =>
            $"« {Argument} » est un mot du Locomotive BASIC qui n'est pas encore disponible ici : il ne peut pas servir de nom de tableau. Voyez la liste dans docs\\COMPATIBILITE-LOCOMOTIVE.md.",
        _ => $"problème inconnu ({Code})."
    };

    public override string ToString() => Message;
}
