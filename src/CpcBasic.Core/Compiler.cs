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

    /// <summary>Les mêmes programmes sur une cassette (.cdt, D178) ; null en cas d'erreur.</summary>
    public TapeImage? Tape { get; init; }
}

/// <summary>
/// Résultat d'une compilation vers la cartouche GX4000 (D182, D183). Cartridge vaut null en cas d'erreur
/// (Diagnostics : fautes du programme ; Problems : ce qui empêche la cartouche). Notes : messages à montrer.
/// </summary>
public sealed record CartridgeResult(IReadOnlyList<Diagnostic> Diagnostics, CartridgeImage? Cartridge, int BinarySize,
                                     IReadOnlyList<string> Notes, IReadOnlyList<string> Problems);

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
        var compiled = new List<(string Name, byte[] Code, bool WithLoader)>();
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
            compiled.Add((name, code!, settings.WithLoader));

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
        var tape = new TapeImage();
        bool first = true;   // le programme de l'éditeur
        foreach ((string name, byte[] code, bool withLoader) in compiled)
        {
            if (withLoader)   // sans chargeur (D168) : NOM.BIN seul, RUN"NOM" le trouve
            {
                byte[] loader = BasicLoader.Build($"{name}.BIN", LoadAddress - 1, LoadAddress);
                disk.AddFile(name, "BAS", AmsdosFileType.Basic, 0x0170, 0x0000, loader);
            }
            disk.AddFile(name, "BIN", AmsdosFileType.Binary, LoadAddress, LoadAddress, code);

            byte[] tapeCode = withLoader ? code : TapeCopy(code);   // D179 : pas de disquette rebranchée
            if (!first)       // D180 : lancé par RUN "nom", qui cherche NOM.BIN : seul, sans chargeur
                tape.AddFile($"{name}.BIN", AmsdosFileType.Binary, LoadAddress, LoadAddress, tapeCode);
            else if (withLoader)
            {
                byte[] loader = BasicLoader.Build($"{name}.BIN", LoadAddress - 1, LoadAddress);
                tape.AddFile(name, AmsdosFileType.Basic, 0x0170, 0x0000, loader);   // D178 : RUN"" lit d'abord le chargeur
                tape.AddFile($"{name}.BIN", AmsdosFileType.Binary, LoadAddress, LoadAddress, code);
            }
            else
                tape.AddFile(name, AmsdosFileType.Binary, LoadAddress, LoadAddress, tapeCode);
            first = false;
        }
        return new CompileResult([], disk, mainSize, mainModules)
        {
            Linked = compiled.Skip(1).Select(c => new LinkedProgram(c.Name, c.Code.Length)).ToList(),
            Missing = missing,
            Tape = tape
        };
    }

    /// <summary>
    /// Cartouche GX4000 (D182) : le programme compilé (avec chargeur : pas de disquette rebranchée au départ), précédé
    /// de notre petit système (Gx4000System). Manettes (D183) : chaque touche d'INKEY(n) écrite en clair qui n'est pas
    /// dans la correspondance de base reçoit un bouton libre (Notes le dit) ; plus de bouton libre : Problems, pas de
    /// cartouche. INKEY avec un numéro calculé, INPUT et LINE INPUT : un avertissement dans Notes.
    /// </summary>
    public static CartridgeResult CompileToCartridge(string source, CompilerOptions? options = null)
    {
        CompilerOptions settings = (options ?? CompilerOptions.Default) with { WithLoader = true };
        (IReadOnlyList<Diagnostic> errors, byte[]? code, _, BasicProgram? program) = CompileOne(source, settings);
        if (errors.Count > 0)
            return new CartridgeResult(errors, null, code?.Length ?? 0, [], []);

        var notes = new List<string>();
        var problems = new List<string>();
        var links = new List<Gx4000System.KeyLink>();
        var placed = new HashSet<int>(Gx4000System.BaseKeys.Select(k => k.Key));
        foreach (Statement statement in program!.Statements)
        {
            if (statement is InputStatement or LineInputStatement)
                notes.Add($"Ligne {statement.SourceLine} : {(statement is InputStatement ? "INPUT" : "LINE INPUT")} " +
                          "attend le clavier : sur la console, seuls le tir 1 (espace) et le tir 2 (ENTRÉE) répondent pour l'instant " +
                          "(la saisie à la manette viendra à l'étape suivante).");
            foreach (Expression expression in AstWalker.All(statement))
            {
                if (expression is not CallExpression { Function: "INKEY" } inkey || inkey.Arguments.Count == 0)
                    continue;
                if (inkey.Arguments[0] is not NumberExpression { Value: int key })
                {
                    notes.Add($"Ligne {statement.SourceLine} : INKEY avec un numéro de touche calculé : seules les touches de la " +
                              "manette répondent (tir 1 = espace 47, tir 2 = ENTRÉE 18, flèches 0, 1, 2, 8, manettes 72 à 77 et 48 à 53).");
                    continue;
                }
                if (!placed.Add(key))
                    continue;
                if (links.Count >= Gx4000System.FreeButtons.Count)
                {
                    problems.Add($"Ligne {statement.SourceLine} : INKEY({key}) : plus aucun bouton libre sur les manettes pour cette touche " +
                                 $"({Gx4000System.FreeButtons.Count} touches en plus au plus). Utilisez JOY(0) ou les touches de la manette.");
                    continue;
                }
                (int joystick, int buttons, string name) = Gx4000System.FreeButtons[links.Count];
                links.Add(new Gx4000System.KeyLink(key, joystick, buttons));
                notes.Add($"Ligne {statement.SourceLine} : INKEY({key}) : sur la console, la touche {key} correspondra {name}." +
                          (joystick == 0 && buttons == 0x10 ? " Le tir 2 sert aussi de touche ENTRÉE : il « appuie » sur les deux." : ""));
            }
        }
        if (problems.Count > 0)
            return new CartridgeResult([], null, code!.Length, notes, problems);
        return new CartridgeResult([], Gx4000System.Build(code!, links), code!.Length, notes, problems);
    }

    /// <summary>
    /// Copie pour la cassette d'un programme sans chargeur (D179). Au départ, ce programme rebranche la disquette si le
    /// firmware vient d'être remis à zéro (D168) : LD A,(&amp;BC77) / CP &amp;CF / JR NZ,+9 / … / CALL &amp;BCCB. Venu d'une
    /// cassette, il ne doit pas le faire (ses fichiers resteraient sinon sur la disquette) : JR NZ devient JR, toujours
    /// pris. Le programme de la disquette ne change pas.
    /// </summary>
    public static byte[] TapeCopy(byte[] code)
    {
        byte[] copy = (byte[])code.Clone();
        if (copy.Length > 6 && copy[0] == 0x3A && copy[1] == 0x77 && copy[2] == 0xBC && copy[3] == 0xFE && copy[4] == 0xCF
            && copy[5] == 0x20)
            copy[5] = 0x18;
        return copy;
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
