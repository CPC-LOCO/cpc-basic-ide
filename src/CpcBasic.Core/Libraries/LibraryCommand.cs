// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using System.Globalization;
using CpcBasic.Core.Language;

namespace CpcBasic.Core.Libraries;

/// <summary>Genre d'un élément de bibliothèque (D144) : commande ou fonction du BASIC, ou routine pour l'assembleur seulement.</summary>
public enum LibraryKind { Command, Function, Routine }

/// <summary>
/// Une valeur donnée à une commande ou à une fonction : son nom (celui de sa case mémoire), ses limites et son rôle.
/// Default : pour une valeur facultative (;@OPTION, D150), ce que reçoit la routine quand la valeur est sautée
/// (elle peut être hors des limites : la routine sait ainsi qu'elle a été sautée) ; null : valeur obligatoire.
/// </summary>
public sealed record LibraryParameter(string Name, int Min, int Max, string Description, int? Default = null)
{
    public bool IsOptional => Default is not null;
}

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
    string Code)
{
    /// <summary>Fichier d'où vient la commande (null si elle n'a pas été lue sur le disque) : pour l'éditeur d'assembleur (D159).</summary>
    public string? FilePath { get; init; }
}

/// <summary>
/// Lecture d'un fichier de commande (D146). Le fichier commence par les réponses du questionnaire, sur des lignes « ;@ »
/// (des commentaires pour un assembleur, le fichier peut donc aussi être essayé dans WinAPE), puis vient le code :
/// <code>
/// ;@NOM STAR
/// ;@GENRE COMMANDE                       (COMMANDE, FONCTION ou ROUTINE)
/// ;@VALEUR X, 0, 639, position horizontale du centre
/// ;@VALEUR Y, 0, 399, position verticale du centre
/// ;@OPTION STYLO, 0, 15, -1, couleur du contour (sautée : la routine reçoit -1)
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
                    if (parameters.Any(p => p.IsOptional))
                        errors.Add(new(number, "une VALEUR obligatoire ne peut pas venir après une OPTION : mettez les OPTION à la fin."));
                    else if (ParseParameter(value, parameters, optional: false) is string parameterError)
                        errors.Add(new(number, parameterError));
                    break;

                case "OPTION":                  // valeur facultative (D150)
                    if (ParseParameter(value, parameters, optional: true) is string optionError)
                        errors.Add(new(number, optionError));
                    break;

                case "AIDE":
                    help = help.Length == 0 ? value : help + "\n" + value;
                    break;

                case "AUTEUR":
                    author = value;
                    break;

                default:
                    errors.Add(new(number, $"« ;@{keyword} » n'est pas connu (NOM, GENRE, VALEUR, OPTION, AIDE ou AUTEUR)."));
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
        if (name is not null && kind is LibraryKind.Command or LibraryKind.Function)
        {
            // le BASIC n'accepte pas _ dans un nom, lit FNXXX comme FN XXX, et un mot du langage ne peut pas changer de sens
            if (name.Contains('_'))
                errors.Add(new(nameLine, $"{name} : une commande ou une fonction ne peut pas contenir _ (le BASIC ne l'accepte pas dans un nom ; une ROUTINE le peut)."));
            else if (name.StartsWith("FN"))
                errors.Add(new(nameLine, $"{name} : un nom qui commence par FN est réservé aux fonctions DEF FN du BASIC."));
            else if (Parser.IsKeyword(name))
                errors.Add(new(nameLine, $"{name} est déjà un mot du BASIC : choisissez un autre nom."));
        }
        if (name is not null && parameters.Any(p => p.Name == name))
            errors.Add(new(nameLine, $"une valeur ne peut pas s'appeler comme la commande ({name})."));

        if (errors.Count > 0 || name is null || kind is null)
            return (null, errors.OrderBy(e => e.Line).ToList());
        return (new LibraryCommand(name, kind.Value, parameters, help, author, library.ToUpperInvariant(), isProtected, text), errors);
    }

    /// <summary>
    /// La bonne écriture en BASIC, avec les noms du questionnaire et les OPTION entre crochets :
    /// « STAR x, y [, stylo] », « LUNE(n [, option]) », ou le nom seul (D150, D155).
    /// </summary>
    public static string Signature(LibraryCommand command)
    {
        var text = new System.Text.StringBuilder();
        foreach (LibraryParameter p in command.Parameters)
        {
            string separator = text.Length == 0 ? "" : ", ";
            string word = p.Name.ToLowerInvariant();
            text.Append(p.IsOptional ? $" [{separator}{word}]" : separator + word);
        }
        string names = text.ToString().Trim();
        if (command.Kind == LibraryKind.Function)
            return command.Parameters.Count == 0 ? command.Name : $"{command.Name}({names})";
        return command.Parameters.Count == 0 ? command.Name : $"{command.Name} {names}";
    }

    /// <summary>
    /// Écrit le texte d'un fichier de commande à partir des réponses du questionnaire (D157) : les lignes « ;@ » puis le code.
    /// Une valeur avec Default est écrite en OPTION, les autres en VALEUR ; l'aide peut avoir plusieurs lignes.
    /// Le texte rendu se relit avec <see cref="Parse"/> (c'est lui qui dit s'il y a une faute).
    /// </summary>
    public static string Compose(string name, LibraryKind kind, IReadOnlyList<LibraryParameter> parameters,
                                 string help, string author, string code)
    {
        var text = new System.Text.StringBuilder();
        text.Append(";@NOM ").Append(name.Trim().ToUpperInvariant()).Append('\n');
        text.Append(";@GENRE ").Append(kind switch
        {
            LibraryKind.Command => "COMMANDE",
            LibraryKind.Function => "FONCTION",
            _ => "ROUTINE"
        }).Append('\n');
        foreach (LibraryParameter p in parameters)
        {
            string role = p.Description.Trim().Length > 0 ? ", " + p.Description.Trim() : "";
            text.Append(p.IsOptional
                ? $";@OPTION {p.Name.ToUpperInvariant()}, {p.Min}, {p.Max}, {p.Default}{role}\n"
                : $";@VALEUR {p.Name.ToUpperInvariant()}, {p.Min}, {p.Max}{role}\n");
        }
        foreach (string line in help.Replace("\r\n", "\n").Split('\n'))
            if (line.Trim().Length > 0)
                text.Append(";@AIDE ").Append(line.Trim()).Append('\n');
        if (author.Trim().Length > 0)
            text.Append(";@AUTEUR ").Append(author.Trim()).Append('\n');
        text.Append('\n').Append(code.Replace("\r\n", "\n").TrimEnd()).Append('\n');
        return text.ToString();
    }

    /// <summary>
    /// Sépare la fiche (les lignes « ;@ » du tout début, avec leur fin de ligne) du code qui suit (D159, D160).
    /// Les fins de ligne sont ramenées à « \n ».
    /// </summary>
    public static (string Header, string Body) SplitHeader(string text)
    {
        text = text.Replace("\r\n", "\n");
        int at = 0;
        while (at < text.Length)
        {
            int end = text.IndexOf('\n', at);
            string line = end < 0 ? text[at..] : text[at..end];
            if (!line.TrimStart().StartsWith(";@"))
                break;
            at = end < 0 ? text.Length : end + 1;
        }
        return (text[..at], text[at..]);
    }

    /// <summary>Code de départ d'une nouvelle commande (D157) : des commentaires qui expliquent quoi faire, puis RET.</summary>
    public static string StarterCode(string name, LibraryKind kind, IReadOnlyList<LibraryParameter> parameters)
    {
        var text = new System.Text.StringBuilder();
        text.Append($"; Code de {name.Trim().ToUpperInvariant()}, écrit comme pour l'assembleur de WinAPE (pas d'ORG).\n");
        if (parameters.Count > 0)
        {
            text.Append("; Chaque valeur se lit dans sa case, par son nom :");
            foreach (LibraryParameter p in parameters)
                text.Append($" LD HL,({p.Name.ToUpperInvariant()})");
            text.Append('\n');
        }
        text.Append(kind switch
        {
            LibraryKind.Function => "; Une fonction rend son résultat dans HL.\n",
            LibraryKind.Routine => "; Une routine reçoit ses valeurs dans les registres (dites lesquels dans l'aide).\n",
            _ => ""
        });
        text.Append("        RET\n");
        return text.ToString();
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

    // VALEUR nom, min, max[, description] ; OPTION nom, min, max, valeur si sautée[, description] (D150)
    static string? ParseParameter(string value, List<LibraryParameter> parameters, bool optional)
    {
        string keyword = optional ? "OPTION" : "VALEUR";
        int fixedParts = optional ? 4 : 3;
        string[] parts = value.Split(',', fixedParts + 1);
        if (parts.Length < fixedParts)
            return optional
                ? $"OPTION « {value} » : écrivez OPTION nom, plus petite valeur, plus grande valeur, valeur si elle est sautée, rôle (par exemple OPTION STYLO, 0, 15, -1, couleur du contour)."
                : $"VALEUR « {value} » : écrivez VALEUR nom, plus petite valeur, plus grande valeur, rôle (par exemple VALEUR X, 0, 639, position horizontale).";
        string name = parts[0].Trim();
        if (CheckName(name, "le nom d'une valeur") is string nameError)
            return nameError;
        name = name.ToUpperInvariant();
        if (parameters.Any(p => p.Name == name))
            return $"la valeur {name} est déjà déclarée.";
        if (!TryNumber(parts[1].Trim(), out int min))
            return $"{keyword} {name} : « {parts[1].Trim()} » n'est pas un nombre entier de -32768 à 65535.";
        if (!TryNumber(parts[2].Trim(), out int max))
            return $"{keyword} {name} : « {parts[2].Trim()} » n'est pas un nombre entier de -32768 à 65535.";
        if (min > max)
            return $"{keyword} {name} : la plus petite valeur ({min}) dépasse la plus grande ({max}).";
        if (max - min > 65535)
            return $"{keyword} {name} : de {min} à {max}, c'est trop pour une case de 2 octets.";
        int? fallback = null;
        if (optional)
        {
            if (!TryNumber(parts[3].Trim(), out int skipped))
                return $"OPTION {name} : la valeur si elle est sautée, « {parts[3].Trim()} », n'est pas un nombre entier de -32768 à 65535.";
            fallback = skipped;
        }
        parameters.Add(new LibraryParameter(name, min, max, parts.Length > fixedParts ? parts[fixedParts].Trim() : "", fallback));
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
