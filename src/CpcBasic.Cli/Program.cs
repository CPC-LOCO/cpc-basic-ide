// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Output;

// Utilisation : CpcBasic.Cli programme.bas [sortie.dsk]
//               CpcBasic.Cli --essai-gx4000 sortie.cpr   (cartouche d'essai GX4000, D181)
//               CpcBasic.Cli --gx4000 programme.bas [sortie.cpr]   (programme sur cartouche GX4000, D182)
if (args.Length < 1)
{
    Console.WriteLine("Utilisation : CpcBasic.Cli programme.bas [sortie.dsk]");
    Console.WriteLine("              CpcBasic.Cli --essai-gx4000 sortie.cpr");
    Console.WriteLine("              CpcBasic.Cli --gx4000 programme.bas [sortie.cpr]");
    return 1;
}

if (args[0] == "--essai-gx4000")
{
    string cprPath = args.Length >= 2 ? args[1] : "essai-gx4000.cpr";
    Gx4000Test.Build().Save(cprPath);
    Console.WriteLine($"{Path.GetFullPath(cprPath)} créé : cartouche d'essai GX4000 (4 bandes de couleur, bordure verte).");
    return 0;
}

if (args[0] == "--gx4000")
{
    if (args.Length < 2 || !File.Exists(args[1]))
    {
        Console.Error.WriteLine("Utilisation : CpcBasic.Cli --gx4000 programme.bas [sortie.cpr]");
        return 1;
    }
    string cprOut = args.Length >= 3 ? args[2] : Path.ChangeExtension(args[1], ".cpr");
    try
    {
        // RUN "nom" écrit en clair : nom.BAS du même dossier est rangé aussi dans la cartouche (D207)
        string cartFolder = Path.GetDirectoryName(Path.GetFullPath(args[1])) ?? ".";
        ProgramSource? FindCart(string program)
        {
            string? file = Directory.EnumerateFiles(cartFolder, "*.bas")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(program, StringComparison.OrdinalIgnoreCase));
            return file is null ? null : new ProgramSource(File.ReadAllText(file), CompilerOptions.Default);
        }
        CartridgeResult cartResult = Compiler.CompileToCartridge(File.ReadAllText(args[1]), null,
                                                                 Path.GetFileNameWithoutExtension(args[1]), FindCart);
        foreach (var diagnostic in cartResult.Diagnostics)
            Console.Error.WriteLine(diagnostic.Message);
        foreach (string problem in cartResult.Problems)
            Console.Error.WriteLine(problem);
        foreach (string note in cartResult.Notes)
            Console.WriteLine(note);
        if (cartResult.Cartridge is null)
        {
            Console.Error.WriteLine($"{cartResult.Diagnostics.Count + cartResult.Problems.Count} erreur(s). Aucun fichier n'a été créé.");
            return 1;
        }
        foreach (LinkedProgram linked in cartResult.Linked)
            Console.WriteLine($"Aussi rangé dans la cartouche (RUN \"{linked.Name}\") : {linked.Name}.BAS, {linked.BinarySize} octets.");
        foreach (string absent in cartResult.Missing)
            Console.WriteLine($"RUN \"{absent}\" : {absent}.BAS introuvable ; ce programme n'est pas dans la cartouche.");
        cartResult.Cartridge.Save(cprOut);
        Console.WriteLine($"{Path.GetFullPath(cprOut)} créé : cartouche GX4000, programme de {cartResult.BinarySize} octets.");
        return 0;
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
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
