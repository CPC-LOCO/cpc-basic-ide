;@NOM TYPO
;@GENRE COMMANDE
;@TEXTE MESSAGE, le texte à écrire
;@OPTION X, 0, 639, -1, position horizontale, comme MOVE (sautée : là où est le curseur graphique)
;@OPTION Y, 0, 399, -1, position verticale, comme MOVE (sautée : là où est le curseur graphique)
;@AIDE Écrit un texte en petits caractères de 4 points sur 8 : deux petits caractères par caractère du CPC.
;@AIDE 40 colonnes en MODE 0, 80 en MODE 1, 160 en MODE 2. TYPO texte [, x, y] : au curseur graphique, ou en x, y.
;@AIDE Couleurs : GRAPHICS PEN et GRAPHICS PAPER (le fond est toujours dessiné). Un nombre impair de caractères est complété par un espace.
;@AIDE Caractères : de l'espace à z, avec à é ù (version française du CPC) ; les autres deviennent des espaces. DEFTYPO change un petit caractère.
;@AIDE Écrit directement dans la mémoire de l'écran (2,6 fois plus vite que le firmware en MODE 0, 4,4 en MODE 1, 6,4 en MODE 2) :
;@AIDE x est arrondi vers la gauche à un multiple de 8 ; la fenêtre graphique et le mode de GRAPHICS PEN ne sont pas suivis ;
;@AIDE un caractère qui sortirait de l'écran n'est pas écrit ; l'écran doit être en &C000 (toujours, sauf changement en assembleur).
;@AUTEUR RetroPoke (Philippe Moulin, fonte et RSX TYPO de 2020), écriture directe par CPC BASIC IDE (D172, D173)

; Deux petits caractères (TYPO_FONTE) forment un caractère de 8 x 8 (lignes dans LIGNES).
; Chaque ligne de 8 points est écrite octet par octet : 4 octets en MODE 0, 2 en MODE 1, 1 en MODE 2 ;
; octet = fond XOR ((stylo XOR fond) AND masque des points allumés). Les deux couleurs sont écrites dans le code
; au départ (dans les AND et XOR), le MODE choisit sa routine une fois pour tout le texte ; la ligne suivante et
; l'octet suivant sont calculés ici (l'écran fait 16 Ko en &C000, 8 blocs de 2 Ko qui tournent sur eux-mêmes).

        LD HL,(X)               ; position : X et Y sautées (-1 toutes les deux) : au curseur graphique
        LD DE,(Y)
        LD A,H
        AND D
        INC A
        JR Z,PLACE
        CALL &BBC6              ; GRA ASK CURSOR : DE = x, HL = y
        LD BC,(X)
        BIT 7,B
        JR NZ,GARDEX
        LD D,B
        LD E,C
GARDEX: LD BC,(Y)
        BIT 7,B
        JR NZ,GARDEY
        LD H,B
        LD L,C
