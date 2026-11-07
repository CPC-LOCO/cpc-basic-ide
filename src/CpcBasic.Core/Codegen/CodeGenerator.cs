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
/// instructions du programme, fin de programme, modules utilisés, puis textes.
/// Le programme ne revient jamais au BASIC : à la fin, il attend une touche puis redémarre.
/// </summary>
public static class CodeGenerator
{
    const string EndLabel = "program_end";

    public static GeneratedProgram Generate(BasicProgram program, int origin)
    {
        var emitter = new Z80Emitter(origin);
        var required = new HashSet<string>();
        var strings = new List<byte[]>();

        foreach (Statement statement in program.Statements)
        {
            switch (statement)
            {
                case PrintStatement print:
                    string text = print.NewLine ? print.Text + "\r\n" : print.Text;
                    if (text.Length == 0)
                        break;                                    // rien à afficher

                    required.Add(RuntimeLibrary.PrintStringName);
                    emitter.LdHlLabel(StringLabel(strings.Count));
                    emitter.CallLabel(RuntimeLibrary.PrintStringLabel);
                    strings.Add(Encode(text));
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
