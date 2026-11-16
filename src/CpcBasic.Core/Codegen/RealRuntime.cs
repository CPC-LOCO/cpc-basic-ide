// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

namespace CpcBasic.Core.Codegen;

/// <summary>
/// Modules des nombres à virgule (lot F1b, D068). Les calculs eux-mêmes sont faits par la ROM
/// (table rt_fm_..., voir FirmwareMaths) ; ces modules ajoutent ce qui manque autour :
///  - RealOps : entier vers nombre à virgule et retour, les 5 opérations avec les avertissements
///    « Overflow » et « Division by zero » (le programme continue avec ±1.70141E+38, comme le Locomotive) ;
///  - RealPrint : l'affichage d'un nombre à virgule exactement comme PRINT (9 chiffres, ou écriture avec E).
/// </summary>
internal static class RealRuntime
{
    const int TxtOutput = RuntimeLibrary.TxtOutput;

    public const string IntToRealLabel = "rt_fint";      // HL = entier signé, DE = où ranger le nombre à virgule
    public const string RealToIntLabel = "rt_fr2i";      // HL = adresse du nombre -> HL = entier arrondi (« Overflow » sinon)
    public const string AddLabel = "rt_fop_add";         // (HL) = (HL) + (DE)
    public const string SubLabel = "rt_fop_sub";         // (HL) = (HL) - (DE)
    public const string MulLabel = "rt_fop_mul";         // (HL) = (HL) * (DE)
    public const string DivLabel = "rt_fop_div";         // (HL) = (HL) / (DE)
    public const string PowLabel = "rt_fop_pow";         // (HL) = (HL) ^ (DE)
    public const string PrintLabel = "rt_fprint";        // HL = adresse du nombre : l'affiche comme PRINT
    public const string PrintFitLabel = "rt_fprint_fit"; // pareil, après être passé à la ligne s'il ne tient pas (D073)

    // Fonctions (lot F2, D069) ; HL = adresse du nombre, gardé
    public const string OverflowPositiveLabel = "rt_fovf_pos";     // (HL) trop grand : « Overflow », +1.70141E+38
    public const string AbsLabel = "rt_fabs";                      // (HL) = ABS((HL))
    public const string CheckNotNegativeLabel = "rt_fchk_neg";     // SQR : « Improper argument » si (HL) < 0
    public const string CheckPositiveLabel = "rt_fchk_pos";        // LOG : « Improper argument » si (HL) <= 0
    public const string RoundLabel = "rt_fround";                  // (HL) = ROUND((HL), A)
    public const string FixLabel = "rt_ffix";                      // (HL) = FIX((HL))
    public const string FloorLabel = "rt_ffloor";                  // (HL) = INT((HL))
    public const string RndLabel = "rt_frnd";                      // (HL) = RND ; A = 1 suivant, 0 le même, &FF repart

