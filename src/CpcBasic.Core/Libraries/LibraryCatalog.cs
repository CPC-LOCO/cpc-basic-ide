// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

namespace CpcBasic.Core.Libraries;

/// <summary>Un fichier de commande à mettre au catalogue : sa bibliothèque, son nom de fichier, son texte, et s'il vient du projet.</summary>
public sealed record CatalogSource(string Library, string FileName, string Text, bool Protected);

/// <summary>Un problème trouvé en faisant le catalogue : le fichier, la ligne (0 si tout le fichier) et l'explication.</summary>
public sealed record CatalogError(string FileName, int Line, string Message)
{
    public override string ToString() => Line > 0 ? $"{FileName}, ligne {Line} : {Message}" : $"{FileName} : {Message}";
}

/// <summary>
/// Catalogue des bibliothèques (D144, D147) : toutes les commandes, fonctions et routines connues, chacune avec sa bibliothèque.
/// Il est fait à partir de deux dossiers : celui du projet (commandes protégées, livrées avec l'éditeur) et celui du
/// programmeur (ses commandes). Dans chacun, un sous-dossier par bibliothèque (GRAPHIQUE, SON...) et un fichier par commande.
/// Une bibliothèque qui existe dans les deux dossiers n'en fait qu'une. Un nom n'existe qu'une fois dans tout le catalogue :
/// le second fichier qui le donne est refusé (le projet est lu en premier, ses commandes passent donc avant).
/// </summary>
public sealed class LibraryCatalog
{
    readonly Dictionary<string, LibraryCommand> byName = new(StringComparer.OrdinalIgnoreCase);
    readonly List<CatalogError> errors = [];

    LibraryCatalog() { }

    /// <summary>Catalogue vide (aucune bibliothèque).</summary>
    public static LibraryCatalog Empty { get; } = new();

    /// <summary>Les fichiers refusés et pourquoi.</summary>
    public IReadOnlyList<CatalogError> Errors => errors;

    /// <summary>Toutes les commandes, fonctions et routines, rangées par bibliothèque puis par nom.</summary>
    public IReadOnlyList<LibraryCommand> All =>
        byName.Values.OrderBy(c => c.Library, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();

    /// <summary>Noms des bibliothèques, dans l'ordre alphabétique.</summary>
    public IReadOnlyList<string> Libraries =>
        byName.Values.Select(c => c.Library).Distinct().OrderBy(l => l, StringComparer.Ordinal).ToList();

    /// <summary>La commande, fonction ou routine de ce nom, ou null (majuscules et minuscules sans différence).</summary>
    public LibraryCommand? Find(string name) => byName.GetValueOrDefault(name);

    /// <summary>Fait le catalogue à partir de textes déjà lus (les sources du projet d'abord).</summary>
    public static LibraryCatalog Build(IEnumerable<CatalogSource> sources)
    {
        var catalog = new LibraryCatalog();
        foreach (CatalogSource source in sources)
            catalog.Add(source);
        return catalog;
    }

    /// <summary>
    /// Fait le catalogue en lisant les dossiers : <paramref name="projectFolder"/> (commandes protégées) puis
    /// <paramref name="userFolder"/>. Un dossier absent (ou null) est simplement sauté.
    /// </summary>
    public static LibraryCatalog Load(string? projectFolder, string? userFolder)
    {
        var catalog = new LibraryCatalog();
        catalog.LoadFolder(projectFolder, isProtected: true);
        catalog.LoadFolder(userFolder, isProtected: false);
        return catalog;
    }

    void LoadFolder(string? root, bool isProtected)
    {
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return;

        foreach (string loose in Sorted(Directory.GetFiles(root, "*" + LibraryCommandFile.Extension)))
            errors.Add(new CatalogError(Path.GetFileName(loose), 0,
                "ce fichier n'est dans aucune bibliothèque : rangez-le dans un dossier (par exemple GRAPHIQUE)."));

        foreach (string folder in Sorted(Directory.GetDirectories(root)))
        {
            string library = Path.GetFileName(folder);
            foreach (string file in Sorted(Directory.GetFiles(folder, "*" + LibraryCommandFile.Extension)))
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    errors.Add(new CatalogError(Path.Combine(library, Path.GetFileName(file)), 0, $"lecture impossible ({e.Message})."));
                    continue;
                }
                Add(new CatalogSource(library, Path.GetFileName(file), text, isProtected));
            }
        }
    }

    static IEnumerable<string> Sorted(string[] paths) => paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

    void Add(CatalogSource source)
    {
        string where = $"{source.Library.ToUpperInvariant()}\\{source.FileName}";
        if (CheckLibraryName(source.Library) is string libraryError)
        {
            errors.Add(new CatalogError(where, 0, libraryError));
            return;
        }

        var (command, problems) = LibraryCommandFile.Parse(source.Text, source.Library, source.FileName, source.Protected);
        if (command is null)
        {
            foreach (LibraryError problem in problems)
                errors.Add(new CatalogError(where, problem.Line, problem.Message));
            return;
        }

        if (byName.TryGetValue(command.Name, out LibraryCommand? existing))
        {
            errors.Add(new CatalogError(where, 0,
                $"le nom {command.Name} existe déjà dans la bibliothèque {existing.Library} : un nom ne peut servir qu'une fois."));
            return;
        }
        byName[command.Name] = command;
    }

    /// <summary>Vérifie le nom d'une bibliothèque (son dossier) ; rend le message d'erreur, ou null s'il est bon.</summary>
    public static string? CheckLibraryName(string name)
    {
        if (name.Length == 0 || !char.IsAsciiLetter(name[0]) || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            return $"le dossier « {name} » ne convient pas comme nom de bibliothèque : lettres sans accent, chiffres et _ seulement, une lettre au début.";
        if (name.Length > LibraryCommandFile.MaxNameLength)
            return $"le nom de bibliothèque « {name} » est trop long ({LibraryCommandFile.MaxNameLength} caractères au plus).";
        return null;
    }
}
