// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.IO;
using System.Text.Json;

namespace CpcIde;

/// <summary>
/// Les réglages de l'éditeur qui sont mémorisés d'une fois sur l'autre : taille du texte et contraste élevé.
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
