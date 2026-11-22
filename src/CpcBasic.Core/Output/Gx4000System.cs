// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Core.Output;

/// <summary>
/// Notre petit système pour la cartouche GX4000 (chemin A, D181 ; étape du texte, D182). Aucune ROM d'Amstrad :
/// la page 0 de la cartouche contient ce système, le jeu de caractères du projet et le programme compilé.
/// Au démarrage : écran préparé (CRTC, MODE 1, encres du CPC), nos routines branchées aux adresses de la ROM du CPC
/// (&amp;BB5A TXT OUTPUT, &amp;BB75 TXT SET CURSOR, &amp;BC0E SCR SET MODE...), le programme recopié en &amp;4000 et lancé.
/// Le programme compilé ne change pas : il appelle les mêmes adresses, nos routines répondent.
/// Les routines tournent depuis la cartouche (ROM basse, toujours branchée) ; leurs variables sont en &amp;BE00.
/// Manettes (D183) : JOY, et les touches de la table (tir 1 = espace, tir 2 = ENTRÉE, directions = flèches, plus les
/// touches d'INKEY placées par le compilateur). INPUT et LINE INPUT : saisie à la manette (D184). Couleurs :
/// PEN, PAPER, INK, BORDER (D186). Dessin (D187) : PLOT, DRAW, MOVE (et relatifs), TEST, CLG,
/// ORIGIN, GRAPHICS PEN / PAPER ; modes d'écriture XOR, AND, OR, fenêtres graphiques, SCR WRITE (&BDE8) pour MASK, FILL
/// et PAINT, FRAME (D188). Fenêtres de texte (D190) : WINDOW, les 8 flux #0 à #7 (chacun son curseur, sa fenêtre,
/// son stylo et son papier), WINDOW SWAP, CLS dans la fenêtre, défilement de la fenêtre. Encres clignotantes (D191) :
/// INK et BORDER à deux couleurs, SPEED INK, par l'interruption (300 fois par seconde). Son (D192) : SOUND sur les
/// trois canaux, avec le bruit et une file de 4 sons par canal, SQ ; enveloppes ENV et ENT (D193) ; sons retenus,
/// RELEASE et rendez-vous entre canaux (D195). Nombres à virgule (D196) : nos propres calculs (opérations, conversions,
/// SQR, LOG, EXP, puissance, SIN, COS, TAN, ATN) aux adresses de la ROM du 664 / 6128, travail en &amp;B700. Temps et événements (D194) : TIME, AFTER, EVERY, ON SQ GOSUB, ON BREAK. Les autres adresses de la ROM (de &B900 à &BDFF) répondent par un simple retour.
/// </summary>
public static class Gx4000System
{
    /// <summary>Adresses fixes des routines, pour les tests : TXT OUTPUT, SCR SET MODE, TXT SET CURSOR, TXT GET CURSOR.</summary>
    public const int TxtOutputEntry = 0x0040, SetModeEntry = 0x0043, SetCursorEntry = 0x0046, GetCursorEntry = 0x0049;

    /// <summary>
    /// Saisie à la manette (D184), pour les tests : un passage de la boucle (retenue : fini, A = caractère rendu à INPUT ;
    /// sinon on continue). Sans INPUT dans le programme, la routine n'est pas dans la cartouche : l'entrée rend « pas fini ».
    /// </summary>
    public const int WheelStepEntry = 0x004C;

    /// <summary>
    /// Variables de la saisie à la manette : lettre proposée (2 octets), début de son groupe (2 octets), lettre montrée
    /// (sinon carré vide).
    /// </summary>
    public const int WheelLetter = 0xBE0A, WheelGroup = 0xBE0C, WheelShown = 0xBE10;

    /// <summary>
    /// Les groupes de la saisie à la manette (D184, schéma de l'utilisateur) : haut / bas changent de lettre dans le groupe
    /// (en restant appuyé, ça défile), tir 2 passe au groupe suivant, droite écrit la lettre, gauche efface, tir 1 valide.
    /// </summary>
    public static readonly IReadOnlyList<string> WheelGroups =
    [
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "abcdefghijklmnopqrstuvwxyz",
        "0123456789",
        " .,-+*/=!?()<>:;@#$%&_"
    ];

    /// <summary>Variables du système : colonne et ligne du curseur (à partir de 0), octets par caractère, largeur...</summary>
    public const int Variables = 0xBE00;

