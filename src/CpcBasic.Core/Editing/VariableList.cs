// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;

namespace CpcBasic.Core.Editing;

/// <summary>
/// Une ligne de la liste des variables. Line : ligne de l'éditeur où aller (0 : aucune) ; Warning : à vérifier ;
/// Address : « &amp;4A48 », écrit au début de Text (après ⚠), que l'éditeur met en valeur (D132), ou null.
/// </summary>
public sealed record VariableLine(string Text, int Line, bool Warning, string? Address = null);

/// <summary>
/// Liste des variables du programme (touche F4, D131, solution A de l'utilisateur) : nom, type, lignes où la variable
/// reçoit une valeur et lignes où elle est utilisée, rangés par ordre alphabétique. Signale une variable utilisée mais
/// qui ne reçoit jamais de valeur (elle vaut toujours 0 ou un texte vide : souvent une faute de frappe, avec un nom
/// proche proposé), et une variable qui reçoit une valeur mais n'est jamais utilisée.
/// Les numéros sont ceux du BASIC (10, 20...) ; pour une ligne sans numéro, c'est la ligne de l'éditeur.
/// </summary>
public static class VariableList
{
    sealed class Info
    {
        public readonly SortedSet<int> Set = new();     // lignes de l'éditeur où la variable reçoit une valeur
        public readonly SortedSet<int> Used = new();    // lignes de l'éditeur où elle est utilisée
        public string? Sizes;                           // tableau : tailles données par DIM
    }

    /// <param name="withAddresses">Ajoute l'adresse de chaque variable (D132) ; faux : seulement la solution A.</param>
    public static IReadOnlyList<VariableLine> Build(string source, CompilerOptions? options = null, bool withAddresses = true)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        string[] lines = source.Replace("\r\n", "\n").Split('\n');

        // Clé : nom écrit (signe compris), et « () » pour un tableau : A et A() sont deux variables, comme sur le CPC
        var all = new Dictionary<string, Info>();
        var bytes = new HashSet<string>();
        var hidden = new HashSet<string>();             // valeurs des fonctions DEF FN (FNDOUBLE.X) : propres à la fonction

        Info Of(string key)
        {
            if (!all.TryGetValue(key, out Info? info))
                all[key] = info = new Info();
            return info;
        }

        void UseIn(Expression expression, int line, Expression? except = null)
        {
            foreach (Expression e in AstWalker.Flatten(expression))
            {
                if (ReferenceEquals(e, except))
                    continue;
                if (e is VariableExpression v && !hidden.Contains(v.Name))
                    Of(v.Name).Used.Add(line);
                else if (e is ArrayElementExpression a)
                    Of(a.Name + "()").Used.Add(line);
            }
        }

        // Ce qui reçoit une valeur (sa case est calculée avec des variables utilisées)
        void SetTarget(Expression target, int line)
        {
            if (target is VariableExpression v)
                Of(v.Name).Set.Add(line);
            else if (target is ArrayElementExpression a)
            {
                Of(a.Name + "()").Set.Add(line);
                foreach (Expression index in a.Indexes)
                    UseIn(index, line);
            }
        }

        foreach (Statement s in analysis.Program.Statements)
            if (s is DefFnStatement def)
                foreach (string p in def.Function.Parameters)
                    hidden.Add(p);

        foreach (Statement s in analysis.Program.Statements)
        {
            int line = s.SourceLine;
            switch (s)
            {
                case AssignStatement assign:
                    Of(assign.Variable).Set.Add(line);
                    UseIn(assign.Value, line);
                    break;
                case ArrayAssignStatement array:
                    Of(array.Name + "()").Set.Add(line);
                    foreach (Expression e in array.Indexes.Append(array.Value))
                        UseIn(e, line);
                    break;
                case ForStatement loop:                         // la boucle s'en sert aussi (comparée à la limite)
                    Of(loop.Variable).Set.Add(line);
                    Of(loop.Variable).Used.Add(line);
                    foreach (Expression e in new[] { loop.Start, loop.LimitValue, loop.StepValue }.OfType<Expression>())
                        UseIn(e, line);
                    break;
                case InputStatement input:
                    foreach (Expression target in input.Targets)
                        SetTarget(target, line);
                    foreach (Expression e in new[] { input.Allowed, input.MaxLength }.OfType<Expression>())
                        UseIn(e, line);                         // ACCEPT (D223)
                    break;
                case LineInputStatement lineInput:
                    SetTarget(lineInput.Target, line);
                    break;
                case ReadStatement read:
                    foreach (Expression target in read.Targets)
                        SetTarget(target, line);
                    break;
                case MidAssignStatement mid:                    // MID$(A$, ...) = ... : A$ est changée et utilisée
                    SetTarget(mid.Target, line);
                    foreach (Expression e in AstWalker.TopLevel(mid))
                        UseIn(e, line);
                    break;
                case DimStatement dim:
                    foreach (ArrayDeclaration array in dim.Arrays)
                    {
                        Info info = Of(array.Name + "()");
                        info.Set.Add(line);
                        info.Sizes ??= string.Join(", ", array.Sizes.Select(size => size is NumberExpression n ? n.Value.ToString() : "calculée"));
                        foreach (Expression size in array.Sizes)
                            UseIn(size, line);
                    }
                    break;
                case DeclareStatement declare:
                    foreach (string name in declare.Names)
                        bytes.Add(name);
                    break;
                case DefFnStatement def when def.Function.Body is not null:
                    UseIn(def.Function.Body, line);
                    break;
                default:
                    foreach (Expression e in AstWalker.TopLevel(s))
                        UseIn(e, line);
                    break;
            }
        }

