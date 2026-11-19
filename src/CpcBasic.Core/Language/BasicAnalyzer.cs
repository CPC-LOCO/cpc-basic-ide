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
    /// <param name="options">Réglages de la compilation (D066) ; null : réglages par défaut.</param>
    public static AnalysisResult Analyze(string source, CompilerOptions? options = null)
    {
        options ??= CompilerOptions.Default;
        var diagnostics = new List<Diagnostic>();
        // D133 : un caractère est un code de la ROM choisie, partout (ài = @i et ùDISC = |DISC sur le CPC français)
        source = CharacterSet.ToKeyboardCodes(source, options.Language);
        List<Token> tokens = Lexer.Tokenize(source, diagnostics);
        BasicProgram program = Parser.Parse(tokens, diagnostics, options.RealByDefault, options.Libraries);   // F8 (D068), bibliothèques (D148)
        CheckCharacters(tokens, diagnostics, options.Language);
        return new AnalysisResult(program, diagnostics);
    }

    // Textes entre guillemets et éléments de DATA : chaque caractère doit exister sur un CPC (D126)
    static void CheckCharacters(List<Token> tokens, List<Diagnostic> diagnostics, CpcLanguage language)
    {
        foreach (Token t in tokens.Where(t => t.Kind is TokenKind.String or TokenKind.DataItem))
        {
            foreach (char c in t.Text.Distinct())
            {
                if (CharacterSet.CodeOf(c, language) is null)
                    diagnostics.Add(new Diagnostic(DiagnosticCode.UnknownCharacter, t.Line,
                                                   $"{c}|{CharacterSet.Name(language)}|{CharacterSet.Plain(c)}"));
            }
        }
    }
}
