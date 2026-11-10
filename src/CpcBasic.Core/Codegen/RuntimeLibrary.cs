// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

namespace CpcBasic.Core.Codegen;

/// <summary>
/// Un module de la bibliothèque : une routine Z80 avec la liste des modules dont elle dépend.
/// </summary>
/// <param name="Emit">Écrit le code du module avec l'assembleur.</param>
public sealed record RuntimeModule(string Name, IReadOnlyList<string> Dependencies, Action<Z80Emitter> Emit);

/// <summary>La bibliothèque : tous les modules disponibles.</summary>
public static class RuntimeLibrary
{
    /// <summary>Routine du firmware CPC qui affiche le caractère contenu dans A (TXT OUTPUT).</summary>
    public const int TxtOutput = 0xBB5A;

    /// <summary>Routine du firmware CPC qui attend l'appui sur une touche (KM WAIT KEY).</summary>
    public const int KmWaitKey = 0xBB18;

    public const string PrintStringName = "PrintString";
    public const string PrintStringLabel = "rt_print_string";

    public const string PrintIntName = "PrintInt";
    public const string PrintIntLabel = "rt_print_int";

    public const string Abs16Name = "Abs16";
    public const string Abs16Label = "rt_abs16";

    public const string Sgn16Name = "Sgn16";
    public const string Sgn16Label = "rt_sgn16";

    public const string Min16Name = "Min16";
    public const string Min16Label = "rt_min16";

    public const string Max16Name = "Max16";
    public const string Max16Label = "rt_max16";

    public const string Mul16Name = "Mul16";
    public const string Mul16Label = "rt_mul16";

    public const string Div16Name = "Div16";
    public const string Div16Label = "rt_div16";