    const string Template = """
                DI
                JP DEPART
                DEFS 52                 ; jusqu'à &0038
        ; &0038 : interruption, 300 fois par seconde : encres clignotantes (D191)
                JP INTER
                DEFS 5                  ; jusqu'à &0040
        ; &0040 : entrées fixes (tests)
                JP TXTOUT
                JP SETMODE
                JP SETCUR
                JP GETCUR
                JP MOLUN
        DEPART: IM 1
                LD SP,&BFF0
                LD BC,&7F89             ; Gate Array : MODE 1, cartouche gardée en &0000, ROM haute coupée
                OUT (C),C
        ; CRTC : écran de 40 x 25 caractères en &C000
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
        ; Adresses de la ROM du CPC : un simple retour partout, puis nos routines là où elles existent
                LD HL,&B900             ; de &B900 (KL...) à &BDFF
                LD DE,&B901
                LD BC,&04FF
                LD (HL),&C9
                LDIR
                LD HL,BRANCHE
        BRL:    LD E,(HL)
                INC HL
                LD D,(HL)
                INC HL
                LD A,D
                OR E
                JR Z,BRFIN
                LD A,&C3
                LD (DE),A
                INC DE
                LDI
                LDI
                JR BRL
        BRFIN:  LD HL,&BE00             ; variables du système à zéro (&BE00 à &BEFF)
                LD DE,&BE01
                LD BC,255
                LD (HL),0
                LDIR
                LD HL,&B800             ; son (D192) : variables de &B800 à &B8FF à zéro
                LD DE,&B801
                LD BC,255
                LD (HL),0
                LDIR
                CALL SRESET             ; puce de son muette
                LD A,10                 ; encres clignotantes (D191) : SPEED INK 10,10 comme le CPC
                LD (&BE33),A
                LD (&BE34),A
                LD (&BE35),A
        ; Encres du CPC au démarrage (D186) : les 16 stylos, puis la bordure (stylo 16)
                LD HL,ENCRES
                LD E,0
        ENCRE:  LD B,(HL)
                LD C,B                  ; pas de seconde couleur
                INC HL
                PUSH HL
                CALL ENCRE1
                POP HL
                INC E
                LD A,E
                CP 17
                JR NZ,ENCRE
                LD A,1
                LD (&BE13),A            ; stylo 1 sur papier 0
                LD (&BE48),A            ; stylo graphique 1 (D187)
                LD HL,&BE86             ; les 8 flux : stylo 1 sur papier 0 (D190)
                LD DE,8
                LD B,E
        STYLOS: LD (HL),A
                ADD HL,DE
                DJNZ STYLOS
                CALL SETMODE
        ; Le programme compilé, recopié en &4000, puis lancé
                LD HL,PROG
                LD DE,&4000
                LD BC,{0}               ; la partie du programme qui tient dans la page 0
                LDIR
        ; la suite du programme, dans les pages suivantes (D189) : chaque page est branchée en ROM haute (&C000),
        ; par le port &DF (&80 + numéro de page), puis recopiée à la suite ; DE avance tout seul
                LD HL,PAGES
                LD A,(HL)
                INC HL
                OR A
                JR Z,COPFIN
                PUSH DE
                LD BC,&7F81             ; ROM haute branchée (MODE 1, cartouche gardée en &0000)
                OUT (C),C
                POP DE
        COPP:   PUSH AF
                LD C,(HL)               ; &80 + numéro de la page
                INC HL
                LD B,&DF
                OUT (C),C
                LD C,(HL)
                INC HL
                LD B,(HL)
                INC HL                  ; BC = longueur dans cette page
                PUSH HL
                LD HL,&C000
                LDIR
                POP HL
                POP AF
                DEC A
                JR NZ,COPP
                LD BC,&7F89             ; ROM haute coupée de nouveau
                OUT (C),C
        COPFIN: EI                      ; interruptions permises : les encres clignotent (D191)
                CALL &4000
        FIN:    JR FIN                  ; programme fini : la console reste sur son écran

        ; ---------- SCR SET MODE (&BC0E) : A = mode ; écran effacé, curseur en haut à gauche ----------
        SETMODE: AND 3
                CP 3
                JR NZ,SETM1
                XOR A                   ; MODE 3 : comme le MODE 0
        SETM1:  LD (&BE06),A
                LD C,A
                OR &88
                LD B,&7F
                OUT (C),A
                LD A,C
                ADD A,A
                ADD A,A
                LD E,A
                LD D,0
                LD HL,MODES
                ADD HL,DE
                LD DE,&BE02
                LD BC,4
                LDIR
                CALL MAJCOUL            ; octets du stylo et du papier pour ce mode
                LD A,(&BE06)            ; réglages du dessin pour ce mode (D187), 7 octets en &BE60
                LD E,A
                ADD A,A
                ADD A,A
                ADD A,A
                SUB E
                LD E,A
                LD D,0
                LD HL,GMODES
                ADD HL,DE
                LD DE,&BE60
                LD BC,7
                LDIR
                LD HL,0                 ; origine et curseur graphique en 0,0
                LD (&BE40),HL
                LD (&BE42),HL
                LD (&BE44),HL
                LD (&BE46),HL
                LD (&BE68),HL           ; fenêtre graphique : tout l'écran (D188)
                LD (&BE6C),HL
                LD HL,(&BE63)
                DEC HL
                LD (&BE6A),HL
                LD HL,199
                LD (&BE6E),HL
                CALL PLEIN              ; fenêtres de texte : tout l'écran, pour les 8 flux (D190)
                XOR A
                LD (&BE67),A            ; écriture normale (le mode XOR... dure jusqu'à MODE)
        ; ---------- TXT CLEAR WINDOW (&BB6C) : fenêtre effacée, curseur en haut à gauche ----------
        CLS:    LD A,(&BE1D)
                LD B,A
                LD A,(&BE03)
                CP B
                JR NZ,CLSF              ; pas toute la largeur
                LD A,(&BE1E)
                CP 24
                JR NZ,CLSF              ; pas toute la hauteur
                LD HL,&C000             ; tout l'écran : d'un seul coup
                LD DE,&C001
                LD BC,&3FFF
                LD A,(&BE14)            ; l'écran prend la couleur du papier
                LD (HL),A
                LDIR
                JR CLSC
        CLSF:   LD A,(&BE1B)            ; une fenêtre : ligne après ligne
                LD C,A
                LD A,(&BE1E)
                INC A
                LD B,A
        CLSF1:  PUSH BC
                LD A,C
                CALL RADR
                CALL VIDLIG
                POP BC
                INC C
                DJNZ CLSF1
        CLSC:   XOR A
                LD (&BE00),A
                LD (&BE01),A
                RET
        ; par mode : octets par caractère, caractères par ligne, masque et décalage d'un octet d'écran
        MODES:  DEFB 4,20,&C0,2
                DEFB 2,40,&F0,4
                DEFB 1,80,&FF,0

        ; ---------- TXT OUTPUT (&BB5A) : A = caractère ou code de contrôle ; tous les registres gardés ----------
        TXTOUT: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                CP 32
                JP C,CTRL
                CP 127
                JR C,TXT1
                LD A,32                 ; pas encore de dessin au-delà de 126 : un espace
        TXT1:   LD C,A
                PUSH BC
                CALL WRAP
                POP BC
                CALL ADDR               ; HL = adresse dans l'écran
                LD A,C
                SUB 32
                LD E,A
                LD D,0
                EX DE,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD BC,POLICE
                ADD HL,BC
                EX DE,HL                ; DE = dessin du caractère, HL = écran
                LD B,8
        LIGNE:  PUSH BC
                PUSH HL
                LD A,(DE)
                INC DE
                LD C,A
                LD A,(&BE0F)            ; dessin en couleurs inversées (lettre proposée, D184) ?
                OR A
                JR Z,LIGNE2
                LD A,C
                CPL
                LD C,A
        LIGNE2: LD A,(&BE02)
                LD B,A                  ; octets par caractère
        OCTET:  LD A,(&BE04)
                AND C                   ; points allumés (bit 0 de leur encre)
                PUSH BC
                LD B,A
                LD A,(&BE06)
                CP 1
                JR Z,OM1
                JR C,OM0
                LD A,B                  ; MODE 2 : un bit par point
                JR OMX
        OM1:    LD A,B                  ; MODE 1 : les deux bits de chaque point
                RRCA
                RRCA
                RRCA
                RRCA
                OR B
                JR OMX
        OM0:    XOR A                   ; MODE 0 : les quatre bits de chaque point
                BIT 7,B
                JR Z,OM0B
                LD A,&AA
        OM0B:   BIT 6,B
                JR Z,OMX
                OR &55
        OMX:    LD B,A                  ; masque des points de la lettre
                LD A,(&BE15)            ; stylo XOR papier
                AND B
                LD B,A
                LD A,(&BE14)            ; papier
                XOR B                   ; stylo sur les points de la lettre, papier ailleurs
                POP BC
                LD (HL),A
                INC HL
                LD A,(&BE05)
                OR A
                JR Z,OCTET2
        DECAL:  SLA C
                DEC A
                JR NZ,DECAL
        OCTET2: DJNZ OCTET
                POP HL
                LD A,H
                ADD A,8                 ; ligne de points suivante : +&800
                LD H,A
                POP BC
                DJNZ LIGNE
                LD HL,&BE00
                INC (HL)                ; colonne suivante
                JR TXTFIN
        CTRL:   CP 13
                JR NZ,CTRL1
                XOR A
                LD (&BE00),A            ; retour au début de la ligne
                JR TXTFIN
        CTRL1:  CP 10
                JR NZ,CTRL2
                CALL BAS                ; ligne suivante
                JR TXTFIN
        CTRL2:  CP 12
                JR NZ,CTRL3
                CALL CLS
                JR TXTFIN
        CTRL3:  CP 8
                JR NZ,CTRL4
                LD HL,&BE00             ; un caractère à gauche
                LD A,(HL)
                OR A
                JR Z,TXTFIN
                DEC (HL)
                JR TXTFIN
        CTRL4:  CP 9
                JR NZ,TXTFIN
                LD HL,&BE00             ; un caractère à droite
                INC (HL)
        TXTFIN: POP HL
                POP DE
                POP BC
                POP AF
                RET

        ; au bout de la ligne de la fenêtre : début de la ligne suivante ; sous la fenêtre (LOCATE plus bas) : la
        ; fenêtre remonte d'une ligne et l'on écrit sur sa dernière ligne (D190)
        WRAP:   LD A,(&BE1D)
                LD B,A
                LD A,(&BE00)
                CP B
                JR C,WRAP2
                XOR A
                LD (&BE00),A
                CALL BAS
        WRAP2:  LD A,(&BE1E)
                LD B,A
                LD A,(&BE01)
                CP B
                RET C
                RET Z
                LD A,B
                LD (&BE01),A
                JR MONTE
        ; ligne suivante ; en bas de la fenêtre, toute la fenêtre remonte d'une ligne
        BAS:    LD A,(&BE1E)
                LD B,A
                LD HL,&BE01
                LD A,(HL)
                CP B
                JR NC,BAS2
                INC (HL)
                RET
        BAS2:   LD (HL),B
        MONTE:  LD A,(&BE1E)
                OR A
                JR Z,MONTEF             ; une seule ligne : elle est effacée
                LD B,A                  ; lignes à recopier
                LD A,(&BE1B)
        MONTE1: PUSH BC
                PUSH AF
                INC A
                CALL RADR               ; HL = la ligne d'en dessous
                POP AF
                PUSH AF
                PUSH HL
                CALL RADR
                EX DE,HL                ; DE = cette ligne
                POP HL
                CALL COPLIG
                POP AF
                INC A
                POP BC
                DJNZ MONTE1
        MONTEF: LD A,(&BE1C)            ; la dernière ligne de la fenêtre, effacée
                CALL RADR
        ; efface une ligne de caractères de la fenêtre (HL = son début) avec le papier
        VIDLIG: LD B,8
        VIDL1:  PUSH BC
                PUSH HL
                LD A,(&BE1F)
                LD B,A
                LD A,(&BE14)
        VIDL2:  LD (HL),A
                INC HL
                DJNZ VIDL2
                POP HL
                LD A,H
                ADD A,8                 ; ligne de points suivante : +&800
                LD H,A
                POP BC
                DJNZ VIDL1
                RET
        ; recopie une ligne de caractères de la fenêtre : HL -> DE (les 8 lignes de points)
        COPLIG: LD B,8
        COPL1:  PUSH BC
                PUSH HL
                PUSH DE
                LD A,(&BE1F)
                LD C,A
                LD B,0
                LDIR
                POP DE
                POP HL
                LD A,H
                ADD A,8
                LD H,A
                LD A,D
                ADD A,8
                LD D,A
                POP BC
                DJNZ COPL1
                RET

        ; A = ligne de l'écran (à partir de 0) -> HL = &C000 + ligne x 80 + gauche de la fenêtre x octets par caractère
        ; (DE et A changés, BC gardé)
        RADR:   LD L,A
                LD H,0
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD D,H
                LD E,L
                ADD HL,HL
                ADD HL,HL
                ADD HL,DE
                LD DE,&C000
                ADD HL,DE
                LD A,(&BE19)
                OR A
                RET Z
                LD E,A
                LD D,0
                LD A,(&BE02)
        RADR1:  ADD HL,DE
                DEC A
                JR NZ,RADR1
                RET

        ; HL = adresse dans l'écran du curseur (colonne et ligne comptées dans la fenêtre ; BC gardé)
        ADDR:   LD HL,&BE1B
                LD A,(&BE01)
                ADD A,(HL)
                CALL RADR
                LD A,(&BE00)
                OR A
                RET Z
                LD E,A
                LD D,0
                LD A,(&BE02)
        ADDR1:  ADD HL,DE
                DEC A
                JR NZ,ADDR1
                RET

        ; ---------- TXT SET CURSOR (&BB75) : H = colonne, L = ligne (à partir de 1) ----------
        SETCUR: PUSH HL
                DEC H
                DEC L
                LD A,H
                LD (&BE00),A
                LD A,L
                LD (&BE01),A
                POP HL
                RET
        ; ---------- TXT GET CURSOR (&BB78) : H = colonne, L = ligne (à partir de 1), A = 0 ----------
        GETCUR: LD A,(&BE00)
                INC A
                LD H,A
                LD A,(&BE01)
                INC A
                LD L,A
                XOR A
                RET
        ; ---------- fenêtres de texte (D190) ----------
        ; fenêtre du flux actuel : &BE19 gauche, &BE1A droite, &BE1B haut, &BE1C bas (colonnes et lignes de l'écran, à
        ; partir de 0) ; &BE1D largeur, &BE1E hauteur - 1, &BE1F largeur en octets ; &BE18 numéro du flux actuel.
        ; Les 8 flux ont chacun 8 octets en &BE80 + flux x 8 : colonne, ligne, gauche, droite, haut, bas, stylo, papier
        ; (la case du flux actuel n'est remise à jour que quand on change de flux).
        ; TXT GET WINDOW (&BB69) : H = gauche, D = droite, L = haut, E = bas
        FENETRE: LD A,(&BE19)
                LD H,A
                LD A,(&BE1A)
                LD D,A
                LD A,(&BE1B)
                LD L,A
                LD A,(&BE1C)
                LD E,A
                OR A
                RET
        ; TXT WIN ENABLE (&BB66) : H = gauche, D = droite, L = haut, E = bas ; remis dans l'ordre, ramenés dans l'écran ;
        ; curseur en haut à gauche de la fenêtre
        FENON:  PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                LD A,(&BE03)
                DEC A
                LD B,A                  ; dernière colonne de l'écran
                LD A,H
                CP B
                JR C,FEN1
                LD H,B
        FEN1:   LD A,D
                CP B
                JR C,FEN2
                LD D,B
        FEN2:   LD A,L
                CP 25
                JR C,FEN3
                LD L,24
        FEN3:   LD A,E
                CP 25
                JR C,FEN4
                LD E,24
        FEN4:   LD A,D
                CP H
                JR NC,FEN5
                LD D,H                  ; gauche et droite échangées
                LD H,A
        FEN5:   LD A,E
                CP L
                JR NC,FEN6
                LD E,L                  ; haut et bas échangés
                LD L,A
        FEN6:   LD A,H
                LD (&BE19),A
                LD A,D
                LD (&BE1A),A
                LD A,L
                LD (&BE1B),A
                LD A,E
                LD (&BE1C),A
                XOR A
                LD (&BE00),A
                LD (&BE01),A
                CALL WINCALC
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; largeur, hauteur - 1 et largeur en octets de la fenêtre actuelle
        WINCALC: LD A,(&BE19)
                LD B,A
                LD A,(&BE1A)
                SUB B
                INC A
                LD (&BE1D),A
                LD B,A
                LD A,(&BE02)
                LD C,A
                XOR A
        WINC1:  ADD A,C
                DJNZ WINC1
                LD (&BE1F),A
                LD A,(&BE1B)
                LD B,A
                LD A,(&BE1C)
                SUB B
                LD (&BE1E),A
                RET
        ; MODE : toutes les fenêtres de texte couvrent l'écran, curseurs en haut à gauche (stylos et papiers gardés)
        PLEIN:  LD HL,&BE80
                LD A,(&BE03)
                DEC A
                LD C,A
                LD B,8
        PLEIN1: XOR A
                LD (HL),A               ; colonne
                INC HL
                LD (HL),A               ; ligne
                INC HL
                LD (HL),A               ; gauche
                INC HL
                LD (HL),C               ; droite
                INC HL
                LD (HL),A               ; haut
                INC HL
                LD (HL),24              ; bas
                INC HL
                INC HL                  ; stylo et papier gardés
                INC HL
                DJNZ PLEIN1
                XOR A
                LD (&BE19),A
                LD (&BE1B),A
                LD A,C
                LD (&BE1A),A
                LD A,24
                LD (&BE1C),A
                JR WINCALC
        ; TXT STR SELECT (&BBB4) : A = flux (0 à 7) -> A = flux d'avant ; les autres registres gardés
        FLUX:   PUSH BC
                PUSH DE
                PUSH HL
                AND 7
                LD C,A
                LD A,(&BE18)
                CP C
                JR Z,FLUXF              ; le même flux : rien à changer
                PUSH AF
                PUSH BC
                CALL RANGE              ; le flux actuel est noté dans sa case
                POP BC
                LD A,C
                LD (&BE18),A
                CALL REPREND            ; le nouveau flux est repris de sa case
                POP AF
        FLUXF:  POP HL
                POP DE
                POP BC
                RET
        ; TXT SWAP STREAMS (&BBB7) : B, C = deux flux, échangés (curseurs, fenêtres, stylos, papiers)
        ECHANGE: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                PUSH BC
                CALL RANGE
                POP BC
                LD A,B
                AND 7
                CALL CASEA
                PUSH HL
                LD A,C
                AND 7
                CALL CASEA
                POP DE
                LD B,8
        ECH1:   LD A,(DE)
                LD C,(HL)
                LD (HL),A
                LD A,C
                LD (DE),A
                INC HL
                INC DE
                DJNZ ECH1
                CALL REPREND
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; HL = case du flux actuel (CASE) ou du flux A (CASEA)
        CASE:   LD A,(&BE18)
        CASEA:  ADD A,A
                ADD A,A
                ADD A,A
                ADD A,&80
                LD L,A
                LD H,&BE
                RET
        ; le flux actuel est noté dans sa case
        RANGE:  CALL CASE
                EX DE,HL
                LD HL,&BE00
                LDI                     ; colonne
                LDI                     ; ligne
                LD HL,&BE19
                LDI                     ; gauche
                LDI                     ; droite
                LDI                     ; haut
                LDI                     ; bas
                LD A,(&BE13)
                LD (DE),A               ; stylo
                INC DE
                LD A,(&BE16)
                LD (DE),A               ; papier
                RET
        ; le flux actuel est repris de sa case
        REPREND: CALL CASE
                LD DE,&BE00
                LDI
                LDI
                LD DE,&BE19
                LDI
                LDI
                LDI
                LDI
                LD A,(HL)
                LD (&BE13),A
                INC HL
                LD A,(HL)
                LD (&BE16),A
                CALL MAJCOUL
                JP WINCALC
        ; ---------- TXT VALIDATE (&BB87) : H = colonne, L = ligne (à partir de 1, dans la fenêtre) -> la position où
        ; s'écrira le caractère suivant (après la fin de la ligne : début de la ligne suivante ; sous la fenêtre : sa
        ; dernière ligne) ----------
        VALIDE: LD A,(&BE1D)
                CP H
                JR NC,VALID1
                LD H,1
                INC L
        VALID1: LD A,(&BE1E)
                INC A
                CP L
                JR NC,VALID2
                LD L,A
        VALID2: SCF
                RET
        ; ---------- manettes (D183) : lues comme le CPC, par la grille du clavier (lignes 9 et 6) ----------
        ; &BE07 = manette 1, &BE08 = manette 2 : bit à 1 = appuyé (0 haut, 1 bas, 2 gauche, 3 droite, 4 tir 1, 5 tir 2 ;
        ; tir 1 = le bouton principal, valeur 16 de JOY, comme le bouton unique des manettes du CPC, D184)
        LIRE:   DI                      ; l'interruption écrit dans la puce de son par les mêmes ports (D192)
                LD BC,&F40E
                OUT (C),C
                LD BC,&F6C0
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                LD BC,&F792
                OUT (C),C
                LD BC,&F649
                OUT (C),C
                LD B,&F4
                IN A,(C)
                CPL
                AND &3F
                LD (&BE07),A
                LD BC,&F646
                OUT (C),C
                LD B,&F4
                IN A,(C)
                CPL
                AND &3F
                LD (&BE08),A
                LD BC,&F782
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                LD A,(&BE31)            ; appelée pendant l'interruption (événement) : on n'y touche pas (D194 ; pas
                OR A                    ; LD A,I, faussé quand une interruption arrive juste à ce moment)
                RET NZ
                EI
                RET
        ; ---------- KM GET JOYSTICK (&BB24) : H = manette 1, L = manette 2, A = manette 1 ----------
        JOYST:  PUSH BC
                CALL LIRE
                POP BC
                LD A,(&BE08)
                LD L,A
                LD A,(&BE07)
                LD H,A
                RET
        ; ---------- KM TEST KEY (&BB1E) : A = touche ; Z faux si appuyée (table des touches), C = 0 ----------
        TESTKEY: PUSH DE
                PUSH BC
                LD E,A
                CALL LIRE
                LD HL,TOUCHES
        TK1:    LD A,(HL)
                CP &FF
                JR Z,TKNON
                CP E
                JR Z,TK2
                INC HL
                INC HL
                INC HL
                JR TK1
        TK2:    INC HL
                LD A,(HL)               ; 0 = manette 1, 1 = manette 2
                INC HL
                LD D,(HL)               ; boutons à appuyer ensemble
                OR A
                LD A,(&BE07)
                JR Z,TK3
                LD A,(&BE08)
        TK3:    AND D
                CP D
                JR NZ,TKNON
                POP BC
                POP DE
                LD C,0
                OR &FF
                RET
        TKNON:  POP BC
                POP DE
                LD C,0
                XOR A
                RET
        ; ---------- KM READ CHAR (&BB09) / KM READ KEY (&BB1B) : un nouvel appui de la manette 1 ----------
        ; tir 1 = espace, tir 2 = ENTRÉE (13), directions = flèches (&F0 à &F3) ; KEY : numéros de touches
        LITCAR: PUSH HL
                LD HL,CARS
                JR LIT
        LITCLE: PUSH HL
                LD HL,CLES
        LIT:    PUSH BC
                CALL LIRE
                LD A,(&BE09)
                CPL
                LD C,A
                LD A,(&BE07)
                LD (&BE09),A
                AND C                   ; boutons qui viennent d'être appuyés
                LD B,6
        LIT1:   RRCA
                JR C,LIT2
                INC HL
                DJNZ LIT1
                POP BC
                POP HL
                OR A                    ; pas de caractère
                RET
        LIT2:   LD A,(HL)
                POP BC
                POP HL
                SCF
                RET
        CARS:   DEFB &F0,&F1,&F2,&F3,32,13
        CLES:   DEFB 0,2,8,1,47,18
        ; ---------- KM WAIT CHAR (&BB06) / KM WAIT KEY (&BB18) : attend un appui ----------
        ATTCAR: CALL LITCAR
                JR NC,ATTCAR
                RET
        ATTCLE: CALL LITCLE
                JR NC,ATTCLE
                RET

        ; ---------- couleurs (D186) : &BE13 stylo, &BE16 papier, &BE17 / &BE14 leurs octets pour le mode, ----------
        ; &BE15 = stylo XOR papier ; &BE20 à &BE30 : couleur (0 à 26) de chaque stylo et de la bordure
        ; TXT SET PEN (&BB90), TXT GET PEN (&BB93), TXT SET PAPER (&BB96), TXT GET PAPER (&BB99) : A = encre
        SETPEN: PUSH BC
                PUSH DE
                AND 15
                LD (&BE13),A
                CALL MAJCOUL
                POP DE
                POP BC
                RET
        GETPEN: LD A,(&BE13)
                RET
        SETPAP: PUSH BC
                PUSH DE
                AND 15
                LD (&BE16),A
                CALL MAJCOUL
                POP DE
                POP BC
                RET
        GETPAP: LD A,(&BE16)
                RET
        MAJCOUL: LD A,(&BE13)
                CALL OCTENC
                LD (&BE17),A
                LD A,(&BE16)
                CALL OCTENC
                LD (&BE14),A
                LD B,A
                LD A,(&BE17)
                XOR B
                LD (&BE15),A
                RET
        ; A = encre -> A = octet d'écran dont tous les points ont cette encre, dans le mode actuel
        OCTENC: LD C,A
                LD A,(&BE06)
                LD HL,TENC0
                LD B,15
                OR A
                JR Z,OCTE1
                LD HL,TENC1
                LD B,3
                DEC A
                JR Z,OCTE1
                LD HL,TENC2
                LD B,1
        OCTE1:  LD A,C
                AND B
                LD E,A
                LD D,0
                ADD HL,DE
                LD A,(HL)
                RET
        ; SCR SET INK (&BC32) : A = stylo, B = couleur, C = seconde couleur (clignotante si elle est différente)
        SETINK: PUSH BC
                PUSH DE
                PUSH HL
                AND 15
                LD E,A
                CALL ENCRE1
                POP HL
                POP DE
                POP BC
                RET
        ; SCR SET BORDER (&BC38) : B = couleur, C = seconde couleur
        SETBORD: PUSH BC
                PUSH DE
                PUSH HL
                LD E,16
                CALL ENCRE1
                POP HL
                POP DE
                POP BC
                RET
        ; SCR GET INK (&BC35) : A = stylo -> B = C = couleur ; SCR GET BORDER (&BC3B)
        GETINK: AND 15
                JR GETI1
        GETBORD: LD A,16
        GETI1:  PUSH HL
                PUSH DE
                LD E,A
                LD D,0
                LD HL,&BE20
                ADD HL,DE
                LD B,(HL)
                LD HL,&BEC0
                ADD HL,DE
                LD C,(HL)
                POP DE
                POP HL
                RET
        ; E = stylo (16 = bordure), B = couleur, C = seconde couleur (0 à 26) : notées (&BE20 et &BEC0 + stylo), puis
        ; la couleur du moment envoyée au Gate Array, sans interruption entre le choix du stylo et la couleur (D191)
        ENCRE1: LD D,0
                LD HL,&BE20
                ADD HL,DE
                LD (HL),B
                LD HL,&BEC0
                ADD HL,DE
                LD (HL),C
                LD A,(&BE32)
                OR A
                JR Z,ENCR2
                LD B,C                  ; c'est le moment de la seconde couleur
        ENCR2:  DI
                CALL ENVOI
                EI
                RET
        ; E = stylo, B = couleur du CPC : envoyée au Gate Array
        ENVOI:  LD A,B
                CP 27
                JR C,ENC2
                LD A,26
        ENC2:   LD HL,COULEURS
                LD C,A
                LD B,0
                ADD HL,BC
                LD A,(HL)
                LD BC,&7F00
                OUT (C),E
                OUT (C),A
                RET
        ; ---------- encres clignotantes (D191) ----------
        ; &BE32 : 0 = premières couleurs, 1 = secondes ; &BE33 : images restantes ; &BE34, &BE35 : durées de SPEED INK.
        ; Seule l'interruption du retour du balayage (début d'une image, 50 fois par seconde) compte : les couleurs
        ; changent pendant que rien n'est affiché, sans décalage dans l'image (essai de l'utilisateur)
        ; Le son (D192) avance toutes les 3 interruptions : 100 fois par seconde, l'unité des durées de SOUND.
        INTER:  PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                LD A,1
                LD (&BE31),A            ; dans l'interruption (LIRE n'y remet pas les interruptions, D194)
                LD HL,&BE36             ; TIME (D194) : 300 fois par seconde, sur 32 bits
                INC (HL)
                JR NZ,INT0
                INC HL
                INC (HL)
                JR NZ,INT0
                INC HL
                INC (HL)
                JR NZ,INT0
                INC HL
                INC (HL)
        INT0:   LD HL,&B8BA
                DEC (HL)
                JR NZ,INT1
                LD (HL),3
                CALL SONTIC
        INT1:   LD BC,&F500             ; PPI, port B : bit 0 = retour du balayage
                IN A,(C)
                RRA
                JR NC,INTFIN
                LD HL,&BE33
                DEC (HL)
                CALL Z,CLIGNE
                CALL TICKS              ; minuteries (AFTER, EVERY), 50 fois par seconde (D194)
        INTFIN: XOR A
                LD (&BE31),A
                POP HL
                POP DE
                POP BC
                POP AF
                EI
                RET
        ; l'autre couleur de chaque encre, pour la durée qui lui correspond
        CLIGNE: LD A,(&BE32)
                XOR 1
                LD (&BE32),A
                LD HL,&BE34
                JR Z,CLIG1
                INC HL
        CLIG1:  LD A,(HL)
                LD (&BE33),A
                LD HL,&BE20
                LD A,(&BE32)
                OR A
                JR Z,CLIG2
                LD HL,&BEC0
        CLIG2:  LD E,0
        CLIG3:  LD B,(HL)
                INC HL
                PUSH HL
                CALL ENVOI
                POP HL
                INC E
                LD A,E
                CP 17
                JR NZ,CLIG3
                RET
        ; SCR SET FLASHING (&BC3E) : H = durée de la première couleur, L = de la seconde (en 1/50 de seconde) ;
        ; SCR GET FLASHING (&BC41)
        SETCLI: LD A,H
                LD (&BE34),A
                LD A,L
                LD (&BE35),A
                RET
        GETCLI: LD A,(&BE34)
                LD H,A
                LD A,(&BE35)
                LD L,A
                RET
        ; ---------- son (D192, enveloppes D193) ----------
        ; Pour chaque canal (A, B, C : 0, 1, 2), 62 octets en &B800 + canal x 62 (IX) :
        ;  +0 sons en attente (0 à 4), +1 place du prochain dans la file, +2 son en cours (1) ou non (0),
        ;  +3 et +4 centièmes restants (durée donnée), +5 volume, +6 enveloppe de volume, +7 répétitions de l'enveloppe
        ;  de volume qui restent (0 : la durée est donnée par SOUND), +8 et +9 section suivante de l'enveloppe de volume
        ;  (0 : pas d'enveloppe en cours), +10 sections restantes, +11 pas restants, +12 centièmes avant le pas
        ;  suivant, +13 taille du pas, +14 pause entre deux pas ; +15 enveloppe de ton (+ &80 : elle se répète),
        ;  +16 à +22 : la même chose pour l'enveloppe de ton ; +24 et +25 période du ton ; +26 : la file, 4 blocs de
        ;  9 octets comme ceux de SOUND QUEUE (état, enveloppes de volume et de ton, période (2), bruit, volume, durée (2)).
        ; &B8BA : interruptions avant le prochain centième ; &B8BB : registre 7 de la puce (bits à 1 : ton puis bruit
        ; coupés) ; &B8BC : canal traité ; &B8BD, &B8BE : calculs ; &B8BF : adresse du bloc reçu ; &B8C2 : adresses des
        ; 15 enveloppes de volume (ENV), &B8E0 : des 15 enveloppes de ton (ENT), notées par le programme.
        ; Un centième de seconde (interruption) : chaque canal avance
        SONTIC: PUSH IX
                LD IX,&B800
                XOR A
                LD (&B8BC),A
        SONT1:  LD A,(IX+2)
                OR A
                JR Z,SONT2              ; pas de son en cours
                LD A,(IX+7)
                OR A
                JR NZ,SONT0             ; la durée est celle de l'enveloppe
                LD L,(IX+3)
                LD H,(IX+4)
                DEC HL
                LD (IX+3),L
                LD (IX+4),H
                LD A,H
                OR L
                JR Z,SONFIN
        SONT0:  CALL AMPTIC
                JR C,SONFIN
                CALL TONTIC
                JR SONTS
        SONFIN: XOR A                   ; le son est fini
                LD (IX+2),A
                CALL SILENCE
        SONT2:  LD A,(IX+0)
                OR A
                JR Z,SONTS
                CALL LANCE              ; le suivant commence, s'il n'attend rien (D195)
        SONTS:  CALL SQEV
                LD DE,62
                ADD IX,DE
                LD HL,&B8BC
                INC (HL)
                LD A,(HL)
                CP 3
                JR NZ,SONT1
                POP IX
                RET
        ; le canal IX (&B8BC) commence le premier son de sa file
        DEBUT:  LD A,(IX+1)
                LD E,A
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,E
                ADD A,26
                LD E,A
                LD D,0
                PUSH IX
                POP HL
                ADD HL,DE               ; HL = le bloc du son
                LD A,(IX+1)
                INC A
                AND 3
                LD (IX+1),A
                DEC (IX+0)
                INC HL
                LD A,(HL)
                LD (IX+6),A             ; enveloppe de volume
                INC HL
                LD A,(HL)
                AND 15
                LD (IX+15),A            ; enveloppe de ton
                INC HL
                LD E,(HL)               ; période du ton (0 : pas de ton)
                INC HL
                LD D,(HL)
                INC HL
                LD (IX+24),E
                LD (IX+25),D
                PUSH HL
                LD A,D
                OR E
                LD (&B8BD),A
                EX DE,HL
                CALL TONECR
                CALL MASQUE
                LD A,(&B8BD)
                OR A
                CALL MIXB               ; ton coupé ou non
                POP HL
                LD A,(HL)               ; bruit (période, 0 : pas de bruit)
                INC HL
                PUSH HL
                AND 31
                LD E,A
                PUSH AF
                JR Z,DEBUT2
                LD A,6
                CALL PSGW
        DEBUT2: CALL MASQUE
                SLA B
                SLA B
                SLA B
                POP AF
                CALL MIXB               ; bruit coupé ou non
                LD A,(&B8BB)
                LD E,A
                LD A,7
                CALL PSGW
                POP HL
                LD A,(HL)               ; volume
                INC HL
                AND 15
                LD (IX+5),A
                LD E,(HL)               ; durée en centièmes
                INC HL
                LD D,(HL)
                LD A,D
                OR E
                JR Z,DEBZ
                BIT 7,D
                JR NZ,DEBNEG
                XOR A                   ; durée donnée par SOUND
                JR DEBFIX
        DEBZ:   INC A                   ; 0 : la durée de l'enveloppe de volume, une fois
                JR DEBENV
        DEBNEG: XOR A                   ; négative : l'enveloppe de volume, répétée
                SUB E
                JR NZ,DEBENV
                DEC A
        DEBENV: LD DE,0
        DEBFIX: LD (IX+7),A
                LD (IX+3),E
                LD (IX+4),D
                LD (IX+2),1
                LD A,(IX+6)
                CALL AMPDEB
                CALL AMPPAS             ; premier pas : le volume est écrit
                CALL TONDEB
                LD A,(IX+16)
                OR (IX+17)
                RET Z
                JP TONPAS
        ; enveloppe de volume du canal IX : un centième ; retenue : le son est fini (durée donnée par l'enveloppe)
        AMPTIC: LD A,(IX+8)
                OR (IX+9)
                RET Z
                DEC (IX+12)
                RET NZ
        AMPPAS: LD A,(IX+11)
                OR A
                JR NZ,AMPP2
                CALL AMPSEC
                JR NC,AMPP2
                LD A,(IX+7)             ; l'enveloppe est finie
                OR A
                JR Z,AMPTEN             ; durée donnée par SOUND : le volume reste
                DEC (IX+7)
                SCF
                RET Z                   ; plus de répétition : le son est fini
                LD A,(IX+6)
                CALL AMPDEB             ; l'enveloppe recommence
                JR AMPPAS
        AMPTEN: LD (IX+8),A
                LD (IX+9),A
                RET
        AMPP2:  DEC (IX+11)
                LD A,(IX+14)
                LD (IX+12),A            ; pause avant le pas suivant (0 : 256)
                LD A,(IX+5)
                CP 16
                JR Z,AMPP3              ; enveloppe de la puce : le volume n'est pas touché
                ADD A,(IX+13)
                AND 15
                LD (IX+5),A
                LD E,A
                LD A,(&B8BC)
                ADD A,8
                CALL PSGW
        AMPP3:  OR A
                RET
        ; section suivante de l'enveloppe de volume ; retenue : il n'y en a plus
        AMPSEC: LD A,(IX+10)
                OR A
                SCF
                RET Z
                DEC (IX+10)
                LD L,(IX+8)
                LD H,(IX+9)
                LD A,(HL)               ; nombre de pas, ou &80 + forme (enveloppe de la puce)
                INC HL
                BIT 7,A
                JR NZ,AMPHARD
                LD (IX+11),A
                LD A,(HL)
                INC HL
                LD (IX+13),A            ; taille du pas
                LD A,(HL)
                INC HL
                LD (IX+14),A            ; pause
                LD (IX+8),L
                LD (IX+9),H
                LD A,(IX+11)
                OR A
                RET NZ
                LD A,(IX+13)            ; 0 pas : le volume est donné tel quel
                AND 15
                LD (IX+5),A
                LD (IX+13),0
                LD (IX+11),1
                OR A
                RET
        AMPHARD: AND 15                 ; enveloppe de la puce : forme, puis période
                LD E,A
                LD A,13
                CALL PSGW
                LD E,(HL)
                INC HL
                LD A,11
                CALL PSGW
                LD E,(HL)
                INC HL
                LD A,12
                CALL PSGW
                LD (IX+8),L
                LD (IX+9),H
                LD (IX+5),16
                LD E,16
                LD A,(&B8BC)
                ADD A,8
                CALL PSGW               ; volume suivi par l'enveloppe de la puce
                JR AMPSEC               ; elle ne dure pas : section suivante tout de suite
        ; l'enveloppe de volume A (0 ou pas notée : 2 secondes au même volume) commence
        AMPDEB: LD HL,&B8C2
                CALL ENVADR
                LD A,H
                OR L
                JR NZ,AMPD1
                LD HL,ENV0
        AMPD1:  LD A,(HL)
                AND &7F
                LD (IX+10),A
                INC HL
                LD (IX+8),L
                LD (IX+9),H
                XOR A
                LD (IX+11),A
                RET
        ENV0:   DEFB 1,1,0,200
        ; HL = table, A = numéro (1 à 15) -> HL = adresse de l'enveloppe notée (0 : aucune) ; DE changé
        ENVADR: AND 15
                JR Z,ENVA0
                DEC A
                ADD A,A
                LD E,A
                LD D,0
                ADD HL,DE
                LD A,(HL)
                INC HL
                LD H,(HL)
                LD L,A
                RET
        ENVA0:  LD H,A
                LD L,A
                RET
        ; enveloppe de ton du canal IX : un centième
        TONTIC: LD A,(IX+16)
                OR (IX+17)
                RET Z
                DEC (IX+20)
                RET NZ
        TONPAS: LD A,(IX+18)
                OR A
                JR NZ,TONP2
                CALL TONSEC
                JR NC,TONP2
                BIT 7,(IX+15)           ; finie : elle recommence si elle se répète (ENT négatif)
                JR Z,TONTEN
                CALL TONDEB
                JR TONPAS
        TONTEN: XOR A
                LD (IX+16),A
                LD (IX+17),A
                RET
        TONP2:  DEC (IX+18)
                LD A,(IX+22)
                LD (IX+20),A
                LD L,(IX+24)
                LD H,(IX+25)
                LD A,(IX+21)            ; pas, de -128 à 127
                LD E,A
                RLA
                SBC A,A
                LD D,A
                ADD HL,DE
                LD A,H
                AND 15
                LD H,A
                LD (IX+24),L
                LD (IX+25),H
        ; HL = période du ton du canal &B8BC : écrite dans la puce
        TONECR: LD E,L
                LD A,(&B8BC)
                ADD A,A
                CALL PSGW
                INC A
                LD E,H
                JP PSGW
        ; section suivante de l'enveloppe de ton ; retenue : il n'y en a plus
        TONSEC: LD A,(IX+19)
                OR A
                SCF
                RET Z
                DEC (IX+19)
                LD L,(IX+16)
                LD H,(IX+17)
                LD A,(HL)               ; nombre de pas, ou &F0 + haut de la période (période donnée)
                INC HL
                CP &F0
                JR NC,TONABS
                LD (IX+18),A
                LD A,(HL)
                INC HL
                LD (IX+21),A
                LD A,(HL)
                INC HL
                LD (IX+22),A
                LD (IX+16),L
                LD (IX+17),H
                LD A,(IX+18)
                OR A
                RET NZ
                LD (IX+18),1            ; 0 pas : un pas de 0
                LD (IX+21),0
                RET
        TONABS: AND 15
                LD D,A
                LD E,(HL)
                INC HL
                LD A,(HL)
                INC HL
                LD (IX+22),A
                LD (IX+16),L
                LD (IX+17),H
                LD (IX+24),E
                LD (IX+25),D
                LD (IX+18),1
                LD (IX+21),0
                OR A
                RET
        ; l'enveloppe de ton (+15) commence ; aucune : +16 et +17 à 0
        TONDEB: LD A,(IX+15)
                LD HL,&B8E0
                CALL ENVADR
                LD (IX+16),L
                LD (IX+17),H
                LD A,H
                OR L
                RET Z
                LD A,(HL)
                AND &80
                OR (IX+15)
                LD (IX+15),A            ; + &80 : elle se répète
                LD A,(HL)
                AND &7F
                LD (IX+19),A
                INC HL
                LD (IX+16),L
                LD (IX+17),H
                XOR A
                LD (IX+18),A
                RET
        ; le canal &B8BC se tait : volume 0, ton et bruit coupés
        SILENCE: LD E,0
                LD A,(&B8BC)
                ADD A,8
                CALL PSGW
                CALL MASQUE
                LD A,B
                ADD A,A
                ADD A,A
                ADD A,A
                OR B
                LD B,A
                LD A,(&B8BB)
                OR B
                LD (&B8BB),A
                LD E,A
                LD A,7
                JP PSGW
        ; B = 1, 2 ou 4 selon le canal &B8BC
        MASQUE: LD A,(&B8BC)
                LD B,1
                OR A
                RET Z
        MASQ1:  SLA B
                DEC A
                JR NZ,MASQ1
                RET
        ; B = bit du registre 7 ; Z : coupé (bit à 1), sinon allumé (bit à 0)
        MIXB:   LD A,(&B8BB)
                JR NZ,MIXON
                OR B
                JR MIXFIN
        MIXON:  LD C,A
                LD A,B
                CPL
                AND C
        MIXFIN: LD (&B8BB),A
                RET
        ; écrit E dans le registre A de la puce de son (par le PPI) ; A, DE et HL gardés, BC changé
        PSGW:   LD B,&F4
                OUT (C),A
                LD BC,&F6C0
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                LD B,&F4
                OUT (C),E
                LD BC,&F680
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                RET
        ; IX = canal A (0 à 2), noté en &B8BC ; DE gardé
        CANAL:  LD (&B8BC),A
                LD IX,&B800
                OR A
                RET Z
                PUSH DE
                LD DE,62
        CANAL1: ADD IX,DE
                DEC A
                JR NZ,CANAL1
                POP DE
                RET
        ; SOUND RESET (&BCA7) : files vidées, enveloppes oubliées, tout se tait
        SRESET: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                DI
                LD HL,&B800
                LD DE,&B801
                LD BC,253
                LD (HL),0
                LDIR
                LD HL,&BED2             ; événements des canaux (ON SQ) oubliés (D194)
                LD B,6
        SRES1:  LD (HL),0
                INC HL
                DJNZ SRES1
                LD A,3
                LD (&B8BA),A
                LD A,&3F
                LD (&B8BB),A
                LD E,A
                LD A,7
                CALL PSGW
                LD E,0
                LD A,8
                CALL PSGW
                INC A
                CALL PSGW
                INC A
                CALL PSGW
                EI
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; SOUND AMPL ENVELOPE (&BCBC), SOUND TONE ENVELOPE (&BCBF) : A = numéro (1 à 15), HL = bloc de 16 octets
        ; (gardé dans le programme : seule son adresse est notée)
        SENVA:  PUSH DE
                LD DE,&B8C2
                JR SENV
        SENVT:  PUSH DE
                LD DE,&B8E0
        SENV:   PUSH AF
                AND 15
                JR Z,SENVF
                DEC A
                ADD A,A
                ADD A,E
                LD E,A
                DI
                EX DE,HL
                LD (HL),E
                INC HL
                LD (HL),D
                DEC HL
                EX DE,HL
                EI
        SENVF:  POP AF
                POP DE
                RET
        ; SOUND QUEUE (&BCAA) : HL = bloc de 9 octets ; retenue : le son est dans la file de chaque canal choisi ;
        ; sinon une de ces files est pleine (rien n'est fait). Bit 7 de l'état : files vidées d'abord, son en cours arrêté.
        SQUEUE: PUSH BC
                PUSH DE
                PUSH HL
                PUSH IX
                DI
                LD (&B8BF),HL
                LD A,(HL)
                LD (&B8BE),A
                BIT 7,A
                JR Z,SQ2
                LD D,A                  ; vider les files choisies
                XOR A
        SQF:    RR D
                JR NC,SQF2
                PUSH AF
                CALL CANAL
                XOR A
                LD (IX+0),A
                LD (IX+1),A
                LD (IX+2),A
                CALL SILENCE
                POP AF
        SQF2:   INC A
                CP 3
                JR NZ,SQF
        SQ2:    LD A,(&B8BE)            ; une place dans chaque file choisie ?
                LD D,A
                XOR A
        SQR:    RR D
                JR NC,SQR2
                PUSH AF
                CALL CANAL
                POP AF
                LD E,A
                LD A,(IX+0)
                CP 4
                LD A,E
                JR NC,SQPLEIN
        SQR2:   INC A
                CP 3
                JR NZ,SQR
                LD A,(&B8BE)            ; le son au bout de chaque file choisie
                LD D,A
                XOR A
        SQA:    RR D
                JR NC,SQA2
                PUSH AF
                CALL CANAL
                CALL MASQUE             ; un son sur plusieurs canaux : rendez-vous avec les autres (D195)
                LD A,B
                CPL
                LD B,A
                LD A,(&B8BE)
                AND 7
                AND B
                ADD A,A
                ADD A,A
                ADD A,A
                LD (&B8C1),A
                LD A,(IX+1)
                ADD A,(IX+0)
                AND 3
                LD E,A
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,E
                ADD A,26
                PUSH DE
                LD E,A
                LD D,0
                PUSH IX
                POP HL
                ADD HL,DE
                EX DE,HL
                LD HL,(&B8BF)
                LD BC,9
                LDIR
                EX DE,HL
                LD DE,&FFF7             ; -9 : l'état du son copié
                ADD HL,DE
                LD A,(&B8C1)
                OR (HL)
                LD (HL),A
                POP DE
                INC (IX+0)
                POP AF
        SQA2:   INC A
                CP 3
                JR NZ,SQA
                SCF
                JR SQFIN
        SQPLEIN: OR A
        SQFIN:  EI
                POP IX
                POP HL
                POP DE
                POP BC
                RET
        ; SOUND CHECK (&BCAD) : A = canal (1, 2 ou 4) -> A = places libres dans sa file, + &80 si un son est en cours
        SCHECK: PUSH BC
                PUSH DE
                PUSH HL
                PUSH IX
                LD E,0
                BIT 0,A
                JR NZ,SCH1
                INC E
                BIT 1,A
                JR NZ,SCH1
                INC E
        SCH1:   LD A,E
                DI
                CALL CANAL
                LD A,4
                SUB (IX+0)
                LD C,A
                BIT 0,(IX+2)
                JR Z,SCH3
                SET 7,C                 ; un son en cours
                JR SCH2
        SCH3:   LD A,(IX+0)
                OR A
                JR Z,SCH2
                CALL TETE               ; le premier son attend : rendez-vous (bits 3 à 5), retenu (bit 6)
                LD A,(HL)
                AND &78
                OR C
                LD C,A
        SCH2:   LD A,C
                EI
                POP IX
                POP HL
                POP DE
                POP BC
                RET
        ; ---------- sons retenus et rendez-vous (D195) ----------
        ; État d'un son : bits 3 à 5 = rendez-vous avec les canaux A, B, C ; bit 6 = retenu jusqu'à RELEASE.
        ; Le canal IX essaie de commencer le premier son de sa file ; ceux du rendez-vous commencent avec lui
        LANCE:  CALL PRET
                RET NZ                  ; il attend encore
                CALL TETE
                LD A,(HL)
                AND &38
                PUSH AF
                CALL DEBUT
                POP AF
                RET Z                   ; pas de rendez-vous
                LD C,A
                LD A,(&B8BC)
                PUSH AF
                PUSH IX
                LD D,0
        LANC1:  BIT 3,C
                JR Z,LANC2
                PUSH BC
                PUSH DE
                LD A,D
                CALL CANAL
                CALL DEBUT
                POP DE
                POP BC
        LANC2:  SRL C
                INC D
                LD A,D
                CP 3
                JR NZ,LANC1
                POP IX
                POP AF
                LD (&B8BC),A
                RET
        ; le premier son de la file du canal IX peut-il commencer ? Z : oui (pas retenu, et chaque canal du rendez-vous
        ; attend lui aussi ce canal, sans son en cours)
        PRET:   CALL TETE
                LD A,(HL)
                BIT 6,A
                RET NZ
                AND &38
                RET Z
                LD C,A
                CALL MASQUE
                SLA B
                SLA B
                SLA B                   ; le bit de rendez-vous de ce canal
                LD D,0
        PRET1:  BIT 3,C
                JR Z,PRET2
                PUSH BC
                PUSH DE
                LD A,D
                CALL TETEP
                POP DE
                POP BC
                RET NZ
                LD A,(HL)
                BIT 6,A
                RET NZ
                AND B
                JR Z,PRETNON
        PRET2:  SRL C
                INC D
                LD A,D
                CP 3
                JR NZ,PRET1
                XOR A
                RET
        PRETNON: OR 1
                RET
        ; HL = premier son de la file du canal IX (DE et A changés)
        TETE:   PUSH IX
                POP HL
                LD A,(IX+1)
                LD E,A
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,E
                ADD A,26
                LD E,A
                LD D,0
                ADD HL,DE
                RET
        ; A = canal -> HL = premier son de sa file (Z) ; Z faux s'il n'a rien en attente ou un son en cours
        TETEP:  LD HL,&B800
                LD DE,62
                OR A
                JR Z,TETP2
        TETP1:  ADD HL,DE
                DEC A
                JR NZ,TETP1
        TETP2:  LD A,(HL)
                OR A
                JR Z,TETPNON
                INC HL
                LD E,(HL)
                INC HL
                LD A,(HL)
                OR A
                JR NZ,TETPNON
                DEC HL
                DEC HL
                LD A,E
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,E
                ADD A,26
                LD E,A
                LD D,0
                ADD HL,DE
                XOR A
                RET
        TETPNON: OR 1
                RET
        ; SOUND RELEASE (&BCB3) : A = canaux (1, 2, 4...) : leur premier son retenu peut commencer
        SREL:   PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                PUSH IX
                DI
                LD C,A
                XOR A
        SREL1:  RR C
                JR NC,SREL2
                PUSH AF
                PUSH BC
                CALL CANAL
                LD A,(IX+0)
                OR A
                JR Z,SREL3
                CALL TETE
                RES 6,(HL)
        SREL3:  POP BC
                POP AF
        SREL2:  INC A
                CP 3
                JR NZ,SREL1
                EI
                POP IX
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; ---------- temps et événements (D194) ----------
        ; &BE36 à &BE39 : temps en 1/300 de seconde (TIME) ; &BED8 à &BEDF : les 4 minuteries (adresses de leurs blocs,
        ; 0 : libre) ; &BED2 à &BED7 : événement attendu par chaque canal du son (ON SQ, 0 : aucun). Loin de la pile
        ; (&BFF0 et en dessous), qui pourrait les écraser.
        ; Bloc de minuterie (KL ADD TICKER) : +0 et +1 libres, +2 et +3 compte, +4 et +5 recharge, puis son bloc
        ; d'événement (KL INIT EVENT) : +0 et +1 libres, +2 compte, +3 classe, +4 et +5 routine, +6 ROM.
        ; KL TIME PLEASE (&BD0D) : DEHL = temps
        TEMPS:  DI
                LD HL,(&BE36)
                LD DE,(&BE38)
                EI
                RET
        ; KL TIME SET (&BD10) : DEHL = temps
        TEMPSM: DI
                LD (&BE36),HL
                LD (&BE38),DE
                EI
                RET
        ; KL INIT EVENT (&BCEF) : HL = bloc, B = classe, C = ROM, DE = routine -> HL = bloc + 7
        INITEV: PUSH AF
                INC HL
                INC HL
                XOR A
                LD (HL),A
                INC HL
                LD (HL),B
                INC HL
                LD (HL),E
                INC HL
                LD (HL),D
                INC HL
                LD (HL),C
                INC HL
                POP AF
                RET
        ; KL ADD TICKER (&BCE9) : HL = bloc de minuterie, DE = compte, BC = recharge (en 1/50 de seconde)
        AJTICK: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                DI
                PUSH HL
                INC HL
                INC HL
                LD (HL),E
                INC HL
                LD (HL),D
                INC HL
                LD (HL),C
                INC HL
                LD (HL),B
                POP DE
                CALL CHERCHE
                JR Z,AJTFIN             ; déjà dans la liste
                PUSH DE
                LD DE,0
                CALL CHERCHE            ; une place libre
                POP DE
                JR NZ,AJTFIN            ; plus de place
                LD (HL),E
                INC HL
                LD (HL),D
        AJTFIN: EI
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; KL DEL TICKER (&BCEC) : HL = bloc de minuterie, retiré de la liste
        DELTICK: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                DI
                EX DE,HL
                CALL CHERCHE
                JR NZ,DELTF
                XOR A
                LD (HL),A
                INC HL
                LD (HL),A
        DELTF:  EI
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; DE = bloc -> HL = sa place dans la liste des minuteries (Z) ; sinon Z faux
        CHERCHE: LD HL,&BED8
                LD B,4
        CHER1:  LD A,(HL)
                INC HL
                CP E
                JR NZ,CHER2
                LD A,(HL)
                CP D
                JR NZ,CHER2
                DEC HL
                RET
        CHER2:  INC HL
                DJNZ CHER1
                OR 1
                RET
        ; 1/50 de seconde (interruption) : chaque minuterie compte ; à 0, elle est rechargée et son événement arrive
        TICKS:  LD HL,&BED8
                LD B,4
        TICK1:  PUSH BC
                PUSH HL
                LD E,(HL)
                INC HL
                LD D,(HL)
                LD A,D
                OR E
                JR Z,TICK2
                EX DE,HL
                INC HL
                INC HL                  ; compte
                LD E,(HL)
                INC HL
                LD D,(HL)
                DEC DE
                LD A,D
                OR E
                JR NZ,TICK3
                INC HL                  ; à 0 : la recharge
                LD E,(HL)
                INC HL
                LD D,(HL)
                DEC HL
                DEC HL
                LD (HL),D
                DEC HL
                LD (HL),E
                INC HL
                INC HL
                INC HL
                INC HL                  ; le bloc d'événement
                CALL KICK
                JR TICK2
        TICK3:  LD (HL),D
                DEC HL
                LD (HL),E
        TICK2:  POP HL
                INC HL
                INC HL
                POP BC
                DJNZ TICK1
                RET
        ; HL = bloc d'événement : sa routine est appelée tout de suite (événements express) ; tous les registres gardés
        KICK:   PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                PUSH IX
                PUSH IY
                INC HL
                INC HL
                INC HL
                INC HL
                LD E,(HL)
                INC HL
                LD D,(HL)
                LD HL,KICKR
                PUSH HL
                EX DE,HL
                JP (HL)
        KICKR:  POP IY
                POP IX
                POP HL
                POP DE
                POP BC
                POP AF
                RET
        ; SOUND ARM EVENT (&BCB0) : A = canal (1, 2 ou 4), HL = bloc d'événement : il arrivera une fois, dès qu'une
        ; place sera libre dans la file du canal
        ARMEV:  PUSH AF
                PUSH DE
                PUSH HL
                LD E,0
                BIT 0,A
                JR NZ,ARM1
                INC E
                BIT 1,A
                JR NZ,ARM1
                INC E
        ARM1:   SLA E
                LD D,0
                PUSH HL
                LD HL,&BED2
                ADD HL,DE
                POP DE
                DI
                LD (HL),E
                INC HL
                LD (HL),D
                EI
                POP HL
                POP DE
                POP AF
                RET
        ; le canal IX (&B8BC) a une place libre : son événement attendu arrive
        SQEV:   LD A,(IX+0)
                CP 4
                RET NC
                LD A,(&B8BC)
                ADD A,A
                LD E,A
                LD D,0
                LD HL,&BED2
                ADD HL,DE
                LD E,(HL)
                INC HL
                LD D,(HL)
                LD A,D
                OR E
                RET Z
                XOR A
                LD (HL),A
                DEC HL
                LD (HL),A
                EX DE,HL
                JP KICK
        ; ---------- nombres à virgule (D196) : nos propres calculs, à la place de ceux de la ROM du CPC ----------
        ; Même format que le Locomotive BASIC : 5 octets, mantisse sur 32 bits (octet de poids faible d'abord, bit 7
        ; de l'octet 3 = signe, 1 de tête sous-entendu), puis exposant + 128 (0 : le nombre zéro).
        ; Deux accumulateurs de 7 octets : FA en &B700, FB en &B708 : +0 garde (8 bits de plus, pour arrondir),
        ; +1 à +4 mantisse (le 1 de tête écrit, bit 7 de +4), +5 exposant (0 : zéro), +6 signe (0 ou &80).
        ; &B720 : mode des angles ;
        ; &B721 : adresse du résultat ; &B726 : exposant sur 16 bits.
        ; Routines appelées par le programme aux adresses du 664 / 6128 : HL = adresse du résultat en sortie,
        ; retenue à 1 si tout va bien (0 : nombre trop grand, ou division par zéro).
        ; (HL) -> accumulateur DE (FA ou FB) ; HL avance de 4
        FUNP:   XOR A
                LD (DE),A
                INC DE
                LD A,(HL)
                LD (DE),A
                INC HL
                INC DE
                LD A,(HL)
                LD (DE),A
                INC HL
                INC DE
                LD A,(HL)
                LD (DE),A
                INC HL
                INC DE
                LD A,(HL)
                LD C,A
                OR &80
                LD (DE),A
                INC HL
                INC DE
                LD A,(HL)
                LD (DE),A
                INC DE
                LD A,C
                AND &80
                LD (DE),A
                RET
        ; FA -> (HL), arrondi ; retenue à 0 si l'arrondi rend le nombre trop grand
        FPACK:  LD A,(&B705)
                OR A
                JR Z,FPZERO
                LD A,(&B700)
                ADD A,A                 ; retenue = bit 7 de la garde : on arrondit vers le haut
                JR NC,FPK1
                LD DE,&B701
                LD B,4
        FPR1:   LD A,(DE)
                ADC A,0
                LD (DE),A
                INC DE
                DJNZ FPR1
                JR NC,FPK1
                LD A,&80                ; la mantisse a débordé : 1 de tête, exposant + 1
                LD (&B704),A
                LD A,(&B705)
                INC A
                JR Z,FPOVF
                LD (&B705),A
        FPK1:   LD DE,&B701
                LD A,(DE)
                LD (HL),A
                INC HL
                INC DE
                LD A,(DE)
                LD (HL),A
                INC HL
                INC DE
                LD A,(DE)
                LD (HL),A
                INC HL
                INC DE
                LD A,(DE)
                AND &7F
                LD C,A
                LD A,(&B706)
                OR C
                LD (HL),A
                INC HL
                LD A,(&B705)
                LD (HL),A
                SCF
                RET
        FPZERO: XOR A
                LD (HL),A
                INC HL
                LD (HL),A
                INC HL
                LD (HL),A
                INC HL
                LD (HL),A
                INC HL
                LD (HL),A
                SCF
                RET
        FPOVF:  OR A
                RET
        ; FA normalisé : le bit 7 de +4 à 1 (sinon décalé à gauche, exposant - 1) ; mantisse nulle : FA = 0
        FNORM:  LD HL,&B700
                LD A,(HL)
                INC HL
                OR (HL)
                INC HL
                OR (HL)
                INC HL
                OR (HL)
                INC HL
                OR (HL)
                JR Z,FNZ
        FNL:    LD A,(&B704)
                OR A
                JR NZ,FNB
                LD A,(&B705)            ; octet du haut à 0 : un octet d'un coup
                SUB 8
                JR C,FNZ
                JR Z,FNZ
                LD (&B705),A
                LD HL,&B703
                LD DE,&B704
                LD BC,4
                LDDR
                XOR A
                LD (&B700),A
                JR FNL
        FNB:    BIT 7,A
                RET NZ
                CALL FASL
                LD HL,&B705
                DEC (HL)
                JR NZ,FNL
                RET                     ; trop petit : zéro
        FNZ:    XOR A
                LD (&B705),A
                RET
        ; mantisse et garde de FA décalées d'un cran à gauche
        FASL:   LD HL,&B700
                SLA (HL)
                INC HL
                RL (HL)
                INC HL
                RL (HL)
                INC HL
                RL (HL)
                INC HL
                RL (HL)
                RET
        ; 5 octets décalés d'un cran à droite ; HL = le plus fort (+4)
        FSR:    SRL (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                RET
        ; FA et FB échangés
        FSWAP:  LD HL,&B700
                LD DE,&B708
                LD B,7
        FSW1:   LD A,(DE)
                LD C,(HL)
                LD (HL),A
                LD A,C
                LD (DE),A
                INC HL
                INC DE
                DJNZ FSW1
                RET
        ; FA = FB
        FBTOA:  LD HL,&B708
                LD DE,&B700
                LD BC,7
                LDIR
                RET
        ; FB = 10
        F10B:   LD HL,FDIX
                LD DE,&B708
                LD BC,7
                LDIR
                RET
        FDIX:   DEFB 0,0,0,0,&A0,132,0
        ; FA = FA + FB
        FADD:   LD A,(&B70D)
                OR A
                JP Z,FADOK
                LD A,(&B705)
                OR A
                JR NZ,FAD1
                CALL FBTOA
                JR FADOK
        FAD1:   LD B,A
                LD A,(&B70D)
                CP B
                JR C,FAD2
                JR Z,FAD2
                CALL FSWAP              ; FA a le plus grand exposant
        FAD2:   LD A,(&B70D)
                LD C,A
                LD A,(&B705)
                SUB C
                CP 41
                JR NC,FADOK             ; FB trop petit pour compter
                LD B,A
        FAD3B:  LD A,B                  ; décalage de FB : par octets, puis par bits
                CP 8
                JR C,FAD3C
                LD HL,&B709
                LD DE,&B708
                PUSH BC
                LD BC,4
                LDIR
                POP BC
                XOR A
                LD (&B70C),A
                LD A,B
                SUB 8
                LD B,A
                JR FAD3B
        FAD3C:  OR A
                JR Z,FAD4
        FAD3:   PUSH BC
                LD HL,&B70C
                CALL FSR
                POP BC
                DJNZ FAD3
        FAD4:   LD A,(&B706)
                LD C,A
                LD A,(&B70E)
                XOR C
                JR NZ,FADSUB
                LD HL,&B700             ; même signe : les mantisses s'ajoutent
                LD DE,&B708
                LD B,5
                OR A
        FAD5:   LD A,(DE)
                ADC A,(HL)
                LD (HL),A
                INC HL
                INC DE
                DJNZ FAD5
                JR NC,FADOK
                LD HL,&B704             ; débordement : un cran à droite, la retenue entre en haut
                RR (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                DEC HL
                RR (HL)
                LD HL,&B705
                INC (HL)
                JR Z,FADOVF
        FADOK:  SCF
                RET
        FADOVF: OR A
                RET
        FADSUB: LD HL,&B700             ; signes différents : FA - FB
                LD DE,&B708
                LD B,5
                OR A
        FAD6:   LD A,(DE)
                LD C,A
                LD A,(HL)
                SBC A,C
                LD (HL),A
                INC HL
                INC DE
                DJNZ FAD6
                JR NC,FAD7
                LD HL,&B700             ; FB était plus grand : on change le signe du résultat
                LD B,5
                OR A
        FAD8:   LD A,0
                SBC A,(HL)
                LD (HL),A
                INC HL
                DJNZ FAD8
                LD A,(&B70E)
                LD (&B706),A
        FAD7:   CALL FNORM
                SCF
                RET
        ; FA = FA x FB
        FMUL:   LD A,(&B705)
                OR A
                JP Z,FMZ
                LD A,(&B70D)
                OR A
                JP Z,FMZERO
                LD A,(&B706)
                LD C,A
                LD A,(&B70E)
                XOR C
                LD (&B706),A
                LD A,(&B705)
                LD L,A
                LD H,0
                LD A,(&B70D)
                LD E,A
                LD D,0
                ADD HL,DE
                LD DE,&FF80
                ADD HL,DE               ; exposant : ea + eb - 128
                LD (&B726),HL
                PUSH IX                 ; produit : les 40 bits du haut, dans C D E H L (C en haut)
                LD IX,&B700             ; multiplicande : garde et mantisse de FA
                LD HL,0
                LD DE,0
                LD C,0
                LD A,(&B709)            ; multiplicateur : mantisse de FB, octet par octet, en commençant en bas
                CALL FMOCT
                LD A,(&B70A)
                CALL FMOCT
                LD A,(&B70B)
                CALL FMOCT
                LD A,(&B70C)
                CALL FMOCT
                POP IX
                LD A,L
                LD (&B700),A
                LD A,H
                LD (&B701),A
                LD A,E
                LD (&B702),A
                LD A,D
                LD (&B703),A
                LD A,C
                LD (&B704),A
        FEXPN:  LD A,(&B704)            ; un cran à gauche si besoin
                BIT 7,A
                JR NZ,FEXP
                CALL FASL
                LD HL,(&B726)
                DEC HL
                LD (&B726),HL
        FEXP:   LD HL,(&B726)           ; exposant de 1 à 255 : bon ; moins : zéro ; plus : trop grand
                LD A,H
                OR A
                JR NZ,FEXP2
                LD A,L
                OR A
                JP Z,FMZERO
                LD (&B705),A
                SCF
                RET
        FEXP2:  BIT 7,H
                JR NZ,FMZERO
                OR A
                RET
        FMZERO: XOR A
                LD (&B705),A
        FMZ:    SCF
                RET
        ; un octet A du multiplicateur : pour chacun de ses bits (le plus bas d'abord), C D E H L + le multiplicande
        ; si le bit est à 1, puis le tout décalé d'un cran à droite (les bits du bas sont perdus)
        FMOCT:  OR A
                JR NZ,FMOC1
                LD L,H                  ; octet à 0 : décalage de 8 bits d'un coup
                LD H,E
                LD E,D
                LD D,C
                LD C,A
                RET
        FMOC1:  LD B,A                  ; B : les bits restants, avec un 1 en plus en haut pour savoir quand s'arrêter
                SCF
                RR B
        FMOC2:  JR NC,FMOC3
                LD A,L
                ADD A,(IX+0)
                LD L,A
                LD A,H
                ADC A,(IX+1)
                LD H,A
                LD A,E
                ADC A,(IX+2)
                LD E,A
                LD A,D
                ADC A,(IX+3)
                LD D,A
                LD A,C
                ADC A,(IX+4)
                LD C,A
        FMOC3:  RR C
                RR D
                RR E
                RR H
                RR L
                SRL B
                JR NZ,FMOC2
                RET
        ; FA = FA / FB ; retenue à 0 si FB = 0
        FDIV:   LD A,(&B70D)
                OR A
                JR NZ,FDV1
                OR A
                RET
        FDV1:   LD A,(&B705)
                OR A
                JP Z,FMZ
                LD A,(&B706)
                LD C,A
                LD A,(&B70E)
                XOR C
                LD (&B706),A
                LD A,(&B705)
                LD L,A
                LD H,0
                LD DE,129
                ADD HL,DE
                LD A,(&B70D)
                LD E,A
                LD D,0
                OR A
                SBC HL,DE               ; exposant : ea - eb + 129
                LD (&B726),HL
                PUSH IX
                EXX                     ; les registres secondaires sont gardés
                PUSH BC
                PUSH DE
                PUSH HL
                LD HL,0                 ; quotient (40 bits) dans C' D' E' H' L', compteur dans B'
                LD DE,0
                LD BC,&2800
                EXX
                LD IX,&B708             ; diviseur : garde et mantisse de FB
                LD HL,(&B700)           ; reste (sur 48 bits, signé) : B C D E H L = garde et mantisse de FA
                LD DE,(&B702)
                LD A,(&B704)
                LD C,A
                LD B,0
        FDL:    BIT 7,B                 ; reste positif : - diviseur ; négatif : + diviseur
                JR NZ,FDLA
                LD A,L
                SUB (IX+0)
                LD L,A
                LD A,H
                SBC A,(IX+1)
                LD H,A
                LD A,E
                SBC A,(IX+2)
                LD E,A
                LD A,D
                SBC A,(IX+3)
                LD D,A
                LD A,C
                SBC A,(IX+4)
                LD C,A
                LD A,B
                SBC A,0
                LD B,A
                JR FDLQ
        FDLA:   LD A,L
                ADD A,(IX+0)
                LD L,A
                LD A,H
                ADC A,(IX+1)
                LD H,A
                LD A,E
                ADC A,(IX+2)
                LD E,A
                LD A,D
                ADC A,(IX+3)
                LD D,A
                LD A,C
                ADC A,(IX+4)
                LD C,A
                LD A,B
                ADC A,0
                LD B,A
        FDLQ:   LD A,B                  ; chiffre du quotient : 1 si le reste est positif
                CPL
                RLA
                EXX
                RL L
                RL H
                RL E
                RL D
                RL C
                DEC B
                EXX
                JR Z,FDLF
                SLA L                   ; reste x 2
                RL H
                RL E
                RL D
                RL C
                RL B
                JR FDL
        FDLF:   EXX                     ; quotient dans FA
                LD A,L
                LD (&B700),A
                LD A,H
                LD (&B701),A
                LD A,E
                LD (&B702),A
                LD A,D
                LD (&B703),A
                LD A,C
                LD (&B704),A
                POP HL
                POP DE
                POP BC
                EXX
                POP IX
                JP FEXPN
        ; FA comparé à FB : A = 1 (plus grand), 0 (égal), &FF (plus petit) ; &B727 : signe de FA
        FCMP:   LD A,(&B705)
                OR A
                LD A,(&B706)
                JR NZ,FCM0
                XOR A                   ; zéro : sans signe
        FCM0:   LD (&B727),A
                LD C,A
                LD A,(&B70D)
                OR A
                LD A,(&B70E)
                JR NZ,FCM5
                XOR A
        FCM5:   CP C
                JR Z,FCM1
                LD A,C                  ; signes différents : le positif est le plus grand
                OR A
                LD A,1
                RET Z
                LD A,&FF
                RET
        FCM1:   LD A,(&B70D)            ; même signe : exposants, puis mantisses
                LD B,A
                LD A,(&B705)
                CP B
                JR NZ,FCM2
                LD HL,&B704
                LD DE,&B70C
                LD B,4
        FCM3:   LD A,(DE)
                LD C,A
                LD A,(HL)
                CP C
                JR NZ,FCM2
                DEC HL
                DEC DE
                DJNZ FCM3
                XOR A
                RET
        FCM2:   LD A,1                  ; retenue : FA plus petit (en valeur absolue)
                JR NC,FCM4
                LD A,&FF
        FCM4:   LD B,A
                LD A,(&B727)
                OR A
                LD A,B
                RET Z
                NEG                     ; deux négatifs : c'est l'inverse
                RET
        ; ---- les routines du firmware ----
        ; (HL) -> FA, (DE) -> FB ; &B721 = HL
        EDEUX:  LD (&B721),HL
                PUSH DE
                LD DE,&B700
                CALL FUNP
                POP HL
                LD DE,&B708
                JP FUNP
        ; FA -> (&B721) si la retenue est à 1 ; HL = &B721
        EFIN:   LD HL,(&B721)
                RET NC
                PUSH HL
                CALL FPACK
                POP HL
                RET
        ; REAL ADDITION (&BD7C) : (HL) = (HL) + (DE)
        EADD:   CALL EDEUX
                CALL FADD
                JR EFIN
        ; REAL REVERSE SUBTRACTION (&BD82) : (HL) = (DE) - (HL)
        ERSUB:  CALL EDEUX
                LD A,(&B706)
                XOR &80
                LD (&B706),A
                CALL FADD
                JR EFIN
        ; REAL MULTIPLICATION (&BD85) : (HL) = (HL) x (DE)
        EMUL:   CALL EDEUX
                CALL FMUL
                JR EFIN
        ; REAL DIVISION (&BD88) : (HL) = (HL) / (DE)
        EDIV:   CALL EDEUX
                CALL FDIV
                JR EFIN
        ; REAL COMPARISON (&BD8E) : A = 1, 0 ou &FF
        ECMP:   CALL EDEUX
                CALL FCMP
                LD HL,(&B721)
                RET
        ; REAL UNARY MINUS (&BD91) : (HL) = -(HL)
        ENEG:   PUSH HL
                INC HL
                INC HL
                INC HL
                INC HL
                LD A,(HL)
                OR A
                JR Z,ENEG1
                DEC HL
                LD A,(HL)
                XOR &80
                LD (HL),A
        ENEG1:  POP HL
                SCF
                RET
        ; REAL SIGNUM (&BD94) : A = 1, 0 ou &FF
        ESGN:   PUSH HL
                INC HL
                INC HL
                INC HL
                INC HL
                LD A,(HL)
                OR A
                JR Z,ESGN1
                DEC HL
                LD A,(HL)
                RLA
                LD A,1
                JR NC,ESGN1
                LD A,&FF
        ESGN1:  POP HL
                RET
        ; INTEGER TO REAL (&BD64) : HL = entier sans signe, A bit 7 = signe, DE = où ranger -> HL = DE
        EI2R:   PUSH DE
                AND &80
                LD (&B706),A
                XOR A
                LD (&B700),A
                LD (&B701),A
                LD (&B702),A
                LD A,L
                LD (&B703),A
                LD A,H
                LD (&B704),A
                LD A,144
                LD (&B705),A
                CALL FNORM
                POP HL
                PUSH HL
                CALL FPACK
                POP HL
                RET
        ; BINARY TO REAL (&BD67) : (HL) = entier sans signe de 4 octets, A bit 7 = signe -> (HL) = nombre
        EB2R:   AND &80
                LD (&B706),A
                PUSH HL
                LD DE,&B701
                LD BC,4
                LDIR
                XOR A
                LD (&B700),A
                LD A,160
                LD (&B705),A
                CALL FNORM
                POP HL
                PUSH HL
                CALL FPACK
                POP HL
                RET
        ; REAL TO INTEGER (&BD6A) : (HL) -> HL = valeur sans signe arrondie ; retenue à 0 au-delà de 65535
        ER2I:   LD DE,&B700
                CALL FUNP
                LD A,(&B705)
                CP 128
                JR C,ER2IZ              ; moins de 0.5 : 0
                CP 145
                JR NC,ER2IO             ; 65536 ou plus
                LD B,A
                LD A,159
                SUB B
                LD B,A                  ; décalage : 159 - exposant (2 x la valeur)
        ER2I0:  LD A,B                  ; par octets, puis par bits
                CP 8
                JR C,ER2I2
                LD HL,&B701
                LD DE,&B700
                PUSH BC
                LD BC,4
                LDIR
                POP BC
                XOR A
                LD (&B704),A
                LD A,B
                SUB 8
                LD B,A
                JR ER2I0
        ER2I2:  OR A
                JR Z,ER2I3
        ER2I1:  PUSH BC
                LD HL,&B704
                CALL FSR
                POP BC
                DJNZ ER2I1
        ER2I3:  LD HL,(&B701)
                LD A,(&B703)
                LD DE,1
                ADD HL,DE               ; + 0.5, puis divisé par 2
                ADC A,0
                SRL A
                RR H
                RR L
                OR A
                RET NZ                  ; plus de 65535 : retenue à 0 (OR A)
                SCF
                RET
        ER2IZ:  LD HL,0
                SCF
                RET
        ER2IO:  OR A
                RET
        ; REAL FIX (&BD70) : (HL) = partie entière (vers zéro) en 4 octets sans signe, B = signe ; HL gardé
        EFIX:   PUSH HL
                LD DE,&B700
                CALL FUNP
                POP HL
                LD A,(&B706)
                LD B,A
                LD A,(&B705)
                CP 129
                JR C,EFIXZ              ; moins de 1
                CP 161
                JR NC,EFIXO             ; 2^32 ou plus
                LD C,A
                LD A,160
                SUB C
                JR Z,EFIX2
                PUSH BC
                LD B,A
        EFIX1:  PUSH BC
                PUSH HL
                LD HL,&B704
                CALL FSR
                POP HL
                POP BC
                DJNZ EFIX1
                POP BC
        EFIX2:  PUSH HL
                PUSH BC
                EX DE,HL
                LD HL,&B701
                LD BC,4
                LDIR
                POP BC
                POP HL
                SCF
                RET
        EFIXZ:  PUSH HL
                XOR A
                LD (HL),A
                INC HL
                LD (HL),A
                INC HL
                LD (HL),A
                INC HL
                LD (HL),A
                POP HL
                SCF
                RET
        EFIXO:  OR A
                RET
        ; REAL *10^A (&BD79) : (HL) = (HL) x 10^A, A de -128 à 127
        EX10:   LD (&B721),HL
                LD B,A
                LD DE,&B700
                CALL FUNP
                LD A,B
                OR A
                SCF
                JR Z,EX10F
                BIT 7,A
                JR NZ,EX10D
                LD B,A
        EX10M:  PUSH BC
                CALL F10B
                CALL FMUL
                POP BC
                JR NC,EX10F
                DJNZ EX10M
                JR EX10F
        EX10D:  NEG
                LD B,A
        EX10V:  PUSH BC
                CALL F10B
                CALL FDIV
                POP BC
                DJNZ EX10V
                SCF
        EX10F:  JP EFIN
        ; SET ANGLE MODE (&BD97) : A = 0 radians, sinon degrés
        EANGLE: LD (&B720),A
                RET
        ; PI (&BD9A) : (HL) = pi
        EPI:    PUSH HL
                EX DE,HL
                LD HL,FPI
                LD BC,5
                LDIR
                POP HL
                SCF
                RET
        FPI:    DEFB &A2,&DA,&0F,&49,&82
        ; ---- les fonctions (D196) : nombres de travail de 5 octets T0 &B730, T1 &B735, T2 &B73A, T3 &B73F,
        ; T4 &B744, T5 &B749, T6 &B74E, T7 &B753 ; &B72C à &B72F : petites variables ----
        ; 5 octets de (HL) vers (DE)
        MOV5:   LD BC,5
                LDIR
                RET
        ; T2 = polynôme de T1 ; HL = table : nombre de coefficients, puis les coefficients (le plus haut degré d'abord) ;
        ; le calcul reste dans FA (40 bits), sans repasser par la mémoire
        POLY:   LD B,(HL)
                INC HL
                PUSH BC
                LD DE,&B700
                CALL FUNP               ; FA = le premier coefficient
                INC HL
                POP BC
                DEC B
                JR Z,POLYF
        POLY1:  PUSH BC
                PUSH HL
                LD HL,&B735
                LD DE,&B708
                CALL FUNP               ; FB = x
                CALL FMUL
                POP HL
                PUSH HL
                LD DE,&B708
                CALL FUNP               ; FB = coefficient suivant
                CALL FADD
                POP HL
                LD BC,5
                ADD HL,BC
                POP BC
                DJNZ POLY1
        POLYF:  LD HL,&B73A
                JP FPACK
        ; (DE) = l'entier signé A (de -128 à 127)
        SI2R:   LD H,0
                LD L,A
                BIT 7,A
                LD A,0
                JR Z,SI2R1
                LD A,L
                NEG
                LD L,A
                LD A,&80
        SI2R1:  JP EI2R
        ; SQR (&BD9D) : (HL) = racine carrée de (HL) (le programme a vérifié qu'il n'est pas négatif)
        ESQR:   PUSH HL
                LD DE,&B730
                CALL MOV5               ; T0 = x = m x 2^E
                LD A,(&B734)
                OR A
                JR Z,ESQRF              ; racine de 0 : 0
                SUB 128
                LD B,A
                SRA A
                BIT 0,B
                JR Z,ESQR0
                INC A                   ; E impair : v = m / 2, k = (E + 1) / 2
        ESQR0:  LD (&B72C),A            ; k
                PUSH BC
                LD HL,&B730
                LD DE,&B735
                CALL MOV5
                POP BC
                LD A,128
                BIT 0,B
                JR Z,ESQR2
                DEC A
        ESQR2:  LD (&B739),A            ; T1 = v, de 0.25 à 1
                LD HL,&B735             ; premier essai : 0.343 + 0.686 v (à 3 % près), x 2^k
                LD DE,KSQB
                CALL EMUL
                LD HL,&B735
                LD DE,KSQA
                CALL EADD
                LD A,(&B72C)
                LD B,A
                LD A,(&B739)
                ADD A,B
                LD (&B739),A
                LD B,4
        ESQR1:  PUSH BC                 ; T1 = (T1 + T0 / T1) / 2, 4 fois
                LD HL,&B730
                LD DE,&B73A
                CALL MOV5
                LD HL,&B73A
                LD DE,&B735
                CALL EDIV
                LD HL,&B735
                LD DE,&B73A
                CALL EADD
                LD HL,&B739
                DEC (HL)
                POP BC
                DJNZ ESQR1
                POP DE
                PUSH DE
                LD HL,&B735
                CALL MOV5
        ESQRF:  POP HL
                SCF
                RET
        ; T3 = ln (HL) (plus grand que 0)
        LNX:    LD DE,&B730
                CALL MOV5               ; T0 = x = m x 2^E
                LD A,(&B734)
                SUB 128
                LD (&B72C),A            ; E
                LD A,128
                LD (&B734),A            ; T0 = m, de 0.5 à 1
                LD HL,&B730
                LD DE,KRAC2
                CALL ECMP
                CP &FF
                JR NZ,LNX1
                LD HL,&B734             ; m plus petit que racine de 1/2 : m x 2, E - 1
                INC (HL)
                LD HL,&B72C
                DEC (HL)
        LNX1:   LD HL,&B730             ; T4 = z = (m - 1) / (m + 1)
                LD DE,&B744
                CALL MOV5
                LD HL,&B744
                LD DE,KUN
                CALL ERSUB
                LD HL,&B744
                CALL ENEG
                LD HL,&B730
                LD DE,KUN
                CALL EADD
                LD HL,&B744
                LD DE,&B730
                CALL EDIV
                LD HL,&B744             ; T1 = z^2
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,&B744
                CALL EMUL
                LD HL,PLN
                CALL POLY               ; T2 = 1 + z^2/3 + ...
                LD HL,&B73A
                LD DE,&B744
                CALL EMUL               ; T2 = z x (...)
                LD A,(&B73E)
                OR A
                JR Z,LNX2
                INC A                   ; x 2
                LD (&B73E),A
        LNX2:   LD A,(&B72C)            ; T3 = E x ln 2 + T2
                LD DE,&B73F
                CALL SI2R
                LD HL,&B73F
                LD DE,KLN2
                CALL EMUL
                LD HL,&B73F
                LD DE,&B73A
                JP EADD
        ; LOG (&BDA3) : (HL) = ln (HL) ; LOG10 (&BDA6) : (HL) = log (HL) en base 10
        ELOG:   PUSH HL
                CALL LNX
                JR ELOGF
        ELOG10: PUSH HL
                CALL LNX
                LD HL,&B73F
                LD DE,KILN10
                CALL EMUL
        ELOGF:  POP DE
                PUSH DE
                LD HL,&B73F
                CALL MOV5
                POP HL
                SCF
                RET
        ; T2 = e^(HL) ; retenue à 0 si trop grand
        EXPX:   LD DE,&B730
                CALL MOV5               ; T0 = x
                LD HL,&B730
                LD DE,K89
                CALL ECMP
                CP 1
                JP Z,EXPOVF
                LD HL,&B730
                LD DE,KM89
                CALL ECMP
                CP &FF
                JP Z,EXPZ
                LD HL,&B730             ; n = x / ln 2, arrondi
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,KLN2
                CALL EDIV
                LD A,(&B738)
                AND &80
                LD (&B72D),A            ; signe de n
                LD HL,&B735
                CALL ER2I               ; HL = |n|
                LD A,L
                LD (&B72C),A
                LD A,(&B72D)
                LD DE,&B73F
                CALL EI2R               ; T3 = n
                LD HL,&B73F             ; r = (x - n x C1) - n x C2
                LD DE,&B744
                CALL MOV5
                LD HL,&B73F
                LD DE,KC1
                CALL EMUL
                LD HL,&B73F
                LD DE,&B730
                CALL ERSUB
                LD HL,&B744
                LD DE,KC2
                CALL EMUL
                LD HL,&B744
                LD DE,&B73F
                CALL ERSUB
                LD HL,&B744
                LD DE,&B735
                CALL MOV5               ; T1 = r
                LD HL,PEXP
                CALL POLY               ; T2 = e^r
                LD A,(&B72C)            ; exposant + n
                LD B,A
                LD A,(&B72D)
                OR A
                LD A,(&B73E)
                JR NZ,EXPNEG
                ADD A,B
                JR C,EXPOVF
                JR EXPST
        EXPNEG: SUB B
                JR C,EXPZ
                JP Z,EXPZ
        EXPST:  LD (&B73E),A
                SCF
                RET
        EXPZ:   XOR A
                LD (&B73E),A
                SCF
                RET
        EXPOVF: OR A
                RET
        ; EXP (&BDA9) : (HL) = e^(HL)
        EEXP:   PUSH HL
                CALL EXPX
                POP HL
                RET NC
        ESTT2:  PUSH HL                 ; T2 -> (HL)
                EX DE,HL
                LD HL,&B73A
                CALL MOV5
                POP HL
                SCF
                RET
        ; REAL POWER (&BDA0) : (HL) = (HL) ^ (DE)
        EPOW:   PUSH HL
                PUSH DE
                LD DE,&B749
                CALL MOV5               ; T5 = x
                POP HL
                LD DE,&B74E
                CALL MOV5               ; T6 = y
                LD A,(&B752)
                OR A
                JR NZ,EPOW1
                LD HL,KUN               ; y = 0 : 1
                LD DE,&B73A
                CALL MOV5
                JP EPOWF
        EPOW1:  LD A,(&B74D)
                OR A
                JR NZ,EPOW2
                LD A,(&B751)            ; x = 0 : 0 si y > 0, sinon trop grand
                RLA
                JP C,EPOWE
                POP HL
                JP EFIXZ1
        EPOW2:  XOR A
                LD (&B72F),A            ; signe du résultat
                LD HL,&B74E             ; y entier ?
                LD DE,&B753
                CALL MOV5
                LD HL,&B753
                CALL EFIX
                JP NC,EPOWR             ; très grand : par ln et exp
                LD A,(&B753)
                LD (&B72E),A            ; octet bas de |y|
                LD A,(&B754)
                LD C,A
                LD A,(&B755)
                OR C
                LD C,A
                LD A,(&B756)
                OR C
                LD (&B72C),A            ; 0 : |y| < 256
                LD A,B
                LD HL,&B753
                CALL EB2R
                LD HL,&B753
                LD DE,&B74E
                CALL ECMP
                OR A
                JR NZ,EPOWR             ; pas entier
                LD A,(&B74C)            ; x négatif et y impair : résultat négatif
                AND &80
                JR Z,EPOW3
                LD A,(&B72E)
                RRA
                JR NC,EPOW3
                LD A,&80
                LD (&B72F),A
        EPOW3:  LD A,(&B74C)            ; |x|
                AND &7F
                LD (&B74C),A
                LD A,(&B72C)
                OR A
                JR NZ,EPOWL             ; |y| de 256 ou plus : par ln et exp
                LD HL,KUN               ; T4 = 1 ; puissance par carrés
                LD DE,&B744
                CALL MOV5
                LD A,(&B72E)
        EPOW4:  LD (&B72E),A
                RRA
                JR NC,EPOW5
                LD HL,&B744
                LD DE,&B749
                CALL EMUL
                JR NC,EPOWE
        EPOW5:  LD A,(&B72E)
                SRL A
                JR Z,EPOW6
                PUSH AF
                LD HL,&B749
                LD DE,&B749
                CALL EMUL
                POP BC
                JR NC,EPOWE
                LD A,B
                JR EPOW4
        EPOW6:  LD A,(&B751)            ; y négatif : 1 / T4
                RLA
                JR NC,EPOW7
                LD HL,KUN
                LD DE,&B73A
                CALL MOV5
                LD HL,&B73A
                LD DE,&B744
                CALL EDIV
                JR NC,EPOWE
                JR EPOW8
        EPOW7:  LD HL,&B744
                LD DE,&B73A
                CALL MOV5
                JR EPOW8
        EPOWR:  LD A,(&B74C)            ; y pas entier : x doit être positif
                RLA
                JP C,EPOWE
        EPOWL:  LD HL,&B749             ; e^(y x ln x)
                CALL LNX
                LD HL,&B73F
                LD DE,&B74E
                CALL EMUL
                JR NC,EPOWE
                LD HL,&B73F
                CALL EXPX
                JR NC,EPOWE
        EPOW8:  LD A,(&B73E)            ; le signe
                OR A
                JR Z,EPOWF
                LD A,(&B72F)
                LD B,A
                LD A,(&B73D)
                OR B
                LD (&B73D),A
        EPOWF:  POP HL
                JP ESTT2
        EPOWE:  POP HL
                OR A
                RET
        ; (HL) = 0 (pour POWER)
        EFIXZ1: PUSH HL
                XOR A
                LD B,5
        EFZ1:   LD (HL),A
                INC HL
                DJNZ EFZ1
                POP HL
                SCF
                RET
        ; T2 = sin (HL) (A = 0) ou cos (HL) (A = 1) ; retenue à 0 si le nombre est trop grand
        SCX:    LD (&B72E),A
                LD DE,&B730
                CALL MOV5               ; T0 = x
                LD HL,&B730             ; k = x / (pi / 2) (en degrés : x / 90), arrondi
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,KPI2
                LD A,(&B720)
                OR A
                JR Z,SCX1
                LD DE,K90
        SCX1:   CALL EDIV
                LD A,(&B738)
                AND &80
                LD (&B72D),A
                LD HL,&B735
                CALL ER2I
                RET NC
                BIT 7,H
                JP NZ,SCXOVF            ; plus de 32767 quarts de tour : trop grand
                LD A,L
                LD (&B72C),A            ; octet bas de |k|
                LD A,(&B72D)
                LD DE,&B73F
                CALL EI2R               ; T3 = k
                LD A,(&B720)
                OR A
                JR Z,SCX4
                LD HL,&B73F             ; en degrés : r = (x - 90 k) x pi / 180
                LD DE,K90
                CALL EMUL
                LD HL,&B73F
                LD DE,&B730
                CALL ERSUB
                LD HL,&B73F
                LD DE,&B744
                CALL MOV5
                LD HL,&B744
                LD DE,KDEG
                CALL EMUL
                JR SCX5
        SCX4:   LD HL,&B73F             ; r = (x - k x P1) - k x P2
                LD DE,&B744
                CALL MOV5
                LD HL,&B73F
                LD DE,KP1
                CALL EMUL
                LD HL,&B73F
                LD DE,&B730
                CALL ERSUB
                LD HL,&B744
                LD DE,KP2
                CALL EMUL
                LD HL,&B744
                LD DE,&B73F
                CALL ERSUB              ; T4 = r
        SCX5:   LD A,(&B72C)            ; quart de tour : k modulo 4 (k négatif : 4 - |k| modulo 4)
                AND 3
                LD B,A
                LD A,(&B72D)
                OR A
                LD A,B
                JR Z,SCX2
                NEG
                AND 3
        SCX2:   LD B,A
                LD A,(&B72E)
                ADD A,B
                AND 3
                LD (&B72C),A
                LD HL,&B744             ; T1 = r^2
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,&B744
                CALL EMUL
                LD A,(&B72C)
                RRA
                JR C,SCXC
                LD HL,PSIN              ; sin r = r x (...)
                CALL POLY
                LD HL,&B73A
                LD DE,&B744
                CALL EMUL
                JR SCX3
        SCXC:   LD HL,PCOS              ; cos r
                CALL POLY
        SCX3:   LD A,(&B72C)            ; quarts de tour 2 et 3 : signe changé
                AND 2
                SCF
                RET Z
                LD HL,&B73A
                JP ENEG
        SCXOVF: OR A
                RET
        ; SIN (&BDAC), COS (&BDAF) : (HL) = sin (HL), cos (HL)
        ESIN:   XOR A
                JR ESC
        ECOS:   LD A,1
        ESC:    PUSH HL
                CALL SCX
                POP HL
                RET NC
                JP ESTT2
        ; TAN (&BDB2) : (HL) = tan (HL)
        ETAN:   PUSH HL
                XOR A
                CALL SCX
                JR NC,ETANE
                LD HL,&B73A
                LD DE,&B74E
                CALL MOV5               ; T6 = sin
                POP HL
                PUSH HL
                LD A,1
                CALL SCX                ; T2 = cos
                JR NC,ETANE
                LD HL,&B74E
                LD DE,&B73A
                CALL EDIV
                JR NC,ETANE
                POP DE
                PUSH DE
                LD HL,&B74E
                CALL MOV5
                POP HL
                SCF
                RET
        ETANE:  POP HL
                OR A
                RET
        ; ATN (&BDB5) : (HL) = arc tangente de (HL)
        EATN:   PUSH HL
                LD DE,&B730
                CALL MOV5               ; T0 = x
                LD A,(&B733)
                AND &80
                LD (&B72D),A            ; signe
                LD A,(&B733)
                AND &7F
                LD (&B733),A            ; |x|
                XOR A
                LD (&B72E),A            ; 1 : 1 / x
                LD (&B72F),A            ; 1 : pi / 6 en plus
                LD HL,&B730
                LD DE,KUN
                CALL ECMP
                CP 1
                JR NZ,EATN1
                LD HL,KUN               ; plus grand que 1 : atn x = pi / 2 - atn (1 / x)
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,&B730
                CALL EDIV
                LD HL,&B735
                LD DE,&B730
                CALL MOV5
                LD A,1
                LD (&B72E),A
        EATN1:  LD HL,&B730
                LD DE,KT15
                CALL ECMP
                CP 1
                JR NZ,EATN2
                LD HL,&B730             ; plus grand que tan (pi / 12) : t' = (t x racine 3 - 1) / (t + racine 3)
                LD DE,&B73F
                CALL MOV5
                LD HL,&B73F
                LD DE,KR3
                CALL EMUL
                LD HL,&B73F
                LD DE,KUN
                CALL ERSUB
                LD HL,&B73F
                CALL ENEG
                LD HL,&B730
                LD DE,KR3
                CALL EADD
                LD HL,&B73F
                LD DE,&B730
                CALL EDIV
                LD HL,&B73F
                LD DE,&B730
                CALL MOV5
                LD A,1
                LD (&B72F),A
        EATN2:  LD HL,&B730             ; T1 = t^2
                LD DE,&B735
                CALL MOV5
                LD HL,&B735
                LD DE,&B730
                CALL EMUL
                LD HL,PATN
                CALL POLY
                LD HL,&B73A
                LD DE,&B730
                CALL EMUL               ; T2 = atn t
                LD A,(&B72F)
                OR A
                JR Z,EATN3
                LD HL,&B73A
                LD DE,KPI6
                CALL EADD
        EATN3:  LD A,(&B72E)
                OR A
                JR Z,EATN4
                LD HL,&B73A
                LD DE,KPI2
                CALL ERSUB
        EATN4:  LD A,(&B73E)            ; le signe
                OR A
                JR Z,EATN5
                LD A,(&B72D)
                LD B,A
                LD A,(&B73D)
                OR B
                LD (&B73D),A
        EATN5:  LD A,(&B720)
                OR A
                JR Z,EATN6
                LD HL,&B73A             ; en degrés
                LD DE,KRAD
                CALL EMUL
        EATN6:  POP HL
                JP ESTT2
        ; constantes des fonctions (D196)
        KUN:    DEFB &00,&00,&00,&00,&81   ; 1
        KSQA:   DEFB &2D,&B2,&9D,&2F,&7F   ; racine : premier essai
        KSQB:   DEFB &2D,&B2,&9D,&2F,&80   ; racine : premier essai
        KRAC2:  DEFB &34,&F3,&04,&35,&80   ; racine de 1/2
        KLN2:   DEFB &F8,&17,&72,&31,&80   ; ln 2
        KILN10: DEFB &A9,&D8,&5B,&5E,&7F   ; 1 / ln 10
        K89:    DEFB &F4,&17,&72,&31,&87   ; plus grand x pour EXP
        KM89:   DEFB &25,&FC,&D4,&B2,&87   ; plus petit x pour EXP
        KC1:    DEFB &00,&00,&72,&31,&80   ; ln 2, partie haute (16 bits)
        KC2:    DEFB &7C,&8E,&BE,&3F,&6D   ; ln 2, le reste
        KPI2:   DEFB &A2,&DA,&0F,&49,&81   ; pi / 2
        KP1:    DEFB &00,&00,&10,&49,&81   ; pi / 2, partie haute (16 bits)
        KP2:    DEFB &5D,&7A,&77,&95,&6F   ; pi / 2, le reste
        KDEG:   DEFB &13,&35,&FA,&0E,&7B   ; degrés -> radians
        K90:    DEFB &00,&00,&00,&34,&87   ; 90
        KRAD:   DEFB &D3,&E0,&2E,&65,&86   ; radians -> degrés
        KPI6:   DEFB &C1,&91,&0A,&06,&80   ; pi / 6
        KR3:    DEFB &43,&D7,&B3,&5D,&81   ; racine de 3
        KT15:   DEFB &F5,&A2,&30,&09,&7F   ; tan(pi / 12)
        ; ln : 1 + w/3 + w^2/5 + ... + w^5/11 (w = z^2)
        PLN: DEFB 6
                DEFB &A3,&8B,&2E,&3A,&7D
                DEFB &E4,&38,&8E,&63,&7D
                DEFB &92,&24,&49,&12,&7E
                DEFB &CD,&CC,&CC,&4C,&7E
                DEFB &AB,&AA,&AA,&2A,&7F
                DEFB &00,&00,&00,&00,&81
        ; exp : 1 + r + r^2/2! + ... + r^10/10!
        PEXP: DEFB 11
                DEFB &BC,&7D,&F2,&13,&6B
                DEFB &2B,&1D,&EF,&38,&6E
                DEFB &D0,&00,&0D,&50,&71
                DEFB &D0,&00,&0D,&50,&74
                DEFB &B6,&60,&0B,&36,&77
                DEFB &89,&88,&88,&08,&7A
                DEFB &AB,&AA,&AA,&2A,&7C
                DEFB &AB,&AA,&AA,&2A,&7E
                DEFB &00,&00,&00,&00,&80
                DEFB &00,&00,&00,&00,&81
                DEFB &00,&00,&00,&00,&81
        ; sin r / r : 1 - w/3! + ... + w^6/13!
        PSIN: DEFB 7
                DEFB &9D,&30,&92,&30,&60
                DEFB &40,&2B,&32,&D7,&67
                DEFB &2B,&1D,&EF,&38,&6E
                DEFB &D0,&00,&0D,&D0,&74
                DEFB &89,&88,&88,&08,&7A
                DEFB &AB,&AA,&AA,&AA,&7E
                DEFB &00,&00,&00,&00,&81
        ; cos r : 1 - w/2! + ... - w^7/14!
        PCOS: DEFB 8
                DEFB &46,&A5,&CB,&C9,&5C
                DEFB &80,&C7,&76,&0F,&64
                DEFB &BC,&7D,&F2,&93,&6B
                DEFB &D0,&00,&0D,&50,&71
                DEFB &B6,&60,&0B,&B6,&77
                DEFB &AB,&AA,&AA,&2A,&7C
                DEFB &00,&00,&00,&80,&80
                DEFB &00,&00,&00,&00,&81
        ; atn t / t : 1 - w/3 + w^2/5 - ... - w^7/15
        PATN: DEFB 8
                DEFB &89,&88,&88,&88,&7D
                DEFB &9E,&D8,&89,&1D,&7D
                DEFB &A3,&8B,&2E,&BA,&7D
                DEFB &E4,&38,&8E,&63,&7D
                DEFB &92,&24,&49,&92,&7E
                DEFB &CD,&CC,&CC,&4C,&7E
                DEFB &AB,&AA,&AA,&AA,&7F
                DEFB &00,&00,&00,&00,&81
        ; KL PROBE ROM (&B915) : H = 4, comme un CPC Plus (le programme choisit alors les adresses du 664 / 6128 pour
        ; les calculs à virgule, D196)
        PROBE:  LD HL,&0400
                XOR A
                RET
        ; ---------- dessin (D187) : routines GRA et SCR du CPC ----------
        ; &BE40 origine X, &BE42 origine Y (repère de l'écran, 0 à 639 et 0 à 399), &BE44 / &BE46 curseur graphique
        ; (repère de l'utilisateur), &BE48 stylo graphique, &BE49 papier graphique ; &BE4A à &BE5E : calculs des traits.
        ; Points « de base » : x en points du mode (0 à 159, 319 ou 639), y de 0 (en bas) à 199 (en haut).
        ; &BE60 décalage x -> octet, &BE61 points par octet - 1, &BE62 masque du point de gauche, &BE63 largeur en
        ; points, &BE65 décalage repère -> point, &BE66 nombre d'encres (copiés par SCR SET MODE depuis GMODES).
        GMODES: DEFB 1,1,&AA,160,0,2,16
                DEFB 2,3,&88,&40,1,1,4
                DEFB 3,7,&80,&80,2,0,2
        ; DE = X, HL = Y de l'utilisateur -> DE = x, HL = y de base (avec l'origine)
        TOBASE: PUSH HL
                LD HL,(&BE40)
                ADD HL,DE
                LD A,(&BE65)
                OR A
                JR Z,TOB2
        TOB1:   SRA H
                RR L
                DEC A
                JR NZ,TOB1
        TOB2:   EX DE,HL
                POP HL
                PUSH DE
                LD DE,(&BE42)
                ADD HL,DE
                SRA H
                RR L
                POP DE
                RET
        ; retenue si le point de base DE, HL est hors de la fenêtre graphique (&BE68 gauche, &BE6A droite,
        ; &BE6C bas, &BE6E haut, en points de base ; D188)
        DEDANS: BIT 7,D
                SCF
                RET NZ
                BIT 7,H
                SCF
                RET NZ
                PUSH HL
                LD HL,(&BE68)
                OR A
                SBC HL,DE
                JR Z,DD1
                JR NC,DDOUT             ; à gauche de la fenêtre
        DD1:    LD HL,(&BE6A)
                OR A
                SBC HL,DE
                JR C,DDOUT              ; à droite
                POP HL
                PUSH DE
                EX DE,HL
                LD HL,(&BE6C)
                OR A
                SBC HL,DE
                JR Z,DD2
                JR NC,DDOUT2            ; en dessous
        DD2:    LD HL,(&BE6E)
                OR A
                SBC HL,DE
                JR C,DDOUT2             ; au-dessus
                EX DE,HL
                POP DE
                OR A
                RET
        DDOUT2: EX DE,HL
                POP DE
                SCF
                RET
        DDOUT:  POP HL
                SCF
                RET
        ; SCR DOT POSITION (&BC1D) : DE = x, HL = y de base -> HL = adresse, C = masque du point, B = points par octet - 1
        DOTPOS: LD A,199
                SUB L
                LD B,A                  ; ligne depuis le haut
                AND 7
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,&C0
                PUSH AF
                LD A,B
                RRCA
                RRCA
                RRCA
                AND 31
                LD L,A
                LD H,0
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD B,H
                LD C,L
                ADD HL,HL
                ADD HL,HL
                ADD HL,BC               ; (ligne / 8) x 80
                POP AF
                ADD A,H
                LD H,A                  ; + &C000 + (ligne et 7) x &800
                PUSH DE
                LD A,(&BE60)
        DOT1:   SRL D
                RR E
                DEC A
                JR NZ,DOT1
                ADD HL,DE               ; + x / points par octet
                POP DE
                LD A,(&BE61)
                AND E                   ; rang du point dans l'octet
                LD B,A
                INC B
                LD A,(&BE62)
                JR DOT3
        DOT2:   SRL A
        DOT3:   DJNZ DOT2
                LD C,A
                LD A,(&BE61)
                LD B,A
                RET
        ; un point de base DE, HL à l'encre codée en &BE5C (rien hors de l'écran)
        POINT:  CALL DEDANS
                RET C
                CALL DOTPOS
                LD A,(&BE5C)
                LD B,A
                JP &BDE8                ; par l'entrée SCR WRITE, comme le firmware (MASK la détourne, D110)
        ; SCR WRITE (&BDE8 -> SCRWR) : HL = adresse, C = masque du point, B = encre codée ; selon SCR ACCESS (&BE67)
        SCRWR:  LD A,(&BE67)
                OR A
                JR NZ,SW1
                LD A,C                  ; normal
                CPL
                AND (HL)
                LD (HL),A
                LD A,B
                AND C
                OR (HL)
                LD (HL),A
                RET
        SW1:    DEC A
                JR NZ,SW2
                LD A,B                  ; XOR
                AND C
                XOR (HL)
                LD (HL),A
                RET
        SW2:    DEC A
                JR NZ,SW3
                LD A,C                  ; AND
                CPL
                OR B
                AND (HL)
                LD (HL),A
                RET
        SW3:    LD A,B                  ; OR
                AND C
                OR (HL)
                LD (HL),A
                RET
        ; SCR ACCESS (&BC59) : A = mode d'écriture (0 normal, 1 XOR, 2 AND, 3 OR)
        SACC:   AND 3
                LD (&BE67),A
                RET
        ; MC WAIT FLYBACK (&BD19) : FRAME, attend le retour du balayage
        FLYB:   PUSH AF
                PUSH BC
                LD B,&F5
        FLY1:   IN A,(C)
                RRA
                JR NC,FLY1
                POP BC
                POP AF
                RET
        ; GRA MOVE ABSOLUTE (&BBC0), GRA MOVE RELATIVE (&BBC3), GRA ASK CURSOR (&BBC6) : DE = X, HL = Y
        GMOVR:  CALL GREL
        GMOVE:  LD (&BE44),DE
                LD (&BE46),HL
                RET
        GREL:   PUSH HL
                LD HL,(&BE44)
                ADD HL,DE
                EX DE,HL
                POP HL
                PUSH DE
                LD DE,(&BE46)
                ADD HL,DE
                POP DE
                RET
        GASK:   LD DE,(&BE44)
                LD HL,(&BE46)
                RET
        ; GRA SET ORIGIN (&BBC9), GRA GET ORIGIN (&BBCC) ; fenêtre graphique : tout l'écran (&BBD5, &BBD8)
        GSETOR: LD (&BE40),DE
                LD (&BE42),HL
                LD HL,0
                LD (&BE44),HL
                LD (&BE46),HL
                RET
        GGETOR: LD DE,(&BE40)
                LD HL,(&BE42)
                RET
        ; GRA GET W WIDTH (&BBD5) : DE = gauche, HL = droite ; GRA GET W HEIGHT (&BBD8) : DE = haut, HL = bas
        ; (repère de l'écran)
        GWWID:  LD HL,(&BE68)
                CALL X2STD
                EX DE,HL
                LD HL,(&BE6A)
                JP X2STD
        X2STD:  LD A,(&BE65)
                OR A
                RET Z
        XS1:    ADD HL,HL
                DEC A
                JR NZ,XS1
                RET
        GWHEI:  LD HL,(&BE6E)
                ADD HL,HL
                EX DE,HL
                LD HL,(&BE6C)
                ADD HL,HL
                RET
        ; GRA WIN WIDTH (&BBCF) : DE, HL = bords gauche et droit (repère de l'écran, dans n'importe quel ordre) ;
        ; gardés dans l'écran et arrondis à l'octet (gauche au début de son octet, droite à la fin du sien)
        GWINW:  CALL ORDRE
                PUSH HL
                EX DE,HL
                CALL STD2X
                LD A,(&BE61)
                CPL
                AND L
                LD L,A
                LD (&BE68),HL
                POP HL
                CALL STD2X
                LD A,(&BE61)
                OR L
                LD L,A
                LD (&BE6A),HL
                RET
        ; GRA WIN HEIGHT (&BBD2) : DE, HL = bords haut et bas (repère de l'écran, dans n'importe quel ordre)
        GWINH:  CALL ORDRE
                PUSH HL
                EX DE,HL
                CALL STD2Y
                LD (&BE6C),HL
                POP HL
                CALL STD2Y
                LD (&BE6E),HL
                RET
        ; DE <= HL (nombres signés) : échangés si besoin
        ORDRE:  PUSH HL
                PUSH DE
                LD A,H
                XOR &80
                LD H,A
                LD A,D
                XOR &80
                LD D,A
                OR A
                SBC HL,DE
                POP DE
                POP HL
                RET NC
                EX DE,HL
                RET
        ; HL (repère de l'écran) -> x de base, de 0 à largeur - 1
        STD2X:  LD A,(&BE65)
                OR A
                JR Z,SX2
        SX1:    SRA H
                RR L
                DEC A
                JR NZ,SX1
        SX2:    BIT 7,H
                JR Z,SX3
                LD HL,0
                RET
        SX3:    PUSH DE
                LD DE,(&BE63)
                DEC DE
                PUSH HL
                OR A
                SBC HL,DE
                POP HL
                JR C,SX4
                EX DE,HL
        SX4:    POP DE
                RET
        ; HL (repère de l'écran) -> y de base, de 0 à 199
        STD2Y:  SRA H
                RR L
                BIT 7,H
                JR Z,SY1
                LD HL,0
                RET
        SY1:    LD A,H
                OR A
                JR NZ,SY2
                LD A,L
                CP 200
                RET C
        SY2:    LD HL,199
                RET
        ; GRA CLEAR WINDOW (&BBDB) : CLG, écran à la couleur du papier graphique, curseur à l'origine
        GCLR:   LD A,(&BE49)            ; la fenêtre graphique seulement (bords à l'octet)
                CALL OCTENC
                LD (&BE5C),A
                LD HL,(&BE6C)
                LD (&BE4C),HL
        GC1:    LD DE,(&BE68)
                LD HL,(&BE4C)
                CALL DOTPOS
                PUSH HL
                LD HL,(&BE6A)
                LD DE,(&BE68)
                OR A
                SBC HL,DE
                LD A,(&BE60)
        GC2:    SRL H
                RR L
                DEC A
                JR NZ,GC2
                LD B,L
                INC B                   ; octets de la ligne
                POP HL
                LD A,(&BE5C)
        GC3:    LD (HL),A
                INC HL
                DJNZ GC3
                LD HL,(&BE4C)
                LD DE,(&BE6E)
                OR A
                PUSH HL
                SBC HL,DE
                POP HL
                JR Z,GC4
                INC HL
                LD (&BE4C),HL
                JR GC1
        GC4:    LD HL,0
                LD (&BE44),HL
                LD (&BE46),HL
                RET
        ; GRA SET / GET PEN (&BBDE, &BBE1), GRA SET / GET PAPER (&BBE4, &BBE7)
        GSPEN:  AND 15
                LD (&BE48),A
                RET
        GGPEN:  LD A,(&BE48)
                RET
        GSPAP:  AND 15
                LD (&BE49),A
                RET
        GGPAP:  LD A,(&BE49)
                RET
        ; GRA PLOT ABSOLUTE (&BBEA), GRA PLOT RELATIVE (&BBED)
        GPLOTR: CALL GREL
        GPLOT:  CALL GMOVE
                CALL TOBASE
                PUSH DE
                PUSH HL
                LD A,(&BE48)
                CALL OCTENC
                LD (&BE5C),A
                POP HL
                POP DE
                JP POINT
        ; GRA TEST ABSOLUTE (&BBF0), GRA TEST RELATIVE (&BBF3) : A = encre du point (hors de l'écran : papier)
        GTESTR: CALL GREL
        GTEST:  CALL GMOVE
                CALL TOBASE
                CALL DEDANS
                JR C,GTDEH
                CALL DOTPOS
                LD A,(HL)
                AND C
                LD (&BE5D),A
                LD A,C
                LD (&BE5E),A
                XOR A
        GT1:    PUSH AF
                CALL OCTENC
                LD B,A
                LD A,(&BE5E)
                AND B
                LD B,A
                LD A,(&BE5D)
                CP B
                JR Z,GT2
                POP AF
                INC A
                LD HL,&BE66
                CP (HL)
                JR C,GT1
        GTDEH:  LD A,(&BE49)
                RET
        GT2:    POP AF
                RET
        ; GRA LINE ABSOLUTE (&BBF6), GRA LINE RELATIVE (&BBF9) : du curseur jusqu'à DE, HL, les deux bouts compris
        GLINER: CALL GREL
        GLINE:  PUSH DE
                PUSH HL
                LD A,(&BE48)
                CALL OCTENC
                LD (&BE5C),A
                CALL GASK
                CALL TOBASE
                LD (&BE4A),DE
                LD (&BE4C),HL
                POP HL
                POP DE
                CALL GMOVE
                CALL TOBASE
                LD (&BE4E),DE
                LD (&BE50),HL
                LD HL,(&BE4E)           ; dx et sens en x
                LD DE,(&BE4A)
                OR A
                SBC HL,DE
                LD A,1
                BIT 7,H
                JR Z,GL1
                CALL NEGHL
                LD A,&FF
        GL1:    LD (&BE52),HL
                LD (&BE56),A
                LD HL,(&BE50)           ; dy et sens en y
                LD DE,(&BE4C)
                OR A
                SBC HL,DE
                LD A,1
                BIT 7,H
                JR Z,GL2
                CALL NEGHL
                LD A,&FF
        GL2:    LD (&BE54),HL
                LD (&BE57),A
                LD A,H                  ; trait tout droit : chemin rapide (D202)
                OR L
                JP Z,GLH
                LD HL,(&BE52)
                LD A,H
                OR L
                JP Z,GLV
                CALL GLDIN              ; en biais, les deux bouts dans la fenêtre : chemin rapide (D203)
                JP NC,GLD
        GLN:    LD HL,(&BE54)
                LD DE,(&BE52)
                OR A
                SBC HL,DE
                JR C,GLX
                JR Z,GLX
                LD HL,(&BE54)           ; surtout vertical
                LD (&BE5A),HL
                SRL H
                RR L
                LD (&BE58),HL
        GLY1:   CALL GLPT
                LD HL,(&BE5A)
                LD A,H
                OR L
                RET Z
                DEC HL
                LD (&BE5A),HL
                LD A,(&BE57)
                LD HL,&BE4C
                CALL GLADD
                LD HL,(&BE58)
                LD DE,(&BE52)
                ADD HL,DE
                LD DE,(&BE54)
                OR A
                SBC HL,DE
                JR NC,GLY2
                ADD HL,DE
                LD (&BE58),HL
                JR GLY1
        GLY2:   LD (&BE58),HL
                LD A,(&BE56)
                LD HL,&BE4A
                CALL GLADD
                JR GLY1
        GLX:    LD HL,(&BE52)           ; surtout horizontal
                LD (&BE5A),HL
                SRL H
                RR L
                LD (&BE58),HL
        GLX1:   CALL GLPT
                LD HL,(&BE5A)
                LD A,H
                OR L
                RET Z
                DEC HL
                LD (&BE5A),HL
                LD A,(&BE56)
                LD HL,&BE4A
                CALL GLADD
                LD HL,(&BE58)
                LD DE,(&BE54)
                ADD HL,DE
                LD DE,(&BE52)
                OR A
                SBC HL,DE
                JR NC,GLX2
                ADD HL,DE
                LD (&BE58),HL
                JR GLX1
        GLX2:   LD (&BE58),HL
                LD A,(&BE57)
                LD HL,&BE4C
                CALL GLADD
                JR GLX1
        GLPT:   LD DE,(&BE4A)
                LD HL,(&BE4C)
                JP POINT
        ; (HL) sur 16 bits, plus 1 si A = 1, moins 1 sinon
        GLADD:  LD E,(HL)
                INC HL
                LD D,(HL)
                CP 1
                JR Z,GLA1
                DEC DE
                DEC DE
        GLA1:   INC DE
                LD (HL),D
                DEC HL
                LD (HL),E
                RET
        ; ---- traits tout droits rapides (D202) : de (&BE4A, &BE4C) à (&BE4E, &BE50), points de base ----
        ; horizontal : octet par octet (plusieurs points d'un coup) par SCR WRITE ; vertical : ligne après ligne.
        ; MASK détourne SCR WRITE (D110) : l'ordre des points compte, les traits à l'envers prennent le chemin normal.
        GLH:    LD A,(&BE56)
                INC A                   ; &FF -> 0 : vers la gauche
                JR NZ,GLH1
                CALL DETOUR
                JP NZ,GLN
                LD HL,(&BE4A)           ; les deux bouts échangés
                LD DE,(&BE4E)
                LD (&BE4A),DE
                LD (&BE4E),HL
        GLH1:   LD HL,(&BE4C)           ; y dans la fenêtre graphique ?
                BIT 7,H
                RET NZ
                EX DE,HL
                LD HL,(&BE6C)
                OR A
                SBC HL,DE
                JR Z,GLH2
                RET NC
        GLH2:   LD HL,(&BE6E)
                OR A
                SBC HL,DE
                RET C
                LD HL,(&BE4E)           ; bout droit : ramené dans la fenêtre
                BIT 7,H
                RET NZ
                EX DE,HL
                LD HL,(&BE6A)
                OR A
                SBC HL,DE
                JR NC,GLH3
                LD HL,(&BE6A)
                LD (&BE4E),HL
        GLH3:   LD HL,(&BE4A)           ; bout gauche : ramené dans la fenêtre
                BIT 7,H
                JR NZ,GLH4
                EX DE,HL
                LD HL,(&BE68)
                OR A
                SBC HL,DE
                JR C,GLH5
                JR Z,GLH5
        GLH4:   LD HL,(&BE68)
                LD (&BE4A),HL
        GLH5:   LD HL,(&BE4E)           ; nombre de points
                LD DE,(&BE4A)
                OR A
                SBC HL,DE
                RET C
                INC HL
                LD (&BE5A),HL
                LD HL,(&BE4C)
                CALL DOTPOS             ; HL = adresse, C = masque du premier point, B = points par octet - 1
                LD A,(&BE4A)
                AND B
                LD E,A
                LD A,B
                SUB E
                INC A
                LD E,A                  ; points de ce premier octet
                LD A,(&BE5C)
                LD B,A
                LD D,0
        GLH6:   LD A,D                  ; un point de plus dans l'octet
                OR C
                LD D,A
                PUSH HL
                LD HL,(&BE5A)
                DEC HL
                LD (&BE5A),HL
                LD A,H
                OR L
                POP HL
                JR Z,GLH8
                DEC E
                JR Z,GLH7
                SRL C
                JR GLH6
        GLH7:   LD C,D                  ; octet fini : écrit
                PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
                INC HL
        GLH9:   LD A,(&BE61)            ; octets pleins : tous les points d'un coup
                INC A
                LD E,A
                LD D,0
                PUSH HL
                LD HL,(&BE5A)
                OR A
                SBC HL,DE
                JR C,GLH10
                LD (&BE5A),HL
                LD A,H
                OR L
                POP HL
                PUSH AF
                LD C,&FF
                PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
                POP AF
                RET Z
                INC HL
                JR GLH9
        GLH10:  POP HL
                LD A,(&BE62)
                LD C,A
                JR GLH6
        GLH8:   LD C,D                  ; dernier octet
                JP &BDE8
        GLV:    CALL DETOUR             ; MASK : chemin normal (l'ordre des points compte)
                JP NZ,GLN
                LD DE,(&BE4A)           ; x dans la fenêtre graphique ?
                BIT 7,D
                RET NZ
                LD HL,(&BE68)
                OR A
                SBC HL,DE
                JR Z,GLV1
                RET NC
        GLV1:   LD HL,(&BE6A)
                OR A
                SBC HL,DE
                RET C
                LD HL,(&BE4C)           ; bas et haut du trait
                LD DE,(&BE50)
                LD A,(&BE57)
                INC A
                JR NZ,GLV2
                EX DE,HL
        GLV2:   LD (&BE58),HL           ; bas
                BIT 7,D                 ; haut : sous l'écran, rien
                RET NZ
                LD HL,(&BE6E)
                OR A
                SBC HL,DE
                JR NC,GLV3
                LD DE,(&BE6E)           ; ramené au haut de la fenêtre
        GLV3:   LD HL,(&BE58)
                BIT 7,H
                JR NZ,GLV4
                PUSH DE
                EX DE,HL
                LD HL,(&BE6C)
                OR A
                SBC HL,DE
                EX DE,HL
                POP DE
                JR C,GLV5
        GLV4:   LD HL,(&BE6C)           ; ramené au bas de la fenêtre
        GLV5:   EX DE,HL                ; HL = haut, DE = bas
                PUSH HL
                OR A
                SBC HL,DE
                POP HL
                RET C
                LD A,L
                SUB E
                INC A
                PUSH AF                 ; nombre de lignes (200 au plus)
                LD DE,(&BE4A)
                CALL DOTPOS
                LD A,(&BE5C)
                LD B,A
                POP AF
                LD E,A
        GLV6:   CALL SCRWR
                DEC E
                RET Z
                LD A,H                  ; ligne de points suivante, vers le bas
                ADD A,8
                LD H,A
                JR NC,GLV6
                LD A,L
                ADD A,&50
                LD L,A
                LD A,H
                ADC A,&C0
                LD H,A
                JR GLV6
        ; ---- traits en biais rapides (D203) : les deux bouts dans la fenêtre graphique, donc tout le trait ; mêmes
        ; points que le chemin normal (même calcul), mais l'adresse et le masque suivent d'un point au suivant ----
        GLDIN:  LD DE,(&BE4A)           ; NC si les deux bouts sont dans la fenêtre
                LD HL,(&BE4C)
                CALL DEDANS
                RET C
                LD DE,(&BE4E)
                LD HL,(&BE50)
                JP DEDANS
        GLD:    LD A,(&BE61)            ; masque du point de droite d'un octet, en &BE70
                LD B,A
                LD A,(&BE62)
        GLD0:   SRL A
                DJNZ GLD0
                LD (&BE70),A
                LD DE,(&BE4A)
                LD HL,(&BE4C)
                CALL DOTPOS             ; HL = adresse, C = masque du premier point
                LD A,(&BE5C)
                LD B,A
                PUSH HL
                LD HL,(&BE54)
                LD DE,(&BE52)
                OR A
                SBC HL,DE
                POP HL
                JR C,GDX
                JR Z,GDX
                LD DE,(&BE54)           ; surtout vertical
                LD (&BE5A),DE
                SRL D
                RR E
                LD (&BE58),DE
        GDY1:   PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
                PUSH HL
                LD HL,(&BE5A)
                LD A,H
                OR L
                DEC HL
                LD (&BE5A),HL
                POP HL
                RET Z
                CALL GSTY
                PUSH HL
                LD HL,(&BE58)
                LD DE,(&BE52)
                ADD HL,DE
                LD DE,(&BE54)
                OR A
                SBC HL,DE
                JR NC,GDY2
                ADD HL,DE
                LD (&BE58),HL
                POP HL
                JR GDY1
        GDY2:   LD (&BE58),HL
                POP HL
                CALL GSTX
                JR GDY1
        GDX:    LD DE,(&BE52)           ; surtout horizontal
                LD (&BE5A),DE
                SRL D
                RR E
                LD (&BE58),DE
        GDX1:   PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
                PUSH HL
                LD HL,(&BE5A)
                LD A,H
                OR L
                DEC HL
                LD (&BE5A),HL
                POP HL
                RET Z
                CALL GSTX
                PUSH HL
                LD HL,(&BE58)
                LD DE,(&BE54)
                ADD HL,DE
                LD DE,(&BE52)
                OR A
                SBC HL,DE
                JR NC,GDX2
                ADD HL,DE
                LD (&BE58),HL
                POP HL
                JR GDX1
        GDX2:   LD (&BE58),HL
                POP HL
                CALL GSTY
                JR GDX1
        ; un point à droite (&BE56 = 1) ou à gauche : masque C et adresse HL
        GSTX:   LD A,(&BE56)
                DEC A
                JR NZ,GSXL
                LD A,(&BE70)
                CP C
                JR Z,GSXR
                SRL C
                RET
        GSXR:   LD A,(&BE62)
                LD C,A
                INC HL
                RET
        GSXL:   LD A,(&BE62)
                CP C
                JR Z,GSXL2
                SLA C
                RET
        GSXL2:  LD A,(&BE70)
                LD C,A
                DEC HL
                RET
        ; un point vers le haut (&BE57 = 1 : ligne de l'écran d'avant) ou vers le bas
        GSTY:   LD A,(&BE57)
                DEC A
                JR NZ,GSYD
                LD A,H
                SUB 8
                LD H,A
                CP &C0
                RET NC
                LD DE,&3FB0
                ADD HL,DE
                RET
        GSYD:   LD A,H
                ADD A,8
                LD H,A
                RET NC
                LD DE,&C050
                ADD HL,DE
                RET
        ; NZ si SCR WRITE (&BDE8) est détourné (MASK, D110)
        DETOUR: PUSH HL
                PUSH DE
                LD HL,(&BDE9)
                LD DE,SCRWR
                OR A
                SBC HL,DE
                POP DE
                POP HL
                RET
        NEGHL:  XOR A
                SUB L
                LD L,A
                SBC A,A
                SUB H
                LD H,A
                RET
        ; SCR GET LOCATION (&BC0B) : écran en &C000, jamais décalé (le texte défile par recopie) ; D204
        SGLOC:  LD A,&C0
                LD HL,0
                RET
        ; SCR GET MODE (&BC11) : A = mode ; retenue en MODE 0, zéro en MODE 1
        SGMODE: LD A,(&BE06)
                CP 1
                RET
        ; SCR INK ENCODE (&BC2C) : A = encre -> A = octet codé ; les autres registres gardés
        SENC:   PUSH BC
                PUSH DE
                PUSH HL
                CALL OCTENC
                POP HL
                POP DE
                POP BC
                RET
        TENC0:  DEFB &00,&C0,&0C,&CC,&30,&F0,&3C,&FC,&03,&C3,&0F,&CF,&33,&F3,&3F,&FF
        TENC1:  DEFB &00,&F0,&0F,&FF
        TENC2:  DEFB &00,&FF
        ; les 27 couleurs du CPC, en valeurs du Gate Array
        COULEURS: DEFB &54,&44,&55,&5C,&58,&5D,&4C,&45,&4D,&56,&46,&57,&5E,&40
                DEFB &5F,&4E,&47,&4F,&52,&42,&53,&5A,&59,&5B,&4A,&43,&4B
        {3}

        CRTC:   DEFB 63,40,46,&8E,38,0,25,30,0,7,0,0,&30,0
        ENCRES: DEFB 1,24,20,6,26,0,2,8,10,12,14,16,18,22,1,16,1   ; couleurs du CPC au démarrage ; bordure : 1
        BRANCHE: DEFW &BB5A,TXTOUT
                DEFW &BB6C,CLS
                DEFW &BB75,SETCUR
                DEFW &BB78,GETCUR
                DEFW &BB69,FENETRE
                DEFW &BB87,VALIDE
                DEFW &BB1E,TESTKEY
                DEFW &BB24,JOYST
                DEFW &BB09,LITCAR
                DEFW &BB1B,LITCLE
                DEFW &BB06,{4}
                DEFW &BB18,ATTCLE
                DEFW &BBB4,FLUX
                DEFW &BB66,FENON
                DEFW &BCA7,SRESET
                DEFW &BCAA,SQUEUE
                DEFW &BCAD,SCHECK
                DEFW &BCBC,SENVA
                DEFW &BCBF,SENVT
                DEFW &BD0D,TEMPS
                DEFW &BD10,TEMPSM
                DEFW &BCEF,INITEV
                DEFW &BCE9,AJTICK
                DEFW &BCEC,DELTICK
                DEFW &BCB0,ARMEV
                DEFW &BCB3,SREL
                DEFW &BD64,EI2R
                DEFW &BD67,EB2R
                DEFW &BD6A,ER2I
                DEFW &BD70,EFIX
                DEFW &BD79,EX10
                DEFW &BD7C,EADD
                DEFW &BD82,ERSUB
                DEFW &BD85,EMUL
                DEFW &BD88,EDIV
                DEFW &BD8E,ECMP
                DEFW &BD91,ENEG
                DEFW &BD94,ESGN
                DEFW &BD97,EANGLE
                DEFW &BD9A,EPI
                DEFW &BD9D,ESQR
                DEFW &BDA0,EPOW
                DEFW &BDA3,ELOG
                DEFW &BDA6,ELOG10
                DEFW &BDA9,EEXP
                DEFW &BDAC,ESIN
                DEFW &BDAF,ECOS
                DEFW &BDB2,ETAN
                DEFW &BDB5,EATN
                DEFW &B915,PROBE
                DEFW &BC3E,SETCLI
                DEFW &BC41,GETCLI
                DEFW &BBB7,ECHANGE
                DEFW &BC0E,SETMODE
                DEFW &BB90,SETPEN
                DEFW &BB93,GETPEN
                DEFW &BB96,SETPAP
                DEFW &BB99,GETPAP
                DEFW &BC32,SETINK
                DEFW &BC35,GETINK
                DEFW &BC38,SETBORD
                DEFW &BC3B,GETBORD
                DEFW &BBC0,GMOVE
                DEFW &BBC3,GMOVR
                DEFW &BBC6,GASK
                DEFW &BBC9,GSETOR
                DEFW &BBCC,GGETOR
                DEFW &BBD5,GWWID
                DEFW &BBD8,GWHEI
                DEFW &BBDB,GCLR
                DEFW &BBDE,GSPEN
                DEFW &BBE1,GGPEN
                DEFW &BBE4,GSPAP
                DEFW &BBE7,GGPAP
                DEFW &BBEA,GPLOT
                DEFW &BBED,GPLOTR
                DEFW &BBF0,GTEST
                DEFW &BBF3,GTESTR
                DEFW &BBF6,GLINE
                DEFW &BBF9,GLINER
                DEFW &BC11,SGMODE
                DEFW &BC0B,SGLOC
                DEFW &BC1D,DOTPOS
                DEFW &BC2C,SENC
                DEFW &BDE8,SCRWR
                DEFW &BC59,SACC
                DEFW &BBCF,GWINW
                DEFW &BBD2,GWINH
                DEFW &BD19,FLYB
                DEFW 0
        TOUCHES: {2}
        {5}
        POLICE: {1}
        PAGES:  {7}
        PROG:
        """;

