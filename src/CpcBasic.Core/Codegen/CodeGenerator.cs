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
    sealed record LoopInfo(LoopKind Kind, string Variable, int Step, int Limit,
                           string BodyLabel, string NextLabel, string ExitLabel,
                           bool HasTopCondition = false);

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

    public static GeneratedProgram Generate(BasicProgram program, int origin)
    {
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
                    : VariableNames.IsString(name) ? VarType.String : VarType.Integer;
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

        // HL = adresse de la case A(i [, j ...])
        void EmitElementAddress(string name, IReadOnlyList<Expression> indexes)
        {
            (string label, int dimensions) = arrays[name];
            if (dimensions == 1)
            {
                EmitExpression(indexes[0]);
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
                emitter.LdDeLabel(label);
                emitter.LdBImm(dimensions);
                emitter.CallLabel(RuntimeLibrary.ArrayElemLabel);
            }
        }

        // Textes (lot E, D062). Un texte écrit dans le programme a sa fiche (longueur, adresse) à la fin du binaire.
        var literals = new List<string>();
        bool usesStrings = program.Statements.Any(StatementUsesStrings);

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

                        case "SGN":
                            EmitExpression(call.Arguments[0]);
                            required.Add(RuntimeLibrary.Sgn16Name);
                            emitter.CallLabel(RuntimeLibrary.Sgn16Label);
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

            emitter.Label(loop.NextLabel);
            EmitLoad(slot);
            emitter.LdDeImm(loop.Step & 0xFFFF);
            emitter.OrA();                                  // retenue à 0
            emitter.AdcHlDe();                              // HL + pas, indicateur de débordement mis à jour
            EmitStore(slot);                                // (LD ne modifie pas les indicateurs)
            if (slot.Type == VarType.Integer)
                emitter.JpPeLabel(loop.ExitLabel);          // débordement : fin de la boucle

            emitter.LdAH();                                 // retourne le bit 15 de HL
            emitter.XorImm(0x80);
            emitter.LdHA();
            emitter.LdDeImm((loop.Limit & 0xFFFF) ^ 0x8000);   // limite, même transformation

            if (loop.Step > 0)
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

            emitter.JpNcLabel(loop.BodyLabel);              // pas de retenue : on continue
            emitter.Label(loop.ExitLabel);
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

        void EmitScreen(ScreenStatement screen)
        {
            IReadOnlyList<Expression> a = screen.Arguments;
            switch (screen.Command)
            {
                case "CLS":
                    emitter.CallAddress(TxtClearWindow);
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
        var dataValues = new List<int>();
        var dataStart = new SortedDictionary<int, int>();
        for (int i = 0; i < program.Statements.Count; i++)
        {
            if (program.Statements[i] is DataStatement data)
            {
                dataStart[i] = dataValues.Count;
                dataValues.AddRange(data.Values);
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
            required.Add(RuntimeLibrary.DataName);

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

        int statementIndex = 0;
        int basicLine = 0;   // numéro de la ligne BASIC en cours, pour les messages d'erreur des tableaux
        foreach (Statement statement in program.Statements)
        {
            if (linesAt.TryGetValue(statementIndex, out List<int>? numbersHere))
                basicLine = numbersHere.Max();
            EmitLineLabels(statementIndex++);

            // Une instruction qui touche un tableau note son numéro de ligne (« Subscript out of range in 30 ») ;
            // sans numéros de ligne, c'est la ligne de l'éditeur.
            // Textes : les fiches temporaires de l'instruction précédente sont rendues (D062)
            if (usesStrings && AstWalker.All(statement).Any(e => e is ConcatExpression || (e is CallExpression c && c.Function.EndsWith('$'))))
                emitter.CallLabel(RuntimeLibrary.StrResetLabel);

            if (AstWalker.UsesArrays(statement) || statement is ReadStatement || StatementUsesStrings(statement)
                || ((arrays.Count > 0 || usesStrings) && statement is ScreenStatement { Command: "MEMORY" }))
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
                        if (VariableNames.IsString(input.Variables[i]) && input.Indexes?[i] is null)
                        {
                            required.Add(RuntimeLibrary.InputStringName);   // INPUT A$ (D062)
                            emitter.LdDeLabel(Slot(input.Variables[i]).Label);
                            emitter.CallLabel(RuntimeLibrary.InputStrLabel);
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
                    EmitText(print.NewLine ? print.Text + "\r\n" : print.Text);
                    break;

                case PrintValueStatement printValue when Types.IsString(printValue.Value):
                    EmitString(printValue.Value);                   // PRINT A$ (D062)
                    emitter.CallLabel(RuntimeLibrary.StrPrintLabel);
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case PrintValueStatement printValue:
                    EmitExpression(printValue.Value);
                    required.Add(RuntimeLibrary.PrintIntName);
                    emitter.CallLabel(RuntimeLibrary.PrintIntLabel);
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case AssignStatement assign when VariableNames.IsString(assign.Variable):
                    EmitString(assign.Value);                       // A$ = texte (D062)
                    emitter.LdDeLabel(Slot(assign.Variable).Label);
                    emitter.CallLabel(RuntimeLibrary.StrAssignLabel);
                    break;

                case AssignStatement assign:
                    EmitExpression(assign.Value);
                    EmitStore(Slot(assign.Variable));
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
                        emitter.LdDeLabel(label);
                        emitter.CallLabel(RuntimeLibrary.ArrayDimLabel);
                    }
                    break;

                case DeclareStatement declare:
                    foreach (string declaredName in declare.Names)
                        declared[declaredName] = VarType.Byte;   // aucun code : seul le type est retenu
                    break;

                case ForStatement forStatement:
                {
                    EmitExpression(forStatement.Start);
                    EmitStore(Slot(forStatement.Variable));

                    int id = loopCount++;
                    var loop = new LoopInfo(LoopKind.For, forStatement.Variable, forStatement.Step, forStatement.Limit,
                                            $"loop_{id}_body", $"loop_{id}_next", $"loop_{id}_exit");
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

                case ScreenStatement screen:
                    EmitScreen(screen);
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
                    emitter.Word(dataValues[k] & 0xFFFF);
            }
            emitter.Label(RuntimeLibrary.DataEndLabel);
        }

        // Variables, à 0 au départ : 1 octet pour un octet, 2 pour un entier
        foreach (VariableSlot slot in slots)
        {
            emitter.Label(slot.Label);
            emitter.Reserve(slot.Type switch { VarType.Byte => 1, VarType.String => 3, _ => 2 });
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
        if (arrays.Count > 0 || usesStrings)
            emitter.Label(RuntimeLibrary.ArrayHeapStartLabel);   // la mémoire libre commence ici

        return new GeneratedProgram(emitter.ToBytes(), modules.Select(m => m.Name).ToList(), memoryLimit, memoryLine);
    }

    // Routines du firmware appelées par les commandes d'écran (décision D046)
    const int ScrSetMode = 0xBC0E;      // SCR SET MODE : A = mode
    const int TxtClearWindow = 0xBB6C;  // TXT CLEAR WINDOW : efface la fenêtre de texte
    const int TxtSetCursor = 0xBB75;    // TXT SET CURSOR : H = colonne, L = ligne
    const int TxtSetPen = 0xBB90;       // TXT SET PEN : A = stylo
    const int TxtSetPaper = 0xBB96;     // TXT SET PAPER : A = fond
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
    static bool StatementUsesStrings(Statement statement) =>
        AstWalker.All(statement).Any(Types.IsString)
        || (statement is AssignStatement assign && VariableNames.IsString(assign.Variable))
        || (statement is InputStatement input && input.Variables.Any(VariableNames.IsString));

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
