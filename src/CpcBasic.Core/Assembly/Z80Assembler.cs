// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using System.Globalization;
using System.Text;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Core.Assembly;

/// <summary>Une erreur trouvée dans le texte assembleur : numéro de ligne (à partir de 1) et explication.</summary>
public sealed record AsmError(int Line, string Message)
{
    public override string ToString() => $"ligne {Line} : {Message}";
}

/// <summary>
/// Assembleur Z80 des bibliothèques (D144, D145) : lit du texte écrit comme pour l'assembleur de WinAPE (style Maxam)
/// et le transforme en instructions pour <see cref="Z80Emitter"/>, qui calcule les adresses à la fin. Il n'y a donc
/// pas d'ORG : le compilateur place la routine. Les étiquettes de la routine reçoivent un préfixe (deux routines peuvent
/// avoir une étiquette BOUCLE) ; un nom qui n'est pas une étiquette de la routine est demandé à <c>resolve</c>
/// (routine d'une bibliothèque, case d'une valeur, routine du projet...).
///
/// Écriture acceptée :
/// - une ligne : [étiquette[:]] [instruction] [; commentaire] ; l'étiquette est au début de la ligne (ou suivie de « : ») ;
///   majuscules et minuscules sans différence ;
/// - nombres : 123, &amp;FF ou #FF (hexadécimal), %1010 (binaire), 'A' (code d'un caractère), $ (adresse de l'instruction) ;
///   calculs avec + - * / et des parenthèses ; une étiquette ne peut qu'être augmentée ou diminuée d'un nombre ;
/// - directives : NOM EQU valeur, DEFB / DB / BYTE (nombres et textes "..."), DEFW / DW / WORD, DEFS / DS taille[,valeur],
///   DEFM / TEXT (textes) ;
/// - toutes les instructions documentées du Z80, avec IX et IY ; RST avec l'adresse (&amp;38) ou le numéro (7) ;
///   EX HL,DE accepté comme EX DE,HL (comme WinAPE).
/// Refusés avec un message : ORG, READ / INCLUDE, macros, ALIGN.
/// </summary>
public static class Z80Assembler
{
    // ---------------------------------------------------------------- valeurs

    /// <summary>Résultat d'un calcul : un nombre, ou une étiquette plus un nombre.</summary>
    readonly record struct Value(string? Label, int Number)
    {
        public bool IsConstant => Label is null;
    }

    sealed class AsmException(string message) : Exception(message);

    static readonly HashSet<string> Mnemonics =
    [
        "LD", "PUSH", "POP", "EX", "EXX", "LDI", "LDIR", "LDD", "LDDR", "CPI", "CPIR", "CPD", "CPDR",
        "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP", "INC", "DEC", "DAA", "CPL", "NEG", "CCF", "SCF",
        "NOP", "HALT", "DI", "EI", "IM", "RLCA", "RLA", "RRCA", "RRA", "RLC", "RL", "RRC", "RR", "SLA", "SRA", "SRL",
        "SLL", "SL1", "RLD", "RRD", "BIT", "SET", "RES", "JP", "JR", "DJNZ", "CALL", "RET", "RETI", "RETN", "RST",
        "IN", "INI", "INIR", "IND", "INDR", "OUT", "OUTI", "OTIR", "OUTD", "OTDR",
        "EQU", "DEFB", "DB", "BYTE", "DEFW", "DW", "WORD", "DEFS", "DS", "DEFM", "TEXT",
        "ORG", "READ", "INCLUDE", "MACRO", "MEND", "ENDM", "ALIGN", "END", "NOLIST", "LIST"
    ];

    static readonly Dictionary<string, int> Reg8 = new()
    {
        ["B"] = 0, ["C"] = 1, ["D"] = 2, ["E"] = 3, ["H"] = 4, ["L"] = 5, ["A"] = 7
    };

    static readonly Dictionary<string, int> Conditions = new()
    {
        ["NZ"] = 0, ["Z"] = 1, ["NC"] = 2, ["C"] = 3, ["PO"] = 4, ["PE"] = 5, ["P"] = 6, ["M"] = 7
    };

    // ---------------------------------------------------------------- opérandes

    enum Kind { R8, R16, Index, HlMem, IndexMem, BcMem, DeMem, SpMem, CMem, AfAlt, I, R, Mem, Imm }

    /// <summary>Un opérande : registre, case mémoire ou valeur. Prefix : &amp;DD (IX) ou &amp;FD (IY), sinon 0.</summary>
    sealed record Operand(Kind Kind, int Code, int Prefix, string? Expression, string Text);

    sealed class Line
    {
        public int Number;
        public string? Label;
        public string? Mnemonic;
        public List<string> Operands = [];
    }

    // ---------------------------------------------------------------- entrée

