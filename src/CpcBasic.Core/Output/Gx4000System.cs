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
/// PEN, PAPER, INK (sans clignotement), BORDER (D186). Dessin (D187) : PLOT, DRAW, MOVE (et relatifs), TEST, CLG,
/// ORIGIN, GRAPHICS PEN / PAPER ; modes d'écriture XOR, AND, OR, fenêtres graphiques, SCR WRITE (&BDE8) pour MASK, FILL
/// et PAINT, FRAME (D188). Fenêtres de texte (D190) : WINDOW, les 8 flux #0 à #7 (chacun son curseur, sa fenêtre,
/// son stylo et son papier), WINDOW SWAP, CLS dans la fenêtre, défilement de la fenêtre. Pas encore faits : le son,
/// les encres clignotantes ; les autres adresses de la ROM (de &B900 à &BDFF) répondent par un simple retour.
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
        ; &0038 : interruption (si le programme en autorise) : rien à faire
                EI
                RET
                DEFS 6                  ; jusqu'à &0040
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
        ; Encres du CPC au démarrage (D186) : les 16 stylos, puis la bordure (stylo 16)
                LD HL,ENCRES
                LD E,0
        ENCRE:  LD B,(HL)
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
        COPFIN: CALL &4000
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
        LIRE:   LD BC,&F40E
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
        ; SCR SET INK (&BC32) : A = stylo, B = couleur (la seconde couleur, clignotante, n'est pas encore faite)
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
        ; SCR SET BORDER (&BC38) : B = couleur
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
                LD C,B
                POP DE
                POP HL
                RET
        ; E = stylo (16 = bordure), B = couleur du CPC (0 à 26) : notée, puis envoyée au Gate Array
        ENCRE1: LD D,0
                LD HL,&BE20
                ADD HL,DE
                LD (HL),B
                LD A,B
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
        NEGHL:  XOR A
                SUB L
                LD L,A
                SBC A,A
                SUB H
                LD H,A
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
    /// <summary>Mémoire pour le programme : de &amp;4000 jusqu'aux routines de la ROM simulées (&amp;B900).</summary>
    public const int MaxProgram = 0xB900 - 0x4000;

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
