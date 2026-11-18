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
    MemoryTooHigh,
    UnknownCharacter,
    ExpectedArrayName,
    ArrayDimensionMismatch,
    ByteArrayNotYet,
    NotYetAvailable,
    ExpectedDataValue,
    DataTextNotYet,
    ExpectedTarget,
    ExpectedHexDigits,
    HexNumberOutOfRange,
    TypeMismatch,
    StringTooLong,
    StringNotYetHere,
    ExpectedLetterRange,
    ExpectedStreamComma,     // lot H1 (D072) : PRINT #1 "A" sans virgule après le numéro de fenêtre
    ExpectedStream,          // lot H1 (D072) : POS(0) au lieu de POS(#0)
    UsingFormatNotLiteral,   // lot H3 (D074) ; depuis D098 : format sans « ; » après lui
    UsingWithoutField,       // lot H3 (D074) : format sans aucun emplacement
    SaveProgram,             // lot J2 (D085) : SAVE sans ,B (le programme BASIC n'existe plus une fois compilé)
    NotForCompiled,          // lot J3 (D086) : commande qui n'a pas de sens une fois compilé (CHAIN, MERGE...)
    UserFunctionName,        // lot K7 (D095) : FN ou DEF FN sans nom de fonction
    UserFunctionParameter,   // DEF FNM(A,3) : une valeur de DEF FN doit être un nom de variable
    UserFunctionUnknown,     // FNX appelée mais jamais définie (le Locomotive : « Unknown user function »)
    UserFunctionTwice,       // deux DEF FN pour le même nom
    UserFunctionRecursive,   // FNA s'appelle elle-même (directement ou par une autre FN)
    UserFunctionNeedsReal,   // une virgule serait perdue : nombre à virgule donné à un entier (conseil : le signe !)
    ExpectedEraseName,       // lot K8 (D096) : ERASE sans nom de tableau
    DecFormat                // lot L2 (D103) : DEC$ avec un format qui n'est pas un seul emplacement de nombre
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
    /// <summary>Fichier où est l'erreur, pour un programme compilé en même temps (RUN "nom", D087) ; null : celui de l'éditeur.</summary>
    public string? File { get; init; }

    public string Message => File is null ? $"ERREUR en ligne {Line} : {Body}" : $"ERREUR dans {File}, ligne {Line} : {Body}";

    string Body => Code switch
    {
        DiagnosticCode.UnexpectedCharacter =>
            $"caractère inattendu « {Argument} ». Ce signe n'est pas utilisé dans ce langage." +
            (Argument.Length == 1 && char.IsLetter(Argument[0]) && !char.IsAsciiLetter(Argument[0])
                ? " Un nom de variable ne prend que des lettres sans accent (A à Z), des chiffres et des points."
                : ""),
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
            Context switch
            {
                "" => $"ON attend GOTO ou GOSUB après la valeur.{Why}",
                "ON ERROR" => $"ON ERROR attend GOTO puis un numéro de ligne (exemple : ON ERROR GOTO 1000).{Why}",
                "ON SQ" => $"ON SQ attend GOSUB puis un numéro de ligne (exemple : ON SQ(1) GOSUB 100).{Why}",
                "ON BREAK" => $"ON BREAK attend CONT, STOP, ou GOSUB puis un numéro de ligne (exemple : ON BREAK GOSUB 100).{Why}",
                _ => $"{Context} attend GOSUB puis un numéro de ligne (exemple : {Context} 50 GOSUB 100).{Why}"
            },
        DiagnosticCode.ProgramTooLarge =>
            $"MEMORY : le programme compilé occupe de &4000 à &{Argument.Split('|')[0]}, ce qui dépasse la limite &{Argument.Split('|')[1]} demandée. Mettez une limite plus haute, ou raccourcissez le programme.",
        DiagnosticCode.UnknownCharacter =>
            $"le caractère « {Argument.Split('|')[0]} » n'existe pas sur le CPC {Argument.Split('|')[1]}, ni sur un autre CPC." +
            (Argument.Split('|')[2].Length == 0 ? " Remplacez-le par un autre caractère." : $" Écrivez par exemple « {Argument.Split('|')[2]} »."),
        DiagnosticCode.MemoryTooHigh =>
            $"MEMORY : l'adresse &{Argument} est trop haute. Le plus haut permis est &A67B (42619) ; au-dessus, le CPC répond « Memory full ».",
        DiagnosticCode.ExpectedArrayName =>
            $"DIM attend un nom de tableau suivi de sa taille entre parenthèses, par exemple DIM A(10).{Why}",
        DiagnosticCode.ArrayDimensionMismatch =>
            $"le tableau {Argument.Split('|')[0]}() est utilisé ici avec {Argument.Split('|')[1]} numéro(s) de case, mais avec {Argument.Split('|')[2]} plus haut. Utilisez toujours le même nombre, par exemple {Argument.Split('|')[0]}(I) partout ou {Argument.Split('|')[0]}(I, J) partout.",
        DiagnosticCode.ByteArrayNotYet =>
            $"« {Argument} » est déclaré BYTE : les tableaux d'octets ne sont pas encore possibles. Donnez un autre nom au tableau.",
        DiagnosticCode.NotYetAvailable =>
            $"« {Argument} » est un mot du Locomotive BASIC qui n'est pas encore disponible ici : il ne peut pas servir de nom de tableau. Voyez la liste dans docs\\COMPATIBILITE-LOCOMOTIVE.md.",
        DiagnosticCode.ExpectedDataValue =>
            $"DATA attend des nombres entiers séparés par des virgules, par exemple DATA 10, -5, 300 (de -32768 à 65535).{Why}",
        DiagnosticCode.DataTextNotYet =>
            $"« {Argument} » : les textes dans DATA ne sont pas encore possibles ; ils viendront avec les chaînes de caractères. Pour l'instant, DATA ne contient que des nombres entiers.",
        DiagnosticCode.ExpectedTarget =>
            $"{(Context.Length == 0 ? "READ" : Context)} attend ici un nom de variable ou une case de tableau, par exemple A ou T(I).{Why}",
        DiagnosticCode.ExpectedHexDigits =>
            $"« {Argument} » : après « & », il faut des chiffres hexadécimaux (0 à 9 et A à F, par exemple &C9 ou &HC9) ou, après « &X », des chiffres binaires (0 et 1, par exemple &X1010).",
        DiagnosticCode.HexNumberOutOfRange =>
            $"le nombre {Argument} est trop grand : un nombre écrit avec « & » va de &0 à &FFFF.",
        DiagnosticCode.TypeMismatch =>
            $"{(Context.Length == 0 ? "" : Context + " : ")}un texte et un nombre ne se mélangent pas ici ({Argument}). Un texte s'écrit entre guillemets ou dans une variable qui finit par $, comme A$.",
        DiagnosticCode.StringTooLong =>
            $"ce texte a {Argument} caractères : un texte en a 255 au plus.",
        DiagnosticCode.StringNotYetHere =>
            $"« {Argument} » : les textes ne sont pas encore possibles ici (tableaux de textes, READ A$, @A$ et MID$(...) = viendront dans l'étape suivante).",
        DiagnosticCode.ExpectedLetterRange =>
            $"{(Context.Length == 0 ? "DEFINT, DEFREAL ou DEFSTR" : Context)} attend une lettre, ou deux lettres séparées par « - » (exemple : DEFINT A-Z, I).{Why}",
        DiagnosticCode.ExpectedStreamComma =>
            $"{(Context.Length == 0 ? "cette commande" : Context)} attend une virgule « , » après le numéro de fenêtre (exemple : {(Context.Length == 0 ? "PRINT" : Context)} #1, ...).{Why}",
        DiagnosticCode.ExpectedStream =>
            $"{(Context.Length == 0 ? "POS ou VPOS" : Context)} attend un numéro de fenêtre précédé de « # », comme le Locomotive (exemple : {(Context.Length == 0 ? "POS" : Context)}(#0)).{Why}",
        DiagnosticCode.UsingFormatNotLiteral =>
            $"PRINT USING attend un format (entre guillemets, ou dans une variable texte) suivi de « ; », par exemple PRINT USING \"###.##\"; X ou PRINT USING F$; X.{Why}",
        DiagnosticCode.UsingWithoutField =>
            $"PRINT USING : le format « {Argument} » ne contient aucun emplacement (# pour un nombre, ! & ou \\  \\ pour un texte). Le vrai BASIC répondrait « Improper argument ».",
        DiagnosticCode.NotForCompiled => Argument,
        DiagnosticCode.DecFormat =>
            $"DEC$ : le format « {Argument} » doit être un seul emplacement de nombre, sans texte autour, par exemple DEC$(X, \"###.##\"). Le vrai BASIC répondrait « Improper argument ».",
        DiagnosticCode.ExpectedEraseName =>
            $"ERASE attend des noms de tableaux séparés par des virgules, sans parenthèses, par exemple ERASE T, N$.{Why}",
        DiagnosticCode.UserFunctionName =>
            $"{(Context.Length == 0 ? "FN" : Context)} attend le nom de la fonction, par exemple FNDOUBLE(3) ou DEF FNDOUBLE(X)=X*2.{Why}",
        DiagnosticCode.UserFunctionParameter =>
            $"DEF FN attend entre parenthèses des noms de variables séparés par des virgules, par exemple DEF FNM(A,B)=A*B.{Why}",
        DiagnosticCode.UserFunctionUnknown =>
            $"la fonction FN{Argument} n'est définie nulle part. Ajoutez une ligne DEF FN{Argument}(...)=calcul (le vrai BASIC répondrait « Unknown user function »).",
        DiagnosticCode.UserFunctionTwice =>
            $"la fonction FN{Argument} est déjà définie plus haut par DEF FN. Donnez un autre nom à l'une des deux.",
        DiagnosticCode.UserFunctionRecursive =>
            $"la fonction FN{Argument} s'appelle elle-même (directement ou par une autre fonction FN). Ce n'est pas possible : sur le vrai CPC, le programme finirait par planter.",
        DiagnosticCode.UserFunctionNeedsReal => Argument.Split('|') is [string f, string p] && p.Length > 0
            ? $"FN{f} reçoit un nombre à virgule pour {p}, mais {p} est un entier : la virgule serait perdue (le vrai BASIC la garde). " +
              $"Dans l'instruction DEF FN, écrivez la valeur avec le signe « ! » : DEF FN{f}({p}!)=..., et {p}! dans le calcul."
            : $"le calcul de FN{Argument.Split('|')[0]} donne un nombre à virgule, mais FN{Argument.Split('|')[0]} rend un entier : la virgule serait perdue (le vrai BASIC la garde). " +
              $"Dans l'instruction DEF FN, écrivez le nom avec le signe « ! » : DEF FN{Argument.Split('|')[0]}!(...)=..., et appelez FN{Argument.Split('|')[0]}!.",
        DiagnosticCode.SaveProgram =>
            "SAVE sans « ,B » enregistrerait le programme BASIC, qui n'existe plus une fois compilé. Pour une zone de mémoire : SAVE \"nom\",B,début,longueur (exemple : SAVE \"ECRAN\",B,&C000,&4000).",
        _ => $"problème inconnu ({Code})."
    };

    public override string ToString() => Message;
}