    public static void EmitOps(Z80Emitter e)
    {
        // ---- entier signé (HL) vers nombre à virgule rangé en DE ----
        e.Label(IntToRealLabel);
        e.LdAH();
        e.AndImm(0x80);
        e.JpZLabel(FirmwareMaths.IntegerToReal.Label);    // positif : A = 0
        e.LdAL();                                         // négatif : HL = -HL, A = &80
        e.Cpl();
        e.LdLA();
        e.LdAH();
        e.Cpl();
        e.LdHA();
        e.IncHl();
        e.LdAImm(0x80);
        e.JpLabel(FirmwareMaths.IntegerToReal.Label);

        // ---- nombre à virgule (HL) vers entier signé dans HL, arrondi ; « Overflow » arrête le programme ----
        e.Label(RealToIntLabel);
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.PopHl();
        e.AndImm(0x80);
        e.PushAf();                                       // signe du nombre
        e.CallLabel(FirmwareMaths.RealToInteger.Label);   // HL = valeur sans signe, retenue = bon
        e.JpNcLabel("rt_fr2i_ovf_pop");
        e.PopAf();
        e.OrA();
        e.JrNzLabel("rt_fr2i_neg");
        e.LdAH();                                         // positif : 32767 au plus
        e.AndImm(0x80);
        e.RetZ();
        e.JpLabel("rt_fr2i_ovf");
        e.Label("rt_fr2i_neg");
        e.LdAL();                                         // négatif : HL = -HL, 32768 au plus
        e.Cpl();
        e.LdLA();
        e.LdAH();
        e.Cpl();
        e.LdHA();
        e.IncHl();
        e.LdAH();
        e.AndImm(0x80);
        e.RetNz();
        e.LdAH();
        e.OrL();
        e.RetZ();
        e.JpLabel("rt_fr2i_ovf");
        e.Label("rt_fr2i_ovf_pop");
        e.PopAf();
        e.Label("rt_fr2i_ovf");
        e.LdHlLabel("rt_fr_msg_ovf");
        e.JpLabel(RuntimeLibrary.ErrorLabel);

        // ---- signe d'un résultat trop grand, noté avant le calcul (HL et DE gardés) ----
        e.Label("rt_fs_left");                            // signe du nombre de gauche
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.AndImm(0x80);
        e.PopHl();
        e.LdMemA("rt_fsign");
        e.Ret();
        e.Label("rt_fs_xor");                             // signe du produit ou du quotient
        e.PushHl();
        e.PushDe();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.IncDe();
        e.IncDe();
        e.IncDe();
        e.LdADe();
        e.XorMemHl();
        e.AndImm(0x80);
        e.PopDe();
        e.PopHl();
        e.LdMemA("rt_fsign");
        e.Ret();

        // ---- les opérations : HL = gauche (et résultat), DE = droite ----
        e.Label(AddLabel);
        e.CallLabel("rt_fs_left");
        e.PushHl();
        e.CallLabel(FirmwareMaths.Add.Label);
        e.PopHl();
        e.RetC();
        e.JpLabel("rt_fovf");

        e.Label(SubLabel);                                // la ROM fait (DE) - (HL) : on change ensuite le signe
        e.CallLabel("rt_fs_left");
        e.PushHl();
        e.CallLabel(FirmwareMaths.ReverseSubtract.Label);
        e.PopHl();
        e.JpNcLabel("rt_fovf");
        e.JpLabel(FirmwareMaths.Negate.Label);

        e.Label(MulLabel);
        e.CallLabel("rt_fs_xor");
        e.PushHl();
        e.CallLabel(FirmwareMaths.Multiply.Label);
        e.PopHl();
        e.RetC();
        e.JpLabel("rt_fovf");

        e.Label(PowLabel);
        e.XorA();
        e.LdMemA("rt_fsign");
        e.PushHl();
        e.CallLabel(FirmwareMaths.Power.Label);
        e.PopHl();
        e.RetC();
        e.JpLabel("rt_fovf");

        e.Label(DivLabel);
        e.CallLabel("rt_fs_xor");
        e.PushHl();
        e.PushDe();
        e.CallLabel(FirmwareMaths.Divide.Label);
        e.PopDe();
        e.PopHl();
        e.RetC();
        e.PushHl();                                       // diviseur nul (exposant à 0) : « Division by zero »
        e.ExDeHl();
        e.LdDeImm(4);
        e.AddHlDe();
        e.LdAHl();
        e.PopHl();
        e.OrA();
        e.JpNzLabel("rt_fovf");
        e.LdDeLabel("rt_fr_msg_div0");
        e.JpLabel("rt_fwarn_max");

        // ---- résultat trop grand : message sur sa ligne, puis (HL) = ±1.70141E+38, et on continue ----
        e.Label("rt_fovf");
        e.LdDeLabel("rt_fr_msg_ovf");
        e.Label("rt_fwarn_max");
        e.PushHl();
        e.ExDeHl();
        e.CallLabel("rt_fwarn");
        e.PopHl();
        e.PushHl();
        e.StoreHlImm(0xFF);
        e.IncHl();
        e.StoreHlImm(0xFF);
        e.IncHl();
        e.StoreHlImm(0xFF);
        e.IncHl();
        e.LdAMem("rt_fsign");
        e.OrImm(0x7F);
        e.LdHlA();
        e.IncHl();
        e.StoreHlImm(0xFF);
        e.PopHl();
        e.Ret();

        // HL = message, puis passage à la ligne
        // (là où est le curseur, comme le vrai BASIC : « 40000 Overflow », capture du 15 nov. 2026, D070)
        // ON ERROR GOTO l'intercepte comme une erreur (D080)
        e.Label("rt_fwarn");
        e.JpLabel(RuntimeLibrary.ErrorSoftLabel);

        // ---- fonctions (lot F2, D069) ----
        e.Label(OverflowPositiveLabel);
        e.XorA();
        e.LdMemA("rt_fsign");
        e.JpLabel("rt_fovf");

        e.Label(AbsLabel);
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.AndImm(0x7F);
        e.LdHlA();
        e.PopHl();
        e.Ret();

        e.Label(CheckNotNegativeLabel);                   // négatif (et pas zéro) : erreur
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.AndImm(0x80);
        e.IncHl();
        e.JrZLabel("rt_fchk_ok");
        e.LdAHl();
        e.OrA();
        e.JrZLabel("rt_fchk_ok");
        e.JrLabel("rt_fchk_bad");
        e.Label(CheckPositiveLabel);                      // négatif ou zéro : erreur
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.AndImm(0x80);
        e.IncHl();
        e.JrNzLabel("rt_fchk_bad");
        e.LdAHl();
        e.OrA();
        e.JrZLabel("rt_fchk_bad");
        e.Label("rt_fchk_ok");
        e.PopHl();
        e.Ret();
        e.Label("rt_fchk_bad");
        e.PopHl();
        e.LdHlLabel("rt_fr_msg_imp");
        e.JpLabel(RuntimeLibrary.ErrorLabel);

        // Partie entière d'un nombre positif ou nul, en place (HL gardé). La ROM (REAL FIX) rend un entier
        // de 4 octets : BINARY TO REAL le remet en nombre à virgule (constaté sur le CPC, D069).
        e.Label("rt_ftrunc");
        e.PushHl();
        e.LdDeImm(4);
        e.AddHlDe();
        e.LdAHl();
        e.OrA();
        e.JrZLabel("rt_ftrunc_end");                      // zéro
        e.CpImm(160);
        e.JrNcLabel("rt_ftrunc_end");                     // 2^32 ou plus : déjà entier
        e.CpImm(129);
        e.JrNcLabel("rt_ftrunc_rom");
        e.StoreHlImm(0);                                  // moins de 1 : zéro
        e.Label("rt_ftrunc_end");
        e.PopHl();
        e.Ret();
        e.Label("rt_ftrunc_rom");
        e.PopHl();
        e.PushHl();
        e.CallLabel(FirmwareMaths.Fix.Label);
        e.PopHl();
        e.PushHl();
        e.XorA();
        e.CallLabel(FirmwareMaths.BinaryToReal.Label);
        e.PopHl();
        e.Ret();

        // Signe à part (rt_fxs) et nombre rendu positif
        e.Label("rt_fsplit");
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.AndImm(0x80);
        e.LdMemA("rt_fxs");
        e.LdAHl();
        e.AndImm(0x7F);
        e.LdHlA();
        e.PopHl();
        e.Ret();
        // Le signe revient (sauf pour zéro)
        e.Label("rt_fsign_back");
        e.PushHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.IncHl();
        e.LdAHl();
        e.DecHl();
        e.OrA();
        e.JrZLabel("rt_fsign_zero");
        e.LdAMem("rt_fxs");
        e.OrMemHl();
        e.LdHlA();
        e.Label("rt_fsign_zero");
        e.PopHl();
        e.Ret();

        // FIX : vers zéro
        e.Label(FixLabel);
        e.CallLabel("rt_fsplit");
        e.CallLabel("rt_ftrunc");
        e.JpLabel("rt_fsign_back");

        // INT : vers le bas (un négatif qui n'est pas entier perd 1 de plus : INT(-2.7) = -3)
        e.Label(FloorLabel);
        e.CallLabel("rt_fsplit");
        e.PushHl();
        e.LdDeLabel("rt_fxo");
        e.LdBcImm(5);
        e.Ldir();
        e.PopHl();
        e.CallLabel("rt_ftrunc");
        e.LdAMem("rt_fxs");
        e.OrA();
        e.JpZLabel("rt_fsign_back");
        e.PushHl();
        e.LdDeLabel("rt_fxo");
        e.CallLabel(FirmwareMaths.Compare.Label);
        e.PopHl();
        e.OrA();
        e.JpZLabel("rt_fsign_back");
        e.PushHl();
        e.LdDeLabel("rt_fone");
        e.CallLabel(FirmwareMaths.Add.Label);
        e.PopHl();
        e.JpLabel("rt_fsign_back");

        // ROUND : x * 10^A, + 0,5 sur la valeur sans signe, partie entière, signe remis, puis / 10^A
        e.Label(RoundLabel);
        e.LdMemA("rt_frn");
        e.PushHl();
        e.CallLabel(FirmwareMaths.Times10Power.Label);
        e.PopHl();
        e.CallLabel("rt_fsplit");
        e.PushHl();
        e.LdDeLabel("rt_fhalf");
        e.CallLabel(FirmwareMaths.Add.Label);
        e.PopHl();
        e.CallLabel("rt_ftrunc");
        e.CallLabel("rt_fsign_back");
        e.LdAMem("rt_frn");
        e.Cpl();
        e.IncA();
        e.JpLabel(FirmwareMaths.Times10Power.Label);

        e.Label("rt_fxs");
        e.Reserve(1);
        e.Label("rt_fxo");
        e.Reserve(5);
        e.Label("rt_fone");
        e.Data(LocoReal.Encode(1)!);
        e.Label("rt_frn");
        e.Reserve(1);
        e.Label("rt_fhalf");
        e.Data(LocoReal.Encode(0.5)!);
        e.Label("rt_fr_msg_imp");
        e.Data(Ascii("Improper argument"));

        e.Label("rt_fsign");
        e.Reserve(1);
        e.Label("rt_fr_msg_ovf");
        e.Data(Ascii("Overflow"));
        e.Label("rt_fr_msg_div0");
        e.Data(Ascii("Division by zero"));
        e.Label("rt_fr_crlf");
        e.Data(Ascii("\r\n"));
    }

