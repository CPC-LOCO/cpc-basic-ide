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
    /// D218 (remplace D216) : les effets sonores prennent leurs numéros d'enveloppes dans une réserve commune : tous ceux que
    /// le programme n'écrit pas lui-même dans ses ENV et ENT (EffectsRuntime). Ici : combien chaque effet en demande au plus
    /// en même temps (ENV, ENT), pour vérifier qu'il en reste assez.
    /// </summary>
    public static readonly IReadOnlyList<(string Effect, int Env, int Ent)> EffectNeeds =
    [
        ("VROOM", 3, 3),   // D213 : la descente en 3 morceaux
        ("BOOM", 2, 1),    // D214
        ("SHOT", 2, 1),    // D215
        ("FALL", 1, 1),    // D217
        ("WAVE", 2, 1)     // D219
    ];

    /// <summary>Vrai pour une instruction d'effet sonore (VROOM, BOOM, SHOT, FALL, WAVE).</summary>
    public static bool IsSoundEffect(Statement statement) =>
        statement is ScreenStatement s && EffectNeeds.Any(e => e.Effect == s.Command);

    /// <summary>
    /// D218 : les numéros d'enveloppes (ENV, ou ENT si tone) que le programme n'écrit pas lui-même en clair, du plus grand
    /// au plus petit : la réserve des effets sonores. Un numéro calculé (ENV N, ...) ne peut pas être connu.
    /// </summary>
    public static int[] FreeEnvelopes(BasicProgram program, bool tone)
    {
        var own = program.Statements.OfType<EnvelopeStatement>()
            .Where(e => e.Tone == tone && e.Number is NumberExpression)
            .Select(e => Math.Abs(((NumberExpression)e.Number).Value)).ToHashSet();
        return Enumerable.Range(1, 15).Reverse().Where(n => !own.Contains(n)).ToArray();
    }

    // D218 : le programme écrit lui-même tant de numéros d'enveloppes qu'il n'en reste plus assez pour ses effets
    static void CheckEffectEnvelopes(BasicProgram program, List<Diagnostic> diagnostics)
    {
        var used = EffectNeeds.Where(e => program.Statements.Any(st => st is ScreenStatement s && s.Command == e.Effect)).ToList();
        if (used.Count == 0)
            return;
        foreach (bool tone in new[] { false, true })
        {
            int free = FreeEnvelopes(program, tone).Length;
            var (effect, need) = used.Select(e => (e.Effect, tone ? e.Ent : e.Env)).MaxBy(e => e.Item2);
            if (free >= need)
                continue;
            int line = program.Statements.OfType<EnvelopeStatement>().Where(e => e.Tone == tone).Select(e => e.SourceLine).DefaultIfEmpty(0).Max();
            diagnostics.Add(new Diagnostic(DiagnosticCode.EnvelopeTaken, line,
                $"{(tone ? "ENT" : "ENV")}|{15 - free}|{free}|{effect}|{need}"));
        }
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
