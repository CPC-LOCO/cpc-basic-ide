// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using CpcBasic.Core.Codegen;
using CpcBasic.Core.Libraries;
using CpcBasic.Core.Output;

namespace CpcBasic.Core.Assembly;

/// <summary>Résultat d'un programme « assembleur seul » : les octets (null s'il y a des fautes), où ils vont, où il démarre.</summary>
/// <param name="Routines">Commandes et routines des bibliothèques ajoutées après le code.</param>
public sealed record AsmProgramResult(byte[]? Code, int Origin, int Entry, IReadOnlyList<AsmError> Errors,
                                      IReadOnlyList<LibraryCommand> Routines)
{
    public bool Success => Code is not null;
}

/// <summary>
/// « Assembleur seul » (idée de l'utilisateur, D163) : un programme écrit entièrement en assembleur, à la façon de WinAPE.
/// ORG obligatoire au début (adresse de chargement, de &amp;0400 à &amp;A67B) ; RUN facultatif (adresse de lancement, sinon
/// celle de ORG) ; WRITE accepté sans effet. Les routines des bibliothèques appelées par leur nom sont ajoutées après le
/// code. Sur la disquette : NOM.BIN, et un petit chargeur NOM.BAS (MEMORY sous ORG, LOAD, CALL) : RUN"NOM" le lance,
/// et un RET à la fin revient au BASIC.
/// </summary>
public static class AsmProgram
{
    public const int MinOrigin = 0x0400;                    // au-dessous : le chargeur BASIC (&0170...) serait écrasé
    public const int MaxEnd = 0xA67B;                       // au-dessus : la mémoire du système (HIMEM du CPC)

    public static AsmProgramResult Build(string source, LibraryCatalog catalog)
    {
        var errors = new List<AsmError>();

        // 1) l'adresse de ORG (un essai à part : les adresses des étiquettes en dépendent)
        var probeInfo = new StandaloneInfo();
        IReadOnlyList<AsmError> probe = Z80Assembler.Assemble(source, new Z80Emitter(0), "p:", name => Resolve(name, catalog), null, probeInfo);
        if (probeInfo.Origin is not int origin)
        {
            // un ORG mal placé a déjà son message ; sinon, il manque
            List<AsmError> orgErrors = probe.Where(e => e.Message.StartsWith("ORG")).ToList();
            errors.AddRange(orgErrors.Count > 0 ? orgErrors
                : [new AsmError(1, "ORG manque au début du programme : il donne l'adresse où le programme est chargé (par exemple ORG &8000).")]);
            return new AsmProgramResult(null, 0, 0, errors, []);
        }
        if (origin < MinOrigin || origin > MaxEnd)
        {
            errors.Add(new AsmError(probeInfo.OriginLine,
                $"ORG &{origin:X4} : le programme doit être chargé entre &{MinOrigin:X4} et &{MaxEnd:X4} " +
                "(plus bas, il écraserait le petit chargeur BASIC ; plus haut, la mémoire du système du CPC)."));
            return new AsmProgramResult(null, origin, origin, errors, []);
        }

        // 2) le programme pour de bon, puis les routines des bibliothèques qu'il appelle
        var info = new StandaloneInfo();
        var emitter = new Z80Emitter(origin);
        var externals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        errors.AddRange(Z80Assembler.Assemble(source, emitter, "p:", name => Resolve(name, catalog), externals, info));
        var (routines, routineErrors) = LibraryLinker.Collect(catalog, externals);
        errors.AddRange(routineErrors.Select(e => new AsmError(0, e.ToString())));
        if (errors.Count > 0)
            return new AsmProgramResult(null, origin, origin, errors, routines);
        LibraryLinker.Emit(emitter, catalog, routines);

        byte[] code;
        try
        {
            code = emitter.ToBytes();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(new AsmError(0, ex.Message));
            return new AsmProgramResult(null, origin, origin, errors, routines);
        }
        int end = origin + code.Length - 1;
        if (end > MaxEnd)
        {
            errors.Add(new AsmError(0, $"le programme va de &{origin:X4} à &{end:X4} : il dépasse &{MaxEnd:X4}, la fin de la mémoire libre " +
                                       $"({end - MaxEnd} octets de trop ; un ORG plus bas peut suffire)."));
            return new AsmProgramResult(null, origin, origin, errors, routines);
        }

        int entry = origin;
        if (info.RunLine > 0)
            entry = ((info.RunLabel is null ? 0 : emitter.AddressOf(info.RunLabel)) + info.RunNumber) & 0xFFFF;
        return new AsmProgramResult(code, origin, entry, errors, routines);
    }

    /// <summary>La disquette : NOM.BAS (chargeur : MEMORY sous ORG, LOAD, CALL au lancement) et NOM.BIN.</summary>
    public static DiskImage ToDisk(AsmProgramResult program, string name)
    {
        if (program.Code is null)
            throw new InvalidOperationException("le programme a des fautes : pas de disquette.");
        string upper = name.Trim().ToUpperInvariant();
        var disk = new DiskImage();
        byte[] loader = BasicLoader.Build($"{upper}.BIN", program.Origin - 1, program.Entry);
        disk.AddFile(upper, "BAS", AmsdosFileType.Basic, 0x0170, 0x0000, loader);
        disk.AddFile(upper, "BIN", AmsdosFileType.Binary, program.Origin, program.Entry, program.Code);
        return disk;
    }

    // un nom qui n'est pas une étiquette du programme : une commande ou une routine du catalogue
    static string? Resolve(string name, LibraryCatalog catalog) =>
        catalog.Find(name) is LibraryCommand command ? LibraryLinker.EntryLabel(command.Name) : null;
}
