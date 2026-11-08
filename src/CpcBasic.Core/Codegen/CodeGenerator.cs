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
/// </summary>
public static class CodeGenerator
{
    const string EndLabel = "program_end";

    sealed record VariableSlot(string Label, VarType Type);

    public static GeneratedProgram Generate(BasicProgram program, int origin)
    {
        var emitter = new Z80Emitter(origin);
        var required = new HashSet<string>();
        var strings = new List<byte[]>();
        var variables = new Dictionary<string, VariableSlot>();
        var slots = new List<VariableSlot>();   // dans l'ordre de première utilisation
        var declared = new Dictionary<string, VarType>();   // variables déclarées BYTE

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

        // Calcule l'expression : le résultat est dans HL
        void EmitExpression(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression number:
                    emitter.LdHlImm(number.Value & 0xFFFF);
                    break;

                case VariableExpression variable:
                    VariableSlot slot = Slot(variable.Name);
                    if (slot.Type == VarType.Integer)
                    {
                        emitter.LdHlMem(slot.Label);
                    }
                    else
                    {
                        emitter.LdAMem(slot.Label);     // octet étendu à 16 bits
                        emitter.LdLA();
                        emitter.LdHZero();
                    }
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
                        if (binary.Operator == BinaryOperator.Subtract)
                            emitter.ExDeHl();                       // HL = gauche, DE = droite
                    }

                    if (binary.Operator == BinaryOperator.Add)
                    {
                        emitter.AddHlDe();
                    }
                    else
                    {
                        emitter.OrA();                              // retenue à 0
                        emitter.SbcHlDe();
                    }
                    break;

                default:
                    throw new NotSupportedException($"Expression non gérée : {expression.GetType().Name}");
            }
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
                    VariableSlot target = Slot(assign.Variable);
                    if (target.Type == VarType.Integer)
                    {
                        emitter.LdMemHl(target.Label);
                    }
                    else
                    {
                        emitter.LdAL();                 // on garde l'octet de poids faible (modulo 256)
                        emitter.LdMemA(target.Label);
                    }
                    break;

                case DeclareStatement declare:
                    foreach (string declaredName in declare.Names)
                        declared[declaredName] = VarType.Byte;   // aucun code : seul le type est retenu
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