GARDEY: CALL &BBC0              ; GRA MOVE ABSOLUTE
PLACE:  CALL &BBC6              ; GRA ASK CURSOR
        LD (CURX),DE
        LD (CURY),HL
        CALL &BBCC              ; GRA GET ORIGIN : DE = x, HL = y
        LD (ORIGX),DE
        LD BC,(CURY)
        ADD HL,BC
        SRA H
        RR L                    ; ligne du haut du caractère (0 en bas de l'écran, 199 en haut)
        LD A,1                  ; hors de l'écran en hauteur : rien n'est écrit (le curseur avance quand même)
        BIT 7,H
        JR NZ,HAUTEUR
        LD A,H
        OR A
        LD A,1
        JR NZ,HAUTEUR
        LD A,L
        CP 200
        LD A,1
        JR NC,HAUTEUR
        LD A,L
        CP 7
        LD A,1
        JR C,HAUTEUR
        XOR A
HAUTEUR:
        LD (HORS),A
        LD (BASEY),HL
        XOR A
        LD (VALIDE),A           ; adresse du premier caractère pas encore calculée

        CALL &BC11              ; SCR GET MODE
        CP 1
        JR C,MODE0
        JR Z,MODE1
        LD HL,DES2              ; MODE 2 : 1 octet par ligne, caractère de 8 de large
        LD DE,8
        LD BC,632
        XOR A
        JR MODEOK
MODE1:  LD HL,DES1
        LD DE,16
        LD BC,312
        LD A,1
        JR MODEOK
MODE0:  LD HL,DES0
        LD DE,32
        LD BC,152
        LD A,2
MODEOK: LD (DESMODE+1),HL       ; la routine du MODE
        LD (PAS),DE             ; largeur d'un caractère (unités de MOVE)
        LD (LIMX),BC            ; dernier point où un caractère commence encore dans l'écran
        LD (DECAL),A            ; x de MOVE -> point de l'écran : décalage à droite

        CALL &BBE1              ; GRA GET PEN
        CALL &BC2C              ; SCR INK ENCODE
        LD B,A
        CALL &BBE7              ; GRA GET PAPER
        CALL &BC2C
        LD C,A                  ; C = fond
        XOR B                   ; A = stylo XOR fond
        LD (C0A+1),A            ; les couleurs écrites dans le code des trois routines
        LD (C1A+1),A
        LD (C1B+1),A
        LD (C2A+1),A
        LD A,C
        LD (C0X+1),A
        LD (C1X+1),A
        LD (C1Y+1),A
        LD (C2X+1),A
        XOR A
        LD (LIGNES),A           ; lignes 1 et 8 vides
        LD (LIGNES+7),A

        LD HL,(MESSAGE)         ; fiche du texte : longueur, puis adresse des lettres ; deux lettres à la fois
        LD A,(HL)
        OR A
        JR Z,FINI
        LD (COMPTE),A
        INC HL
        LD E,(HL)
        INC HL
        LD D,(HL)
        LD (LETTRE),DE
PAIRE:  LD HL,(LETTRE)
        LD A,(HL)
        INC HL
        LD C,(HL)
        INC HL
        LD (LETTRE),HL
        PUSH BC
        CALL TYPO_ADRESSE
        EX DE,HL
        POP BC
        LD A,(COMPTE)
        DEC A
        JR NZ,DEUX
        LD C,32
        INC A
DEUX:   DEC A
        LD (COMPTE),A
        LD A,C
        CALL TYPO_ADRESSE
        LD B,H
        LD C,L
        LD HL,LIGNES+1
        CALL UNIR
        CALL UNIR
        CALL UNIR
        CALL DESSINE
        LD A,(COMPTE)
        OR A
        JR NZ,PAIRE
FINI:   LD DE,(CURX)            ; le curseur graphique après le texte
        LD HL,(CURY)
        CALL &BBC0              ; GRA MOVE ABSOLUTE
        RET

; un octet de chaque dessin donne deux lignes : DE = 1er, BC = 2e, HL = ligne
UNIR:   LD A,(BC)
        RRCA
        RRCA
        RRCA
        RRCA
        AND &0F
        LD (HL),A
        LD A,(DE)
        AND &F0
        OR (HL)
        LD (HL),A
        INC HL
        LD A,(BC)
        AND &0F
        LD (HL),A
        LD A,(DE)
        RLCA
        RLCA
        RLCA
        RLCA
        AND &F0
        OR (HL)
        LD (HL),A
        INC HL
        INC DE
        INC BC
        RET

; écrit LIGNES au curseur (CURX), puis avance CURX d'un caractère
DESSINE:
        LD A,(HORS)
        OR A
        JR NZ,AVANCE
        LD HL,(CURX)
        LD DE,(ORIGX)
        ADD HL,DE
        LD A,(DECAL)
        OR A
        JR Z,DXOK
DXBCL:  SRA H
        RR L
        DEC A
        JR NZ,DXBCL
DXOK:   BIT 7,H                 ; à gauche de l'écran : rien
        JR NZ,AVANCE
        EX DE,HL                ; DE = point de l'écran
        LD HL,(LIMX)
        OR A
        SBC HL,DE
        JR C,AVANCE             ; à droite de l'écran : rien
        LD A,(VALIDE)
        OR A
        JR NZ,DESMODE
        LD HL,(BASEY)           ; premier caractère écrit : son adresse par le firmware, ensuite on avance
        CALL &BC1D              ; SCR DOT POSITION : HL = octet de l'écran
        LD (ADR),HL
        LD A,1
        LD (VALIDE),A
DESMODE:
        CALL DES1               ; DES0, DES1 ou DES2, choisie au départ selon le MODE
AVANCE: LD HL,(CURX)
        LD DE,(PAS)
        ADD HL,DE
        LD (CURX),HL
        RET

; octet suivant sur la même ligne (HL), dans le même bloc de 2 Ko
DROITE: INC L
        RET NZ
        INC H
        LD A,H
        AND 7
        RET NZ
        LD A,H
        SUB 8
        LD H,A
        RET

; ligne suivante (HL) : +&800 ; après la 8e ligne d'un rang, retour en haut du bloc et +80, dans les 2 Ko
DESCEND:
        LD A,H
        ADD A,8
        LD H,A
        RET NC
        LD A,L
        ADD A,80
        LD L,A
        LD A,H
        ADC A,&C0
        LD H,A
        CP &C8
        RET NZ
        LD H,&C0
        RET

; MODE 2 : 1 octet par ligne = les 8 points
DES2:   LD HL,(ADR)
        LD DE,LIGNES
        LD B,8
L2:     LD A,(DE)
        INC DE
C2A:    AND 0                   ; stylo XOR fond (écrit au départ)
C2X:    XOR 0                   ; fond
        LD (HL),A
        CALL DESCEND
        DJNZ L2
        LD HL,(ADR)
        CALL DROITE
        LD (ADR),HL
        RET

; MODE 1 : 2 octets par ligne, 4 points chacun : masque = le quartet en haut et en bas de l'octet
DES1:   LD HL,(ADR)
        LD DE,LIGNES
        LD B,8
L1:     PUSH HL
        LD A,(DE)
        INC DE
        LD C,A
        RRCA                    ; quartet du haut, en double
        RRCA
        RRCA
        RRCA
        XOR C
        AND &0F
        XOR C
C1A:    AND 0
C1X:    XOR 0
        LD (HL),A
        CALL DROITE
        LD A,C                  ; quartet du bas, en double
        RRCA
        RRCA
        RRCA
        RRCA
        XOR C
        AND &F0
        XOR C
C1B:    AND 0
C1Y:    XOR 0
        LD (HL),A
        POP HL
        CALL DESCEND
        DJNZ L1
        LD HL,(ADR)
        CALL DROITE
        CALL DROITE
        LD (ADR),HL
        RET

; MODE 0 : 4 octets par ligne, 2 points chacun : gauche = &AA, droite = &55
DES0:   LD HL,(ADR)
        LD DE,LIGNES
        LD B,8
L0:     PUSH HL
        PUSH BC
        LD A,(DE)
        INC DE
        LD C,A
        LD B,4
L0B:    XOR A
        RLC C
        JR NC,G0
        LD A,&AA
G0:     RLC C
        JR NC,D0
        OR &55
D0:
C0A:    AND 0
C0X:    XOR 0
        LD (HL),A
        CALL DROITE
        DJNZ L0B
        POP BC
        POP HL
        CALL DESCEND
        DJNZ L0
        LD HL,(ADR)
        LD B,4
A0:     CALL DROITE
        DJNZ A0
        LD (ADR),HL
        RET

CURX:   DS 2
CURY:   DS 2
ORIGX:  DS 2
BASEY:  DS 2
PAS:    DS 2
LIMX:   DS 2
DECAL:  DS 1
HORS:   DS 1
VALIDE: DS 1
ADR:    DS 2
LETTRE: DS 2
COMPTE: DS 1
LIGNES: DS 8
