// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.IO;
using CpcBasic.Core.Libraries;

namespace CpcIde;

/// <summary>
/// Où sont les bibliothèques (D144, D149) :
/// - celles du projet, livrées avec l'éditeur et remplacées à chaque version : dossier « bibliotheques » à côté de CpcIde.exe ;
/// - celles du programmeur, jamais touchées par une mise à jour : Documents\CPC BASIC IDE\bibliotheques.
/// Dans chacun, un sous-dossier par bibliothèque (GRAPHIQUE...) et un fichier par commande.
/// </summary>
static class LibraryFolders
{
    public static string Project => Path.Combine(AppContext.BaseDirectory, "bibliotheques");

    public static string User => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CPC BASIC IDE", "bibliotheques");

    /// <summary>Lit les deux dossiers (à chaque compilation : une commande ajoutée sert tout de suite).</summary>
    public static LibraryCatalog Load()
    {
        try
        {
            return LibraryCatalog.Load(Project, User);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return LibraryCatalog.Empty;        // dossier illisible : le programme se compile sans bibliothèque
        }
    }
}