    static readonly Dictionary<string, RuntimeModule> Modules = new()
    {
        // HL = valeur absolue de HL (la valeur -32768 reste -32768). Détruit AF.
        [Abs16Name] = new RuntimeModule(Abs16Name, [], e =>
        {
            e.Label(Abs16Label);
            e.Bit7H();
            e.RetZ();                            // positif : rien à faire
            e.XorA();                            // HL = 0 - HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.Ret();
        }),

        // HL = signe de HL : -1, 0 ou 1. Détruit AF.
        [Sgn16Name] = new RuntimeModule(Sgn16Name, [], e =>
        {
            e.Label(Sgn16Label);
            e.LdAH();
            e.OrL();
            e.RetZ();                            // HL = 0 : le résultat est déjà 0
            e.Bit7H();
            e.LdHlImm(1);                        // (LD ne modifie pas les indicateurs)
            e.RetZ();                            // positif : 1
            e.LdHlImm(0xFFFF);                   // négatif : -1
            e.Ret();
        }),

        // HL = le plus petit de HL et DE (comparaison signée). Détruit AF, DE.
        // Le bit 15 des deux valeurs est retourné pour que la retenue donne la comparaison signée.
        [Min16Name] = new RuntimeModule(Min16Name, [], e =>
        {
            e.Label(Min16Label);
            e.PushHl();
            e.PushDe();
            e.LdAH();
            e.XorImm(0x80);
            e.LdHA();
            e.LdAD();
            e.XorImm(0x80);
            e.LdDA();
            e.OrA();
            e.SbcHlDe();                         // retenue si HL < DE
            e.PopDe();                           // POP ne modifie pas les indicateurs
            e.PopHl();
            e.RetC();                            // HL < DE : le minimum est HL
            e.ExDeHl();                          // sinon c'est DE
            e.Ret();
        }),

        // HL = le plus grand de HL et DE (comparaison signée). Détruit AF, DE.
        [Max16Name] = new RuntimeModule(Max16Name, [], e =>
        {
            e.Label(Max16Label);
            e.PushHl();
            e.PushDe();
            e.LdAH();
            e.XorImm(0x80);
            e.LdHA();
            e.LdAD();
            e.XorImm(0x80);
            e.LdDA();
            e.OrA();
            e.SbcHlDe();                         // retenue si HL < DE
            e.PopDe();
            e.PopHl();
            e.RetNc();                           // HL >= DE : le maximum est HL
            e.ExDeHl();                          // sinon c'est DE
            e.Ret();
        }),

        // HL = HL * DE (16 bits de poids faible du produit : le résultat est le même pour des nombres
        // signés ou non signés). Doublement et addition, en commençant par le bit de poids fort.
        // Détruit AF, BC, DE.
        [Mul16Name] = new RuntimeModule(Mul16Name, [], e =>
        {
            e.Label(Mul16Label);
            e.LdBH();                            // BC = multiplicande
            e.LdCL();
            e.LdHlImm(0);                        // résultat
            e.LdAImm(16);                        // 16 bits à traiter
            e.Label("rt_mul_loop");
            e.AddHlHl();                         // résultat * 2
            e.SlaE();                            // bit de poids fort du multiplicateur -> retenue
            e.RlD();
            e.JrNcLabel("rt_mul_skip");
            e.AddHlBc();                         // bit à 1 : on ajoute le multiplicande
            e.Label("rt_mul_skip");
            e.DecA();
            e.JrNzLabel("rt_mul_loop");
            e.Ret();
        }),

        // Division entière signée : HL / DE. Résultat : quotient dans HL, reste dans DE.
        // Le quotient est tronqué vers 0 et le reste a le signe du dividende (-7 MOD 3 = -1).
        // Diviseur à 0 : quotient 0 et reste = dividende (pas d'erreur à l'exécution pour l'instant).
        // Détruit AF, BC, DE, HL.
        [Div16Name] = new RuntimeModule(Div16Name, [], e =>
        {
            e.Label(Div16Label);
            e.LdAD();
            e.OrE();
            e.JrNzLabel("rt_div_ok");
            e.ExDeHl();                          // diviseur 0 : DE = dividende
            e.LdHlImm(0);
            e.Ret();

            // B : bit 0 = dividende négatif (reste négatif), bit 1 = quotient négatif
            e.Label("rt_div_ok");
            e.LdBImm(0);
            e.Bit7H();
            e.JrZLabel("rt_div_pos1");
            e.XorA();                            // HL = 0 - HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.LdAB();
            e.XorImm(3);
            e.LdBA();
            e.Label("rt_div_pos1");
            e.Bit7D();
            e.JrZLabel("rt_div_pos2");
            e.XorA();                            // DE = 0 - DE
            e.SubE();
            e.LdEA();
            e.SbcAA();
            e.SubD();
            e.LdDA();
            e.LdAB();
            e.XorImm(2);
            e.LdBA();
            e.Label("rt_div_pos2");

            // Division non signée : le dividende passe dans A:C, le reste se construit dans HL.
            e.PushBc();                          // on garde les indicateurs de signe
            e.LdAH();
            e.LdCL();
            e.LdHlImm(0);
            e.LdBImm(16);
            e.Label("rt_div_loop");
            e.Scf();                             // décale le dividende, le bit de quotient entre à 1
            e.RlC();
            e.Rla();
            e.AdcHlHl();                         // le bit sorti entre dans le reste
            e.SbcHlDe();                         // reste - diviseur
            e.JrNcLabel("rt_div_next");
            e.AddHlDe();                         // trop soustrait : on remet, et le bit de quotient vaut 0
            e.DecC();
            e.Label("rt_div_next");
            e.DjnzLabel("rt_div_loop");
            e.ExDeHl();                          // DE = reste
            e.LdHA();                            // HL = quotient (A:C)
            e.LdLC();
            e.PopBc();

            e.Bit1B();
            e.JrZLabel("rt_div_q");
            e.XorA();                            // quotient négatif
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.Label("rt_div_q");
            e.Bit0B();
            e.JrZLabel("rt_div_r");
            e.XorA();                            // reste négatif
            e.SubE();
            e.LdEA();
            e.SbcAA();
            e.SubD();
            e.LdDA();
            e.Label("rt_div_r");
            e.Ret();
        }),

        // Affiche le texte terminé par un octet 0, dont l'adresse est dans HL.
        [PrintStringName] = new RuntimeModule(PrintStringName, [], e =>
        {
            e.Label(PrintStringLabel);
            e.LdAHl();                       // LD A,(HL)
            e.OrA();                         // OR A
            e.RetZ();                        // RET Z : fin du texte
            e.CallAddress(TxtOutput);        // CALL &BB5A : affiche le caractère
            e.IncHl();                       // INC HL
            e.JrLabel(PrintStringLabel);     // JR début
        }),

        // Affiche l'entier signé contenu dans HL, comme le BASIC : un espace devant s'il est positif
        // (ou « - » s'il est négatif), puis les chiffres sans zéros inutiles, puis un espace.
        // Détruit AF, BC, DE, HL.
        [PrintIntName] = new RuntimeModule(PrintIntName, [], e =>
        {
            e.Label(PrintIntLabel);
            e.LdBImm(0);                         // B = 0 : aucun chiffre affiché encore
            e.Bit7H();                           // BIT 7,H : nombre négatif ?
            e.JrZLabel("rt_pi_pos");

            e.LdAImm('-');
            e.CallLabel("rt_pi_out");
            e.XorA();                            // HL = 0 - HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.JrLabel("rt_pi_digits");

            e.Label("rt_pi_pos");
            e.LdAImm(' ');
            e.CallLabel("rt_pi_out");

            e.Label("rt_pi_digits");
            foreach (int divisor in new[] { 10000, 1000, 100, 10 })
            {
                e.LdDeImm(divisor);
                e.CallLabel("rt_pi_digit");
            }
            e.LdBImm(1);                         // le dernier chiffre est toujours affiché
            e.LdDeImm(1);
            e.CallLabel("rt_pi_digit");
            e.LdAImm(' ');                       // espace final
            e.JpLabel("rt_pi_out");

            // Calcule et affiche un chiffre : divise HL par DE, le reste reste dans HL.
            e.Label("rt_pi_digit");
            e.LdAImm('0' - 1);
            e.Label("rt_pi_loop");
            e.IncA();
            e.OrA();                             // retenue à 0
            e.SbcHlDe();
            e.JrNcLabel("rt_pi_loop");
            e.AddHlDe();                         // on a trop soustrait une fois
            e.LdCA();
            e.LdAB();
            e.OrA();                             // un chiffre a déjà été affiché ?
            e.LdAC();
            e.JrNzLabel("rt_pi_show");
            e.CpImm('0');
            e.RetZ();                            // zéro de tête : rien à afficher
            e.Label("rt_pi_show");
            e.LdBImm(1);

            // Affiche A sans rien détruire
            e.Label("rt_pi_out");
            e.PushBc();
            e.PushDe();
            e.PushHl();
            e.CallAddress(TxtOutput);
            e.PopHl();
            e.PopDe();
            e.PopBc();
            e.Ret();
        })
    };

    public static RuntimeModule Get(string name) =>
        Modules.TryGetValue(name, out RuntimeModule? module)
            ? module
            : throw new InvalidOperationException($"Module inconnu dans la bibliothèque : {name}");
}

/// <summary>Liaison sélective : ne retient que les modules demandés et leurs dépendances.</summary>
public static class Linker
{
    public static List<RuntimeModule> Resolve(IEnumerable<string> required)
    {
        var found = new Dictionary<string, RuntimeModule>();
        var pending = new Stack<string>(required);

        while (pending.Count > 0)
        {
            string name = pending.Pop();
            if (found.ContainsKey(name))
                continue;

            RuntimeModule module = RuntimeLibrary.Get(name);
            found[name] = module;
            foreach (string dependency in module.Dependencies)
                pending.Push(dependency);
        }

        // Ordre fixe, pour que le même programme donne toujours le même binaire
        return found.Values.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
    }
}
