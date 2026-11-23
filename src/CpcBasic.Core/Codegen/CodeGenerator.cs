// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;
using CpcBasic.Core.Libraries;

namespace CpcBasic.Core.Codegen;

/// <summary>Le binaire produit et la liste des modules de la bibliothèque qui y ont été liés.</summary>
/// <summary>
/// Le programme compilé. MemoryLimit : la plus basse adresse écrite après MEMORY (un nombre écrit dans le programme), ou -1
/// s'il n'y en a pas ; MemoryLine : la ligne de ce MEMORY. Le compilateur vérifie que le programme tient en dessous (D057).
/// </summary>
/// <param name="Addresses">Adresse de chaque variable dans la mémoire du CPC (D132, liste des variables F4) : nom écrit,
/// signe compris ; « () » ajouté pour un tableau (adresse de sa fiche).</param>
public sealed record GeneratedProgram(byte[] Code, IReadOnlyList<string> Modules, int MemoryLimit = -1, int MemoryLine = 0,
                                      IReadOnlyDictionary<string, int>? Addresses = null);

/// <summary>
/// Transforme un programme analysé en code Z80. Organisation du binaire :
/// instructions du programme, fin de programme, modules utilisés, textes, puis variables.
/// Le programme ne revient jamais au BASIC : à la fin, il attend une touche puis redémarre.
/// Les expressions sont calculées sur 16 bits, résultat dans HL (décision D027).
/// Le programme a déjà été vérifié par l'analyseur : les boucles sont bien refermées.
/// </summary>
public static class CodeGenerator
{
    const string EndLabel = "program_end";

    sealed record VariableSlot(string Label, VarType Type);

    /// <summary>Une boucle ouverte : ses étiquettes servent à NEXT, EXIT et CONTINUE.</summary>
    // LimitSlot, StepSlot : variables cachées qui gardent une limite et un pas calculés (D078) ; DirSlot (FOR X!) :
    // signe du pas calculé (1, 0 ou &FF).
    sealed record LoopInfo(LoopKind Kind, string Variable, int Step, int Limit,
                           string BodyLabel, string NextLabel, string ExitLabel,
                           bool HasTopCondition = false, double RealLimit = 0, double RealStep = 0,
                           string? LimitSlot = null, string? StepSlot = null, string? DirSlot = null);

    /// <summary>Un IF ouvert : ses étiquettes servent à ELSE et END IF.</summary>
    // SELECT ouvert (D206) : la variable cachée qui garde la valeur, la fin, et le début du cas suivant (null : aucun)
    sealed class SelectInfo(string variable, string endLabel)
    {
        public string Variable { get; } = variable;
        public string EndLabel { get; } = endLabel;
        public string? NextLabel { get; set; }
        public int Cases { get; set; }
    }

    sealed class IfInfo(string elseLabel, string endLabel)
    {
        public string ElseLabel { get; } = elseLabel;
        public string EndLabel { get; } = endLabel;
        public bool HasElse { get; set; }
    }

    /// <summary>Indicateur du Z80 qui vaut « comparaison vraie » après SBC HL,DE.</summary>
    enum Flag { Z, NZ, C, NC }

    static Flag Invert(Flag flag) => flag switch
    {
        Flag.Z => Flag.NZ,
        Flag.NZ => Flag.Z,
        Flag.C => Flag.NC,
        _ => Flag.C
    };

