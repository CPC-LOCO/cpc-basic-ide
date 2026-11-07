// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;

// Utilisation : CpcBasic.Cli programme.bas [sortie.dsk]
if (args.Length < 1)
{
    Console.WriteLine("Utilisation : CpcBasic.Cli programme.bas [sortie.dsk]");
    return 1;
}

string inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Fichier introuvable : {inputPath}");
    return 1;
}

// Le nom du programme sur la disquette est celui du fichier, en majuscules, 8 caractères au plus
string name = Path.GetFileNameWithoutExtension(inputPath).ToUpperInvariant();
if (name.Length is 0 or > 8)
{
    Console.Error.WriteLine(
        $"Le nom « {name} » ne convient pas : une disquette CPC accepte 8 caractères au plus. " +
        "Renommez le fichier, par exemple « jeu.bas ».");
    return 1;
}

string outputPath = args.Length >= 2 ? args[1] : Path.ChangeExtension(inputPath, ".dsk");

CompileResult result = Compiler.CompileToDisk(File.ReadAllText(inputPath), name);

foreach (var diagnostic in result.Diagnostics)
    Console.Error.WriteLine(diagnostic.Message);

if (!result.Success)
{
    Console.Error.WriteLine($"{result.Diagnostics.Count} erreur(s). Aucun fichier n'a été créé.");
    return 1;
}

result.Disk!.Save(outputPath);

string modules = result.Modules.Count == 0 ? "aucun" : string.Join(", ", result.Modules);
Console.WriteLine($"{outputPath} créé.");
Console.WriteLine($"Programme Z80 : {result.BinarySize} octets. Modules liés : {modules}.");
Console.WriteLine($"Dans l'émulateur : insérez le disque, puis tapez RUN\"{name}\"");
return 0;