    /// <summary>
    /// Affichage (règle tirée des captures du vrai BASIC, D068) : le nombre est ramené entre 1E8 et 1E9
    /// (REAL *10^A), arrondi à un entier de 9 chiffres ; écrit sans exposant s'il tient en 9 chiffres,
    /// sinon avec 6 chiffres et E+nn ou E-nn. Espace (ou « - ») devant, espace après.
    /// </summary>
    public static void EmitPrint(Z80Emitter e)
    {
        e.Label(PrintLabel);
        e.CallLabel("rt_fp_text");
        e.LdAImm(' ');
        e.CallLabel("rt_fp_out");
        e.Ret();

        // PRINT : on compte d'abord les caractères (la sortie est détournée vers un compteur), puis on vérifie que le
        // nombre tient dans la fin de la ligne (rt_pf_fit, D073), puis on l'affiche.
        e.Label(PrintFitLabel);
        e.PushHl();
        e.XorA();
        e.LdMemA("rt_fp_fitcnt");
        e.LdHlLabel("rt_fp_fitinc");
        e.LdMemHl("rt_fp_vec");
        e.PopHl();
        e.PushHl();
        e.CallLabel("rt_fp_text");
        e.LdHlImm(TxtOutput);
        e.LdMemHl("rt_fp_vec");
        e.LdAMem("rt_fp_fitcnt");
        e.CallLabel(RuntimeLibrary.PrintFitLabel);
        e.PopHl();
        e.JpLabel(PrintLabel);
        e.Label("rt_fp_fitinc");
        e.PushHl();
        e.LdHlLabel("rt_fp_fitcnt");
        e.IncMemHl();
        e.PopHl();
        e.Ret();
        e.Label("rt_fp_fitcnt");
        e.Reserve(1);

        e.Label("rt_fp_text");
        e.LdDeLabel("rt_fp_x");
        e.LdBcImm(5);
        e.Ldir();
        e.LdAMem("rt_fp_x4");
        e.OrA();
        e.JpNzLabel("rt_fp_nz");
        e.LdAImm(' ');                                    // zéro : « 0 » (même -0)
        e.CallLabel("rt_fp_out");
        e.LdAImm('0');
        e.CallLabel("rt_fp_out");
        e.Ret();

        e.Label("rt_fp_nz");
        e.CallLabel("rt_fp_conv");
        e.JpLabel("rt_fp_sgn");

        // Chiffres d'un nombre non nul (aussi pour PRINT USING, D074) : rt_fp_x -> 9 chiffres rt_fp_d0..d8
        // (valeur = d0,d1d2... x 10^rt_fp_e), rt_fp_neg (&80 si négatif), rt_fp_nd
        e.Label("rt_fp_conv");
        e.LdAMem("rt_fp_x3");                             // signe à part, nombre rendu positif
        e.LdBA();
        e.AndImm(0x80);
        e.LdMemA("rt_fp_neg");
        e.LdAB();
        e.AndImm(0x7F);
        e.LdMemA("rt_fp_x3");

        // Puissance de 10 approchée : e10 = partie entière de (exposant - 129) * 77 / 256 (77/256 ≈ log10 2)
        e.LdAMem("rt_fp_x4");
        e.SubImm(129);
        e.LdLA();
        e.AddAA();
        e.SbcAA();
        e.LdHA();                                         // HL = exposant - 129, avec son signe
        e.LdDH();
        e.LdEL();
        e.AddHlHl();
        e.AddHlHl();
        e.AddHlHl();
        e.AddHlDe();                                      // 9
        e.AddHlHl();
        e.AddHlDe();                                      // 19
        e.AddHlHl();
        e.AddHlHl();
        e.AddHlDe();                                      // 77
        e.LdAH();
        e.LdMemA("rt_fp_e");
        e.LdBA();
        e.LdAImm(8);
        e.SubB();                                         // A = 8 - e10
        e.LdHlLabel("rt_fp_x");
        e.CallLabel(FirmwareMaths.Times10Power.Label);

        // Ajustement : le nombre doit être au moins 1E8 et moins de 1E9
        e.Label("rt_fp_fix");
        e.LdHlLabel("rt_fp_x");
        e.LdDeLabel("rt_fp_1e9");
        e.CallLabel(FirmwareMaths.Compare.Label);
        e.CpImm(0xFF);
        e.JpZLabel("rt_fp_lt9");
        e.LdAImm(0xFF);                                   // trop grand : divisé par 10
        e.LdHlLabel("rt_fp_x");
        e.CallLabel(FirmwareMaths.Times10Power.Label);
        e.LdHlLabel("rt_fp_e");
        e.IncMemHl();
        e.JpLabel("rt_fp_fix");
        e.Label("rt_fp_lt9");
        e.LdHlLabel("rt_fp_x");
        e.LdDeLabel("rt_fp_1e8");
        e.CallLabel(FirmwareMaths.Compare.Label);
        e.CpImm(0xFF);
        e.JpNzLabel("rt_fp_scaled");
        e.LdAImm(1);                                      // trop petit : multiplié par 10
        e.LdHlLabel("rt_fp_x");
        e.CallLabel(FirmwareMaths.Times10Power.Label);
        e.LdHlLabel("rt_fp_e");
        e.DecMemHl();
        e.JpLabel("rt_fp_lt9");

        // Entier arrondi sur 32 bits : mantisse (avec son 1 de tête) décalée de 160 - exposant, .5 vers le haut
        e.Label("rt_fp_scaled");
        e.LdHlLabel("rt_fp_x");
        e.LdDeLabel("rt_fp_n0");
        e.LdBcImm(4);
        e.Ldir();
        e.LdAMem("rt_fp_n3");
        e.OrImm(0x80);
        e.LdMemA("rt_fp_n3");
        e.LdAMem("rt_fp_x4");
        e.LdBA();
        e.LdAImm(159);
        e.SubB();
        e.LdBA();                                         // B = décalages avant le dernier (1 à 4)
        e.Label("rt_fp_sh");
        e.CallLabel("rt_fp_shr");
        e.DjnzLabel("rt_fp_sh");
        e.LdHlLabel("rt_fp_n0");                          // + 1 (le demi), puis le dernier décalage
        e.IncMemHl();
        e.JrNzLabel("rt_fp_inc");
        e.IncHl();
        e.IncMemHl();
        e.JrNzLabel("rt_fp_inc");
        e.IncHl();
        e.IncMemHl();
        e.JrNzLabel("rt_fp_inc");
        e.IncHl();
        e.IncMemHl();
        e.Label("rt_fp_inc");
        e.CallLabel("rt_fp_shr");

        // Les 9 chiffres, du dernier au premier ; s'il reste 1, le nombre arrondi valait 1E9
        e.LdDeLabel("rt_fp_d8");
        e.LdCImm(9);
        e.Label("rt_fp_dl");
        e.PushDe();
        e.PushBc();
        e.CallLabel("rt_fp_div10");
        e.PopBc();
        e.PopDe();
        e.StoreDeA();
        e.DecDe();
        e.DecC();
        e.JrNzLabel("rt_fp_dl");
        e.LdAMem("rt_fp_n0");
        e.OrA();
        e.JrZLabel("rt_fp_count9");
        e.LdAImm(1);
        e.LdMemA("rt_fp_d0");
        e.LdHlLabel("rt_fp_e");
        e.IncMemHl();
        e.Label("rt_fp_count9");
        e.LdHlLabel("rt_fp_d8");
        e.LdBImm(9);
        e.CallLabel("rt_fp_count");

        e.Ret();

        // Signe
        e.Label("rt_fp_sgn");
        e.LdAMem("rt_fp_neg");
        e.OrA();
        e.LdAImm(' ');
        e.JrZLabel("rt_fp_sign");
        e.LdAImm('-');
        e.Label("rt_fp_sign");
        e.CallLabel("rt_fp_out");

        // Sans exposant ?  e10 de 0 à 8 : 12.5 ; e10 négatif : 0.0025 si les chiffres tiennent en 9
        e.LdAMem("rt_fp_e");
        e.CpImm(9);
        e.JpCLabel("rt_fp_fixed");
        e.AndImm(0x80);
        e.JpZLabel("rt_fp_exp");
        e.LdAMem("rt_fp_e");
        e.Cpl();                                          // zéros après la virgule : -e10 - 1
        e.LdBA();
        e.LdAMem("rt_fp_nd");
        e.AddAB();
        e.CpImm(10);
        e.JpNcLabel("rt_fp_exp");
        e.LdAImm('0');
        e.CallLabel("rt_fp_out");
        e.LdAImm('.');
        e.CallLabel("rt_fp_out");
        e.LdAB();
        e.OrA();
        e.JrZLabel("rt_fp_small");
        e.Label("rt_fp_zero");
        e.LdAImm('0');
        e.CallLabel("rt_fp_out");
        e.DjnzLabel("rt_fp_zero");
        e.Label("rt_fp_small");
        e.LdHlLabel("rt_fp_d0");
        e.LdAMem("rt_fp_nd");
        e.LdBA();
        e.JpLabel("rt_fp_digits");

        e.Label("rt_fp_fixed");                           // A = e10 : e10 + 1 chiffres avant la virgule
        e.IncA();
        e.LdCA();
        e.LdBA();
        e.LdHlLabel("rt_fp_d0");
        e.CallLabel("rt_fp_digits");
        e.LdAMem("rt_fp_nd");
        e.SubC();
        e.RetZ();
        e.RetC();
        e.LdBA();
        e.LdAImm('.');
        e.CallLabel("rt_fp_out");
        e.JpLabel("rt_fp_digits");

        // Avec exposant : 6 chiffres, arrondis
        e.Label("rt_fp_exp");
        e.LdAMem("rt_fp_d6");
        e.CpImm(5);
        e.JpCLabel("rt_fp_e6");
        e.LdHlLabel("rt_fp_d5");
        e.LdBImm(6);
        e.Label("rt_fp_round");
        e.IncMemHl();
        e.LdAHl();
        e.CpImm(10);
        e.JpCLabel("rt_fp_e6");
        e.StoreHlImm(0);
        e.DecHl();
        e.DjnzLabel("rt_fp_round");
        e.LdAImm(1);                                      // 9.99999x devient 1 et l'exposant grandit
        e.LdMemA("rt_fp_d0");
        e.LdHlLabel("rt_fp_e");
        e.IncMemHl();
        e.Label("rt_fp_e6");
        e.LdHlLabel("rt_fp_d5");
        e.LdBImm(6);
        e.CallLabel("rt_fp_count");
        e.LdHlLabel("rt_fp_d0");
        e.LdBImm(1);
        e.CallLabel("rt_fp_digits");
        e.LdAMem("rt_fp_nd");
        e.DecA();
        e.JrZLabel("rt_fp_ee");
        e.LdBA();
        e.LdAImm('.');
        e.CallLabel("rt_fp_out");
        e.CallLabel("rt_fp_digits");
        e.Label("rt_fp_ee");
        e.LdAImm('E');
        e.CallLabel("rt_fp_out");
        e.LdAMem("rt_fp_e");
        e.LdBA();
        e.AndImm(0x80);
        e.LdAImm('+');
        e.JrZLabel("rt_fp_esign");
        e.LdAB();
        e.Cpl();
        e.IncA();
        e.LdBA();
        e.LdAImm('-');
        e.Label("rt_fp_esign");
        e.CallLabel("rt_fp_out");
        e.LdAB();
        e.LdCImm(0);
        e.Label("rt_fp_tens");
        e.CpImm(10);
        e.JrCLabel("rt_fp_units");
        e.SubImm(10);
        e.IncC();
        e.JrLabel("rt_fp_tens");
        e.Label("rt_fp_units");
        e.LdBA();
        e.LdAC();
        e.AddAImm('0');
        e.CallLabel("rt_fp_out");
        e.LdAB();
        e.AddAImm('0');
        e.CallLabel("rt_fp_out");
        e.Ret();

        // HL = premier chiffre, B = nombre de chiffres (au moins 1) : les affiche ; HL avance, C est gardé
        e.Label("rt_fp_digits");
        e.LdAHl();
        e.AddAImm('0');
        e.CallLabel("rt_fp_out");
        e.IncHl();
        e.DjnzLabel("rt_fp_digits");
        e.Ret();

        // HL = dernier chiffre, B = nombre de chiffres : rt_fp_nd = chiffres sans les zéros de fin (au moins 1)
        e.Label("rt_fp_count");
        e.LdAB();
        e.CpImm(1);
        e.JrZLabel("rt_fp_counted");
        e.LdAHl();
        e.OrA();
        e.JrNzLabel("rt_fp_counted");
        e.DecHl();
        e.DecB();
        e.JrLabel("rt_fp_count");
        e.Label("rt_fp_counted");
        e.LdAB();
        e.LdMemA("rt_fp_nd");
        e.Ret();

        // Nombre de 32 bits divisé par 2 (B gardé)
        e.Label("rt_fp_shr");
        e.LdHlLabel("rt_fp_n3");
        e.SrlMemHl();
        e.DecHl();
        e.RrMemHl();
        e.DecHl();
        e.RrMemHl();
        e.DecHl();
        e.RrMemHl();
        e.Ret();

        // Nombre de 32 bits divisé par 10 ; A = reste
        e.Label("rt_fp_div10");
        e.XorA();
        e.LdBImm(32);
        e.Label("rt_fp_dv");
        e.LdHlLabel("rt_fp_n0");
        e.SlaMemHl();
        e.IncHl();
        e.RlMemHl();
        e.IncHl();
        e.RlMemHl();
        e.IncHl();
        e.RlMemHl();
        e.Rla();
        e.CpImm(10);
        e.JrCLabel("rt_fp_dn");
        e.SubImm(10);
        e.LdHlLabel("rt_fp_n0");
        e.IncMemHl();
        e.Label("rt_fp_dn");
        e.DjnzLabel("rt_fp_dv");
        e.Ret();

        // Sortie d'un caractère : l'écran, ou le texte de STR$ (D070)
        e.Label("rt_fp_out");
        e.Bytes(0xC3);
        e.Label("rt_fp_vec");
        e.Word(TxtOutput);

        // Données
        e.Label("rt_fp_x");
        e.Reserve(3);
        e.Label("rt_fp_x3");
        e.Reserve(1);
        e.Label("rt_fp_x4");
        e.Reserve(1);
        for (int k = 0; k < 4; k++)
        {
            e.Label($"rt_fp_n{k}");
            e.Reserve(1);
        }
        for (int k = 0; k < 9; k++)
        {
            e.Label($"rt_fp_d{k}");
            e.Reserve(1);
        }
        e.Label("rt_fp_e");
        e.Reserve(1);
        e.Label("rt_fp_neg");
        e.Reserve(1);
        e.Label("rt_fp_nd");
        e.Reserve(1);
        e.Label("rt_fp_1e8");
        e.Data(LocoReal.Encode(1e8)!);
        e.Label("rt_fp_1e9");
        e.Data(LocoReal.Encode(1e9)!);
    }

