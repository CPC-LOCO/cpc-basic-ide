// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Editing;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>D115 : commandes du mode direct tapées dans l'éditeur, sans numéro de ligne (D077).</summary>
public class DirectCommandTests
{
    [Theory]
    [InlineData("NEW", DirectCommandKind.New, "")]
    [InlineData("  new  ", DirectCommandKind.New, "")]
    [InlineData("edit 20", DirectCommandKind.Edit, "20")]
    [InlineData("AUTO 100,5", DirectCommandKind.Auto, "100,5")]
    [InlineData("RENUM", DirectCommandKind.Renum, "")]
    [InlineData("LIST 10-50", DirectCommandKind.List, "10-50")]
    [InlineData("DELETE 10-20", DirectCommandKind.Delete, "10-20")]
    [InlineData("memory &9000", DirectCommandKind.Explain, "&9000")]
    [InlineData("RUN\"JEU\"", DirectCommandKind.Explain, "\"JEU\"")]
    [InlineData("|dir", DirectCommandKind.Explain, "")]
    [InlineData("CHAIN MERGE \"X\"", DirectCommandKind.Explain, "MERGE \"X\"")]
    public void Commande_reconnue(string line, DirectCommandKind kind, string argument)
    {
        var command = DirectCommands.Recognize(line);
        Assert.NotNull(command);
        Assert.Equal(kind, command.Kind);
        Assert.Equal(argument, command.Argument);
        Assert.Equal(kind == DirectCommandKind.Explain, command.Explanation is not null);
    }

    [Theory]
    [InlineData("")]
    [InlineData("10 NEW")]
    [InlineData("PRINT 1")]
    [InlineData("NEWS")]
    [InlineData("LIST$")]
    [InlineData("NEW2")]
    [InlineData("|FOO")]
    [InlineData("|")]
    public void Pas_une_commande(string line) => Assert.Null(DirectCommands.Recognize(line));

    [Fact]
    public void Ligne_du_numero_demande()
    {
        const string text = "10 CLS\r\n20 PRINT 1\r\n  30 END\r\n";
        Assert.Equal(1, DirectCommands.FindLine(text, 10));
        Assert.Equal(3, DirectCommands.FindLine(text, 30));
        Assert.Null(DirectCommands.FindLine(text, 2));
        Assert.Null(DirectCommands.FindLine(text, 25));
    }

    [Theory]
    [InlineData("", 10, 10)]
    [InlineData("100", 100, 10)]
    [InlineData("100,5", 100, 5)]
    [InlineData(" 100 , 5 ", 100, 5)]
    [InlineData(",5", 10, 5)]
    public void AUTO_valeurs(string argument, int start, int step)
    {
        Assert.True(DirectCommands.TryParseAuto(argument, out int s, out int p));
        Assert.Equal(start, s);
        Assert.Equal(step, p);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("100,0")]
    [InlineData("70000")]
    [InlineData("A")]
    [InlineData("10,5,2")]
    [InlineData("-10")]
    public void AUTO_valeurs_refusees(string argument) => Assert.False(DirectCommands.TryParseAuto(argument, out _, out _));

    [Theory]
    [InlineData("100", 100, true)]
    [InlineData("  100  ", 100, true)]
    [InlineData("100 PRINT 1", 100, false)]
    [InlineData("100PRINT", 100, false)]
    public void Numero_en_debut_de_ligne(string line, int number, bool only)
    {
        Assert.Equal(number, DirectCommands.LeadingNumber(line, out bool numberOnly));
        Assert.Equal(only, numberOnly);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PRINT 1")]
    [InlineData("0")]
    [InlineData("70000")]
    public void Pas_de_numero(string line) => Assert.Null(DirectCommands.LeadingNumber(line, out _));

    [Fact]
    public void Autre_ligne_du_meme_numero()
    {
        const string text = "10 CLS\n20 END\n10\n";
        Assert.Equal(1, DirectCommands.FindLine(text, 10, exceptLine: 3));
        Assert.Equal(3, DirectCommands.FindLine(text, 10, exceptLine: 1));
    }

    [Theory]
    [InlineData("", 1, 65535)]
    [InlineData("30", 30, 30)]
    [InlineData("10-50", 10, 50)]
    [InlineData(" 10 - 50 ", 10, 50)]
    [InlineData("-50", 1, 50)]
    [InlineData("100-", 100, 65535)]
    public void Plage(string argument, int from, int to)
    {
        Assert.True(DirectCommands.TryParseRange(argument, out int f, out int t));
        Assert.Equal((from, to), (f, t));
    }

    [Theory]
    [InlineData("-")]
    [InlineData("50-10")]
    [InlineData("A")]
    [InlineData("0-10")]
    [InlineData("10,20")]
    public void Plage_refusee(string argument) => Assert.False(DirectCommands.TryParseRange(argument, out _, out _));

    [Fact]
    public void Lignes_dans_la_plage()
    {
        const string text = "10 CLS\n\n20 A=1\n30 B=2\n40 END\n";
        Assert.Equal(new[] { 3, 4 }, DirectCommands.LinesInRange(text, 15, 35));
        Assert.Equal(new[] { 1, 3, 4, 5 }, DirectCommands.LinesInRange(text, 1, 65535));
    }
}
