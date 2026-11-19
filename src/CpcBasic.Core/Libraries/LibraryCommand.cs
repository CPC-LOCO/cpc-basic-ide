// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using System.Globalization;

namespace CpcBasic.Core.Libraries;

/// <summary>Genre d'un élément de bibliothèque (D144) : commande ou fonction du BASIC, ou routine pour l'assembleur seulement.</summary>
public enum LibraryKind { Command, Function, Routine }

/// <summary>Une valeur donnée à une commande ou à une fonction : son nom (celui de sa case mémoire), ses limites et son rôle.</summary>
public sealed record LibraryParameter(string Name, int Min, int Max, string Description);

/// <summary>Une erreur dans un fichier de commande : numéro de ligne (à partir de 1) et explication.</summary>
public sealed record LibraryError(int Line, string Message)
{
    public override string ToString() => $"ligne {Line} : {Message}";
}

/// <summary>
/// Une commande, une fonction ou une routine de bibliothèque (D144, D146), lue dans son fichier texte.
/// <see cref="Code"/> est le texte assembleur complet du fichier : les lignes « ;@ » y restent, ce sont des commentaires.
/// </summary>
public sealed record LibraryCommand(
    string Name,
    LibraryKind Kind,
    IReadOnlyList<LibraryParameter> Parameters,
    string Help,
    string Author,
    string Library,
    bool Protected,
    string Code);

/// <summary>
/// Lecture d'un fichier de commande (D146). Le fichier commence par les réponses du questionnaire, sur des lignes « ;@ »
/// (des commentaires pour un assembleur, le fichier peut donc aussi être essayé dans WinAPE), puis vient le code :
/// <code>
/// ;@NOM STAR
/// ;@GENRE COMMANDE                       (COMMANDE, FONCTION ou ROUTINE)
/// ;@VALEUR X, 0, 639, position horizontale du centre
/// ;@VALEUR Y, 0, 399, position verticale du centre
/// ;@AIDE Dessine une étoile
/// ;@AUTEUR RetroPoke
///         LD HL,(X)
///         ...
///         RET
/// </code>
/// Une commande ou une fonction lit ses valeurs dans des cases qui portent leur nom ; une fonction rend son résultat
/// dans HL ; une routine n'a pas de VALEUR (elle reçoit ses valeurs dans les registres, comme le dit son AIDE).
/// </summary>
public static class LibraryCommandFile
{
    /// <summary>Extension des fichiers de commande. Provisoire : l'utilisateur la choisira (D144, question 5).</summary>
    public const string Extension = ".cmd.asm";

    public const int MaxParameters = 16;
    public const int MaxNameLength = 16;

    /// <summary>Noms interdits pour une commande ou une valeur : registres et conditions du Z80 (on ne pourrait plus les distinguer).</summary>
    static readonly HashSet<string> Reserved =
    [
        "A", "B", "C", "D", "E", "H", "L", "I", "R", "F", "IX", "IY", "AF", "BC", "DE", "HL", "SP",
        "NZ", "Z", "NC", "PO", "PE", "P", "M"
    ];

    /// <summary>
    /// Lit le texte d'un fichier. <paramref name="fileName"/> (sans dossier) doit correspondre au NOM.
    /// Rend la commande, ou null avec la liste des erreurs.
    /// </summary>
    public static (LibraryCommand? Command, IReadOnlyList<LibraryError> Errors) Parse(
        string text, string library, string fileName, bool isProtected)
    {
        var errors = new List<LibraryError>();
        string? name = null;
        LibraryKind? kind = null;
        var parameters = new List<LibraryParameter>();
        string help = "", author = "";
        int nameLine = 1, kindLine = 1;

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i].Trim();
            if (!line.StartsWith(";@"))
                continue;
            string body = line[2..].Trim();
            int space = body.IndexOfAny([' ', '\t']);
            string keyword = (space < 0 ? body : body[..space]).ToUpperInvariant();
            string value = space < 0 ? "" : body[(space + 1)..].Trim();

