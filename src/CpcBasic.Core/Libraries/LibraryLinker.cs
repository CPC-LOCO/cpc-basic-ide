// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Core.Libraries;

/// <summary>Un problème dans le code assembleur d'une commande de bibliothèque utilisée par le programme.</summary>
public sealed record LibraryCodeError(string Command, string Library, int Line, string Message)
{
    public override string ToString() => $"{Command} (bibliothèque {Library}), ligne {Line} de son fichier : {Message}";
}

/// <summary>
/// Liaison des bibliothèques (D144, D148) : comme pour les modules du compilateur, seules les commandes utilisées
/// par le programme sont ajoutées, avec les routines qu'elles appellent (et celles que ces routines appellent...).
/// Chaque commande ou fonction reçoit ses cases de valeurs (2 octets chacune, nommées comme au questionnaire) ;
/// le programme y range les valeurs puis appelle la routine. Une fonction rend son résultat dans HL.
/// </summary>
public static class LibraryLinker
{
    /// <summary>Étiquette d'entrée d'une commande, fonction ou routine.</summary>
    public static string EntryLabel(string name) => "lb:" + name.ToLowerInvariant();

    /// <summary>Étiquette de la case d'une valeur.</summary>
    public static string CellLabel(string command, string parameter) =>
        $"lb:{command.ToLowerInvariant()}:v:{parameter.ToLowerInvariant()}";

    // préfixe des étiquettes écrites dans le code de la commande ; « : » ne peut pas être dans un nom, donc pas de mélange
    static string LocalPrefix(string name) => $"lb:{name.ToLowerInvariant()}:l:";

    /// <summary>
    /// Les commandes à ajouter au programme : celles qu'il utilise (<paramref name="used"/>) et toutes celles qu'elles appellent,
    /// dans un ordre fixe (le même programme donne toujours le même binaire). Les fautes du code assembleur sont rendues
    /// dans <c>Errors</c> (le code est vérifié ici, avant la compilation du programme).
    /// </summary>
    public static (IReadOnlyList<LibraryCommand> Commands, IReadOnlyList<LibraryCodeError> Errors) Collect(
        LibraryCatalog catalog, IEnumerable<string> used)
    {
        var found = new Dictionary<string, LibraryCommand>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<LibraryCodeError>();
        var pending = new Queue<string>(used);

        while (pending.Count > 0)
        {
            string name = pending.Dequeue();
            if (found.ContainsKey(name))
                continue;
            LibraryCommand? command = catalog.Find(name);
            if (command is null)
                continue;                       // le parseur a déjà signalé le mot inconnu
            found[command.Name] = command;

            // essai sur un émetteur jetable : fautes et routines appelées
            var externals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<AsmError> problems = Assemble(new Z80Emitter(0), command, catalog, externals);
            errors.AddRange(problems.Select(p => new LibraryCodeError(command.Name, command.Library, p.Line, p.Message)));
            foreach (string external in externals)
                if (!command.Parameters.Any(p => p.Name.Equals(external, StringComparison.OrdinalIgnoreCase)))
                    pending.Enqueue(external);
        }
        return (found.Values.OrderBy(c => c.Name, StringComparer.Ordinal).ToList(), errors);
    }

    /// <summary>Écrit les commandes dans le programme : cases des valeurs, puis le code.</summary>
    public static void Emit(Z80Emitter emitter, LibraryCatalog catalog, IEnumerable<LibraryCommand> commands)
    {
        foreach (LibraryCommand command in commands)
        {
            foreach (LibraryParameter parameter in command.Parameters)
            {
                emitter.Label(CellLabel(command.Name, parameter.Name));
                emitter.Reserve(2);
            }
            IReadOnlyList<AsmError> problems = Assemble(emitter, command, catalog, null);
            if (problems.Count > 0)             // déjà vérifié par Collect : ne devrait pas arriver
                throw new InvalidOperationException($"{command.Name} : {problems[0]}");
        }
    }

    static IReadOnlyList<AsmError> Assemble(Z80Emitter emitter, LibraryCommand command, LibraryCatalog catalog,
                                            ISet<string>? externals)
    {
        emitter.Label(EntryLabel(command.Name));
        return Z80Assembler.Assemble(command.Code, emitter, LocalPrefix(command.Name), name =>
        {
            // une valeur de la commande : sa case ; sinon une autre commande ou routine du catalogue
            if (command.Parameters.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return CellLabel(command.Name, name);
            LibraryCommand? other = catalog.Find(name);
            return other is null ? null : EntryLabel(other.Name);
        }, externals);
    }
}