    /// <summary>Une touche du CPC reliée à la manette : Joystick 0 ou 1, Buttons = boutons à appuyer ensemble.</summary>
    public sealed record KeyLink(int Key, int Joystick, int Buttons);

    /// <summary>
    /// Correspondance de base (D183) : tir 1 = espace (47), tir 2 = ENTRÉE (18 et 6), directions = flèches (0, 2, 8, 1),
    /// le tir 1 étant le bouton principal, bit 4 (valeur 16 de JOY) : D184, essai de l'utilisateur dans AMSpiriT ;
    /// et les touches de la manette elles-mêmes (72 à 77 pour la manette 1, 48 à 53 pour la manette 2).
    /// </summary>
    public static readonly IReadOnlyList<KeyLink> BaseKeys =
    [
        new(47, 0, 0x10), new(18, 0, 0x20), new(6, 0, 0x20),
        new(0, 0, 0x01), new(2, 0, 0x02), new(8, 0, 0x04), new(1, 0, 0x08),
        new(72, 0, 0x01), new(73, 0, 0x02), new(74, 0, 0x04), new(75, 0, 0x08), new(76, 0, 0x10), new(77, 0, 0x20),
        new(48, 1, 0x01), new(49, 1, 0x02), new(50, 1, 0x04), new(51, 1, 0x08), new(52, 1, 0x10), new(53, 1, 0x20)
    ];

