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

        // HL = message : affiché seul sur sa ligne (à la ligne d'abord si le curseur n'est pas au début)
        e.Label("rt_fwarn");
        e.PushHl();
        e.CallAddress(RuntimeLibrary.TxtGetCursor);
        e.LdAH();
        e.CpImm(1);
        e.JrZLabel("rt_fwarn_col1");
        e.LdHlLabel("rt_fr_crlf");
        e.CallLabel(RuntimeLibrary.PrintStringLabel);
        e.Label("rt_fwarn_col1");
        e.PopHl();
        e.CallLabel(RuntimeLibrary.PrintStringLabel);
        e.LdHlLabel("rt_fr_crlf");
        e.JpLabel(RuntimeLibrary.PrintStringLabel);

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
        e.CallAddress(TxtOutput);
        e.Ret();

        e.Label("rt_fp_text");
        e.LdDeLabel("rt_fp_x");
        e.LdBcImm(5);
        e.Ldir();
        e.LdAMem("rt_fp_x4");
        e.OrA();
        e.JpNzLabel("rt_fp_nz");
        e.LdAImm(' ');                                    // zéro : « 0 » (même -0)
        e.CallAddress(TxtOutput);
        e.LdAImm('0');
        e.CallAddress(TxtOutput);
        e.Ret();

        e.Label("rt_fp_nz");
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

        // Signe
        e.LdAMem("rt_fp_neg");
        e.OrA();
        e.LdAImm(' ');
        e.JrZLabel("rt_fp_sign");
        e.LdAImm('-');
        e.Label("rt_fp_sign");
        e.CallAddress(TxtOutput);

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
        e.CallAddress(TxtOutput);
        e.LdAImm('.');
        e.CallAddress(TxtOutput);
        e.LdAB();
        e.OrA();
        e.JrZLabel("rt_fp_small");
        e.Label("rt_fp_zero");
        e.LdAImm('0');
        e.CallAddress(TxtOutput);
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
        e.CallAddress(TxtOutput);
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
        e.CallAddress(TxtOutput);
        e.CallLabel("rt_fp_digits");
        e.Label("rt_fp_ee");
        e.LdAImm('E');
        e.CallAddress(TxtOutput);
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
        e.CallAddress(TxtOutput);
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
        e.CallAddress(TxtOutput);
        e.LdAB();
        e.AddAImm('0');
        e.CallAddress(TxtOutput);
        e.Ret();

        // HL = premier chiffre, B = nombre de chiffres (au moins 1) : les affiche ; HL avance, C est gardé
        e.Label("rt_fp_digits");
        e.LdAHl();
        e.AddAImm('0');
        e.CallAddress(TxtOutput);
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

    static byte[] Ascii(string text) => [.. System.Text.Encoding.ASCII.GetBytes(text), 0];
}
