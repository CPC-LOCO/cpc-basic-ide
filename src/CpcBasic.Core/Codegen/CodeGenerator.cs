// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;

namespace CpcBasic.Core.Codegen;

/// <summary>Le binaire produit et la liste des modules de la bibliothèque qui y ont été liés.</summary>
public sealed record GeneratedProgram(byte[] Code, IReadOnlyList<string> Modules);

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
                VarType type = declared.TryGetValue(name, out VarType declaredType) ? declaredType : VarType.Integer;
                slot = new VariableSlot($"var_{slots.Count}", type);
                variables[name] = slot;
                slots.Add(slot);
            }
            return slot;
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

                        default:
                            required.Add(RuntimeLibrary.Div16Name);
                            emitter.CallLabel(RuntimeLibrary.Div16Label);
                            emitter.ExDeHl();                                      // HL = reste
                            break;
                    }
                    break;

                case CallExpression call:
                    switch (call.Function)
                    {
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

            EmitExpression(comparison.Left);
            if (comparison.Right is NumberExpression right)
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

        foreach (Statement statement in program.Statements)
        {
            switch (statement)
            {
                case PrintStatement print:
                    EmitText(print.NewLine ? print.Text + "\r\n" : print.Text);
                    break;

                case PrintValueStatement printValue:
                    EmitExpression(printValue.Value);
                    required.Add(RuntimeLibrary.PrintIntName);
                    emitter.CallLabel(RuntimeLibrary.PrintIntLabel);
                    if (printValue.NewLine)
                        EmitText("\r\n");
                    break;

                case AssignStatement assign:
                    EmitExpression(assign.Value);
                    EmitStore(Slot(assign.Variable));
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

        // Variables, à 0 au départ : 1 octet pour un octet, 2 pour un entier
        foreach (VariableSlot slot in slots)
        {
            emitter.Label(slot.Label);
            emitter.Reserve(slot.Type == VarType.Byte ? 1 : 2);
        }

        return new GeneratedProgram(emitter.ToBytes(), modules.Select(m => m.Name).ToList());
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
