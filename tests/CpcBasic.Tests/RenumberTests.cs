// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Editing;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D117 : RENUM dans l'éditeur, avec les renvois corrigés.</summary>
public class RenumberTests
{
    static string Renum(string text, int start = 10, int? old = null, int step = 10)
    {
        var result = Renumber.Apply(text, start, old, step);
        Assert.Null(result.Error);
        return result.Text!;
    }

    [Fact]
    public void Numeros_et_renvois()
    {
        const string text = "5 CLS\n7 GOSUB 100\n12 IF A THEN 5 ELSE 7\n100 PRINT \"GOTO 5\":RETURN\n";
        Assert.Equal("10 CLS\n20 GOSUB 40\n30 IF A THEN 10 ELSE 20\n40 PRINT \"GOTO 5\":RETURN\n", Renum(text));
    }

    [Fact]
    public void Fins_de_ligne_Windows_gardees()
    {
        Assert.Equal("10 GOTO 20\r\n20 END\r\n", Renum("1 GOTO 2\r\n2 END\r\n"));
    }

    [Fact]
    public void ON_GOTO_ON_ERROR_EVERY_RESTORE_RESUME_RUN()
    {
        const string text =
            "1 ON X GOTO 2,3,4\n2 ON ERROR GOTO 0:ON ERROR GOTO 4\n3 EVERY 50,1 GOSUB 4:RESTORE 4:RUN 1\n4 RESUME 3\n";
        Assert.Equal(
            "100 ON X GOTO 105,110,115\n105 ON ERROR GOTO 0:ON ERROR GOTO 115\n110 EVERY 50,1 GOSUB 115:RESTORE 115:RUN 100\n115 RESUME 110\n",
            Renum(text, 100, null, 5));
    }

    [Fact]
    public void REM_DATA_et_textes_jamais_touches()
    {
        const string text = "1 REM GOTO 2\n2 DATA 1,2\n3 ' GOSUB 1\n4 PRINT 1;\"THEN 2\"\n";
        Assert.Equal("10 REM GOTO 2\n20 DATA 1,2\n30 ' GOSUB 1\n40 PRINT 1;\"THEN 2\"\n", Renum(text));
    }

    [Fact]
    public void A_partir_d_une_ancienne_ligne()
    {
        const string text = "10 GOTO 30\n20 GOTO 31\n30 END\n31 STOP\n";
        Assert.Equal("10 GOTO 1000\n20 GOTO 1010\n1000 END\n1010 STOP\n", Renum(text, 1000, 30, 10));
    }

    [Fact]
    public void Renvoi_vers_une_ligne_absente_signale()
    {
        var result = Renumber.Apply("1 GOTO 99\n2 END\n");
        Assert.Equal("10 GOTO 99\n20 END\n", result.Text);
        Assert.Single(result.Warnings);
        Assert.Contains("99", result.Warnings[0]);
    }

    [Theory]
    [InlineData("10 CLS\n10 END\n", 10, null, 10)]          // deux lignes 10
    [InlineData("10 CLS\n20 END\n", 5, 20, 10)]             // rejoindrait la ligne 10 gardée
    [InlineData("10 CLS\n20 END\n", 65530, null, 10)]       // plus de 65535
    [InlineData("10 CLS\n20 END\n", 10, 30, 10)]            // aucune ligne à partir de 30
    [InlineData("PRINT 1\n", 10, null, 10)]                 // aucune ligne numérotée
    public void Refus(string text, int start, int? old, int step)
    {
        var result = Renumber.Apply(text, start, old, step);
        Assert.NotNull(result.Error);
        Assert.Null(result.Text);
    }

    [Theory]
    [InlineData("", 10, null, 10)]
    [InlineData("100", 100, null, 10)]
    [InlineData("100,50", 100, 50, 10)]
    [InlineData("100,,5", 100, null, 5)]
    [InlineData(",,5", 10, null, 5)]
    public void Valeurs(string argument, int start, int? old, int step)
    {
        Assert.True(Renumber.TryParse(argument, out int s, out int? o, out int p));
        Assert.Equal((start, old, step), (s, o, p));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10,20,0")]
    [InlineData("A")]
    [InlineData("1,2,3,4")]
    public void Valeurs_refusees(string argument) => Assert.False(Renumber.TryParse(argument, out _, out _, out _));
}
