// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D181 : cartouche GX4000 au format .cpr, et la cartouche d'essai (notre propre démarrage, sans ROM).</summary>
public class CartridgeImageTests
{
    static int Long(byte[] b, int at) => b[at] | b[at + 1] << 8 | b[at + 2] << 16 | b[at + 3] << 24;

    [Fact]
    public void Le_fichier_cpr_a_l_en_tete_RIFF_et_une_partie_par_page()
    {
        var cartridge = new CartridgeImage();
        cartridge.AddPage([0xF3, 0x76]);
        cartridge.AddPage([1, 2, 3]);
        byte[] cpr = cartridge.ToCprBytes();

        Assert.Equal("RIFF", Encoding.ASCII.GetString(cpr, 0, 4));
        Assert.Equal(cpr.Length - 8, Long(cpr, 4));
        Assert.Equal("AMS!", Encoding.ASCII.GetString(cpr, 8, 4));
        Assert.Equal("cb00", Encoding.ASCII.GetString(cpr, 12, 4));
        Assert.Equal(CartridgeImage.PageSize, Long(cpr, 16));
        Assert.Equal(0xF3, cpr[20]);
        Assert.Equal(0xFF, cpr[22]);                          // page complétée par des &FF
        int second = 20 + CartridgeImage.PageSize;
        Assert.Equal("cb01", Encoding.ASCII.GetString(cpr, second, 4));
        Assert.Equal(1, cpr[second + 8]);
        Assert.Equal(12 + 2 * (8 + CartridgeImage.PageSize), cpr.Length);
    }

    [Fact]
    public void Une_page_de_plus_de_16_Ko_est_refusee()
    {
        Assert.Throws<ArgumentException>(() => new CartridgeImage().AddPage(new byte[CartridgeImage.PageSize + 1]));
    }

    [Fact]
    public void La_cartouche_d_essai_s_assemble_et_commence_par_DI()
    {
        byte[] code = Gx4000Test.Code();

        Assert.Equal(0xF3, code[0]);                          // DI
        Assert.Equal(new byte[] { 0x31, 0xF0, 0xBF }, code[1..4]);   // LD SP,&BFF0
        Assert.True(code.Length < 200);
        Assert.Equal(1, Gx4000Test.Build().PageCount);
    }

    [Fact]
    public void La_cartouche_d_essai_finit_par_les_registres_du_CRTC_et_les_encres()
    {
        byte[] code = Gx4000Test.Code();

        Assert.Equal(new byte[] { 63, 40, 46, 0x8E, 38, 0, 25, 30, 0, 7, 0, 0, 0x30, 0, 0x44, 0x4A, 0x53, 0x4C, 0x56 },
                     code[^19..]);
    }
}
