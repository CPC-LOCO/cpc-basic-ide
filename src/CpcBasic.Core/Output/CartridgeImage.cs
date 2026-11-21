// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;

namespace CpcBasic.Core.Output;

/// <summary>
/// Cartouche GX4000 / CPC Plus au format .cpr (D181), lu par WinAPE, les autres émulateurs et le C4CPC.
/// Le fichier est au format RIFF : « RIFF », la taille du reste, « AMS! », puis une partie par page de 16 Ko
/// (« cb00 », « cb01 »...). Au démarrage, la console place la page 0 en &amp;0000 et la lance : il n'y a aucune ROM
/// d'Amstrad, seulement le contenu de la cartouche.
/// </summary>
public sealed class CartridgeImage
{
    /// <summary>Taille d'une page de cartouche.</summary>
    public const int PageSize = 16384;

    /// <summary>Nombre de pages au plus (512 Ko).</summary>
    public const int MaxPages = 32;

    readonly List<byte[]> pages = new();

    /// <summary>Le nombre de pages de la cartouche.</summary>
    public int PageCount => pages.Count;

    /// <summary>Ajoute une page (16 Ko au plus, complétée par des &amp;FF).</summary>
    public void AddPage(byte[] content)
    {
        if (content.Length > PageSize)
            throw new ArgumentException($"Une page de cartouche fait {PageSize} octets au plus ({content.Length} donnés).", nameof(content));
        if (pages.Count >= MaxPages)
            throw new InvalidOperationException($"Une cartouche a {MaxPages} pages au plus (512 Ko).");
        byte[] page = new byte[PageSize];
        Array.Fill(page, (byte)0xFF);
        content.CopyTo(page, 0);
        pages.Add(page);
    }

    /// <summary>Produit le contenu complet du fichier .cpr.</summary>
    public byte[] ToCprBytes()
    {
        using var ms = new MemoryStream();
        ms.Write("RIFF"u8);
        WriteLong(ms, 4 + pages.Count * (8 + PageSize));   // tout ce qui suit cette taille
        ms.Write("AMS!"u8);
        for (int p = 0; p < pages.Count; p++)
        {
            ms.Write(Encoding.ASCII.GetBytes($"cb{p:D2}"));
            WriteLong(ms, PageSize);
            ms.Write(pages[p]);
        }
        return ms.ToArray();
    }

    /// <summary>Écrit le fichier .cpr sur le disque.</summary>
    public void Save(string path) => File.WriteAllBytes(path, ToCprBytes());

    static void WriteLong(Stream s, int value)
    {
        for (int i = 0; i < 4; i++)
            s.WriteByte((byte)((value >> (8 * i)) & 0xFF));
    }
}
