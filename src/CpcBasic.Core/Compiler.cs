// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Output;

namespace CpcBasic.Core;

/// <summary>Résultat d'une compilation. En cas d'erreur, Disk vaut null.</summary>
/// <param name="BinarySize">Taille du binaire Z80 en octets.</param>
/// <param name="Modules">Modules de la bibliothèque liés au binaire.</param>
public sealed record CompileResult(
    IReadOnlyList<Diagnostic> Diagnostics,
    DiskImage? Disk,
    int BinarySize,
    IReadOnlyList<string> Modules)
{
    public bool Success => Disk is not null;

    /// <summary>Les autres programmes compilés en même temps et mis sur la disquette (RUN "nom", D087).</summary>
    public IReadOnlyList<LinkedProgram> Linked { get; init; } = [];

    /// <summary>Les programmes lancés par RUN "nom" qui n'ont pas été trouvés (D087).</summary>
    public IReadOnlyList<string> Missing { get; init; } = [];
}

/// <summary>Un programme trouvé pour RUN "nom" : son texte et ses réglages (D087).</summary>
public sealed record ProgramSource(string Source, CompilerOptions Options);

/// <summary>Un programme compilé en même temps que celui de l'éditeur (RUN "nom", D087).</summary>
public sealed record LinkedProgram(string Name, int BinarySize);

/// <summary>Le compilateur : analyse le programme, génère le Z80, construit la disquette.</summary>
public static class Compiler
{
    /// <summary>Adresse de chargement et d'exécution du programme dans la mémoire du CPC.</summary>
    public const int LoadAddress = 0x4000;

    /// <param name="programName">Nom des fichiers sur la disquette, 8 caractères au plus.</param>
    /// <param name="options">Réglages choisis dans l'éditeur : machine, variables sans signe (D066).</param>
    public static CompileResult CompileToDisk(string source, string programName, CompilerOptions? options = null) =>
        CompileWithLinked(source, programName, options, _ => null);

    /// <summary>
    /// Compilation en chaîne (lot J4, idée de l'utilisateur, D087) : chaque RUN "nom" écrit en clair fait compiler aussi le
    /// programme « nom » (findProgram le cherche, en général nom.BAS dans le même dossier), puis ceux qu'il lance à son
    /// tour, chacun une seule fois. Tous vont sur la même disquette, chacun avec son chargeur. Un programme introuvable
    /// est seulement signalé (CompileResult.Missing).
    /// </summary>
    public static CompileResult CompileWithLinked(string source, string programName, CompilerOptions? options,
                                                  Func<string, ProgramSource?> findProgram)
    {
        string mainName = programName.Trim().ToUpperInvariant();
        var waiting = new Queue<(string Name, string Source, CompilerOptions Options, bool Main)>();
        waiting.Enqueue((mainName, source, options ?? CompilerOptions.Default, true));
        var seen = new HashSet<string>(StringComparer.Ordinal) { mainName };
        var diagnostics = new List<Diagnostic>();
        var compiled = new List<(string Name, byte[] Code)>();
        var missing = new List<string>();
        int mainSize = 0;
        IReadOnlyList<string> mainModules = [];

        while (waiting.Count > 0)
        {
            (string name, string text, CompilerOptions settings, bool main) = waiting.Dequeue();
            (IReadOnlyList<Diagnostic> errors, byte[]? code, IReadOnlyList<string> modules, BasicProgram? program) =
                CompileOne(text, settings);
            if (main)
            {
                mainSize = code?.Length ?? 0;
                mainModules = modules;
            }
            if (errors.Count > 0)
            {
                diagnostics.AddRange(main ? errors : errors.Select(d => d with { File = $"{name}.BAS" }));
                continue;
            }
            compiled.Add((name, code!));

            foreach (string next in LaunchedPrograms(program!))
            {
                if (!seen.Add(next))
                    continue;
                ProgramSource? found = findProgram(next);
                if (found is null)
                    missing.Add(next);
                else
                    waiting.Enqueue((next, found.Source, found.Options, false));
            }
        }

        if (diagnostics.Count > 0)
            return new CompileResult(diagnostics, null, mainSize, mainModules) { Missing = missing };

        var disk = new DiskImage();
        foreach ((string name, byte[] code) in compiled)
        {
            byte[] loader = BasicLoader.Build($"{name}.BIN", LoadAddress - 1, LoadAddress);
            disk.AddFile(name, "BAS", AmsdosFileType.Basic, 0x0170, 0x0000, loader);
            disk.AddFile(name, "BIN", AmsdosFileType.Binary, LoadAddress, LoadAddress, code);
        }
        return new CompileResult([], disk, mainSize, mainModules)
        {
            Linked = compiled.Skip(1).Select(c => new LinkedProgram(c.Name, c.Code.Length)).ToList(),
            Missing = missing
        };
    }

    /// <summary>Les programmes lancés par RUN "nom" écrit en clair (sans point, ou en .BIN), en majuscules.</summary>
    public static IEnumerable<string> LaunchedPrograms(BasicProgram program)
    {
        foreach (Statement statement in program.Statements)
        {
            if (statement is not RunFileStatement { Name: StringLiteralExpression literal })
                continue;
            string name = literal.Text.Trim().ToUpperInvariant();
            int dot = name.IndexOf('.');
            if (dot >= 0)
            {
                if (name[(dot + 1)..] != "BIN")
                    continue;
                name = name[..dot];
            }
            if (name.Length is > 0 and <= 8)
                yield return name;
        }
    }

    static (IReadOnlyList<Diagnostic> Errors, byte[]? Code, IReadOnlyList<string> Modules, BasicProgram? Program)
        CompileOne(string source, CompilerOptions options)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        if (analysis.HasErrors)
            return (analysis.Diagnostics, null, [], null);

        GeneratedProgram generated = CodeGenerator.Generate(analysis.Program, LoadAddress, options);

        // MEMORY adresse : le programme (code, textes et variables) doit finir au plus tard à cette adresse
        int lastAddress = LoadAddress + generated.Code.Length - 1;
        if (generated.MemoryLimit >= 0 && lastAddress > generated.MemoryLimit)
        {
            var tooLarge = new Diagnostic(DiagnosticCode.ProgramTooLarge, generated.MemoryLine,
                                          $"{lastAddress:X4}|{generated.MemoryLimit:X4}");
            return ([tooLarge], generated.Code, generated.Modules, null);
        }
        return ([], generated.Code, generated.Modules, analysis.Program);
    }
}