    /// <summary>
    /// RND (D069) : nombre de 0 (compris) à 1 (non compris) fait de deux tirages du générateur 16 bits (Rand16) :
    /// premier / 65536 + second / 4294967296. RANDOMIZE agit donc aussi sur RND. La suite n'est pas celle du CPC.
    /// </summary>
    public static void EmitRnd(Z80Emitter e)
    {
        e.Label(RndLabel);
        e.LdMemHl("rt_frnd_dst");
        e.OrA();
        e.JrZLabel("rt_frnd_last");
        e.IncA();
        e.JrNzLabel("rt_frnd_next");
        e.LoadEFromHl();                                  // RND(négatif) : la graine vient des octets du nombre
        e.IncHl();
        e.LoadDFromHl();
        e.IncHl();
        e.LdAHl();
        e.XorE();
        e.LdEA();
        e.IncHl();
        e.LdAHl();
        e.XorD();
        e.LdHA();
        e.LdLE();
        e.CallLabel(RuntimeLibrary.RandomizeLabel);
        e.Label("rt_frnd_next");
        e.CallLabel(RuntimeLibrary.Rand16Label);
        e.LdDeLabel("rt_frnd_v");
        e.XorA();
        e.CallLabel(FirmwareMaths.IntegerToReal.Label);
        e.LdHlLabel("rt_frnd_v");
        e.LdDeLabel("rt_frnd_k16");
        e.CallLabel(FirmwareMaths.Multiply.Label);
        e.CallLabel(RuntimeLibrary.Rand16Label);
        e.LdDeLabel("rt_frnd_t");
        e.XorA();
        e.CallLabel(FirmwareMaths.IntegerToReal.Label);
        e.LdHlLabel("rt_frnd_t");
        e.LdDeLabel("rt_frnd_k32");
        e.CallLabel(FirmwareMaths.Multiply.Label);
        e.LdHlLabel("rt_frnd_v");
        e.LdDeLabel("rt_frnd_t");
        e.CallLabel(FirmwareMaths.Add.Label);
        e.Label("rt_frnd_last");
        e.LdHlLabel("rt_frnd_v");
        e.LdDeMem("rt_frnd_dst");
        e.LdBcImm(5);
        e.Ldir();
        e.Ret();

        e.Label("rt_frnd_dst");
        e.Reserve(2);
        e.Label("rt_frnd_v");
        e.Reserve(5);
        e.Label("rt_frnd_t");
        e.Reserve(5);
        e.Label("rt_frnd_k16");
        e.Data(LocoReal.Encode(1.0 / 65536)!);
        e.Label("rt_frnd_k32");
        e.Data(LocoReal.Encode(1.0 / 4294967296)!);
    }

