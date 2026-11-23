// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D209 : les fichiers de LOAD "nom" écrits en clair vont aussi sur la disquette (F5). examples\charge.bas.</summary>
public class DiskDataTests
{
    static byte[] Pattern(int length) => Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();

    static byte[] WithHeader(byte[] content, int load)
    {
        byte[] header = new byte[128];
        Encoding.ASCII.GetBytes("IMAGE   BIN").CopyTo(header, 1);
        header[18] = 2;
        header[21] = (byte)load; header[22] = (byte)(load >> 8);
        header[26] = (byte)load; header[27] = (byte)(load >> 8);
        header[64] = (byte)content.Length; header[65] = (byte)(content.Length >> 8);
        int sum = header[..67].Sum(b => b);
        header[67] = (byte)sum; header[68] = (byte)(sum >> 8);
        return [.. header, .. content];
    }

    static Func<string, DataFile?> Folder(params (string Name, byte[] Content)[] files) =>
        wanted => files.Where(f => f.Name.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                       .Select(f => new DataFile(f.Name, f.Content)).FirstOrDefault();

    [Fact]
    public void L_ecran_de_LOAD_va_sur_la_disquette_avec_son_adresse()
    {
        CompileResult result = Compiler.CompileWithLinked("10 LOAD \"IMAGE\",&C000", "CHARGE", null, _ => null,
                                                          Folder(("IMAGE.BIN", WithHeader(Pattern(16384), 0xC000))));

        Assert.True(result.Success);
        Assert.Contains("IMAGE.BIN", result.Disk!.FileNames);
        Assert.Equal(("IMAGE.BIN", 16384), (Assert.Single(result.Data).Name, result.Data[0].BinarySize));
        Assert.Empty(result.MissingData);
        Assert.Empty(result.DataNotes);
    }

    [Fact]
    public void Fichier_absent_et_fichier_sans_en_tete_sont_signales()
    {
        CompileResult result = Compiler.CompileWithLinked("10 LOAD \"ABSENT\":LOAD \"BRUT\"", "CHARGE", null, _ => null,
                                                          Folder(("BRUT.BIN", Pattern(100))));

        Assert.True(result.Success);
        Assert.Equal("ABSENT", Assert.Single(result.MissingData));
        Assert.Contains("BRUT.BIN", result.Disk!.FileNames);
        Assert.Contains("LOAD \"BRUT\",&C000", Assert.Single(result.DataNotes));
    }

    [Fact]
    public void Sans_recherche_rien_ne_change()
    {
        CompileResult result = Compiler.CompileToDisk("10 LOAD \"IMAGE\",&C000", "CHARGE");

        Assert.True(result.Success);
        Assert.DoesNotContain("IMAGE.BIN", result.Disk!.FileNames);
        Assert.Empty(result.MissingData);
    }
}
