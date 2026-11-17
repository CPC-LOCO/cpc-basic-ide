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
}