    /// <summary>
    /// Boutons donnés, dans l'ordre, aux autres touches d'INKEY(n) (idée de l'utilisateur, D183) : tir 2, tirs 1 et 2
    /// ensemble, puis la manette 2.
    /// </summary>
    public static readonly IReadOnlyList<(int Joystick, int Buttons, string Name)> FreeButtons =
    [
        (0, 0x20, "au tir 2 de la manette 1"),
        (0, 0x30, "aux tirs 1 et 2 de la manette 1 appuyés ensemble"),
        (1, 0x01, "au haut de la manette 2"), (1, 0x02, "au bas de la manette 2"),
        (1, 0x04, "à la gauche de la manette 2"), (1, 0x08, "à la droite de la manette 2"),
        (1, 0x10, "au tir 1 de la manette 2"), (1, 0x20, "au tir 2 de la manette 2"),
        (1, 0x30, "aux tirs 1 et 2 de la manette 2 appuyés ensemble")
    ];

    const string Wheel = """
        ; ---------- KM WAIT CHAR (&BB06) avec INPUT (D184) : saisie à la manette ----------
        ; lettre proposée en couleurs inversées là où elle va s'écrire ; haut / bas : lettre précédente / suivante
        ; (défile en restant appuyé) ; tir 2 : groupe suivant ; droite : écrit la lettre ; gauche : efface (DEL) ;
        ; tir 1 : valide (13). &BE0A : lettre proposée, &BE0C : début de son groupe, &BE0E : compteur du défilement.
        ; Ce qu'on voit est ce qu'on obtient (essai de l'utilisateur) : après une lettre écrite et au début de chaque
        ; INPUT, un carré vide (&BE10 = 0) ; droite sur le carré vide écrit un espace ; haut / bas repartent de la
        ; dernière lettre (la toute première fois, A est montré sans bouger, &BE12) ; tir 1 avec une lettre montrée :
        ; la lettre est écrite, puis la saisie validée (&BE11 : 13 en attente).
        MOLETTE: PUSH BC
                PUSH DE
                PUSH HL
                CALL MOLINIT
                CALL MONTRE
        MOLB:   CALL MOLUN
                JR C,MOLRET
                CALL PAUSE
                JR MOLB
        MOLRET: POP HL
                POP DE
                POP BC
                RET
        MOLINIT: LD HL,(&BE0A)
                LD A,H
                OR L
                RET NZ
                LD HL,GROUPES
                LD (&BE0A),HL
                LD (&BE0C),HL
                LD A,1
                LD (&BE12),A            ; première fois : A montré sans bouger
                RET
        ; un passage : retenue = fini (A = caractère) ; sinon, pas fini
        MOLUN:  CALL MOLINIT
                LD A,(&BE11)            ; 13 en attente (tir 1 après une lettre montrée)
                OR A
                JR Z,MOLU1
                XOR A
                LD (&BE11),A
                CALL EFFACE
                LD A,13
                SCF
                RET
        MOLU1:  CALL LIRE
                LD A,(&BE09)
                CPL
                LD C,A
                LD A,(&BE07)
                LD (&BE09),A
                LD B,A                  ; boutons tenus
                AND C
                LD E,A                  ; boutons qui viennent d'être appuyés
                BIT 3,E
                JP NZ,MOLDR
                BIT 2,E
                JP NZ,MOLGA
                BIT 4,E                 ; tir 1 (bit 4) : valider
                JP NZ,MOLT1
                BIT 5,E                 ; tir 2 (bit 5) : groupe suivant
                JP NZ,MOLT2
                LD A,E
                AND 3
                JR NZ,MOLHB
                LD A,B
                AND 3
                JR Z,MOLRIEN            ; ni haut ni bas tenu
                LD HL,&BE0E
                DEC (HL)
                JR NZ,MOLRIEN
                LD (HL),4               ; tenu : une lettre toutes les 4 attentes
                JR MOLHB2
        MOLHB:  LD A,15                 ; nouvel appui : une lettre, puis attente avant de défiler
                LD (&BE0E),A
        MOLHB2: LD HL,(&BE0A)
                LD A,(&BE12)
                OR A
                JR NZ,MOLNOUV           ; toute première fois : la lettre est montrée sans bouger
                BIT 0,B
                JR Z,MOLBAS
                LD DE,(&BE0C)           ; haut : lettre précédente
                OR A
                SBC HL,DE
                ADD HL,DE
                JR NZ,MOLPREC
        MOLDER: INC HL                  ; au début du groupe : la dernière lettre du groupe
                LD A,(HL)
                OR A
                JR NZ,MOLDER
        MOLPREC: DEC HL
                JR MOLNOUV
        MOLBAS: INC HL                  ; bas : lettre suivante ; après la dernière, la première
                LD A,(HL)
                OR A
                JR NZ,MOLNOUV
                LD HL,(&BE0C)
                JR MOLNOUV
        MOLT2:  LD HL,(&BE0C)           ; tir 2 : groupe suivant, à sa première lettre
        MOLT2A: LD A,(HL)
                INC HL
                OR A
                JR NZ,MOLT2A
                LD A,(HL)
                CP &FF
                JR NZ,MOLT2B
                LD HL,GROUPES
        MOLT2B: LD (&BE0C),HL
        MOLNOUV: LD (&BE0A),HL
                XOR A
                LD (&BE12),A
                INC A
                LD (&BE10),A            ; une lettre est montrée
                CALL MONTRE
        MOLRIEN: OR A
                RET
        MOLDR:  CALL EFFACE             ; droite : la lettre montrée, ou un espace sur le carré vide
                LD A,(&BE10)
                OR A
                LD A,32
                JR Z,MOLDR2
                LD HL,(&BE0A)
                LD A,(HL)
        MOLDR2: LD B,A
                XOR A
                LD (&BE10),A            ; ensuite : carré vide
                LD A,B
                SCF
                RET
        MOLGA:  CALL EFFACE             ; gauche : DEL, efface la lettre d'avant
                LD A,127
                SCF
                RET
        MOLT1:  CALL EFFACE             ; tir 1 : la saisie est finie
                LD A,(&BE10)
                OR A
                LD A,13
                JR Z,MOLT1R
                XOR A                   ; une lettre est montrée : elle est écrite d'abord, 13 ensuite
                LD (&BE10),A
                INC A
                LD (&BE11),A
                LD HL,(&BE0A)
                LD A,(HL)
        MOLT1R: SCF
                RET
        ; la lettre proposée en couleurs inversées, ou un espace normal, sans bouger le curseur
        MONTRE: LD A,(&BE10)            ; carré vide (un espace inversé) ou la lettre proposée
                OR A
                LD A,32
                JR Z,MONTRE2
                LD HL,(&BE0A)
                LD A,(HL)
        MONTRE2: LD B,&FF
                JR DESSINE
        EFFACE: LD A,32
                LD B,0
        DESSINE: LD C,A
                LD A,(&BE1D)            ; largeur de la fenêtre (D190)
                LD D,A
                LD A,(&BE00)
                CP D
                RET NC                  ; au bout de la ligne : rien à montrer
                LD A,B
                LD (&BE0F),A
                LD HL,(&BE00)
                PUSH HL
                LD A,C
                CALL TXTOUT
                POP HL
                LD (&BE00),HL
                XOR A
                LD (&BE0F),A
                RET
        ; environ 1/50 de seconde
        PAUSE:  LD D,24
        PAUSE1: LD B,0
        PAUSE2: DJNZ PAUSE2
                DEC D
                JR NZ,PAUSE1
                RET
        """;

