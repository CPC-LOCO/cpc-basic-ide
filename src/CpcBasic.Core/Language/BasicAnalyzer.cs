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
        CheckEffectEnvelopes(program, diagnostics);
        return new AnalysisResult(program, diagnostics);
    }

    /// <summary>
    /// D216 : numéros d'enveloppes de chaque effet sonore (posées juste avant chaque son, chacun les siens pour que deux
    /// effets puissent jouer en même temps). Un programme ne perd que ceux des effets qu'il utilise.
    /// </summary>
    public static readonly IReadOnlyList<(string Effect, int[] Env, int[] Ent)> EffectEnvelopes =
    [
        ("VROOM", [12, 13, 14, 15], [11, 12, 13, 14, 15]),   // D213
        ("BOOM", [9, 10], [8, 9, 10]),                       // D214
        ("SHOT", [4, 5, 6, 7, 8], [6, 7]),                   // D215
        ("FALL", [2, 3], [4, 5])                             // D217
    ];

    // ENV ou ENT écrit en clair avec un numéro pris par un effet du même programme : message avec les numéros libres
    static void CheckEffectEnvelopes(BasicProgram program, List<Diagnostic> diagnostics)
    {
        var used = EffectEnvelopes.Where(e => program.Statements.Any(st => st is ScreenStatement s && s.Command == e.Effect)).ToList();
        if (used.Count == 0)
            return;
        foreach (EnvelopeStatement envelope in program.Statements.OfType<EnvelopeStatement>())
        {
            if (envelope.Number is not NumberExpression { Value: var value })
                continue;
            int number = Math.Abs(value);
            string kind = envelope.Tone ? "ENT" : "ENV";
            var taken = used.Where(e => (envelope.Tone ? e.Ent : e.Env).Contains(number)).ToList();
            if (taken.Count == 0)
                continue;
            var allTaken = used.SelectMany(e => envelope.Tone ? e.Ent : e.Env).ToHashSet();
            int[] own = envelope.Tone ? taken[0].Ent : taken[0].Env;
            diagnostics.Add(new Diagnostic(DiagnosticCode.EnvelopeTaken, envelope.SourceLine,
                $"{kind}|{number}|{taken[0].Effect}|{Ranges(own)}|{Ranges(Enumerable.Range(1, 15).Where(n => !allTaken.Contains(n)).ToArray())}"));
        }
    }

    // 1, 2, 3, 5, 6 -> « 1 à 3, 5 et 6 »
    static string Ranges(int[] numbers)
    {
        var parts = new List<string>();
        for (int i = 0; i < numbers.Length;)
        {
            int j = i;
            while (j + 1 < numbers.Length && numbers[j + 1] == numbers[j] + 1)
                j++;
            parts.Add(j == i ? $"{numbers[i]}" : j == i + 1 ? $"{numbers[i]}, {numbers[j]}" : $"{numbers[i]} à {numbers[j]}");
            i = j + 1;
        }
        return parts.Count <= 1 ? string.Concat(parts) : string.Join(", ", parts.Take(parts.Count - 1)) + " et " + parts[^1];
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