    public const string ReadRealLabel = "rt_read_f";      // READ A! : DE = où ranger le nombre suivant des DATA (D070)

    /// <summary>
    /// DATA et nombres à virgule (D070). Un élément « 2 » de la table des DATA est un nombre à virgule :
    /// 2, ses 5 octets, la longueur de son écriture, puis les lettres.
    ///  - rt_read_real : READ A (entier) tombe sur un nombre à virgule : arrondi, « Overflow » s'il ne tient pas ;
    ///  - rt_read_f    : READ A! : nombre à virgule tel quel, ou entier converti.
    /// </summary>
    public static void EmitDataReal(Z80Emitter e)
    {
        e.Label("rt_read_real");                          // HL = les 5 octets (venant de rt_read)
        e.CallLabel("rt_dr_skip");
        e.LdDeLabel("rt_dr_tmp");                         // copie : la table des DATA ne doit pas changer
        e.LdBcImm(5);
        e.Ldir();
        e.LdHlLabel("rt_dr_tmp");
        e.JpLabel(RealToIntLabel);

        e.Label(ReadRealLabel);
        e.LdMemDe("rt_dr_dst");
        e.LdHlMem(RuntimeLibrary.DataPointerLabel);
        e.LdDeLabel(RuntimeLibrary.DataEndLabel);
        e.OrA();
        e.SbcHlDe();
        e.AddHlDe();
        e.JpNcLabel("rt_read_none");                      // plus rien à lire
        e.LdAHl();
        e.IncHl();
        e.CpImm(1);
        e.JpZLabel("rt_read_text");                       // un texte : Type mismatch
        e.CpImm(2);
        e.JrZLabel("rt_dr_real");
        e.LoadEFromHl();                                  // entier : converti
        e.IncHl();
        e.LoadDFromHl();
        e.IncHl();
        e.LoadCFromHl();
        e.IncHl();
        e.LdBImm(0);
        e.AddHlBc();
        e.LdMemHl(RuntimeLibrary.DataPointerLabel);
        e.ExDeHl();
        e.LdDeMem("rt_dr_dst");
        e.JpLabel(IntToRealLabel);
        e.Label("rt_dr_real");
        e.CallLabel("rt_dr_skip");
        e.LdDeMem("rt_dr_dst");
        e.LdBcImm(5);
        e.Ldir();
        e.Ret();

        // HL = les 5 octets d'un nombre à virgule : le pointeur des DATA passe à l'élément suivant (HL gardé)
        e.Label("rt_dr_skip");
        e.PushHl();
        e.LdDeImm(5);
        e.AddHlDe();
        e.LoadCFromHl();
        e.IncHl();
        e.LdBImm(0);
        e.AddHlBc();
        e.LdMemHl(RuntimeLibrary.DataPointerLabel);
        e.PopHl();
        e.Ret();

        e.Label("rt_dr_tmp");
        e.Reserve(5);
        e.Label("rt_dr_dst");
        e.Reserve(2);
    }

