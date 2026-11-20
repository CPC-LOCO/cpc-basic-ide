// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using CpcBasic.Core.Language;

namespace CpcBasic.Core.Libraries;

/// <summary>
/// Ce qu'il faut déposer dans un « répertoire de distribution » (idée de l'utilisateur, D144, D161) : les programmes
/// (celui de l'éditeur et ceux qu'il lance par RUN "nom", comme F5), et les commandes ajoutées par le programmeur qu'ils
/// utilisent, avec les routines qu'elles appellent. Les commandes du projet n'y sont pas : tout le monde les a.
/// </summary>
/// <param name="Programs">Noms des programmes, le premier est celui de l'éditeur.</param>
/// <param name="Commands">Commandes, fonctions et routines du programmeur à donner avec.</param>
/// <param name="ProjectCommands">Commandes du projet utilisées (pour le dire, pas à copier).</param>
/// <param name="Missing">Programmes lancés par RUN "nom" qui n'ont pas été trouvés.</param>
public sealed record DistributionPlan(IReadOnlyList<string> Programs, IReadOnlyList<LibraryCommand> Commands,
                                      IReadOnlyList<LibraryCommand> ProjectCommands, IReadOnlyList<string> Missing);

public static class Distribution
{
    /// <summary>
    /// Fait la liste. <paramref name="findProgram"/> trouve un programme lancé par RUN "nom" (comme pour F5) ;
    /// le catalogue est celui des réglages (<see cref="CompilerOptions.Libraries"/>).
    /// </summary>
    public static DistributionPlan Plan(string mainName, string source, CompilerOptions options,
                                        Func<string, ProgramSource?> findProgram)
    {
        LibraryCatalog catalog = options.Libraries ?? LibraryCatalog.Empty;
        var programs = new List<string>();
        var missing = new List<string>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var waiting = new Queue<(string Name, string Source, CompilerOptions Options)>();
        string first = mainName.Trim().ToUpperInvariant();
        waiting.Enqueue((first, source, options));
        var seen = new HashSet<string>(StringComparer.Ordinal) { first };

        while (waiting.Count > 0)
        {
            (string name, string text, CompilerOptions settings) = waiting.Dequeue();
            programs.Add(name);
            BasicProgram program = BasicAnalyzer.Analyze(text, settings with { Libraries = catalog }).Program;
            foreach (Statement statement in program.Statements)
            {
                if (statement is LibraryCallStatement call)
                    used.Add(call.Name);
                foreach (LibraryCallExpression function in AstWalker.All(statement).OfType<LibraryCallExpression>())
                    used.Add(function.Name);
            }
            foreach (string next in Compiler.LaunchedPrograms(program))
            {
                if (!seen.Add(next))
                    continue;
                ProgramSource? found = findProgram(next);
                if (found is null)
                    missing.Add(next);
                else
                    waiting.Enqueue((next, found.Source, found.Options));
            }
        }

        IReadOnlyList<LibraryCommand> all = LibraryLinker.Collect(catalog, used).Commands;
        return new DistributionPlan(programs, all.Where(c => !c.Protected).ToList(), all.Where(c => c.Protected).ToList(), missing);
    }
}
