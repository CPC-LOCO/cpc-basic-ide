// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>Une instruction du programme. SourceLine est la ligne de l'éditeur (à partir de 1).</summary>
public abstract record Statement(int SourceLine);

/// <summary>PRINT "texte" : affiche un texte. NewLine est faux si l'instruction se termine par « ; ».</summary>
public sealed record PrintStatement(int SourceLine, string Text, bool NewLine) : Statement(SourceLine);

/// <summary>END : fin du programme.</summary>
public sealed record EndStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Le programme complet : la suite de ses instructions.</summary>
public sealed record BasicProgram(IReadOnlyList<Statement> Statements);