            switch (keyword)
            {
                case "NOM":
                    if (name is not null)
                        errors.Add(new(number, "NOM est donné deux fois."));
                    else if (CheckName(value, "le nom de la commande") is string nameError)
                        errors.Add(new(number, nameError));
                    else
                        (name, nameLine) = (value.ToUpperInvariant(), number);
                    break;

                case "GENRE":
                    if (kind is not null)
                        errors.Add(new(number, "GENRE est donné deux fois."));
                    else
                    {
                        kind = value.ToUpperInvariant() switch
                        {
                            "COMMANDE" => LibraryKind.Command,
                            "FONCTION" => LibraryKind.Function,
                            "ROUTINE" => LibraryKind.Routine,
                            _ => null
                        };
                        kindLine = number;
                        if (kind is null)
                            errors.Add(new(number, $"GENRE « {value} » : écrivez COMMANDE, FONCTION ou ROUTINE."));
                    }
                    break;

                case "VALEUR":
                    if (ParseParameter(value, parameters) is string parameterError)
                        errors.Add(new(number, parameterError));
                    break;

                case "AIDE":
                    help = help.Length == 0 ? value : help + "\n" + value;
                    break;

                case "AUTEUR":
                    author = value;
                    break;

                default:
                    errors.Add(new(number, $"« ;@{keyword} » n'est pas connu (NOM, GENRE, VALEUR, AIDE ou AUTEUR)."));
                    break;
            }
        }

        if (name is null)
            errors.Add(new(1, "la ligne « ;@NOM » manque (le nom de la commande)."));
        if (kind is null && !errors.Any(e => e.Message.StartsWith("GENRE")))
            errors.Add(new(1, "la ligne « ;@GENRE » manque (COMMANDE, FONCTION ou ROUTINE)."));

        if (name is not null)
        {
            string stem = StemOf(fileName);
            if (!stem.Equals(name, StringComparison.OrdinalIgnoreCase))
                errors.Add(new(nameLine, $"le fichier s'appelle {stem} mais la commande s'appelle {name} : les deux doivent être pareils."));
        }
        if (kind == LibraryKind.Routine && parameters.Count > 0)
            errors.Add(new(kindLine, "une ROUTINE n'a pas de VALEUR : elle reçoit ses valeurs dans les registres (décrivez-les dans AIDE)."));
        if (parameters.Count > MaxParameters)
            errors.Add(new(kindLine, $"{parameters.Count} valeurs : {MaxParameters} au plus."));
        if (name is not null && parameters.Any(p => p.Name == name))
            errors.Add(new(nameLine, $"une valeur ne peut pas s'appeler comme la commande ({name})."));

        if (errors.Count > 0 || name is null || kind is null)
            return (null, errors.OrderBy(e => e.Line).ToList());
        return (new LibraryCommand(name, kind.Value, parameters, help, author, library.ToUpperInvariant(), isProtected, text), errors);
    }

    /// <summary>Nom du fichier sans dossier ni extension (« STAR.cmd.asm » donne « STAR »).</summary>
    public static string StemOf(string fileName)
    {
        string file = Path.GetFileName(fileName);
        if (file.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            return file[..^Extension.Length];
        return Path.GetFileNameWithoutExtension(file);
    }

    /// <summary>Vérifie un nom de commande ou de valeur ; rend le message d'erreur, ou null s'il est bon.</summary>
    public static string? CheckName(string name, string what)
    {
        if (name.Length == 0)
            return $"{what} manque.";
        if (!char.IsAsciiLetter(name[0]) || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            return $"« {name} » ne convient pas pour {what} : lettres sans accent, chiffres et _ seulement, une lettre au début.";
        if (name.Length > MaxNameLength)
            return $"« {name} » est trop long pour {what} ({MaxNameLength} caractères au plus).";
        if (Reserved.Contains(name.ToUpperInvariant()))
            return $"« {name} » ne convient pas pour {what} : c'est un nom de registre ou de condition du Z80.";
        return null;
    }

    // VALEUR nom, min, max[, description]
    static string? ParseParameter(string value, List<LibraryParameter> parameters)
    {
        string[] parts = value.Split(',', 4);
        if (parts.Length < 3)
            return $"VALEUR « {value} » : écrivez VALEUR nom, plus petite valeur, plus grande valeur, rôle (par exemple VALEUR X, 0, 639, position horizontale).";
        string name = parts[0].Trim();
        if (CheckName(name, "le nom d'une valeur") is string nameError)
            return nameError;
        name = name.ToUpperInvariant();
        if (parameters.Any(p => p.Name == name))
            return $"la valeur {name} est déjà déclarée.";
        if (!TryNumber(parts[1].Trim(), out int min))
            return $"VALEUR {name} : « {parts[1].Trim()} » n'est pas un nombre entier de -32768 à 65535.";
        if (!TryNumber(parts[2].Trim(), out int max))
            return $"VALEUR {name} : « {parts[2].Trim()} » n'est pas un nombre entier de -32768 à 65535.";
        if (min > max)
            return $"VALEUR {name} : la plus petite valeur ({min}) dépasse la plus grande ({max}).";
        if (max - min > 65535)
            return $"VALEUR {name} : de {min} à {max}, c'est trop pour une case de 2 octets.";
        parameters.Add(new LibraryParameter(name, min, max, parts.Length == 4 ? parts[3].Trim() : ""));
        return null;
    }

    // 123, -5, &FF ou #FF
    static bool TryNumber(string text, out int value)
    {
        value = 0;
        bool ok;
        if (text.Length > 1 && text[0] is '&' or '#')
            ok = int.TryParse(text[1..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        else
            ok = int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        return ok && value is >= -32768 and <= 65535;
    }
}
