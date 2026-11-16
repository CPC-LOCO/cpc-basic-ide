// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;

namespace CpcBasic.Core.Codegen;

/// <summary>Le binaire produit et la liste des modules de la bibliothèque qui y ont été liés.</summary>
/// <summary>
/// Le programme compilé. MemoryLimit : la plus basse adresse écrite après MEMORY (un nombre écrit dans le programme), ou -1
/// s'il n'y en a pas ; MemoryLine : la ligne de ce MEMORY. Le compilateur vérifie que le programme tient en dessous (D057).
/// </summary>
public sealed record GeneratedProgram(byte[] Code, IReadOnlyList<string> Modules, int MemoryLimit = -1, int MemoryLine = 0);

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
        var emitter = new Z80Emitter(origin);
        var required = new HashSet<string>();
        var strings = new List<byte[]>();
        var variables = new Dictionary<string, VariableSlot>();
        var slots = new List<VariableSlot>();   // dans l'ordre de première utilisation
        var declared = new Dictionary<string, VarType>();   // variables déclarées BYTE
        var loops = new Stack<LoopInfo>();      // boucles ouvertes, la plus récente en premier
        int loopCount = 0;
        var ifs = new Stack<IfInfo>();         // IF ouverts, le plus récent en premier
        int ifCount = 0;
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
            strings.Add(Encode(text));
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

        // Calcule l'expression : le résultat est dans HL
        void EmitExpression(Expression expression)
        {
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
                            EmitExpression(call.Arguments[0]);
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
                            EmitExpression(call.Arguments[0]);
                            emitter.LdBH();
                            emitter.LdCL();
                            emitter.InAC();
                            emitter.LdLA();
                            emitter.LdHZero();
                            break;

                        case "TIME":                        // temps écoulé : 16 bits de poids faible
                            emitter.CallAddress(RuntimeLibrary.KlTimePlease);   // le firmware rend DEHL ; HL est le bas
                            break;

                        case "RANDOM":                      // nombre de min à max compris
                            EmitPair(call.Arguments[0], call.Arguments[1]);
                            required.Add(RuntimeLibrary.RandomRangeName);
                            emitter.CallLabel(RuntimeLibrary.RandomRangeLabel);
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

                case "RELEASE":                     // RELEASE canaux (D071)
                    EmitCallWithA(a[0], SoundRelease);
                    break;

                case "DEG" or "RAD":                // angles de SIN, COS, TAN et ATN (D069)
                    emitter.LdAImm(screen.Command == "DEG" ? 1 : 0);
                    emitter.CallLabel(FirmwareMaths.AngleMode.Label);
                    break;

                case "MEMORY":                      // MEMORY adresse : réserve le haut de la mémoire ; rien à faire à l'exécution,
                {                                   // le compilateur vérifie que le programme tient en dessous
                    if (a[0] is NumberExpression limit && (memoryLimit < 0 || (limit.Value & 0xFFFF) < memoryLimit))
                    {
                        memoryLimit = limit.Value & 0xFFFF;
                        memoryLine = screen.SourceLine;
                    }
                    if (arrays.Count > 0 || usesStrings)
                    {
                        EmitExpression(a[0]);                       // tableaux et textes restent sous cette adresse (D058, D062)
                        emitter.CallLabel(RuntimeLibrary.ArrayMemoryLabel);
                    }
                    break;
                }

                case "POKE":                        // POKE adresse, valeur : seul l'octet de poids faible de la valeur est écrit
                    EmitPair(a[0], a[1]);
                    emitter.LdAE();
                    emitter.LdHlA();
                    break;

                case "OUT":                         // OUT port, valeur : le port est dans BC
                    EmitPair(a[0], a[1]);
                    emitter.LdBH();
                    emitter.LdCL();
                    emitter.LdAE();
                    emitter.OutCA();
                    break;

                case "CALL":                        // CALL adresse : le sous-programme rend la main avec RET
                {
                    string target = $"call_{labelCount++}";
                    string after = $"call_{labelCount++}";
                    EmitExpression(a[0]);
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

            if (draw.Pen is not null)
                EmitCallWithA(draw.Pen, GraSetPen);
            if (draw.Mode is not null)
                EmitCallWithA(draw.Mode, ScrAccess);

            emitter.PopHl();       // y
            emitter.PopDe();       // x
            emitter.CallAddress(address);
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

        // Nombres à virgule (D068) : table des routines de la ROM selon la machine, angles en radians
        if (usesReals)
        {
            required.Add(RuntimeLibrary.MathsModuleFor(options.Target));
            required.Add(RuntimeLibrary.RealOpsName);
            required.Add(RuntimeLibrary.RealPrintName);
            emitter.CallLabel(RuntimeLibrary.MathsInitLabel);
        }

        // Tableaux : la mémoire libre commence juste après le programme (D058)
        if (arrays.Count > 0 || usesStrings)
        {
            if (arrays.Count > 0)
                required.Add(RuntimeLibrary.ArrayName);
            if (usesStrings)
                required.Add(RuntimeLibrary.StringsName);           // textes (D062)
            emitter.LdHlLabel(RuntimeLibrary.ArrayHeapStartLabel);
            emitter.LdMemHl(RuntimeLibrary.ArrayHeapLabel);
        }

        // Minuteries (lot I1, D079) : le compteur du firmware est branché au départ
        bool usesEvents = program.Statements.Any(st => st is TimerStatement);
        bool usesOnError = program.Statements.Any(st => st is OnErrorStatement);   // D080
        var errorRecords = new List<(int Index, int Line)>();
        if (usesEvents)
        {
            required.Add(RuntimeLibrary.EventsName);
            emitter.CallLabel(RuntimeLibrary.EventsInitLabel);
        }

        int statementIndex = 0;
        int basicLine = 0;   // numéro de la ligne BASIC en cours, pour les messages d'erreur des tableaux
        bool insideStream = false;
        foreach (Statement statement in program.Statements)
        {
            if (linesAt.TryGetValue(statementIndex, out List<int>? numbersHere))
                basicLine = numbersHere.Max();
            EmitLineLabels(statementIndex++);

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
                || ((arrays.Count > 0 || usesStrings) && statement is ScreenStatement { Command: "MEMORY" })
                || statement is StreamStatement { Stream: not null and not NumberExpression }
                || statement is ScreenStatement { Command: "ZONE" or "SYMBOL" or "SYMBOL AFTER" }
                || statement is TimerStatement or ErrorStatement or ResumeStatement
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

                case GosubStatement call:
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

                case ReturnStatement:
                    emitter.Ret();
                    break;

                case StopStatement:
                    emitter.JpLabel(EndLabel);
                    break;

                case OnStatement on:
                {
                    // HL = valeur. Pour chaque ligne k (de 1 à N) : si la valeur vaut k, on saute ou on appelle ; sinon on continue.
                    EmitExpression(on.Selector);
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

                case StreamStatement stream:            // PRINT #1, ... : choix de la fenêtre, puis retour à #0 (D072)
                    EmitSelectStream(stream.Stream);
                    break;

                case DrawStatement draw:
                    EmitDraw(draw);
                    break;

                case EndStatement:
                    emitter.JpLabel(EndLabel);
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

        // Liaison sélective : seuls les modules utilisés sont écrits dans le binaire
        List<RuntimeModule> modules = Linker.Resolve(required);
        foreach (RuntimeModule module in modules)
            module.Emit(emitter);

        // Textes, terminés par un octet 0
        for (int i = 0; i < strings.Count; i++)
        {
            emitter.Label(StringLabel(i));
            emitter.Data(strings[i]);
        }

        // Textes écrits dans le programme (D062) : fiche (longueur, adresse), puis les lettres
        for (int k = 0; k < literals.Count; k++)
        {
            byte[] letters = Encode(literals[k]);
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
                    byte[] written = Encode(item.Text);
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

        if (arrays.Count > 0 || usesStrings)
            emitter.Label(RuntimeLibrary.ArrayHeapStartLabel);   // la mémoire libre commence ici

        return new GeneratedProgram(emitter.ToBytes(), modules.Select(m => m.Name).ToList(), memoryLimit, memoryLine);
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
    const int GraPlotAbsolute = 0xBBEA; // GRA PLOT ABSOLUTE : DE = x, HL = y
    const int GraPlotRelative = 0xBBED; // GRA PLOT RELATIVE : DE = écart x, HL = écart y
    const int GraLineAbsolute = 0xBBF6; // GRA LINE ABSOLUTE : DE = x, HL = y
    const int GraLineRelative = 0xBBF9; // GRA LINE RELATIVE : DE = écart x, HL = écart y
    const int ScrAccess = 0xBC59;       // SCR ACCESS : A = mode d'écriture (0 normal, 1 XOR, 2 AND, 3 OR)

    static string StringLabel(int index) => $"str_{index}";

    /// <summary>Vrai si l'instruction utilise un texte (variable A$, texte entre guillemets dans un calcul...) (D062).</summary>
    // Nombres à virgule (D068) : un nombre écrit avec une virgule, une variable A!, ou une affectation à A!
    static bool StatementUsesReals(Statement statement) =>
        AstWalker.All(statement).Any(Types.IsReal)
        || statement is ScreenStatement { Command: "DEG" or "RAD" }
        || statement is PrintUsingNumberStatement                   // PRINT USING : calculs par la ROM (D074)
        || (statement is AssignStatement assign && VariableNames.IsReal(assign.Variable))
        || (statement is ForStatement loop && VariableNames.IsReal(loop.Variable))
        || (statement is DataStatement data && data.Items.Any(item => item.IsReal));

    static bool StatementUsesStrings(Statement statement) =>
        AstWalker.All(statement).Any(Types.IsString)
        || (statement is AssignStatement assign && VariableNames.IsString(assign.Variable))
        || (statement is InputStatement input && input.Variables.Any(VariableNames.IsString))
        || statement is LineInputStatement;

    // Texte en octets, avec 0 à la fin. Les caractères hors ASCII sont remplacés par « ? » pour
    // l'instant : la table des caractères du CPC n'est pas encore gérée.
    static byte[] Encode(string text)
    {
        byte[] bytes = new byte[text.Length + 1];
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool allowed = (c >= ' ' && c <= '~') || c == '\r' || c == '\n';
            bytes[i] = allowed ? (byte)c : (byte)'?';
        }
        return bytes;
    }
}