    public const string InputLabel = "rt_input_real";   // DE = où ranger : lit le nombre tapé (retenue = « Redo »)

    /// <summary>
    /// INPUT A! (D068) : lit dans la ligne tapée (rt_inp_ptr, module Input) un nombre écrit comme dans un programme :
    /// espaces, signe, chiffres avec au plus un point, puis E et l'exposant ; puis espaces et « , » ou la fin.
    /// Chaque chiffre : x = x * 10 + chiffre (exact jusqu'à 9 chiffres), puis une seule fois x * 10^(exposant - décimales).
    /// Retenue à 1 si le texte n'est pas un nombre (le programme redemande : « ?Redo from start »).
    /// </summary>
    public static void EmitInput(Z80Emitter e)
    {
        e.Label(InputLabel);
        e.LdHlMem("rt_inp_ptr");
        e.XorA();                                         // lecture stricte
        e.CallLabel(ParseLabel);
        e.RetC();
        e.LdMemHl("rt_inp_ptr");
        e.Ret();
    }

    public const string ValLabel = "rt_fval";            // HL = fiche du texte, DE = où ranger le nombre (D070)
    public const string StrLabel = "rt_fstr";            // HL = nombre -> HL = fiche temporaire de son écriture (D070)

    /// <summary>VAL à virgule (D070) : texte copié avec un 0 à la fin, puis lu souplement ; « &amp;FF » : VAL entier.</summary>
    public static void EmitVal(Z80Emitter e)
    {
        e.Label(ValLabel);
        e.LdMemDe("rt_fv_dst");
        e.LdMemHl("rt_fv_desc");
        e.LoadCFromHl();                                  // C = longueur
        e.IncHl();
        e.LoadEFromHl();
        e.IncHl();
        e.LoadDFromHl();
        e.ExDeHl();                                       // HL = lettres
        e.LdDeLabel("rt_fv_buf");
        e.LdAC();
        e.OrA();
        e.JrZLabel("rt_fv_empty");
        e.LdBImm(0);
        e.Ldir();
        e.Label("rt_fv_empty");
        e.XorA();
        e.StoreDeA();
        e.LdHlLabel("rt_fv_buf");
        e.Label("rt_fv_sp");
        e.LdAHl();
        e.CpImm(' ');
        e.JrNzLabel("rt_fv_first");
        e.IncHl();
        e.JrLabel("rt_fv_sp");
        e.Label("rt_fv_first");
        e.CpImm('&');
        e.JrZLabel("rt_fv_hex");
        e.LdHlLabel("rt_fv_buf");
        e.LdDeMem("rt_fv_dst");
        e.LdAImm(1);                                      // lecture souple
        e.JpLabel(ParseLabel);
        e.Label("rt_fv_hex");
        e.LdHlMem("rt_fv_desc");
        e.CallLabel("rt_val");                            // HL = valeur entière
        e.LdDeMem("rt_fv_dst");
        e.JpLabel(IntToRealLabel);

        e.Label("rt_fv_dst");
        e.Reserve(2);
        e.Label("rt_fv_desc");
        e.Reserve(2);
        e.Label("rt_fv_buf");
        e.Reserve(256);
    }