    /// <summary>
    /// Assemble <paramref name="source"/> dans <paramref name="emitter"/>. <paramref name="prefix"/> est ajouté aux étiquettes
    /// de la routine ; <paramref name="resolve"/> rend l'étiquette de l'émetteur pour un nom extérieur (ou null s'il est inconnu).
    /// Rend la liste des erreurs (vide si tout va bien) ; les noms extérieurs utilisés vont dans <paramref name="externals"/>.
    /// </summary>
    public static IReadOnlyList<AsmError> Assemble(string source, Z80Emitter emitter, string prefix,
                                                    Func<string, string?> resolve, ISet<string>? externals = null)
    {
        var errors = new List<AsmError>();
        var lines = new List<Line>();
        var locals = new HashSet<string>();
        var equs = new Dictionary<string, (string Text, int Line)>();

        // 1er passage : découpage des lignes, étiquettes de la routine, EQU
        string[] texts = source.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < texts.Length; i++)
        {
            try
            {
                Line? line = Split(texts[i], i + 1);
                if (line is null)
                    continue;
                if (line.Mnemonic == "EQU")
                {
                    if (line.Label is null || line.Operands.Count != 1)
                        throw new AsmException("EQU s'écrit NOM EQU valeur.");
                    if (!equs.TryAdd(line.Label, (line.Operands[0], line.Number)) || locals.Contains(line.Label))
                        throw new AsmException($"le nom {line.Label} est déjà défini.");
                    continue;
                }
                if (line.Label is not null && (!locals.Add(line.Label) || equs.ContainsKey(line.Label)))
                    throw new AsmException($"l'étiquette {line.Label} est déjà définie.");
                lines.Add(line);
            }
            catch (AsmException e)
            {
                errors.Add(new AsmError(i + 1, e.Message));
            }
        }

        var context = new Context(emitter, prefix, resolve, externals, locals, equs);