    /// <summary>Le système seul, pour un programme de la longueur donnée (sans le programme).</summary>
    /// <param name="withInput">Vrai si le programme utilise INPUT ou LINE INPUT : la saisie à la manette est ajoutée (D184).</param>
    /// <param name="otherPages">D189 : longueurs du programme dans les pages 1, 2... (programLength : sa longueur dans la
    /// page 0, juste après le système).</param>
    public static byte[] SystemCode(int programLength, IEnumerable<KeyLink>? extraKeys = null, bool withInput = false,
                                    IReadOnlyList<int>? otherPages = null)
    {
        IReadOnlyList<int> others = otherPages ?? [];
        string pages = "DEFB " + others.Count + string.Concat(others.Select((length, i) => $"\n                DEFB &{0x81 + i:X2}\n                DEFW {length}"));
        string keys = string.Join(",", BaseKeys.Concat(extraKeys ?? []).Select(k => $"{k.Key},{k.Joystick},&{k.Buttons:X2}")) + ",&FF";
        var font = new StringBuilder();
        byte[] glyphs = Gx4000Font.Bytes();
        for (int i = 0; i < glyphs.Length; i += 8)
        {
            if (i > 0)
                font.Append("\n        ");
            font.Append("DEFB ").Append(string.Join(",", glyphs.Skip(i).Take(8).Select(b => $"&{b:X2}")));
        }
        string source = Template.Replace("{0}", programLength.ToString()).Replace("{1}", font.ToString())
                                .Replace("{2}", "DEFB " + keys)
                                .Replace("{3}", withInput ? Wheel.Trim() : "MOLUN:  OR A                   ; pas de saisie à la manette\n                RET")
                                .Replace("{4}", withInput ? "MOLETTE" : "ATTCAR")
                                .Replace("{5}", withInput
                                    ? "GROUPES: DEFB " + string.Join(",0,", WheelGroups.Select(g => string.Join(",", g.Select(c => (int)c)))) + ",0,&FF"
                                    : "")
                                .Replace("{7}", pages);
        var emitter = new Z80Emitter(0);
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(source, emitter, "gx_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Système de la cartouche : " + string.Join(" / ", errors.Select(e => e.ToString())));
        return emitter.ToBytes();
    }

