// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Editing;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D122 : LIST range les lignes et les écrit comme le CPC.</summary>
public class ListerTests
{
    [Theory]
    [InlineData("print \"hello\";a$", "PRINT \"hello\";a$")]
    [InlineData("for i=1 to 10:next i", "FOR i=1 TO 10:NEXT i")]
    [InlineData("? x", "PRINT x")]
    [InlineData("poke &a400,&ff", "POKE &A400,&FF")]
    [InlineData("rem print gosub", "REM print gosub")]
    [InlineData("a=1 ' print", "a=1 ' print")]
    [InlineData("data abc,print,\"x\"", "DATA abc,print,\"x\"")]
    [InlineData("x=fndouble(2)", "x=FNdouble(2)")]
    [InlineData("a=1e5:b=mid$(c$,2)", "a=1e5:b=MID$(c$,2)")]
    [InlineData("print \"non ferme  ", "PRINT \"non ferme  ")]
    [InlineData("if a then 10 else gosub 20", "IF a THEN 10 ELSE GOSUB 20")]
    public void Ecrit_comme_le_CPC(string line, string expected) => Assert.Equal(expected, Lister.Format(line));

    [Fact]
    public void Range_par_numero_et_retire_les_lignes_vides()
    {
        var result = Lister.Apply("20 end\r\n\r\n010 cls\r\n  15   print 1\r\n");
        Assert.Null(result.Error);
        Assert.Equal("10 CLS\r\n15 PRINT 1\r\n20 END\r\n", result.Text);
        Assert.Equal(3, result.LineCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Ligne_sans_numero_gardee_a_la_fin()
    {
        var result = Lister.Apply("20 END\nprint 1\n10 CLS");
        Assert.Equal("10 CLS\n20 END\nprint 1", result.Text);
        Assert.Single(result.Warnings);
    }

    [Theory]
    [InlineData("10 CLS\n10 END\n")]
    [InlineData("print 1\n")]
    [InlineData("")]
    public void Refus(string text)
    {
        var result = Lister.Apply(text);
        Assert.NotNull(result.Error);
        Assert.Null(result.Text);
    }
}
