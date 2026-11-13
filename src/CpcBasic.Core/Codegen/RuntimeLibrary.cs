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

    /// <summary>Routine du firmware CPC qui donne le temps écoulé dans DEHL (KL TIME PLEASE).</summary>
    public const int KlTimePlease = 0xBD0D;

    public const string PrintStringName = "PrintString";
    public const string PrintStringLabel = "rt_print_string";

    public const string PrintIntName = "PrintInt";
    public const string PrintIntLabel = "rt_print_int";

    public const string RandStateLabel = "rt_rand_state";

    public const string Rand16Name = "Rand16";
    public const string Rand16Label = "rt_rand16";

    public const string RandomizeName = "Randomize";
    public const string RandomizeLabel = "rt_randomize";
    public const string RandomizeClockLabel = "rt_randomize_clock";

    public const string RandomRangeName = "RandomRange";
    public const string RandomRangeLabel = "rt_random_range";

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

    // Tracé direct dans la mémoire écran (D048)
    public const string PixelName = "Pixel";
    public const string BoxName = "Box";
    public const string BoxLabel = "rt_box";
    public const string BoxX1Label = "rt_box_x1";
    public const string BoxY1Label = "rt_box_y1";
    public const string BoxX2Label = "rt_box_x2";
    public const string BoxY2Label = "rt_box_y2";
    public const string BoxPenLabel = "rt_box_pen";
    public const string BoxFillPenLabel = "rt_box_fillpen";
    public const string BoxDoFillLabel = "rt_box_dofill";

    static readonly Dictionary<string, RuntimeModule> Modules = new()
    {
        // HL = nombre aléatoire de 16 bits. Registre à décalage de 16 bits (polynôme &B400, période 65535),
        // avancé de 16 pas à chaque appel : chaque résultat est fait de bits entièrement nouveaux.
        // L'état est rangé juste après le code. État à 0 = pas encore initialisé : le départ est alors
        // pris dans l'horloge du CPC (KL TIME PLEASE). Détruit AF, B, DE, HL.
        [Rand16Name] = new RuntimeModule(Rand16Name, [], e =>
        {
            e.Label(Rand16Label);
            e.LdHlMem(RandStateLabel);
            e.LdAH();
            e.OrL();
            e.JrNzLabel("rt_rand_go");
            e.CallAddress(KlTimePlease);         // DEHL = temps écoulé
            e.LdAH();                            // HL = HL xor DE
            e.XorD();
            e.LdHA();
            e.LdAL();
            e.XorE();
            e.LdLA();
            e.LdAH();
            e.OrL();
            e.JrNzLabel("rt_rand_go");
            e.LdHlImm(1);                        // l'état ne doit jamais être 0
            e.Label("rt_rand_go");
            e.LdBImm(16);
            e.Label("rt_rand_loop");
            e.SrlH();                            // décalage à droite, le bit sorti va dans la retenue
            e.RrL();
            e.JrNcLabel("rt_rand_skip");
            e.LdAH();                            // bit sorti à 1 : on applique le polynôme
            e.XorImm(0xB4);
            e.LdHA();
            e.Label("rt_rand_skip");
            e.DjnzLabel("rt_rand_loop");
            e.LdMemHl(RandStateLabel);
            e.Ret();
            e.Label(RandStateLabel);
            e.Reserve(2);                        // l'état
        }),

        // RANDOMIZE : HL = graine (0 devient 1, car l'état 0 veut dire « pas initialisé »).
        // rt_randomize_clock : la prochaine valeur repart de l'horloge. Détruit HL.
        [RandomizeName] = new RuntimeModule(RandomizeName, [Rand16Name], e =>
        {
            e.Label(RandomizeLabel);
            e.LdAH();
            e.OrL();
            e.JrNzLabel("rt_randomize_set");
            e.LdHlImm(1);
            e.Label("rt_randomize_set");
            e.LdMemHl(RandStateLabel);
            e.Ret();

            e.Label(RandomizeClockLabel);
            e.LdHlImm(0);
            e.LdMemHl(RandStateLabel);
            e.Ret();
        }),

        // RANDOM(min, max) : HL = min, DE = max, résultat dans HL, de min à max compris.
        // L'écart (max - min + 1) doit aller de 1 à 32767 ; sinon le résultat est min.
        // On prend le reste de la division d'un nombre aléatoire de 15 bits par l'écart.
        [RandomRangeName] = new RuntimeModule(RandomRangeName, [Rand16Name, Div16Name], e =>
        {
            e.Label(RandomRangeLabel);
            e.PushHl();                          // min
            e.ExDeHl();                          // HL = max, DE = min
            e.OrA();
            e.SbcHlDe();
            e.IncHl();                           // HL = nombre de valeurs possibles
            e.Bit7H();
            e.JrNzLabel("rt_rr_bad");            // négatif (max < min, ou écart trop grand)
            e.LdAH();
            e.OrL();
            e.JrZLabel("rt_rr_bad");             // 0
            e.PushHl();                          // écart
            e.CallLabel(Rand16Label);
            e.Res7H();                           // 0 à 32767
            e.PopDe();                           // DE = écart
            e.CallLabel(Div16Label);             // DE = reste : 0 à écart - 1
            e.ExDeHl();
            e.PopDe();                           // DE = min
            e.AddHlDe();
            e.Ret();
            e.Label("rt_rr_bad");
            e.PopHl();                           // HL = min
            e.Ret();
        }),

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

        // Tracé direct dans la mémoire écran, sans passer par le firmware (D048). Coordonnées natives du MODE courant,
        // origine en haut à gauche : x de 0 à 159 (MODE 0), 319 (MODE 1) ou 639 (MODE 2), y de 0 à 199.
        // L'écran est supposé à son adresse normale (&C000) et non décalé par un défilement matériel.
        // L'appelant doit avoir coupé la ROM haute (KL U ROM DISABLE), sinon le Z80 lirait la ROM du BASIC à la place de l'écran.
        [PixelName] = new RuntimeModule(PixelName, [], e =>
        {
            // Module Pixel : tracé direct dans la mémoire écran (D048). Coordonnées natives : x de 0 à 159, 319 ou 639 selon le MODE, y de 0 à 199.
            // ---------- rt_px_setup : A = stylo. Lit le MODE avec le firmware et prépare les réglages ----------
            e.Label("rt_px_setup");
            e.AndImm(15);                                 // stylo de 0 à 15
            e.PushAf();
            e.CallAddress(0xBC11);                        // SCR GET MODE : A = mode courant (0, 1 ou 2)
            e.PopBc();                                    // B = stylo
            e.LdCA();                                     // C = mode
            e.AddAA();                                    // A = mode * 8
            e.AddAA();
            e.AddAA();
            e.LdEA();
            e.LdDZero();                                  // DE = mode * 8
            e.LdHlLabel("rt_px_params");
            e.AddHlDe();
            e.LdAHl();
            e.LdMemA("rt_px_shift");                      // décalage de x pour trouver l'octet
            e.IncHl();
            e.LdAHl();
            e.LdMemA("rt_px_pmask");                      // masque du rang du point dans l'octet
            e.IncHl();
            e.LdAHl();
            e.LdMemA("rt_px_mask");                       // bits du point le plus à gauche
            e.IncHl();
            e.IncHl();
            e.LdAHl();
            e.LdMemA("rt_px_wmax_lo");                    // plus grand x : 159, 319 ou 639
            e.IncHl();
            e.LdAHl();
            e.LdMemA("rt_px_wmax_hi");
            e.LdHlLabel("rt_px_from");
            e.AddHlDe();
            e.LdMemHl("rt_px_fromp");                     // table « du point p jusqu'à la fin de l'octet »
            e.LdHlLabel("rt_px_head");
            e.AddHlDe();
            e.LdMemHl("rt_px_headp");                     // table « du début de l'octet jusqu'au point p »
            e.LdAC();                                     // A = mode
            e.AddAA();                                    // A = mode * 16
            e.AddAA();
            e.AddAA();
            e.AddAA();
            e.AddAB();                                    // + stylo
            e.LdEA();
            e.LdDZero();
            e.LdHlLabel("rt_px_pens");
            e.AddHlDe();
            e.LdAHl();
            e.LdMemA("rt_px_pat");                        // bits du stylo pour tous les points de l'octet
            e.Ret();
            // ---------- rt_px_rowaddr : DE = y (0 à 199) -> HL = adresse du début de la ligne écran. Détruit AF, C, DE ----------
            e.Label("rt_px_rowaddr");
            e.LdAE();
            e.AndImm(7);
            e.AddAA();                                    // (y MOD 8) * 8
            e.AddAA();
            e.AddAA();
            e.AddAImm(0xC0);
            e.LdHA();
            e.LdLZero();                                  // HL = &C000 + (y MOD 8) * &800
            e.LdAE();
            e.SrlA();                                     // y \ 8 = ligne de caractères
            e.SrlA();
            e.SrlA();
            e.LdCA();
            e.AddAA();
            e.AddAA();
            e.AddAC();                                    // ligne * 5
            e.LdEA();
            e.LdDZero();
            e.SlaE();                                     // * 16, donc ligne * 80
            e.RlD();
            e.SlaE();
            e.RlD();
            e.SlaE();
            e.RlD();
            e.SlaE();
            e.RlD();
            e.AddHlDe();
            e.Ret();
            // ---------- rt_px_split : DE = x -> DE = x \ points par octet, C = rang du point dans l'octet. Détruit AF, B ----------
            e.Label("rt_px_split");
            e.LdAMem("rt_px_pmask");
            e.AndE();
            e.LdCA();
            e.LdAMem("rt_px_shift");
            e.LdBA();
            e.Label("rt_px_xs");
            e.SrlD();
            e.RrE();
            e.DjnzLabel("rt_px_xs");
            e.Ret();
            // ---------- rt_px_lookup : HL = table, A = rang -> A = octet de la table. Détruit HL seulement (en plus de A) ----------
            e.Label("rt_px_lookup");
            e.AddAL();
            e.LdLA();
            e.LdAH();
            e.AdcAImm(0);
            e.LdHA();
            e.LdAHl();
            e.Ret();
            // ---------- rt_px_putmask : HL = adresse, D = bits à changer. Met le stylo sur ces bits. Détruit AF, E ----------
            e.Label("rt_px_putmask");
            e.LdAMem("rt_px_pat");
            e.AndD();
            e.LdEA();
            e.LdAD();
            e.Cpl();
            e.AndAHl();
            e.OrE();
            e.LdHlA();
            e.Ret();
            // ---------- rt_pixel : HL = x, DE = y. Un point, ou rien s'il est hors de l'écran. Détruit AF, BC, DE, HL ----------
            e.Label("rt_pixel");
            e.LdAD();                                     // y doit tenir sur un octet
            e.OrA();
            e.RetNz();
            e.LdAE();
            e.CpImm(200);                                 // et valoir moins de 200
            e.RetNc();
            e.PushHl();                                   // x
            e.CallLabel("rt_px_rowaddr");
            e.PopDe();                                    // DE = x
            e.CallLabel("rt_px_split");
            e.LdAD();
            e.OrA();
            e.RetNz();                                    // hors de l'écran (à droite ou x négatif)
            e.LdAE();
            e.CpImm(80);
            e.RetNc();
            e.AddHlDe();                                  // D vaut 0 : HL = adresse de l'octet
            e.LdAMem("rt_px_mask");
            e.LdDA();                                     // D = bits du point le plus à gauche
            e.LdAC();
            e.OrA();
            e.JrZLabel("rt_px_one");
            e.LdBA();
            e.Label("rt_px_ps");
            e.SrlD();                                     // on décale jusqu'au point voulu
            e.DjnzLabel("rt_px_ps");
            e.Label("rt_px_one");
            e.JpLabel("rt_px_putmask");
            // ---------- rt_px_hline : DE = x1, BC = x2, HL = y. Ligne horizontale, par octets entiers. Valeurs dans l'écran, x1 <= x2 ----------
            e.Label("rt_px_hline");
            e.PushBc();
            e.PushDe();
            e.ExDeHl();                                   // DE = y
            e.CallLabel("rt_px_rowaddr");
            e.LdMemHl("rt_px_row");
            e.PopDe();                                    // x1
            e.CallLabel("rt_px_split");
            e.LdAE();
            e.LdMemA("rt_px_c1");
            e.LdAC();
            e.LdMemA("rt_px_p1");
            e.PopDe();                                    // x2
            e.CallLabel("rt_px_split");
            e.LdAE();
            e.LdMemA("rt_px_c2");
            e.LdAC();
            e.LdMemA("rt_px_p2");
            e.LdHlMem("rt_px_row");
            e.LdAMem("rt_px_c1");
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();                                  // HL = octet de x1
            e.LdAMem("rt_px_c2");
            e.LdBA();
            e.LdAMem("rt_px_c1");
            e.CpB();
            e.JrNzLabel("rt_hl_many");
            e.PushHl();                                   // un seul octet : du point p1 au point p2
            e.LdHlMem("rt_px_fromp");
            e.LdAMem("rt_px_p1");
            e.CallLabel("rt_px_lookup");
            e.LdDA();
            e.LdHlMem("rt_px_headp");
            e.LdAMem("rt_px_p2");
            e.CallLabel("rt_px_lookup");
            e.AndD();
            e.LdDA();
            e.PopHl();
            e.JpLabel("rt_px_putmask");
            e.Label("rt_hl_many");
            e.PushHl();                                   // premier octet : du point p1 à la fin
            e.LdHlMem("rt_px_fromp");
            e.LdAMem("rt_px_p1");
            e.CallLabel("rt_px_lookup");
            e.LdDA();
            e.PopHl();
            e.CallLabel("rt_px_putmask");
            e.IncHl();
            e.LdAMem("rt_px_c2");                         // octets entiers du milieu : c2 - c1 - 1
            e.LdBA();
            e.LdAMem("rt_px_c1");
            e.LdCA();
            e.LdAB();
            e.SubC();
            e.DecA();
            e.JrZLabel("rt_hl_last");
            e.LdBA();
            e.LdAMem("rt_px_pat");
            e.Label("rt_hl_fill");
            e.LdHlA();
            e.IncHl();
            e.DjnzLabel("rt_hl_fill");
            e.Label("rt_hl_last");
            e.PushHl();                                   // dernier octet : du début au point p2
            e.LdHlMem("rt_px_headp");
            e.LdAMem("rt_px_p2");
            e.CallLabel("rt_px_lookup");
            e.LdDA();
            e.PopHl();
            e.JpLabel("rt_px_putmask");
            // ---------- rt_px_vline : DE = x, HL = y1, BC = y2. Ligne verticale, en descendant d'une ligne écran à la fois. Valeurs dans l'écran, y1 <= y2 ----------
            e.Label("rt_px_vline");
            e.LdAC();
            e.SubL();
            e.IncA();
            e.LdMemA("rt_px_cnt");                        // nombre de lignes
            e.PushHl();                                   // y1
            e.CallLabel("rt_px_split");
            e.LdAE();
            e.LdMemA("rt_px_c1");
            e.LdAC();
            e.LdMemA("rt_px_p1");
            e.PopDe();                                    // DE = y1
            e.CallLabel("rt_px_rowaddr");
            e.LdAMem("rt_px_c1");
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();                                  // HL = octet du point de départ
            e.PushHl();
            e.LdHlMem("rt_px_fromp");
            e.LdAMem("rt_px_p1");
            e.CallLabel("rt_px_lookup");
            e.LdDA();
            e.LdHlMem("rt_px_headp");
            e.LdAMem("rt_px_p1");
            e.CallLabel("rt_px_lookup");
            e.AndD();
            e.LdDA();                                     // D = bits du point
            e.LdAMem("rt_px_pat");
            e.AndD();
            e.LdEA();                                     // E = bits du stylo
            e.PopHl();
            e.LdAMem("rt_px_cnt");
            e.LdBA();
            e.Label("rt_vl_loop");
            e.LdAD();
            e.Cpl();
            e.AndAHl();
            e.OrE();
            e.LdHlA();
            e.PushDe();
            e.LdDeImm(0x0800);                            // ligne écran suivante
            e.AddHlDe();
            e.JrNcLabel("rt_vl_next");
            e.LdDeImm(0xC050);                            // on change de ligne de caractères
            e.AddHlDe();
            e.Label("rt_vl_next");
            e.PopDe();
            e.DjnzLabel("rt_vl_loop");
            e.Ret();
            // ---------- Données ----------
            e.Label("rt_px_params");
            e.Data(new byte[] { 0x01, 0x01, 0xAA, 0x00, 0x9F, 0x00, 0x00, 0x00 });// MODE 0 : 2 points par octet, x jusqu'à 159
            e.Data(new byte[] { 0x02, 0x03, 0x88, 0x00, 0x3F, 0x01, 0x00, 0x00 });// MODE 1 : 4 points par octet, x jusqu'à 319
            e.Data(new byte[] { 0x03, 0x07, 0x80, 0x00, 0x7F, 0x02, 0x00, 0x00 });// MODE 2 : 8 points par octet, x jusqu'à 639
            e.Label("rt_px_pens");
            e.Data(new byte[] { 0x00, 0xC0, 0x0C, 0xCC, 0x30, 0xF0, 0x3C, 0xFC, 0x03, 0xC3, 0x0F, 0xCF, 0x33, 0xF3, 0x3F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xF0, 0x0F, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF });
            e.Label("rt_px_from");
            e.Data(new byte[] { 0xFF, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x77, 0x33, 0x11, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x7F, 0x3F, 0x1F, 0x0F, 0x07, 0x03, 0x01 });
            e.Label("rt_px_head");
            e.Data(new byte[] { 0xAA, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88, 0xCC, 0xEE, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, 0xFF });
            e.Label("rt_px_shift");
            e.Reserve(1);
            e.Label("rt_px_pmask");
            e.Reserve(1);
            e.Label("rt_px_mask");
            e.Reserve(1);
            e.Label("rt_px_pat");
            e.Reserve(1);
            e.Label("rt_px_wmax_lo");
            e.Reserve(1);
            e.Label("rt_px_wmax_hi");
            e.Reserve(1);
            e.Label("rt_px_fromp");
            e.Reserve(2);
            e.Label("rt_px_headp");
            e.Reserve(2);
            e.Label("rt_px_row");
            e.Reserve(2);
            e.Label("rt_px_c1");
            e.Reserve(1);
            e.Label("rt_px_p1");
            e.Reserve(1);
            e.Label("rt_px_c2");
            e.Reserve(1);
            e.Label("rt_px_p2");
            e.Reserve(1);
            e.Label("rt_px_cnt");
            e.Reserve(1);
        }),

        // BOX : rectangle (D048, D051, D052). Les quatre valeurs sont d'abord rangées dans rt_box_x1, y1, x2, y2 par le programme,
        // ainsi que le stylo du contour (rt_box_pen), le stylo de l'intérieur (rt_box_fillpen) et l'indicateur rt_box_dofill.
        // Si l'intérieur est demandé, il est rempli en premier, ligne par ligne, puis le contour est tracé par-dessus : aucun point
        // n'est écrit deux fois. La ROM haute est coupée pendant le tracé puis remise comme avant.
        [BoxName] = new RuntimeModule(BoxName, [PixelName, Min16Name, Max16Name], e =>
        {
            // Module Box : rectangle (D048, D051). Les quatre coins sont rangés dans rt_box_x1, y1, x2, y2.
            // rt_box_pen = stylo du contour ; rt_box_dofill <> 0 : l'intérieur est d'abord rempli avec le stylo rt_box_fillpen.
            // Les parties hors de l'écran sont ignorées.
            e.Label("rt_box");
            e.CallAddress(0xB903);                        // KL U ROM DISABLE : sans cela, le Z80 lirait la ROM du BASIC à la place de l'écran
            e.PushAf();                                   // état précédent
            e.CallLabel("rt_box_draw");
            e.PopAf();
            e.JpAddress(0xB90C);                          // KL ROM RESTORE : remet la ROM comme avant, puis revient à l'appelant
            e.Label("rt_box_draw");
            e.LdHlMem("rt_box_x1");                       // on range les coins : x1 <= x2 et y1 <= y2
            e.LdDeMem("rt_box_x2");
            e.CallLabel("rt_min16");
            e.PushHl();
            e.LdHlMem("rt_box_x1");
            e.LdDeMem("rt_box_x2");
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_x2");
            e.PopHl();
            e.LdMemHl("rt_box_x1");
            e.LdHlMem("rt_box_y1");
            e.LdDeMem("rt_box_y2");
            e.CallLabel("rt_min16");
            e.PushHl();
            e.LdHlMem("rt_box_y1");
            e.LdDeMem("rt_box_y2");
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_y2");
            e.PopHl();
            e.LdMemHl("rt_box_y1");
            e.LdAMem("rt_box_dofill");                    // intérieur à remplir ?
            e.OrA();
            e.JrZLabel("rt_box_outline");
            e.LdAMem("rt_box_fillpen");
            e.CallLabel("rt_px_setup");
            e.CallLabel("rt_box_fillrun");
            e.Label("rt_box_outline");
            e.LdAMem("rt_box_pen");                       // puis le contour avec son propre stylo
            e.CallLabel("rt_px_setup");
            // ----- côtés du haut et du bas : x de max(x1, 0) à min(x2, xmax)
            e.LdHlMem("rt_box_x1");
            e.LdDeImm(0);
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cx1");
            e.LdHlMem("rt_box_x2");
            e.LdDeMem("rt_px_wmax_lo");
            e.CallLabel("rt_min16");
            e.LdMemHl("rt_box_cx2");
            e.LdHlMem("rt_box_cx1");                      // rien à tracer si cx1 > cx2
            e.LdDeMem("rt_box_cx2");
            e.CallLabel("rt_min16");
            e.LdDeMem("rt_box_cx1");
            e.OrA();
            e.SbcHlDe();
            e.JrNzLabel("rt_box_sides");
            e.LdHlImm(199);                               // la ligne y1 est-elle dans l'écran ?
            e.LdDeMem("rt_box_y1");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_box_bottom");
            e.LdHlMem("rt_box_cx2");
            e.LdBH();
            e.LdCL();
            e.LdDeMem("rt_box_cx1");
            e.LdHlMem("rt_box_y1");
            e.CallLabel("rt_px_hline");
            e.Label("rt_box_bottom");
            e.LdHlImm(199);                               // la ligne y2 est-elle dans l'écran ?
            e.LdDeMem("rt_box_y2");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_box_sides");
            e.LdHlMem("rt_box_cx2");
            e.LdBH();
            e.LdCL();
            e.LdDeMem("rt_box_cx1");
            e.LdHlMem("rt_box_y2");
            e.CallLabel("rt_px_hline");
            // ----- côtés de gauche et de droite : y de max(y1, 0) à min(y2, 199)
            e.Label("rt_box_sides");
            e.LdHlMem("rt_box_y1");
            e.LdDeImm(0);
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cy1");
            e.LdHlMem("rt_box_y2");
            e.LdDeImm(199);
            e.CallLabel("rt_min16");
            e.LdMemHl("rt_box_cy2");
            e.LdHlMem("rt_box_cy1");                      // rien à tracer si cy1 > cy2
            e.LdDeMem("rt_box_cy2");
            e.CallLabel("rt_min16");
            e.LdDeMem("rt_box_cy1");
            e.OrA();
            e.SbcHlDe();
            e.RetNz();
            e.LdHlMem("rt_px_wmax_lo");                   // la colonne x1 est-elle dans l'écran ?
            e.LdDeMem("rt_box_x1");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_box_right");
            e.LdHlMem("rt_box_cy2");
            e.LdBH();
            e.LdCL();
            e.LdDeMem("rt_box_x1");
            e.LdHlMem("rt_box_cy1");
            e.CallLabel("rt_px_vline");
            e.Label("rt_box_right");
            e.LdHlMem("rt_px_wmax_lo");                   // la colonne x2 est-elle dans l'écran ?
            e.LdDeMem("rt_box_x2");
            e.OrA();
            e.SbcHlDe();
            e.RetC();
            e.LdHlMem("rt_box_cy2");
            e.LdBH();
            e.LdCL();
            e.LdDeMem("rt_box_x2");
            e.LdHlMem("rt_box_cy1");
            e.JpLabel("rt_px_vline");
            // ----- intérieur : ce qui est strictement entre les côtés, découpé à l'écran. Une ligne horizontale par ligne.
            // Les coins sont d'abord ramenés dans [-1, xmax + 1] (puis [-1, 200]) : ni dépassement, ni perte.
            e.Label("rt_box_fillrun");
            e.LdHlMem("rt_px_wmax_lo");
            e.IncHl();
            e.ExDeHl();                                   // DE = xmax + 1
            e.LdHlMem("rt_box_x1");
            e.CallLabel("rt_min16");
            e.LdDeImm(65535);                             // -1
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cx1");
            e.LdHlMem("rt_px_wmax_lo");
            e.IncHl();
            e.ExDeHl();
            e.LdHlMem("rt_box_x2");
            e.CallLabel("rt_min16");
            e.LdDeImm(65535);
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cx2");
            e.LdHlMem("rt_box_cx2");                      // pas d'intérieur si x2 - x1 < 2
            e.LdDeMem("rt_box_cx1");
            e.OrA();
            e.SbcHlDe();
            e.LdDeImm(2);
            e.OrA();
            e.SbcHlDe();
            e.RetC();
            e.LdHlMem("rt_box_cx1");                      // x de x1 + 1 à x2 - 1
            e.IncHl();
            e.LdMemHl("rt_box_cx1");
            e.LdHlMem("rt_box_cx2");
            e.LdDeImm(65535);
            e.AddHlDe();
            e.LdMemHl("rt_box_cx2");
            e.LdDeImm(200);
            e.LdHlMem("rt_box_y1");
            e.CallLabel("rt_min16");
            e.LdDeImm(65535);
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cy1");
            e.LdDeImm(200);
            e.LdHlMem("rt_box_y2");
            e.CallLabel("rt_min16");
            e.LdDeImm(65535);
            e.CallLabel("rt_max16");
            e.LdMemHl("rt_box_cy2");
            e.LdHlMem("rt_box_cy2");                      // pas d'intérieur si y2 - y1 < 2
            e.LdDeMem("rt_box_cy1");
            e.OrA();
            e.SbcHlDe();
            e.LdDeImm(2);
            e.OrA();
            e.SbcHlDe();
            e.RetC();
            e.LdHlMem("rt_box_cy1");                      // y de y1 + 1 à y2 - 1
            e.IncHl();
            e.LdMemHl("rt_box_cy1");
            e.LdHlMem("rt_box_cy2");
            e.LdDeImm(65535);
            e.AddHlDe();
            e.LdMemHl("rt_box_cy2");
            e.Label("rt_box_fillrow");
            e.LdHlMem("rt_box_cx2");
            e.LdBH();
            e.LdCL();
            e.LdDeMem("rt_box_cx1");
            e.LdHlMem("rt_box_cy1");
            e.CallLabel("rt_px_hline");
            e.LdHlMem("rt_box_cy1");                      // ligne suivante, jusqu'à cy2 compris
            e.IncHl();
            e.LdMemHl("rt_box_cy1");
            e.LdDeMem("rt_box_cy1");
            e.LdHlMem("rt_box_cy2");
            e.OrA();
            e.SbcHlDe();
            e.RetC();
            e.JrLabel("rt_box_fillrow");
            // ---------- Données ----------
            e.Label("rt_box_x1");
            e.Reserve(2);
            e.Label("rt_box_y1");
            e.Reserve(2);
            e.Label("rt_box_x2");
            e.Reserve(2);
            e.Label("rt_box_y2");
            e.Reserve(2);
            e.Label("rt_box_cx1");
            e.Reserve(2);
            e.Label("rt_box_cx2");
            e.Reserve(2);
            e.Label("rt_box_cy1");
            e.Reserve(2);
            e.Label("rt_box_cy2");
            e.Reserve(2);
            e.Label("rt_box_pen");
            e.Reserve(1);
            e.Label("rt_box_fillpen");
            e.Reserve(1);
            e.Label("rt_box_dofill");
            e.Reserve(1);
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