    public static GeneratedProgram Generate(BasicProgram program, int origin, CompilerOptions? options = null)
    {
        options ??= CompilerOptions.Default;   // machine cible et nombres à virgule (D066)
        program = ExpandDebug(program, options);   // DEBUG avec le réglage « Mise au point » (D136)
        // Priorité « place » (F3, D141) : avec ELLIPSE dans le programme, CIRCLE x, y, r = ELLIPSE x, y, r, r (mêmes points)
        bool circleViaEllipse = options.PreferSize && program.Statements.Any(s => s is EllipseStatement);
        var emitter = new Z80Emitter(origin);
        var required = new HashSet<string>();
        LibraryCatalog libraryCatalog = options.Libraries ?? LibraryCatalog.Empty;   // bibliothèques (D148)
        var libraryUsed = new HashSet<string>(StringComparer.Ordinal);
        var strings = new List<byte[]>();
        var variables = new Dictionary<string, VariableSlot>();
        var slots = new List<VariableSlot>();   // dans l'ordre de première utilisation
        var declared = new Dictionary<string, VarType>();   // variables déclarées BYTE
        var loops = new Stack<LoopInfo>();      // boucles ouvertes, la plus récente en premier
        int loopCount = 0;
        var ifs = new Stack<IfInfo>();         // IF ouverts, le plus récent en premier
        int ifCount = 0;
        var selects = new Stack<SelectInfo>(); // SELECT ouverts (D206)
        int selectCount = 0;
        int labelCount = 0;
        int memoryLimit = -1;
        int memoryLine = 0;

        // Affiche un texte (rien si le texte est vide)
        void EmitText(string text)
        {
            if (text.Length == 0)
                return;

            required.Add(RuntimeLibrary.PrintStringName);
            emitter.LdHlLabel(StringLabel(strings.Count));
            emitter.CallLabel(RuntimeLibrary.PrintStringLabel);
            strings.Add(Encode(text, options.Language));
        }

        // Trouve l'emplacement d'une variable, en le réservant à la première utilisation
        VariableSlot Slot(string name)
        {
            if (!variables.TryGetValue(name, out VariableSlot? slot))
            {
                VarType type = declared.TryGetValue(name, out VarType declaredType) ? declaredType
                    : VariableNames.IsString(name) ? VarType.String
                    : VariableNames.IsReal(name) ? VarType.Real : VarType.Integer;
                slot = new VariableSlot($"var_{slots.Count}", type);
                variables[name] = slot;
                slots.Add(slot);
            }
            return slot;
        }

        // Tableaux (D058) : un descripteur par tableau, dans l'ordre de première apparition dans le programme.
        // Le parser a vérifié qu'un tableau garde toujours le même nombre de dimensions.
        var arrays = new Dictionary<string, (string Label, int Dimensions)>();
        foreach (Statement s in program.Statements)
        {
            IEnumerable<(string Name, int Count)> found = AstWalker.All(s)
                .OfType<ArrayElementExpression>().Select(x => (x.Name, x.Indexes.Count));
            if (s is DimStatement dim)
                found = dim.Arrays.Select(x => (x.Name, x.Sizes.Count)).Concat(found);
            if (s is ArrayAssignStatement assign)
                found = found.Append((assign.Name, assign.Indexes.Count));
            foreach ((string name, int count) in found)
                arrays.TryAdd(name, ($"arr_{arrays.Count}", count));
        }

        // ERASE (D096) : un tableau seulement effacé (jamais créé) a un descripteur ; « Improper argument » à l'exécution
        bool usesErase = false;
        foreach (EraseStatement erase in program.Statements.OfType<EraseStatement>())
        {
            usesErase = true;
            foreach (string name in erase.Names)
                arrays.TryAdd(name, ($"arr_{arrays.Count}", 1));
        }

        string SizeLabel(string array, int dimension) => $"{array}_s{dimension}";

        // Tableaux de textes (D065) : leurs cases sont des fiches de 3 octets. S'il y en a, chaque accès à un tableau
        // indique d'abord la taille de ses cases (2 ou 3) ; sinon rien n'est ajouté.
        // Tableaux de nombres à virgule (D070) : cases de 5 octets.
        bool hasStringArrays = arrays.Keys.Any(name => VariableNames.IsString(name) || VariableNames.IsReal(name));
        void EmitElementSize(string name)
        {
            if (!hasStringArrays)
                return;
            emitter.LdAImm(VariableNames.IsString(name) ? 3 : VariableNames.IsReal(name) ? 5 : 2);
            emitter.LdMemA(RuntimeLibrary.ArrayElementSizeLabel);
        }

        // HL = adresse de la case A(i [, j ...])
        void EmitElementAddress(string name, IReadOnlyList<Expression> indexes)
        {
            (string label, int dimensions) = arrays[name];
            if (dimensions == 1)
            {
                EmitExpression(indexes[0]);
                EmitElementSize(name);
                emitter.LdDeLabel(label);
                emitter.CallLabel(RuntimeLibrary.ArrayElem1Label);
            }
            else
            {
                foreach (Expression index in indexes)
                {
                    EmitExpression(index);
                    emitter.PushHl();
                }
                EmitElementSize(name);
                emitter.LdDeLabel(label);
                emitter.LdBImm(dimensions);
                emitter.CallLabel(RuntimeLibrary.ArrayElemLabel);
            }
        }

        // Textes (lot E, D062). Un texte écrit dans le programme a sa fiche (longueur, adresse) à la fin du binaire.
        var literals = new List<string>();
        bool usesStrings = program.Statements.Any(StatementUsesStrings);
        bool usesReals = program.Statements.Any(StatementUsesReals);   // nombres à virgule (D068)
        // Mémoire libre (D058, D062) : tableaux, textes, ou HIMEM et FRE qui la lisent (D094)
        // MASK ou GRAPHICS PEN stylo, mode : SCR WRITE est détourné pendant DRAW (D110)
        bool usesMask = program.Statements.Any(st => st is ScreenStatement { Command: "MASK" or "MASK FIRST" }
                                                         or ScreenStatement { Command: "GRAPHICS PEN", Arguments.Count: 2 });
        // D205 : le mode d'écriture ne change jamais (pas de 4e valeur à DRAW, PLOT..., ni CALL, ni commande de bibliothèque) :
        // DRAW écrit ses points directement dans l'écran
        bool directLines = !program.Statements.Any(st => st is DrawStatement { Mode: not null } or ExternalCallStatement
                                                             or LibraryCallStatement);
        // D210 : PLOT et PLOTR rapides (réglages gardés d'un point à l'autre) ; décidé juste avant les instructions
        bool fastPlot = false;
        bool usesHeap = arrays.Count > 0 || usesStrings
                        || program.Statements.Any(st => AstWalker.All(st).Any(x => x is CallExpression { Function: "HIMEM" or "FRE" }))
                        || program.Statements.Any(st => st is ScreenStatement { Command: "FILL" or "PAINT" });   // pile du remplissage (D107)

        // Cases de calcul des nombres à virgule (voir EmitRealInto) : déclarées ici, avant EmitString qui s'en sert (STR$)
        int realFree = 0;
        int realTemps = 0;
        bool usesSound = false;                 // SOUND (D071)
        int envelopeBlocks = 0;                 // ENV et ENT (D071)
        var realLiterals = new Dictionary<double, string>();

        // HL = adresse de la fiche du texte
        void EmitString(Expression expression)
        {
            switch (expression)
            {
                case StringLiteralExpression literal:
                    emitter.LdHlLabel($"lit_{literals.Count}");
                    literals.Add(literal.Text);
                    break;

                case VariableExpression variable:
                    emitter.LdHlLabel(Slot(variable.Name).Label);
                    break;

                case ArrayElementExpression element:
                    EmitElementAddress(element.Name, element.Indexes);   // la case est la fiche du texte (D065)
                    break;

                case ConcatExpression concat:
                    EmitString(concat.Left);
                    emitter.PushHl();
                    EmitString(concat.Right);
                    emitter.PopDe();
                    emitter.CallLabel(RuntimeLibrary.StrConcatLabel);
                    break;

                case CallExpression call:
                {
                    // Fonctions qui donnent un texte (D063) : le résultat est une fiche temporaire
                    required.Add(RuntimeLibrary.StrFunctionsName);
                    IReadOnlyList<Expression> a = call.Arguments;
                    switch (call.Function)
                    {
                        case "LEFT$" or "RIGHT$":
                            EmitString(a[0]);
                            emitter.PushHl();
                            EmitExpression(a[1]);
                            emitter.PopDe();
                            emitter.CallLabel(call.Function == "LEFT$" ? "rt_left" : "rt_right");
                            break;

                        case "MID$":
                            EmitString(a[0]);
                            emitter.PushHl();
                            EmitExpression(a[1]);
                            emitter.PushHl();
                            if (a.Count == 3)
                                EmitExpression(a[2]);
                            else
                                emitter.LdHlImm(255);                // longueur omise : jusqu'à la fin
                            emitter.LdBH();
                            emitter.LdCL();
                            emitter.PopHl();
                            emitter.PopDe();
                            emitter.CallLabel("rt_mid");
                            break;

                        case "INKEY$":
                            emitter.CallLabel("rt_inkey");           // D064
                            break;

                        case "STR$" when Types.IsReal(a[0]):
                            required.Add(RuntimeLibrary.RealStrName);   // STR$(1/3) = " 0.333333333" (D070)
                            EmitRealInto(a[0], realFree);
                            emitter.LdHlLabel(RealTemp(realFree));
                            emitter.CallLabel(RealRuntime.StrLabel);
                            break;

                        case "DEC$":
                        {
                            // DEC$(nombre, format) (D103) : comme PRINT USING, rendu comme un texte
                            int decSaved = realFree;
                            EmitRealInto(a[0], decSaved);
                            if (a[1] is StringLiteralExpression decWritten && Parser.DecField(decWritten.Text) is (int decW, int decB, int decD, int decF))
                            {
                                required.Add(RuntimeLibrary.DecName);
                                emitter.LdHlLabel(RealTemp(decSaved));
                                emitter.LdBcImm((decW << 8) | decB);
                                emitter.LdDeImm((decD << 8) | decF);
                                emitter.CallLabel(RuntimeLibrary.DecLabel);
                            }
                            else
                            {
                                required.Add(RuntimeLibrary.DecDynName);
                                realFree = decSaved + 1;                   // le nombre reste dans sa case pendant le format
                                EmitString(a[1]);
                                realFree = decSaved;
                                emitter.ExDeHl();                       // DE = fiche du format
                                emitter.LdHlLabel(RealTemp(decSaved));
                                emitter.CallLabel(RuntimeLibrary.DecDynLabel);
                            }
                            break;
                        }

                        case "COPYCHR$":
                        {
                            // COPYCHR$(#n) (D102) : TXT RD CHAR dans la fenêtre n, puis retour à la fenêtre d'avant ;
                            // caractère non reconnu : texte vide
                            string read = $"cpy_{labelCount++}";
                            EmitSelectStream(a[0]);
                            emitter.PushAf();
                            emitter.CallAddress(0xBB60);            // TXT RD CHAR : A = caractère, retenue = reconnu
                            emitter.LdEA();
                            emitter.LdCImm(1);
                            emitter.JrCLabel(read);
                            emitter.LdCImm(0);
                            emitter.Label(read);
                            emitter.PopAf();
                            emitter.PushBc();
                            emitter.PushDe();
                            emitter.CallAddress(TxtStrSelect);
                            emitter.PopDe();
                            emitter.PopBc();
                            emitter.CallLabel("rt_sf_fill");
                            break;
                        }

                        case "CHR$" or "SPACE$" or "STR$":
                            EmitExpression(a[0]);
                            emitter.CallLabel(call.Function switch { "CHR$" => "rt_chr", "SPACE$" => "rt_space", _ => "rt_strs" });
                            break;

                        case "STRING$":
                            EmitExpression(a[0]);
                            emitter.PushHl();
                            if (Types.IsString(a[1]))
                            {
                                EmitString(a[1]);                    // STRING$(5, "AB") : premier caractère
                                emitter.CallLabel("rt_asc");
                            }
                            else
                            {
                                EmitExpression(a[1]);
                            }
                            emitter.ExDeHl();
                            emitter.PopHl();
                            emitter.CallLabel("rt_string");
                            break;

                        case "UPPER$" or "LOWER$":
                            EmitString(a[0]);
                            emitter.CallLabel(call.Function == "UPPER$" ? "rt_upper" : "rt_lower");
                            break;

                        case "HEX$" or "BIN$":
                            EmitExpression(a[0]);
                            emitter.PushHl();
                            if (a.Count == 2)
                                EmitExpression(a[1]);
                            else
                                emitter.LdHlImm(0);
                            emitter.ExDeHl();
                            emitter.PopHl();
                            emitter.CallLabel(call.Function == "HEX$" ? "rt_hex" : "rt_bin");
                            break;

                        default:
                            throw new NotSupportedException($"Fonction texte non gérée : {call.Function}");
                    }
                    break;
                }

                case UserFunctionExpression user:                   // FNA$(...) (D095)
                    EmitUserArguments(user, realFree);
                    EmitString(user.Function.Body!);
                    break;

                default:
                    throw new NotSupportedException($"Texte non géré : {expression.GetType().Name}");
            }
        }

        // Nombres à virgule (lot F1b, D068). Un calcul se fait dans des cases de 5 octets (ftmp_0, ftmp_1...) :
        // la case k reçoit la valeur, la case k + 1 sert pour l'opérande de droite. realFree = première case libre
        // (un calcul entier au milieu d'un calcul à virgule ne doit pas écraser les cases déjà prises).

        string RealTemp(int k)
        {
            realTemps = Math.Max(realTemps, k + 1);
            return $"ftmp_{k}";
        }

        string RealLiteral(double value)
        {
            if (!realLiterals.TryGetValue(value, out string? label))
            {
                label = $"flit_{realLiterals.Count}";
                realLiterals[value] = label;
            }
            return label;
        }

        void EmitCopyReal(string from, string to)
        {
            emitter.LdHlLabel(from);
            emitter.LdDeLabel(to);
            emitter.LdBcImm(5);
            emitter.Ldir();
        }

        // Fonction du programmeur (DEF FN, D095) : les valeurs vont dans les variables cachées de la fonction, puis le
        // calcul est fait sur place. Si une valeur après la première appelle une FN, elle pourrait changer les valeurs
        // déjà rangées : on passe alors par des variables propres à cet appel. free : première case à virgule libre.
        void EmitUserArguments(UserFunctionExpression user, int free)
        {
            UserFunction f = user.Function;
            IReadOnlyList<Expression> args = user.Arguments;
            int saved = realFree;
            realFree = free;
            bool viaTemps = args.Skip(1).Any(x => AstWalker.Flatten(x).Any(e => e is UserFunctionExpression));
            int site = labelCount++;
            for (int i = 0; i < args.Count; i++)
            {
                VariableSlot slot = Slot(viaTemps ? $"FN#{site}.{f.Parameters[i]}" : f.Parameters[i]);
                switch (slot.Type)
                {
                    case VarType.String:
                        EmitString(args[i]);
                        emitter.LdDeLabel(slot.Label);
                        emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                        break;
                    case VarType.Real:
                        EmitRealInto(args[i], free);
                        EmitCopyReal(RealTemp(free), slot.Label);
                        break;
                    default:
                        EmitExpression(args[i]);
                        EmitStore(slot);
                        break;
                }
            }
            if (viaTemps)
            {
                for (int i = 0; i < args.Count; i++)
                {
                    VariableSlot from = Slot($"FN#{site}.{f.Parameters[i]}");
                    VariableSlot to = Slot(f.Parameters[i]);
                    switch (to.Type)
                    {
                        case VarType.String:
                            emitter.LdHlLabel(from.Label);
                            emitter.LdDeLabel(to.Label);
                            emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                            break;
                        case VarType.Real:
                            EmitCopyReal(from.Label, to.Label);
                            break;
                        default:
                            EmitLoad(from);
                            EmitStore(to);
                            break;
                    }
                }
            }
            realFree = saved;
        }

        // Calcule l'expression comme nombre à virgule dans la case k
        void EmitRealInto(Expression expression, int k)
        {
            string temp = RealTemp(k);
            switch (expression)
            {
                case RealNumberExpression real:
                    EmitCopyReal(RealLiteral(real.Value), temp);
                    break;

                case NumberExpression number:
                    EmitCopyReal(RealLiteral(number.Value), temp);
                    break;

                case VariableExpression variable when VariableNames.IsReal(variable.Name):
                    EmitCopyReal(Slot(variable.Name).Label, temp);
                    break;

                case ArrayElementExpression element when VariableNames.IsReal(element.Name):
                {
                    int saved = realFree;                           // T!(I) (D070) : les numéros de case sont entiers
                    realFree = k;
                    EmitElementAddress(element.Name, element.Indexes);
                    realFree = saved;
                    emitter.LdDeLabel(temp);
                    emitter.LdBcImm(5);
                    emitter.Ldir();
                    break;
                }

                case BinaryExpression { Operator: BinaryOperator.Subtract, Left: NumberExpression { Value: 0 } } minus
                    when Types.IsReal(minus):
                    EmitRealInto(minus.Right, k);                   // -X
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(FirmwareMaths.Negate.Label);
                    break;

                case BinaryExpression binary when Types.IsReal(binary):
                    EmitRealInto(binary.Left, k);
                    EmitRealInto(binary.Right, k + 1);
                    emitter.LdHlLabel(temp);
                    emitter.LdDeLabel(RealTemp(k + 1));
                    emitter.CallLabel(binary.Operator switch
                    {
                        BinaryOperator.Add => RealRuntime.AddLabel,
                        BinaryOperator.Subtract => RealRuntime.SubLabel,
                        BinaryOperator.Multiply => RealRuntime.MulLabel,
                        BinaryOperator.Divide => RealRuntime.DivLabel,
                        _ => RealRuntime.PowLabel
                    });
                    break;

                case UserFunctionExpression user when Types.IsReal(user):
                    EmitUserArguments(user, k);                     // FNR!(...) (D095)
                    EmitRealInto(user.Function.Body!, k);
                    break;

                case CallExpression call when Types.IsReal(call):
                    EmitRealFunction(call, k);                      // SIN, SQR, INT... (lot F2, D069)
                    break;

                default:
                {
                    int saved = realFree;                           // calcul entier, puis converti
                    realFree = k + 1;
                    EmitExpression(expression);
                    realFree = saved;
                    emitter.LdDeLabel(temp);
                    emitter.CallLabel(RealRuntime.IntToRealLabel);
                    break;
                }
            }
        }

        // Routine de calcul de la ROM sur la case : « Overflow » (le programme continue) si le résultat est trop grand
        void EmitFirmwareFunction(FirmwareMathsRoutine routine, string temp)
        {
            string ok = $"ffn_{labelCount++}";
            emitter.LdHlLabel(temp);
            emitter.CallLabel(routine.Label);
            emitter.JrCLabel(ok);
            emitter.LdHlLabel(temp);
            emitter.CallLabel(RealRuntime.OverflowPositiveLabel);
            emitter.Label(ok);
        }

        // Fonction mathématique à virgule (lot F2, D069) : résultat dans la case k
        void EmitRealFunction(CallExpression call, int k)
        {
            string temp = RealTemp(k);
            IReadOnlyList<Expression> a = call.Arguments;
            switch (call.Function)
            {
                case "PI":
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(FirmwareMaths.Pi.Label);
                    break;

                case "CREAL":
                    EmitRealInto(a[0], k);
                    break;

                case "TIME!":
                    // TIME avec le réglage F8 (D097) : KL TIME PLEASE rend DEHL, rangé sur 4 octets puis BINARY TO REAL
                    emitter.CallAddress(RuntimeLibrary.KlTimePlease);
                    emitter.LdMemHl(temp);
                    emitter.LdHlLabel(temp);
                    emitter.IncHl();
                    emitter.IncHl();
                    emitter.StoreHlE();
                    emitter.IncHl();
                    emitter.StoreHlD();
                    emitter.LdHlLabel(temp);
                    emitter.XorA();
                    emitter.CallLabel(FirmwareMaths.BinaryToReal.Label);
                    break;

                case "HIMEM" or "FRE":
                    // HIMEM : dernier octet libre (&A67B, ou l'adresse de MEMORY) ; FRE : place entre les tableaux et les
                    // textes, après le ménage des textes pour FRE(""). Plus de 32767 : nombre à virgule (D094).
                    if (call.Function == "HIMEM")
                    {
                        emitter.LdHlMem(RuntimeLibrary.ArrayHeapLimitLabel);
                        emitter.DecHl();
                    }
                    else
                    {
                        if (Types.IsString(a[0]))
                            emitter.CallLabel(RuntimeLibrary.StrGcLabel);
                        emitter.LdHlMem(RuntimeLibrary.StringPointerLabel);
                        emitter.LdDeMem(RuntimeLibrary.ArrayHeapLabel);
                        emitter.OrA();
                        emitter.SbcHlDe();
                    }
                    emitter.LdDeLabel(temp);
                    emitter.LdAImm(0);                              // sans signe
                    emitter.CallLabel(FirmwareMaths.IntegerToReal.Label);
                    break;

                case "VAL":
                {
                    // VAL("3.14") (D070) : le texte est lu comme un nombre à virgule (VAL("&FF") : 255)
                    required.Add(RuntimeLibrary.RealValName);
                    int saved = realFree;
                    realFree = k;
                    EmitString(a[0]);
                    realFree = saved;
                    emitter.LdDeLabel(temp);
                    emitter.CallLabel(RealRuntime.ValLabel);
                    break;
                }

                case "INT" or "FIX" or "ABS":
                    EmitRealInto(a[0], k);
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(call.Function switch
                    {
                        "INT" => RealRuntime.FloorLabel,         // la ROM rend un entier de 4 octets (D069)
                        "FIX" => RealRuntime.FixLabel,
                        _ => RealRuntime.AbsLabel
                    });
                    break;

                case "SQR" or "LOG" or "LOG10":
                    // SQR(-1), LOG(0) : « Improper argument », le programme s'arrête
                    EmitRealInto(a[0], k);
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(call.Function == "SQR" ? RealRuntime.CheckNotNegativeLabel : RealRuntime.CheckPositiveLabel);
                    EmitFirmwareFunction(call.Function switch
                    {
                        "SQR" => FirmwareMaths.Sqr,
                        "LOG" => FirmwareMaths.Log,
                        _ => FirmwareMaths.Log10
                    }, temp);
                    break;

                case "SIN" or "COS" or "TAN" or "ATN" or "EXP":
                    EmitRealInto(a[0], k);
                    EmitFirmwareFunction(call.Function switch
                    {
                        "SIN" => FirmwareMaths.Sin,
                        "COS" => FirmwareMaths.Cos,
                        "TAN" => FirmwareMaths.Tan,
                        "ATN" => FirmwareMaths.Atn,
                        _ => FirmwareMaths.Exp
                    }, temp);
                    break;

                case "ROUND":
                    EmitRealInto(a[0], k);
                    if (a.Count == 2)
                    {
                        int saved = realFree;                       // nombre de décimales (entier)
                        realFree = k + 1;
                        EmitExpression(a[1]);
                        realFree = saved;
                        emitter.LdAL();
                    }
                    else
                    {
                        emitter.XorA();
                    }
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(RealRuntime.RoundLabel);
                    break;

                case "MIN" or "MAX":
                {
                    string other = RealTemp(k + 1);
                    string skip = $"fmm_{labelCount++}";
                    EmitRealInto(a[0], k);
                    EmitRealInto(a[1], k + 1);
                    emitter.LdHlLabel(temp);
                    emitter.LdDeLabel(other);
                    emitter.CallLabel(FirmwareMaths.Compare.Label);
                    emitter.CpImm(call.Function == "MAX" ? 0xFF : 1);   // MAX : gauche plus petit ; MIN : plus grand
                    emitter.JrNzLabel(skip);
                    EmitCopyReal(other, temp);
                    emitter.Label(skip);
                    break;
                }

                case "RND":
                    // RND : nombre suivant ; RND(0) : le même que le précédent ; RND(négatif) : repart de ce nombre
                    required.Add(RuntimeLibrary.RealRndName);
                    if (a.Count == 1)
                    {
                        EmitRealInto(a[0], k);
                        emitter.LdHlLabel(temp);
                        emitter.CallLabel(FirmwareMaths.Sign.Label);
                    }
                    else
                    {
                        emitter.LdAImm(1);
                    }
                    emitter.LdHlLabel(temp);
                    emitter.CallLabel(RealRuntime.RndLabel);
                    break;

                default:
                    throw new NotSupportedException($"Fonction à virgule non gérée : {call.Function}");
            }
        }

        // Variable -> HL (un octet est étendu à 16 bits)
        void EmitLoad(VariableSlot slot)
        {
            if (slot.Type == VarType.Integer)
            {
                emitter.LdHlMem(slot.Label);
            }
            else
            {
                emitter.LdAMem(slot.Label);
                emitter.LdLA();
                emitter.LdHZero();
            }
        }

        // HL -> variable (pour un octet, on garde l'octet de poids faible : modulo 256)
        void EmitStore(VariableSlot slot)
        {
            if (slot.Type == VarType.Integer)
            {
                emitter.LdMemHl(slot.Label);
            }
            else
            {
                emitter.LdAL();
                emitter.LdMemA(slot.Label);
            }
        }

        // Calcule deux expressions : HL = gauche, DE = droite
        void EmitPair(Expression left, Expression right)
        {
            EmitExpression(left);
            if (right is NumberExpression number)
            {
                emitter.LdDeImm(number.Value & 0xFFFF);
            }
            else
            {
                emitter.PushHl();
                EmitExpression(right);
                emitter.PopDe();                            // DE = gauche, HL = droite
                emitter.ExDeHl();                           // HL = gauche, DE = droite
            }
        }

        // MIN ou MAX de deux expressions : le résultat est dans HL
        void EmitMinMax(string module, string label, Expression first, Expression second)
        {
            EmitPair(first, second);
            required.Add(module);
            emitter.CallLabel(label);
        }

        // Adresse ou port (D094) : un nombre à virgule va de -32768 à 65535 (MEMORY HIMEM-1000, PEEK(HIMEM)),
        // comme le vrai BASIC ; un entier est pris tel quel. Résultat dans HL.
        void EmitAddress(Expression expression)
        {
            if (!Types.IsReal(expression))
            {
                EmitExpression(expression);
                return;
            }
            EmitRealInto(expression, realFree);
            emitter.LdHlLabel(RealTemp(realFree));
            emitter.CallLabel(RealRuntime.RealToAddressLabel);
        }

        // Valeur calculée de POKE et OUT (DE) hors de 0 à 255 : « Improper argument », comme le CPC (D128) ;
        // une valeur écrite en nombre est vérifiée à la compilation
        void EmitByteCheck(Expression value)
        {
            if (value is NumberExpression)
                return;
            required.Add(RuntimeLibrary.ByteCheckName);
            emitter.LdAD();
            emitter.OrA();
            emitter.JpNzLabel(RuntimeLibrary.ByteCheckLabel);
        }

        // Adresse (HL) puis valeur (DE) : POKE, OUT (D094)
        void EmitAddressPair(Expression address, Expression value)
        {
            if (!Types.IsReal(address))
            {
                EmitPair(address, value);
                return;
            }
            EmitAddress(address);
            emitter.PushHl();
            EmitExpression(value);
            emitter.ExDeHl();
            emitter.PopHl();
        }

        // Calcule l'expression : le résultat est dans HL
        // Commande ou fonction de bibliothèque (D148) : chaque valeur va dans sa case, puis appel ; une fonction rend HL.
        // Plusieurs valeurs passent d'abord par la pile : LUNE(A, LUNE(B, C)) ne mélange pas les cases.
        void EmitLibraryCall(string name, IReadOnlyList<Expression> arguments)
        {
            LibraryCommand command = libraryCatalog.Find(name)!;
            libraryUsed.Add(command.Name);
            // un TEXTE (D169) : HL = adresse de la fiche du texte ; une VALEUR : HL = le nombre
            void EmitArgument(int i)
            {
                if (command.Parameters[i].IsText)
                    EmitString(arguments[i]);
                else
                    EmitExpression(arguments[i]);
            }
            if (arguments.Count == 1)
            {
                EmitArgument(0);
                emitter.LdMemHl(LibraryLinker.CellLabel(command.Name, command.Parameters[0].Name));
            }
            else if (arguments.Count > 1)
            {
                for (int i = 0; i < arguments.Count; i++)
                {
                    EmitArgument(i);
                    emitter.PushHl();
                }
                for (int i = arguments.Count - 1; i >= 0; i--)
                {
                    emitter.PopHl();
                    emitter.LdMemHl(LibraryLinker.CellLabel(command.Name, command.Parameters[i].Name));
                }
            }
            emitter.CallLabel(LibraryLinker.EntryLabel(command.Name));
        }

        void EmitExpression(Expression expression)
        {
            if (expression is CallExpression { Function: "TIME!" })
            {
                emitter.CallAddress(RuntimeLibrary.KlTimePlease);   // TIME (F8) là où il faut un entier : les 16 bits du bas (D097)
                return;
            }
            if (Types.IsReal(expression))
            {
                // Nombre à virgule là où il faut un entier : arrondi, « Overflow » s'il ne tient pas (D068)
                EmitRealInto(expression, realFree);
                emitter.LdHlLabel(RealTemp(realFree));
                emitter.CallLabel(RealRuntime.RealToIntLabel);
                return;
            }

            switch (expression)
            {
                case NumberExpression number:
                    emitter.LdHlImm(number.Value & 0xFFFF);
                    break;

                case VariableExpression variable:
                    EmitLoad(Slot(variable.Name));
                    break;

                case BinaryExpression binary:
                    EmitExpression(binary.Left);
                    if (binary.Right is NumberExpression right)
                    {
                        emitter.LdDeImm(right.Value & 0xFFFF);      // DE = opérande de droite
                    }
                    else
                    {
                        emitter.PushHl();
                        EmitExpression(binary.Right);
                        emitter.PopDe();                            // DE = gauche, HL = droite
                        if (binary.Operator is not (BinaryOperator.Add or BinaryOperator.Multiply))
                            emitter.ExDeHl();                       // HL = gauche, DE = droite
                    }

                    switch (binary.Operator)
                    {
                        case BinaryOperator.Add:
                            emitter.AddHlDe();
                            break;

                        case BinaryOperator.Subtract:
                            emitter.OrA();                          // retenue à 0
                            emitter.SbcHlDe();
                            break;

                        case BinaryOperator.Multiply:
                            required.Add(RuntimeLibrary.Mul16Name);
                            emitter.CallLabel(RuntimeLibrary.Mul16Label);          // HL = HL * DE
                            break;

                        case BinaryOperator.Divide:
                            required.Add(RuntimeLibrary.Div16Name);
                            emitter.CallLabel(RuntimeLibrary.Div16Label);          // HL = quotient
                            break;

                        case BinaryOperator.And:                                   // bit à bit, octet par octet (D060)
                            emitter.LdAH();
                            emitter.AndD();
                            emitter.LdHA();
                            emitter.LdAL();
                            emitter.AndE();
                            emitter.LdLA();
                            break;

                        case BinaryOperator.Or:
                            emitter.LdAH();
                            emitter.OrD();
                            emitter.LdHA();
                            emitter.LdAL();
                            emitter.OrE();
                            emitter.LdLA();
                            break;

                        case BinaryOperator.Xor:
                            emitter.LdAH();
                            emitter.XorD();
                            emitter.LdHA();
                            emitter.LdAL();
                            emitter.XorE();
                            emitter.LdLA();
                            break;

                        case BinaryOperator.Power:
                            required.Add(RuntimeLibrary.Pow16Name);
                            emitter.CallLabel(RuntimeLibrary.Pow16Label);          // HL = HL ^ DE
                            break;

                        default:
                            required.Add(RuntimeLibrary.Div16Name);
                            emitter.CallLabel(RuntimeLibrary.Div16Label);
                            emitter.ExDeHl();                                      // HL = reste
                            break;
                    }
                    break;

                case CompareExpression compare:
                {
                    // Comparaison utilisée comme valeur : HL = -1 si vraie, 0 sinon (D060)
                    Flag flag = EmitCompare(new ComparisonCondition(compare.Operator, compare.Left, compare.Right));
                    string skip = $"cmp_{labelCount++}";
                    emitter.LdHlImm(0);                             // LD ne change pas les indicateurs
                    switch (Invert(flag))
                    {
                        case Flag.Z: emitter.JrZLabel(skip); break;
                        case Flag.NZ: emitter.JrNzLabel(skip); break;
                        case Flag.C: emitter.JrCLabel(skip); break;
                        default: emitter.JrNcLabel(skip); break;
                    }
                    emitter.DecHl();                                // vraie : -1
                    emitter.Label(skip);
                    break;
                }

                case AddressOfExpression address:
                    // @A : adresse de la variable (fixe dans le programme compilé) ; @T(3) : adresse de la case (D061)
                    if (address.Target is ArrayElementExpression cell)
                        EmitElementAddress(cell.Name, cell.Indexes);
                    else
                        emitter.LdHlLabel(Slot(((VariableExpression)address.Target).Name).Label);
                    break;

                case NotExpression negation:
                    EmitExpression(negation.Operand);               // NOT : tous les bits inversés (D060)
                    emitter.LdAH();
                    emitter.Cpl();
                    emitter.LdHA();
                    emitter.LdAL();
                    emitter.Cpl();
                    emitter.LdLA();
                    break;

                case CallExpression call:
                    switch (call.Function)
                    {
                        case "ASC" or "VAL":                        // D063
                            required.Add(RuntimeLibrary.StrFunctionsName);
                            EmitString(call.Arguments[0]);
                            emitter.CallLabel(call.Function == "ASC" ? "rt_asc" : "rt_val");
                            break;

                        case "INSTR":                               // INSTR([début,] texte, cherché) (D063)
                            required.Add(RuntimeLibrary.StrFunctionsName);
                            if (call.Arguments.Count == 3)
                            {
                                EmitExpression(call.Arguments[0]);
                                emitter.PushHl();
                            }
                            EmitString(call.Arguments[^2]);
                            emitter.PushHl();
                            EmitString(call.Arguments[^1]);
                            emitter.PopDe();
                            if (call.Arguments.Count == 3)
                                emitter.PopBc();
                            else
                                emitter.LdBcImm(1);
                            emitter.CallLabel("rt_instr");
                            break;

                        case "LEN":                                 // LEN(texte) : la longueur est le premier octet de la fiche (D062)
                            EmitString(call.Arguments[0]);
                            emitter.LoadLFromHl();
                            emitter.LdHZero();
                            break;

                        case "ABS":
                            EmitExpression(call.Arguments[0]);
                            required.Add(RuntimeLibrary.Abs16Name);
                            emitter.CallLabel(RuntimeLibrary.Abs16Label);
                            break;

                        case "POS" or "VPOS":
                        {
                            // POS(#n), VPOS(#n) : colonne ou ligne où s'écrira le caractère suivant dans la fenêtre n,
                            // puis retour à la fenêtre choisie avant (PRINT #2, POS(#1) écrit bien dans #2) (D072)
                            if (call.Arguments[0] is NumberExpression { Value: 8 })
                            {
                                required.Add(RuntimeLibrary.PrinterName);   // POS(#8) : colonne de l'imprimante (D100)
                                emitter.LdAMem(RuntimeLibrary.PrinterColumnLabel);
                                emitter.LdLA();
                                emitter.LdHZero();
                                break;
                            }
                            EmitSelectStream(call.Arguments[0]);
                            emitter.PushAf();
                            emitter.CallAddress(RuntimeLibrary.TxtGetCursor);
                            emitter.CallAddress(TxtValidate);
                            emitter.PopAf();
                            emitter.PushHl();
                            emitter.CallAddress(TxtStrSelect);
                            emitter.PopHl();
                            if (call.Function == "POS")
                                emitter.LdLH();
                            emitter.LdHZero();
                            break;
                        }

                        case "XPOS" or "YPOS":              // position du curseur graphique (D091) : GRA ASK CURSOR, DE = x, HL = y
                            emitter.CallAddress(0xBBC6);
                            if (call.Function == "XPOS")
                                emitter.ExDeHl();
                            break;

                        case "TEST" or "TESTR":             // couleur d'un point ; le curseur graphique y va (D091)
                            EmitExpression(call.Arguments[0]);
                            emitter.PushHl();
                            EmitExpression(call.Arguments[1]);
                            emitter.PopDe();
                            emitter.CallAddress(call.Function == "TEST" ? 0xBBF0 : 0xBBF3);   // GRA TEST ABSOLUTE / RELATIVE
                            emitter.LdLA();
                            emitter.LdHZero();
                            break;

                        case "EOF":                         // EOF : -1 à la fin du fichier ouvert par OPENIN (ou sans fichier), sinon 0 (D084)
                            required.Add(RuntimeLibrary.FileInName);
                            emitter.CallLabel("rt_fl_eof");
                            break;

                        case "ERR" or "ERL":                // numéro et ligne de la dernière erreur (D080)
                            required.Add(RuntimeLibrary.OnErrorName);
                            emitter.LdHlMem(call.Function == "ERR" ? RuntimeLibrary.ErrLabel : RuntimeLibrary.ErlLabel);
                            break;

                        case "REMAIN":                      // REMAIN(minuterie) : temps restant, minuterie arrêtée (D079)
                            EmitExpression(call.Arguments[0]);
                            required.Add(RuntimeLibrary.EventsName);
                            emitter.CallLabel(RuntimeLibrary.EventsRemainLabel);
                            break;

                        case "SQ":                          // SQ(canal) : état du canal sonore (D071)
                            EmitExpression(call.Arguments[0]);
                            emitter.LdAL();
                            emitter.CallAddress(SoundCheck);
                            emitter.LdLA();
                            emitter.LdHZero();
                            break;

                        case "SGN" when Types.IsReal(call.Arguments[0]):
                            EmitRealInto(call.Arguments[0], realFree);  // SGN(2.5) : 1, 0 ou -1 (D069)
                            emitter.LdHlLabel(RealTemp(realFree));
                            emitter.CallLabel(FirmwareMaths.Sign.Label);
                            emitter.LdLA();
                            emitter.AddAA();
                            emitter.SbcAA();
                            emitter.LdHA();
                            break;

                        case "SGN":
                            EmitExpression(call.Arguments[0]);
                            required.Add(RuntimeLibrary.Sgn16Name);
                            emitter.CallLabel(RuntimeLibrary.Sgn16Label);
                            break;

                        case "CINT" when Types.IsReal(call.Arguments[0]):
                            EmitRealInto(call.Arguments[0], realFree);  // arrondi ; « Overflow » s'il ne tient pas (D069)
                            emitter.LdHlLabel(RealTemp(realFree));
                            emitter.CallLabel(RealRuntime.RealToIntLabel);
                            break;

                        case "CINT" or "INT" or "FIX" or "ROUND":
                            EmitExpression(call.Arguments[0]);          // sur un entier : rien à changer (D069)
                            break;

                        case "HIBYTE":                      // octet de poids fort, 0 à 255
                            EmitExpression(call.Arguments[0]);
                            emitter.LdLH();
                            emitter.LdHZero();
                            break;

                        case "LOBYTE":                      // octet de poids faible, 0 à 255
                            EmitExpression(call.Arguments[0]);
                            emitter.LdHZero();
                            break;

                        case "MIN":
                            EmitMinMax(RuntimeLibrary.Min16Name, RuntimeLibrary.Min16Label,
                                       call.Arguments[0], call.Arguments[1]);
                            break;

                        case "MAX":
                            EmitMinMax(RuntimeLibrary.Max16Name, RuntimeLibrary.Max16Label,
                                       call.Arguments[0], call.Arguments[1]);
                            break;

                        case "INKEY":                       // INKEY(n) : -1 si la touche n'est pas appuyée, sinon 0, 32 (Shift), 128 (Ctrl) ou 160 (les deux)
                        {
                            string no = $"ik_{labelCount++}";
                            string end = $"ik_{labelCount++}";
                            EmitExpression(call.Arguments[0]);
                            emitter.LdAL();                 // A = numéro de touche
                            emitter.CallAddress(RuntimeLibrary.KmTestKey);   // Z = touche non appuyée ; C = état de Shift (bit 5) et Ctrl (bit 7)
                            emitter.JrZLabel(no);
                            emitter.LdAC();
                            emitter.AndImm(0xA0);
                            emitter.LdLA();
                            emitter.LdHZero();
                            emitter.JrLabel(end);
                            emitter.Label(no);
                            emitter.LdHlImm(0xFFFF);
                            emitter.Label(end);
                            break;
                        }

                        case "JOY":                         // JOY(0) ou JOY(1) : bits 0 haut, 1 bas, 2 gauche, 3 droite, 4 tir 2, 5 tir 1
                        {
                            string second = $"joy_{labelCount++}";
                            EmitExpression(call.Arguments[0]);
                            emitter.LdAL();
                            emitter.PushAf();
                            emitter.CallAddress(RuntimeLibrary.KmGetJoystick);   // H = manette 0, L = manette 1
                            emitter.PopAf();
                            emitter.OrA();
                            emitter.JrNzLabel(second);
                            emitter.LdAH();                 // manette 0 : on la met dans L
                            emitter.LdLA();
                            emitter.Label(second);
                            emitter.LdHZero();
                            break;
                        }

                        case "PEEK":                        // PEEK(adresse) : l'octet à cette adresse, de 0 à 255
                        {
                            // À partir de &C000, le Z80 lirait la ROM haute du BASIC au lieu de la mémoire (écran) : on la coupe un instant
                            string low = $"peek_{labelCount++}";
                            string end = $"peek_{labelCount++}";
                            EmitAddress(call.Arguments[0]);
                            emitter.LdAH();
                            emitter.CpImm(0xC0);
                            emitter.JrCLabel(low);
                            emitter.PushHl();
                            emitter.CallAddress(0xB903);    // KL U ROM DISABLE : A = état précédent
                            emitter.PopHl();
                            emitter.PushAf();
                            emitter.LdAHl();
                            emitter.LdLA();
                            emitter.LdHZero();
                            emitter.PopAf();
                            emitter.PushHl();               // la valeur lue attend pendant que la ROM est remise
                            emitter.CallAddress(0xB90C);    // KL ROM RESTORE
                            emitter.PopHl();
                            emitter.JrLabel(end);
                            emitter.Label(low);
                            emitter.LdAHl();
                            emitter.LdLA();
                            emitter.LdHZero();
                            emitter.Label(end);
                            break;
                        }

                        case "INP":                         // INP(port) : l'octet lu sur ce port, de 0 à 255
                            EmitAddress(call.Arguments[0]);
                            emitter.LdBH();
                            emitter.LdCL();
                            emitter.InAC();
                            emitter.LdLA();
                            emitter.LdHZero();
                            break;

                        case "DERR":                        // dernière erreur d'AMSDOS, 0 au départ (D099)
                            required.Add(RuntimeLibrary.FileCommonName);
                            emitter.LdAMem(RuntimeLibrary.DerrLabel);
                            emitter.LdLA();
                            emitter.LdHZero();
                            break;

                        case "UNT":                         // UNT(adresse) : la même valeur sur 16 bits, en entier signé (D094)
                            EmitAddress(call.Arguments[0]);
                            break;

                        case "TIME":                        // temps écoulé : 16 bits de poids faible
                            emitter.CallAddress(RuntimeLibrary.KlTimePlease);   // le firmware rend DEHL ; HL est le bas
                            break;

                        case "RANDOM":                      // nombre de min à max compris
                            EmitPair(call.Arguments[0], call.Arguments[1]);
                            required.Add(RuntimeLibrary.RandomRangeName);
                            emitter.CallLabel(RuntimeLibrary.RandomRangeLabel);
                            break;

                        case "ISIN" or "ICOS":              // D142 : HL = angle, DE = échelle, table des sinus
                            EmitPair(call.Arguments[0], call.Arguments[1]);
                            required.Add(RuntimeLibrary.ISinName);
                            emitter.CallLabel(call.Function == "ISIN" ? RuntimeLibrary.ISinLabel : RuntimeLibrary.ICosLabel);
                            break;

                        case "COLLIDE":                     // D200 : HL = a, DE = b -> HL = -1 ou 0
                            EmitPair(call.Arguments[0], call.Arguments[1]);
                            required.Add(RuntimeLibrary.SpriteName);
                            emitter.CallLabel(SpriteRuntime.CollideLabel);
                            break;

                        case "CLAMP":                       // CLAMP(v, mini, maxi) = MIN(MAX(v, mini), maxi)
                            EmitMinMax(RuntimeLibrary.Min16Name, RuntimeLibrary.Min16Label,
                                       new CallExpression("MAX", [call.Arguments[0], call.Arguments[1]]),
                                       call.Arguments[2]);
                            break;

                        default:
                            throw new NotSupportedException($"Fonction non gérée : {call.Function}");
                    }
                    break;

                case ArrayElementExpression element:
                    EmitElementAddress(element.Name, element.Indexes);
                    emitter.LdAHl();                                // HL = contenu de la case
                    emitter.IncHl();
                    emitter.LoadHFromHl();
                    emitter.LdLA();
                    break;

                case UserFunctionExpression user:                   // FNDOUBLE(...) (D095)
                    EmitUserArguments(user, realFree);
                    EmitExpression(user.Function.Body!);
                    break;

                case LibraryCallExpression library:                 // fonction de bibliothèque (D148)
                    EmitLibraryCall(library.Name, library.Arguments);
                    break;

                default:
                    throw new NotSupportedException($"Expression non gérée : {expression.GetType().Name}");
            }
        }

        // Fin d'une boucle FOR : ajoute le pas, puis revient au début du corps tant que la
        // limite n'est pas dépassée. Le test se fait ici, donc le corps s'exécute au moins une fois.
        // Les comparaisons sont signées : on retourne le bit de poids fort des deux valeurs, ce qui
        // permet une comparaison non signée (retenue). Un débordement de l'entier (limite 32767)
        // termine la boucle au lieu de la faire recommencer.
        void EmitNext(LoopInfo loop)
        {
            VariableSlot slot = Slot(loop.Variable);

            if (slot.Type == VarType.Real)
            {
                // FOR X! (D068) : X! = X! + pas, puis comparaison avec la limite par la ROM ; on continue tant que X!
                // n'a pas dépassé la limite (dans le sens du pas)
                emitter.Label(loop.NextLabel);
                emitter.LdHlLabel(slot.Label);
                emitter.LdDeLabel(loop.StepSlot ?? RealLiteral(loop.RealStep));
                emitter.CallLabel(RealRuntime.AddLabel);
                EmitRealForCompare(loop);
                emitter.JpNzLabel(loop.BodyLabel);
                emitter.Label(loop.ExitLabel);
                return;
            }

            emitter.Label(loop.NextLabel);
            EmitLoad(slot);
            if (loop.StepSlot is not null)
                emitter.LdDeMem(loop.StepSlot);             // pas calculé (D078)
            else
                emitter.LdDeImm(loop.Step & 0xFFFF);
            emitter.OrA();                                  // retenue à 0
            emitter.AdcHlDe();                              // HL + pas, indicateur de débordement mis à jour
            EmitStore(slot);                                // (LD ne modifie pas les indicateurs)
            if (slot.Type == VarType.Integer)
                emitter.JpPeLabel(loop.ExitLabel);          // débordement : fin de la boucle

            EmitForCompare(loop);
            emitter.JpNcLabel(loop.BodyLabel);              // pas de retenue : on continue
            emitter.Label(loop.ExitLabel);
        }

        // HL = valeur de la variable d'un FOR entier : retenue à 1 si elle a dépassé la limite dans le sens du pas
        // (comparaison signée : on retourne le bit 15 des deux côtés)
        void EmitForCompare(LoopInfo loop)
        {
            emitter.LdAH();                                 // retourne le bit 15 de HL
            emitter.XorImm(0x80);
            emitter.LdHA();
            if (loop.LimitSlot is not null)
            {
                emitter.LdDeMem(loop.LimitSlot);            // limite calculée (D078), même transformation
                emitter.LdAD();
                emitter.XorImm(0x80);
                emitter.LdDA();
            }
            else
            {
                emitter.LdDeImm((loop.Limit & 0xFFFF) ^ 0x8000);   // limite, même transformation
            }

            if (loop.StepSlot is not null)
            {
                // Pas calculé : le sens se décide pendant l'exécution, selon son signe
                string negative = $"for_{labelCount++}";
                string done = $"for_{labelCount++}";
                emitter.PushHl();
                emitter.LdHlMem(loop.StepSlot);
                emitter.LdAH();
                emitter.PopHl();
                emitter.AndImm(0x80);
                emitter.JrNzLabel(negative);
                emitter.ExDeHl();                           // pas positif : limite - valeur
                emitter.OrA();
                emitter.SbcHlDe();
                emitter.JrLabel(done);
                emitter.Label(negative);
                emitter.OrA();                              // pas négatif : valeur - limite
                emitter.SbcHlDe();
                emitter.Label(done);
            }
            else if (loop.Step > 0)
            {
                emitter.ExDeHl();                           // HL = limite, DE = valeur
                emitter.OrA();
                emitter.SbcHlDe();                          // limite - valeur : retenue si valeur > limite
            }
            else
            {
                emitter.OrA();
                emitter.SbcHlDe();                          // valeur - limite : retenue si valeur < limite
            }
        }

        // FOR X! : compare la variable à la limite (la ROM rend A = 1 si plus grande, 0 si égale, &FF si plus petite),
        // puis Z à 1 si elle a dépassé la limite dans le sens du pas
        void EmitRealForCompare(LoopInfo loop)
        {
            emitter.LdHlLabel(Slot(loop.Variable).Label);
            emitter.LdDeLabel(loop.LimitSlot ?? RealLiteral(loop.RealLimit));
            emitter.CallLabel(FirmwareMaths.Compare.Label);
            if (loop.DirSlot is null)
            {
                emitter.CpImm(loop.RealStep > 0 ? 1 : 0xFF);
                return;
            }
            string positive = $"for_{labelCount++}";
            emitter.LdBA();                                 // B = résultat de la comparaison
            emitter.LdAMem(loop.DirSlot);
            emitter.CpImm(0xFF);
            emitter.LdAImm(1);                              // pas positif (ou nul) : dépassé si plus grande
            emitter.JrNzLabel(positive);
            emitter.LdAImm(0xFF);                           // pas négatif : dépassé si plus petite
            emitter.Label(positive);
            emitter.CpB();
        }

        // Compare deux expressions et renvoie l'indicateur qui vaut « vrai ».
        // Égalité : SBC suffit. Autres comparaisons : on retourne le bit 15 des deux valeurs (la
        // comparaison signée devient une comparaison non signée par la retenue), et > et <= se font
        // en échangeant les deux valeurs.
        Flag EmitCompare(ComparisonCondition comparison)
        {
            bool equality = comparison.Operator is CompareOperator.Equal or CompareOperator.NotEqual;
            bool swap = comparison.Operator is CompareOperator.Greater or CompareOperator.LessOrEqual;

            Expression rightSide = comparison.Right;
            if (Types.IsString(comparison.Left))
            {
                // Deux textes (D062) : HL = -1, 0 ou 1, puis on compare HL avec 0
                EmitString(comparison.Left);
                emitter.PushHl();
                EmitString(comparison.Right);
                emitter.PopDe();
                emitter.CallLabel(RuntimeLibrary.StrCmpLabel);
                rightSide = new NumberExpression(0);
            }
            else if (Types.IsReal(comparison.Left) || Types.IsReal(comparison.Right))
            {
                // Nombres à virgule (D068) : la ROM rend A = 1, 0 ou &FF ; HL = A avec son signe, comparé à 0
                int k = realFree;
                EmitRealInto(comparison.Left, k);
                EmitRealInto(comparison.Right, k + 1);
                emitter.LdHlLabel(RealTemp(k));
                emitter.LdDeLabel(RealTemp(k + 1));
                emitter.CallLabel(FirmwareMaths.Compare.Label);
                emitter.LdLA();
                emitter.AddAA();
                emitter.SbcAA();
                emitter.LdHA();
                rightSide = new NumberExpression(0);
            }
            else
            {
                EmitExpression(comparison.Left);
            }
            if (rightSide is NumberExpression right)
            {
                if (!equality)
                {
                    emitter.LdAH();
                    emitter.XorImm(0x80);
                    emitter.LdHA();
                }
                int value = right.Value & 0xFFFF;
                emitter.LdDeImm(equality ? value : value ^ 0x8000);
            }
            else
            {
                emitter.PushHl();
                EmitExpression(comparison.Right);
                emitter.PopDe();                            // DE = gauche, HL = droite
                emitter.ExDeHl();                           // HL = gauche, DE = droite
                if (!equality)
                {
                    emitter.LdAH();
                    emitter.XorImm(0x80);
                    emitter.LdHA();
                    emitter.LdAD();
                    emitter.XorImm(0x80);
                    emitter.LdDA();
                }
            }

            if (swap)
                emitter.ExDeHl();
            emitter.OrA();
            emitter.SbcHlDe();

            return comparison.Operator switch
            {
                CompareOperator.Equal => Flag.Z,
                CompareOperator.NotEqual => Flag.NZ,
                CompareOperator.Less => Flag.C,
                CompareOperator.GreaterOrEqual => Flag.NC,
                CompareOperator.Greater => Flag.C,          // droite < gauche
                _ => Flag.NC                                // gauche <= droite : non (droite < gauche)
            };
        }

        // Saute vers target si la condition vaut whenTrue, sinon continue en séquence.
        // AND et OR sont évalués de gauche à droite, en s'arrêtant dès que le résultat est connu.
        void Jump(Condition condition, bool whenTrue, string target)
        {
            switch (condition)
            {
                case ComparisonCondition comparison:
                {
                    Flag flag = EmitCompare(comparison);
                    if (!whenTrue)
                        flag = Invert(flag);
                    switch (flag)
                    {
                        case Flag.Z: emitter.JpZLabel(target); break;
                        case Flag.NZ: emitter.JpNzLabel(target); break;
                        case Flag.C: emitter.JpCLabel(target); break;
                        default: emitter.JpNcLabel(target); break;
                    }
                    break;
                }

                case AndCondition both when whenTrue:
                {
                    string skip = $"cond_{labelCount++}";
                    Jump(both.Left, false, skip);
                    Jump(both.Right, true, target);
                    emitter.Label(skip);
                    break;
                }

                case AndCondition both:
                    Jump(both.Left, false, target);
                    Jump(both.Right, false, target);
                    break;

                case OrCondition either when whenTrue:
                    Jump(either.Left, true, target);
                    Jump(either.Right, true, target);
                    break;

                case OrCondition either:
                {
                    string skip = $"cond_{labelCount++}";
                    Jump(either.Left, true, skip);
                    Jump(either.Right, false, target);
                    emitter.Label(skip);
                    break;
                }

                default:
                    throw new NotSupportedException($"Condition non gérée : {condition.GetType().Name}");
            }
        }

        // Première couleur dans B, seconde dans C (la même si elle est omise). first : rang de la première couleur.
        void EmitColors(IReadOnlyList<Expression> arguments, int first)
        {
            EmitExpression(arguments[first]);
            if (arguments.Count > first + 1)
            {
                emitter.PushHl();
                EmitExpression(arguments[first + 1]);
                emitter.LdAL();
                emitter.LdCA();          // C = seconde couleur
                emitter.PopHl();
                emitter.LdAL();
                emitter.LdBA();          // B = première couleur
            }
            else
            {
                emitter.LdAL();
                emitter.LdBA();          // B = couleur
                emitter.LdCA();          // C = la même couleur
            }
        }

        // Une valeur dans A, puis appel de la routine du firmware
        void EmitCallWithA(Expression value, int address)
        {
            EmitExpression(value);
            emitter.LdAL();
            emitter.CallAddress(address);
        }

        // Choisit la fenêtre (lot H1, D072) ; null : la fenêtre #0. Rend A = fenêtre choisie avant.
        void EmitSelectStream(Expression? stream)
        {
            switch (stream)
            {
                case null:
                    emitter.XorA();
                    emitter.CallAddress(TxtStrSelect);
                    break;
                case NumberExpression number:           // nombre écrit : déjà vérifié par le parser
                    emitter.LdAImm(number.Value);
                    emitter.CallAddress(TxtStrSelect);
                    break;
                default:                                // valeur calculée : vérifiée pendant l'exécution
                    EmitExpression(stream);
                    required.Add(RuntimeLibrary.StreamName);
                    emitter.CallLabel(RuntimeLibrary.StreamLabel);
                    break;
            }
        }

        // 1 : appelle « on », 0 : appelle « off » ; une valeur calculée : tout ce qui n'est pas 0 vaut 1 (D102)
        void EmitSwitch(Expression value, int on, int off)
        {
            if (value is NumberExpression number)
            {
                emitter.CallAddress(number.Value != 0 ? on : off);
                return;
            }
            string isOff = $"sw_{labelCount++}";
            string done = $"sw_{labelCount++}";
            EmitExpression(value);
            emitter.LdAH();
            emitter.OrL();
            emitter.JrZLabel(isOff);
            emitter.CallAddress(on);
            emitter.JrLabel(done);
            emitter.Label(isOff);
            emitter.CallAddress(off);
            emitter.Label(done);
        }

        void EmitScreen(ScreenStatement screen)
        {
            IReadOnlyList<Expression> a = screen.Arguments;
            switch (screen.Command)
            {
                case "CLS":
                    emitter.CallAddress(TxtClearWindow);
                    break;

                case "SOUND":
                {
                    // SOUND (D071) : bloc de 9 octets pour SOUND QUEUE : canal, env. volume, env. ton, période (2),
                    // bruit, volume, durée (2). File pleine : on attend qu'une place se libère, comme le BASIC.
                    usesSound = true;
                    void Store(int index, int byteIndex, bool word)
                    {
                        EmitExpression(a[index]);
                        emitter.LdAL();
                        emitter.LdMemA($"snd_b{byteIndex}");
                        if (word)
                        {
                            emitter.LdAH();
                            emitter.LdMemA($"snd_b{byteIndex + 1}");
                        }
                    }
                    Store(0, 0, false);
                    Store(4, 1, false);
                    Store(5, 2, false);
                    Store(1, 3, true);
                    Store(6, 5, false);
                    Store(3, 6, false);
                    Store(2, 7, true);
                    string wait = $"snd_{labelCount++}";
                    emitter.Label(wait);
                    emitter.LdHlLabel("snd_b0");
                    emitter.CallAddress(SoundQueue);
                    emitter.JpNcLabel(wait);
                    break;
                }

                case "VROOM":                       // VROOM son (D213) : 1 monte, 2 ralenti, 3 descend
                    required.Add(RuntimeLibrary.VroomName);
                    EmitExpression(a[0]);
                    if (a[0] is not NumberExpression)   // calculé hors de 1 à 3 : « Improper argument »
                    {
                        required.Add(RuntimeLibrary.ByteCheckName);
                        emitter.LdAH();
                        emitter.OrA();
                        emitter.JpNzLabel(RuntimeLibrary.ByteCheckLabel);
                        emitter.LdAL();
                        emitter.DecA();
                        emitter.CpImm(3);
                        emitter.JpNcLabel(RuntimeLibrary.ByteCheckLabel);
                    }
                    else
                    {
                        emitter.LdAL();
                        emitter.DecA();
                    }
                    emitter.CallLabel(VroomRuntime.EntryLabel);
                    break;

                case "BOOM":                        // BOOM son [, durée] (D214) : 1 voiture, 2 feu d'artifice, 3 pétard,
                {                                   // 4 marteau, 5 malus ; durée en centièmes de seconde
                    required.Add(RuntimeLibrary.BoomName);
                    bool checks = a.Any(x => x is not NumberExpression);
                    if (checks)
                        required.Add(RuntimeLibrary.ByteCheckName);
                    if (a.Count == 2)
                    {
                        EmitExpression(a[1]);
                        if (a[1] is not NumberExpression)   // durée calculée hors de 1 à 32767 : « Improper argument »
                        {
                            emitter.LdAH();
                            emitter.OrA();
                            emitter.JpMLabel(RuntimeLibrary.ByteCheckLabel);
                            emitter.OrL();
                            emitter.JpZLabel(RuntimeLibrary.ByteCheckLabel);
                        }
                        emitter.PushHl();
                    }
                    EmitExpression(a[0]);
                    if (a[0] is not NumberExpression)       // son calculé hors de 1 à 5 : « Improper argument »
                    {
                        emitter.LdAH();
                        emitter.OrA();
                        emitter.JpNzLabel(RuntimeLibrary.ByteCheckLabel);
                        emitter.LdAL();
                        emitter.DecA();
                        emitter.CpImm(5);
                        emitter.JpNcLabel(RuntimeLibrary.ByteCheckLabel);
                    }
                    else
                    {
                        emitter.LdAL();
                        emitter.DecA();
                    }
                    if (a.Count == 2)
                        emitter.PopHl();
                    else
                        emitter.LdHlImm(0);                 // la durée du son
                    emitter.CallLabel(BoomRuntime.EntryLabel);
                    break;
                }

                case "RELEASE":                     // RELEASE canaux (D071)
                    EmitCallWithA(a[0], SoundRelease);
                    break;

                case "DEG" or "RAD":                // angles de SIN, COS, TAN et ATN (D069)
                    emitter.LdAImm(screen.Command == "DEG" ? 1 : 0);
                    emitter.CallLabel(FirmwareMaths.AngleMode.Label);
                    break;

                case "WAIT":                        // WAIT port, masque [, inversion] (D094) : attend que
                {                                   // (INP(port) XOR inversion) AND masque ne vaille plus 0
                    string wait = $"wait_{labelCount++}";
                    EmitAddress(a[0]);
                    emitter.PushHl();
                    EmitExpression(a[1]);
                    emitter.PushHl();
                    if (a.Count == 3)
                        EmitExpression(a[2]);
                    else
                        emitter.LdHlImm(0);
                    emitter.LdEL();                                 // E = inversion
                    emitter.PopHl();
                    emitter.LdAL();
                    emitter.LdDA();                                 // D = masque
                    emitter.PopBc();                                // BC = port
                    emitter.Label(wait);
                    emitter.InAC();
                    emitter.XorE();
                    emitter.AndD();
                    emitter.JrZLabel(wait);
                    break;
                }

                case "FILL" or "PAINT":             // remplissage (D107) : notre propre routine
                    required.Add(RuntimeLibrary.FillName);
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    emitter.LdBImm(screen.Command == "FILL" ? 1 : 0);
                    emitter.CallLabel(RuntimeLibrary.FillLabel);
                    break;

                case "GRAPHICS PEN":                // stylo des tracés (D106) : GRA SET PEN ; mode transparent (D110)
                    EmitCallWithA(a[0], GraSetPen);
                    if (a.Count == 2)
                    {
                        required.Add(RuntimeLibrary.MaskName);
                        EmitExpression(a[1]);
                        emitter.LdAL();
                        emitter.LdMemA(RuntimeLibrary.MaskTransparentLabel);
                        emitter.CallLabel(RuntimeLibrary.MaskInstallLabel);
                    }
                    break;

                case "MASK":                        // motif des lignes [, premier point] (D110)
                    required.Add(RuntimeLibrary.MaskName);
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    emitter.LdMemA(RuntimeLibrary.MaskPatternLabel);
                    if (a.Count == 2)
                    {
                        EmitExpression(a[1]);
                        emitter.LdAL();
                        emitter.LdMemA(RuntimeLibrary.MaskFirstLabel);
                    }
                    emitter.CallLabel(RuntimeLibrary.MaskInstallLabel);
                    break;

                case "MASK FIRST":                  // MASK ,premier point (D123) : le motif ne change pas
                    required.Add(RuntimeLibrary.MaskName);
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    emitter.LdMemA(RuntimeLibrary.MaskFirstLabel);
                    emitter.CallLabel(RuntimeLibrary.MaskInstallLabel);
                    break;

                case "GRAPHICS PAPER":              // fond des tracés, pour CLG (D106) : GRA SET PAPER
                    EmitCallWithA(a[0], GraSetPaper);
                    break;

                case "FRAME":                       // attend le début du balayage de l'écran (D102)
                    emitter.CallAddress(0xBD19);    // MC WAIT FLYBACK
                    break;

                case "CURSOR":                      // CURSOR système [, utilisateur] (D102) : TXT CUR ON / OFF, ENABLE / DISABLE
                    EmitSwitch(a[0], 0xBB81, 0xBB84);
                    if (a.Count == 2)
                        EmitSwitch(a[1], 0xBB7B, 0xBB7E);
                    break;

                case "CURSOR USER":                 // CURSOR ,utilisateur (D123) : TXT CUR ENABLE / DISABLE seulement
                    EmitSwitch(a[0], 0xBB7B, 0xBB7E);
                    break;

                case "WIDTH":                       // largeur de ligne de l'imprimante (D100)
                    required.Add(RuntimeLibrary.PrinterName);
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    emitter.LdMemA(RuntimeLibrary.PrinterWidthLabel);
                    break;

                case "MEMORY":                      // MEMORY adresse : réserve le haut de la mémoire ; rien à faire à l'exécution,
                {                                   // le compilateur vérifie que le programme tient en dessous
                    if (a[0] is NumberExpression limit && (memoryLimit < 0 || (limit.Value & 0xFFFF) < memoryLimit))
                    {
                        memoryLimit = limit.Value & 0xFFFF;
                        memoryLine = screen.SourceLine;
                    }
                    if (usesHeap)
                    {
                        EmitAddress(a[0]);                          // tableaux et textes restent sous cette adresse (D058, D062)
                        emitter.CallLabel(RuntimeLibrary.ArrayMemoryLabel);
                    }
                    else if (a[0] is not NumberExpression)          // adresse calculée : vérifiée à l'exécution (D125)
                    {
                        required.Add(RuntimeLibrary.MemoryCheckName);
                        EmitAddress(a[0]);
                        emitter.CallLabel(RuntimeLibrary.MemoryCheckLabel);
                    }
                    break;
                }

                case "POKE":                        // POKE adresse, valeur (0 à 255)
                    EmitAddressPair(a[0], a[1]);
                    EmitByteCheck(a[1]);
                    emitter.LdAE();
                    emitter.LdHlA();
                    break;

                case "OUT":                         // OUT port, valeur (0 à 255) : le port est dans BC
                    EmitAddressPair(a[0], a[1]);
                    EmitByteCheck(a[1]);
                    emitter.LdBH();
                    emitter.LdCL();
                    emitter.LdAE();
                    emitter.OutCA();
                    break;

                case "CALL":                        // CALL adresse : le sous-programme rend la main avec RET
                {
                    string target = $"call_{labelCount++}";
                    string after = $"call_{labelCount++}";
                    EmitAddress(a[0]);
                    emitter.CallLabel(target);
                    emitter.JpLabel(after);
                    emitter.Label(target);
                    emitter.JpHl();
                    emitter.Label(after);
                    break;
                }

                case "MODE":
                    EmitCallWithA(a[0], ScrSetMode);
                    break;

                case "WINDOW":
                {
                    // WINDOW gauche, droite, haut, bas (à partir de 1) -> H, D, L, E (à partir de 0). Le firmware remet
                    // dans l'ordre et ramène dans l'écran ce qui dépasse, comme le Locomotive (D072).
                    for (int i = 0; i < 3; i++)
                    {
                        EmitExpression(a[i]);
                        emitter.PushHl();
                    }
                    EmitExpression(a[3]);
                    emitter.LdAL();
                    emitter.DecA();
                    emitter.LdEA();                     // E = bas
                    emitter.PopHl();
                    emitter.LdAL();
                    emitter.DecA();
                    emitter.LdCA();                     // C = haut (pour l'instant)
                    emitter.PopHl();
                    emitter.LdAL();
                    emitter.DecA();
                    emitter.LdDA();                     // D = droite
                    emitter.PopHl();
                    emitter.LdAL();
                    emitter.DecA();
                    emitter.LdHA();                     // H = gauche
                    emitter.LdLC();                     // L = haut
                    emitter.CallAddress(TxtSetWindow);
                    break;
                }

                case "SYMBOL":                          // SYMBOL caractère, lignes (D075) : lignes absentes = 0
                    for (int row = 0; row < 8; row++)
                    {
                        if (row + 1 < a.Count)
                        {
                            EmitExpression(a[row + 1]);
                            emitter.LdAL();
                        }
                        else
                        {
                            emitter.XorA();
                        }
                        emitter.LdMemA($"rt_sym_r{row}");
                    }
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    required.Add(RuntimeLibrary.SymbolName);
                    emitter.CallLabel(RuntimeLibrary.SymbolLabel);
                    break;

                case "SYMBOL AFTER":                    // D075 : table des caractères redessinables
                    EmitExpression(a[0]);
                    required.Add(RuntimeLibrary.SymbolAfterName);
                    emitter.CallLabel(RuntimeLibrary.SymbolAfterLabel);
                    break;

                case "DI" or "EI":                      // D079 : retient ou laisse partir les appels des minuteries
                    required.Add(RuntimeLibrary.EventsName);
                    emitter.LdAImm(screen.Command == "DI" ? 1 : 0);
                    emitter.LdMemA(RuntimeLibrary.EventsDiLabel);
                    break;

                case "TAG" or "TAGOFF":                 // D075 : TXT SET GRAPHIC, texte au curseur graphique ou non
                    emitter.LdAImm(screen.Command == "TAG" ? 1 : 0);
                    emitter.CallAddress(0xBB63);
                    break;

                case "ZONE":                            // ZONE largeur (D073) : vérifiée pendant l'exécution (1 à 255)
                    EmitExpression(a[0]);
                    required.Add(RuntimeLibrary.PrintFormatName);
                    emitter.CallLabel(RuntimeLibrary.ZoneSetLabel);
                    break;

                case "WINDOW SWAP":                     // WINDOW SWAP a [, b] : b vaut 0 s'il est omis (D072)
                    EmitExpression(a[0]);
                    if (a.Count > 1)
                    {
                        emitter.PushHl();
                        EmitExpression(a[1]);
                        emitter.LdCL();
                        emitter.PopHl();
                    }
                    else
                    {
                        emitter.LdCImm(0);
                    }
                    emitter.LdAL();
                    emitter.LdBA();
                    emitter.CallAddress(TxtSwapStreams);
                    break;

                case "PEN":
                    EmitCallWithA(a[0], TxtSetPen);
                    if (a.Count == 2)
                        EmitCallWithA(a[1], 0xBB9F);    // TXT SET BACK : 0 opaque, 1 transparent (D104)
                    break;

                case "PAPER":
                    EmitCallWithA(a[0], TxtSetPaper);
                    break;

                case "LOCATE":
                    // H = colonne, L = ligne
                    EmitExpression(a[0]);
                    emitter.PushHl();
                    EmitExpression(a[1]);
                    emitter.PopDe();
                    emitter.LdHE();
                    emitter.CallAddress(TxtSetCursor);
                    break;

                case "INK":
                    // A = stylo, B = couleur, C = seconde couleur
                    EmitExpression(a[0]);
                    emitter.PushHl();
                    EmitColors(a, 1);
                    emitter.PopHl();
                    emitter.LdAL();
                    emitter.CallAddress(ScrSetInk);
                    break;

                case "BORDER":
                    EmitColors(a, 0);
                    emitter.CallAddress(ScrSetBorder);
                    break;

                case "CLG":
                    // Avec un stylo : il devient le fond graphique, puis la fenêtre graphique est effacée
                    if (a.Count > 0)
                        EmitCallWithA(a[0], GraSetPaper);
                    emitter.CallAddress(GraClearWindow);
                    break;

                case "ORIGIN":
                    // DE = x, HL = y
                    EmitExpression(a[0]);
                    emitter.PushHl();
                    EmitExpression(a[1]);
                    emitter.PopDe();
                    emitter.CallAddress(GraSetOrigin);
                    if (a.Count == 6)
                    {
                        // fenêtre graphique (D104) : GRA WIN WIDTH (DE = gauche, HL = droite), GRA WIN HEIGHT (haut, bas)
                        EmitExpression(a[2]);
                        emitter.PushHl();
                        EmitExpression(a[3]);
                        emitter.PopDe();
                        emitter.CallAddress(0xBBCF);
                        EmitExpression(a[4]);
                        emitter.PushHl();
                        EmitExpression(a[5]);
                        emitter.PopDe();
                        emitter.CallAddress(0xBBD2);
                    }
                    break;

                case "BOX":
                {
                    // x1, y1, x2, y2 sont rangés dans le module, puis A = stylo (1 s'il est omis)
                    string[] cells = [RuntimeLibrary.BoxX1Label, RuntimeLibrary.BoxY1Label,
                                      RuntimeLibrary.BoxX2Label, RuntimeLibrary.BoxY2Label];
                    for (int i = 0; i < 4; i++)
                    {
                        EmitExpression(a[i]);
                        emitter.LdMemHl(cells[i]);
                    }
                    // 5e valeur : stylo du contour (stylo courant du tracé s'il est omis) ; 6e : stylo de l'intérieur (pas d'intérieur s'il est omis)
                    if (a.Count > 4)
                    {
                        EmitExpression(a[4]);
                        emitter.LdAL();
                    }
                    else
                        emitter.CallAddress(GraGetPen);
                    emitter.LdMemA(RuntimeLibrary.BoxPenLabel);
                    if (a.Count > 5)
                    {
                        EmitExpression(a[5]);
                        emitter.LdAL();
                        emitter.LdMemA(RuntimeLibrary.BoxFillPenLabel);
                    }
                    emitter.LdHlImm(a.Count > 5 ? 1 : 0);
                    emitter.LdAL();
                    emitter.LdMemA(RuntimeLibrary.BoxDoFillLabel);
                    required.Add(RuntimeLibrary.BoxName);
                    emitter.CallLabel(RuntimeLibrary.BoxLabel);
                    break;
                }

                case "SPRITE":                      // D198 : SPRITE n, x, y (montré ou déplacé) ; SPRITE n (caché)
                    required.Add(RuntimeLibrary.SpriteName);
                    EmitExpression(a[0]);
                    emitter.LdAL();
                    emitter.LdMemA(SpriteRuntime.NumberLabel);
                    if (a.Count == 3)
                    {
                        EmitExpression(a[1]);
                        emitter.LdMemHl(SpriteRuntime.XLabel);
                        EmitExpression(a[2]);
                        emitter.LdMemHl(SpriteRuntime.YLabel);
                        emitter.CallLabel(SpriteRuntime.ShowLabel);
                    }
                    else
                        emitter.CallLabel(SpriteRuntime.HideLabel);
                    break;

                case "CIRCLE":                      // D130 : x, y, rayon rangés dans le module, puis les stylos comme BOX
                {
                    // D141 : priorité « place » et ELLIPSE dans le programme : le module Ellipse avec deux rayons égaux
                    bool viaEllipse = circleViaEllipse;
                    EmitExpression(a[0]);
                    emitter.LdMemHl(viaEllipse ? RuntimeLibrary.EllipseXLabel : RuntimeLibrary.CircleXLabel);
                    EmitExpression(a[1]);
                    emitter.LdMemHl(viaEllipse ? RuntimeLibrary.EllipseYLabel : RuntimeLibrary.CircleYLabel);
                    EmitExpression(a[2]);
                    emitter.LdMemHl(viaEllipse ? RuntimeLibrary.EllipseRadiusXLabel : RuntimeLibrary.CircleRadiusLabel);
                    if (viaEllipse)
                        emitter.LdMemHl(RuntimeLibrary.EllipseRadiusYLabel);
                    if (a[2] is not NumberExpression)       // rayon calculé hors de 0 à 1000 : « Improper argument »
                    {
                        required.Add(RuntimeLibrary.ByteCheckName);
                        emitter.LdDeImm(1001);
                        emitter.OrA();
                        emitter.SbcHlDe();
                        emitter.JpNcLabel(RuntimeLibrary.ByteCheckLabel);
                    }
                    if (viaEllipse)                         // angle 0
                    {
                        emitter.LdHlImm(0);
                        emitter.LdMemHl(RuntimeLibrary.EllipseAngleLabel);
                    }
                    if (a.Count > 3)                        // stylo du contour (stylo courant du tracé s'il est omis)
                    {
                        EmitExpression(a[3]);
                        emitter.LdAL();
                    }
                    else
                        emitter.CallAddress(GraGetPen);
                    emitter.LdMemA(viaEllipse ? RuntimeLibrary.EllipsePenLabel : RuntimeLibrary.CirclePenLabel);
                    if (a.Count > 4)                        // stylo de l'intérieur (pas d'intérieur s'il est omis)
                    {
                        EmitExpression(a[4]);
                        emitter.LdAL();
                        emitter.LdMemA(viaEllipse ? RuntimeLibrary.EllipseFillPenLabel : RuntimeLibrary.CircleFillPenLabel);
                    }
                    emitter.LdAImm(a.Count > 4 ? 1 : 0);
                    emitter.LdMemA(viaEllipse ? RuntimeLibrary.EllipseDoFillLabel : RuntimeLibrary.CircleDoFillLabel);
                    required.Add(viaEllipse ? RuntimeLibrary.EllipseName : RuntimeLibrary.CircleName);
                    emitter.CallLabel(viaEllipse ? RuntimeLibrary.EllipseLabel : RuntimeLibrary.CircleLabel);
                    break;
                }

                default:
                    throw new NotSupportedException($"Commande d'écran non gérée : {screen.Command}");
            }
        }

        // MOVE, MOVER, PLOT, PLOTR, DRAW, DRAWR : DE = x, HL = y ; le stylo et le mode (facultatifs) sont réglés avant
        void EmitDraw(DrawStatement draw)
        {
            int address = draw.Command switch
            {
                "MOVE" => GraMoveAbsolute,
                "MOVER" => GraMoveRelative,
                "PLOT" => GraPlotAbsolute,
                "PLOTR" => GraPlotRelative,
                "DRAW" => GraLineAbsolute,
                "DRAWR" => GraLineRelative,
                _ => throw new NotSupportedException($"Commande de tracé non gérée : {draw.Command}")
            };

            EmitExpression(draw.X);
            emitter.PushHl();
            EmitExpression(draw.Y);
            emitter.PushHl();

            if (draw.Pen is not null && fastPlot && draw.Command is "PLOT" or "PLOTR")
            {
                EmitExpression(draw.Pen);                           // D210 : le stylo gardé par nos points rapides
                emitter.LdAL();
                required.Add(RuntimeLibrary.LineName);
                emitter.CallLabel(LineRuntime.PlotPenLabel);
            }
            else if (draw.Pen is not null)
                EmitCallWithA(draw.Pen, GraSetPen);
            if (draw.Mode is not null)
                EmitCallWithA(draw.Mode, ScrAccess);

            emitter.PopHl();       // y
            emitter.PopDe();       // x
            if (fastPlot && draw.Pen is null && draw.Command is "MOVE" or "MOVER")
            {
                required.Add(RuntimeLibrary.LineName);              // D212 : seulement notre curseur gardé
                emitter.CallLabel(draw.Command == "MOVE" ? LineRuntime.MoveLabel : LineRuntime.MoveRelativeLabel);
                return;
            }
            if (fastPlot && draw.Command is "PLOT" or "PLOTR")
            {
                required.Add(RuntimeLibrary.LineName);              // D210 : nos points rapides
                emitter.CallLabel(draw.Command == "PLOT" ? LineRuntime.PlotLabel : LineRuntime.PlotRelativeLabel);
                return;
            }
            bool masked = usesMask && draw.Command is "DRAW" or "DRAWR";
            if (masked)
                emitter.CallLabel(RuntimeLibrary.MaskBeginLabel);   // MASK et fond transparent pendant la ligne (D110)
            if (!usesMask && draw.Command is "DRAW" or "DRAWR")
            {
                required.Add(RuntimeLibrary.LineName);              // D204 : nos traits rapides (avec MASK : la ROM)
                emitter.CallLabel(draw.Command == "DRAW"
                    ? directLines ? LineRuntime.DirectLabel : LineRuntime.LineLabel
                    : directLines ? LineRuntime.DirectRelativeLabel : LineRuntime.RelativeLabel);
            }
            else
                emitter.CallAddress(address);
            if (masked)
                emitter.CallLabel(RuntimeLibrary.MaskEndLabel);
        }

        // Étiquettes des lignes numérotées, placées juste avant l'instruction qui les suit (GOTO, GOSUB, ON ; D054)
        string LineLabel(int number) => $"line_{number}";
        var linesAt = program.LineTable
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).ToList());

        void EmitLineLabels(int statementIndex)
        {
            if (linesAt.TryGetValue(statementIndex, out List<int>? numbers))
                foreach (int number in numbers)
                    emitter.Label(LineLabel(number));
        }

        // DATA (D059) : tous les nombres, dans l'ordre du programme. dataStart : rang de l'instruction DATA -> rang de son
        // premier nombre. RESTORE ligne repart au premier DATA de cette ligne ou après (étiquette data_at_N, N = rang du nombre).
        var dataValues = new List<DataItem>();
        var dataStart = new SortedDictionary<int, int>();
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is DataStatement data)
            {
                dataStart[i] = dataValues.Count;
                dataValues.AddRange(data.Items);
            }
        }
        bool usesData = program.Statements.Any(st => st is ReadStatement or RestoreStatement);
        var restorePoints = new SortedSet<int>();

        string RestoreLabel(int? line)
        {
            int offset = 0;
            if (line is int number)
            {
                int firstIndex = program.LineTable[number];
                offset = dataStart.Where(pair => pair.Key >= firstIndex).Select(pair => pair.Value).DefaultIfEmpty(dataValues.Count).First();
            }
            restorePoints.Add(offset);
            return $"data_at_{offset}";
        }

        if (usesData)
        {
            required.Add(RuntimeLibrary.DataName);
            // Nombres à virgule dans DATA ou READ A! (D070) : lecture par le module DataReal
            required.Add(usesReals ? RuntimeLibrary.DataRealName : RuntimeLibrary.DataNoRealName);
        }

        // Sans chargeur BASIC (D168) : lancé par RUN"NOM" (fichier binaire), le CPC a tout remis à zéro et la disquette n'est
        // plus branchée (OPENOUT demanderait la cassette). Si l'entrée CAS IN OPEN (&BC77) est encore celle du firmware
        // (RST 1, &CF), on rebranche les ROM d'extension comme le fait le BASIC : KL ROM WALK, mémoire de &0040 à &ABFF
        // (AMSDOS prend le haut, HIMEM redevient &A67B). Déjà branchée (programme lancé par RUN "nom") : rien.
        if (!options.WithLoader)
        {
            emitter.Bytes(0x3A, 0x77, 0xBC);                        // LD A,(&BC77)
            emitter.CpImm(0xCF);
            emitter.JrNzLabel("prog_roms_ok");
            emitter.LdDeImm(0x0040);
            emitter.LdHlImm(0xABFF);
            emitter.CallAddress(0xBCCB);                            // KL ROM WALK
            emitter.Label("prog_roms_ok");
        }

        // RUN (D090) : la pile du départ est gardée ; RUN la remet et recommence ici
        bool usesRun = program.Statements.Any(st => st is RestartStatement);
        bool usesClear = usesRun || program.Statements.Any(st => st is ClearStatement);
        if (usesRun)
        {
            emitter.LdMemSp("rt_run_sp");
            emitter.Label("prog_restart");
        }

        // Nombres à virgule (D068) : table des routines de la ROM selon la machine, angles en radians
        if (usesReals)
        {
            required.Add(RuntimeLibrary.MathsModuleFor(options.Target));
            required.Add(RuntimeLibrary.RealOpsName);
            required.Add(RuntimeLibrary.RealPrintName);
            emitter.CallLabel(RuntimeLibrary.MathsInitLabel);
        }

        // Tableaux : la mémoire libre commence juste après le programme (D058)
        if (usesHeap)
        {
            required.Add(RuntimeLibrary.HeapName);                  // HIMEM et FRE seuls (D094)
            if (arrays.Count > 0)
                required.Add(RuntimeLibrary.ArrayName);
            if (usesStrings)
                required.Add(RuntimeLibrary.StringsName);           // textes (D062)
            emitter.LdHlLabel(RuntimeLibrary.ArrayHeapStartLabel);
            emitter.LdMemHl(RuntimeLibrary.ArrayHeapLabel);
        }

        // Minuteries (lot I1, D079) : le compteur du firmware est branché au départ
        // Touche ESC (D083) : réglage F9 de l'éditeur, ou ON BREAK dans le programme
        bool usesBreak = options.BreakKey || program.Statements.Any(st => st is OnBreakStatement);
        bool usesEvents = usesBreak || program.Statements.Any(st => st is TimerStatement or OnSqStatement);   // D079, D082
        bool usesOnError = program.Statements.Any(st => st is OnErrorStatement);   // D080
        var errorRecords = new List<(int Index, int Line)>();
        if (usesEvents)
        {
            required.Add(RuntimeLibrary.EventsName);
            emitter.CallLabel(RuntimeLibrary.EventsInitLabel);
            if (usesBreak)
            {
                // Au départ : ESC comme le vrai BASIC (STOP) si le réglage est mis, sinon ignorée (CONT) (D083)
                emitter.LdAImm(options.BreakKey ? (int)BreakMode.Stop : (int)BreakMode.Cont);
                emitter.LdMemA(RuntimeLibrary.BreakModeLabel);
            }
        }
        int breakLine = -1;   // dernière ligne rangée pour « Break in N » (D083)
        bool usesTrace = program.Statements.Any(st => st is TraceStatement);   // TRON (D088)
        int traceLine = -1;

        // RUN ligne (D090) : après la mise en route, on saute à la ligne demandée
        if (program.Statements.Any(st => st is RestartStatement { Target: not null }))
        {
            emitter.LdHlMem("rt_run_go");
            emitter.LdAH();
            emitter.OrL();
            emitter.JrZLabel("prog_run_top");
            emitter.LdDeImm(0);
            emitter.LdMemDe("rt_run_go");
            emitter.JpHl();
            emitter.Label("prog_run_top");
        }

        int statementIndex = 0;
        int basicLine = 0;   // numéro de la ligne BASIC en cours, pour les messages d'erreur des tableaux
        bool insideStream = false;
        bool fileGroup = false;     // entre PRINT #9 (ou INPUT #9...) et la fin de l'instruction (D084)
        bool fileOutput = false;    // ... et les caractères vont dans le fichier
        bool printerGroup = false;  // entre PRINT #8 et la fin de l'instruction (D100)
        // D210 : PLOT rapides sur CPC : écriture normale toujours (comme D205), sans MASK, sans événements ni TRON (qui
        // peuvent changer l'écran entre deux instructions). Les réglages (écran, MODE, origine, fenêtre, stylo) sont lus
        // une fois puis gardés ; toute instruction qui pourrait les changer les fait relire (SafeForPlot).
        fastPlot = !options.Cartridge && directLines && !usesMask && !usesTrace
                   && program.Statements.Any(st => st is DrawStatement { Command: "PLOT" or "PLOTR" })
                   && !program.Statements.Any(st => st is TimerStatement or OnSqStatement or OnBreakStatement);
        foreach (Statement statement in program.Statements)
        {
            if (linesAt.TryGetValue(statementIndex, out List<int>? numbersHere))
                basicLine = numbersHere.Max();
            EmitLineLabels(statementIndex++);

            if (fastPlot && !SafeForPlot(statement))
                emitter.CallLabel(LineRuntime.PlotSyncLabel);        // D210 : curseur et stylo donnés à la ROM, réglages à relire
            else if (fastPlot && (statement is DrawStatement { Command: "DRAW" or "DRAWR" }
                                  || AstWalker.All(statement).Any(x => x is CallExpression { Function: "XPOS" or "YPOS" or "TEST" or "TESTR" })))
                emitter.CallLabel(LineRuntime.PlotFlushLabel);       // D210 : curseur et stylo donnés à la ROM

            // TRON (D088) : au début de chaque ligne, son numéro entre crochets si TRON est en marche
            if (usesTrace && program.IsStart(statement))
            {
                int here = program.LineTable.Count > 0 ? basicLine : statement.SourceLine;
                if (here != traceLine)
                {
                    traceLine = here;
                    required.Add(RuntimeLibrary.TraceName);
                    emitter.LdHlImm(here);
                    emitter.CallLabel("rt_tron");
                }
            }

            // Touche ESC (D083) : chaque ligne range son numéro avant de regarder les événements, pour « Break in N »
            if (usesBreak && program.IsStart(statement))
            {
                int here = program.LineTable.Count > 0 ? basicLine : statement.SourceLine;
                if (here != breakLine)
                {
                    breakLine = here;
                    emitter.LdHlImm(here);
                    emitter.LdMemHl(RuntimeLibrary.ErrorLineLabel);
                }
            }

            // Minuteries (D079) : au début de chaque ligne et avant chaque fin de boucle, on regarde si une minuterie
            // est arrivée au bout (7 octets ; pas au milieu d'un PRINT #n). Seulement si le programme utilise AFTER, EVERY.
            if (usesEvents && !insideStream
                && (program.IsStart(statement)
                    || statement is NextStatement or WendStatement or UntilStatement or LoopStatement))
            {
                emitter.LdAMem(RuntimeLibrary.EventsPendingLabel);
                emitter.OrA();
                emitter.CallNzLabel(RuntimeLibrary.EventsRunLabel);
            }
            if (statement is StreamStatement streamMark)
                insideStream = streamMark.Stream is not null;

            // ON ERROR GOTO (D080) : au début de chaque instruction BASIC, la pile et la fiche de l'instruction
            // (ligne, début, instruction suivante) sont rangées pour ERL, RESUME et RESUME NEXT (10 octets)
            if (usesOnError && program.IsStart(statement))
            {
                int index = statementIndex - 1;
                emitter.Label($"stmt_{index}");
                emitter.LdMemSp(RuntimeLibrary.OnErrorSpLabel);
                emitter.LdHlLabel($"erec_{index}");
                emitter.LdMemHl(RuntimeLibrary.OnErrorRecordLabel);
                errorRecords.Add((index, program.LineTable.Count > 0 ? basicLine : statement.SourceLine));
            }

            // Une instruction qui touche un tableau note son numéro de ligne (« Subscript out of range in 30 ») ;
            // sans numéros de ligne, c'est la ligne de l'éditeur.
            // Textes : les fiches temporaires de l'instruction précédente sont rendues (D062)
            if (usesStrings && (AstWalker.All(statement).Any(e => e is ConcatExpression || (e is CallExpression c && c.Function.EndsWith('$')))
                                || (statement is ReadStatement r && r.Targets.Any(Types.IsString))))
                emitter.CallLabel(RuntimeLibrary.StrResetLabel);

            if (AstWalker.UsesArrays(statement) || statement is ReadStatement || StatementUsesStrings(statement)
                || (usesReals && StatementUsesReals(statement))
                || (usesHeap && statement is ScreenStatement { Command: "MEMORY" })
                || statement is ScreenStatement { Command: "MEMORY", Arguments: [not NumberExpression] }   // D125
                || statement is ScreenStatement { Command: "POKE" or "OUT", Arguments: [_, not NumberExpression] }   // D128
                || statement is ScreenStatement { Command: "CIRCLE", Arguments: [_, _, not NumberExpression, ..] }   // D130
                || statement is ScreenStatement { Command: "VROOM", Arguments: [not NumberExpression] }   // D213
                || statement is ScreenStatement { Command: "BOOM" } boomCheck && boomCheck.Arguments.Any(x => x is not NumberExpression)   // D214
                || statement is EllipseStatement { RadiusX: not NumberExpression } or EllipseStatement { RadiusY: not NumberExpression }   // D139
                || statement is StreamStatement { Stream: not null and not NumberExpression }
                || statement is ScreenStatement { Command: "ZONE" or "SYMBOL" or "SYMBOL AFTER" }
                || statement is FileStatement or SaveStatement or LoadStatement or SpeedWriteStatement or RunFileStatement
                || statement is ExternalCallStatement { Rsx: not null } || statement is KeyStatement or KeyDefStatement
                || statement is StreamStatement { Stream: NumberExpression { Value: 9 } }
                || (statement is OnStatement onCheck && OnNeedsCheck(onCheck))
                || statement is PrintUsingDynamicStatement      // « Improper argument », « Type mismatch » (D098)
                || AstWalker.All(statement).Any(x => x is CallExpression { Function: "DEC$" })   // D103
                || statement is TimerStatement or OnSqStatement or ErrorStatement or ResumeStatement or StopStatement or ReturnStatement
                || AstWalker.All(statement).Any(x => x is CallExpression { Function: "REMAIN" }))   // #N calculé : « Improper argument in 20 » (D072)
            {
                emitter.LdHlImm(program.LineTable.Count > 0 ? basicLine : statement.SourceLine);
                emitter.LdMemHl(RuntimeLibrary.ErrorLineLabel);
            }

            switch (statement)
            {
                case GotoStatement jump:
                    emitter.JpLabel(LineLabel(jump.Target));
                    break;

                case GosubStatement call:                           // le compteur sert à « Unexpected RETURN » (D081)
                    required.Add(RuntimeLibrary.GosubName);
                    emitter.LdHlLabel(RuntimeLibrary.GosubCountLabel);
                    emitter.IncMemHl();
                    emitter.CallLabel(LineLabel(call.Target));
                    break;

                case OnErrorStatement onError:                      // ON ERROR GOTO ligne / 0 (D080)
                    required.Add(RuntimeLibrary.OnErrorName);
                    if (onError.Target == 0)
                    {
                        emitter.LdHlImm(0);
                    }
                    else
                    {
                        emitter.LdHlLabel(LineLabel(onError.Target));
                        emitter.LdMemHl(RuntimeLibrary.OnErrorHandlerLabel);
                        emitter.LdHlLabel(RuntimeLibrary.OnErrorTrapLabel);
                    }
                    emitter.LdMemHl(RuntimeLibrary.ErrorTrapLabel);
                    break;

                case ResumeStatement resume:                        // RESUME, RESUME NEXT, RESUME ligne (D080)
                    required.Add(RuntimeLibrary.OnErrorName);
                    switch (resume.Kind)
                    {
                        case ResumeKind.Same:
                            emitter.JpLabel("rt_oe_resume");
                            break;
                        case ResumeKind.Next:
                            emitter.JpLabel("rt_oe_resume_next");
                            break;
                        default:
                            emitter.CallLabel("rt_oe_resume_line");
                            emitter.JpLabel(LineLabel(resume.Target));
                            break;
                    }
                    break;

                case ErrorStatement error:                          // ERROR numéro (D080)
                    EmitExpression(error.Code);
                    required.Add(RuntimeLibrary.ErrorCodesName);
                    emitter.JpLabel(RuntimeLibrary.RaiseLabel);
                    break;

                case OnBreakStatement onBreak:                      // ON BREAK CONT / STOP / GOSUB ligne (D083)
                    required.Add(RuntimeLibrary.EventsName);
                    if (onBreak.Mode == BreakMode.Gosub)
                    {
                        emitter.LdHlLabel(LineLabel(onBreak.Target));
                        emitter.LdMemHl(RuntimeLibrary.BreakTargetLabel);
                    }
                    emitter.LdAImm((int)onBreak.Mode);
                    emitter.LdMemA(RuntimeLibrary.BreakModeLabel);
                    break;

                case OnSqStatement onSq:                            // ON SQ(canal) GOSUB ligne (D082)
                    required.Add(RuntimeLibrary.EventsName);
                    EmitExpression(onSq.Channel);
                    emitter.LdDeLabel(LineLabel(onSq.Target));
                    emitter.CallLabel(RuntimeLibrary.SoundEventSetLabel);
                    break;

                case TimerStatement timer:                          // AFTER / EVERY durée, minuterie GOSUB ligne (D079)
                    required.Add(RuntimeLibrary.EventsName);
                    EmitExpression(timer.Time);
                    emitter.LdMemHl("rt_ev_tc");
                    if (!timer.Every)
                        emitter.LdHlImm(0);                         // AFTER : une seule fois
                    emitter.LdMemHl("rt_ev_tr");
                    emitter.LdHlLabel(LineLabel(timer.Target));
                    emitter.LdMemHl("rt_ev_tt");
                    EmitExpression(timer.Timer);
                    emitter.LdAL();
                    emitter.CallLabel(RuntimeLibrary.EventsSetLabel);
                    break;

                case ReturnStatement:                               // « Unexpected RETURN » sans GOSUB en cours (D081)
                    required.Add(RuntimeLibrary.GosubName);
                    emitter.JpLabel(RuntimeLibrary.ReturnLabel);
                    break;

                case StopStatement:                                 // « Break in N », puis fin comme le vrai BASIC (D081)
                    required.Add(RuntimeLibrary.ErrorName);
                    emitter.LdHlLabel(RuntimeLibrary.BreakMessageLabel);
                    emitter.JpLabel(RuntimeLibrary.ErrorShowLabel);
                    break;

                case OnStatement on:
                {
                    // HL = valeur. Pour chaque ligne k (de 1 à N) : si la valeur vaut k, on saute ou on appelle ; sinon on continue.
                    // Hors de 0 à 255 : « Improper argument » (D097).
                    EmitExpression(on.Selector);
                    if (OnNeedsCheck(on))
                    {
                        required.Add(RuntimeLibrary.OnRangeName);
                        emitter.LdAH();
                        emitter.OrA();
                        emitter.JpNzLabel(RuntimeLibrary.OnRangeErrorLabel);
                    }
                    string done = $"on_{labelCount++}";
                    for (int k = 0; k < on.Targets.Count; k++)
                    {
                        emitter.LdDeImm(k + 1);
                        emitter.OrA();
                        emitter.SbcHlDe();              // HL = valeur - k ; l'indicateur Z dit « égal »
                        emitter.AddHlDe();              // remet HL ; ADD HL,DE ne touche pas à Z
                        if (on.IsGosub)
                        {
                            string next = $"on_{labelCount++}";
                            emitter.JrNzLabel(next);
                            required.Add(RuntimeLibrary.GosubName);
                            emitter.LdHlLabel(RuntimeLibrary.GosubCountLabel);
                            emitter.IncMemHl();
                            emitter.CallLabel(LineLabel(on.Targets[k]));
                            emitter.JpLabel(done);
                            emitter.Label(next);
                        }
                        else
                        {
                            emitter.JpZLabel(LineLabel(on.Targets[k]));
                        }
                    }
                    emitter.Label(done);
                    break;
                }

                case InputStatement fileInput when fileGroup:
                {
                    // INPUT #9 (D084) : une ligne du fichier, puis une valeur par variable, comme au clavier ; quand la ligne
                    // est finie, la suivante. Une valeur qui n'est pas un nombre donne 0 (vrai BASIC, SugarBox, 16 nov. 2026).
                    required.Add(RuntimeLibrary.FileInName);
                    emitter.CallLabel("rt_fl_line");
                    for (int i = 0; i < fileInput.Variables.Count; i++)
                    {
                        string fiName = fileInput.Variables[i];
                        IReadOnlyList<Expression>? fiCells = fileInput.Indexes?[i];
                        void FiTarget()
                        {
                            if (fiCells is not null)
                            {
                                EmitElementAddress(fiName, fiCells);
                                emitter.ExDeHl();
                            }
                            else
                            {
                                emitter.LdDeLabel(Slot(fiName).Label);
                            }
                        }
                        string fiOk = $"fin_{labelCount++}";
                        if (i > 0)
                            emitter.CallLabel("rt_fl_more");
                        if (VariableNames.IsString(fiName))
                        {
                            required.Add(RuntimeLibrary.InputStringName);
                            FiTarget();
                            emitter.CallLabel(RuntimeLibrary.InputStrLabel);
                            emitter.JrNcLabel(fiOk);
                            emitter.CallLabel("rt_fl_skip");
                            emitter.Label(fiOk);
                            continue;
                        }
                        if (VariableNames.IsReal(fiName))
                        {
                            required.Add(RuntimeLibrary.RealParseName);
                            FiTarget();
                            emitter.LdHlMem("rt_inp_ptr");
                            emitter.LdAImm(1);                          // lecture souple, comme VAL : pas de chiffre = 0
                            emitter.CallLabel(RealRuntime.ParseLabel);
                            emitter.CallLabel("rt_fl_after");
                            continue;
                        }
                        emitter.CallLabel("rt_fl_num");                 // HL = nombre (pas de chiffre : 0)
                        if (fiCells is not null)
                        {
                            emitter.PushHl();
                            EmitElementAddress(fiName, fiCells);
                            emitter.PopDe();
                            emitter.StoreHlE();
                            emitter.IncHl();
                            emitter.StoreHlD();
                        }
                        else
                        {
                            EmitStore(Slot(fiName));
                        }
                    }
                    break;
                }

                case LineInputStatement fileLine when fileGroup:      // LINE INPUT #9 : toute la ligne suivante (D084)
                    required.Add(RuntimeLibrary.FileInName);
                    required.Add(RuntimeLibrary.InputStringName);
                    emitter.CallLabel("rt_fl_line");
                    if (fileLine.Indexes is not null)
                    {
                        EmitElementAddress(fileLine.Variable, fileLine.Indexes);
                        emitter.ExDeHl();
                    }
                    else
                    {
                        emitter.LdDeLabel(Slot(fileLine.Variable).Label);
                    }
                    emitter.CallLabel("rt_input_all");
                    break;

                case InputStatement input:
                {
                    // Texte et « ? », lecture de la ligne, puis un nombre par variable. Un nombre faux (ou trop grand pour
                    // une variable BYTE) donne « ?Redo from start » et on recommence tout l'INPUT.
                    string retry = $"in_{labelCount++}";
                    string redo = $"in_{labelCount++}";
                    string done = $"in_{labelCount++}";
                    required.Add(RuntimeLibrary.InputName);

                    emitter.Label(retry);
                    if (input.Prompt is not null)
                        EmitText(input.Prompt);
                    if (input.Question)
                        EmitText("? ");
                    emitter.CallLabel(RuntimeLibrary.InputLineLabel);
                    if (!input.NoNewline)
                        EmitText("\r\n");

                    for (int i = 0; i < input.Variables.Count; i++)
                    {
                        if (i > 0)
                            emitter.CallLabel(RuntimeLibrary.InputMoreLabel);
                        if (VariableNames.IsString(input.Variables[i]))
                        {
                            // INPUT A$ (D062) ou INPUT N$(I) (D065) : DE = fiche qui reçoit le texte, avant toute lecture
                            required.Add(RuntimeLibrary.InputStringName);
                            if (input.Indexes?[i] is { } textIndexes)
                            {
                                EmitElementAddress(input.Variables[i], textIndexes);
                                emitter.ExDeHl();
                            }
                            else
                            {
                                emitter.LdDeLabel(Slot(input.Variables[i]).Label);
                            }
                            emitter.CallLabel(RuntimeLibrary.InputStrLabel);
                            emitter.JpCLabel(redo);
                            continue;
                        }
                        if (VariableNames.IsReal(input.Variables[i]))
                        {
                            // INPUT A! (D068) : le nombre tapé, écrit comme dans un programme (1.5, -2E3...)
                            required.Add(RuntimeLibrary.InputRealName);
                            if (input.Indexes?[i] is { } realIndexes)
                            {
                                EmitElementAddress(input.Variables[i], realIndexes);   // INPUT T!(I) (D070)
                                emitter.ExDeHl();
                            }
                            else
                            {
                                emitter.LdDeLabel(Slot(input.Variables[i]).Label);
                            }
                            emitter.CallLabel(RealRuntime.InputLabel);
                            emitter.JpCLabel(redo);
                            continue;
                        }
                        emitter.CallLabel(RuntimeLibrary.InputNumLabel);
                        emitter.JpCLabel(redo);
                        if (input.Indexes?[i] is { } cellIndexes)
                        {
                            emitter.PushHl();               // INPUT T(I) : le nombre va dans une case de tableau (D059)
                            EmitElementAddress(input.Variables[i], cellIndexes);
                            emitter.PopDe();
                            emitter.StoreHlE();
                            emitter.IncHl();
                            emitter.StoreHlD();
                            continue;
                        }
                        VariableSlot slot = Slot(input.Variables[i]);
                        if (slot.Type != VarType.Integer)
                        {
                            emitter.LdAH();                 // variable BYTE : la valeur doit tenir sur un octet
                            emitter.OrA();
                            emitter.JpNzLabel(redo);
                        }
                        EmitStore(slot);
                    }
                    emitter.CallLabel(RuntimeLibrary.InputEndLabel);
                    emitter.JpLabel(done);
                    emitter.Label(redo);
                    EmitText("?Redo from start\r\n");
                    emitter.JpLabel(retry);
                    emitter.Label(done);
                    break;
                }

                case LineInputStatement lineInput:
                    // LINE INPUT : texte, lecture de la ligne, puis toute la ligne dans la variable (D064)
                    required.Add(RuntimeLibrary.InputName);
                    required.Add(RuntimeLibrary.InputStringName);
                    if (lineInput.Prompt is not null)
                        EmitText(lineInput.Prompt);
                    if (lineInput.Question)
                        EmitText("? ");                             // comme INPUT (SugarBox, 16 nov. 2026, D093)
                    emitter.CallLabel(RuntimeLibrary.InputLineLabel);
                    if (!lineInput.NoNewline)
                        EmitText("\r\n");
                    if (lineInput.Indexes is not null)
                    {
                        EmitElementAddress(lineInput.Variable, lineInput.Indexes);   // LINE INPUT N$(I) (D065)
                        emitter.ExDeHl();
                    }
                    else
                    {
                        emitter.LdDeLabel(Slot(lineInput.Variable).Label);
                    }
                    emitter.CallLabel("rt_input_all");
                    break;

                case MidAssignStatement midAssign:
                {
                    // MID$(A$, début [, longueur]) = texte (D065)
                    required.Add(RuntimeLibrary.StrFunctionsName);
                    EmitString(midAssign.Target);                   // fiche de A$ (variable ou case)
                    emitter.PushHl();
                    EmitString(midAssign.Value);
                    emitter.PushHl();
                    EmitExpression(midAssign.Start);
                    emitter.PushHl();
                    if (midAssign.Length is not null)
                        EmitExpression(midAssign.Length);
                    else
                        emitter.LdHlImm(255);
                    emitter.CallLabel("rt_sf_byte");                // longueur de 0 à 255
                    emitter.LdCA();
                    emitter.PopHl();
                    emitter.CallLabel("rt_sf_byte");                // début de 0 à 255 (0 : refusé par rt_midset)
                    emitter.LdBA();
                    emitter.PopHl();
                    emitter.PopDe();
                    emitter.CallLabel("rt_midset");
                    break;
                }

                case ClearInputStatement:
                {
                    // On lit les touches en attente (KM READ CHAR) tant qu'il y en a : la retenue dit « un caractère a été lu »
                    string again = $"ci_{labelCount++}";
                    emitter.Label(again);
                    emitter.CallAddress(RuntimeLibrary.KmReadChar);
                    emitter.JrCLabel(again);
                    break;
                }

                case PrintStatement print:
                    if (print.Text.Length >= 2)                     // passe à la ligne s'il ne tient pas (D073)
                    {
                        required.Add(RuntimeLibrary.PrintFitName);
                        emitter.LdAImm(Math.Min(print.Text.Length, 255));
                        emitter.CallLabel(RuntimeLibrary.PrintFitLabel);
                    }
                    EmitText(print.NewLine ? print.Text + "\r\n" : print.Text);
                    break;

                case PrintValueStatement { NoLeadingSpace: true } write:
                    EmitString(write.Value);                        // WRITE 12 : STR$ sans l'espace du début (D073)
                    emitter.CallLabel(RuntimeLibrary.StrPrintNoSpaceLabel);
                    if (write.NewLine)
                        EmitText("\r\n");
                    break;

                case PrintUsingNumberStatement usingNumber:         // PRINT USING "###.##"; X (D074)
                    EmitRealInto(usingNumber.Value, 0);
                    required.Add(RuntimeLibrary.PrintUsingName);
                    emitter.LdHlLabel(RealTemp(0));
                    emitter.LdBcImm((usingNumber.Width << 8) | usingNumber.Before);
                    emitter.LdDeImm((usingNumber.Decimals << 8) | usingNumber.Flags);
                    emitter.CallLabel(RuntimeLibrary.UsingNumLabel);
                    break;

                case PrintUsingDynamicStatement usingDyn:           // PRINT USING F$; valeurs (D098)
                    required.Add(RuntimeLibrary.UsingDynName);
                    EmitString(usingDyn.Format);
                    emitter.CallLabel(RuntimeLibrary.UsingDynStartLabel);
                    foreach (Expression value in usingDyn.Values)
                    {
                        if (Types.IsString(value))
                        {
                            required.Add(RuntimeLibrary.UsingDynStrName);
                            EmitString(value);
                            emitter.CallLabel(RuntimeLibrary.UsingDynStrLabel);
                        }
                        else
                        {
                            required.Add(RuntimeLibrary.UsingDynNumName);
                            EmitRealInto(value, 0);
                            emitter.LdHlLabel(RealTemp(0));
                            emitter.CallLabel(RuntimeLibrary.UsingDynNumLabel);
                        }
                    }
                    emitter.CallLabel(RuntimeLibrary.UsingDynTailLabel);
                    break;

                case PrintUsingStringStatement usingText:           // PRINT USING "!"; A$ (D074)
                    EmitString(usingText.Value);
                    required.Add(RuntimeLibrary.PrintUsingStrName);
                    emitter.LdAImm(usingText.Width);
                    emitter.CallLabel(RuntimeLibrary.UsingStrLabel);
                    break;

                case PrintZoneStatement:                            // « , » dans PRINT (D073)
                    required.Add(RuntimeLibrary.PrintFormatName);
                    emitter.CallLabel(RuntimeLibrary.ZoneLabel);
                    break;

                case PrintTabStatement tab:                         // TAB(n), SPC(n) dans PRINT (D073)
                    EmitExpression(tab.Value);
                    required.Add(RuntimeLibrary.PrintFormatName);
                    emitter.CallLabel(tab.Spc ? RuntimeLibrary.SpcLabel : RuntimeLibrary.TabLabel);
                    break;

                case PrintValueStatement printValue when Types.IsString(printValue.Value):
                    EmitString(printValue.Value);                   // PRINT A$ (D062)
                    required.Add(RuntimeLibrary.PrintFitName);
                    emitter.CallLabel(RuntimeLibrary.PrintFitStrLabel);   // à la ligne s'il ne tient pas (D073)
                    emitter.CallLabel(RuntimeLibrary.StrPrintLabel);
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case PrintValueStatement printValue when Types.IsReal(printValue.Value):
                    EmitRealInto(printValue.Value, 0);              // PRINT 1/3 (D068)
                    emitter.LdHlLabel(RealTemp(0));
                    emitter.CallLabel(RealRuntime.PrintFitLabel);   // à la ligne s'il ne tient pas (D073)
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case PrintValueStatement printValue:
                    EmitExpression(printValue.Value);
                    required.Add(RuntimeLibrary.PrintIntName);
                    required.Add(RuntimeLibrary.PrintFitName);
                    emitter.CallLabel(RuntimeLibrary.PrintFitIntLabel);   // à la ligne s'il ne tient pas (D073)
                    emitter.CallLabel(RuntimeLibrary.PrintIntLabel);
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case AssignStatement assign when VariableNames.IsString(assign.Variable):
                    EmitString(assign.Value);                       // A$ = texte (D062)
                    emitter.LdDeLabel(Slot(assign.Variable).Label);
                    emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                    break;

                case AssignStatement assign when VariableNames.IsReal(assign.Variable):
                    EmitRealInto(assign.Value, 0);                  // A! = valeur (D068)
                    EmitCopyReal(RealTemp(0), Slot(assign.Variable).Label);
                    break;

                case AssignStatement assign:
                    EmitExpression(assign.Value);
                    EmitStore(Slot(assign.Variable));
                    break;

                case ArrayAssignStatement arrayAssign when VariableNames.IsReal(arrayAssign.Name):
                    EmitRealInto(arrayAssign.Value, 0);             // T!(I) = valeur (D070)
                    realFree = 1;
                    EmitElementAddress(arrayAssign.Name, arrayAssign.Indexes);
                    realFree = 0;
                    emitter.ExDeHl();
                    emitter.LdHlLabel(RealTemp(0));
                    emitter.LdBcImm(5);
                    emitter.Ldir();
                    break;

                case ArrayAssignStatement arrayAssign when VariableNames.IsString(arrayAssign.Name):
                    EmitString(arrayAssign.Value);                  // N$(I) = texte (D065)
                    emitter.PushHl();
                    EmitElementAddress(arrayAssign.Name, arrayAssign.Indexes);
                    emitter.ExDeHl();
                    emitter.PopHl();
                    emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                    break;

                case ArrayAssignStatement arrayAssign:
                    if (arrayAssign.Value is NumberExpression constant)
                    {
                        EmitElementAddress(arrayAssign.Name, arrayAssign.Indexes);
                        emitter.StoreHlImm(constant.Value & 0xFF);
                        emitter.IncHl();
                        emitter.StoreHlImm((constant.Value >> 8) & 0xFF);
                    }
                    else
                    {
                        EmitExpression(arrayAssign.Value);
                        emitter.PushHl();
                        EmitElementAddress(arrayAssign.Name, arrayAssign.Indexes);
                        emitter.PopDe();
                        emitter.StoreHlE();
                        emitter.IncHl();
                        emitter.StoreHlD();
                    }
                    break;

                case DataStatement:
                    break;                                  // les nombres sont rangés dans la table data_table

                case ReadStatement read:
                    foreach (Expression target in read.Targets)
                    {
                        if (Types.IsString(target))
                        {
                            required.Add(RuntimeLibrary.DataStringName);   // READ A$ (D064)
                            emitter.CallLabel(RuntimeLibrary.ReadStringLabel);
                            if (target is ArrayElementExpression textCell)
                            {
                                emitter.PushHl();                   // READ N$(I) (D065)
                                EmitElementAddress(textCell.Name, textCell.Indexes);
                                emitter.ExDeHl();
                                emitter.PopHl();
                            }
                            else
                            {
                                emitter.LdDeLabel(Slot(((VariableExpression)target).Name).Label);
                            }
                            emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                            continue;
                        }
                        if (Types.IsReal(target))
                        {
                            // READ A! ou READ T!(I) (D070) : DE = où ranger le nombre
                            if (target is ArrayElementExpression realCell)
                            {
                                EmitElementAddress(realCell.Name, realCell.Indexes);
                                emitter.ExDeHl();
                            }
                            else
                            {
                                emitter.LdDeLabel(Slot(((VariableExpression)target).Name).Label);
                            }
                            emitter.CallLabel(RealRuntime.ReadRealLabel);
                            continue;
                        }
                        emitter.CallLabel(RuntimeLibrary.ReadLabel);   // HL = nombre suivant
                        if (target is ArrayElementExpression cell)
                        {
                            emitter.PushHl();
                            EmitElementAddress(cell.Name, cell.Indexes);
                            emitter.PopDe();
                            emitter.StoreHlE();
                            emitter.IncHl();
                            emitter.StoreHlD();
                        }
                        else
                        {
                            EmitStore(Slot(((VariableExpression)target).Name));
                        }
                    }
                    break;

                case RestoreStatement restore:
                    emitter.LdHlLabel(RestoreLabel(restore.Line));
                    emitter.LdMemHl(RuntimeLibrary.DataPointerLabel);
                    break;

                case DimStatement dim:
                    foreach (ArrayDeclaration array in dim.Arrays)
                    {
                        string label = arrays[array.Name].Label;
                        for (int k = 0; k < array.Sizes.Count; k++)
                        {
                            EmitExpression(array.Sizes[k]);
                            emitter.CallLabel(RuntimeLibrary.ArrayDimSizeLabel);   // HL = nombre de cases
                            emitter.LdMemHl(SizeLabel(label, k + 1));
                            if (k > 0)
                            {
                                emitter.PopDe();
                                emitter.CallLabel(RuntimeLibrary.ArrayMulLabel);   // HL = total jusqu'ici
                            }
                            emitter.PushHl();
                        }
                        emitter.PopHl();
                        EmitElementSize(array.Name);
                        emitter.LdDeLabel(label);
                        emitter.CallLabel(RuntimeLibrary.ArrayDimLabel);
                    }
                    break;

                case DeclareStatement declare:
                    foreach (string declaredName in declare.Names)
                        declared[declaredName] = VarType.Byte;   // aucun code : seul le type est retenu
                    break;

                case ForStatement forStatement when VariableNames.IsReal(forStatement.Variable):
                {
                    EmitRealInto(forStatement.Start, 0);            // FOR X! = départ (D068)
                    EmitCopyReal(RealTemp(0), Slot(forStatement.Variable).Label);

                    // Limite et pas calculés : gardés dans des variables cachées, avec le signe du pas (D078)
                    int id = loopCount++;
                    string? limitSlot = null, stepSlot = null, dirSlot = null;
                    if (forStatement.LimitValue is not null)
                    {
                        limitSlot = Slot($"@L{id}!").Label;
                        EmitRealInto(forStatement.LimitValue, 0);
                        EmitCopyReal(RealTemp(0), limitSlot);
                    }
                    if (forStatement.StepValue is not null)
                    {
                        stepSlot = Slot($"@S{id}!").Label;
                        dirSlot = Slot($"@D{id}%").Label;
                        EmitRealInto(forStatement.StepValue, 0);
                        EmitCopyReal(RealTemp(0), stepSlot);
                        emitter.LdHlLabel(stepSlot);
                        emitter.CallLabel(FirmwareMaths.Sign.Label);
                        emitter.LdMemA(dirSlot);
                    }
                    var loop = new LoopInfo(LoopKind.For, forStatement.Variable, forStatement.Step, 0,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit",
                                            RealLimit: forStatement.RealLimit, RealStep: forStatement.RealStep,
                                            LimitSlot: limitSlot, StepSlot: stepSlot, DirSlot: dirSlot);

                    // Test du début : une boucle déjà finie ne tourne pas du tout (FOR X! = 5 TO 1)
                    double? constantStart = forStatement.Start switch
                    {
                        RealNumberExpression real => real.Value,
                        NumberExpression number => number.Value,
                        _ => null
                    };
                    if (constantStart is double first && limitSlot is null && stepSlot is null)
                    {
                        if (forStatement.RealStep > 0 ? first > forStatement.RealLimit : first < forStatement.RealLimit)
                            emitter.JpLabel(loop.ExitLabel);
                    }
                    else
                    {
                        EmitRealForCompare(loop);
                        emitter.JpZLabel(loop.ExitLabel);
                    }
                    emitter.Label(loop.BodyLabel);
                    loops.Push(loop);
                    break;
                }

                case ForStatement forStatement:
                {
                    EmitExpression(forStatement.Start);
                    EmitStore(Slot(forStatement.Variable));

                    // Limite et pas calculés : gardés dans des variables cachées (D078)
                    int id = loopCount++;
                    string? limitSlot = null, stepSlot = null;
                    if (forStatement.LimitValue is not null)
                    {
                        limitSlot = Slot($"@L{id}%").Label;
                        EmitExpression(forStatement.LimitValue);
                        emitter.LdMemHl(limitSlot);
                    }
                    if (forStatement.StepValue is not null)
                    {
                        stepSlot = Slot($"@S{id}%").Label;
                        EmitExpression(forStatement.StepValue);
                        emitter.LdMemHl(stepSlot);
                    }
                    var loop = new LoopInfo(LoopKind.For, forStatement.Variable, forStatement.Step, forStatement.Limit,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit",
                                            LimitSlot: limitSlot, StepSlot: stepSlot);

                    // Test du début : une boucle déjà finie ne tourne pas du tout (FOR I = 5 TO 1, D078). Tout écrit en
                    // nombres : décidé ici, sans code s'il n'y a rien à sauter.
                    if (forStatement.Start is NumberExpression first && limitSlot is null && stepSlot is null)
                    {
                        if (forStatement.Step > 0 ? first.Value > forStatement.Limit : first.Value < forStatement.Limit)
                            emitter.JpLabel(loop.ExitLabel);
                    }
                    else
                    {
                        EmitLoad(Slot(forStatement.Variable));
                        EmitForCompare(loop);
                        emitter.JpCLabel(loop.ExitLabel);
                    }
                    emitter.Label(loop.BodyLabel);
                    loops.Push(loop);
                    break;
                }

                case WhileStatement whileStatement:
                {
                    int id = loopCount++;
                    var loop = new LoopInfo(LoopKind.While, "", 0, 0,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit");
                    emitter.Label(loop.NextLabel);                                // le test est en haut
                    Jump(whileStatement.Condition, false, loop.ExitLabel);       // faux : on sort
                    loops.Push(loop);
                    break;
                }

                case WendStatement:
                {
                    LoopInfo loop = loops.Pop();
                    emitter.JpLabel(loop.NextLabel);
                    emitter.Label(loop.ExitLabel);
                    break;
                }

                case RepeatStatement:
                {
                    int id = loopCount++;
                    var loop = new LoopInfo(LoopKind.Repeat, "", 0, 0,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit");
                    emitter.Label(loop.BodyLabel);
                    loops.Push(loop);
                    break;
                }

                case UntilStatement untilStatement:
                {
                    LoopInfo loop = loops.Pop();
                    emitter.Label(loop.NextLabel);                                // CONTINUE arrive ici
                    Jump(untilStatement.Condition, false, loop.BodyLabel);       // faux : on recommence
                    emitter.Label(loop.ExitLabel);
                    break;
                }

                case DoStatement doStatement:
                {
                    int id = loopCount++;
                    bool hasTop = doStatement.Condition is not null;
                    var loop = new LoopInfo(LoopKind.Do, "", 0, 0,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit", hasTop);
                    emitter.Label(loop.BodyLabel);
                    if (doStatement.Condition is not null)
                    {
                        emitter.AliasLabel(loop.NextLabel, loop.BodyLabel);      // CONTINUE revient au test du haut
                        // DO WHILE : on sort si faux ; DO UNTIL : on sort si vrai
                        Jump(doStatement.Condition, doStatement.UntilForm, loop.ExitLabel);
                    }
                    loops.Push(loop);
                    break;
                }

                case LoopStatement loopStatement:
                {
                    LoopInfo loop = loops.Pop();
                    if (loopStatement.Condition is null)
                    {
                        if (!loop.HasTopCondition)
                            emitter.AliasLabel(loop.NextLabel, loop.BodyLabel);
                        emitter.JpLabel(loop.BodyLabel);
                    }
                    else
                    {
                        emitter.Label(loop.NextLabel);                            // CONTINUE arrive ici
                        // LOOP WHILE : on recommence si vrai ; LOOP UNTIL : on recommence si faux
                        Jump(loopStatement.Condition, !loopStatement.UntilForm, loop.BodyLabel);
                    }
                    emitter.Label(loop.ExitLabel);
                    break;
                }

                case NextStatement:
                    EmitNext(loops.Pop());
                    break;

                case ExitStatement exit:
                    emitter.JpLabel(loops.First(l => exit.Kind is null || l.Kind == exit.Kind).ExitLabel);
                    break;

                case ContinueStatement skip:
                    emitter.JpLabel(loops.First(l => skip.Kind is null || l.Kind == skip.Kind).NextLabel);
                    break;

                case IfStatement ifStatement:
                {
                    int id = ifCount++;
                    var info = new IfInfo($"if_{id}_else", $"if_{id}_end");
                    Jump(ifStatement.Condition, false, info.ElseLabel);   // condition fausse : on saute au ELSE
                    ifs.Push(info);
                    break;
                }

                case ElseStatement:
                {
                    IfInfo info = ifs.Peek();
                    emitter.JpLabel(info.EndLabel);                        // fin de la partie « alors »
                    emitter.Label(info.ElseLabel);
                    info.HasElse = true;
                    break;
                }

                case SelectStatement selectStatement:   // D206 : la valeur gardée dans une variable cachée (§1, §1!, §1$...)
                {
                    int id = selectCount++;
                    string name = $"§{selects.Count + 1}" + (Types.IsString(selectStatement.Value) ? "$" : Types.IsReal(selectStatement.Value) ? "!" : "");
                    if (Types.IsString(selectStatement.Value))
                    {
                        EmitString(selectStatement.Value);
                        emitter.LdDeLabel(Slot(name).Label);
                        emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                    }
                    else if (Types.IsReal(selectStatement.Value))
                    {
                        EmitRealInto(selectStatement.Value, 0);
                        EmitCopyReal(RealTemp(0), Slot(name).Label);
                    }
                    else
                    {
                        EmitExpression(selectStatement.Value);
                        EmitStore(Slot(name));
                    }
                    selects.Push(new SelectInfo(name, $"sel_{id}_end"));
                    break;
                }

                case CaseStatement caseStatement:  // D206 : cas d'avant fini -> après END SELECT ; sinon, cas suivant
                {
                    SelectInfo info = selects.Peek();
                    if (info.NextLabel is not null)
                    {
                        emitter.JpLabel(info.EndLabel);
                        emitter.Label(info.NextLabel);
                    }
                    var value = new VariableExpression(info.Variable);
                    Condition? condition = null;
                    foreach (CaseItem item in caseStatement.Items)
                    {
                        Condition one = item.Upper is not null
                            ? new AndCondition(new ComparisonCondition(CompareOperator.GreaterOrEqual, value, item.Value),
                                               new ComparisonCondition(CompareOperator.LessOrEqual, value, item.Upper))
                            : new ComparisonCondition(item.Operator ?? CompareOperator.Equal, value, item.Value);
                        condition = condition is null ? one : new OrCondition(condition, one);
                    }
                    info.NextLabel = $"{info.EndLabel[..^4]}_{info.Cases++}";
                    Jump(condition!, false, info.NextLabel);           // ne convient pas : cas suivant
                    break;
                }

                case CaseElseStatement:
                {
                    SelectInfo info = selects.Peek();
                    if (info.NextLabel is not null)
                    {
                        emitter.JpLabel(info.EndLabel);
                        emitter.Label(info.NextLabel);
                    }
                    info.NextLabel = null;
                    break;
                }

                case EndSelectStatement:
                {
                    SelectInfo info = selects.Pop();
                    if (info.NextLabel is not null)
                        emitter.Label(info.NextLabel);
                    emitter.Label(info.EndLabel);
                    break;
                }

                case EndIfStatement:
                {
                    IfInfo info = ifs.Pop();
                    if (!info.HasElse)
                        emitter.Label(info.ElseLabel);
                    emitter.Label(info.EndLabel);
                    break;
                }

                case RandomizeStatement randomize:
                    if (randomize.Seed is null)
                    {
                        emitter.CallLabel(RuntimeLibrary.RandomizeClockLabel);   // départ pris dans l'horloge
                    }
                    else
                    {
                        EmitExpression(randomize.Seed);
                        emitter.CallLabel(RuntimeLibrary.RandomizeLabel);
                    }
                    required.Add(RuntimeLibrary.RandomizeName);
                    break;

                case EnvelopeStatement envelope:
                {
                    // ENV / ENT (D071) : bloc de la ROM = nombre de parties (ENT qui se répète : + &80), puis 3 octets
                    // par partie. Partie « = » : ENV &80 + forme, période (2 octets) ; ENT &F0 + haut de la période,
                    // bas de la période, pause.
                    string block = $"env_{envelopeBlocks++}";
                    for (int i = 0; i < envelope.Sections.Count; i++)
                    {
                        EnvelopeSection section = envelope.Sections[i];
                        string at = $"{block}_{1 + 3 * i}";
                        if (!section.Absolute)
                        {
                            EmitExpression(section.A);
                            emitter.LdAL();
                            emitter.LdMemA(at);
                            EmitExpression(section.B);
                            emitter.LdAL();
                            emitter.LdMemA($"{block}_{2 + 3 * i}");
                            EmitExpression(section.C!);
                            emitter.LdAL();
                            emitter.LdMemA($"{block}_{3 + 3 * i}");
                        }
                        else if (!envelope.Tone)
                        {
                            EmitExpression(section.A);              // =forme, période
                            emitter.LdAL();
                            emitter.OrImm(0x80);
                            emitter.LdMemA(at);
                            EmitExpression(section.B);
                            emitter.LdAL();
                            emitter.LdMemA($"{block}_{2 + 3 * i}");
                            emitter.LdAH();
                            emitter.LdMemA($"{block}_{3 + 3 * i}");
                        }
                        else
                        {
                            EmitExpression(section.A);              // =période, pause
                            emitter.LdAH();
                            emitter.AndImm(0x0F);
                            emitter.OrImm(0xF0);
                            emitter.LdMemA(at);
                            emitter.LdAL();
                            emitter.LdMemA($"{block}_{2 + 3 * i}");
                            EmitExpression(section.B);
                            emitter.LdAL();
                            emitter.LdMemA($"{block}_{3 + 3 * i}");
                        }
                    }
                    EmitExpression(envelope.Number);
                    string positive = $"env_{labelCount++}";
                    emitter.LdAH();
                    emitter.AndImm(0x80);
                    emitter.JrZLabel(positive);
                    emitter.LdAL();                                 // ENT négatif : se répète
                    emitter.Cpl();
                    emitter.IncA();
                    emitter.LdLA();
                    emitter.LdAImm(0x80);
                    emitter.Label(positive);
                    emitter.OrImm(envelope.Sections.Count);
                    emitter.LdMemA($"{block}_0");
                    emitter.LdAL();
                    emitter.LdHlLabel($"{block}_0");
                    emitter.CallAddress(envelope.Tone ? SoundToneEnvelope : SoundAmplEnvelope);
                    break;
                }

                case ScreenStatement screen:
                    EmitScreen(screen);
                    break;

                case SpriteDefStatement { Continuation: true }:     // D198 : déjà dans le DEFSPR d'avant
                    break;

                case SpriteDefStatement spriteDef:                  // D198 : l'image (les trois MODE) et la place du décor
                {
                    required.Add(RuntimeLibrary.SpriteName);
                    string data = $"spr_{labelCount++}";
                    string background = $"spr_{labelCount++}";
                    string shifted = $"spr_{labelCount++}";
                    string skip = $"spr_{labelCount++}";
                    (byte[] blocks, int backgroundSize, int cacheSize) = SpriteRuntime.Build(spriteDef.Rows);
                    emitter.JpLabel(skip);
                    emitter.Label(data);
                    emitter.Bytes((byte)spriteDef.Rows[0].Length, (byte)spriteDef.Rows.Count);
                    emitter.Word(background);
                    emitter.Word(shifted);                          // D199 : images décalées, MODE (&FF : aucun), prêtes
                    emitter.Bytes(0xFF, 0);
                    emitter.Bytes(blocks);
                    emitter.Label(background);
                    emitter.Reserve(backgroundSize);
                    emitter.Label(shifted);
                    emitter.Reserve(cacheSize);
                    emitter.Label(skip);
                    emitter.LdHlLabel(data);
                    emitter.LdAImm(spriteDef.Number);
                    emitter.CallLabel(SpriteRuntime.DefLabel);
                    break;
                }

                case StreamStatement stream:            // PRINT #1, ... : choix de la fenêtre, puis retour à #0 (D072)
                    if (stream.Stream is NumberExpression { Value: 8 })
                    {
                        // #8 : l'imprimante (D100), jusqu'à la fin de l'instruction
                        printerGroup = true;
                        required.Add(RuntimeLibrary.PrinterName);
                        emitter.CallLabel(RuntimeLibrary.PrinterOnLabel);
                        break;
                    }
                    if (stream.Stream is null && printerGroup)
                    {
                        emitter.CallLabel(RuntimeLibrary.PrinterOffLabel);
                        printerGroup = false;
                        break;
                    }
                    if (stream.Stream is NumberExpression { Value: 9 })
                    {
                        // #9 : le fichier (D084). PRINT #9 et WRITE #9 : les caractères partent dans le fichier ouvert par
                        // OPENOUT jusqu'à la fin de l'instruction ; INPUT #9 et LINE INPUT #9 lisent celui d'OPENIN.
                        fileGroup = true;
                        Statement? inside = statementIndex < program.Statements.Count ? program.Statements[statementIndex] : null;
                        fileOutput = inside is not (InputStatement or LineInputStatement);
                        if (fileOutput)
                        {
                            required.Add(RuntimeLibrary.FileOutName);
                            emitter.CallLabel(RuntimeLibrary.FileOutOnLabel);
                        }
                        break;
                    }
                    if (stream.Stream is null && fileGroup)
                    {
                        if (fileOutput)
                            emitter.CallLabel(RuntimeLibrary.FileOutOffLabel);
                        fileGroup = false;
                        fileOutput = false;
                        break;
                    }
                    EmitSelectStream(stream.Stream);
                    break;

                case FileStatement { Command: "CAT" }:  // CAT (D085)
                    required.Add(RuntimeLibrary.FileBinaryName);
                    emitter.CallLabel("rt_fb_cat");
                    break;

                case SaveStatement save:                // SAVE nom,B,début,longueur[,lancement] (D085)
                    required.Add(RuntimeLibrary.FileBinaryName);
                    EmitExpression(save.Start);
                    emitter.LdMemHl("rt_fb_a");
                    EmitExpression(save.Length);
                    emitter.LdMemHl("rt_fb_l");
                    if (save.Entry is not null)
                        EmitExpression(save.Entry);
                    else
                        emitter.LdHlImm(0);
                    emitter.LdMemHl("rt_fb_e");
                    EmitString(save.Name);
                    emitter.CallLabel("rt_fb_save");
                    break;

                case LoadStatement load:                // LOAD nom[,adresse] (D085)
                    required.Add(RuntimeLibrary.FileBinaryName);
                    if (load.Address is not null)
                    {
                        EmitExpression(load.Address);
                        emitter.LdMemHl("rt_fb_a");
                    }
                    emitter.LdAImm(load.Address is null ? 0 : 1);
                    emitter.LdMemA("rt_fb_has");
                    // Comme le vrai BASIC : on ne charge qu'au-dessus de la mémoire du programme (MEMORY), sinon « Memory full »
                    if (usesHeap)
                        emitter.LdHlMem(RuntimeLibrary.ArrayHeapLimitLabel);
                    else
                        emitter.LdHlImm(memoryLimit < 0 ? RuntimeLibrary.DefaultHeapLimit : (memoryLimit + 1) & 0xFFFF);
                    emitter.LdMemHl("rt_fb_lim");
                    EmitString(load.Name);
                    emitter.CallLabel("rt_fb_load");
                    break;

                case ExternalCallStatement external:    // |NOM, valeurs ou CALL adresse, valeurs (D089)
                {
                    // Les valeurs sont posées sur la pile dans l'ordre : IX = pile = la dernière valeur, comme le BASIC
                    foreach (Expression argument in external.Arguments)
                    {
                        if (Types.IsString(argument))
                            EmitString(argument);                      // un texte : l'adresse de sa fiche (comme @a$)
                        else
                            EmitExpression(argument);
                        emitter.PushHl();
                    }
                    string extTarget = $"ext_{labelCount++}";
                    string extAfter = $"ext_{labelCount++}";
                    if (external.Rsx is not null)
                    {
                        // KL FIND COMMAND : le nom, sa dernière lettre avec le bit 7 ; pas trouvée : « Unknown command »
                        string rsxLabel = $"rsx_{labelCount++}";
                        required.Add(RuntimeLibrary.RsxName);
                        emitter.LdHlLabel(rsxLabel);
                        emitter.CallAddress(0xBCD4);
                        emitter.JpNcLabel("rt_rsx_unknown");
                        if (external.Arguments.Count > 0)
                        {
                            emitter.PopDe();                            // DE = la dernière valeur, comme le BASIC
                            emitter.PushDe();                           // (|GPRINT, |GPEN... la prennent là)
                        }
                        emitter.LdAImm(external.Arguments.Count);
                        emitter.LdIxImm(0);
                        emitter.AddIxSp();
                        emitter.CallAddress(0x001B);                    // KL FAR PCHL : HL = adresse, C = ROM
                        emitter.JpLabel(extAfter);
                        emitter.Label(rsxLabel);
                        byte[] rsxLetters = System.Text.Encoding.ASCII.GetBytes(external.Rsx);
                        rsxLetters[^1] |= 0x80;
                        emitter.Data(rsxLetters);
                    }
                    else
                    {
                        EmitAddress(external.Address!);
                        if (external.Arguments.Count > 0)
                        {
                            emitter.PopDe();                            // DE = la dernière valeur, comme le BASIC
                            emitter.PushDe();
                        }
                        emitter.LdAImm(external.Arguments.Count);
                        emitter.LdIxImm(0);
                        emitter.AddIxSp();
                        emitter.CallLabel(extTarget);
                        emitter.JpLabel(extAfter);
                        emitter.Label(extTarget);
                        emitter.JpHl();
                    }
                    emitter.Label(extAfter);
                    for (int k = 0; k < external.Arguments.Count; k++)
                        emitter.PopDe();                               // les valeurs quittent la pile
                    break;
                }

                case KeyStatement keyText:              // KEY n, texte (D092) : KM SET EXPAND
                    required.Add(RuntimeLibrary.KeysName);
                    EmitExpression(keyText.Number);
                    emitter.PushHl();
                    EmitString(keyText.Text);                           // HL = fiche du texte
                    emitter.PopDe();
                    emitter.CallLabel("rt_key_set");
                    break;

                case KeyDefStatement keyDef:            // KEY DEF touche, répétition, normal, shift, ctrl (D092)
                {
                    required.Add(RuntimeLibrary.KeysName);
                    EmitExpression(keyDef.Values[0]);
                    emitter.CallLabel("rt_key_which");                  // touche de 0 à 79, gardée
                    int[] routines = [0xBB39, 0xBB27, 0xBB2D, 0xBB33];   // KM SET REPEAT, TRANSLATE, SHIFT, CONTROL
                    for (int k = 1; k < keyDef.Values.Count; k++)
                    {
                        EmitExpression(keyDef.Values[k]);
                        emitter.CallLabel(k == 1 ? "rt_key_repeat" : "rt_key_value");   // B = valeur, A = touche
                        emitter.CallAddress(routines[k - 1]);
                    }
                    break;
                }

                case SpeedStatement speedChange:        // SPEED INK a,b ; SPEED KEY a,b (D091) : H = a, L = b
                    EmitExpression(speedChange.First);
                    emitter.PushHl();
                    EmitExpression(speedChange.Second);
                    emitter.PopDe();
                    emitter.LdHE();
                    emitter.CallAddress(speedChange.Kind == "INK" ? 0xBC3E : 0xBB3F);   // SCR SET FLASHING / KM SET DELAY
                    break;

                case ClearStatement:                    // CLEAR (D090)
                    emitter.CallLabel("prog_clear");
                    break;

                case RestartStatement restart:          // RUN, RUN ligne (D090)
                    if (usesEvents)
                        emitter.CallLabel(RuntimeLibrary.EventsStopLabel);   // le compteur sera rebranché au départ
                    if (program.Statements.Any(st => st is FileStatement or SaveStatement or LoadStatement or RunFileStatement))
                    {
                        required.Add(RuntimeLibrary.FileCommonName);
                        emitter.CallLabel("rt_file_closeall");
                    }
                    if (usesOnError)
                    {
                        emitter.LdHlImm(0);                             // plus de ON ERROR GOTO
                        emitter.LdMemHl(RuntimeLibrary.ErrorTrapLabel);
                        emitter.XorA();
                        emitter.LdMemA("rt_oe_active");
                    }
                    if (program.Statements.Any(st => st is ReturnStatement))
                    {
                        required.Add(RuntimeLibrary.GosubName);         // plus de GOSUB en cours
                        emitter.XorA();
                        emitter.LdMemA(RuntimeLibrary.GosubCountLabel);
                    }
                    if (usesData)
                    {
                        emitter.LdHlLabel(RuntimeLibrary.DataTableLabel);   // DATA au début
                        emitter.LdMemHl(RuntimeLibrary.DataPointerLabel);
                    }
                    emitter.CallLabel("prog_clear");
                    if (restart.Target is int runTarget)
                        emitter.LdHlLabel(LineLabel(runTarget));
                    else
                        emitter.LdHlImm(0);
                    if (program.Statements.Any(st => st is RestartStatement { Target: not null }))
                        emitter.LdMemHl("rt_run_go");
                    emitter.LdSpMem("rt_run_sp");
                    emitter.JpLabel("prog_restart");
                    break;

                case TraceStatement trace:              // TRON / TROFF (D088)
                    required.Add(RuntimeLibrary.TraceName);
                    emitter.LdAImm(trace.On ? 1 : 0);
                    emitter.LdMemA("rt_tr_on");
                    break;

                case RunFileStatement runFile:          // RUN "nom" : autre programme compilé (D086)
                    if (usesMask)
                        emitter.CallLabel(RuntimeLibrary.MaskRemoveLabel);   // SCR WRITE ne doit plus venir ici (D110)
                    if (usesEvents)
                        emitter.CallLabel(RuntimeLibrary.EventsStopLabel);   // le compteur du firmware ne doit plus venir ici
                    EmitString(runFile.Name);
                    if (options.Cartridge)              // D207 : le programme suivant est lu dans la cartouche
                    {
                        required.Add(RuntimeLibrary.FileRunCartridgeName);
                        emitter.CallLabel("rt_fbc_run");
                    }
                    else
                    {
                        required.Add(RuntimeLibrary.FileBinaryName);
                        emitter.CallLabel("rt_fb_run");
                    }
                    break;

                case SpeedWriteStatement speedWrite:    // SPEED WRITE 0 ou 1 (D085)
                    required.Add(RuntimeLibrary.FileBinaryName);
                    EmitExpression(speedWrite.Value);
                    emitter.CallLabel("rt_fb_speed");
                    break;

                case FileStatement file:                // OPENOUT, OPENIN, CLOSEOUT, CLOSEIN (D084)
                {
                    bool output = file.Command is "OPENOUT" or "CLOSEOUT";
                    required.Add(output ? RuntimeLibrary.FileOutName : RuntimeLibrary.FileInName);
                    if (file.Name is not null)
                        EmitString(file.Name);                          // HL = fiche du nom
                    emitter.CallLabel(file.Command switch
                    {
                        "OPENOUT" => "rt_fo_open",
                        "CLOSEOUT" => "rt_fo_close",
                        "OPENIN" => "rt_fl_open",
                        _ => "rt_fl_close"
                    });
                    break;
                }

                case DrawStatement draw:
                    EmitDraw(draw);
                    break;

                case LibraryCallStatement library:  // commande de bibliothèque (D148)
                    EmitLibraryCall(library.Name, library.Arguments);
                    break;

                case EllipseStatement ellipse:      // D139 : comme CIRCLE (D130), plus le second rayon et l'angle
                {
                    EmitExpression(ellipse.X);
                    emitter.LdMemHl(RuntimeLibrary.EllipseXLabel);
                    EmitExpression(ellipse.Y);
                    emitter.LdMemHl(RuntimeLibrary.EllipseYLabel);
                    foreach ((Expression radius, string label) in new[] { (ellipse.RadiusX, RuntimeLibrary.EllipseRadiusXLabel),
                                                                          (ellipse.RadiusY, RuntimeLibrary.EllipseRadiusYLabel) })
                    {
                        EmitExpression(radius);
                        emitter.LdMemHl(label);
                        if (radius is not NumberExpression)     // rayon calculé hors de 0 à 1000 : « Improper argument »
                        {
                            required.Add(RuntimeLibrary.ByteCheckName);
                            emitter.LdDeImm(1001);
                            emitter.OrA();
                            emitter.SbcHlDe();
                            emitter.JpNcLabel(RuntimeLibrary.ByteCheckLabel);
                        }
                    }
                    if (ellipse.Angle is null)               // angle omis : 0 (ovale droit)
                        emitter.LdHlImm(0);
                    else
                        EmitExpression(ellipse.Angle);
                    emitter.LdMemHl(RuntimeLibrary.EllipseAngleLabel);
                    if (ellipse.Pen is null)                 // stylo du contour omis : stylo courant du tracé
                        emitter.CallAddress(GraGetPen);
                    else
                    {
                        EmitExpression(ellipse.Pen);
                        emitter.LdAL();
                    }
                    emitter.LdMemA(RuntimeLibrary.EllipsePenLabel);
                    if (ellipse.Fill is not null)            // stylo de l'intérieur (pas d'intérieur s'il est omis)
                    {
                        EmitExpression(ellipse.Fill);
                        emitter.LdAL();
                        emitter.LdMemA(RuntimeLibrary.EllipseFillPenLabel);
                    }
                    emitter.LdAImm(ellipse.Fill is null ? 0 : 1);
                    emitter.LdMemA(RuntimeLibrary.EllipseDoFillLabel);
                    required.Add(RuntimeLibrary.EllipseName);
                    emitter.CallLabel(RuntimeLibrary.EllipseLabel);
                    break;
                }

                case EndStatement:
                    emitter.JpLabel(EndLabel);
                    break;

                // DEBUG (D136) : sans le réglage « Mise au point », rien ; sinon titre, routine d'affichage, attente
                case DebugStatement when !options.Debug:
                    break;

                case DebugStatement:
                {
                    string skip = $"dbg_skip_{labelCount++}";
                    required.Add(RuntimeLibrary.DebugName);
                    emitter.LdAMem(RuntimeLibrary.DebugOffLabel);   // S a été tapé : plus aucun DEBUG
                    emitter.OrA();
                    emitter.JrNzLabel(skip);
                    emitter.LdHlImm(program.LineTable.Count > 0 ? basicLine : statement.SourceLine);
                    emitter.CallLabel(RuntimeLibrary.DebugTitleLabel);
                    emitter.CallLabel(((DebugStatement)statement).BodyLabel);
                    emitter.Label(skip);
                    break;
                }

                case DebugBodyStatement body:
                    emitter.Label(body.Label);
                    break;

                case DebugPageStatement:                            // écran plein : ESPACE ou S ; S : retour tout de suite
                    emitter.CallLabel(RuntimeLibrary.DebugPageLabel);
                    emitter.RetNz();
                    break;

                case DebugEndStatement:
                    emitter.CallLabel(RuntimeLibrary.DebugWaitLabel);
                    emitter.Ret();
                    break;

                case DefFnStatement:                    // DEF FN : rien à faire ici, le calcul est recopié à chaque appel (D095)
                    break;

                case EraseStatement erase:              // ERASE T, N$ (D096)
                    required.Add(RuntimeLibrary.ArrayEraseName);
                    foreach (string name in erase.Names)
                    {
                        (string eraseLabel, int eraseDims) = arrays[name];
                        emitter.LdHlLabel(eraseLabel);
                        emitter.LdBImm(eraseDims);
                        emitter.LdCImm(ElementBytes(name));
                        emitter.CallLabel(RuntimeLibrary.ArrayEraseLabel);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Instruction non gérée : {statement.GetType().Name}");
            }
        }

        EmitLineLabels(statementIndex);   // lignes numérotées placées après la dernière instruction

        // Fin de programme : attendre une touche, puis redémarrer la machine
        emitter.Label(EndLabel);
        emitter.CallAddress(RuntimeLibrary.KmWaitKey);
        emitter.JpAddress(0);

        // CLEAR (D090) : variables à 0 (un texte : fiche à 0 = vide), tableaux effacés, mémoire libre comme au départ
        if (usesClear)
        {
            emitter.Label("prog_clear");
            void Zero(string start, int size)
            {
                if (size == 0)
                    return;
                emitter.LdHlLabel(start);
                emitter.StoreHlImm(0);
                if (size == 1)
                    return;
                emitter.LdDH();
                emitter.LdEL();
                emitter.IncDe();
                emitter.LdBcImm(size - 1);
                emitter.Ldir();
            }
            Zero("vars_start", slots.Sum(slot => slot.Type switch { VarType.Byte => 1, VarType.String => 3, VarType.Real => 5, _ => 2 }));
            Zero("arrays_start", arrays.Values.Sum(a => 2 + 2 * a.Dimensions));
            if (usesHeap)
            {
                emitter.LdHlLabel(RuntimeLibrary.ArrayHeapStartLabel);
                emitter.LdMemHl(RuntimeLibrary.ArrayHeapLabel);
                emitter.LdHlMem(RuntimeLibrary.ArrayHeapLimitLabel);
                emitter.LdMemHl(RuntimeLibrary.StringPointerLabel);
            }
            emitter.Ret();
        }
        if (usesRun)
        {
            emitter.Label("rt_run_sp");
            emitter.Reserve(2);
            emitter.Label("rt_run_go");
            emitter.Reserve(2);
        }

        // Liaison sélective : seuls les modules utilisés sont écrits dans le binaire
        // ON SQ (D082) et touche ESC (D083) : le module des minuteries est remplacé par celui qui les gère aussi
        if (required.Contains(RuntimeLibrary.EventsName))
        {
            required.Remove(RuntimeLibrary.EventsName);
            required.Add(RuntimeLibrary.EventsModuleName(program.Statements.Any(st => st is OnSqStatement), usesBreak));
        }
        List<RuntimeModule> modules = Linker.Resolve(required);
        foreach (RuntimeModule module in modules)
            module.Emit(emitter);

        // Commandes des bibliothèques utilisées, avec les routines qu'elles appellent (D148)
        IReadOnlyList<LibraryCommand> libraryCommands = LibraryLinker.Collect(libraryCatalog, libraryUsed).Commands;
        LibraryLinker.Emit(emitter, libraryCatalog, libraryCommands);

        // Textes, terminés par un octet 0
        for (int i = 0; i < strings.Count; i++)
        {
            emitter.Label(StringLabel(i));
            emitter.Data(strings[i]);
        }

        // Textes écrits dans le programme (D062) : fiche (longueur, adresse), puis les lettres
        for (int k = 0; k < literals.Count; k++)
        {
            byte[] letters = Encode(literals[k], options.Language);
            emitter.Label($"lit_{k}");
            emitter.Bytes((byte)(letters.Length - 1));
            emitter.Word($"lit_{k}_c");
            emitter.Label($"lit_{k}_c");
            emitter.Data(letters);
        }

        // Nombres des DATA (D059), avec les points de départ de RESTORE
        if (usesData)
        {
            emitter.Label(RuntimeLibrary.DataTableLabel);
            for (int k = 0; k <= dataValues.Count; k++)
            {
                if (restorePoints.Contains(k))
                    emitter.Label($"data_at_{k}");
                if (k < dataValues.Count)
                {
                    // Nombre : 0, valeur, longueur et lettres de son écriture ; texte : 1, longueur, lettres (D064) ;
                    // nombre à virgule : 2, ses 5 octets, longueur et lettres (D070)
                    DataItem item = dataValues[k];
                    byte[] written = Encode(item.Text, options.Language);
                    emitter.Bytes((byte)(item.IsText ? 1 : item.IsReal ? 2 : 0));
                    if (item.IsReal)
                        emitter.Data(LocoReal.Encode(item.Real)!);
                    else if (!item.IsText)
                        emitter.Word(item.Value & 0xFFFF);
                    emitter.Bytes((byte)(written.Length - 1));
                    emitter.Data(written[..^1]);
                }
            }
            emitter.Label(RuntimeLibrary.DataEndLabel);
        }

        // Son (D071) : bloc de SOUND, et un bloc de 16 octets par ENV ou ENT
        if (usesSound)
        {
            for (int k = 0; k < 9; k++)
            {
                emitter.Label($"snd_b{k}");
                emitter.Reserve(1);
            }
        }
        for (int b = 0; b < envelopeBlocks; b++)
        {
            for (int k = 0; k < 16; k++)
            {
                emitter.Label($"env_{b}_{k}");
                emitter.Reserve(1);
            }
        }

        // Variables, à 0 au départ : 1 octet pour un octet, 2 pour un entier
        emitter.Label("vars_start");
        foreach (VariableSlot slot in slots)
        {
            emitter.Label(slot.Label);
            emitter.Reserve(slot.Type switch { VarType.Byte => 1, VarType.String => 3, VarType.Real => 5, _ => 2 });
        }

        // Nombres à virgule : cases de calcul, puis nombres écrits dans le programme (D068)
        for (int k = 0; k < realTemps; k++)
        {
            emitter.Label($"ftmp_{k}");
            emitter.Reserve(5);
        }
        foreach ((double value, string label) in realLiterals)
        {
            emitter.Label(label);
            emitter.Data(LocoReal.Encode(value)!);
        }

        // Descripteurs des tableaux : adresse des cases (0 = pas encore créé), puis nombre de cases de chaque dimension
        emitter.Label("arrays_start");
        foreach ((string label, int dimensions) in arrays.Values)
        {
            emitter.Label(label);
            emitter.Reserve(2);
            for (int k = 1; k <= dimensions; k++)
            {
                emitter.Label(SizeLabel(label, k));
                emitter.Reserve(2);
            }
        }
        if (usesErase)
        {
            emitter.Label(RuntimeLibrary.ArrayTableLabel);     // ERASE (D096) : tous les tableaux
            foreach ((string name, (string label, int dimensions)) in arrays)
            {
                emitter.Word(label);
                emitter.Data([(byte)dimensions, (byte)ElementBytes(name)]);
            }
            emitter.Word(0);
        }
        // Fiches des instructions pour ON ERROR (D080) : ligne, début, instruction suivante (la fin pour la dernière)
        for (int k = 0; k < errorRecords.Count; k++)
        {
            emitter.Label($"erec_{errorRecords[k].Index}");
            emitter.Word(errorRecords[k].Line);
            emitter.Word($"stmt_{errorRecords[k].Index}");
            emitter.Word(k + 1 < errorRecords.Count ? $"stmt_{errorRecords[k + 1].Index}" : EndLabel);
        }

        // Table des caractères redessinables (SYMBOL AFTER, D075) : (256 - plus petit premier caractère) x 8 octets ;
        // premier caractère calculé : la table complète (2 Ko)
        var symbolAfters = program.Statements.OfType<ScreenStatement>().Where(sa => sa.Command == "SYMBOL AFTER").ToList();
        int symbolChars = symbolAfters
            .Select(sa => sa.Arguments[0] is NumberExpression firstChar ? Math.Max(0, 256 - firstChar.Value) : 256)
            .DefaultIfEmpty(0).Max();
        if (symbolAfters.Count > 0)
        {
            emitter.Label(RuntimeLibrary.SymbolTableLabel);
            emitter.Reserve(symbolChars * 8);
        }

        // La mémoire libre commence ici ; sert aussi à MEMORY avec une adresse calculée (D125)
        if (usesHeap || program.Statements.Any(st => st is ScreenStatement { Command: "MEMORY" } m && m.Arguments[0] is not NumberExpression))
            emitter.Label(RuntimeLibrary.ArrayHeapStartLabel);

        // Adresses des variables et des fiches des tableaux (D132)
        // (une étiquette qui n'aurait pas été placée est simplement ignorée : la compilation ne doit jamais échouer ici)
        var addresses = new Dictionary<string, int>();
        void AddAddress(string key, string label)
        {
            try { addresses[key] = emitter.AddressOf(label); }
            catch (InvalidOperationException) { }
        }
        foreach ((string name, VariableSlot slot) in variables)
            AddAddress(name, slot.Label);
        foreach ((string name, (string label, int _)) in arrays)
            AddAddress(name + "()", label);

        List<string> moduleNames = modules.Select(m => m.Name)
            .Concat(libraryCommands.Select(c => $"{c.Library}:{c.Name}")).ToList();   // « GRAPHIQUE:STAR » (D148)
        return new GeneratedProgram(emitter.ToBytes(), moduleNames, memoryLimit, memoryLine, addresses);
    }

    // Routines du firmware appelées par les commandes d'écran (décision D046)
    // Son (D071)
    const int SoundQueue = 0xBCAA;          // SOUND QUEUE : HL = bloc de 9 octets ; retenue à 0 si la file est pleine
    const int SoundCheck = 0xBCAD;          // SOUND CHECK : A = canal (1, 2 ou 4) -> A = état
    const int SoundRelease = 0xBCB3;        // SOUND RELEASE : A = canaux
    const int SoundAmplEnvelope = 0xBCBC;   // SOUND AMPL ENVELOPE : A = numéro, HL = bloc (ENV)
    const int SoundToneEnvelope = 0xBCBF;   // SOUND TONE ENVELOPE : A = numéro, HL = bloc (ENT)
    const int ScrSetMode = 0xBC0E;      // SCR SET MODE : A = mode
    const int TxtClearWindow = 0xBB6C;  // TXT CLEAR WINDOW : efface la fenêtre de texte
    const int TxtSetCursor = 0xBB75;    // TXT SET CURSOR : H = colonne, L = ligne
    const int TxtSetPen = 0xBB90;       // TXT SET PEN : A = stylo
    const int TxtSetPaper = 0xBB96;     // TXT SET PAPER : A = fond
    // Fenêtres (lot H1, D072)
    const int TxtStrSelect = RuntimeLibrary.TxtStrSelect;   // TXT STR SELECT : A = fenêtre -> A = fenêtre d'avant
    const int TxtSetWindow = 0xBB66;    // TXT WIN ENABLE : H = gauche, D = droite, L = haut, E = bas (à partir de 0)
    const int TxtSwapStreams = 0xBBB7;  // TXT SWAP STREAMS : B et C = les deux fenêtres
    const int TxtValidate = 0xBB87;     // TXT VALIDATE : H = colonne, L = ligne -> position où s'écrira le caractère suivant
    const int ScrSetInk = 0xBC32;       // SCR SET INK : A = stylo, B = couleur, C = seconde couleur
    const int ScrSetBorder = 0xBC38;    // SCR SET BORDER : B = couleur, C = seconde couleur

    // Tracé (décision D047)
    const int GraMoveAbsolute = 0xBBC0; // GRA MOVE ABSOLUTE : DE = x, HL = y
    const int GraMoveRelative = 0xBBC3; // GRA MOVE RELATIVE : DE = écart x, HL = écart y
    const int GraSetOrigin = 0xBBC9;    // GRA SET ORIGIN : DE = x, HL = y
    const int GraGetPen = 0xBBE1;       // GRA GET PEN : renvoie A = stylo du tracé (0 à 15)
    const int GraClearWindow = 0xBBDB;  // GRA CLEAR WINDOW : efface la fenêtre graphique
    const int GraSetPen = 0xBBDE;       // GRA SET PEN : A = stylo
    const int GraSetPaper = 0xBBE4;     // GRA SET PAPER : A = fond
    /// <summary>
    /// D210 : instructions qui ne changent ni l'écran (place, défilement), ni le MODE, ni l'origine, ni la fenêtre graphique :
    /// les réglages gardés par PLOT rapide restent bons (PLOT x,y,stylo garde son stylo lui-même). Toutes les autres les
    /// font relire (SYNC) ; DRAW, DRAWR, XPOS, YPOS, TEST, TESTR reçoivent d'abord le curseur et le stylo gardés (FLUSH) ;
    /// MOVE et MOVER sans stylo ne changent que notre curseur gardé (D212).
    /// </summary>
    static bool SafeForPlot(Statement statement) => statement is AssignStatement or ArrayAssignStatement or DeclareStatement
        or ForStatement or NextStatement or WhileStatement or WendStatement or RepeatStatement or UntilStatement or DoStatement
        or LoopStatement or ExitStatement or ContinueStatement or IfStatement or ElseStatement or EndIfStatement
        or SelectStatement or CaseStatement or CaseElseStatement or EndSelectStatement or GotoStatement or GosubStatement
        or ReturnStatement or OnStatement or DataStatement or ReadStatement or RestoreStatement or RandomizeStatement
        or DefFnStatement or MidAssignStatement or EndStatement
        or DrawStatement { Pen: null, Mode: null, Command: "MOVE" or "MOVER" or "PLOT" or "PLOTR" or "DRAW" or "DRAWR" }
        or DrawStatement { Mode: null, Command: "PLOT" or "PLOTR" };

    const int GraPlotAbsolute = 0xBBEA; // GRA PLOT ABSOLUTE : DE = x, HL = y
    const int GraPlotRelative = 0xBBED; // GRA PLOT RELATIVE : DE = écart x, HL = écart y
    const int GraLineAbsolute = 0xBBF6; // GRA LINE ABSOLUTE : DE = x, HL = y
    const int GraLineRelative = 0xBBF9; // GRA LINE RELATIVE : DE = écart x, HL = écart y
    const int ScrAccess = 0xBC59;       // SCR ACCESS : A = mode d'écriture (0 normal, 1 XOR, 2 AND, 3 OR)

    static string StringLabel(int index) => $"str_{index}";

    /// <summary>Vrai si l'instruction utilise un texte (variable A$, texte entre guillemets dans un calcul...) (D062).</summary>
    // Nombres à virgule (D068) : un nombre écrit avec une virgule, une variable A!, ou une affectation à A!
    // ON x GOTO : la valeur est vérifiée (0 à 255) sauf si c'est un nombre écrit déjà bon (D097)
    static bool OnNeedsCheck(OnStatement on) => on.Selector is not NumberExpression { Value: >= 0 and <= 255 };

    // Taille d'une case de tableau : 2 (entier), 3 (fiche de texte, D065), 5 (nombre à virgule, D070)
    static int ElementBytes(string name) => VariableNames.IsString(name) ? 3 : VariableNames.IsReal(name) ? 5 : 2;

    static bool StatementUsesReals(Statement statement) =>
        AstWalker.All(statement).Any(Types.IsReal)
        || statement is ScreenStatement { Command: "DEG" or "RAD" }
        || statement is PrintUsingNumberStatement                   // PRINT USING : calculs par la ROM (D074)
        || AstWalker.All(statement).Any(x => x is CallExpression { Function: "DEC$" })   // D103
        || (statement is PrintUsingDynamicStatement dyn && dyn.Values.Any(v => !Types.IsString(v)))   // D098
        || (statement is AssignStatement assign && VariableNames.IsReal(assign.Variable))
        || (statement is ForStatement loop && VariableNames.IsReal(loop.Variable))
        || (statement is DataStatement data && data.Items.Any(item => item.IsReal));

    static bool StatementUsesStrings(Statement statement) =>
        AstWalker.All(statement).Any(Types.IsString)
        || (statement is AssignStatement assign && VariableNames.IsString(assign.Variable))
        || (statement is InputStatement input && input.Variables.Any(VariableNames.IsString))
        || statement is LineInputStatement;

    // DEBUG (D136) : avec le réglage « Mise au point », la routine d'affichage est ajoutée à la fin du programme, après
    // une fin de programme (on n'y entre que par CALL) : pour chaque variable, par ordre alphabétique, un arrêt si l'écran
    // est plein, puis « NOM= valeur » avec les PRINT habituels (nombres, nombres à virgule, textes entre guillemets) ;
    // pour un tableau, son nombre de cases (DIM écrit en nombres) ou « tableau ».
    // DEBUG A,B$ (D137) : une routine de plus par liste différente, avec seulement ces variables, dans l'ordre écrit ;
    // DEBUG T(I) (D138) : la case avec son vrai numéro, « T(3)= 25 ».
    static BasicProgram ExpandDebug(BasicProgram program, CompilerOptions options)
    {
        List<DebugStatement> debugs = program.Statements.OfType<DebugStatement>().ToList();
        if (!options.Debug || debugs.Count == 0)
            return program;

        (HashSet<string> names, Dictionary<string, string?> arrays) = DebugVariables.Collect(program.Statements);
        var statements = program.Statements.ToList();
        statements.Add(new EndStatement(0));

        void AddBody(string label, IEnumerable<(string Key, bool Array)> list)
        {
            statements.Add(new DebugBodyStatement(label));
            foreach ((string key, bool array) in list)
            {
                statements.Add(new DebugPageStatement());
                if (array)
                {
                    statements.Add(new PrintStatement(0, $"{key} : {arrays[key[..^2]] ?? "tableau"}  ", false));
                    continue;
                }
                bool text = VariableNames.IsString(key);
                statements.Add(new PrintStatement(0, text ? $"{key}=\"" : $"{key}=", false));
                statements.Add(new PrintValueStatement(0, new VariableExpression(key), false));
                statements.Add(new PrintStatement(0, text ? "\"  " : " ", false));
            }
            statements.Add(new DebugEndStatement());
        }

        if (debugs.Any(d => d.Items is null))
            AddBody("dbg_body", names.Select(n => (Key: n, Array: false))
                                     .Concat(arrays.Keys.Select(n => (Key: n + "()", Array: true)))
                                     .OrderBy(x => x.Key, StringComparer.Ordinal));
        var done = new HashSet<string>();
        foreach (DebugStatement debug in debugs.Where(d => d.Items is not null))
        {
            if (!done.Add(debug.BodyLabel))
                continue;
            statements.Add(new DebugBodyStatement(debug.BodyLabel));
            foreach (Expression item in debug.Items!)
            {
                statements.Add(new DebugPageStatement());
                bool text = Types.IsString(item);
                if (item is ArrayElementExpression element)
                {
                    statements.Add(new PrintStatement(0, element.Name + "(", false));
                    for (int k = 0; k < element.Indexes.Count; k++)
                    {
                        if (k > 0)
                            statements.Add(new PrintStatement(0, ",", false));
                        statements.Add(new PrintValueStatement(0, new CallExpression("STR$", [element.Indexes[k]]), false,
                                                               NoLeadingSpace: true));
                    }
                    statements.Add(new PrintStatement(0, text ? ")=\"" : ")=", false));
                }
                else
                    statements.Add(new PrintStatement(0, $"{((VariableExpression)item).Name}=" + (text ? "\"" : ""), false));
                statements.Add(new PrintValueStatement(0, item, false));
                statements.Add(new PrintStatement(0, text ? "\"  " : " ", false));
            }
            statements.Add(new DebugEndStatement());
        }

        return program with { Statements = statements };
    }

    // Texte en octets, avec 0 à la fin : codes de la version du CPC choisie (D126 ; é : 123 sur le CPC français)
    static byte[] Encode(string text, CpcLanguage language) => [.. CharacterSet.Encode(text, language), 0];
}
