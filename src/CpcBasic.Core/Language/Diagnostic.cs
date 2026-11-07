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
    ExpectedString,
    UnexpectedToken
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
        _ => $"Ligne {Line} : problème inconnu ({Code})."
    };

    public override string ToString() => Message;
}
