// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CpcBasic.Core;

namespace CpcIde;

/// <summary>
/// Les réglages de l'éditeur qui sont mémorisés d'une fois sur l'autre : taille du texte, contraste élevé,
/// programmes dont les variables sans signe sont des nombres à virgule (D066). La machine n'est plus un réglage (D111).
/// Ils sont écrits dans le fichier  %APPDATA%\CpcBasicIde\reglages.json  (dossier de l'utilisateur Windows).
/// Si le fichier manque ou est abîmé, les valeurs par défaut sont utilisées, sans message d'erreur.
/// </summary>
public sealed class EditorSettings
{
    public const int MinFontSize = 10;
    public const int MaxFontSize = 48;
    public const int DefaultFontSize = 16;

    /// <summary>Taille du texte de l'éditeur (en points).</summary>
    public int FontSize { get; set; } = DefaultFontSize;

    /// <summary>Vrai si le thème à fort contraste (noir et blanc) est choisi.</summary>
    public bool HighContrast { get; set; }


    /// <summary>
    /// Programmes (chemins complets des fichiers .bas) dont les variables sans signe sont des nombres à virgule,
    /// comme le Locomotive (D066). Les autres gardent des entiers. Rien n'est écrit dans le fichier .bas lui-même.
    /// </summary>
    public List<string> RealByDefaultFiles { get; set; } = new();

    /// <summary>
    /// Programmes (chemins complets) dont la touche ESC est ignorée (D083). Les autres : ESC comme le vrai BASIC
    /// (pause, puis « Break in N »), le choix par défaut.
    /// </summary>
    public List<string> BreakIgnoredFiles { get; set; } = new();

    /// <summary>
    /// Vrai si F5 met aussi le texte du programme sur la disquette (NOM.TXT), pour le lancer avec le vrai BASIC :
    /// LOAD"NOM.TXT" puis RUN (D101). Réglage commun à tous les programmes, comme la machine.
    /// </summary>
    public bool TextOnDisk { get; set; }

    // Une ligne trop longue continue sur la ligne suivante de l'écran, comme sur le CPC (D113)
    public bool WordWrap { get; set; } = true;

    static string Key(string path) => Path.GetFullPath(path).ToUpperInvariant();

    public bool IsBreakIgnored(string path) => BreakIgnoredFiles.Contains(Key(path));

    public void SetBreakIgnored(string path, bool ignored)
    {
        BreakIgnoredFiles.Remove(Key(path));
        if (ignored)
            BreakIgnoredFiles.Add(Key(path));
    }

    public bool IsRealByDefault(string path) => RealByDefaultFiles.Contains(Key(path));

    public void SetRealByDefault(string path, bool real)
    {
        RealByDefaultFiles.Remove(Key(path));
        if (real)
            RealByDefaultFiles.Add(Key(path));
    }

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CpcBasicIde",
        "reglages.json");

    public static EditorSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(FilePath));
                if (settings is not null)
                {
                    settings.FontSize = Math.Clamp(settings.FontSize, MinFontSize, MaxFontSize);
                    settings.RealByDefaultFiles ??= new();
                    settings.BreakIgnoredFiles ??= new();
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Fichier illisible : on repart des valeurs par défaut.
        }

        return new EditorSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Impossible d'écrire : le réglage ne sera pas mémorisé, mais l'éditeur continue.
        }
    }
}