        // Numéro BASIC de la ligne de l'éditeur, s'il y en a un au début
        string Number(int editorLine)
        {
            string text = editorLine >= 1 && editorLine <= lines.Length ? lines[editorLine - 1].TrimStart() : "";
            string digits = new(text.TakeWhile(char.IsDigit).ToArray());
            return digits.Length > 0 ? digits.TrimStart('0').PadLeft(1, '0') : editorLine.ToString();
        }

        string Lines(SortedSet<int> set)
        {
            List<string> numbers = set.Select(Number).Distinct().ToList();
            return (numbers.Count == 1 ? "ligne " : "lignes ") + string.Join(", ", numbers);
        }

        string Kind(string key)
        {
            bool array = key.EndsWith("()");
            string name = array ? key[..^2] : key;
            string type = bytes.Contains(name) ? "octet (0 à 255)"
                : name.EndsWith('$') ? "texte"
                : name.EndsWith('!') ? "nombre à virgule"
                : "entier";
            if (!array)
                return type;
            string of = type switch { "texte" => "de textes", "nombre à virgule" => "de nombres à virgule", "octet (0 à 255)" => "d'octets", _ => "d'entiers" };
            Info info = all[key];
            return info.Sizes is null ? $"tableau {of}" : $"tableau {of} ({info.Sizes})";
        }

        // Solution B (D132) : adresses dans la mémoire du CPC, si le programme se compile sans erreur
        IReadOnlyDictionary<string, int>? addresses = null;
        if (withAddresses && !analysis.HasErrors)
        {
            try { addresses = CodeGenerator.Generate(analysis.Program, Compiler.LoadAddress, options).Addresses; }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException) { }
        }

        // Taille de la variable, écrite à la fin ; l'adresse est au début de la ligne (choix A de l'utilisateur :
        // les adresses alignées se voient vite)
        string Address(string key)
        {
            if (AddressText(key) is null)
                return "";
            string name = key.EndsWith("()") ? key[..^2] : key;
            string what = key.EndsWith("()") ? "fiche du tableau : adresse de ses cases, puis leur nombre"
                : bytes.Contains(name) ? "1 octet"
                : name.EndsWith('$') ? "fiche de 3 octets : longueur, puis adresse des lettres"
                : name.EndsWith('!') ? "5 octets, nombre à virgule du Locomotive"
                : "2 octets, poids faible en premier";
            return $" ({what})";
        }

        string? AddressText(string key) =>
            addresses is not null && addresses.TryGetValue(key, out int address) ? $"&{address:X4}" : null;

        string Start(string key) => AddressText(key) is string a ? a + "  " : "";

        var result = new List<VariableLine>();
        foreach (string key in all.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            Info info = all[key];
            int first = info.Set.Concat(info.Used).DefaultIfEmpty(0).Min();
            if (info.Set.Count == 0)
            {
                string empty = key.Contains('$') ? "un texte vide" : "0";
                string? near = Near(key, all.Keys.Where(k => all[k].Set.Count > 0));
                string hint = near is null ? "" : $" Faute de frappe pour {near} ?";
                result.Add(new VariableLine($"⚠ {Start(key)}{key} : {Kind(key)}. Utilisée {Lines(info.Used)}, mais ne reçoit jamais de valeur (elle vaut toujours {empty}).{hint}{Address(key)}",
                                            first, true, AddressText(key)));
            }
            else if (info.Used.Count == 0)
                result.Add(new VariableLine($"⚠ {Start(key)}{key} : {Kind(key)}. Reçoit une valeur {Lines(info.Set)}, mais n'est jamais utilisée.{Address(key)}", first, true, AddressText(key)));
            else
                result.Add(new VariableLine($"{Start(key)}{key} : {Kind(key)}. Reçoit une valeur {Lines(info.Set)} ; utilisée {Lines(info.Used)}.{Address(key)}", first, false, AddressText(key)));
        }
        return result;
    }

    // Un autre nom de la même sorte (variable ou tableau, même signe) à une lettre près, ou avec deux lettres échangées
    static string? Near(string key, IEnumerable<string> candidates)
    {
        if (key.TrimEnd('(', ')', '$', '!', '%').Length < 3)
            return null;
        foreach (string other in candidates)
        {
            if (other == key || other.EndsWith("()") != key.EndsWith("()") || Sign(other) != Sign(key))
                continue;
            if (Distance(key, other) == 1)
                return other;
        }
        return null;
    }

    static char Sign(string key)
    {
        string name = key.EndsWith("()") ? key[..^2] : key;
        return name.Length > 0 && name[^1] is '$' or '!' or '%' ? name[^1] : ' ';
    }

    // Distance d'édition avec échange de deux lettres voisines (SCOER / SCORE : 1)
    static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }
}