    /// <summary>La cartouche : notre système, le jeu de caractères, puis le programme compilé (chargé en &amp;4000).</summary>
    /// <summary>
    /// Mémoire pour le programme : de &amp;4000 jusqu'aux nombres de travail des calculs à virgule (&amp;B700, D196), sous les
    /// variables du son (&amp;B800, D192) et les routines de la ROM simulées (&amp;B900).
    /// </summary>
    public const int MaxProgram = 0xB700 - 0x4000;

    /// <summary>
    /// D189 : comment le programme est rangé dans la cartouche : sa longueur dans la page 0 (après le système), puis dans
    /// les pages 1, 2... (16 Ko chacune au plus). La taille du système dépend du nombre de pages : on recalcule jusqu'à ce
    /// qu'elle ne change plus.
    /// </summary>
    public static (int FirstPart, IReadOnlyList<int> OtherPages) SplitProgram(int length, IEnumerable<KeyLink>? extraKeys = null,
                                                                               bool withInput = false)
    {
        List<KeyLink> keys = extraKeys?.ToList() ?? [];
        int pages = 0;
        for (int round = 0; round < 8; round++)
        {
            int system = SystemCode(0, keys, withInput, Enumerable.Repeat(0, pages).ToList()).Length;
            int first = Math.Min(length, CartridgeImage.PageSize - system);
            int rest = length - first;
            int needed = (rest + CartridgeImage.PageSize - 1) / CartridgeImage.PageSize;
            if (needed == pages)
            {
                var others = new List<int>();
                for (int p = 0; p < pages; p++)
                    others.Add(Math.Min(CartridgeImage.PageSize, rest - p * CartridgeImage.PageSize));
                return (first, others);
            }
            pages = needed;
        }
        throw new InvalidOperationException("Rangement du programme dans la cartouche impossible.");
    }

    /// <summary>
    /// La cartouche : notre système, le jeu de caractères, puis le programme compilé (chargé en &amp;4000) ; ce qui ne tient
    /// pas dans la page 0 va dans les pages suivantes (D189).
    /// </summary>
    public static CartridgeImage Build(byte[] program, IEnumerable<KeyLink>? extraKeys = null, bool withInput = false)
    {
        if (program.Length > MaxProgram)
            throw new InvalidOperationException(
                $"Le programme fait {program.Length} octets : sur la cartouche, la mémoire en accepte {MaxProgram} au plus.");
        List<KeyLink> keys = extraKeys?.ToList() ?? [];
        (int first, IReadOnlyList<int> others) = SplitProgram(program.Length, keys, withInput);
        byte[] system = SystemCode(first, keys, withInput, others);
        var cartridge = new CartridgeImage();
        cartridge.AddPage([.. system, .. program[..first]]);
        int start = first;
        foreach (int length in others)
        {
            cartridge.AddPage(program[start..(start + length)]);
            start += length;
        }
        return cartridge;
    }
}
