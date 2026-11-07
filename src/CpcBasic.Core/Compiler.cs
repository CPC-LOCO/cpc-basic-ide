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
}

/// <summary>Le compilateur : analyse le programme, génère le Z80, construit la disquette.</summary>
public static class Compiler
{
    /// <summary>Adresse de chargement et d'exécution du programme dans la mémoire du CPC.</summary>
    public const int LoadAddress = 0x4000;

    /// <param name="programName">Nom des fichiers sur la disquette, 8 caractères au plus.</param>
    public static CompileResult CompileToDisk(string source, string programName)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        if (analysis.HasErrors)
            return new CompileResult(analysis.Diagnostics, null, 0, []);

        GeneratedProgram generated = CodeGenerator.Generate(analysis.Program, LoadAddress);

        string name = programName.Trim().ToUpperInvariant();
        byte[] loader = BasicLoader.Build($"{name}.BIN", LoadAddress - 1, LoadAddress);

        var disk = new DiskImage();
        disk.AddFile(name, "BAS", AmsdosFileType.Basic, 0x0170, 0x0000, loader);
        disk.AddFile(name, "BIN", AmsdosFileType.Binary, LoadAddress, LoadAddress, generated.Code);

        return new CompileResult([], disk, generated.Code.Length, generated.Modules);
    }
}
