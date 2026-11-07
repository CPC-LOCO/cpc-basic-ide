// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>Résultat de l'analyse : le programme reconnu et la liste des problèmes trouvés.</summary>
public sealed record AnalysisResult(BasicProgram Program, IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Count > 0;
}

/// <summary>Analyse un texte BASIC complet (analyse lexicale puis syntaxique).</summary>
public static class BasicAnalyzer
{
    public static AnalysisResult Analyze(string source)
    {
        var diagnostics = new List<Diagnostic>();
        List<Token> tokens = Lexer.Tokenize(source, diagnostics);
        BasicProgram program = Parser.Parse(tokens, diagnostics);
        return new AnalysisResult(program, diagnostics);
    }
}