    /// <summary>STR$ à virgule (D070) : l'affichage de PRINT, sans l'espace de fin, envoyé dans un texte.</summary>
    public static void EmitStr(Z80Emitter e)
    {
        e.Label(StrLabel);
        e.PushHl();
        e.LdHlLabel("rt_fs_buf");
        e.LdMemHl("rt_fs_ptr");
        e.LdHlLabel("rt_fs_put");
        e.LdMemHl("rt_fp_vec");                           // les caractères vont dans rt_fs_buf
        e.PopHl();
        e.CallLabel("rt_fp_text");
        e.LdHlImm(TxtOutput);
        e.LdMemHl("rt_fp_vec");                           // retour à l'écran
        e.LdHlMem("rt_fs_ptr");
        e.LdDeLabel("rt_fs_buf");
        e.OrA();
        e.SbcHlDe();
        e.LdCL();                                         // C = nombre de caractères, DE = lettres
        e.JpLabel("rt_sf_mem");

        e.Label("rt_fs_put");                             // A = caractère (les registres sont gardés, comme TXT OUTPUT)
        e.PushHl();
        e.LdHlMem("rt_fs_ptr");
        e.LdHlA();
        e.IncHl();
        e.LdMemHl("rt_fs_ptr");
        e.PopHl();
        e.Ret();

        e.Label("rt_fs_ptr");
        e.Reserve(2);
        e.Label("rt_fs_buf");
        e.Reserve(20);
    }

    public const string ParseLabel = "rt_fparse";

