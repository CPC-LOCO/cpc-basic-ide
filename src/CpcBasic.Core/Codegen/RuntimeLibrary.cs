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

    /// <summary>KM TEST KEY : A = numéro de touche ; Z si elle n'est pas appuyée ; C = état de Shift (bit 5) et Ctrl (bit 7).</summary>
    public const int KmTestKey = 0xBB1E;

    /// <summary>KM GET JOYSTICK : H = état de la manette 0, L = état de la manette 1.</summary>
    public const int KmGetJoystick = 0xBB24;

    /// <summary>KM READ CHAR : lit une touche en attente sans attendre ; retenue à 1 si un caractère a été lu (dans A).</summary>
    public const int KmReadChar = 0xBB09;

    /// <summary>KM WAIT CHAR : attend une touche ; le caractère est rendu dans A.</summary>
    public const int KmWaitChar = 0xBB06;

    // Saisie de nombres (INPUT, D055)
    public const string InputName = "Input";
    public const string InputLineLabel = "rt_input_line";
    public const string InputMoreLabel = "rt_input_more";
    public const string InputNumLabel = "rt_input_num";
    public const string InputEndLabel = "rt_input_end";

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

    // Tableaux (DIM, lot C, D058)
    /// <summary>TXT GET CURSOR : H = colonne, L = ligne du curseur de texte.</summary>
    public const int TxtGetCursor = 0xBB78;

    /// <summary>Haut de la mémoire libre par défaut, non compris (HIMEM du BASIC avec la disquette = &amp;A67B).</summary>
    public const int DefaultHeapLimit = 0xA67C;

    // Erreurs pendant l'exécution (D058, D059) : message du Locomotive, « in », numéro de ligne, puis fin du programme
    public const string ErrorName = "RunError";
    public const string ErrorLabel = "rt_error";                // HL = adresse du message (terminé par 0)
    public const string ErrorLineLabel = "rt_err_line";         // numéro de la ligne en cours, rangé par le programme

    // DATA, READ, RESTORE (D059)
    public const string DataName = "Data";
    public const string ReadLabel = "rt_read";                  // HL = nombre suivant des DATA
    public const string DataPointerLabel = "rt_data_ptr";       // prochain nombre à lire
    public const string DataTableLabel = "data_table";          // placée par le générateur : les nombres des DATA
    public const string DataEndLabel = "data_end";              // placée par le générateur : fin des nombres

    // Puissance entière (^, D060)
    public const string Pow16Name = "Pow16";
    public const string Pow16Label = "rt_pow16";

    public const string ArrayName = "Array";
    public const string ArrayHeapLabel = "rt_arr_heap";         // début de la mémoire libre (les tableaux sont pris ici)
    public const string ArrayHeapLimitLabel = "rt_arr_limit";   // fin de la mémoire libre (non comprise), changée par MEMORY
    public const string ArrayHeapStartLabel = "rt_heap_start";  // placée par le générateur après les variables
    public const string ArrayDimSizeLabel = "rt_arr_dimsize";
    public const string ArrayMulLabel = "rt_arr_mul";
    public const string ArrayDimLabel = "rt_arr_dim";
    public const string ArrayElem1Label = "rt_arr_elem1";
    public const string ArrayElemLabel = "rt_arr_elem";
    public const string ArrayMemoryLabel = "rt_arr_memory";
    const string ArrayAllocLabel = "rt_arr_alloc";
    const string ArrayAutoDimLabel = "rt_arr_autodim";
    const string ErrSubscript = "rt_arr_err_sub";
    const string ErrMemory = "rt_arr_err_mem";
    const string ErrRedim = "rt_arr_err_redim";

    static byte[] Ascii(string text) => [.. System.Text.Encoding.ASCII.GetBytes(text), 0];

    static readonly Dictionary<string, RuntimeModule> Modules = new()
    {
        // Tableaux d'entiers (DIM, lot C, D058). Un tableau a un descripteur dans les variables :
        //   2 octets = adresse de ses cases (0 = pas encore créé), puis 2 octets par dimension = nombre de cases.
        // Les cases (2 octets chacune, à 0 au départ) sont prises dans la mémoire libre, après le programme,
        // jusqu'à rt_arr_limit (&A67C, ou l'adresse donnée par MEMORY + 1).
        //  rt_arr_dimsize : HL = plus grande case écrite dans DIM -> HL = nombre de cases (erreur si négatif).
        //  rt_arr_mul     : HL = HL * DE sans signe ; « Memory full » si le résultat dépasse 65535. Garde BC.
        //  rt_arr_dim     : DIM. HL = nombre total de cases, DE = descripteur (tailles déjà rangées).
        //  rt_arr_elem1   : tableau à une dimension. HL = numéro de case, DE = descripteur -> HL = adresse de la case.
        //  rt_arr_elem    : plusieurs dimensions. Numéros empilés (le dernier en haut), DE = descripteur, B = dimensions.
        //  rt_arr_memory  : MEMORY. HL = adresse : la mémoire libre s'arrête là.
        // Un tableau utilisé sans DIM est créé avec les cases 0 à 10 dans chaque dimension, comme le Locomotive.
        // Les erreurs affichent le message du Locomotive (« Subscript out of range in 30 ») par le module RunError.
        // Erreur pendant l'exécution. HL = message. Affiche (à la ligne si le curseur n'est pas au début)
        // « message in N », N étant rt_err_line, attend une touche, puis redémarre la machine comme la fin du programme.
        [ErrorName] = new RuntimeModule(ErrorName, [PrintStringName], e =>
        {
            e.Label(ErrorLabel);
            e.PushHl();
            e.CallAddress(TxtGetCursor);             // pas en début de ligne : on passe à la ligne
            e.LdAH();
            e.CpImm(1);
            e.JrZLabel("rt_err_col1");
            e.LdHlLabel("rt_err_crlf");
            e.CallLabel(PrintStringLabel);
            e.Label("rt_err_col1");
            e.PopHl();
            e.CallLabel(PrintStringLabel);
            e.LdHlLabel("rt_err_in");
            e.CallLabel(PrintStringLabel);
            e.LdHlMem(ErrorLineLabel);
            e.CallLabel("rt_err_dec");
            e.LdHlLabel("rt_err_crlf");
            e.CallLabel(PrintStringLabel);
            e.CallAddress(KmWaitKey);
            e.JpAddress(0);

            // Affiche HL de 0 à 65535, sans espace
            e.Label("rt_err_dec");
            e.LdEImm(0);                             // E = 1 dès qu'un chiffre est affiché
            foreach (int power in new[] { 10000, 1000, 100, 10 })
            {
                e.LdBcImm(-power & 0xFFFF);
                e.CallLabel("rt_err_digit");
            }
            e.LdAL();
            e.AddAImm('0');
            e.CallAddress(TxtOutput);
            e.Ret();
            e.Label("rt_err_digit");
            e.LdAImm('0' - 1);
            e.Label("rt_err_digit_loop");
            e.IncA();
            e.AddHlBc();
            e.JrCLabel("rt_err_digit_loop");
            e.SbcHlBc();                             // retenue à 0 : on rajoute ce qui a été retiré de trop
            e.CpImm('0');
            e.JrNzLabel("rt_err_digit_show");
            e.LdDA();                                // un 0 avant le premier chiffre n'est pas affiché
            e.LdAE();
            e.OrA();
            e.LdAD();
            e.RetZ();
            e.Label("rt_err_digit_show");
            e.LdEImm(1);
            e.CallAddress(TxtOutput);
            e.Ret();

            e.Label(ErrorLineLabel);
            e.Reserve(2);
            e.Label("rt_err_in");
            e.Data(Ascii(" in "));
            e.Label("rt_err_crlf");
            e.Data(Ascii("\r\n"));
        }),

        // READ : HL = nombre suivant des DATA ; « DATA exhausted » s'il n'y en a plus (D059).
        // Le générateur place les nombres entre data_table et data_end ; RESTORE change rt_data_ptr.
        [DataName] = new RuntimeModule(DataName, [ErrorName], e =>
        {
            e.Label(ReadLabel);
            e.LdHlMem(DataPointerLabel);
            e.LdDeLabel(DataEndLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrNcLabel("rt_read_none");             // plus rien à lire
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.IncHl();
            e.LdMemHl(DataPointerLabel);
            e.ExDeHl();
            e.Ret();
            e.Label("rt_read_none");
            e.LdHlLabel("rt_read_msg");
            e.JpLabel(ErrorLabel);

            e.Label(DataPointerLabel);
            e.Word(DataTableLabel);                  // au départ : le premier nombre
            e.Label("rt_read_msg");
            e.Data(Ascii("DATA exhausted"));
        }),

        // Puissance entière : HL = HL ^ DE (D060). Méthode rapide : on élève au carré et on multiplie quand le bit
        // de l'exposant est à 1 (au plus 15 tours). Résultat sur 16 bits (comme * : pas d'erreur s'il déborde).
        // Exposant négatif : 1 pour 1, 1 ou -1 pour -1 (selon que l'exposant est pair ou impair), 0 sinon
        // (le vrai résultat est un nombre à virgule : il viendra avec les réels, lot F). 0 ^ 0 = 1, comme le Locomotive.
        [Pow16Name] = new RuntimeModule(Pow16Name, [Mul16Name], e =>
        {
            e.Label(Pow16Label);
            e.Bit7D();
            e.JrNzLabel("rt_pow_neg");
            e.LdMemHl("rt_pow_base");
            e.ExDeHl();
            e.LdMemHl("rt_pow_exp");
            e.LdHlImm(1);
            e.LdMemHl("rt_pow_res");
            e.Label("rt_pow_loop");
            e.LdHlMem("rt_pow_exp");
            e.LdAH();
            e.OrL();
            e.JrZLabel("rt_pow_done");
            e.SrlH();                                // bit de poids faible de l'exposant -> retenue
            e.RrL();
            e.LdMemHl("rt_pow_exp");
            e.JrNcLabel("rt_pow_square");
            e.LdHlMem("rt_pow_res");
            e.LdDeMem("rt_pow_base");
            e.CallLabel(Mul16Label);
            e.LdMemHl("rt_pow_res");
            e.Label("rt_pow_square");
            e.LdHlMem("rt_pow_base");
            e.LdDH();
            e.LdEL();
            e.CallLabel(Mul16Label);
            e.LdMemHl("rt_pow_base");
            e.JrLabel("rt_pow_loop");
            e.Label("rt_pow_done");
            e.LdHlMem("rt_pow_res");
            e.Ret();

            e.Label("rt_pow_neg");                   // exposant négatif
            e.LdAH();
            e.OrA();
            e.JrNzLabel("rt_pow_minus1");
            e.LdAL();
            e.CpImm(1);
            e.RetZ();                                // 1 ^ n = 1
            e.LdHlImm(0);
            e.Ret();
            e.Label("rt_pow_minus1");                // HL = -1 (&FFFF) ?
            e.LdAH();
            e.IncA();
            e.JrNzLabel("rt_pow_zero");
            e.LdAL();
            e.IncA();
            e.JrNzLabel("rt_pow_zero");
            e.Bit0E();
            e.RetNz();                               // exposant impair : -1
            e.LdHlImm(1);
            e.Ret();
            e.Label("rt_pow_zero");
            e.LdHlImm(0);
            e.Ret();

            e.Label("rt_pow_base");
            e.Reserve(2);
            e.Label("rt_pow_exp");
            e.Reserve(2);
            e.Label("rt_pow_res");
            e.Reserve(2);
        }),

        [ArrayName] = new RuntimeModule(ArrayName, [ErrorName], e =>
        {
            e.Label(ArrayDimSizeLabel);
            e.IncHl();                               // nombre de cases = plus grande case + 1
            e.LdAH();
            e.OrL();
            e.JpZLabel(ErrSubscript);                // DIM A(-1)
            e.Bit7H();
            e.RetZ();                                // 1 à 32767 : bon
            e.JpLabel(ErrSubscript);

            e.Label(ArrayMulLabel);
            e.PushBc();
            e.LdBH();                                // BC = multiplicande
            e.LdCL();
            e.LdHlImm(0);
            e.LdAImm(16);
            e.Label("rt_arr_mul_loop");
            e.AddHlHl();
            e.JpCLabel(ErrMemory);
            e.SlaE();                                // bit de poids fort du multiplicateur -> retenue
            e.RlD();
            e.JrNcLabel("rt_arr_mul_skip");
            e.AddHlBc();
            e.JpCLabel(ErrMemory);
            e.Label("rt_arr_mul_skip");
            e.DecA();
            e.JrNzLabel("rt_arr_mul_loop");
            e.PopBc();
            e.Ret();

            e.Label(ArrayDimLabel);
            e.LdADe();                               // le tableau existe déjà ?
            e.LdBA();
            e.IncDe();
            e.LdADe();
            e.DecDe();
            e.OrB();
            e.JpNzLabel(ErrRedim);

            // Réserve HL cases pour le descripteur DE, et les met à 0
            e.Label(ArrayAllocLabel);
            e.AddHlHl();                             // 2 octets par case
            e.JpCLabel(ErrMemory);
            e.LdBH();                                // BC = octets
            e.LdCL();
            e.LdHlMem(ArrayHeapLabel);
            e.PushHl();                              // début des cases
            e.AddHlBc();
            e.JpCLabel(ErrMemory);
            e.PushDe();
            e.LdDeMem(ArrayHeapLimitLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();                             // retenue : fin < limite ; Z : fin = limite
            e.JrCLabel("rt_arr_alloc_ok");
            e.JpNzLabel(ErrMemory);
            e.Label("rt_arr_alloc_ok");
            e.LdMemHl(ArrayHeapLabel);
            e.PopHl();                               // HL = descripteur
            e.PopDe();                               // DE = début des cases
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.ExDeHl();
            e.StoreHlImm(0);                         // cases à 0
            e.DecBc();
            e.LdDH();
            e.LdEL();
            e.IncDe();
            e.Ldir();
            e.Ret();

            // Tableau utilisé sans DIM : HL = descripteur, B = dimensions ; cases 0 à 10. Garde BC, DE, HL.
            e.Label(ArrayAutoDimLabel);
            e.PushHl();
            e.PushDe();
            e.PushBc();
            e.PushHl();
            e.IncHl();
            e.IncHl();
            e.LdDeImm(1);                            // DE = nombre total de cases
            e.Label("rt_arr_auto_loop");
            e.StoreHlImm(11);
            e.IncHl();
            e.StoreHlImm(0);
            e.IncHl();
            e.PushHl();
            e.ExDeHl();
            e.LdDeImm(11);
            e.CallLabel(ArrayMulLabel);
            e.ExDeHl();
            e.PopHl();
            e.DjnzLabel("rt_arr_auto_loop");
            e.ExDeHl();
            e.PopDe();
            e.CallLabel(ArrayAllocLabel);
            e.PopBc();
            e.PopDe();
            e.PopHl();
            e.Ret();

            e.Label(ArrayElem1Label);
            e.ExDeHl();                              // HL = descripteur, DE = numéro
            e.LdAHl();
            e.IncHl();
            e.OrMemHl();
            e.JrNzLabel("rt_arr_e1_ok");
            e.DecHl();
            e.LdBImm(1);
            e.CallLabel(ArrayAutoDimLabel);
            e.IncHl();
            e.Label("rt_arr_e1_ok");
            e.IncHl();
            e.LoadCFromHl();
            e.IncHl();
            e.LoadBFromHl();                         // BC = nombre de cases
            e.ExDeHl();                              // HL = numéro, DE = descripteur + 3
            e.OrA();
            e.SbcHlBc();
            e.AddHlBc();                             // sans signe : un numéro négatif est très grand
            e.JpNcLabel(ErrSubscript);
            e.AddHlHl();                             // écart en octets
            e.ExDeHl();
            e.DecHl();
            e.DecHl();
            e.LdAHl();
            e.DecHl();
            e.LoadLFromHl();
            e.LdHA();                                // HL = début des cases
            e.AddHlDe();
            e.Ret();

            e.Label(ArrayElemLabel);
            e.PopHl();
            e.LdMemHl("rt_arr_ret");
            e.ExDeHl();
            e.LdMemHl("rt_arr_desc");
            e.LdAHl();
            e.IncHl();
            e.OrMemHl();
            e.DecHl();
            e.CallZLabel(ArrayAutoDimLabel);
            e.LdAB();                                // taille de la dernière dimension : descripteur + 2 * B
            e.AddAA();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();
            e.LdMemHl("rt_arr_size");
            e.LdHlImm(0);
            e.LdMemHl("rt_arr_off");
            e.LdHlImm(1);
            e.LdMemHl("rt_arr_stride");
            e.Label("rt_arr_e_loop");                // de la dernière dimension à la première
            e.PopDe();                               // DE = numéro
            e.LdHlMem("rt_arr_size");
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();                                // HL = nombre de cases de cette dimension
            e.PushHl();
            e.ExDeHl();
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JpNcLabel(ErrSubscript);
            e.LdDeMem("rt_arr_stride");
            e.CallLabel(ArrayMulLabel);              // numéro * pas
            e.LdDeMem("rt_arr_off");
            e.AddHlDe();
            e.LdMemHl("rt_arr_off");
            e.PopDe();
            e.LdHlMem("rt_arr_stride");
            e.CallLabel(ArrayMulLabel);              // pas suivant = pas * nombre de cases
            e.LdMemHl("rt_arr_stride");
            e.LdHlMem("rt_arr_size");
            e.DecHl();
            e.DecHl();
            e.LdMemHl("rt_arr_size");
            e.DjnzLabel("rt_arr_e_loop");
            e.LdHlMem("rt_arr_off");
            e.AddHlHl();
            e.ExDeHl();
            e.LdHlMem("rt_arr_desc");
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.AddHlDe();
            e.LdDeMem("rt_arr_ret");
            e.PushDe();
            e.Ret();

            e.Label(ArrayMemoryLabel);
            e.IncHl();                               // limite = adresse + 1
            e.LdAH();
            e.OrL();
            e.JrZLabel("rt_arr_mem_default");        // MEMORY 65535
            e.LdDeImm(DefaultHeapLimit);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrCLabel("rt_arr_mem_set");
            e.Label("rt_arr_mem_default");
            e.LdHlImm(DefaultHeapLimit);
            e.Label("rt_arr_mem_set");
            e.LdDeMem(ArrayHeapLabel);               // les tableaux déjà créés doivent rester dessous
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JpCLabel(ErrMemory);
            e.LdMemHl(ArrayHeapLimitLabel);
            e.Ret();

            // Erreurs : message du Locomotive, puis module RunError
            e.Label(ErrSubscript);
            e.LdHlLabel("rt_arr_msg_sub");
            e.JpLabel(ErrorLabel);
            e.Label(ErrMemory);
            e.LdHlLabel("rt_arr_msg_mem");
            e.JpLabel(ErrorLabel);
            e.Label(ErrRedim);
            e.LdHlLabel("rt_arr_msg_redim");
            e.JpLabel(ErrorLabel);

            e.Label(ArrayHeapLabel);
            e.Reserve(2);
            e.Label(ArrayHeapLimitLabel);
            e.Bytes((byte)(DefaultHeapLimit & 0xFF), (byte)(DefaultHeapLimit >> 8));
            foreach (string scratch in new[] { "rt_arr_ret", "rt_arr_desc", "rt_arr_off", "rt_arr_stride", "rt_arr_size" })
            {
                e.Label(scratch);
                e.Reserve(2);
            }
            e.Label("rt_arr_msg_sub");
            e.Data(Ascii("Subscript out of range"));
            e.Label("rt_arr_msg_mem");
            e.Data(Ascii("Memory full"));
            e.Label("rt_arr_msg_redim");
            e.Data(Ascii("Array already dimensioned"));
        }),

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

        // INPUT : saisie d'une ligne au clavier puis lecture de nombres entiers dans cette ligne (D055).
        //  rt_input_line : lit une ligne (Entrée la termine, Suppr efface, 80 caractères au plus), avec écho.
        //  rt_input_more : si la ligne est épuisée, affiche « ?? » et lit une nouvelle ligne (INPUT A, B avec un seul nombre).
        //  rt_input_num  : lit le nombre suivant : HL = valeur, retenue à 0 ; retenue à 1 si ce qui est tapé n'est pas un
        //                  nombre entier de -32768 à 32767 (ou n'est pas suivi d'une virgule ou de la fin de la ligne).
        //  rt_input_end  : affiche « ?Extra ignored » s'il reste du texte non lu.
        [InputName] = new RuntimeModule(InputName, [], e =>
        {
            e.Label(InputLineLabel);
            e.XorA();
            e.LdMemA("rt_inp_len");
            e.Label("rt_in_key");
            e.CallAddress(KmWaitChar);
            e.CpImm(13);
            e.JrZLabel("rt_in_done");
            e.CpImm(127);
            e.JrZLabel("rt_in_back");
            e.CpImm(32);
            e.JrCLabel("rt_in_key");             // caractère de commande : ignoré
            e.CpImm(128);
            e.JrNcLabel("rt_in_key");            // caractère étendu : ignoré
            e.LdCA();
            e.LdAMem("rt_inp_len");
            e.CpImm(80);
            e.JrNcLabel("rt_in_key");            // ligne pleine
            e.LdEA();
            e.LdDZero();
            e.LdHlLabel("rt_inp_buf");
            e.AddHlDe();
            e.LdAC();
            e.LdHlA();
            e.LdAMem("rt_inp_len");
            e.IncA();
            e.LdMemA("rt_inp_len");
            e.LdAC();
            e.CallAddress(TxtOutput);
            e.JrLabel("rt_in_key");

            e.Label("rt_in_back");
            e.LdAMem("rt_inp_len");
            e.OrA();
            e.JrZLabel("rt_in_key");             // rien à effacer
            e.DecA();
            e.LdMemA("rt_inp_len");
            e.LdAImm(127);
            e.CallAddress(TxtOutput);
            e.JrLabel("rt_in_key");

            e.Label("rt_in_done");
            e.LdAMem("rt_inp_len");
            e.LdEA();
            e.LdDZero();
            e.LdHlLabel("rt_inp_buf");
            e.AddHlDe();
            e.XorA();
            e.LdHlA();                           // fin du texte : un octet à 0
            e.LdHlLabel("rt_inp_buf");
            e.LdMemHl("rt_inp_ptr");
            e.Ret();

            e.Label(InputMoreLabel);
            e.LdHlMem("rt_inp_ptr");
            e.LdAHl();
            e.OrA();
            e.RetNz();                           // il reste du texte
            e.LdAImm('?');
            e.CallAddress(TxtOutput);
            e.LdAImm('?');
            e.CallAddress(TxtOutput);
            e.LdAImm(' ');
            e.CallAddress(TxtOutput);
            e.CallLabel(InputLineLabel);
            e.LdAImm(13);
            e.CallAddress(TxtOutput);
            e.LdAImm(10);
            e.CallAddress(TxtOutput);
            e.Ret();

            e.Label(InputNumLabel);
            e.LdHlMem("rt_inp_ptr");
            e.Label("rt_in_sp1");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_in_sign");
            e.IncHl();
            e.JrLabel("rt_in_sp1");
            e.Label("rt_in_sign");
            e.LdBImm(0);                         // B = 1 si le nombre est négatif
            e.CpImm('-');
            e.JrNzLabel("rt_in_plus");
            e.LdBImm(1);
            e.IncHl();
            e.JrLabel("rt_in_first");
            e.Label("rt_in_plus");
            e.CpImm('+');
            e.JrNzLabel("rt_in_first");
            e.IncHl();
            e.Label("rt_in_first");
            e.PushHl();
            e.LdHlImm(0);
            e.LdMemHl("rt_inp_acc");
            e.PopHl();
            e.LdAHl();
            e.AddAImm(-'0' & 0xFF);
            e.CpImm(10);
            e.JrNcLabel("rt_in_err");            // il faut au moins un chiffre
            e.Label("rt_in_digit");              // A = chiffre, HL = sa place dans le texte
            e.LdCA();
            e.IncHl();
            e.LdMemHl("rt_inp_ptr");
            e.LdHlMem("rt_inp_acc");
            e.LdDeImm(3277);
            e.OrA();
            e.SbcHlDe();
            e.JrNcLabel("rt_in_err");            // le nombre dépasserait 32767 une fois multiplié par 10
            e.AddHlDe();
            e.AddHlHl();                         // x2
            e.PushHl();
            e.AddHlHl();                         // x4
            e.AddHlHl();                         // x8
            e.PopDe();
            e.AddHlDe();                         // x10
            e.LdAC();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();                         // + chiffre
            e.LdMemHl("rt_inp_acc");
            e.LdHlMem("rt_inp_ptr");
            e.LdAHl();
            e.AddAImm(-'0' & 0xFF);
            e.CpImm(10);
            e.JrCLabel("rt_in_digit");

            e.Label("rt_in_sp2");                // après les chiffres : espaces, puis virgule ou fin
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_in_after");
            e.IncHl();
            e.JrLabel("rt_in_sp2");
            e.Label("rt_in_after");
            e.OrA();
            e.JrZLabel("rt_in_end");
            e.CpImm(',');
            e.JrNzLabel("rt_in_err");
            e.IncHl();
            e.Label("rt_in_end");
            e.LdMemHl("rt_inp_ptr");
            e.LdHlMem("rt_inp_acc");
            e.LdAB();
            e.OrA();
            e.JrNzLabel("rt_in_neg");
            e.Bit7H();
            e.JrNzLabel("rt_in_err");            // 32768 ou plus : trop grand
            e.OrA();
            e.Ret();
            e.Label("rt_in_neg");
            e.Bit7H();
            e.JrZLabel("rt_in_negate");
            e.LdAH();                            // seul -32768 (&8000) est permis ici
            e.CpImm(0x80);
            e.JrNzLabel("rt_in_err");
            e.LdAL();
            e.OrA();
            e.JrNzLabel("rt_in_err");
            e.Ret();                             // la retenue est déjà à 0 (OR A)
            e.Label("rt_in_negate");
            e.XorA();                            // HL = 0 - HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.OrA();
            e.Ret();
            e.Label("rt_in_err");
            e.Scf();
            e.Ret();

            e.Label(InputEndLabel);
            e.LdHlMem("rt_inp_ptr");
            e.LdAHl();
            e.OrA();
            e.RetZ();
            e.LdHlLabel("rt_inp_extra");
            e.Label("rt_in_extra_loop");
            e.LdAHl();
            e.OrA();
            e.RetZ();
            e.PushHl();
            e.CallAddress(TxtOutput);
            e.PopHl();
            e.IncHl();
            e.JrLabel("rt_in_extra_loop");
            e.Label("rt_inp_extra");
            e.Data(System.Text.Encoding.ASCII.GetBytes("?Extra ignored\r\n\0"));
            e.Label("rt_inp_len");
            e.Reserve(1);
            e.Label("rt_inp_ptr");
            e.Reserve(2);
            e.Label("rt_inp_acc");
            e.Reserve(2);
            e.Label("rt_inp_buf");
            e.Reserve(82);
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