        // 2e passage : les instructions
        foreach (Line line in lines)
        {
            try
            {
                context.LineNumber = line.Number;
                context.Here = null;
                if (line.Label is not null)
                    emitter.Label(prefix + line.Label);
                if (line.Mnemonic is not null)
                    Encode(line, context);
            }
            catch (AsmException e)
            {
                errors.Add(new AsmError(line.Number, e.Message));
            }
        }
        return errors;
    }

    // ---------------------------------------------------------------- découpage d'une ligne

    static Line? Split(string text, int number)
    {
        // commentaire : après un « ; » qui n'est pas dans un texte
        var clean = new StringBuilder();
        char? quote = null;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quote is null && ch == ';')
                break;
            if (ch is '"' or '\'' && !(quote is null && IsPrime(text, i)))
            {
                if (quote is null)
                    quote = ch;
                else if (quote == ch)
                    quote = null;
            }
            clean.Append(ch);
        }
        string body = clean.ToString().TrimEnd();
        if (body.Trim().Length == 0)
            return null;

        var line = new Line { Number = number };
        bool startsAtLeft = !char.IsWhiteSpace(body[0]);
        string rest = body.Trim();

        // « nom: » n'importe où, ou un mot au début de la ligne qui n'est pas une instruction
        string first = FirstWord(rest);
        if (first.EndsWith(':'))
        {
            line.Label = CheckName(first[..^1]);
            rest = rest[first.Length..].Trim();
        }
        else if (startsAtLeft && first.Length > 0 && !Mnemonics.Contains(first.ToUpperInvariant()))
        {
            line.Label = CheckName(first);
            rest = rest[first.Length..].Trim();
        }
        else
        {
            // « NOM EQU valeur » même décalé
            string[] words = rest.Split(new[] { ' ', '\t' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2 && words[1].Equals("EQU", StringComparison.OrdinalIgnoreCase))
            {
                line.Label = CheckName(words[0]);
                rest = rest[words[0].Length..].Trim();
            }
        }
        if (rest.Length == 0)
            return line;

        string mnemonic = FirstWord(rest);
        line.Mnemonic = mnemonic.ToUpperInvariant();
        if (!Mnemonics.Contains(line.Mnemonic))
            throw new AsmException($"« {mnemonic} » n'est pas une instruction du Z80.");
        line.Operands = SplitOperands(rest[mnemonic.Length..].Trim());
        return line;
    }

    // l'apostrophe de AF' n'ouvre pas un caractère
    static bool IsPrime(string text, int i) =>
        text[i] == '\'' && i >= 2 && char.ToUpperInvariant(text[i - 1]) == 'F' && char.ToUpperInvariant(text[i - 2]) == 'A';

    static string FirstWord(string text)
    {
        int end = 0;
        while (end < text.Length && !char.IsWhiteSpace(text[end]))
            end++;
        return text[..end];
    }

    static string CheckName(string name)
    {
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] is '_' or '.')
            || !name.All(c => char.IsLetterOrDigit(c) || c is '_' or '.'))
            throw new AsmException($"« {name} » ne peut pas servir d'étiquette (lettres, chiffres et _ seulement, une lettre au début).");
        return name.ToUpperInvariant();
    }

    // virgules hors des parenthèses et des textes
    static List<string> SplitOperands(string text)
    {
        var result = new List<string>();
        if (text.Length == 0)
            return result;
        int depth = 0;
        char? quote = null;
        var current = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quote is not null)
            {
                if (ch == quote)
                    quote = null;
                current.Append(ch);
                continue;
            }
            if (ch is '"' or '\'' && !IsPrime(text, i))
                quote = ch;
            else if (ch == '(')
                depth++;
            else if (ch == ')')
                depth--;
            else if (ch == ',' && depth == 0)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }
        result.Add(current.ToString().Trim());
        if (result.Any(r => r.Length == 0))
            throw new AsmException("une valeur manque entre deux virgules.");
        return result;
    }

    // ---------------------------------------------------------------- calculs

    sealed class Context(Z80Emitter emitter, string prefix, Func<string, string?> resolve, ISet<string>? externals,
                         HashSet<string> locals, Dictionary<string, (string Text, int Line)> equs)
    {
        public readonly Z80Emitter Emitter = emitter;
        public int LineNumber;
        public string? Here;                 // étiquette « $ » de l'instruction en cours
        int hereCount;
        readonly HashSet<string> evaluating = [];

        public string HereLabel()
        {
            Here ??= $"{prefix}$${LineNumber}_{hereCount++}";
            return Here;
        }

        public Value Name(string name)
        {
            string upper = name.ToUpperInvariant();
            if (equs.TryGetValue(upper, out var equ))
            {
                if (!evaluating.Add(upper))
                    throw new AsmException($"le nom {upper} se définit par lui-même (EQU).");
                try { return Evaluate(equ.Text, this); }
                finally { evaluating.Remove(upper); }
            }
            if (locals.Contains(upper))
                return new Value(prefix + upper, 0);
            string? external = resolve(upper);
            if (external is null)
                throw new AsmException($"le nom {upper} n'est défini nulle part (ni étiquette de la routine, ni routine d'une bibliothèque).");
            externals?.Add(upper);
            return new Value(external, 0);
        }

        public void PlaceHere()
        {
            if (Here is not null)
                Emitter.Label(Here);
        }
    }

    static Value Evaluate(string text, Context context)
    {
        int pos = 0;
        Value value = Sum(text, ref pos, context);
        SkipSpaces(text, ref pos);
        if (pos < text.Length)
            throw new AsmException($"« {text} » : calcul mal écrit près de « {text[pos..]} ».");
        return value;
    }

    static void SkipSpaces(string text, ref int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            pos++;
    }

    static Value Sum(string text, ref int pos, Context context)
    {
        Value left = Product(text, ref pos, context);
        while (true)
        {
            SkipSpaces(text, ref pos);
            if (pos >= text.Length || text[pos] is not ('+' or '-'))
                return left;
            char op = text[pos++];
            Value right = Product(text, ref pos, context);
            if (op == '+')
            {
                if (left.Label is not null && right.Label is not null)
                    throw new AsmException("on ne peut pas additionner deux étiquettes.");
                left = new Value(left.Label ?? right.Label, left.Number + right.Number);
            }
            else
            {
                if (right.Label is not null)
                {
                    if (left.Label != right.Label)
                        throw new AsmException("une étiquette ne peut être soustraite que d'elle-même (écart entre deux étiquettes : pas encore).");
                    left = new Value(null, left.Number - right.Number);
                }
                else
                    left = new Value(left.Label, left.Number - right.Number);
            }
        }
    }

    static Value Product(string text, ref int pos, Context context)
    {
        Value left = Unary(text, ref pos, context);
        while (true)
        {
            SkipSpaces(text, ref pos);
            if (pos >= text.Length || text[pos] is not ('*' or '/'))
                return left;
            char op = text[pos++];
            Value right = Unary(text, ref pos, context);
            if (!left.IsConstant || !right.IsConstant)
                throw new AsmException("une étiquette ne peut pas être multipliée ou divisée.");
            if (op == '/' && right.Number == 0)
                throw new AsmException("division par zéro.");
            left = new Value(null, op == '*' ? left.Number * right.Number : left.Number / right.Number);
        }
    }

    static Value Unary(string text, ref int pos, Context context)
    {
        SkipSpaces(text, ref pos);
        if (pos < text.Length && text[pos] == '-')
        {
            pos++;
            Value v = Unary(text, ref pos, context);
            if (!v.IsConstant)
                throw new AsmException("le signe moins ne peut pas porter sur une étiquette.");
            return new Value(null, -v.Number);
        }
        if (pos < text.Length && text[pos] == '+')
        {
            pos++;
            return Unary(text, ref pos, context);
        }
        return Primary(text, ref pos, context);
    }

    static Value Primary(string text, ref int pos, Context context)
    {
        SkipSpaces(text, ref pos);
        if (pos >= text.Length)
            throw new AsmException("une valeur manque.");
        char ch = text[pos];
        if (ch == '(')
        {
            pos++;
            Value v = Sum(text, ref pos, context);
            SkipSpaces(text, ref pos);
            if (pos >= text.Length || text[pos] != ')')
                throw new AsmException($"« {text} » : parenthèse fermante manquante.");
            pos++;
            return v;
        }
        if (ch == '\'' || ch == '"')
        {
            if (pos + 2 < text.Length && text[pos + 2] == ch)
            {
                int code = text[pos + 1];
                pos += 3;
                return new Value(null, code);
            }
            throw new AsmException($"« {text} » : un caractère s'écrit entre apostrophes, par exemple 'A'.");
        }
        if (ch == '$' && (pos + 1 >= text.Length || !IsHexDigit(text[pos + 1])))
        {
            pos++;
            return new Value(context.HereLabel(), 0);
        }
        if (ch is '&' or '#' or '$')
        {
            pos++;
            if (ch == '&' && pos < text.Length && char.ToUpperInvariant(text[pos]) == 'X')
            {
                pos++;
                return new Value(null, Digits(text, ref pos, 2));       // &X1010 : binaire (Locomotive)
            }
            return new Value(null, Digits(text, ref pos, 16));
        }
        if (ch == '%')
        {
            pos++;
            return new Value(null, Digits(text, ref pos, 2));
        }
        if (char.IsDigit(ch))
        {
            int start = pos;
            while (pos < text.Length && char.IsLetterOrDigit(text[pos]))
                pos++;
            string word = text[start..pos].ToUpperInvariant();
            if (word.EndsWith('H'))
                return new Value(null, Parse(word[..^1], 16, text));
            if (word.EndsWith('B') && word[..^1].All(c => c is '0' or '1'))
                return new Value(null, Parse(word[..^1], 2, text));
            return new Value(null, Parse(word, 10, text));
        }
        if (char.IsLetter(ch) || ch is '_' or '.')
        {
            int start = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] is '_' or '.'))
                pos++;
            return context.Name(text[start..pos]);
        }
        throw new AsmException($"« {text} » : valeur mal écrite.");
    }

    static bool IsHexDigit(char c) => Uri.IsHexDigit(c);

    static int Digits(string text, ref int pos, int radix)
    {
        int start = pos;
        while (pos < text.Length && (radix == 16 ? IsHexDigit(text[pos]) : text[pos] is '0' or '1'))
            pos++;
        if (pos == start)
            throw new AsmException($"« {text} » : chiffres manquants.");
        return Parse(text[start..pos], radix, text);
    }

    static int Parse(string digits, int radix, string text)
    {
        try
        {
            long value = radix == 10 ? long.Parse(digits, CultureInfo.InvariantCulture) : Convert.ToInt64(digits, radix);
            if (value > 0xFFFF)
                throw new AsmException($"« {text} » : nombre trop grand (65535 au plus).");
            return (int)value;
        }
        catch (FormatException)
        {
            throw new AsmException($"« {text} » : nombre mal écrit.");
        }
        catch (OverflowException)
        {
            throw new AsmException($"« {text} » : nombre trop grand (65535 au plus).");
        }
    }

    static int Constant(string expression, Context context, int min, int max, string what)
    {
        Value v = Evaluate(expression, context);
        if (!v.IsConstant)
            throw new AsmException($"{what} doit être un nombre, pas une étiquette (« {expression} »).");
        if (v.Number < min || v.Number > max)
            throw new AsmException($"{what} doit aller de {min} à {max} (« {expression} » vaut {v.Number}).");
        return v.Number;
    }

    // ---------------------------------------------------------------- opérandes

    static Operand Classify(string text)
    {
        string upper = text.ToUpperInvariant().Replace(" ", "").Replace("\t", "");
        if (Reg8.TryGetValue(upper, out int r))
            return new Operand(Kind.R8, r, 0, null, text);
        switch (upper)
        {
            case "BC": return new Operand(Kind.R16, 0, 0, null, text);
            case "DE": return new Operand(Kind.R16, 1, 0, null, text);
            case "HL": return new Operand(Kind.R16, 2, 0, null, text);
            case "SP": return new Operand(Kind.R16, 3, 0, null, text);
            case "AF": return new Operand(Kind.R16, 4, 0, null, text);
            case "IX": return new Operand(Kind.Index, 2, 0xDD, null, text);
            case "IY": return new Operand(Kind.Index, 2, 0xFD, null, text);
            case "AF'": return new Operand(Kind.AfAlt, 0, 0, null, text);
            case "I": return new Operand(Kind.I, 0, 0, null, text);
            case "R": return new Operand(Kind.R, 0, 0, null, text);
            case "(HL)": return new Operand(Kind.HlMem, 6, 0, null, text);
            case "(BC)": return new Operand(Kind.BcMem, 0, 0, null, text);
            case "(DE)": return new Operand(Kind.DeMem, 0, 0, null, text);
            case "(SP)": return new Operand(Kind.SpMem, 0, 0, null, text);
            case "(C)": return new Operand(Kind.CMem, 0, 0, null, text);
        }
        if (IsWrapped(text))
        {
            string inner = text.Trim()[1..^1].Trim();
            string innerUpper = inner.ToUpperInvariant();
            foreach ((string name, int prefix) in new[] { ("IX", 0xDD), ("IY", 0xFD) })
            {
                if (innerUpper == name)
                    return new Operand(Kind.IndexMem, 6, prefix, "0", text);
                if (innerUpper.StartsWith(name) && innerUpper.Length > 2 && innerUpper[2..].TrimStart() is { Length: > 0 } after && after[0] is '+' or '-')
                {
                    string displacement = inner[2..].TrimStart();
                    if (displacement[0] == '+')
                        displacement = displacement[1..];
                    return new Operand(Kind.IndexMem, 6, prefix, displacement, text);
                }
            }
            return new Operand(Kind.Mem, 0, 0, inner, text);
        }
        return new Operand(Kind.Imm, 0, 0, text, text);
    }

    // « (...) » qui entoure tout l'opérande
    static bool IsWrapped(string text)
    {
        string t = text.Trim();
        if (t.Length < 2 || t[0] != '(' || t[^1] != ')')
            return false;
        int depth = 0;
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == '(')
                depth++;
            else if (t[i] == ')')
                depth--;
            if (depth == 0 && i < t.Length - 1)
                return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- écriture

    sealed class Writer(Context context)
    {
        public readonly Context Context = context;
        readonly List<Action> parts = [];
        public bool HasParts => parts.Count > 0;

        public Writer Byte(int b) { parts.Add(() => Context.Emitter.Bytes((byte)b)); return this; }

        public Writer Prefix(int prefix) => prefix == 0 ? this : Byte(prefix);

        public Writer Imm8(string expression)
        {
            Value v = Evaluate(expression, Context);
            if (!v.IsConstant)
                throw new AsmException($"« {expression} » : une étiquette ne tient pas dans un octet.");
            if (v.Number < -128 || v.Number > 255)
                throw new AsmException($"« {expression} » vaut {v.Number} : un octet va de -128 à 255.");
            return Byte(v.Number & 0xFF);
        }

        public Writer Displacement(string expression)
        {
            int d = Constant(expression, Context, -128, 127, "le décalage de (IX+d) ou (IY+d)");
            return Byte(d & 0xFF);
        }

        public Writer Imm16(string expression)
        {
            Value v = Evaluate(expression, Context);
            if (v.IsConstant)
            {
                if (v.Number < -32768 || v.Number > 65535)
                    throw new AsmException($"« {expression} » vaut {v.Number} : un mot va de -32768 à 65535.");
                int n = v.Number & 0xFFFF;
                parts.Add(() => Context.Emitter.Bytes((byte)(n & 0xFF), (byte)(n >> 8)));
            }
            else
            {
                string label = v.Label!;
                int addend = v.Number;
                parts.Add(() => Context.Emitter.AbsoluteWord(label, addend));
            }
            return this;
        }

        public Writer Relative(string expression)
        {
            Value v = Evaluate(expression, Context);
            if (v.IsConstant)
                throw new AsmException($"« {expression} » : un saut relatif (JR, DJNZ) va vers une étiquette, pas vers une adresse en nombre.");
            string label = v.Label!;
            int addend = v.Number;
            parts.Add(() => Context.Emitter.RelativeByte(label, addend));
            return this;
        }

        public void Flush()
        {
            Context.PlaceHere();
            foreach (Action part in parts)
                part();
        }
    }

    static AsmException Bad(Line line) =>
        new($"{line.Mnemonic} {string.Join(",", line.Operands)} : cette forme n'existe pas sur le Z80.");

    static void Count(Line line, int count)
    {
        if (line.Operands.Count != count)
            throw new AsmException(count == 0
                ? $"{line.Mnemonic} s'écrit sans valeur."
                : $"{line.Mnemonic} attend {count} valeur{(count > 1 ? "s" : "")} séparée{(count > 1 ? "s" : "")} par des virgules.");
    }

    static void Encode(Line line, Context context)
    {
        var w = new Writer(context);
        string m = line.Mnemonic!;
        List<string> o = line.Operands;

        // instructions sans valeur
        int? simple = m switch
        {
            "NOP" => 0x00, "RLCA" => 0x07, "RRCA" => 0x0F, "RLA" => 0x17, "RRA" => 0x1F, "DAA" => 0x27, "CPL" => 0x2F,
            "SCF" => 0x37, "CCF" => 0x3F, "HALT" => 0x76, "EXX" => 0xD9, "DI" => 0xF3, "EI" => 0xFB, _ => null
        };
        if (simple is int code)
        {
            Count(line, 0);
            w.Byte(code).Flush();
            return;
        }
        int? extended = m switch
        {
            "NEG" => 0x44, "RETN" => 0x45, "RETI" => 0x4D, "RRD" => 0x67, "RLD" => 0x6F,
            "LDI" => 0xA0, "CPI" => 0xA1, "INI" => 0xA2, "OUTI" => 0xA3, "LDD" => 0xA8, "CPD" => 0xA9, "IND" => 0xAA, "OUTD" => 0xAB,
            "LDIR" => 0xB0, "CPIR" => 0xB1, "INIR" => 0xB2, "OTIR" => 0xB3, "LDDR" => 0xB8, "CPDR" => 0xB9, "INDR" => 0xBA, "OTDR" => 0xBB,
            _ => null
        };
        if (extended is int ed)
        {
            Count(line, 0);
            w.Byte(0xED).Byte(ed).Flush();
            return;
        }

        switch (m)
        {
            case "ORG":
                throw new AsmException("ORG n'est pas utile ici : le compilateur place lui-même la routine dans le programme.");
            case "READ" or "INCLUDE":
                throw new AsmException($"{m} (inclure un autre fichier) n'existe pas encore : appelez plutôt une routine de bibliothèque par CALL.");
            case "MACRO" or "MEND" or "ENDM":
                throw new AsmException("les macros n'existent pas encore.");
            case "ALIGN":
                throw new AsmException("ALIGN n'est pas possible : le compilateur choisit lui-même l'adresse de la routine.");
            case "END" or "NOLIST" or "LIST":
                return;                                                     // sans effet ici

            case "DEFB" or "DB" or "BYTE" or "DEFM" or "TEXT":
                if (o.Count == 0)
                    throw new AsmException($"{m} attend au moins une valeur.");
                foreach (string item in o)
                {
                    string t = item.Trim();
                    if (t.Length >= 2 && t[0] == '"' && t[^1] == '"')
                    {
                        foreach (char ch in t[1..^1])
                            w.Byte(ch & 0xFF);
                    }
                    else
                        w.Imm8(t);
                }
                w.Flush();
                return;

            case "DEFW" or "DW" or "WORD":
                if (o.Count == 0)
                    throw new AsmException($"{m} attend au moins une valeur.");
                foreach (string item in o)
                    w.Imm16(item);
                w.Flush();
                return;

            case "DEFS" or "DS":
            {
                if (o.Count is < 1 or > 2)
                    throw new AsmException($"{m} s'écrit {m} taille ou {m} taille,valeur.");
                int size = Constant(o[0], context, 0, 16384, "la taille de DEFS");
                int fill = o.Count == 2 ? Constant(o[1], context, -128, 255, "la valeur de DEFS") & 0xFF : 0;
                for (int i = 0; i < size; i++)
                    w.Byte(fill);
                w.Flush();
                return;
            }

            case "LD":
                Count(line, 2);
                EncodeLd(line, Classify(o[0]), Classify(o[1]), w);
                w.Flush();
                return;

            case "PUSH" or "POP":
            {
                Count(line, 1);
                Operand a = Classify(o[0]);
                int baseCode = m == "PUSH" ? 0xC5 : 0xC1;
                if (a.Kind == Kind.R16 && a.Code != 3)
                    w.Byte(baseCode | ((a.Code == 4 ? 3 : a.Code) << 4));
                else if (a.Kind == Kind.Index)
                    w.Byte(a.Prefix).Byte(baseCode | 0x20);
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "EX":
            {
                Count(line, 2);
                Operand a = Classify(o[0]), b = Classify(o[1]);
                if (a.Kind == Kind.R16 && b.Kind == Kind.R16 && ((a.Code == 1 && b.Code == 2) || (a.Code == 2 && b.Code == 1)))
                    w.Byte(0xEB);
                else if (a.Kind == Kind.R16 && a.Code == 4 && b.Kind == Kind.AfAlt)
                    w.Byte(0x08);
                else if (a.Kind == Kind.SpMem && b.Kind == Kind.R16 && b.Code == 2)
                    w.Byte(0xE3);
                else if (a.Kind == Kind.SpMem && b.Kind == Kind.Index)
                    w.Byte(b.Prefix).Byte(0xE3);
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "ADD" or "ADC" or "SUB" or "SBC" or "AND" or "XOR" or "OR" or "CP":
                EncodeAlu(line, w);
                w.Flush();
                return;

            case "INC" or "DEC":
            {
                Count(line, 1);
                Operand a = Classify(o[0]);
                bool inc = m == "INC";
                switch (a.Kind)
                {
                    case Kind.R8 or Kind.HlMem:
                        w.Byte((a.Code << 3) | (inc ? 0x04 : 0x05));
                        break;
                    case Kind.IndexMem:
                        w.Byte(a.Prefix).Byte(inc ? 0x34 : 0x35).Displacement(a.Expression!);
                        break;
                    case Kind.R16 when a.Code < 4:
                        w.Byte((a.Code << 4) | (inc ? 0x03 : 0x0B));
                        break;
                    case Kind.Index:
                        w.Byte(a.Prefix).Byte(inc ? 0x23 : 0x2B);
                        break;
                    default:
                        throw Bad(line);
                }
                w.Flush();
                return;
            }

            case "IM":
            {
                Count(line, 1);
                int mode = Constant(o[0], context, 0, 2, "le mode de IM");
                w.Byte(0xED).Byte(mode switch { 0 => 0x46, 1 => 0x56, _ => 0x5E }).Flush();
                return;
            }

            case "RLC" or "RRC" or "RL" or "RR" or "SLA" or "SRA" or "SLL" or "SL1" or "SRL":
            {
                Count(line, 1);
                int op = m switch { "RLC" => 0x00, "RRC" => 0x08, "RL" => 0x10, "RR" => 0x18, "SLA" => 0x20, "SRA" => 0x28, "SRL" => 0x38, _ => 0x30 };
                EncodeCb(line, Classify(o[0]), op, w);
                w.Flush();
                return;
            }

            case "BIT" or "SET" or "RES":
            {
                Count(line, 2);
                int bit = Constant(o[0], context, 0, 7, $"le numéro de bit de {m}");
                int op = (m switch { "BIT" => 0x40, "RES" => 0x80, _ => 0xC0 }) | (bit << 3);
                EncodeCb(line, Classify(o[1]), op, w);
                w.Flush();
                return;
            }

            case "JP":
            {
                if (o.Count == 1)
                {
                    Operand a = Classify(o[0]);
                    if (a.Kind == Kind.HlMem)
                        w.Byte(0xE9);
                    else if (a.Kind == Kind.IndexMem && a.Expression == "0")
                        w.Byte(a.Prefix).Byte(0xE9);
                    else if (a.Kind is Kind.Imm)
                        w.Byte(0xC3).Imm16(a.Expression!);
                    else
                        throw Bad(line);
                }
                else if (o.Count == 2 && Conditions.TryGetValue(o[0].Trim().ToUpperInvariant(), out int cc))
                    w.Byte(0xC2 | (cc << 3)).Imm16(o[1]);
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "JR":
            {
                if (o.Count == 1)
                    w.Byte(0x18).Relative(o[0]);
                else if (o.Count == 2 && Conditions.TryGetValue(o[0].Trim().ToUpperInvariant(), out int cc) && cc < 4)
                    w.Byte(0x20 | (cc << 3)).Relative(o[1]);
                else
                    throw new AsmException($"JR {string.Join(",", o)} : JR s'écrit JR étiquette ou JR NZ / Z / NC / C,étiquette.");
                w.Flush();
                return;
            }

            case "DJNZ":
                Count(line, 1);
                w.Byte(0x10).Relative(o[0]).Flush();
                return;

            case "CALL":
            {
                if (o.Count == 1)
                    w.Byte(0xCD).Imm16(o[0]);
                else if (o.Count == 2 && Conditions.TryGetValue(o[0].Trim().ToUpperInvariant(), out int cc))
                    w.Byte(0xC4 | (cc << 3)).Imm16(o[1]);
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "RET":
            {
                if (o.Count == 0)
                    w.Byte(0xC9);
                else if (o.Count == 1 && Conditions.TryGetValue(o[0].Trim().ToUpperInvariant(), out int cc))
                    w.Byte(0xC0 | (cc << 3));
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "RST":
            {
                Count(line, 1);
                int n = Constant(o[0], context, 0, 0x38, "l'adresse de RST");
                if (n <= 7)
                    n *= 8;                                                 // RST 7 = RST &38 (WinAPE accepte les deux)
                if (n % 8 != 0)
                    throw new AsmException($"RST {o[0]} : RST va vers 0, 8, &10, &18, &20, &28, &30 ou &38 (ou 0 à 7).");
                w.Byte(0xC7 | n).Flush();
                return;
            }

            case "IN":
            {
                Count(line, 2);
                Operand a = Classify(o[0]), b = Classify(o[1]);
                if (a.Kind == Kind.R8 && a.Code == 7 && b.Kind == Kind.Mem)
                    w.Byte(0xDB).Imm8(b.Expression!);
                else if (a.Kind == Kind.R8 && b.Kind == Kind.CMem)
                    w.Byte(0xED).Byte(0x40 | (a.Code << 3));
                else
                    throw Bad(line);
                w.Flush();
                return;
            }

            case "OUT":
            {
                Count(line, 2);
                Operand a = Classify(o[0]), b = Classify(o[1]);
                if (a.Kind == Kind.Mem && b.Kind == Kind.R8 && b.Code == 7)
                    w.Byte(0xD3).Imm8(a.Expression!);
                else if (a.Kind == Kind.CMem && b.Kind == Kind.R8)
                    w.Byte(0xED).Byte(0x41 | (b.Code << 3));
                else
                    throw Bad(line);
                w.Flush();
                return;
            }
        }
        throw new AsmException($"« {m} » n'est pas encore pris en charge.");
    }

    static void EncodeCb(Line line, Operand a, int op, Writer w)
    {
        if (a.Kind is Kind.R8 or Kind.HlMem)
            w.Byte(0xCB).Byte(op | a.Code);
        else if (a.Kind == Kind.IndexMem)
            w.Byte(a.Prefix).Byte(0xCB).Displacement(a.Expression!).Byte(op | 6);
        else
            throw Bad(line);
    }

    static void EncodeAlu(Line line, Writer w)
    {
        string m = line.Mnemonic!;
        List<string> o = line.Operands;
        int alu = m switch { "ADD" => 0, "ADC" => 1, "SUB" => 2, "SBC" => 3, "AND" => 4, "XOR" => 5, "OR" => 6, _ => 7 };

        // ADD HL,rr ; ADC HL,rr ; SBC HL,rr ; ADD IX,rr
        if (o.Count == 2)
        {
            Operand first = Classify(o[0]);
            if ((first.Kind == Kind.R16 && first.Code == 2) || first.Kind == Kind.Index)
            {
                Operand second = Classify(o[1]);
                int rr;
                if (second.Kind == Kind.R16 && second.Code < 4 && !(first.Kind == Kind.Index && second.Code == 2))
                    rr = second.Code;
                else if (second.Kind == Kind.Index && first.Kind == Kind.Index && second.Prefix == first.Prefix)
                    rr = 2;
                else
                    throw Bad(line);
                if (m == "ADD")
                    w.Prefix(first.Prefix).Byte(0x09 | (rr << 4));
                else if ((m is "ADC" or "SBC") && first.Kind == Kind.R16)
                    w.Byte(0xED).Byte((m == "ADC" ? 0x4A : 0x42) | (rr << 4));
                else
                    throw Bad(line);
                return;
            }
            Operand acc = first;
            if (acc.Kind != Kind.R8 || acc.Code != 7)
                throw Bad(line);
            o = [o[1]];
        }
        if (o.Count != 1)
            throw new AsmException($"{m} attend une valeur (par exemple {m} B, {m} 5 ou {m} A,B).");

        Operand a = Classify(o[0]);
        switch (a.Kind)
        {
            case Kind.R8 or Kind.HlMem:
                w.Byte(0x80 | (alu << 3) | a.Code);
                break;
            case Kind.IndexMem:
                w.Byte(a.Prefix).Byte(0x86 | (alu << 3)).Displacement(a.Expression!);
                break;
            case Kind.Imm:
                w.Byte(0xC6 | (alu << 3)).Imm8(a.Expression!);
                break;
            default:
                throw Bad(line);
        }
    }

    static void EncodeLd(Line line, Operand a, Operand b, Writer w)
    {
        // LD r,r' ; LD r,(HL) ; LD (HL),r
        if (a.Kind is Kind.R8 or Kind.HlMem && b.Kind is Kind.R8 or Kind.HlMem && !(a.Kind == Kind.HlMem && b.Kind == Kind.HlMem))
        {
            w.Byte(0x40 | (a.Code << 3) | b.Code);
            return;
        }
        // LD r,(IX+d) ; LD (IX+d),r ; LD (IX+d),n
        if (a.Kind == Kind.R8 && b.Kind == Kind.IndexMem)
        {
            w.Byte(b.Prefix).Byte(0x46 | (a.Code << 3)).Displacement(b.Expression!);
            return;
        }
        if (a.Kind == Kind.IndexMem && b.Kind == Kind.R8)
        {
            w.Byte(a.Prefix).Byte(0x70 | b.Code).Displacement(a.Expression!);
            return;
        }
        if (a.Kind == Kind.IndexMem && b.Kind == Kind.Imm)
        {
            w.Byte(a.Prefix).Byte(0x36).Displacement(a.Expression!).Imm8(b.Expression!);
            return;
        }
        // LD r,n ; LD (HL),n
        if (a.Kind is Kind.R8 or Kind.HlMem && b.Kind == Kind.Imm)
        {
            w.Byte(0x06 | (a.Code << 3)).Imm8(b.Expression!);
            return;
        }
        // LD A,(BC) ; LD A,(DE) ; LD A,(nn) ; LD (BC),A ; LD (DE),A ; LD (nn),A
        bool aIsA = a.Kind == Kind.R8 && a.Code == 7, bIsA = b.Kind == Kind.R8 && b.Code == 7;
        if (aIsA && b.Kind == Kind.BcMem) { w.Byte(0x0A); return; }
        if (aIsA && b.Kind == Kind.DeMem) { w.Byte(0x1A); return; }
        if (aIsA && b.Kind == Kind.Mem) { w.Byte(0x3A).Imm16(b.Expression!); return; }
        if (a.Kind == Kind.BcMem && bIsA) { w.Byte(0x02); return; }
        if (a.Kind == Kind.DeMem && bIsA) { w.Byte(0x12); return; }
        if (a.Kind == Kind.Mem && bIsA) { w.Byte(0x32).Imm16(a.Expression!); return; }
        // LD A,I ; LD A,R ; LD I,A ; LD R,A
        if (aIsA && b.Kind == Kind.I) { w.Byte(0xED).Byte(0x57); return; }
        if (aIsA && b.Kind == Kind.R) { w.Byte(0xED).Byte(0x5F); return; }
        if (a.Kind == Kind.I && bIsA) { w.Byte(0xED).Byte(0x47); return; }
        if (a.Kind == Kind.R && bIsA) { w.Byte(0xED).Byte(0x4F); return; }
        // LD rr,nn ; LD IX,nn
        if (a.Kind == Kind.R16 && a.Code < 4 && b.Kind == Kind.Imm) { w.Byte(0x01 | (a.Code << 4)).Imm16(b.Expression!); return; }
        if (a.Kind == Kind.Index && b.Kind == Kind.Imm) { w.Byte(a.Prefix).Byte(0x21).Imm16(b.Expression!); return; }
        // LD HL,(nn) ; LD rr,(nn) ; LD IX,(nn) ; et l'inverse
        if (a.Kind == Kind.R16 && a.Code == 2 && b.Kind == Kind.Mem) { w.Byte(0x2A).Imm16(b.Expression!); return; }
        if (a.Kind == Kind.R16 && a.Code < 4 && b.Kind == Kind.Mem) { w.Byte(0xED).Byte(0x4B | (a.Code << 4)).Imm16(b.Expression!); return; }
        if (a.Kind == Kind.Index && b.Kind == Kind.Mem) { w.Byte(a.Prefix).Byte(0x2A).Imm16(b.Expression!); return; }
        if (a.Kind == Kind.Mem && b.Kind == Kind.R16 && b.Code == 2) { w.Byte(0x22).Imm16(a.Expression!); return; }
        if (a.Kind == Kind.Mem && b.Kind == Kind.R16 && b.Code < 4) { w.Byte(0xED).Byte(0x43 | (b.Code << 4)).Imm16(a.Expression!); return; }
        if (a.Kind == Kind.Mem && b.Kind == Kind.Index) { w.Byte(b.Prefix).Byte(0x22).Imm16(a.Expression!); return; }
        // LD SP,HL ; LD SP,IX
        if (a.Kind == Kind.R16 && a.Code == 3 && b.Kind == Kind.R16 && b.Code == 2) { w.Byte(0xF9); return; }
        if (a.Kind == Kind.R16 && a.Code == 3 && b.Kind == Kind.Index) { w.Byte(b.Prefix).Byte(0xF9); return; }
        throw Bad(line);
    }
}
