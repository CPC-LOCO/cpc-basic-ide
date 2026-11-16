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
    public const int TxtSetCursorAddress = 0xBB75;   // TXT SET CURSOR : H = colonne, L = ligne

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
    public const string ErrorTrapLabel = "rt_err_trap";         // 0, ou adresse qui intercepte les erreurs (ON ERROR, D080)
    public const string ErrorSoftLabel = "rt_err_soft";         // erreur qui laisse continuer (Overflow, Division by zero)
    public const string ErrorShowLabel = "rt_err_show";         // affiche « message in N » sans interception

    // RETURN sans GOSUB, STOP (lot I2b, D081)
    public const string GosubName = "Gosub";
    public const string GosubCountLabel = "rt_gosub_n";         // octet : GOSUB en cours (non revenus)
    public const string ReturnLabel = "rt_return";              // RETURN : « Unexpected RETURN » s'il n'y a pas de GOSUB en cours
    public const string BreakMessageLabel = "rt_err_break";     // « Break » (STOP : « Break in 20 »)

    // ON ERROR GOTO, RESUME, ERROR, ERR, ERL (lot I2, D080)
    public const string OnErrorName = "OnError";
    public const string ErrorCodesName = "ErrorCodes";
    public const string OnErrorTrapLabel = "rt_oe_trap";
    public const string OnErrorHandlerLabel = "rt_oe_handler";  // adresse de la ligne du ON ERROR GOTO
    public const string OnErrorSpLabel = "rt_oe_sp";            // pile au début de l'instruction en cours
    public const string OnErrorRecordLabel = "rt_oe_rec";       // fiche de l'instruction en cours : ligne, début, suivante
    public const string ErrLabel = "rt_oe_err";                 // ERR
    public const string ErlLabel = "rt_oe_erl";                 // ERL
    public const string RaiseLabel = "rt_raise";                // ERROR : HL = numéro

    // Fenêtres (lot H1, D072)
    /// <summary>TXT STR SELECT : A = fenêtre (flux 0 à 7) -> A = fenêtre choisie avant.</summary>
    public const int TxtStrSelect = 0xBBB4;
    public const string StreamName = "Stream";
    public const string StreamLabel = "rt_stream";              // HL = numéro calculé : vérifié (0 à 7), puis choisi

    // Mise en page de PRINT (lot H2, D073)
    public const int TxtGetWindow = 0xBB69;                     // TXT GET WINDOW : H = gauche, D = droite (à partir de 0)
    public const string PrintFormatName = "PrintFormat";
    public const int TxtValidate = 0xBB87;                      // TXT VALIDATE : H, L -> position où s'écrira le caractère suivant
    public const string PrintUsingName = "PrintUsing";          // PRINT USING, nombres (D074)
    public const string UsingNumLabel = "rt_using_num";         // HL = nombre (5 octets), B = largeur, C = places avant le point,
                                                                // D = chiffres après le point, E = indicateurs (UsingFlags)
    public const string PrintUsingStrName = "PrintUsingStr";    // PRINT USING, textes (D074)
    public const string UsingStrLabel = "rt_using_str";         // HL = fiche, A = largeur (0 : tout le texte)
    // Minuteries AFTER, EVERY (lot I1, D079)
    public const string EventsName = "Events";
    public const string EventsSoundName = "EventsSound";        // Events avec ON SQ GOSUB (D082)
    public const string SoundEventSetLabel = "rt_sq_set";       // ON SQ : HL = canal, DE = ligne
    // Fichiers de données (lot J1, D084)
    public const string FileCommonName = "FileCommon";
    public const string FileOutName = "FileOut";
    public const string FileInName = "FileIn";
    public const string FileBinaryName = "FileBinary";          // CAT, SAVE, LOAD, SPEED WRITE (D085)
    public const string FileOutOnLabel = "rt_fo_on";            // début de PRINT #9 : les caractères vont dans le fichier
    public const string FileOutOffLabel = "rt_fo_off";          // fin de PRINT #9 : retour à l'écran
    public const int FileInBuffer = 0x3000;                     // zones de 2 Ko demandées par le firmware, sous le programme
    public const int FileOutBuffer = 0x3800;
    public const string EventsStopLabel = "rt_ev_stop";         // RUN "nom" : le compteur et les sons sont arrêtés (D086)
    public const int RunStub = 0x3800;                          // RUN "nom" : petit chargeur, hors du programme (D086)
    public const int RunName = 0x3880;
    public const string EventsBreakName = "EventsBreak";        // Events avec la touche ESC (D083)
    public const string EventsSoundBreakName = "EventsSoundBreak";
    public const string BreakModeLabel = "rt_brk_mode";         // 0 : ESC ignorée, 1 : pause puis Break, 2 : pause puis appel
    public const string BreakTargetLabel = "rt_brk_t";          // ligne de ON BREAK GOSUB

    /// <summary>Le module des événements qui convient : minuteries, avec ou sans ON SQ, avec ou sans la touche ESC.</summary>
    public static string EventsModuleName(bool sound, bool brk) =>
        (sound, brk) switch
        {
            (false, false) => EventsName,
            (true, false) => EventsSoundName,
            (false, true) => EventsBreakName,
            _ => EventsSoundBreakName
        };
    public const string EventsInitLabel = "rt_ev_init";         // au départ : branche le compteur du firmware (1/50 s)
    public const string EventsPendingLabel = "rt_ev_pending";   // octet : 1 si une minuterie est arrivée au bout
    public const string EventsRunLabel = "rt_ev_run";           // appelé entre deux instructions si rt_ev_pending
    public const string EventsSetLabel = "rt_ev_set";           // A = minuterie ; durée, recharge, cible dans rt_ev_tc, tr, tt
    public const string EventsRemainLabel = "rt_ev_remain";     // HL = minuterie -> HL = temps restant (minuterie arrêtée)
    public const string EventsDiLabel = "rt_ev_di";             // octet : 1 après DI

    // Caractères (lot H4, D075)
    public const string SymbolName = "Symbol";
    public const string SymbolLabel = "rt_symbol";              // A = caractère ; lignes dans rt_sym_r0 à rt_sym_r7
    public const string SymbolAfterName = "SymbolAfter";
    public const string SymbolAfterLabel = "rt_symafter";       // HL = premier caractère redessinable (0 à 256)
    public const string SymbolTableLabel = "rt_sym_table";      // placée par le générateur : (256 - premier) x 8 octets
    public const string PrintFitName = "PrintFit";              // un élément qui ne tient pas dans la fin de la ligne passe à la ligne
    public const string PrintFitLabel = "rt_pf_fit";            // A = longueur de l'élément
    public const string PrintFitStrLabel = "rt_pf_fit_str";     // HL = fiche du texte (gardée)
    public const string PrintFitIntLabel = "rt_pf_fit_int";     // HL = entier (gardé)
    public const string ZoneLabel = "rt_zone";                  // « , » dans PRINT
    public const string ZoneSetLabel = "rt_zone_set";           // ZONE : HL = largeur (1 à 255)
    public const string TabLabel = "rt_tab";                    // TAB : HL = colonne
    public const string SpcLabel = "rt_spc";                    // SPC : HL = nombre d'espaces

    // DATA, READ, RESTORE (D059)
    public const string DataName = "Data";
    public const string DataStringName = "DataString";         // READ A$ (D064)
    public const string ReadStringLabel = "rt_read_str";
    public const string ReadLabel = "rt_read";                  // HL = nombre suivant des DATA
    public const string DataPointerLabel = "rt_data_ptr";       // prochain nombre à lire
    public const string DataTableLabel = "data_table";          // placée par le générateur : les nombres des DATA
    public const string DataEndLabel = "data_end";              // placée par le générateur : fin des nombres

    // Puissance entière (^, D060)
    public const string Pow16Name = "Pow16";
    public const string RealOpsName = "RealOps";       // nombres à virgule : conversions, opérations (D068)
    public const string RealPrintName = "RealPrint";   // nombres à virgule : affichage comme PRINT (D068)
    public const string InputRealName = "InputReal";   // nombres à virgule : INPUT A! (D068)
    public const string RealRndName = "RealRnd";       // RND (D069)
    public const string RealParseName = "RealParse";   // lecture d'un nombre écrit (INPUT A!, VAL) (D070)
    public const string RealValName = "RealVal";       // VAL à virgule (D070)
    public const string RealStrName = "RealStr";       // STR$ d'un nombre à virgule (D070)
    public const string DataRealName = "DataReal";     // nombres à virgule dans DATA, READ A! (D070)
    public const string DataNoRealName = "DataNoReal"; // programme sans nombre à virgule : rien à lire de tel (D070)
    public const string MathsAllName = "FMathsAll";
    public const string Maths464Name = "FMaths464";
    public const string Maths6128Name = "FMaths6128";
    /// <summary>À appeler une fois au départ d'un programme qui calcule avec des nombres à virgule.</summary>
    public const string MathsInitLabel = "rt_fm_init";

    /// <summary>Le module de calcul à virgule qui convient à la machine choisie (F7).</summary>
    public static string MathsModuleFor(TargetMachine target) => target switch
    {
        TargetMachine.Cpc464 => Maths464Name,
        TargetMachine.Cpc664And6128 => Maths6128Name,
        _ => MathsAllName
    };

    static void EmitMathsTable(Z80Emitter e, bool rom464)
    {
        e.Label("rt_fm_table");
        foreach (FirmwareMathsRoutine routine in FirmwareMaths.All)
        {
            int address = rom464 ? routine.Address464 : routine.Address6128;
            e.Label(routine.Label);
            e.Bytes(0xC3, (byte)(address & 0xFF), (byte)(address >> 8));
        }
    }

    static void EmitMathsFixed(Z80Emitter e, bool rom464)
    {
        EmitMathsTable(e, rom464);
        e.Label(MathsInitLabel);
        e.XorA();
        e.JpLabel(FirmwareMaths.AngleMode.Label);   // angles en radians, comme au démarrage du BASIC
    }
    public const string Pow16Label = "rt_pow16";

    public const string HeapName = "Heap";
    public const string StringPointerLabel = "rt_str_ptr";      // bas des textes rangés (ils descendent depuis la limite)

    // Textes (lot E, D062)
    public const string StringsName = "Strings";
    public const string StrResetLabel = "rt_str_reset";      // libère les textes temporaires de l'instruction précédente
    public const string StrAllocLabel = "rt_str_alloc";
    public const string StrGcLabel = "rt_str_gc";
    public const string StrConcatLabel = "rt_str_concat";
    public const string StrAssignLabel = "rt_str_assign";
    public const string StrPrintLabel = "rt_str_print";
    public const string StrPrintNoSpaceLabel = "rt_str_print_ns";   // WRITE (D073) : sans l'espace du début
    public const string StrCmpLabel = "rt_str_cmp";
    public const string StrFunctionsName = "StrFunctions";    // LEFT$, MID$, CHR$, VAL... (D063)
    public const string InputStringName = "InputString";
    public const string InputStrLabel = "rt_input_str";

    public const string ArrayName = "Array";
    public const string ArrayHeapLabel = "rt_arr_heap";         // début de la mémoire libre (les tableaux sont pris ici)
    public const string ArrayHeapLimitLabel = "rt_arr_limit";   // fin de la mémoire libre (non comprise), changée par MEMORY
    public const string ArrayHeapStartLabel = "rt_heap_start";  // placée par le générateur après les variables
    public const string ArrayDimSizeLabel = "rt_arr_dimsize";
    public const string ArrayMulLabel = "rt_arr_mul";
    public const string ArrayDimLabel = "rt_arr_dim";
    public const string ArrayElem1Label = "rt_arr_elem1";
    public const string ArrayElementSizeLabel = "rt_arr_esize";  // taille d'une case : 2 (entier) ou 3 (texte, D065)
    public const string ArrayElemLabel = "rt_arr_elem";
    public const string ArrayMemoryLabel = "rt_arr_memory";
    const string ArrayAllocLabel = "rt_arr_alloc";
    const string ArrayAutoDimLabel = "rt_arr_autodim";
    const string ErrSubscript = "rt_arr_err_sub";
    public const string ErrMemory = "rt_heap_err_mem";
    const string ErrRedim = "rt_arr_err_redim";

    static byte[] Ascii(string text) => [.. System.Text.Encoding.ASCII.GetBytes(text), 0];

    // PRINT USING, nombres (D074). Voir le commentaire du module PrintUsing.
    static void EmitUsingNumber(Z80Emitter e)
    {
        const int PlusLead = Language.UsingFlags.PlusLead, TrailMinus = Language.UsingFlags.TrailMinus,
                  TrailPlus = Language.UsingFlags.TrailPlus, Asterisk = Language.UsingFlags.Asterisk,
                  Dollar = Language.UsingFlags.Dollar, Comma = Language.UsingFlags.Comma,
                  Exponent = Language.UsingFlags.Exponent, Point = Language.UsingFlags.Point;

        e.Label(UsingNumLabel);
        e.LdAB();
        e.LdMemA("rt_us_w");
        e.LdAC();
        e.LdMemA("rt_us_l");
        e.LdAD();
        e.LdMemA("rt_us_d");
        e.LdAE();
        e.LdMemA("rt_us_f");
        e.LdDeLabel("rt_fp_x");
        e.LdBcImm(5);
        e.Ldir();
        e.LdHlLabel("rt_us_b");                  // chiffres b0 à b9 : b0 = 0 (place pour une retenue), b1 à b9 = d0 à d8
        e.LdBImm(10);
        e.Label("rt_us_clr");
        e.StoreHlImm(0);
        e.IncHl();
        e.DjnzLabel("rt_us_clr");
        e.XorA();
        e.LdMemA("rt_us_neg");
        e.LdMemA("rt_us_e");
        e.LdAImm(1);
        e.LdMemA("rt_us_zero");
        e.LdAMem("rt_fp_x4");
        e.OrA();
        e.JrZLabel("rt_us_conv");                // zéro : tous les chiffres à 0
        e.CallLabel("rt_fp_conv");
        e.LdHlLabel("rt_fp_d0");
        e.LdDeLabel("rt_us_b1");
        e.LdBcImm(9);
        e.Ldir();
        e.LdAMem("rt_fp_e");
        e.LdMemA("rt_us_e");                     // b1 vaut b1 x 10^e
        e.LdAMem("rt_fp_neg");
        e.LdMemA("rt_us_neg");
        e.XorA();
        e.LdMemA("rt_us_zero");
        e.Label("rt_us_conv");
        e.LdHlLabel("rt_us_buf");
        e.LdMemHl("rt_us_p");

        // Signe devant, puis $
        e.LdAMem("rt_us_f");
        e.AndImm(PlusLead);
        e.JrZLabel("rt_us_nolead");
        e.LdAMem("rt_us_neg");
        e.OrA();
        e.LdAImm('+');
        e.JrZLabel("rt_us_lead");
        e.LdAImm('-');
        e.Label("rt_us_lead");
        e.CallLabel("rt_us_put");
        e.JrLabel("rt_us_dollar");
        e.Label("rt_us_nolead");
        e.LdAMem("rt_us_f");
        e.AndImm(TrailMinus | TrailPlus);
        e.JrNzLabel("rt_us_dollar");
        e.LdAMem("rt_us_neg");
        e.OrA();
        e.JrZLabel("rt_us_dollar");
        e.LdAImm('-');
        e.CallLabel("rt_us_put");
        e.Label("rt_us_dollar");
        e.LdAMem("rt_us_f");
        e.AndImm(Dollar);
        e.JrZLabel("rt_us_body");
        e.LdAImm('$');
        e.CallLabel("rt_us_put");
        e.Label("rt_us_body");
        e.LdHlMem("rt_us_p");
        e.LdMemHl("rt_us_ip");                   // début des chiffres (pour le « 0 » ajouté)
        e.XorA();
        e.LdMemA("rt_us_st");
        e.LdAMem("rt_us_f");
        e.AndImm(Exponent);
        e.JpNzLabel("rt_us_exp");

        // Sans exposant : arrondi au chiffre d'indice e + 2 + décimales, partie entière = b0 à b(e+1)
        e.LdAMem("rt_us_e");
        e.AddAImm(2);
        e.LdBA();
        e.LdAMem("rt_us_d");
        e.AddAB();
        e.CallLabel("rt_us_round");
        e.LdAMem("rt_us_e");
        e.AddAImm(2);
        e.LdMemA("rt_us_r");                     // chiffres de la partie entière restant à écrire
        e.AndImm(0x80);
        e.JrNzLabel("rt_us_intdone");
        e.LdAMem("rt_us_r");
        e.OrA();
        e.JrZLabel("rt_us_intdone");
        e.XorA();
        e.LdMemA("rt_us_i");
        e.Label("rt_us_int");
        e.LdAMem("rt_us_i");
        e.CallLabel("rt_us_dig");
        e.LdCA();
        e.LdAMem("rt_us_st");
        e.OrA();
        e.JrNzLabel("rt_us_more");
        e.LdAC();
        e.OrA();
        e.JrZLabel("rt_us_inext");               // zéros de tête : rien
        e.LdAImm(1);
        e.LdMemA("rt_us_st");
        e.JrLabel("rt_us_pdig");
        e.Label("rt_us_more");
        e.LdAMem("rt_us_f");
        e.AndImm(Comma);
        e.JrZLabel("rt_us_pdig");
        e.LdAMem("rt_us_r");                     // « , » avant un chiffre si le nombre de chiffres restants est multiple de 3
        e.Label("rt_us_mod3");
        e.SubImm(3);
        e.JrZLabel("rt_us_comma");
        e.JrNcLabel("rt_us_mod3");
        e.JrLabel("rt_us_pdig");
        e.Label("rt_us_comma");
        e.LdAImm(',');
        e.CallLabel("rt_us_put");
        e.Label("rt_us_pdig");
        e.LdAC();
        e.AddAImm('0');
        e.CallLabel("rt_us_put");
        e.Label("rt_us_inext");
        e.LdHlLabel("rt_us_i");
        e.IncMemHl();
        e.LdHlLabel("rt_us_r");
        e.DecMemHl();
        e.JrNzLabel("rt_us_int");
        e.Label("rt_us_intdone");
        e.LdAMem("rt_us_f");
        e.AndImm(Point);
        e.JpZLabel("rt_us_trail");
        e.LdAImm('.');
        e.CallLabel("rt_us_put");
        e.LdAMem("rt_us_e");
        e.AddAImm(2);
        e.LdMemA("rt_us_i");
        e.CallLabel("rt_us_decs");
        e.JpLabel("rt_us_trail");

        // Avec exposant : k chiffres avant le point (une place de moins s'il n'y a pas de signe écrit)
        e.Label("rt_us_exp");
        e.LdAMem("rt_us_l");
        e.LdBA();
        e.LdAMem("rt_us_f");
        e.AndImm(PlusLead | TrailMinus | TrailPlus);
        e.JrNzLabel("rt_us_kset");
        e.LdAB();
        e.OrA();
        e.JrZLabel("rt_us_kset");
        e.DecB();
        e.Label("rt_us_kset");
        e.LdAB();
        e.LdMemA("rt_us_k");
        e.LdAMem("rt_us_d");
        e.AddAB();
        e.IncA();
        e.CallLabel("rt_us_round");              // arrondi au chiffre d'indice 1 + k + décimales
        e.LdAMem("rt_us_b");
        e.OrA();
        e.LdAImm(1);
        e.JrZLabel("rt_us_s");
        e.XorA();                                // retenue jusqu'à b0 : les chiffres commencent à b0
        e.Label("rt_us_s");
        e.LdMemA("rt_us_start");
        e.LdAMem("rt_us_e");                     // exposant écrit : e + 1 - k (+ 1 si retenue), 0 pour zéro
        e.IncA();
        e.LdBA();
        e.LdAMem("rt_us_k");
        e.LdCA();
        e.LdAB();
        e.SubC();
        e.LdBA();
        e.LdAMem("rt_us_start");
        e.OrA();
        e.LdAB();
        e.JrNzLabel("rt_us_pe1");
        e.IncA();
        e.Label("rt_us_pe1");
        e.LdBA();
        e.LdAMem("rt_us_zero");
        e.OrA();
        e.LdAB();
        e.JrZLabel("rt_us_pe2");
        e.XorA();
        e.Label("rt_us_pe2");
        e.LdMemA("rt_us_pe");
        e.LdAMem("rt_us_start");
        e.LdMemA("rt_us_i");
        e.LdAMem("rt_us_k");
        e.OrA();
        e.JrZLabel("rt_us_epoint");
        e.LdBA();
        e.Label("rt_us_eint");
        e.PushBc();
        e.LdAMem("rt_us_i");
        e.CallLabel("rt_us_dig");
        e.AddAImm('0');
        e.CallLabel("rt_us_put");
        e.LdHlLabel("rt_us_i");
        e.IncMemHl();
        e.PopBc();
        e.DjnzLabel("rt_us_eint");
        e.Label("rt_us_epoint");
        e.LdAMem("rt_us_f");
        e.AndImm(Point);
        e.JrZLabel("rt_us_eexp");
        e.LdAImm('.');
        e.CallLabel("rt_us_put");
        e.CallLabel("rt_us_decs");
        e.Label("rt_us_eexp");
        e.LdAImm('E');
        e.CallLabel("rt_us_put");
        e.LdAMem("rt_us_pe");
        e.LdBA();
        e.AndImm(0x80);
        e.LdAImm('+');
        e.JrZLabel("rt_us_es");
        e.LdAB();
        e.Cpl();
        e.IncA();
        e.LdBA();
        e.LdAImm('-');
        e.Label("rt_us_es");
        e.CallLabel("rt_us_put");
        e.LdAB();
        e.LdCImm('0' - 1);
        e.Label("rt_us_et");
        e.IncC();
        e.SubImm(10);
        e.JrNcLabel("rt_us_et");
        e.AddAImm(10);
        e.LdBA();
        e.LdAC();
        e.CallLabel("rt_us_put");
        e.LdAB();
        e.AddAImm('0');
        e.CallLabel("rt_us_put");
        e.LdAImm(1);
        e.LdMemA("rt_us_st");                    // pas de « 0 » ajouté avec un exposant

        // Signe derrière
        e.Label("rt_us_trail");
        e.LdAMem("rt_us_f");
        e.AndImm(TrailMinus);
        e.JrZLabel("rt_us_tp");
        e.LdAMem("rt_us_neg");
        e.OrA();
        e.LdAImm(' ');
        e.JrZLabel("rt_us_tput");
        e.LdAImm('-');
        e.JrLabel("rt_us_tput");
        e.Label("rt_us_tp");
        e.LdAMem("rt_us_f");
        e.AndImm(TrailPlus);
        e.JrZLabel("rt_us_len");
        e.LdAMem("rt_us_neg");
        e.OrA();
        e.LdAImm('+');
        e.JrZLabel("rt_us_tput");
        e.LdAImm('-');
        e.Label("rt_us_tput");
        e.CallLabel("rt_us_put");

        // Longueur ; partie entière nulle : « 0 » inséré au début des chiffres s'il reste de la place
        e.Label("rt_us_len");
        e.LdHlMem("rt_us_p");
        e.LdDeLabel("rt_us_buf");
        e.OrA();
        e.SbcHlDe();
        e.LdAL();
        e.LdMemA("rt_us_n");
        e.LdAMem("rt_us_st");
        e.OrA();
        e.JrNzLabel("rt_us_out");
        e.LdAMem("rt_us_w");
        e.LdBA();
        e.LdAMem("rt_us_n");
        e.CpB();
        e.JrNcLabel("rt_us_out");
        e.LdHlMem("rt_us_p");
        e.LdDeMem("rt_us_ip");
        e.OrA();
        e.SbcHlDe();                             // HL = caractères à décaler
        e.LdAH();
        e.OrL();
        e.JrZLabel("rt_us_z0");
        e.LdBH();
        e.LdCL();
        e.LdHlMem("rt_us_p");
        e.LdDH();
        e.LdEL();
        e.DecHl();
        e.Lddr();
        e.Label("rt_us_z0");
        e.LdHlMem("rt_us_ip");
        e.StoreHlImm('0');
        e.LdHlLabel("rt_us_n");
        e.IncMemHl();

        // Écriture : vérification de fin de ligne, puis « % » ou les espaces (étoiles) de gauche, puis le nombre
        e.Label("rt_us_out");
        e.LdAMem("rt_us_w");
        e.LdBA();
        e.LdAMem("rt_us_n");
        e.CpB();
        e.JrCLabel("rt_us_pad");
        e.JrZLabel("rt_us_pad");
        e.IncA();                                // trop long : « % » puis le nombre
        e.CallLabel(PrintFitLabel);
        e.LdAImm('%');
        e.CallAddress(TxtOutput);
        e.JrLabel("rt_us_text");
        e.Label("rt_us_pad");
        e.LdAB();
        e.CallLabel(PrintFitLabel);
        e.LdAMem("rt_us_n");
        e.LdCA();
        e.LdAMem("rt_us_w");
        e.SubC();
        e.JrZLabel("rt_us_text");
        e.LdBA();
        e.LdAMem("rt_us_f");
        e.AndImm(Asterisk);
        e.LdAImm(' ');
        e.JrZLabel("rt_us_padl");
        e.LdAImm('*');
        e.Label("rt_us_padl");
        e.CallAddress(TxtOutput);
        e.DjnzLabel("rt_us_padl");
        e.Label("rt_us_text");
        e.LdAMem("rt_us_n");
        e.OrA();
        e.RetZ();
        e.LdBA();
        e.LdHlLabel("rt_us_buf");
        e.Label("rt_us_tl");
        e.LdAHl();
        e.CallAddress(TxtOutput);
        e.IncHl();
        e.DjnzLabel("rt_us_tl");
        e.Ret();

        // A = caractère, ajouté au texte (BC gardés)
        e.Label("rt_us_put");
        e.PushHl();
        e.LdHlMem("rt_us_p");
        e.LdHlA();
        e.IncHl();
        e.LdMemHl("rt_us_p");
        e.PopHl();
        e.Ret();

        // A = indice -> A = chiffre b(indice), 0 en dehors de b0 à b9 (BC gardés)
        e.Label("rt_us_dig");
        e.CpImm(10);
        e.JrNcLabel("rt_us_dig0");
        e.LdEA();
        e.LdDZero();
        e.LdHlLabel("rt_us_b");
        e.AddHlDe();
        e.LdAHl();
        e.Ret();
        e.Label("rt_us_dig0");
        e.XorA();
        e.Ret();

        // Décimales : rt_us_d chiffres à partir de l'indice rt_us_i
        e.Label("rt_us_decs");
        e.LdAMem("rt_us_d");
        e.OrA();
        e.RetZ();
        e.LdBA();
        e.Label("rt_us_dl");
        e.PushBc();
        e.LdAMem("rt_us_i");
        e.CallLabel("rt_us_dig");
        e.AddAImm('0');
        e.CallLabel("rt_us_put");
        e.LdHlLabel("rt_us_i");
        e.IncMemHl();
        e.PopBc();
        e.DjnzLabel("rt_us_dl");
        e.Ret();

        // Arrondi : A = indice du premier chiffre enlevé. 0 ou moins : tout devient 0 ; 10 ou plus : rien à faire.
        e.Label("rt_us_round");
        e.LdMemA("rt_us_q");
        e.LdBA();
        e.AndImm(0x80);
        e.JrNzLabel("rt_us_rzero");
        e.LdAB();
        e.OrA();
        e.JrZLabel("rt_us_rzero");
        e.CpImm(10);
        e.RetNc();
        e.LdEA();
        e.LdDZero();
        e.LdHlLabel("rt_us_b");
        e.AddHlDe();
        e.LdAHl();
        e.CpImm(5);
        e.JrCLabel("rt_us_rclr");
        e.PushHl();
        e.Label("rt_us_rinc");                   // + 1 au chiffre d'avant, avec retenue
        e.DecHl();
        e.IncMemHl();
        e.LdAHl();
        e.CpImm(10);
        e.JrCLabel("rt_us_rdone");
        e.StoreHlImm(0);
        e.JrLabel("rt_us_rinc");
        e.Label("rt_us_rdone");
        e.PopHl();
        e.Label("rt_us_rclr");
        e.LdAMem("rt_us_q");
        e.LdBA();
        e.LdAImm(10);
        e.SubB();
        e.LdBA();
        e.Label("rt_us_rcl");
        e.StoreHlImm(0);
        e.IncHl();
        e.DjnzLabel("rt_us_rcl");
        e.Ret();
        e.Label("rt_us_rzero");
        e.LdHlLabel("rt_us_b");
        e.LdBImm(10);
        e.JrLabel("rt_us_rcl");

        foreach (string name in new[] { "rt_us_w", "rt_us_l", "rt_us_d", "rt_us_f", "rt_us_neg", "rt_us_e", "rt_us_zero",
                                        "rt_us_q", "rt_us_r", "rt_us_i", "rt_us_st", "rt_us_k", "rt_us_start", "rt_us_pe", "rt_us_n" })
        {
            e.Label(name);
            e.Reserve(1);
        }
        e.Label("rt_us_p");
        e.Reserve(2);
        e.Label("rt_us_ip");
        e.Reserve(2);
        e.Label("rt_us_b");
        e.Reserve(1);
        e.Label("rt_us_b1");
        e.Reserve(9);
        e.Label("rt_us_buf");
        e.Reserve(100);
    }

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
        // Numéro de fenêtre calculé (PRINT #N, ..., lot H1, D072) : HL de 0 à 7, sinon « Improper argument » comme le
        // Locomotive (#8 et #9 viendront avec le lot J). Rend A = fenêtre choisie avant, comme TXT STR SELECT.
        [StreamName] = new RuntimeModule(StreamName, [ErrorName], e =>
        {
            e.Label(StreamLabel);
            e.LdAH();
            e.OrA();
            e.JrNzLabel("rt_stream_bad");
            e.LdAL();
            e.CpImm(8);
            e.JpCLabel("rt_stream_ok");
            e.Label("rt_stream_bad");
            e.LdHlLabel("rt_stream_msg");
            e.JpLabel(ErrorLabel);
            e.Label("rt_stream_ok");
            e.JpAddress(TxtStrSelect);
            e.Label("rt_stream_msg");
            e.Data(Ascii("Improper argument"));
        }),

        // Mise en page de PRINT, comme le Locomotive (lot H2, D073). Tout se fait dans la fenêtre choisie : sa largeur vient
        // de TXT GET WINDOW, la colonne du curseur de TXT GET CURSOR (à partir de 1).
        //  rt_zone : début de la zone suivante (1 + k * largeur, après la colonne actuelle) ; si cette zone ne tient pas
        //            entière dans la fenêtre, passage à la ligne.
        //  rt_tab  : HL = colonne (moins de 1 : 1 ; au-delà de la largeur : ramenée dedans). Colonne déjà dépassée :
        //            passage à la ligne puis espaces.
        //  rt_spc  : HL = nombre d'espaces (négatif : aucun ; au-delà de la largeur : reste de la division).
        //  rt_zone_set : HL = largeur des zones, de 1 à 255, sinon « Improper argument ».
        // Avant d'écrire un élément de PRINT (texte de 2 caractères ou plus, texte calculé, nombre), le vrai BASIC vérifie
        // qu'il tient dans la fin de la ligne ; sinon il passe d'abord à la ligne, sauf si le curseur est en colonne 1
        // (constaté dans SugarBox : PRINT "X";TAB(38);"ABCD" écrit ABCD entier sur la ligne suivante, D073).
        // La colonne est celle où s'écrira le caractère suivant (TXT VALIDATE) : après une ligne remplie, c'est 1.
        [PrintFitName] = new RuntimeModule(PrintFitName, [], e =>
        {
            e.Label(PrintFitLabel);
            e.OrA();
            e.RetZ();
            e.LdMemA("rt_pf_len");
            e.CallLabel("rt_pf_width");
            e.LdMemA("rt_pf_w");
            e.CallAddress(TxtGetCursor);
            e.CallAddress(TxtValidate);
            e.LdAH();
            e.DecA();
            e.RetZ();                                // colonne 1 : rien à faire
            e.LdLA();
            e.LdHZero();                             // HL = colonne - 1
            e.LdAMem("rt_pf_len");
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();                             // HL = dernière colonne de l'élément
            e.LdAMem("rt_pf_w");
            e.LdEA();
            e.OrA();
            e.SbcHlDe();
            e.RetC();
            e.RetZ();                                // il tient
            e.JpLabel("rt_pf_newline");

            e.Label(PrintFitStrLabel);
            e.PushHl();
            e.LdAHl();                               // longueur du texte
            e.CallLabel(PrintFitLabel);
            e.PopHl();
            e.Ret();

            // Longueur d'un entier écrit par PRINT : signe ou espace, puis 1 à 5 chiffres (sans l'espace final)
            e.Label(PrintFitIntLabel);
            e.PushHl();
            e.Bit7H();
            e.JrZLabel("rt_pf_int_pos");
            e.XorA();                                // HL = 0 - HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.Label("rt_pf_int_pos");
            e.LdCImm(2);
            foreach (int limit in new[] { 10, 100, 1000, 10000 })
            {
                e.LdDeImm(limit);
                e.OrA();
                e.SbcHlDe();
                e.AddHlDe();
                e.JrCLabel("rt_pf_int_len");
                e.IncC();
            }
            e.Label("rt_pf_int_len");
            e.LdAC();
            e.CallLabel(PrintFitLabel);
            e.PopHl();
            e.Ret();

            // A = largeur de la fenêtre
            e.Label("rt_pf_width");
            e.CallAddress(TxtGetWindow);
            e.LdAD();
            e.SubH();
            e.IncA();
            e.Ret();

            e.Label("rt_pf_newline");
            e.LdAImm(13);
            e.CallAddress(TxtOutput);
            e.LdAImm(10);
            e.JpAddress(TxtOutput);

            foreach (string scratch in new[] { "rt_pf_w", "rt_pf_len" })
            {
                e.Label(scratch);
                e.Reserve(1);
            }
        }),

        // PRINT USING, textes (D074) : « ! » (A = 1), « \   \ » (A = largeur : le texte est coupé ou complété d'espaces),
        // « & » (A = 0 : tout le texte).
        [PrintUsingStrName] = new RuntimeModule(PrintUsingStrName, [StringsName, PrintFitName], e =>
        {
            e.Label(UsingStrLabel);
            e.OrA();
            e.JrNzLabel("rt_ust_fixed");
            e.CallLabel(PrintFitStrLabel);
            e.JpLabel(StrPrintLabel);
            e.Label("rt_ust_fixed");
            e.LdMemA("rt_ust_w");
            e.PushHl();
            e.CallLabel(PrintFitLabel);
            e.PopHl();
            e.LoadCFromHl();                         // C = longueur du texte
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();                                // HL = lettres
            e.LdAMem("rt_ust_w");
            e.LdBA();                                // B = largeur
            e.Label("rt_ust_loop");
            e.LdAC();
            e.OrA();
            e.JrZLabel("rt_ust_pad");
            e.LdAHl();
            e.CallAddress(TxtOutput);
            e.IncHl();
            e.DecC();
            e.DjnzLabel("rt_ust_loop");
            e.Ret();
            e.Label("rt_ust_pad");
            e.LdAImm(' ');
            e.CallAddress(TxtOutput);
            e.DjnzLabel("rt_ust_pad");
            e.Ret();
            e.Label("rt_ust_w");
            e.Reserve(1);
        }),

        // PRINT USING, nombres (D074), règles relevées dans le vrai BASIC (SugarBox, 15 nov. 2026). Le nombre (5 octets) est
        // découpé en 9 chiffres par rt_fp_conv, arrondi au dernier chiffre écrit, puis écrit dans rt_us_buf :
        // signe devant (+, ou - sans signe derrière), $, chiffres (groupés par 3 avec « , »), point, décimales, exposant,
        // signe derrière. Partie entière nulle : « 0 » ajouté s'il reste de la place (0.50). Plus court que l'emplacement :
        // espaces (ou étoiles) à gauche ; plus long : « % » puis le nombre entier.
        [PrintUsingName] = new RuntimeModule(PrintUsingName, [RealPrintName, PrintFitName], EmitUsingNumber),

        // Fichiers de données (lot J1, D084) : erreurs communes. Un fichier qu'AMSDOS ne peut pas ouvrir (« ... not found »,
        // message écrit par AMSDOS) arrête le programme sans autre message, comme le vrai BASIC ; avec ON ERROR GOTO :
        // erreur 32 (« Broken in »).
        [FileCommonName] = new RuntimeModule(FileCommonName, [ErrorName], e =>
        {
            e.Label("rt_file_notopen");
            e.LdHlLabel("rt_file_m1");
            e.JpLabel(ErrorLabel);
            e.Label("rt_file_already");
            e.LdHlLabel("rt_file_m2");
            e.JpLabel(ErrorLabel);
            e.Label("rt_file_eofmet");
            e.LdHlLabel("rt_file_m3");
            e.JpLabel(ErrorLabel);
            e.Label("rt_file_fail");
            e.LdHlMem(ErrorTrapLabel);
            e.LdAH();
            e.OrL();
            e.JrZLabel("rt_file_quiet");
            e.LdHlLabel("rt_file_m4");
            e.JpLabel(ErrorLabel);
            e.Label("rt_file_quiet");
            e.CallAddress(KmWaitKey);
            e.JpAddress(0);
            // Fichier qui n'est pas du bon type (OPENIN d'un binaire, LOAD d'un texte ou d'un programme BASIC, D085)
            e.Label("rt_file_typeerr");
            e.CallAddress(0xBC7D);                   // CAS IN ABANDON
            e.LdHlLabel("rt_file_m5");
            e.JpLabel(ErrorLabel);
            e.Label("rt_file_m5");
            e.Data(Ascii("File type error"));
            e.Label("rt_fo_isopen");                 // fichier ouvert en écriture, en lecture
            e.Reserve(1);
            e.Label("rt_fl_isopen");
            e.Reserve(1);
            e.Label("rt_file_m1");
            e.Data(Ascii("File not open"));
            e.Label("rt_file_m2");
            e.Data(Ascii("File already open"));
            e.Label("rt_file_m3");
            e.Data(Ascii("EOF met"));
            e.Label("rt_file_m4");
            e.Data(Ascii("Broken in"));

            // HL = fiche d'un texte -> HL = adresse des lettres, B = longueur
            e.Label("rt_file_name");
            e.LoadBFromHl();
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.ExDeHl();
            e.Ret();
        }),

        // OPENOUT, PRINT #9, WRITE #9, CLOSEOUT (D084). Pendant PRINT #9, quatre routines du firmware sont détournées (leur
        // entrée de 3 octets est remplacée par un saut, puis remise) : TXT OUTPUT écrit dans le fichier (CAS OUT CHAR) ;
        // TXT GET CURSOR donne la colonne du fichier ; TXT VALIDATE accepte tout ; TXT GET WINDOW donne 255 colonnes.
        // Les virgules (zones de 13), TAB, SPC et les nombres sortent donc comme à l'écran (SugarBox, 16 nov. 2026).
        [FileOutName] = new RuntimeModule(FileOutName, [FileCommonName], e =>
        {
            (int Address, string Handler)[] patches =
            [
                (TxtOutput, "rt_fo_out"), (TxtGetCursor, "rt_fo_cur"), (TxtValidate, "rt_fo_val"), (TxtGetWindow, "rt_fo_win")
            ];
            e.Label("rt_fo_open");                   // HL = fiche du nom
            e.LdAMem("rt_fo_isopen");
            e.OrA();
            e.JpNzLabel("rt_file_already");
            e.CallLabel("rt_file_name");
            e.LdDeImm(FileOutBuffer);
            e.CallAddress(0xBC8C);                   // CAS OUT OPEN
            e.JpNcLabel("rt_file_fail");
            e.LdAImm(1);
            e.LdMemA("rt_fo_isopen");
            e.LdMemA("rt_fo_col");
            e.Ret();

            e.Label("rt_fo_close");
            e.LdAMem("rt_fo_isopen");
            e.OrA();
            e.RetZ();
            e.XorA();
            e.LdMemA("rt_fo_isopen");
            e.CallAddress(0xBC8F);                   // CAS OUT CLOSE : le reste est écrit
            e.Ret();

            e.Label(FileOutOnLabel);
            e.LdAMem("rt_fo_isopen");
            e.OrA();
            e.JpZLabel("rt_file_notopen");
            for (int k = 0; k < patches.Length; k++)
            {
                e.LdHlImm(patches[k].Address);       // l'entrée d'origine est gardée
                e.LdDeLabel($"rt_fo_s{k}");
                e.LdBcImm(3);
                e.Ldir();
                e.LdHlImm(patches[k].Address);
                e.LdDeLabel(patches[k].Handler);
                e.CallLabel("rt_fo_jp");
            }
            e.Ret();
            e.Label("rt_fo_jp");                     // HL = entrée : JP DE
            e.StoreHlImm(0xC3);
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.Ret();

            e.Label(FileOutOffLabel);
            for (int k = 0; k < patches.Length; k++)
            {
                e.LdHlLabel($"rt_fo_s{k}");
                e.LdDeImm(patches[k].Address);
                e.LdBcImm(3);
                e.Ldir();
            }
            e.Ret();

            // TXT OUTPUT détourné : A = caractère. Pendant CAS OUT CHAR, l'entrée d'origine est remise (AMSDOS peut écrire
            // un message à l'écran). Tous les registres sont gardés, comme TXT OUTPUT.
            e.Label("rt_fo_out");
            e.PushAf();
            e.PushBc();
            e.PushDe();
            e.PushHl();
            e.PushAf();
            e.LdHlLabel("rt_fo_s0");
            e.LdDeImm(TxtOutput);
            e.LdBcImm(3);
            e.Ldir();
            e.PopAf();
            e.PushAf();
            e.CallAddress(0xBC95);                   // CAS OUT CHAR
            e.PopAf();
            e.CpImm(13);
            e.JrNzLabel("rt_fo_notcr");
            e.LdAImm(1);
            e.LdMemA("rt_fo_col");
            e.JrLabel("rt_fo_again");
            e.Label("rt_fo_notcr");
            e.CpImm(32);
            e.JrCLabel("rt_fo_again");
            e.LdHlLabel("rt_fo_col");
            e.IncMemHl();
            e.Label("rt_fo_again");
            e.LdHlImm(TxtOutput);
            e.LdDeLabel("rt_fo_out");
            e.CallLabel("rt_fo_jp");
            e.PopHl();
            e.PopDe();
            e.PopBc();
            e.PopAf();
            e.Ret();

            e.Label("rt_fo_cur");                    // H = colonne du fichier, L = 1, A = 0
            e.LdAMem("rt_fo_col");
            e.LdHA();
            e.LdAImm(1);
            e.LdLA();
            e.XorA();
            e.Ret();
            e.Label("rt_fo_val");                    // toute position est bonne
            e.Scf();
            e.Ret();
            e.Label("rt_fo_win");                    // colonnes 0 à 254, lignes 0 à 24
            e.LdHlImm(0);
            e.LdDeImm((254 << 8) | 24);
            e.Ret();

            e.Label("rt_fo_col");
            e.Reserve(1);
            for (int k = 0; k < patches.Length; k++)
            {
                e.Label($"rt_fo_s{k}");
                e.Reserve(3);
            }
        }),

        // OPENIN, INPUT #9, LINE INPUT #9, EOF, CLOSEIN (D084). Une ligne du fichier (jusqu'au retour à la ligne, le saut
        // de ligne qui suit est sauté) va dans la ligne de saisie du clavier (rt_inp_buf) : INPUT #9 la découpe avec les
        // mêmes routines qu'INPUT ; quand elle est finie, la ligne suivante est lue.
        [FileInName] = new RuntimeModule(FileInName, [FileCommonName, InputName], e =>
        {
            e.Label("rt_fl_open");
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JpNzLabel("rt_file_already");
            e.CallLabel("rt_file_name");
            e.LdDeImm(FileInBuffer);
            e.CallAddress(0xBC77);                   // CAS IN OPEN
            e.JpNcLabel("rt_file_fail");
            e.AndImm(0x0E);                          // seulement un fichier texte (D085)
            e.CpImm(0x06);
            e.JpNzLabel("rt_file_typeerr");
            e.LdAImm(1);
            e.LdMemA("rt_fl_isopen");
            e.Ret();

            e.Label("rt_fl_close");
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.RetZ();
            e.XorA();
            e.LdMemA("rt_fl_isopen");
            e.CallAddress(0xBC7A);                   // CAS IN CLOSE
            e.Ret();

            e.Label("rt_fl_eof");                    // HL = -1 à la fin (ou sans fichier), sinon 0
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JrZLabel("rt_fl_end");
            e.CallAddress(0xBC89);                   // CAS TEST EOF : retenue à 0 à la fin
            e.JrNcLabel("rt_fl_end");
            e.LdHlImm(0);
            e.Ret();
            e.Label("rt_fl_end");
            e.LdHlImm(0xFFFF);
            e.Ret();

            // Ligne suivante du fichier -> rt_inp_buf (255 caractères au plus) ; rien à lire : « EOF met »
            e.Label("rt_fl_line");
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JpZLabel("rt_file_notopen");
            e.LdHlLabel("rt_inp_buf");
            e.LdMemHl("rt_inp_ptr");
            e.LdBImm(0);
            e.Label("rt_fl_next");
            e.CallLabel("rt_fl_char");
            e.JrNcLabel("rt_fl_stop");
            e.CpImm(13);
            e.JrZLabel("rt_fl_cr");
            e.CpImm(10);
            e.JrZLabel("rt_fl_next");                // saut de ligne seul : sauté
            e.LdHlA();
            e.IncHl();
            e.IncB();
            e.LdAB();
            e.CpImm(255);
            e.JrNzLabel("rt_fl_next");
            e.JrLabel("rt_fl_done");
            e.Label("rt_fl_cr");                     // après le retour à la ligne : un saut de ligne est sauté
            e.CallLabel("rt_fl_char");
            e.JrNcLabel("rt_fl_done");
            e.CpImm(10);
            e.JrZLabel("rt_fl_done");
            e.CallAddress(0xBC86);                   // CAS RETURN : ce caractère sera relu
            e.JrLabel("rt_fl_done");
            e.Label("rt_fl_stop");                   // fin du fichier
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_file_eofmet");
            e.Label("rt_fl_done");
            e.StoreHlImm(0);
            e.Ret();
            e.Label("rt_fl_char");                   // CAS IN CHAR en gardant B et HL
            e.PushBc();
            e.PushHl();
            e.CallAddress(0xBC80);
            e.PopHl();
            e.PopBc();
            e.Ret();

            // Valeur suivante : s'il ne reste rien de la ligne, la ligne suivante
            e.Label("rt_fl_more");
            e.LdHlMem("rt_inp_ptr");
            e.LdAHl();
            e.OrA();
            e.RetNz();
            e.JpLabel("rt_fl_line");

            // Valeur qui n'est pas lisible : on passe jusqu'après la virgule (un texte entre guillemets peut en contenir)
            e.Label("rt_fl_skip");
            e.LdHlMem("rt_inp_ptr");
            e.Label("rt_fl_sk_sp");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_fl_sk_q");
            e.IncHl();
            e.JrLabel("rt_fl_sk_sp");
            e.Label("rt_fl_sk_q");
            e.CpImm('"');
            e.JrNzLabel("rt_fl_sk_to");
            e.Label("rt_fl_sk_in");
            e.IncHl();
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_fl_sk_end");
            e.CpImm('"');
            e.JrNzLabel("rt_fl_sk_in");
            e.IncHl();
            e.Label("rt_fl_sk_to");
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_fl_sk_end");
            e.IncHl();
            e.CpImm(',');
            e.JrNzLabel("rt_fl_sk_to");
            e.Label("rt_fl_sk_end");
            e.LdMemHl("rt_inp_ptr");
            e.Ret();

            // Nombre entier, lu souplement (comme VAL) : espaces, signe, chiffres -> HL ; pas de chiffre : 0, valeur passée
            e.Label("rt_fl_num");
            e.LdHlMem("rt_inp_ptr");
            e.Label("rt_fl_n_sp");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_fl_n_sign");
            e.IncHl();
            e.JrLabel("rt_fl_n_sp");
            e.Label("rt_fl_n_sign");
            e.LdCImm(0);                             // C = 1 : négatif
            e.CpImm('-');
            e.JrNzLabel("rt_fl_n_plus");
            e.IncC();
            e.IncHl();
            e.JrLabel("rt_fl_n_first");
            e.Label("rt_fl_n_plus");
            e.CpImm('+');
            e.JrNzLabel("rt_fl_n_first");
            e.IncHl();
            e.Label("rt_fl_n_first");
            e.LdAHl();
            e.SubImm('0');
            e.CpImm(10);
            e.JrCLabel("rt_fl_n_go");
            e.CallLabel("rt_fl_skip");               // pas un nombre
            e.LdHlImm(0);
            e.Ret();
            e.Label("rt_fl_n_go");
            e.ExDeHl();                              // DE = texte, HL = valeur
            e.LdHlImm(0);
            e.Label("rt_fl_n_digit");
            e.LdADe();
            e.SubImm('0');
            e.CpImm(10);
            e.JrNcLabel("rt_fl_n_end");
            e.IncDe();
            e.PushDe();
            e.AddHlHl();                             // HL x 10 + chiffre
            e.PushHl();
            e.AddHlHl();
            e.AddHlHl();
            e.PopDe();
            e.AddHlDe();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();
            e.PopDe();
            e.JrLabel("rt_fl_n_digit");
            e.Label("rt_fl_n_end");
            e.LdAC();
            e.OrA();
            e.JrZLabel("rt_fl_n_pos");
            e.XorA();                                // HL = -HL
            e.SubL();
            e.LdLA();
            e.SbcAA();
            e.SubH();
            e.LdHA();
            e.Label("rt_fl_n_pos");
            e.PushHl();
            e.ExDeHl();
            e.CallLabel("rt_fl_after");
            e.PopHl();
            e.Ret();

            // Après un nombre (HL = suite du texte) : espaces ; une virgule est passée ; un autre nombre (séparé par des
            // espaces) reste pour la valeur suivante ; autre chose : passé jusqu'à la virgule
            e.Label("rt_fl_after");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_fl_a_look");
            e.IncHl();
            e.JrLabel("rt_fl_after");
            e.Label("rt_fl_a_look");
            e.LdMemHl("rt_inp_ptr");
            e.OrA();
            e.RetZ();
            e.CpImm(',');
            e.JrNzLabel("rt_fl_a_num");
            e.IncHl();
            e.LdMemHl("rt_inp_ptr");
            e.Ret();
            e.Label("rt_fl_a_num");
            e.CpImm('-');
            e.RetZ();
            e.CpImm('+');
            e.RetZ();
            e.CpImm('.');
            e.RetZ();
            e.SubImm('0');
            e.CpImm(10);
            e.RetC();
            e.JpLabel("rt_fl_skip");

            e.Label("rt_fl_save");
            e.Reserve(2);
        }),

        // CAT, SAVE nom,B,..., LOAD nom[,adresse], SPEED WRITE (lot J2, D085), comme le vrai BASIC (SugarBox, 16 nov. 2026).
        // LOAD : seulement un fichier binaire, et seulement au-dessus de la mémoire du programme (« Memory full » sinon,
        // comme le vrai BASIC avant MEMORY) ; sans adresse, à l'adresse où il a été enregistré.
        [FileBinaryName] = new RuntimeModule(FileBinaryName, [FileCommonName], e =>
        {
            e.Label("rt_fb_save");                   // HL = fiche du nom ; rt_fb_a, rt_fb_l, rt_fb_e
            e.LdAMem("rt_fo_isopen");
            e.OrA();
            e.JpNzLabel("rt_file_already");
            e.CallLabel("rt_file_name");
            e.LdDeImm(FileOutBuffer);
            e.CallAddress(0xBC8C);                   // CAS OUT OPEN
            e.JpNcLabel("rt_file_fail");
            e.LdHlMem("rt_fb_e");
            e.PushHl();
            e.PopBc();                               // BC = adresse de lancement
            e.LdDeMem("rt_fb_l");
            e.LdHlMem("rt_fb_a");
            e.LdAImm(2);                             // fichier binaire
            e.CallAddress(0xBC98);                   // CAS OUT DIRECT
            e.JpNcLabel("rt_file_fail");
            e.CallAddress(0xBC8F);                   // CAS OUT CLOSE
            e.JpNcLabel("rt_file_fail");
            e.Ret();

            e.Label("rt_fb_load");                   // HL = fiche du nom ; rt_fb_has, rt_fb_a, rt_fb_lim
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JpNzLabel("rt_file_already");
            e.CallLabel("rt_file_name");
            e.LdDeImm(FileInBuffer);
            e.CallAddress(0xBC77);                   // CAS IN OPEN : A = type, DE = adresse d'origine
            e.JpNcLabel("rt_file_fail");
            e.AndImm(0x0E);
            e.JpZLabel("rt_file_typeerr");           // programme BASIC
            e.CpImm(0x06);
            e.JpZLabel("rt_file_typeerr");           // fichier texte
            e.LdAMem("rt_fb_has");
            e.OrA();
            e.JrNzLabel("rt_fb_where");
            e.ExDeHl();
            e.LdMemHl("rt_fb_a");
            e.Label("rt_fb_where");
            e.LdHlMem("rt_fb_a");
            e.LdDeMem("rt_fb_lim");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_fb_full");
            e.LdHlMem("rt_fb_a");
            e.CallAddress(0xBC83);                   // CAS IN DIRECT
            e.JpNcLabel("rt_file_fail");
            e.CallAddress(0xBC7A);                   // CAS IN CLOSE
            e.Ret();
            e.Label("rt_fb_full");
            e.CallAddress(0xBC7D);                   // CAS IN ABANDON
            e.LdHlLabel("rt_fb_m1");
            e.JpLabel(ErrorLabel);

            e.Label("rt_fb_cat");                    // le firmware refuse le catalogue si un fichier est ouvert en lecture
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JpNzLabel("rt_file_already");
            e.LdDeImm(FileInBuffer);
            e.CallAddress(0xBC9B);                   // CAS CATALOG
            e.Ret();

            e.Label("rt_fb_speed");                  // HL = 0 (1000 bauds) ou 1 (2000 bauds)
            e.LdAH();
            e.OrA();
            e.JrNzLabel("rt_fb_bad");
            e.OrL();
            e.JrZLabel("rt_fb_s0");
            e.DecA();
            e.JrNzLabel("rt_fb_bad");
            e.LdHlImm(167);
            e.LdAImm(50);
            e.JrLabel("rt_fb_set");
            e.Label("rt_fb_s0");
            e.LdHlImm(333);
            e.LdAImm(25);
            e.Label("rt_fb_set");
            e.CallAddress(0xBC68);                   // CAS SET SPEED
            e.Ret();
            e.Label("rt_fb_bad");
            e.LdHlLabel("rt_fb_m2");
            e.JpLabel(ErrorLabel);

            // RUN "nom" (D086) : HL = fiche du nom. Sans point, « .BIN » est ajouté (le .BAS du même nom est le
            // chargeur BASIC). Les fichiers ouverts sont fermés ; le fichier doit être binaire. Un petit chargeur est
            // recopié en &3800, hors du programme, puis il charge le fichier à son adresse et saute à son lancement.
            e.Label("rt_fb_run");
            e.PushHl();
            e.LdAMem("rt_fo_isopen");
            e.OrA();
            e.JrZLabel("rt_fb_r_in");
            e.XorA();
            e.LdMemA("rt_fo_isopen");
            e.CallAddress(0xBC8F);                   // CAS OUT CLOSE
            e.Label("rt_fb_r_in");
            e.LdAMem("rt_fl_isopen");
            e.OrA();
            e.JrZLabel("rt_fb_r_name");
            e.XorA();
            e.LdMemA("rt_fl_isopen");
            e.CallAddress(0xBC7A);                   // CAS IN CLOSE
            e.Label("rt_fb_r_name");
            e.PopHl();
            e.CallLabel("rt_file_name");             // HL = lettres, B = longueur
            e.LdAB();
            e.OrA();
            e.JrZLabel("rt_fb_r_open");
            e.PushBc();
            e.LdCB();
            e.LdBImm(0);
            e.LdDeImm(RunName);
            e.Ldir();
            e.PopBc();
            e.LdHlImm(RunName);                      // un point dans le nom ?
            e.LdCB();
            e.Label("rt_fb_r_dot");
            e.LdAHl();
            e.CpImm('.');
            e.JrZLabel("rt_fb_r_open");
            e.IncHl();
            e.DecC();
            e.JrNzLabel("rt_fb_r_dot");
            e.ExDeHl();                              // DE = fin du nom : « .BIN »
            e.LdHlLabel("rt_fb_bin");
            e.LdCImm(4);
            e.PushBc();
            e.LdBImm(0);
            e.Ldir();
            e.PopBc();
            e.LdAB();
            e.AddAImm(4);
            e.LdBA();
            e.Label("rt_fb_r_open");
            e.LdHlImm(RunName);
            e.LdDeImm(FileInBuffer);
            e.CallAddress(0xBC77);                   // CAS IN OPEN
            e.JpNcLabel("rt_file_fail");
            e.AndImm(0x0E);
            e.CpImm(0x02);
            e.JpNzLabel("rt_file_typeerr");          // seulement un fichier binaire
            e.LdMemDe("rt_fb_stub_at");              // adresse de chargement
            e.LdDeImm(0x100);                        // plus de caractères redessinés dans le programme qui part
            e.CallAddress(0xBBAB);                   // TXT SET M TABLE
            e.LdHlLabel("rt_fb_stub");
            e.LdDeImm(RunStub);
            e.LdBcImm(20);
            e.Ldir();
            e.JpAddress(RunStub);
            e.Label("rt_fb_stub");                   // recopié en &3800 (20 octets)
            e.Data([0x21]);                          // LD HL,adresse
            e.Label("rt_fb_stub_at");
            e.Data([0x00, 0x00]);
            e.Data([0xCD, 0x83, 0xBC]);              // CALL CAS IN DIRECT : HL = lancement
            e.Data([0x30, 0x06]);                    // JR NC,échec
            e.Data([0xE5]);                          // PUSH HL
            e.Data([0xCD, 0x7A, 0xBC]);              // CALL CAS IN CLOSE
            e.Data([0xE1]);                          // POP HL
            e.Data([0xE9]);                          // JP (HL)
            e.Data([0xCD, 0x18, 0xBB]);              // échec : CALL KM WAIT KEY
            e.Data([0xC3, 0x00, 0x00]);              // JP 0
            e.Label("rt_fb_bin");
            e.Data(System.Text.Encoding.ASCII.GetBytes(".BIN"));

            e.Label("rt_fb_m1");
            e.Data(Ascii("Memory full"));
            e.Label("rt_fb_m2");
            e.Data(Ascii("Improper argument"));
            foreach (string name in new[] { "rt_fb_a", "rt_fb_l", "rt_fb_e", "rt_fb_lim" })
            {
                e.Label(name);
                e.Reserve(2);
            }
            e.Label("rt_fb_has");
            e.Reserve(1);
        }),

        // GOSUB compte les appels en cours ; RETURN sans appel en cours : « Unexpected RETURN in N », comme le vrai BASIC
        // (lot I2b, D081). Le compteur tient sur un octet.
        [GosubName] = new RuntimeModule(GosubName, [ErrorName], e =>
        {
            e.Label(ReturnLabel);
            e.LdHlLabel(GosubCountLabel);
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_ret_bad");
            e.DecMemHl();
            e.Ret();                                 // retour du GOSUB
            e.Label("rt_ret_bad");
            e.LdHlLabel("rt_ret_msg");
            e.JpLabel(ErrorLabel);
            e.Label("rt_ret_msg");
            e.Data(Ascii("Unexpected RETURN"));
            e.Label(GosubCountLabel);
            e.Reserve(1);
        }),

        // Les messages d'erreur du Locomotive et leur numéro (lot I2, D080).
        //  rt_ec_code : HL = message -> A = numéro (0 s'il est inconnu).
        //  rt_raise   : ERROR numéro. HL = numéro (1 à 255, sinon Improper argument) : erreur de ce numéro, avec son
        //               message (« Unknown error » s'il n'en a pas) ; rt_ec_forced garde le numéro pour ERR.
        [ErrorCodesName] = new RuntimeModule(ErrorCodesName, [ErrorName], e =>
        {
            e.Label("rt_ec_code");
            e.LdDeLabel("rt_ec_table");
            e.Label("rt_ec_next");
            e.LdADe();
            e.OrA();
            e.RetZ();                                // fin du tableau : inconnu
            e.LdCA();                                // C = numéro de cette entrée
            e.IncDe();
            e.PushHl();
            e.Label("rt_ec_cmp");
            e.LdADe();
            e.CpMemHl();
            e.JrNzLabel("rt_ec_diff");
            e.OrA();
            e.JrZLabel("rt_ec_same");
            e.IncHl();
            e.IncDe();
            e.JrLabel("rt_ec_cmp");
            e.Label("rt_ec_same");
            e.PopHl();
            e.LdAC();
            e.Ret();
            e.Label("rt_ec_diff");                   // passe le reste du texte de l'entrée
            e.LdADe();
            e.IncDe();
            e.OrA();
            e.JrNzLabel("rt_ec_diff");
            e.PopHl();
            e.JrLabel("rt_ec_next");

            e.Label(RaiseLabel);
            e.LdAH();
            e.OrA();
            e.JrNzLabel("rt_ec_bad");
            e.OrL();
            e.JrZLabel("rt_ec_bad");
            e.LdAL();
            e.LdMemA("rt_ec_forced");
            e.LdCA();
            e.LdHlLabel("rt_ec_table");
            e.Label("rt_ec_find");
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_ec_unknown");
            e.CpC();
            e.IncHl();
            e.JpZLabel(ErrorLabel);                  // HL = texte du message
            e.Label("rt_ec_skip");
            e.LdAHl();
            e.IncHl();
            e.OrA();
            e.JrNzLabel("rt_ec_skip");
            e.JrLabel("rt_ec_find");
            e.Label("rt_ec_unknown");
            e.LdHlLabel("rt_ec_msg_unknown");
            e.JpLabel(ErrorLabel);
            e.Label("rt_ec_bad");
            e.LdHlLabel("rt_ec_msg_improper");
            e.JpLabel(ErrorLabel);

            e.Label("rt_ec_forced");
            e.Reserve(1);
            e.Label("rt_ec_msg_unknown");
            e.Data(Ascii("Unknown error"));
            e.Label("rt_ec_table");
            string[] messages =
            [
                "Unexpected NEXT", "Syntax error", "Unexpected RETURN", "DATA exhausted", "Improper argument",
                "Overflow", "Memory full", "Line does not exist", "Subscript out of range", "Array already dimensioned",
                "Division by zero", "Invalid direct command", "Type mismatch", "String space full", "String too long",
                "String expression too complex", "Cannot CONTinue", "Unknown user function", "RESUME missing",
                "Unexpected RESUME", "Direct command found", "Operand missing", "Line too long", "EOF met",
                "File type error", "NEXT missing", "File already open", "Unknown command", "WEND missing",
                "Unexpected WEND", "File not open", "Broken in"
            ];
            for (int k = 0; k < messages.Length; k++)
            {
                if (k == 4)
                    e.Label("rt_ec_msg_improper");   // texte de l'entrée 5, réutilisé
                e.Data([(byte)(k + 1)]);
                e.Data(Ascii(messages[k]));
            }
            e.Data([0]);
        }),

        // ON ERROR GOTO, RESUME, ERR, ERL (lot I2, D080), comme le vrai BASIC (SugarBox, 16 nov. 2026).
        // Chaque instruction BASIC range, à son début, la pile et l'adresse de sa fiche (ligne, début, instruction
        // suivante). Une erreur (DE = message) : si une erreur est déjà en cours de traitement, elle s'affiche et le
        // programme s'arrête ; sinon ERR et ERL sont rangés, la pile est remise comme au début de l'instruction, et le
        // programme saute à la ligne du ON ERROR GOTO. RESUME : refait l'instruction, RESUME NEXT : la suivante.
        [OnErrorName] = new RuntimeModule(OnErrorName, [ErrorName, ErrorCodesName], e =>
        {
            e.Label(OnErrorTrapLabel);
            e.LdAMem("rt_oe_active");
            e.OrA();
            e.JrZLabel("rt_oe_go");
            e.ExDeHl();
            e.JpLabel(ErrorShowLabel);               // erreur pendant le traitement d'une erreur : arrêt
            e.Label("rt_oe_go");
            e.IncA();
            e.LdMemA("rt_oe_active");
            e.LdAMem("rt_ec_forced");                // ERROR n : numéro donné
            e.OrA();
            e.JrNzLabel("rt_oe_have");
            e.ExDeHl();
            e.CallLabel("rt_ec_code");
            e.Label("rt_oe_have");
            e.LdLA();
            e.LdHZero();
            e.LdMemHl(ErrLabel);
            e.XorA();
            e.LdMemA("rt_ec_forced");
            e.LdHlMem(OnErrorRecordLabel);           // fiche et pile de l'instruction fautive, mises de côté : les
            e.LdMemHl("rt_oe_failrec");              // instructions du sous-programme d'erreur rangent les leurs
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.LdMemDe(ErlLabel);
            e.LdHlMem(OnErrorSpLabel);
            e.LdMemHl("rt_oe_failsp");
            e.LdSpMem(OnErrorSpLabel);
            e.LdHlMem(OnErrorHandlerLabel);
            e.JpHl();

            // RESUME : refait l'instruction en erreur ; RESUME NEXT : passe à la suivante
            e.Label("rt_oe_resume");
            e.CallLabel("rt_oe_resume_line");
            e.LdDeImm(2);
            e.JrLabel("rt_oe_jump");
            e.Label("rt_oe_resume_next");
            e.CallLabel("rt_oe_resume_line");
            e.LdDeImm(4);
            e.Label("rt_oe_jump");
            e.LdHlMem("rt_oe_failrec");
            e.AddHlDe();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.ExDeHl();
            e.LdSpMem("rt_oe_failsp");               // pile comme au début de l'instruction fautive
            e.JpHl();

            // RESUME ligne (et début des deux autres) : il faut une erreur en cours, sinon « Unexpected RESUME »
            e.Label("rt_oe_resume_line");
            e.LdAMem("rt_oe_active");
            e.OrA();
            e.JrZLabel("rt_oe_unexpected");
            e.XorA();
            e.LdMemA("rt_oe_active");
            e.Ret();
            e.Label("rt_oe_unexpected");
            e.LdHlLabel("rt_oe_msg_resume");
            e.JpLabel(ErrorLabel);

            e.Label("rt_oe_active");
            e.Reserve(1);
            foreach (string name in new[] { ErrLabel, ErlLabel, OnErrorSpLabel, OnErrorRecordLabel, OnErrorHandlerLabel,
                                            "rt_oe_failrec", "rt_oe_failsp" })
            {
                e.Label(name);
                e.Reserve(2);
            }
            e.Label("rt_oe_msg_resume");
            e.Data(Ascii("Unexpected RESUME"));
        }),

        // Minuteries (lot I1, D079), comme le vrai BASIC (SugarBox, 16 nov. 2026). Un compteur du firmware (KL ADD TICKER,
        // tous les 1/50 s, événement express) fait descendre les 4 minuteries ; au bout, la minuterie est marquée et
        // rechargée (EVERY) ou arrêtée (AFTER). Entre deux instructions, le programme appelle rt_ev_run si rt_ev_pending :
        // les sous-programmes partent de la minuterie 3 (la plus prioritaire) à 0. Pendant l'un d'eux, seules les
        // minuteries plus prioritaires peuvent partir ; DI les retient toutes jusqu'à EI.
        // Table : 8 octets par minuterie : active, compte (2), recharge (2), cible (2), marquée.
        [EventsName] = new RuntimeModule(EventsName, [ErrorName, GosubName], e => EmitEvents(e, false, false)),

        // Les mêmes, avec ON SQ(canal) GOSUB (lot I3a, D082) et / ou la touche ESC (lot I3b, D083) : remplacent le
        // module Events quand le programme s'en sert
        [EventsSoundName] = new RuntimeModule(EventsSoundName, [ErrorName, GosubName], e => EmitEvents(e, true, false)),
        [EventsBreakName] = new RuntimeModule(EventsBreakName, [ErrorName, GosubName], e => EmitEvents(e, false, true)),
        [EventsSoundBreakName] = new RuntimeModule(EventsSoundBreakName, [ErrorName, GosubName], e => EmitEvents(e, true, true)),

        // SYMBOL caractère, lignes (D075) : TXT SET MATRIX (&BBA8) ; caractère pas redessinable : « Improper argument »
        // (comme le vrai BASIC : SYMBOL 65,255 sans SYMBOL AFTER 65 ou moins).
        [SymbolName] = new RuntimeModule(SymbolName, [ErrorName], e =>
        {
            e.Label(SymbolLabel);
            e.LdHlLabel("rt_sym_r0");
            e.CallAddress(0xBBA8);
            e.RetC();
            e.LdHlLabel("rt_sym_msg");
            e.JpLabel(ErrorLabel);
            for (int k = 0; k < 8; k++)
            {
                e.Label($"rt_sym_r{k}");
                e.Reserve(1);
            }
            e.Label("rt_sym_msg");
            e.Data(Ascii("Improper argument"));
        }),

        // SYMBOL AFTER n (D075) : les dessins actuels des caractères n à 255 (TXT GET MATRIX &BBA5, lus avec la ROM basse
        // ouverte) sont copiés dans rt_sym_table, puis la table est donnée au firmware (TXT SET M TABLE &BBAB).
        // n = 256 : plus aucun caractère redessinable. Hors de 0 à 256 : « Improper argument ».
        [SymbolAfterName] = new RuntimeModule(SymbolAfterName, [ErrorName], e =>
        {
            e.Label(SymbolAfterLabel);
            e.LdAH();
            e.OrA();
            e.JrZLabel("rt_sa_ok");
            e.DecA();
            e.JrNzLabel("rt_sa_bad");
            e.LdAL();
            e.OrA();
            e.JrNzLabel("rt_sa_bad");
            e.LdDeImm(256);                          // SYMBOL AFTER 256 : pas de table
            e.JpAddress(0xBBAB);
            e.Label("rt_sa_ok");
            e.LdAL();
            e.LdMemA("rt_sa_n");
            e.LdMemA("rt_sa_c");
            e.LdHlLabel(SymbolTableLabel);
            e.LdMemHl("rt_sa_p");
            e.Label("rt_sa_copy");
            e.LdAMem("rt_sa_c");
            e.CallAddress(0xBBA5);                   // TXT GET MATRIX : HL = dessin actuel du caractère
            e.CallAddress(0xB906);                   // KL L ROM ENABLE : A = état d'avant
            e.PushAf();
            e.LdDeMem("rt_sa_p");
            e.LdBcImm(8);
            e.Ldir();
            e.LdMemDe("rt_sa_p");
            e.PopAf();
            e.CallAddress(0xB90C);                   // KL ROM RESTORE
            e.LdHlLabel("rt_sa_c");
            e.IncMemHl();
            e.JrNzLabel("rt_sa_copy");               // jusqu'au caractère 255
            e.LdAMem("rt_sa_n");
            e.LdEA();
            e.LdDZero();
            e.LdHlLabel(SymbolTableLabel);
            e.JpAddress(0xBBAB);
            e.Label("rt_sa_bad");
            e.LdHlLabel("rt_sa_msg");
            e.JpLabel(ErrorLabel);
            e.Label("rt_sa_n");
            e.Reserve(1);
            e.Label("rt_sa_c");
            e.Reserve(1);
            e.Label("rt_sa_p");
            e.Reserve(2);
            e.Label("rt_sa_msg");
            e.Data(Ascii("Improper argument"));
        }),

        [PrintFormatName] = new RuntimeModule(PrintFormatName, [ErrorName, PrintFitName], e =>
        {

            // B espaces (TXT OUTPUT garde tous les registres)
            e.Label("rt_pf_spaces");
            e.LdAB();
            e.OrA();
            e.RetZ();
            e.Label("rt_pf_spaces_loop");
            e.LdAImm(' ');
            e.CallAddress(TxtOutput);
            e.DjnzLabel("rt_pf_spaces_loop");
            e.Ret();

            e.Label(ZoneLabel);
            e.CallLabel("rt_pf_width");
            e.LdMemA("rt_pf_w");
            e.CallAddress(TxtGetCursor);
            e.CallAddress(TxtValidate);             // après une ligne remplie : colonne 1
            e.LdAH();
            e.LdMemA("rt_pf_col");
            e.LdEA();
            e.LdDZero();                             // DE = colonne
            e.LdAMem("rt_zone_w");
            e.LdCA();
            e.LdBImm(0);                             // BC = largeur des zones
            e.LdHlImm(1);
            e.Label("rt_zone_next");
            e.AddHlBc();                             // HL = début de zone suivant
            e.PushHl();
            e.OrA();
            e.SbcHlDe();
            e.PopHl();
            e.JrCLabel("rt_zone_next");
            e.JrZLabel("rt_zone_next");              // jusqu'à dépasser la colonne actuelle
            e.PushHl();
            e.AddHlBc();
            e.DecHl();                               // HL = dernière colonne de la zone
            e.LdAMem("rt_pf_w");
            e.LdEA();
            e.LdDZero();
            e.OrA();
            e.SbcHlDe();
            e.PopHl();
            e.JrCLabel("rt_zone_fits");
            e.JrZLabel("rt_zone_fits");
            e.JpLabel("rt_pf_newline");              // la zone ne tient pas : à la ligne
            e.Label("rt_zone_fits");
            e.LdAMem("rt_pf_col");
            e.LdBA();
            e.LdAL();
            e.SubB();
            e.LdBA();
            e.JpLabel("rt_pf_spaces");

            e.Label(TabLabel);
            e.Bit7H();
            e.JrNzLabel("rt_tab_one");
            e.LdAH();
            e.OrL();
            e.JrNzLabel("rt_tab_pos");
            e.Label("rt_tab_one");
            e.LdHlImm(1);
            e.Label("rt_tab_pos");
            e.PushHl();
            e.CallLabel("rt_pf_width");
            e.PopHl();
            e.LdEA();
            e.LdDZero();                             // DE = largeur
            e.Label("rt_tab_reduce");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_tab_back");
            e.JrZLabel("rt_tab_back");
            e.JrLabel("rt_tab_reduce");
            e.Label("rt_tab_back");
            e.AddHlDe();                             // 1 <= colonne <= largeur
            e.LdAL();
            e.LdMemA("rt_pf_n");
            e.CallAddress(TxtGetCursor);
            e.CallAddress(TxtValidate);
            e.LdAMem("rt_pf_n");
            e.SubH();
            e.RetZ();                                // déjà à la bonne colonne
            e.JrCLabel("rt_tab_newline");
            e.LdBA();
            e.JpLabel("rt_pf_spaces");
            e.Label("rt_tab_newline");               // colonne dépassée : à la ligne, puis colonne - 1 espaces
            e.CallLabel("rt_pf_newline");
            e.LdAMem("rt_pf_n");
            e.DecA();
            e.LdBA();
            e.JpLabel("rt_pf_spaces");

            e.Label(SpcLabel);
            e.Bit7H();
            e.RetNz();
            e.PushHl();
            e.CallLabel("rt_pf_width");
            e.PopHl();
            e.LdEA();
            e.LdDZero();
            e.Label("rt_spc_reduce");
            e.OrA();
            e.SbcHlDe();
            e.JrNcLabel("rt_spc_reduce");
            e.AddHlDe();                             // reste de la division par la largeur
            e.LdAL();
            e.LdBA();
            e.JpLabel("rt_pf_spaces");

            e.Label(ZoneSetLabel);
            e.LdAH();
            e.OrA();
            e.JrNzLabel("rt_zone_bad");
            e.OrL();
            e.JrZLabel("rt_zone_bad");
            e.LdAL();
            e.LdMemA("rt_zone_w");
            e.Ret();
            e.Label("rt_zone_bad");
            e.LdHlLabel("rt_zone_msg");
            e.JpLabel(ErrorLabel);
            e.Label("rt_zone_msg");
            e.Data(Ascii("Improper argument"));

            e.Label("rt_zone_w");
            e.Data([13]);                            // 13 colonnes au départ, comme le Locomotive
            foreach (string scratch in new[] { "rt_pf_col", "rt_pf_n" })
            {
                e.Label(scratch);
                e.Reserve(1);
            }
        }),

        [ErrorName] = new RuntimeModule(ErrorName, [PrintStringName], e =>
        {
            // ON ERROR GOTO (D080) : si une adresse d'interception est rangée, on y va avec DE = message
            e.Label(ErrorLabel);
            e.LdDeMem(ErrorTrapLabel);
            e.LdAD();
            e.OrE();
            e.JrZLabel(ErrorShowLabel);
            e.ExDeHl();
            e.JpHl();

            // Erreur qui laisse continuer (Overflow, Division by zero) : interceptée par ON ERROR, sinon le message
            // s'écrit là où est le curseur, puis on passe à la ligne et le programme continue (D080)
            e.Label(ErrorSoftLabel);
            e.LdDeMem(ErrorTrapLabel);
            e.LdAD();
            e.OrE();
            e.JrZLabel("rt_err_warn");
            e.ExDeHl();
            e.JpHl();
            e.Label("rt_err_warn");
            e.CallLabel(PrintStringLabel);
            e.LdHlLabel("rt_err_crlf");
            e.JpLabel(PrintStringLabel);

            e.Label(ErrorShowLabel);
            e.PushHl();
            e.XorA();                                // le message s'affiche dans la fenêtre #0, comme le Locomotive (D072)
            e.CallAddress(TxtStrSelect);
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
            e.Label(ErrorTrapLabel);
            e.Reserve(2);
            e.Label(BreakMessageLabel);
            e.Data(Ascii("Break"));
            e.Label("rt_err_in");
            e.Data(Ascii(" in "));
            e.Label("rt_err_crlf");
            e.Data(Ascii("\r\n"));
        }),

        // READ : HL = nombre suivant des DATA ; « DATA exhausted » s'il n'y en a plus (D059).
        // Le générateur place les éléments entre data_table et data_end ; RESTORE change rt_data_ptr.
        // Un élément (D064) : nombre = 0, valeur (2 octets), longueur et lettres de son écriture ; texte = 1, longueur, lettres.
        [DataName] = new RuntimeModule(DataName, [ErrorName], e =>
        {
            e.Label(ReadLabel);
            e.LdHlMem(DataPointerLabel);
            e.LdDeLabel(DataEndLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrNcLabel("rt_read_none");             // plus rien à lire
            e.LdAHl();                               // 0 : nombre (valeur, puis son écriture) ; 1 : texte (D064)
            e.IncHl();                               // 2 : nombre à virgule (D070, module DataReal)
            e.OrA();
            e.JrZLabel("rt_read_int");
            e.CpImm(2);
            e.JpZLabel("rt_read_real");
            e.JrLabel("rt_read_text");
            e.Label("rt_read_int");
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.IncHl();
            e.LoadCFromHl();                         // on saute l'écriture du nombre
            e.IncHl();
            e.LdBImm(0);
            e.AddHlBc();
            e.LdMemHl(DataPointerLabel);
            e.ExDeHl();
            e.Ret();
            e.Label("rt_read_text");                 // READ A sur un texte qui n'est pas un nombre
            e.LdHlLabel("rt_read_msg_type");
            e.JpLabel(ErrorLabel);
            e.Label("rt_read_none");
            e.LdHlLabel("rt_read_msg");
            e.JpLabel(ErrorLabel);

            e.Label(DataPointerLabel);
            e.Word(DataTableLabel);                  // au départ : le premier élément
            e.Label("rt_read_msg");
            e.Data(Ascii("DATA exhausted"));
            e.Label("rt_read_msg_type");
            e.Data(Ascii("Type mismatch"));
        }),

        // READ A$ (D064) : HL = fiche temporaire du texte suivant des DATA (un nombre est rendu tel qu'il est écrit).
        // La fiche pointe directement dans la table des DATA : rien n'est copié.
        [DataStringName] = new RuntimeModule(DataStringName, [DataName, StringsName], e =>
        {
            e.Label(ReadStringLabel);
            e.LdHlMem(DataPointerLabel);
            e.LdDeLabel(DataEndLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JpNcLabel("rt_read_none");
            e.LdAHl();
            e.IncHl();
            e.CpImm(1);
            e.JrZLabel("rt_read_str_text");
            e.OrA();
            e.JrZLabel("rt_read_str_int");
            e.IncHl();                               // nombre à virgule : 5 octets à sauter (D070)
            e.IncHl();
            e.IncHl();
            e.Label("rt_read_str_int");
            e.IncHl();                               // nombre : on saute sa valeur
            e.IncHl();
            e.Label("rt_read_str_text");
            e.LoadCFromHl();
            e.IncHl();
            e.PushHl();                              // lettres
            e.LdBImm(0);
            e.AddHlBc();
            e.LdMemHl(DataPointerLabel);
            e.CallLabel("rt_str_newtmp");            // garde C
            e.PopDe();
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.DecHl();
            e.DecHl();
            e.Ret();
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

        // Mémoire libre (D058, D062) : les tableaux montent depuis la fin du programme (rt_arr_heap), les textes descendent
        // depuis la limite (rt_str_ptr). rt_arr_limit : &A67C, ou l'adresse donnée par MEMORY + 1.
        //  rt_arr_memory : MEMORY. HL = adresse : la mémoire libre s'arrête là.
        [HeapName] = new RuntimeModule(HeapName, [ErrorName], e =>
        {
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
            e.ExDeHl();                              // DE = nouvelle limite
            e.LdHlMem(ArrayHeapLimitLabel);          // des textes sont déjà rangés sous l'ancienne limite ?
            e.PushDe();
            e.LdDeMem(StringPointerLabel);
            e.OrA();
            e.SbcHlDe();
            e.PopDe();
            e.JpNzLabel(ErrMemory);                  // oui : on ne peut pas les déplacer (pas encore)
            e.ExDeHl();
            e.LdMemHl(ArrayHeapLimitLabel);
            e.LdMemHl(StringPointerLabel);           // les textes partiront de la nouvelle limite
            e.Ret();

            e.Label(ErrMemory);
            e.LdHlLabel("rt_heap_msg_mem");
            e.JpLabel(ErrorLabel);

            e.Label(ArrayHeapLabel);
            e.Reserve(2);
            e.Label(ArrayHeapLimitLabel);
            e.Word(DefaultHeapLimit);
            e.Label(StringPointerLabel);
            e.Word(DefaultHeapLimit);
            e.Label("rt_heap_msg_mem");
            e.Data(Ascii("Memory full"));
        }),

        // Textes (lot E, D062), rangés comme le Locomotive : chaque texte a une fiche de 3 octets (longueur 0 à 255, puis
        // adresse des lettres). Un texte écrit dans le programme reste où il est (la fiche pointe dessus). Les autres sont
        // rangés en haut de la mémoire libre, en descendant depuis rt_arr_limit : chaque bloc = les lettres, puis la
        // longueur et l'adresse de la fiche qui le possède (3 octets). Une valeur texte, pendant un calcul, est l'adresse
        // d'une fiche (HL) : celle d'une variable, d'un texte du programme, ou une fiche temporaire (16 au plus par
        // instruction, rendues par rt_str_reset au début de l'instruction suivante).
        // Ménage (rt_str_gc) : quand la place manque, on parcourt les blocs de haut en bas ; un bloc dont la fiche pointe
        // encore sur lui est gardé et remonté, les autres sont oubliés. Les fiches sont mises à jour.
        //  rt_str_alloc  : C = longueur (1 à 255), DE = fiche propriétaire -> HL = adresse des lettres. Garde C et DE.
        //  rt_str_concat : DE = fiche gauche, HL = fiche droite -> HL = fiche temporaire de A$ + B$.
        //  rt_str_assign : HL = fiche source, DE = fiche de la variable : la variable reçoit le texte.
        //  rt_str_print  : HL = fiche : affiche le texte.
        //  rt_str_cmp    : DE = fiche gauche, HL = fiche droite -> HL = -1, 0 ou 1 (ordre des codes des caractères).
        [StringsName] = new RuntimeModule(StringsName, [ErrorName, HeapName], e =>
        {
            e.Label(StrResetLabel);
            e.LdHlMem("rt_str_tmpptr");
            e.LdDeLabel("rt_str_tmp");
            e.OrA();
            e.SbcHlDe();
            e.RetZ();                                // aucune fiche temporaire utilisée
            e.LdBH();
            e.LdCL();                                // BC = octets utilisés
            e.ExDeHl();
            e.LdMemHl("rt_str_tmpptr");
            e.Label("rt_str_reset_loop");            // les fiches remises à 0 : leurs blocs seront oubliés au ménage
            e.StoreHlImm(0);
            e.IncHl();
            e.DecBc();
            e.LdAB();
            e.OrC();
            e.JrNzLabel("rt_str_reset_loop");
            e.Ret();

            e.Label("rt_str_newtmp");                // HL = nouvelle fiche temporaire (vide). Garde C.
            e.LdHlMem("rt_str_tmpptr");
            e.PushHl();
            e.IncHl();
            e.IncHl();
            e.IncHl();
            e.LdDeLabel("rt_str_tmp_end");
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrCLabel("rt_str_newtmp_ok");
            e.JpNzLabel("rt_str_err_complex");       // plus de 16 textes intermédiaires dans une instruction
            e.Label("rt_str_newtmp_ok");
            e.LdMemHl("rt_str_tmpptr");
            e.PopHl();
            e.Ret();

            e.Label(StrAllocLabel);
            e.CallLabel("rt_str_try");
            e.RetNc();
            e.PushBc();
            e.PushDe();
            e.CallLabel(StrGcLabel);                 // pas de place : ménage, puis on réessaie
            e.PopDe();
            e.PopBc();
            e.CallLabel("rt_str_try");
            e.RetNc();
            e.JpLabel(ErrMemory);

            e.Label("rt_str_try");                   // retenue à 1 s'il n'y a pas la place
            e.PushDe();
            e.LdHZero();
            e.LdLC();
            e.IncHl();
            e.IncHl();
            e.IncHl();
            e.ExDeHl();                              // DE = longueur + 3
            e.LdHlMem(StringPointerLabel);
            e.OrA();
            e.SbcHlDe();                             // HL = début du nouveau bloc
            e.JrCLabel("rt_str_try_fail");
            e.LdDeMem(ArrayHeapLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrCLabel("rt_str_try_fail");           // il descendrait sur les tableaux
            e.LdMemHl(StringPointerLabel);
            e.PopDe();
            e.PushHl();
            e.LdAL();
            e.AddAC();
            e.LdLA();
            e.LdAH();
            e.AdcAImm(0);
            e.LdHA();                                // HL = juste après les lettres
            e.StoreHlC();                            // longueur, puis adresse de la fiche propriétaire
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.PopHl();
            e.OrA();
            e.Ret();
            e.Label("rt_str_try_fail");
            e.PopDe();
            e.Scf();
            e.Ret();

            e.Label(StrGcLabel);
            e.LdHlMem(ArrayHeapLimitLabel);
            e.LdMemHl("rt_gc_dest");                 // les blocs gardés sont remontés à partir d'ici
            e.LdMemHl("rt_gc_cur");                  // bloc examiné (on descend)
            e.Label("rt_gc_loop");
            e.LdHlMem("rt_gc_cur");
            e.LdDeMem(StringPointerLabel);
            e.OrA();
            e.SbcHlDe();
            e.JrZLabel("rt_gc_done");
            e.LdHlMem("rt_gc_cur");
            e.DecHl();
            e.LoadDFromHl();
            e.DecHl();
            e.LoadEFromHl();                         // DE = fiche propriétaire
            e.DecHl();
            e.LoadCFromHl();                         // C = longueur
            e.LdBImm(0);
            e.OrA();
            e.SbcHlBc();                             // HL = début du bloc
            e.LdMemHl("rt_gc_start");
            e.ExDeHl();                              // HL = fiche, DE = début du bloc
            e.LdAHl();                               // la fiche pointe-t-elle encore sur ce bloc ?
            e.SubC();
            e.JrNzLabel("rt_gc_dead");
            e.IncHl();
            e.LdAHl();
            e.SubE();
            e.JrNzLabel("rt_gc_dead");
            e.IncHl();
            e.LdAHl();
            e.SubD();
            e.JrNzLabel("rt_gc_dead");
            e.PushHl();                              // fiche + 2
            e.LdHZero();
            e.LdLC();
            e.IncHl();
            e.IncHl();
            e.IncHl();
            e.LdBH();
            e.LdCL();                                // BC = taille du bloc
            e.LdHlMem("rt_gc_dest");
            e.DecHl();
            e.ExDeHl();                              // DE = dernier octet de la place d'arrivée
            e.LdHlMem("rt_gc_cur");
            e.DecHl();                               // HL = dernier octet du bloc
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrZLabel("rt_gc_same");                // déjà à sa place
            e.Lddr();
            e.IncDe();                               // DE = nouveau début
            e.JrLabel("rt_gc_moved");
            e.Label("rt_gc_same");
            e.LdDeMem("rt_gc_start");
            e.Label("rt_gc_moved");
            e.PopHl();
            e.StoreHlD();                            // la fiche suit son texte
            e.DecHl();
            e.StoreHlE();
            e.LdMemDe("rt_gc_dest");
            e.Label("rt_gc_dead");
            e.LdHlMem("rt_gc_start");
            e.LdMemHl("rt_gc_cur");
            e.JrLabel("rt_gc_loop");
            e.Label("rt_gc_done");
            e.LdHlMem("rt_gc_dest");
            e.LdMemHl(StringPointerLabel);
            e.Ret();

            e.Label(StrConcatLabel);
            e.LdMemDe("rt_str_a");
            e.LdMemHl("rt_str_b");
            e.LdADe();
            e.AddAMemHl();                           // A = longueur totale
            e.JpCLabel("rt_str_err_long");           // plus de 255 caractères
            e.LdCA();
            e.CallLabel("rt_str_newtmp");
            e.LdMemHl("rt_str_dst");
            e.LdAC();
            e.OrA();
            e.RetZ();                                // deux textes vides
            e.ExDeHl();
            e.CallLabel(StrAllocLabel);              // le ménage a pu déplacer les deux textes : on relit leurs fiches
            e.ExDeHl();
            e.LdHlMem("rt_str_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.LdHlMem("rt_str_a");
            e.CallLabel("rt_str_copy");
            e.LdHlMem("rt_str_b");
            e.CallLabel("rt_str_copy");
            e.LdHlMem("rt_str_dst");
            e.Ret();

            e.Label("rt_str_copy");                  // HL = fiche, DE = destination : copie les lettres, DE avance
            e.LoadCFromHl();
            e.LdBImm(0);
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.Ldir();
            e.Ret();

            e.Label(StrAssignLabel);
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.RetZ();                                // A$ = A$
            e.LdMemHl("rt_str_a");
            e.LdMemDe("rt_str_dst");
            e.LdAHl();
            e.OrA();
            e.JrNzLabel("rt_str_as_full");
            e.ExDeHl();                              // texte vide
            e.StoreHlImm(0);
            e.IncHl();
            e.StoreHlImm(0);
            e.IncHl();
            e.StoreHlImm(0);
            e.Ret();
            e.Label("rt_str_as_full");
            e.LdCA();
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();                         // DE = lettres de la source
            e.LdHlMem(StringPointerLabel);           // lettres sous la zone des textes (programme, DATA) : on pointe dessus
            e.ExDeHl();
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.ExDeHl();
            e.JrCLabel("rt_str_as_set");
            e.LdHlMem("rt_str_a");                   // fiche temporaire ? la variable prend son bloc tel quel
            e.PushDe();
            e.LdDeLabel("rt_str_tmp");
            e.OrA();
            e.SbcHlDe();
            e.AddHlDe();
            e.JrCLabel("rt_str_as_nottmp");
            e.LdDeLabel("rt_str_tmp_end");
            e.OrA();
            e.SbcHlDe();
            e.JrNcLabel("rt_str_as_nottmp");
            e.PopHl();
            e.PushHl();                              // HL = lettres
            e.LdBImm(0);
            e.AddHlBc();
            e.IncHl();                               // HL = adresse de la fiche propriétaire, dans le bloc
            e.LdDeMem("rt_str_dst");
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.PopDe();
            e.JrLabel("rt_str_as_set");
            e.Label("rt_str_as_nottmp");
            e.PopDe();
            e.LdDeMem("rt_str_dst");
            e.CallLabel(StrAllocLabel);              // texte d'une autre variable : on le copie
            e.PushHl();
            e.ExDeHl();
            e.LdHlMem("rt_str_a");                   // relue après un éventuel ménage
            e.CallLabel("rt_str_copy");
            e.PopDe();
            e.LdHlMem("rt_str_a");
            e.LoadCFromHl();
            e.Label("rt_str_as_set");                // fiche de la variable = longueur C, lettres DE
            e.LdHlMem("rt_str_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.Ret();

            e.Label(StrPrintLabel);
            e.LoadBFromHl();
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LdAB();
            e.OrA();
            e.RetZ();
            e.Label("rt_str_print_loop");
            e.LdAHl();
            e.CallAddress(TxtOutput);
            e.IncHl();
            e.DjnzLabel("rt_str_print_loop");
            e.Ret();

            // Comme rt_str_print, sans l'espace du début s'il y en a un (WRITE 12 : « 12 », lot H2, D073)
            e.Label(StrPrintNoSpaceLabel);
            e.LoadBFromHl();
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LdAB();
            e.OrA();
            e.RetZ();
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_str_print_loop");
            e.IncHl();
            e.DecB();
            e.JrNzLabel("rt_str_print_loop");
            e.Ret();

            e.Label(StrCmpLabel);
            e.LoadCFromHl();
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();                                // HL = lettres de droite, C = leur nombre
            e.PushHl();
            e.ExDeHl();
            e.LoadBFromHl();
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();                         // DE = lettres de gauche, B = leur nombre
            e.PopHl();
            e.Label("rt_str_cmp_loop");
            e.LdAB();
            e.OrA();
            e.JrZLabel("rt_str_cmp_lend");
            e.LdAC();
            e.OrA();
            e.JrZLabel("rt_str_cmp_gt");             // la droite est finie : la gauche est plus grande
            e.LdADe();
            e.CpMemHl();
            e.JrCLabel("rt_str_cmp_lt");
            e.JrNzLabel("rt_str_cmp_gt");
            e.IncDe();
            e.IncHl();
            e.DecB();
            e.DecC();
            e.JrLabel("rt_str_cmp_loop");
            e.Label("rt_str_cmp_lend");
            e.LdAC();
            e.OrA();
            e.JrZLabel("rt_str_cmp_eq");
            e.Label("rt_str_cmp_lt");
            e.LdHlImm(0xFFFF);
            e.Ret();
            e.Label("rt_str_cmp_gt");
            e.LdHlImm(1);
            e.Ret();
            e.Label("rt_str_cmp_eq");
            e.LdHlImm(0);
            e.Ret();

            e.Label("rt_str_err_long");
            e.LdHlLabel("rt_str_msg_long");
            e.JpLabel(ErrorLabel);
            e.Label("rt_str_err_complex");
            e.LdHlLabel("rt_str_msg_complex");
            e.JpLabel(ErrorLabel);

            e.Label("rt_str_tmpptr");
            e.Word("rt_str_tmp");
            foreach (string scratch in new[] { "rt_str_a", "rt_str_b", "rt_str_dst", "rt_gc_dest", "rt_gc_cur", "rt_gc_start" })
            {
                e.Label(scratch);
                e.Reserve(2);
            }
            e.Label("rt_str_tmp");
            e.Reserve(16 * 3);
            e.Label("rt_str_tmp_end");
            e.Label("rt_str_msg_long");
            e.Data(Ascii("String too long"));
            e.Label("rt_str_msg_complex");
            e.Data(Ascii("String expression too complex"));
        }),

        // Fonctions de textes (étape E2, D063), comme le Locomotive. Les résultats texte sont des fiches temporaires.
        //  rt_left, rt_right : DE = fiche, HL = nombre de caractères (0 à 255).
        //  rt_mid   : DE = fiche, HL = début (1 à 255), BC = longueur (0 à 255 ; 255 si elle est omise).
        //  rt_chr, rt_space, rt_strs : HL = nombre.      rt_string : HL = nombre, DE = code du caractère.
        //  rt_upper, rt_lower : HL = fiche.               rt_hex, rt_bin : HL = nombre, DE = chiffres (0 à 16).
        //  rt_asc, rt_val : HL = fiche -> HL = nombre.    rt_instr : BC = début, DE = fiche du texte, HL = fiche cherchée.
        // Une valeur hors limites donne « Improper argument », comme le Locomotive.
        [StrFunctionsName] = new RuntimeModule(StrFunctionsName, [StringsName, ErrorName], e =>
        {
            // A = octet de HL (0 à 255), sinon Improper argument
            e.Label("rt_sf_byte");
            e.LdAH();
            e.OrA();
            e.JpNzLabel("rt_sf_improper");
            e.LdAL();
            e.Ret();

            // Morceau d'un texte : DE = fiche source, B = premier caractère (à partir de 0), C = nombre -> HL = fiche temporaire
            e.Label("rt_sf_sub");
            e.LdMemDe("rt_sf_src");
            e.LdAB();
            e.LdMemA("rt_sf_off");
            e.CallLabel("rt_str_newtmp");
            e.LdMemHl("rt_sf_dst");
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.ExDeHl();
            e.CallLabel(StrAllocLabel);              // le ménage a pu déplacer la source : on relit sa fiche ensuite
            e.PushHl();
            e.ExDeHl();
            e.LdHlMem("rt_sf_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.LdHlMem("rt_sf_src");
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LdAMem("rt_sf_off");
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();
            e.PopDe();
            e.LdBImm(0);
            e.Ldir();
            e.LdHlMem("rt_sf_dst");
            e.Ret();

            // Texte copié depuis la mémoire (hors zone des textes) : DE = lettres, C = nombre -> HL = fiche temporaire
            e.Label("rt_sf_mem");
            e.LdMemDe("rt_sf_src");
            e.CallLabel("rt_str_newtmp");
            e.LdMemHl("rt_sf_dst");
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.ExDeHl();
            e.CallLabel(StrAllocLabel);
            e.PushHl();
            e.ExDeHl();
            e.LdHlMem("rt_sf_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.PopDe();
            e.LdHlMem("rt_sf_src");
            e.LdBImm(0);
            e.Ldir();
            e.LdHlMem("rt_sf_dst");
            e.Ret();

            // C fois le caractère E -> HL = fiche temporaire
            e.Label("rt_sf_fill");
            e.LdAE();
            e.LdMemA("rt_sf_char");
            e.CallLabel("rt_str_newtmp");
            e.LdMemHl("rt_sf_dst");
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.ExDeHl();
            e.CallLabel(StrAllocLabel);
            e.PushHl();
            e.ExDeHl();
            e.LdHlMem("rt_sf_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.PopHl();
            e.LdBC();
            e.LdAMem("rt_sf_char");
            e.Label("rt_sf_fill_loop");
            e.LdHlA();
            e.IncHl();
            e.DjnzLabel("rt_sf_fill_loop");
            e.LdHlMem("rt_sf_dst");
            e.Ret();

            e.Label("rt_left");
            e.CallLabel("rt_sf_byte");
            e.LdCA();
            e.LdADe();
            e.CpC();
            e.JrNcLabel("rt_left_ok");
            e.LdCA();                                // plus que la longueur : tout le texte
            e.Label("rt_left_ok");
            e.LdBImm(0);
            e.JpLabel("rt_sf_sub");

            e.Label("rt_right");
            e.CallLabel("rt_sf_byte");
            e.LdCA();
            e.LdADe();
            e.CpC();
            e.JrNcLabel("rt_right_ok");
            e.LdCA();
            e.Label("rt_right_ok");
            e.LdADe();
            e.SubC();
            e.LdBA();                                // départ = longueur - nombre
            e.JpLabel("rt_sf_sub");

            e.Label("rt_mid");
            e.CallLabel("rt_sf_byte");
            e.OrA();
            e.JpZLabel("rt_sf_improper");            // MID$(A$, 0) : Improper argument
            e.DecA();
            e.PushAf();                              // départ (à partir de 0)
            e.LdHB();
            e.LdLC();
            e.CallLabel("rt_sf_byte");
            e.LdCA();                                // longueur demandée
            e.PopAf();
            e.LdBA();
            e.LdADe();
            e.SubB();                                // caractères disponibles après le départ
            e.JrNcLabel("rt_mid_has");
            e.XorA();                                // départ après la fin : texte vide
            e.Label("rt_mid_has");
            e.CpC();
            e.JrNcLabel("rt_mid_ok");
            e.LdCA();
            e.Label("rt_mid_ok");
            e.JpLabel("rt_sf_sub");

            e.Label("rt_inkey");                     // INKEY$ (D064) : touche tapée (sans attendre), ou ""
            e.CallAddress(KmReadChar);
            e.LdCImm(0);
            e.JrNcLabel("rt_inkey_none");
            e.LdEA();
            e.LdCImm(1);
            e.Label("rt_inkey_none");
            e.JpLabel("rt_sf_fill");

            e.Label("rt_chr");
            e.CallLabel("rt_sf_byte");
            e.LdEA();
            e.LdCImm(1);
            e.JpLabel("rt_sf_fill");

            e.Label("rt_space");
            e.CallLabel("rt_sf_byte");
            e.LdCA();
            e.LdEImm(' ');
            e.JpLabel("rt_sf_fill");

            e.Label("rt_string");
            e.PushDe();
            e.CallLabel("rt_sf_byte");
            e.LdCA();
            e.PopHl();
            e.CallLabel("rt_sf_byte");               // code du caractère : 0 à 255
            e.LdEA();
            e.JpLabel("rt_sf_fill");

            e.Label("rt_asc");
            e.LdAHl();
            e.OrA();
            e.JpZLabel("rt_sf_improper");            // ASC("") : Improper argument
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LoadLFromHl();
            e.LdHZero();
            e.Ret();

            // STR$ : espace (ou « - ») puis les chiffres, comme PRINT mais sans espace à la fin
            e.Label("rt_strs");
            e.XorA();
            e.LdMemA("rt_sf_started");
            e.LdDeLabel("rt_sf_buf");
            e.LdAImm(' ');
            e.Bit7H();
            e.JrZLabel("rt_strs_pos");
            e.LdAL();                                // valeur absolue (32768 compris, en non signé)
            e.Cpl();
            e.LdLA();
            e.LdAH();
            e.Cpl();
            e.LdHA();
            e.IncHl();
            e.LdAImm('-');
            e.Label("rt_strs_pos");
            e.StoreDeA();
            e.IncDe();
            foreach (int power in new[] { 10000, 1000, 100, 10 })
            {
                e.LdBcImm(-power & 0xFFFF);
                e.CallLabel("rt_sf_digit");
            }
            e.LdAL();
            e.AddAImm('0');
            e.StoreDeA();
            e.IncDe();
            e.ExDeHl();
            e.LdDeLabel("rt_sf_buf");
            e.OrA();
            e.SbcHlDe();
            e.LdCL();                                // C = nombre de caractères
            e.JpLabel("rt_sf_mem");
            e.Label("rt_sf_digit");                  // chiffre de HL pour la puissance -BC, rangé en (DE) sauf zéros du début
            e.LdAImm('0' - 1);
            e.Label("rt_sf_digit_loop");
            e.IncA();
            e.AddHlBc();
            e.JrCLabel("rt_sf_digit_loop");
            e.SbcHlBc();
            e.CpImm('0');
            e.JrNzLabel("rt_sf_digit_put");
            e.PushAf();
            e.LdAMem("rt_sf_started");
            e.OrA();
            e.JrNzLabel("rt_sf_digit_zero");
            e.PopAf();
            e.Ret();
            e.Label("rt_sf_digit_zero");
            e.PopAf();
            e.Label("rt_sf_digit_put");
            e.StoreDeA();
            e.IncDe();
            e.LdAImm(1);
            e.LdMemA("rt_sf_started");
            e.Ret();

            // HEX$ et BIN$ : chiffres écrits à droite d'une zone de 16 « 0 », puis on garde ce qu'il faut
            foreach ((string name, int digits, int bits) in new[] { ("rt_hex", 4, 4), ("rt_bin", 16, 1) })
            {
                e.Label(name);
                e.LdAD();
                e.OrA();
                e.JpNzLabel("rt_sf_improper");
                e.LdAE();
                e.CpImm(17);
                e.JpNcLabel("rt_sf_improper");       // 16 chiffres au plus
                e.LdMemA("rt_sf_width");
                e.CallLabel("rt_sf_zeros");
                e.LdDeLabel("rt_sf_buf16_end");
                e.DecDe();
                e.LdBImm(digits);
                e.Label(name + "_loop");
                e.LdAL();
                e.AndImm((1 << bits) - 1);
                e.AddAImm('0');
                if (bits == 4)
                {
                    e.CpImm('9' + 1);
                    e.JrCLabel(name + "_digit");
                    e.AddAImm(7);                    // A à F
                    e.Label(name + "_digit");
                }
                e.StoreDeA();
                e.DecDe();
                for (int k = 0; k < bits; k++)
                {
                    e.SrlH();
                    e.RrL();
                }
                e.DjnzLabel(name + "_loop");
                e.JrLabel("rt_sf_trim");
            }
            e.Label("rt_sf_zeros");
            e.PushHl();
            e.LdHlLabel("rt_sf_buf16");
            e.LdBImm(16);
            e.Label("rt_sf_zeros_loop");
            e.StoreHlImm('0');
            e.IncHl();
            e.DjnzLabel("rt_sf_zeros_loop");
            e.PopHl();
            e.Ret();
            e.Label("rt_sf_trim");                   // C = chiffres utiles (au moins 1), ou plus si on en demande plus
            e.LdHlLabel("rt_sf_buf16");
            e.LdCImm(16);
            e.Label("rt_sf_trim_loop");
            e.LdAC();
            e.CpImm(1);
            e.JrZLabel("rt_sf_trim_found");
            e.LdAHl();
            e.CpImm('0');
            e.JrNzLabel("rt_sf_trim_found");
            e.IncHl();
            e.DecC();
            e.JrLabel("rt_sf_trim_loop");
            e.Label("rt_sf_trim_found");
            e.LdAMem("rt_sf_width");
            e.CpC();
            e.JrCLabel("rt_sf_trim_keep");
            e.JrZLabel("rt_sf_trim_keep");
            e.LdCA();
            e.LdHlLabel("rt_sf_buf16_end");
            e.LdBImm(0);
            e.OrA();
            e.SbcHlBc();
            e.Label("rt_sf_trim_keep");
            e.ExDeHl();
            e.JpLabel("rt_sf_mem");

            // UPPER$ et LOWER$ : copie, puis on change les lettres de A à Z (ou de a à z)
            foreach ((string name, char from, int shift) in new[] { ("rt_upper", 'a', -32), ("rt_lower", 'A', 32) })
            {
                e.Label(name);
                e.ExDeHl();
                e.LdADe();
                e.LdCA();
                e.LdBImm(0);
                e.CallLabel("rt_sf_sub");
                e.PushHl();
                e.LoadBFromHl();
                e.IncHl();
                e.LdAHl();
                e.IncHl();
                e.LoadHFromHl();
                e.LdLA();
                e.LdAB();
                e.OrA();
                e.JrZLabel(name + "_done");
                e.Label(name + "_loop");
                e.LdAHl();
                e.CpImm(from);
                e.JrCLabel(name + "_next");
                e.CpImm(from + 26);
                e.JrNcLabel(name + "_next");
                e.AddAImm(shift & 0xFF);
                e.LdHlA();
                e.Label(name + "_next");
                e.IncHl();
                e.DjnzLabel(name + "_loop");
                e.Label(name + "_done");
                e.PopHl();
                e.Ret();
            }

            // VAL : espaces, signe, puis chiffres décimaux, ou &hexadécimal, &Hhexadécimal, &Xbinaire ; s'arrête au
            // premier caractère qui ne convient pas. Résultat sur 16 bits (&FFFF = -1, 40000 = -25536).
            e.Label("rt_val");
            e.LoadBFromHl();
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.ExDeHl();                              // DE = lettres, B = nombre
            e.LdHlImm(0);
            e.LdMemHl("rt_sf_acc");
            e.LdCImm(0);                             // C = 1 si négatif
            e.Label("rt_val_sp");
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.CpImm(' ');
            e.JrNzLabel("rt_val_sign");
            e.IncDe();
            e.DecB();
            e.JrLabel("rt_val_sp");
            e.Label("rt_val_sign");
            e.CpImm('-');
            e.JrNzLabel("rt_val_plus");
            e.LdCImm(1);
            e.JrLabel("rt_val_skip");
            e.Label("rt_val_plus");
            e.CpImm('+');
            e.JrNzLabel("rt_val_base");
            e.Label("rt_val_skip");
            e.IncDe();
            e.DecB();
            e.Label("rt_val_base");
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.CpImm('&');
            e.JrZLabel("rt_val_amp");
            e.Label("rt_val_dec");
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.SubImm('0');
            e.JpCLabel("rt_val_done");
            e.CpImm(10);
            e.JpNcLabel("rt_val_done");
            e.PushBc();
            e.PushDe();
            e.LdHlMem("rt_sf_acc");
            e.AddHlHl();                             // * 10 = * 2 + * 8
            e.LdDH();
            e.LdEL();
            e.AddHlHl();
            e.AddHlHl();
            e.AddHlDe();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();
            e.LdMemHl("rt_sf_acc");
            e.PopDe();
            e.PopBc();
            e.IncDe();
            e.DecB();
            e.JrLabel("rt_val_dec");
            e.Label("rt_val_amp");
            e.IncDe();
            e.DecB();
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.AndImm(0xDF);                          // en majuscule
            e.CpImm('X');
            e.JrZLabel("rt_val_bin");
            e.CpImm('H');
            e.JrNzLabel("rt_val_hex");
            e.IncDe();
            e.DecB();
            e.Label("rt_val_hex");
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.CpImm('a');
            e.JrCLabel("rt_val_hex_up");
            e.SubImm(32);
            e.Label("rt_val_hex_up");
            e.SubImm('0');
            e.JpCLabel("rt_val_done");
            e.CpImm(10);
            e.JrCLabel("rt_val_hex_digit");
            e.SubImm(7);
            e.CpImm(10);
            e.JpCLabel("rt_val_done");
            e.CpImm(16);
            e.JpNcLabel("rt_val_done");
            e.Label("rt_val_hex_digit");
            e.LdHlMem("rt_sf_acc");
            e.AddHlHl();
            e.AddHlHl();
            e.AddHlHl();
            e.AddHlHl();
            e.OrL();
            e.LdLA();
            e.LdMemHl("rt_sf_acc");
            e.IncDe();
            e.DecB();
            e.JrLabel("rt_val_hex");
            e.Label("rt_val_bin");
            e.IncDe();
            e.DecB();
            e.Label("rt_val_bin_loop");
            e.LdAB();
            e.OrA();
            e.JpZLabel("rt_val_done");
            e.LdADe();
            e.SubImm('0');
            e.JpCLabel("rt_val_done");
            e.CpImm(2);
            e.JpNcLabel("rt_val_done");
            e.LdHlMem("rt_sf_acc");
            e.AddHlHl();
            e.OrL();
            e.LdLA();
            e.LdMemHl("rt_sf_acc");
            e.IncDe();
            e.DecB();
            e.JrLabel("rt_val_bin_loop");
            e.Label("rt_val_done");
            e.LdHlMem("rt_sf_acc");
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.LdAL();
            e.Cpl();
            e.LdLA();
            e.LdAH();
            e.Cpl();
            e.LdHA();
            e.IncHl();
            e.Ret();

            // INSTR : position (à partir de 1) du texte cherché, à partir du caractère « début » ; 0 s'il n'y est pas
            e.Label("rt_instr");
            e.LdAB();
            e.OrA();
            e.JpNzLabel("rt_sf_improper");
            e.LdAC();
            e.OrA();
            e.JpZLabel("rt_sf_improper");            // début de 1 à 255
            e.LdMemA("rt_sf_start");
            e.LdMemDe("rt_sf_src");
            e.LdMemHl("rt_sf_find");
            e.LdADe();
            e.LdMemA("rt_sf_slen");
            e.LdAHl();
            e.LdMemA("rt_sf_flen");
            e.OrA();
            e.JrNzLabel("rt_instr_loop");
            e.LdAMem("rt_sf_slen");                  // texte cherché vide : le début, s'il est dans le texte
            e.CpC();
            e.JpCLabel("rt_instr_zero");
            e.LdLC();
            e.LdHZero();
            e.Ret();
            e.Label("rt_instr_loop");
            e.LdAMem("rt_sf_start");                 // assez de place après le début ?
            e.DecA();
            e.LdBA();
            e.LdAMem("rt_sf_flen");
            e.AddAB();
            e.JpCLabel("rt_instr_zero");
            e.LdBA();
            e.LdAMem("rt_sf_slen");
            e.CpB();
            e.JpCLabel("rt_instr_zero");
            e.LdHlMem("rt_sf_src");
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();
            e.LdAMem("rt_sf_start");
            e.DecA();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();
            e.PushHl();
            e.LdHlMem("rt_sf_find");
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.PopHl();
            e.LdAMem("rt_sf_flen");
            e.LdBA();
            e.Label("rt_instr_cmp");
            e.LdADe();
            e.CpMemHl();
            e.JrNzLabel("rt_instr_next");
            e.IncHl();
            e.IncDe();
            e.DjnzLabel("rt_instr_cmp");
            e.LdAMem("rt_sf_start");                 // trouvé
            e.LdLA();
            e.LdHZero();
            e.Ret();
            e.Label("rt_instr_next");
            e.LdAMem("rt_sf_start");
            e.IncA();
            e.LdMemA("rt_sf_start");
            e.JrLabel("rt_instr_loop");
            e.Label("rt_instr_zero");
            e.LdHlImm(0);
            e.Ret();

            // MID$(A$, début [, longueur]) = B$ (D065) : DE = fiche de A$, HL = fiche de B$, B = début, C = longueur (255 si
            // omise). Les lettres de B$ remplacent celles de A$ à partir du début, sans changer la longueur de A$.
            e.Label("rt_midset");
            e.LdMemDe("rt_ms_dst");
            e.LdMemHl("rt_ms_src");
            e.LdAC();
            e.LdMemA("rt_ms_n");
            e.LdAB();
            e.LdMemA("rt_ms_p");
            e.OrA();
            e.JpZLabel("rt_sf_improper");            // début 0
            e.LdBA();
            e.LdADe();
            e.CpB();
            e.JpCLabel("rt_sf_improper");            // début après la fin de A$
            e.LdHlMem("rt_ms_dst");
            e.LoadCFromHl();                         // C = longueur de A$
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();                         // DE = lettres de A$
            e.PushDe();
            e.LdHlMem(StringPointerLabel);
            e.ExDeHl();
            e.OrA();
            e.SbcHlDe();
            e.PopDe();
            e.JrNcLabel("rt_ms_own");                // déjà dans la zone des textes : A$ a son propre exemplaire
            e.PushDe();                              // texte du programme ou des DATA : on le recopie d'abord
            e.LdDeMem("rt_ms_dst");
            e.CallLabel(StrAllocLabel);
            e.ExDeHl();
            e.PopHl();
            e.PushDe();
            e.LdBImm(0);
            e.Ldir();
            e.PopDe();
            e.LdHlMem("rt_ms_dst");
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.Label("rt_ms_own");
            e.LdAMem("rt_ms_p");
            e.DecA();
            e.LdBA();                                // B = écart du début
            e.LdHlMem("rt_ms_dst");
            e.LdAHl();
            e.SubB();
            e.LdCA();                                // C = lettres disponibles dans A$
            e.LdAMem("rt_ms_n");
            e.CpC();
            e.JrNcLabel("rt_ms_n_ok");
            e.LdCA();
            e.Label("rt_ms_n_ok");
            e.LdHlMem("rt_ms_src");
            e.LdAHl();
            e.CpC();
            e.JrNcLabel("rt_ms_src_ok");
            e.LdCA();                                // pas plus que la longueur de B$
            e.Label("rt_ms_src_ok");
            e.LdAC();
            e.OrA();
            e.RetZ();
            e.LdHlMem("rt_ms_dst");
            e.IncHl();
            e.LoadEFromHl();
            e.IncHl();
            e.LoadDFromHl();
            e.LdLB();
            e.LdHZero();
            e.AddHlDe();
            e.ExDeHl();                              // DE = où écrire dans A$
            e.LdHlMem("rt_ms_src");
            e.IncHl();
            e.LdAHl();
            e.IncHl();
            e.LoadHFromHl();
            e.LdLA();                                // HL = lettres de B$
            e.LdBImm(0);
            e.Ldir();
            e.Ret();

            e.Label("rt_sf_improper");
            e.LdHlLabel("rt_sf_msg_improper");
            e.JpLabel(ErrorLabel);

            foreach (string scratch in new[] { "rt_sf_src", "rt_sf_dst", "rt_sf_find", "rt_sf_acc", "rt_ms_dst", "rt_ms_src" })
            {
                e.Label(scratch);
                e.Reserve(2);
            }
            foreach (string scratch in new[] { "rt_sf_off", "rt_sf_char", "rt_sf_started", "rt_sf_width", "rt_sf_start", "rt_sf_slen", "rt_sf_flen", "rt_ms_p", "rt_ms_n" })
            {
                e.Label(scratch);
                e.Reserve(1);
            }
            e.Label("rt_sf_buf");
            e.Reserve(7);
            e.Label("rt_sf_buf16");
            e.Reserve(16);
            e.Label("rt_sf_buf16_end");
            e.Label("rt_sf_msg_improper");
            e.Data(Ascii("Improper argument"));
        }),

        // INPUT A$ (D062) : lit le texte suivant de la ligne tapée. Entre guillemets, il peut contenir des virgules ;
        // sinon il s'arrête à la virgule ou à la fin. Les espaces du début sont sautés.
        // DE = fiche de la variable ; retenue à 1 si ce qui suit le texte n'est ni une virgule ni la fin.
        [InputStringName] = new RuntimeModule(InputStringName, [InputName, StringsName], e =>
        {
            e.Label(InputStrLabel);
            e.LdMemDe("rt_str_dst");
            e.LdHlMem("rt_inp_ptr");
            e.Label("rt_is_sp");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_is_start");
            e.IncHl();
            e.JrLabel("rt_is_sp");
            e.Label("rt_is_start");
            e.LdCImm(0);                             // C = nombre de lettres
            e.CpImm('"');
            e.JrNzLabel("rt_is_plain");
            e.IncHl();
            e.LdMemHl("rt_str_a");                   // début des lettres
            e.Label("rt_is_q");
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_is_after");               // guillemet jamais refermé : on prend jusqu'à la fin
            e.CpImm('"');
            e.JrZLabel("rt_is_qend");
            e.IncHl();
            e.IncC();
            e.JrLabel("rt_is_q");
            e.Label("rt_is_qend");
            e.IncHl();
            e.Label("rt_is_sp2");
            e.LdAHl();
            e.CpImm(' ');
            e.JrNzLabel("rt_is_after");
            e.IncHl();
            e.JrLabel("rt_is_sp2");
            e.Label("rt_is_plain");
            e.LdMemHl("rt_str_a");
            e.Label("rt_is_p");
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_is_after");
            e.CpImm(',');
            e.JrZLabel("rt_is_after");
            e.IncHl();
            e.IncC();
            e.JrLabel("rt_is_p");
            e.Label("rt_is_after");                  // HL sur la virgule, la fin, ou autre chose
            e.LdAHl();
            e.OrA();
            e.JrZLabel("rt_is_ok");
            e.CpImm(',');
            e.JrNzLabel("rt_is_err");
            e.IncHl();
            e.Label("rt_is_ok");
            e.LdMemHl("rt_inp_ptr");
            e.LdAC();
            e.OrA();
            e.JrNzLabel("rt_is_copy");
            e.LdHlMem("rt_str_dst");                 // texte vide
            e.StoreHlImm(0);
            e.IncHl();
            e.StoreHlImm(0);
            e.IncHl();
            e.StoreHlImm(0);
            e.OrA();
            e.Ret();
            e.Label("rt_is_copy");
            e.LdDeMem("rt_str_dst");
            e.CallLabel(StrAllocLabel);              // HL = lettres (la ligne tapée n'est pas dans la zone des textes)
            e.PushHl();
            e.PushBc();
            e.ExDeHl();
            e.LdHlMem("rt_str_a");
            e.LdBImm(0);
            e.Ldir();
            e.PopBc();
            e.PopDe();
            e.LdHlMem("rt_str_dst");
            e.StoreHlC();
            e.IncHl();
            e.StoreHlE();
            e.IncHl();
            e.StoreHlD();
            e.OrA();
            e.Ret();
            e.Label("rt_is_err");
            e.Scf();
            e.Ret();

            // LINE INPUT A$ (D064) : toute la ligne tapée, telle quelle. DE = fiche de la variable.
            e.Label("rt_input_all");
            e.LdMemDe("rt_str_dst");
            e.LdHlLabel("rt_inp_buf");
            e.LdMemHl("rt_str_a");
            e.LdCImm(0);
            e.Label("rt_ia_len");
            e.LdAHl();
            e.OrA();
            e.JpZLabel("rt_is_ok");                  // HL = fin de la ligne ; la suite est commune avec INPUT A$
            e.IncHl();
            e.IncC();
            e.JrLabel("rt_ia_len");
        }),

        [ArrayName] = new RuntimeModule(ArrayName, [ErrorName, HeapName], e =>
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
            e.LdBH();                                // taille d'une case : 2 octets (entier), 3 (fiche de texte, D065)
            e.LdCL();                                // ou 5 (nombre à virgule, D070)
            e.AddHlHl();
            e.JpCLabel(ErrMemory);
            e.LdAMem(ArrayElementSizeLabel);
            e.CpImm(2);
            e.JrZLabel("rt_arr_alloc_two");
            e.CpImm(3);
            e.JrZLabel("rt_arr_alloc_three");
            e.AddHlHl();                             // 4 cases, puis + 1 = 5
            e.JpCLabel(ErrMemory);
            e.Label("rt_arr_alloc_three");
            e.AddHlBc();
            e.JpCLabel(ErrMemory);
            e.Label("rt_arr_alloc_two");
            e.LdBH();                                // BC = octets
            e.LdCL();
            e.LdHlMem(ArrayHeapLabel);
            e.PushHl();                              // début des cases
            e.AddHlBc();
            e.JpCLabel(ErrMemory);
            e.PushDe();
            e.LdDeMem(StringPointerLabel);           // les textes occupent le haut : on s'arrête sous eux (D062)
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
            e.LdBH();                                // taille d'une case : 2 octets (entier) ou 3 (fiche de texte, D065)
            e.LdCL();
            e.AddHlHl();
            e.LdAMem(ArrayElementSizeLabel);         // écart en octets : 2, 3 ou 5 fois le numéro (D070)
            e.CpImm(2);
            e.JrZLabel("rt_arr_e1_two");
            e.CpImm(3);
            e.JrZLabel("rt_arr_e1_two_3");
            e.AddHlHl();
            e.Label("rt_arr_e1_two_3");
            e.AddHlBc();
            e.Label("rt_arr_e1_two");
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
            e.LdBH();                                // taille d'une case : 2 octets (entier) ou 3 (fiche de texte, D065)
            e.LdCL();
            e.AddHlHl();
            e.LdAMem(ArrayElementSizeLabel);         // écart en octets : 2, 3 ou 5 fois le numéro (D070)
            e.CpImm(2);
            e.JrZLabel("rt_arr_e_two");
            e.CpImm(3);
            e.JrZLabel("rt_arr_e_two_3");
            e.AddHlHl();
            e.Label("rt_arr_e_two_3");
            e.AddHlBc();
            e.Label("rt_arr_e_two");
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

            // Erreurs : message du Locomotive, puis module RunError
            e.Label(ErrSubscript);
            e.LdHlLabel("rt_arr_msg_sub");
            e.JpLabel(ErrorLabel);
            e.Label(ErrRedim);
            e.LdHlLabel("rt_arr_msg_redim");
            e.JpLabel(ErrorLabel);

            e.Label(ArrayElementSizeLabel);          // 2 ; mis à 3 avant chaque accès à un tableau de textes (D065)
            e.Bytes(2);
            foreach (string scratch in new[] { "rt_arr_ret", "rt_arr_desc", "rt_arr_off", "rt_arr_stride", "rt_arr_size" })
            {
                e.Label(scratch);
                e.Reserve(2);
            }
            e.Label("rt_arr_msg_sub");
            e.Data(Ascii("Subscript out of range"));
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
        [Div16Name] = new RuntimeModule(Div16Name, [ErrorName], e =>
        {
            e.Label(Div16Label);
            e.LdAD();
            e.OrE();
            e.JrNzLabel("rt_div_ok");
            e.PushHl();                          // diviseur 0 : « Division by zero » (le programme continue, ou ON ERROR,
            e.LdHlLabel("rt_div_msg");           // D080), puis DE = dividende, HL = 0
            e.CallLabel(ErrorSoftLabel);
            e.PopDe();
            e.LdHlImm(0);
            e.Ret();
            e.Label("rt_div_msg");
            e.Data(Ascii("Division by zero"));

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
            // Éditeur de la ligne tapée, comme celui du vrai BASIC (lot H5, D076) : flèches gauche et droite dans la ligne,
            // haut et bas d'une largeur d'écran, CTRL + gauche / droite au début / à la fin, une lettre s'insère, DEL efface
            // à gauche, CLR sous le curseur, SHIFT + flèches déplacent le curseur de copie, COPY recopie le caractère
            // qui est sous lui (TXT RD CHAR) et l'avance. 255 caractères au plus. Les positions à l'écran sont calculées
            // depuis celle du premier caractère (rt_in_sc, rt_in_sr), recalée après chaque affichage (défilement).
            e.Label(InputLineLabel);
            e.XorA();
            e.LdMemA("rt_inp_len");
            e.LdMemA("rt_in_pos");
            e.LdMemA("rt_in_cp");
            e.CallAddress(0xBB69);                   // TXT GET WINDOW : largeur et hauteur
            e.LdAD();
            e.SubH();
            e.IncA();
            e.LdMemA("rt_in_w");
            e.LdAE();
            e.SubL();
            e.IncA();
            e.LdMemA("rt_in_h");
            e.CallAddress(TxtGetCursor);
            e.CallAddress(TxtValidate);              // où s'écrira le premier caractère
            e.LdAH();
            e.LdMemA("rt_in_sc");
            e.LdAL();
            e.LdMemA("rt_in_sr");

            e.Label("rt_in_key");
            e.LdHlImm(0xBB8A);                       // TXT PLACE CURSOR : les curseurs sont montrés pendant l'attente
            e.CallLabel("rt_in_blobs");
            e.CallAddress(KmWaitChar);
            e.PushAf();
            e.LdHlImm(0xBB8D);                       // TXT REMOVE CURSOR
            e.CallLabel("rt_in_blobs");
            e.PopAf();
            foreach ((int code, string target) in new[] { (13, "rt_in_done"), (127, "rt_in_del"), (0x10, "rt_in_clr"),
                         (0xE0, "rt_in_copy"), (0xF2, "rt_in_left"), (0xF3, "rt_in_right"), (0xF0, "rt_in_up"),
                         (0xF1, "rt_in_down"), (0xFA, "rt_in_home"), (0xFB, "rt_in_ctrlend"), (0xF4, "rt_in_cup"),
                         (0xF5, "rt_in_cdown"), (0xF6, "rt_in_cleft"), (0xF7, "rt_in_cright") })
            {
                e.CpImm(code);
                e.JpZLabel(target);
            }
            e.CpImm(32);
            e.JrCLabel("rt_in_key");             // autre caractère de commande : ignoré
            e.CpImm(127);
            e.JrNcLabel("rt_in_key");            // caractère étendu : ignoré

            // Insertion du caractère A à la position du curseur
            e.Label("rt_in_insert");
            e.LdMemA("rt_in_ch");
            e.LdAMem("rt_inp_len");
            e.CpImm(255);
            e.JpZLabel("rt_in_key");             // ligne pleine
            e.LdAMem("rt_in_pos");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.SubB();                            // A = caractères à décaler vers la droite
            e.JrZLabel("rt_in_put");
            e.LdCA();
            e.LdBImm(0);
            e.LdAMem("rt_inp_len");
            e.CallLabel("rt_in_addr");           // HL = tampon + longueur
            e.LdDH();
            e.LdEL();
            e.DecHl();
            e.Lddr();
            e.Label("rt_in_put");
            e.LdAMem("rt_in_pos");
            e.CallLabel("rt_in_addr");
            e.LdAMem("rt_in_ch");
            e.LdHlA();
            e.LdHlLabel("rt_inp_len");
            e.IncMemHl();
            e.XorA();
            e.CallLabel("rt_in_tail");
            e.LdHlLabel("rt_in_pos");
            e.IncMemHl();
            e.JpLabel("rt_in_key");

            // DEL : efface à gauche (le curseur recule puis CLR) ; CLR : efface sous le curseur
            e.Label("rt_in_del");
            e.LdAMem("rt_in_pos");
            e.OrA();
            e.JpZLabel("rt_in_key");
            e.DecA();
            e.LdMemA("rt_in_pos");
            e.JrLabel("rt_in_erase");
            e.Label("rt_in_clr");
            e.LdAMem("rt_in_pos");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.CpB();
            e.JpZLabel("rt_in_key");             // rien sous le curseur
            e.Label("rt_in_erase");
            e.LdAMem("rt_in_pos");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.SubB();
            e.DecA();                            // A = caractères à décaler vers la gauche
            e.JrZLabel("rt_in_shorter");
            e.LdCA();
            e.LdBImm(0);
            e.LdAMem("rt_in_pos");
            e.CallLabel("rt_in_addr");
            e.LdDH();
            e.LdEL();
            e.IncHl();
            e.Ldir();
            e.Label("rt_in_shorter");
            e.LdHlLabel("rt_inp_len");
            e.DecMemHl();
            e.LdAImm(1);                         // un espace efface le dernier caractère à l'écran
            e.CallLabel("rt_in_tail");
            e.JpLabel("rt_in_key");

            // Déplacements dans la ligne
            e.Label("rt_in_left");
            e.LdAMem("rt_in_pos");
            e.OrA();
            e.JpZLabel("rt_in_key");
            e.DecA();
            e.JrLabel("rt_in_setpos");
            e.Label("rt_in_right");
            e.LdAMem("rt_in_pos");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.CpB();
            e.JpZLabel("rt_in_key");
            e.LdAB();
            e.IncA();
            e.JrLabel("rt_in_setpos");
            e.Label("rt_in_up");
            e.LdAMem("rt_in_w");
            e.LdBA();
            e.LdAMem("rt_in_pos");
            e.SubB();
            e.JpCLabel("rt_in_key");
            e.JrLabel("rt_in_setpos");
            e.Label("rt_in_down");
            e.LdAMem("rt_in_w");
            e.LdBA();
            e.LdAMem("rt_in_pos");
            e.AddAB();
            e.JpCLabel("rt_in_key");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.CpB();
            e.JpCLabel("rt_in_key");             // au-delà de la fin
            e.LdAB();
            e.JrLabel("rt_in_setpos");
            e.Label("rt_in_home");
            e.XorA();
            e.JrLabel("rt_in_setpos");
            e.Label("rt_in_ctrlend");
            e.LdAMem("rt_inp_len");
            e.Label("rt_in_setpos");
            e.LdMemA("rt_in_pos");
            e.JpLabel("rt_in_key");

            // Curseur de copie (SHIFT + flèches) : il part de la position du curseur, puis reste dans la fenêtre
            e.Label("rt_in_cup");
            e.CallLabel("rt_in_cstart");
            e.LdAMem("rt_in_cr");
            e.CpImm(2);
            e.JpCLabel("rt_in_key");
            e.DecA();
            e.LdMemA("rt_in_cr");
            e.JpLabel("rt_in_key");
            e.Label("rt_in_cdown");
            e.CallLabel("rt_in_cstart");
            e.LdAMem("rt_in_h");
            e.LdBA();
            e.LdAMem("rt_in_cr");
            e.CpB();
            e.JpNcLabel("rt_in_key");
            e.IncA();
            e.LdMemA("rt_in_cr");
            e.JpLabel("rt_in_key");
            e.Label("rt_in_cleft");
            e.CallLabel("rt_in_cstart");
            e.LdAMem("rt_in_cc");
            e.CpImm(2);
            e.JpCLabel("rt_in_key");
            e.DecA();
            e.LdMemA("rt_in_cc");
            e.JpLabel("rt_in_key");
            e.Label("rt_in_cright");
            e.CallLabel("rt_in_cstart");
            e.LdAMem("rt_in_w");
            e.LdBA();
            e.LdAMem("rt_in_cc");
            e.CpB();
            e.JpNcLabel("rt_in_key");
            e.IncA();
            e.LdMemA("rt_in_cc");
            e.JpLabel("rt_in_key");

            // COPY : lit le caractère sous le curseur de copie, l'avance d'une place, puis insère le caractère lu
            e.Label("rt_in_copy");
            e.LdAMem("rt_in_cp");
            e.OrA();
            e.JpZLabel("rt_in_key");
            e.LdAMem("rt_in_cc");
            e.LdHA();
            e.LdAMem("rt_in_cr");
            e.LdLA();
            e.CallAddress(TxtSetCursorAddress);
            e.CallAddress(0xBB60);               // TXT RD CHAR : A = caractère, retenue à 1 s'il est reconnu
            e.JpNcLabel("rt_in_key");
            e.PushAf();
            e.LdAMem("rt_in_w");
            e.LdBA();
            e.LdAMem("rt_in_cc");
            e.CpB();
            e.JrNcLabel("rt_in_cwrap");
            e.IncA();
            e.LdMemA("rt_in_cc");
            e.JrLabel("rt_in_cdone");
            e.Label("rt_in_cwrap");              // au bord droit : début de la ligne suivante, s'il y en a une
            e.LdAImm(1);
            e.LdMemA("rt_in_cc");
            e.LdAMem("rt_in_h");
            e.LdBA();
            e.LdAMem("rt_in_cr");
            e.CpB();
            e.JrNcLabel("rt_in_cdone");
            e.IncA();
            e.LdMemA("rt_in_cr");
            e.Label("rt_in_cdone");
            e.PopAf();
            e.JpLabel("rt_in_insert");

            // Entrée : curseur après le dernier caractère (pas au début de la ligne suivante), puis fin du texte
            e.Label("rt_in_done");
            e.LdAMem("rt_inp_len");
            e.OrA();
            e.JrNzLabel("rt_in_doneafter");
            e.CallLabel("rt_in_goto");
            e.JrLabel("rt_in_end0");
            e.Label("rt_in_doneafter");
            e.DecA();
            e.CallLabel("rt_in_rowcol");
            e.IncH();
            e.CallAddress(TxtSetCursorAddress);
            e.Label("rt_in_end0");
            e.LdAMem("rt_inp_len");
            e.CallLabel("rt_in_addr");
            e.XorA();
            e.LdHlA();                           // fin du texte : un octet à 0
            e.LdHlLabel("rt_inp_buf");
            e.LdMemHl("rt_inp_ptr");
            e.Ret();

            // A = position -> HL = adresse dans le tampon
            e.Label("rt_in_addr");
            e.LdEA();
            e.LdDZero();
            e.LdHlLabel("rt_inp_buf");
            e.AddHlDe();
            e.Ret();

            // A = position -> H = colonne, L = ligne à l'écran (la ligne peut être hors de la fenêtre)
            e.Label("rt_in_rowcol");
            e.LdLA();
            e.LdHZero();
            e.LdAMem("rt_in_sc");
            e.DecA();
            e.LdEA();
            e.LdDZero();
            e.AddHlDe();                         // HL = colonne de départ - 1 + position
            e.LdAMem("rt_in_w");
            e.LdEA();
            e.LdCImm(0);
            e.Label("rt_in_rc_div");
            e.OrA();
            e.SbcHlDe();
            e.JrCLabel("rt_in_rc_back");
            e.IncC();
            e.JrLabel("rt_in_rc_div");
            e.Label("rt_in_rc_back");
            e.AddHlDe();
            e.LdAL();
            e.IncA();
            e.LdBA();                            // B = colonne
            e.LdAMem("rt_in_sr");
            e.AddAC();
            e.LdLA();                            // L = ligne
            e.LdHB();
            e.Ret();

            // A = position : le curseur y est placé
            e.Label("rt_in_goto");
            e.CallLabel("rt_in_rowcol");
            e.JpAddress(TxtSetCursorAddress);

            // HL = TXT PLACE CURSOR ou TXT REMOVE CURSOR : appliqué au curseur de copie puis à celui de la ligne
            e.Label("rt_in_blobs");
            e.LdMemHl("rt_in_vec");
            e.LdAMem("rt_in_cp");
            e.OrA();
            e.JrZLabel("rt_in_bmain");
            e.LdAMem("rt_in_cc");
            e.LdHA();
            e.LdAMem("rt_in_cr");
            e.LdLA();
            e.CallAddress(TxtSetCursorAddress);
            e.CallLabel("rt_in_vcall");
            e.Label("rt_in_bmain");
            e.LdAMem("rt_in_pos");
            e.CallLabel("rt_in_goto");
            e.Label("rt_in_vcall");
            e.Bytes(0xC3);
            e.Label("rt_in_vec");
            e.Word(0xBB8A);

            // Curseur de copie : au premier SHIFT + flèche, il part de la position du curseur de la ligne
            e.Label("rt_in_cstart");
            e.LdAMem("rt_in_cp");
            e.OrA();
            e.RetNz();
            e.IncA();
            e.LdMemA("rt_in_cp");
            e.LdAMem("rt_in_pos");
            e.CallLabel("rt_in_rowcol");
            e.LdAH();
            e.LdMemA("rt_in_cc");
            e.LdAL();
            e.LdMemA("rt_in_cr");
            e.Ret();

            // Réaffiche la ligne à partir du curseur (A = 1 : un espace en plus), puis recale la position du début :
            // l'écran a pu défiler. Le curseur de copie suit ; s'il sort par le haut, il disparaît.
            e.Label("rt_in_tail");
            e.LdMemA("rt_in_sp");
            e.LdAMem("rt_in_pos");
            e.CallLabel("rt_in_goto");
            e.LdAMem("rt_in_pos");
            e.LdMemA("rt_in_m");
            e.Label("rt_in_tl");
            e.LdAMem("rt_in_m");
            e.LdBA();
            e.LdAMem("rt_inp_len");
            e.CpB();
            e.JrZLabel("rt_in_tl_end");
            e.LdAB();
            e.CallLabel("rt_in_addr");
            e.LdAHl();
            e.CallAddress(TxtOutput);
            e.LdHlLabel("rt_in_m");
            e.IncMemHl();
            e.JrLabel("rt_in_tl");
            e.Label("rt_in_tl_end");
            e.LdAMem("rt_in_sp");
            e.OrA();
            e.JrZLabel("rt_in_sync");
            e.LdAImm(' ');
            e.CallAddress(TxtOutput);
            e.LdHlLabel("rt_in_m");
            e.IncMemHl();
            e.Label("rt_in_sync");
            e.LdAMem("rt_in_pos");
            e.LdBA();
            e.LdAMem("rt_in_m");
            e.CpB();
            e.RetZ();                            // rien d'affiché
            e.CallAddress(TxtGetCursor);         // L = ligne du dernier caractère affiché
            e.LdAL();
            e.LdMemA("rt_in_t");
            e.LdAMem("rt_in_m");
            e.DecA();
            e.CallLabel("rt_in_rowcol");         // L = ligne prévue pour lui
            e.LdAMem("rt_in_t");
            e.SubL();
            e.LdBA();                            // B = décalage dû au défilement
            e.LdAMem("rt_in_sr");
            e.AddAB();
            e.LdMemA("rt_in_sr");
            e.LdAMem("rt_in_cr");
            e.AddAB();
            e.LdMemA("rt_in_cr");
            e.DecA();
            e.AndImm(0x80);
            e.RetZ();
            e.XorA();
            e.LdMemA("rt_in_cp");
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
            foreach (string name in new[] { "rt_in_pos", "rt_in_cp", "rt_in_w", "rt_in_h", "rt_in_sc", "rt_in_sr", "rt_in_ch",
                                            "rt_in_cc", "rt_in_cr", "rt_in_sp", "rt_in_m", "rt_in_t" })
            {
                e.Label(name);
                e.Reserve(1);
            }
            e.Label("rt_inp_buf");
            e.Reserve(257);                          // 255 caractères et l'octet 0 de fin (D076)
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
        }),
        // Calculs à virgule par la ROM (lot F1b, D068) : table de sauts rt_fm_... (voir FirmwareMaths).
        // rt_fm_init (appelée au départ du programme) remplit la table si besoin et met les angles en radians.
        //  - FMathsAll  : tous les CPC. La table contient les adresses du 664/6128 ; si KL PROBE ROM
        //                 dit que c'est un 464 (H = 0), on recopie par-dessus la table du 464.
        //  - FMaths464 / FMaths6128 : machine choisie (F7), la bonne table directement.
        [MathsAllName] = new RuntimeModule(MathsAllName, [], e =>
        {
            EmitMathsTable(e, rom464: false);
            e.Label(MathsInitLabel);
            e.LdCImm(0);
            e.CallAddress(FirmwareMaths.KlProbeRom);   // H = version de la machine
            e.LdAH();
            e.OrA();
            e.JpNzLabel("rt_fm_radians");               // 664, 6128, Plus : la table est déjà bonne
            e.LdHlLabel("rt_fm_t464");
            e.LdDeLabel("rt_fm_table");
            e.LdBcImm(FirmwareMaths.All.Count * 3);
            e.Ldir();
            e.Label("rt_fm_radians");
            e.XorA();
            e.JpLabel(FirmwareMaths.AngleMode.Label);
            e.Label("rt_fm_t464");
            foreach (FirmwareMathsRoutine routine in FirmwareMaths.All)
                e.Bytes(0xC3, (byte)(routine.Address464 & 0xFF), (byte)(routine.Address464 >> 8));
        }),
        [RealOpsName] = new RuntimeModule(RealOpsName, [ErrorName, PrintStringName], RealRuntime.EmitOps),
        [RealPrintName] = new RuntimeModule(RealPrintName, [PrintFitName], RealRuntime.EmitPrint),
        [InputRealName] = new RuntimeModule(InputRealName, [InputName, RealParseName], RealRuntime.EmitInput),
        [RealParseName] = new RuntimeModule(RealParseName, [RealOpsName], RealRuntime.EmitParse),
        [RealValName] = new RuntimeModule(RealValName, [RealParseName, StrFunctionsName], RealRuntime.EmitVal),
        [RealStrName] = new RuntimeModule(RealStrName, [RealPrintName, StrFunctionsName], RealRuntime.EmitStr),
        [RealRndName] = new RuntimeModule(RealRndName, [Rand16Name, RandomizeName], RealRuntime.EmitRnd),
        [DataRealName] = new RuntimeModule(DataRealName, [DataName, RealOpsName], RealRuntime.EmitDataReal),
        [DataNoRealName] = new RuntimeModule(DataNoRealName, [DataName], e =>
        {
            e.Label("rt_read_real");                 // jamais atteint : pas de nombre à virgule dans les DATA
            e.JpLabel("rt_read_text");
        }),
        [Maths464Name] = new RuntimeModule(Maths464Name, [], e => EmitMathsFixed(e, rom464: true)),
        [Maths6128Name] = new RuntimeModule(Maths6128Name, [], e => EmitMathsFixed(e, rom464: false))
    };

    public static RuntimeModule Get(string name) =>
        Modules.TryGetValue(name, out RuntimeModule? module)
            ? module
            : throw new InvalidOperationException($"Module inconnu dans la bibliothèque : {name}");

    // Minuteries (D079) ; si sound, événements des canaux sonores (ON SQ, D082) ; si brk, la touche ESC (D083)
    static void EmitEvents(Z80Emitter e, bool sound, bool brk)
    {
        e.Label(EventsInitLabel);
        if (sound)
        {
            for (int k = 0; k < 3; k++)
            {
                e.LdHlLabel($"rt_sq_blk{k}");
                e.LdBcImm(0xC100);                   // événement express, asynchrone, adresse proche
                e.LdDeLabel($"rt_sq_kick{k}");
                e.CallAddress(0xBCEF);               // KL INIT EVENT
            }
        }
        e.LdHlLabel("rt_ev_blk_ev");
        e.LdBcImm(0xC100);                       // B = &C1 : événement express, asynchrone, adresse proche ; C = 0
        e.LdDeLabel("rt_ev_tick");
        e.CallAddress(0xBCEF);                   // KL INIT EVENT
        e.LdHlLabel("rt_ev_blk");
        e.LdDeImm(1);
        e.LdBcImm(1);
        e.JpAddress(0xBCE9);                     // KL ADD TICKER : tous les 1/50 s

        // Tous les 1/50 s (pendant l'interruption) : les registres sont gardés
        e.Label("rt_ev_tick");
        e.PushAf();
        e.PushHl();
        if (brk)
        {
            // Touche ESC (D083) : un nouvel appui (elle n'était pas enfoncée au top d'avant) est marqué
            e.PushBc();
            e.PushDe();
            e.LdAImm(66);
            e.CallAddress(0xBB1E);                   // KM TEST KEY : NZ si ESC est enfoncée
            e.LdAImm(0);
            e.JrZLabel("rt_brk_up");
            e.IncA();
            e.Label("rt_brk_up");
            e.LdCA();                                // C = enfoncée maintenant
            e.LdHlLabel("rt_brk_down");
            e.LdAHl();
            e.StoreHlC();
            e.OrA();
            e.JrNzLabel("rt_brk_held");              // déjà enfoncée : rien de nouveau
            e.OrC();
            e.JrZLabel("rt_brk_held");
            e.LdMemA("rt_brk_p");                    // A = 1
            e.LdMemA(EventsPendingLabel);
            e.Label("rt_brk_held");
            e.PopDe();
            e.PopBc();
        }
        for (int t = 0; t < 4; t++)
        {
            e.LdAMem($"rt_ev_a{t}");
            e.OrA();
            e.JrZLabel($"rt_ev_skip{t}");
            e.LdHlMem($"rt_ev_c{t}");
            e.DecHl();
            e.LdMemHl($"rt_ev_c{t}");
            e.LdAH();
            e.OrL();
            e.JrNzLabel($"rt_ev_skip{t}");
            e.LdHlMem($"rt_ev_r{t}");            // au bout : recharge (EVERY) ou arrêt (AFTER)
            e.LdMemHl($"rt_ev_c{t}");
            e.LdAH();
            e.OrL();
            e.JrNzLabel($"rt_ev_fire{t}");
            e.LdMemA($"rt_ev_a{t}");             // A = 0
            e.Label($"rt_ev_fire{t}");
            e.LdAImm(1);
            e.LdMemA($"rt_ev_p{t}");
            e.LdMemA(EventsPendingLabel);
            e.Label($"rt_ev_skip{t}");
        }
        e.PopHl();
        e.PopAf();
        e.Ret();

        // Entre deux instructions : lance les sous-programmes des minuteries marquées
        e.Label(EventsRunLabel);
        if (brk)
        {
            // Touche ESC (D083), avant tout (DI ne la retient pas). ON BREAK CONT : rien. Sinon : pause (curseur
            // affiché) jusqu'à une touche ; ESC : « Break in N » (ON BREAK STOP) ou appel (ON BREAK GOSUB) ; une autre
            // touche : on continue, comme le vrai BASIC (SugarBox, 16 nov. 2026).
            e.LdAMem("rt_brk_p");
            e.OrA();
            e.JrZLabel("rt_brk_none");
            e.XorA();
            e.LdMemA("rt_brk_p");
            e.CallLabel("rt_brk_flush");             // le caractère d'ESC (&FC) quitte la file du clavier
            e.LdAMem(BreakModeLabel);
            e.OrA();
            e.JrZLabel("rt_brk_none");               // ON BREAK CONT
            e.CallAddress(0xBB8A);                   // TXT PLACE CURSOR
            e.CallAddress(0xBB06);                   // KM WAIT CHAR
            e.PushAf();
            e.CallAddress(0xBB8D);                   // TXT REMOVE CURSOR
            e.LdAImm(1);
            e.LdMemA("rt_brk_down");                 // cette touche ne compte pas comme un nouvel appui
            e.XorA();
            e.LdMemA("rt_brk_p");
            e.CallLabel("rt_brk_flush");
            e.PopAf();
            e.CpImm(0xFC);                           // ESC ?
            e.JrNzLabel("rt_brk_none");              // une autre touche : on continue
            e.LdAMem(BreakModeLabel);
            e.CpImm(2);
            e.JrZLabel("rt_brk_gosub");
            e.LdHlLabel(BreakMessageLabel);          // « Break in N », puis fin comme STOP
            e.JpLabel(ErrorShowLabel);
            e.Label("rt_brk_gosub");
            e.LdAMem("rt_ev_lvl");
            e.PushAf();
            e.LdAImm(5);                             // au-dessus de toutes les minuteries
            e.LdMemA("rt_ev_lvl");
            e.LdHlLabel(GosubCountLabel);
            e.IncMemHl();
            e.LdHlMem(BreakTargetLabel);
            e.CallLabel("rt_ev_jphl");               // GOSUB ligne : revient ici par RETURN
            e.PopAf();
            e.LdMemA("rt_ev_lvl");
            e.Label("rt_brk_none");
        }
        e.LdAMem(EventsDiLabel);
        e.OrA();
        e.RetNz();                               // DI : rien pour l'instant
        e.XorA();
        e.LdMemA(EventsPendingLabel);
        // Du plus prioritaire au moins prioritaire : minuterie 3, minuterie 2 et les canaux sonores (même niveau,
        // D082), minuterie 1, minuterie 0
        var slots = new List<(string Name, int Level)> { ("ev3", 4), ("ev2", 3) };
        if (sound)
            slots.AddRange([("sq0", 3), ("sq1", 3), ("sq2", 3)]);
        slots.AddRange([("ev1", 2), ("ev0", 1)]);
        foreach ((string name, int level) in slots)
        {
            string flag = name.StartsWith("ev") ? $"rt_ev_p{name[2]}" : $"rt_sq_p{name[2]}";
            string target = name.StartsWith("ev") ? $"rt_ev_t{name[2]}" : $"rt_sq_t{name[2]}";
            e.LdAMem(flag);
            e.OrA();
            e.JrZLabel($"rt_ev_next_{name}");
            e.LdAMem("rt_ev_lvl");               // niveau en cours (0 : aucun)
            e.CpImm(level);
            e.JrCLabel($"rt_ev_go_{name}");
            e.LdAImm(1);                         // pas assez prioritaire : attendra le RETURN
            e.LdMemA(EventsPendingLabel);
            e.JrLabel($"rt_ev_next_{name}");
            e.Label($"rt_ev_go_{name}");
            e.PushAf();                          // A = niveau d'avant
            e.XorA();
            e.LdMemA(flag);
            e.LdAImm(level);
            e.LdMemA("rt_ev_lvl");
            e.LdHlLabel(GosubCountLabel);        // compte comme un GOSUB (son RETURN le décompte, D081)
            e.IncMemHl();
            e.LdHlMem(target);
            e.CallLabel("rt_ev_jphl");           // GOSUB ligne : revient ici par RETURN
            e.PopAf();
            e.LdMemA("rt_ev_lvl");
            e.JpLabel(EventsRunLabel);           // on recommence : d'autres ont pu arriver
            e.Label($"rt_ev_next_{name}");
        }
        e.Ret();
        e.Label("rt_ev_jphl");
        e.JpHl();

        // A = minuterie (0 à 3) ; rt_ev_tc, tr, tt : durée, recharge (0 pour AFTER), adresse du sous-programme
        e.Label(EventsSetLabel);
        e.CallLabel("rt_ev_entry");
        e.Di();
        e.StoreHlImm(0);                         // arrêtée pendant le changement
        e.IncHl();
        e.LdDH();
        e.LdEL();
        e.LdHlLabel("rt_ev_tc");
        e.LdBcImm(6);
        e.Ldir();
        e.ExDeHl();
        e.StoreHlImm(0);                         // pas marquée
        e.LdBcImm(-7 & 0xFFFF);
        e.AddHlBc();
        e.StoreHlImm(1);                         // active
        e.Ei();
        e.Ret();

        // REMAIN(minuterie) : HL = compte restant (0 si elle ne tournait pas), puis la minuterie est arrêtée
        e.Label(EventsRemainLabel);
        e.LdAH();
        e.OrA();
        e.JrNzLabel("rt_ev_bad");
        e.LdAL();
        e.CallLabel("rt_ev_entry");
        e.Di();
        e.LdAHl();
        e.StoreHlImm(0);                         // arrêtée
        e.IncHl();
        e.LoadEFromHl();
        e.IncHl();
        e.LoadDFromHl();
        e.LdBcImm(5);                            // de l'octet 2 à l'octet 7
        e.AddHlBc();
        e.StoreHlImm(0);                         // plus marquée
        e.Ei();
        e.OrA();
        e.ExDeHl();
        e.RetNz();
        e.LdHlImm(0);
        e.Ret();

        // A = minuterie -> HL = sa ligne de la table ; hors de 0 à 3 : « Improper argument »
        e.Label("rt_ev_entry");
        e.CpImm(4);
        e.JrNcLabel("rt_ev_bad");
        e.LdLA();
        e.LdHZero();
        e.AddHlHl();
        e.AddHlHl();
        e.AddHlHl();
        e.LdDeLabel("rt_ev_tab");
        e.AddHlDe();
        e.Ret();
        e.Label("rt_ev_bad");
        e.LdHlLabel("rt_ev_msg");
        e.JpLabel(ErrorLabel);
        e.Label("rt_ev_msg");
        e.Data(Ascii("Improper argument"));

        e.Label("rt_ev_tab");
        for (int t = 0; t < 4; t++)
        {
            e.Label($"rt_ev_a{t}");
            e.Reserve(1);
            e.Label($"rt_ev_c{t}");
            e.Reserve(2);
            e.Label($"rt_ev_r{t}");
            e.Reserve(2);
            e.Label($"rt_ev_t{t}");
            e.Reserve(2);
            e.Label($"rt_ev_p{t}");
            e.Reserve(1);
        }
        e.Label("rt_ev_tc");
        e.Reserve(2);
        e.Label("rt_ev_tr");
        e.Reserve(2);
        e.Label("rt_ev_tt");
        e.Reserve(2);
        e.Label(EventsPendingLabel);
        e.Reserve(1);
        e.Label("rt_ev_lvl");
        e.Reserve(1);
        e.Label(EventsDiLabel);
        e.Reserve(1);
        e.Label("rt_ev_blk");                    // bloc du compteur (6 octets) suivi du bloc de l'événement
        e.Reserve(6);
        e.Label("rt_ev_blk_ev");
        e.Reserve(10);

        // RUN "nom" (D086) : le compteur est retiré et les sons arrêtés (leurs blocs vont être recouverts)
        e.Label(EventsStopLabel);
        e.LdHlLabel("rt_ev_blk");
        e.CallAddress(0xBCEC);                       // KL DEL TICKER
        e.CallAddress(0xBCA7);                       // SOUND RESET
        e.Ret();

        if (brk)
        {
            // Vide la file du clavier (KM READ CHAR jusqu'à ce qu'il n'y ait plus rien)
            e.Label("rt_brk_flush");
            e.CallAddress(0xBB09);
            e.JrCLabel("rt_brk_flush");
            e.Ret();
            e.Label(BreakModeLabel);
            e.Reserve(1);
            e.Label("rt_brk_p");
            e.Reserve(1);
            e.Label("rt_brk_down");
            e.Reserve(1);
            e.Label(BreakTargetLabel);
            e.Reserve(2);
        }

        if (!sound)
            return;

        // ON SQ(canal) GOSUB ligne : HL = canal (1, 2 ou 4), DE = adresse de la ligne. Le firmware (SOUND ARM EVENT)
        // déclenche l'événement une seule fois, dès qu'il y a une place libre dans la file du canal (tout de suite si
        // elle en a déjà une) : le sous-programme doit refaire ON SQ, comme dans le vrai BASIC.
        e.Label(SoundEventSetLabel);
        e.LdAH();
        e.OrA();
        e.JpNzLabel("rt_ev_bad");
        e.LdAL();
        for (int k = 0; k < 3; k++)
        {
            e.CpImm(1 << k);
            e.JrNzLabel($"rt_sq_not{k}");
            e.ExDeHl();
            e.LdMemHl($"rt_sq_t{k}");
            e.LdHlLabel($"rt_sq_blk{k}");
            e.CallAddress(0xBCB0);                   // SOUND ARM EVENT : A = canal, HL = bloc
            e.Ret();
            e.Label($"rt_sq_not{k}");
        }
        e.JpLabel("rt_ev_bad");

        // Appelées par le firmware (souvent pendant l'interruption) : le canal est marqué
        for (int k = 0; k < 3; k++)
        {
            e.Label($"rt_sq_kick{k}");
            e.PushAf();
            e.LdAImm(1);
            e.LdMemA($"rt_sq_p{k}");
            e.LdMemA(EventsPendingLabel);
            e.PopAf();
            e.Ret();
        }
        for (int k = 0; k < 3; k++)
        {
            e.Label($"rt_sq_p{k}");
            e.Reserve(1);
            e.Label($"rt_sq_t{k}");
            e.Reserve(2);
            e.Label($"rt_sq_blk{k}");
            e.Reserve(8);
        }
    }
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
