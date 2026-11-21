// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Core.Output;

/// <summary>
/// Cartouche d'essai GX4000 (D181, première étape du chemin A : notre propre système, sans ROM d'Amstrad).
/// Elle prépare elle-même l'écran (CRTC, MODE 1, encres), puis dessine 4 bandes verticales : bleu, jaune, bleu ciel,
/// rouge, avec une bordure verte. Sert à vérifier le format .cpr et le démarrage de la console dans un émulateur.
/// </summary>
public static class Gx4000Test
{
    /// <summary>Le programme de démarrage, en assembleur ; il tourne depuis la page 0 de la cartouche, en &amp;0000.</summary>
    public const string Source = """
                DI
                LD SP,&BFF0             ; la pile dans la mémoire vive
        ; Gate Array : MODE 1, ROM basse (la cartouche) gardée, ROM haute coupée
                LD BC,&7F89
                OUT (C),C
        ; CRTC : les 14 premiers registres, valeurs habituelles du CPC (écran de 40 x 25 caractères en &C000)
                LD HL,CRTC
                LD D,0
        CRTCL:  LD B,&BC
                OUT (C),D
                LD A,(HL)
                INC HL
                LD B,&BD
                OUT (C),A
                INC D
                LD A,D
                CP 14
                JR NZ,CRTCL
        ; Encres des stylos 0 à 3, puis la bordure (stylo 16)
                LD HL,ENCRES
                LD D,0
                LD B,&7F
        ENCRE:  OUT (C),D
                LD A,(HL)
                INC HL
                OUT (C),A
                INC D
                LD A,D
                CP 4
                JR NZ,ENCRE
                LD A,16
                OUT (C),A
                LD A,(HL)
                OUT (C),A
        ; Écran : 8 blocs de &800 ; dans chaque bloc, 25 lignes de 80 octets en 4 bandes (stylos 0, 1, 2, 3)
                LD HL,&C000
                LD E,8
        BLOC:   PUSH HL
                LD D,25
        RANG:   XOR A
                CALL BANDE
                LD A,&F0
                CALL BANDE
                LD A,&0F
                CALL BANDE
                LD A,&FF
                CALL BANDE
                DEC D
                JR NZ,RANG
                POP HL
                LD BC,&0800
                ADD HL,BC
                DEC E
                JR NZ,BLOC
        FIN:    JR FIN                  ; la console reste sur cet écran
        BANDE:  LD B,20
        BANDE1: LD (HL),A
                INC HL
                DJNZ BANDE1
                RET
        CRTC:   DEFB 63,40,46,&8E,38,0,25,30,0,7,0,0,&30,0
        ENCRES: DEFB &44,&4A,&53,&4C,&56  ; bleu, jaune vif, bleu ciel vif, rouge vif ; bordure : vert vif
        """;

    /// <summary>Les octets du programme de démarrage.</summary>
    public static byte[] Code()
    {
        var emitter = new Z80Emitter(0);
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "gx_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Cartouche d'essai : " + string.Join(" / ", errors.Select(e => e.ToString())));
        return emitter.ToBytes();
    }

    /// <summary>La cartouche d'essai : une seule page.</summary>
    public static CartridgeImage Build()
    {
        var cartridge = new CartridgeImage();
        cartridge.AddPage(Code());
        return cartridge;
    }
}