    /// <summary>
    /// Lecture d'un nombre écrit dans un texte (D068, D070). HL = texte terminé par 0, DE = où ranger, A = 0 : lecture
    /// stricte (INPUT : après le nombre, seulement des espaces puis « , » ou la fin ; sinon retenue = erreur), A = 1 :
    /// lecture souple (VAL : on s'arrête au premier caractère qui ne va pas ; sans chiffre, 0 ; « 1E » vaut 1 ;
    /// trop grand : « Overflow » et ±1.70141E+38). Rend HL = suite du texte.
    /// </summary>
    public static void EmitParse(Z80Emitter e)
    {
        e.Label(ParseLabel);
        e.LdMemA("rt_fi_mode");
        e.LdMemDe("rt_fi_dst");
        e.PushHl();
        e.XorA();
        e.LdMemA("rt_fi_neg");
        e.LdMemA("rt_fi_dec");
        e.LdMemA("rt_fi_nd");
        e.LdMemA("rt_fi_pt");
        e.LdMemA("rt_fi_x4");                             // x = 0
        e.LdHlImm(0);
        e.LdMemHl("rt_fi_e");
        e.PopHl();
        e.Label("rt_fi_sp1");
        e.LdAHl();
        e.CpImm(' ');
        e.JrNzLabel("rt_fi_sign");
        e.IncHl();
        e.JrLabel("rt_fi_sp1");
        e.Label("rt_fi_sign");
        e.CpImm('-');
        e.JrNzLabel("rt_fi_plus");
        e.LdAImm(0x80);
        e.LdMemA("rt_fi_neg");
        e.IncHl();
        e.JrLabel("rt_fi_mant");
        e.Label("rt_fi_plus");
        e.CpImm('+');
        e.JrNzLabel("rt_fi_mant");
        e.IncHl();
        e.Label("rt_fi_mant");
        e.LdAHl();                                        // « - 5 » : espaces permis après le signe (vrai BASIC)
        e.CpImm(' ');
        e.JrNzLabel("rt_fi_mant2");
        e.IncHl();
        e.JrLabel("rt_fi_mant");
        e.Label("rt_fi_mant2");
        e.LdMemHl("rt_fi_p");

        // Chiffres et point
        e.Label("rt_fi_mloop");
        e.LdHlMem("rt_fi_p");
        e.LdAHl();
        e.CpImm('.');
        e.JrZLabel("rt_fi_point");
        e.SubImm('0');
        e.CpImm(10);
        e.JrNcLabel("rt_fi_mend");
        e.CallLabel("rt_fi_digit");
        e.JpCLabel("rt_fi_fail");
        e.LdHlMem("rt_fi_p");
        e.IncHl();
        e.LdMemHl("rt_fi_p");
        e.JrLabel("rt_fi_mloop");
        e.Label("rt_fi_point");
        e.LdAMem("rt_fi_pt");
        e.OrA();
        e.JrNzLabel("rt_fi_mend");                        // un second point : fin du nombre (donc erreur plus loin)
        e.IncA();
        e.LdMemA("rt_fi_pt");
        e.IncHl();
        e.LdMemHl("rt_fi_p");
        e.JrLabel("rt_fi_mloop");

        // Exposant
        e.Label("rt_fi_mend");
        e.LdAMem("rt_fi_nd");
        e.OrA();
        e.JrNzLabel("rt_fi_some");
        e.LdAMem("rt_fi_mode");
        e.OrA();
        e.JpZLabel("rt_fi_fail");                         // INPUT : il faut au moins un chiffre
        e.JpLabel("rt_fi_sgn");                           // VAL : sans chiffre, 0
        e.Label("rt_fi_some");
        e.LdHlMem("rt_fi_p");
        e.LdAHl();
        e.CpImm('E');
        e.JrZLabel("rt_fi_exp");
        e.CpImm('e');
        e.JpNzLabel("rt_fi_tail");
        e.Label("rt_fi_exp");
        e.IncHl();
        e.LdBImm(0);                                      // B = 1 : exposant négatif
        e.LdAHl();
        e.CpImm('-');
        e.JrNzLabel("rt_fi_eplus");
        e.LdBImm(1);
        e.IncHl();
        e.JrLabel("rt_fi_edig");
        e.Label("rt_fi_eplus");
        e.CpImm('+');
        e.JrNzLabel("rt_fi_edig");
        e.IncHl();
        e.Label("rt_fi_edig");
        e.LdCImm(0);
        e.LdAHl();
        e.SubImm('0');
        e.CpImm(10);
        e.JrCLabel("rt_fi_eloop");
        e.LdAMem("rt_fi_mode");
        e.OrA();
        e.JpZLabel("rt_fi_fail");                         // INPUT : E sans chiffre
        e.JpLabel("rt_fi_powcalc");                       // VAL : « 1E » vaut 1
        e.Label("rt_fi_eloop");
        e.LdAHl();
        e.SubImm('0');
        e.CpImm(10);
        e.JrNcLabel("rt_fi_eend");
        e.LdDA();
        e.LdAC();
        e.CpImm(20);
        e.JrNcLabel("rt_fi_ebig");                        // déjà très grand : on n'ajoute plus de chiffre
        e.AddAA();
        e.LdEA();
        e.AddAA();
        e.AddAA();
        e.AddAE();
        e.AddAD();
        e.LdCA();
        e.Label("rt_fi_ebig");
        e.IncHl();
        e.JrLabel("rt_fi_eloop");
        e.Label("rt_fi_eend");
        e.LdMemHl("rt_fi_p");
        e.LdLC();
        e.LdHZero();
        e.LdAB();
        e.OrA();
        e.JrZLabel("rt_fi_epos");
        e.XorA();                                         // HL = -HL
        e.SubL();
        e.LdLA();
        e.SbcAA();
        e.SubH();
        e.LdHA();
        e.Label("rt_fi_epos");
        e.LdMemHl("rt_fi_e");

        // Après le nombre : espaces, puis « , » ou la fin
        e.Label("rt_fi_tail");
        e.LdAMem("rt_fi_mode");
        e.OrA();
        e.JpNzLabel("rt_fi_powcalc");                     // VAL : la suite du texte est ignorée
        e.LdHlMem("rt_fi_p");
        e.Label("rt_fi_sp2");
        e.LdAHl();
        e.CpImm(' ');
        e.JrNzLabel("rt_fi_after");
        e.IncHl();
        e.JrLabel("rt_fi_sp2");
        e.Label("rt_fi_after");
        e.OrA();
        e.JrZLabel("rt_fi_ok");
        e.CpImm(',');
        e.JpNzLabel("rt_fi_fail");
        e.IncHl();
        e.Label("rt_fi_ok");
        e.LdMemHl("rt_fi_p");
        e.Label("rt_fi_powcalc");

        // Puissance de 10 : exposant - décimales, ramenée entre -127 et 127
        e.LdHlMem("rt_fi_e");
        e.LdAMem("rt_fi_dec");
        e.LdEA();
        e.LdDZero();
        e.OrA();
        e.SbcHlDe();
        e.LdAH();
        e.OrA();
        e.JrZLabel("rt_fi_hi0");
        e.IncA();
        e.JrZLabel("rt_fi_hiff");
        e.LdAH();
        e.AndImm(0x80);
        e.LdAImm(127);
        e.JrZLabel("rt_fi_pow");
        e.LdAImm(129);                                    // -127
        e.JrLabel("rt_fi_pow");
        e.Label("rt_fi_hi0");
        e.LdAL();
        e.CpImm(128);
        e.JrCLabel("rt_fi_pow");
        e.LdAImm(127);
        e.JrLabel("rt_fi_pow");
        e.Label("rt_fi_hiff");
        e.LdAL();
        e.CpImm(129);
        e.JrNcLabel("rt_fi_pow");
        e.LdAImm(129);
        e.Label("rt_fi_pow");
        e.OrA();
        e.JrZLabel("rt_fi_sgn");
        e.LdHlLabel("rt_fi_x");
        e.CallLabel(FirmwareMaths.Times10Power.Label);
        e.JrNcLabel("rt_fi_fail");
        e.Label("rt_fi_sgn");
        e.LdAMem("rt_fi_neg");
        e.OrA();
        e.JrZLabel("rt_fi_store");
        e.LdHlLabel("rt_fi_x");
        e.CallLabel(FirmwareMaths.Negate.Label);
        e.Label("rt_fi_store");
        e.LdHlLabel("rt_fi_x");
        e.LdDeMem("rt_fi_dst");
        e.LdBcImm(5);
        e.Ldir();
        e.LdHlMem("rt_fi_p");
        e.OrA();                                          // retenue à 0 : tout va bien
        e.Ret();
        e.Label("rt_fi_fail");
        e.LdAMem("rt_fi_mode");
        e.OrA();
        e.JrNzLabel("rt_fi_vovf");
        e.Scf();                                          // INPUT : « ?Redo from start »
        e.Ret();
        e.Label("rt_fi_vovf");                            // VAL trop grand : « Overflow », ±1.70141E+38
        e.LdAMem("rt_fi_neg");
        e.LdMemA("rt_fsign");
        e.LdHlMem("rt_fi_dst");
        e.CallLabel("rt_fovf");
        e.LdHlMem("rt_fi_p");
        e.OrA();
        e.Ret();

        // A = chiffre : x = x * 10 + chiffre ; retenue à 1 si le nombre devient trop grand
        e.Label("rt_fi_digit");
        e.LdCA();
        e.LdHlLabel("rt_fi_nd");
        e.IncMemHl();
        e.LdAMem("rt_fi_pt");
        e.OrA();
        e.JrZLabel("rt_fi_whole");
        e.LdHlLabel("rt_fi_dec");
        e.IncMemHl();                                     // un chiffre après le point
        e.Label("rt_fi_whole");
        e.PushBc();
        e.LdAImm(1);
        e.LdHlLabel("rt_fi_x");
        e.CallLabel(FirmwareMaths.Times10Power.Label);
        e.PopBc();
        e.JrNcLabel("rt_fi_dfail");
        e.LdLC();
        e.LdHZero();
        e.XorA();
        e.LdDeLabel("rt_fi_d");
        e.CallLabel(FirmwareMaths.IntegerToReal.Label);
        e.LdHlLabel("rt_fi_x");
        e.LdDeLabel("rt_fi_d");
        e.CallLabel(FirmwareMaths.Add.Label);
        e.JrNcLabel("rt_fi_dfail");
        e.OrA();
        e.Ret();
        e.Label("rt_fi_dfail");
        e.Scf();
        e.Ret();

        e.Label("rt_fi_x");
        e.Reserve(4);
        e.Label("rt_fi_x4");
        e.Reserve(1);
        e.Label("rt_fi_d");
        e.Reserve(5);
        e.Label("rt_fi_dst");
        e.Reserve(2);
        e.Label("rt_fi_p");
        e.Reserve(2);
        e.Label("rt_fi_e");
        e.Reserve(2);
        foreach (string label in new[] { "rt_fi_neg", "rt_fi_dec", "rt_fi_nd", "rt_fi_pt", "rt_fi_mode" })
        {
            e.Label(label);
            e.Reserve(1);
        }
    }

    static byte[] Ascii(string text) => [.. System.Text.Encoding.ASCII.GetBytes(text), 0];
}
