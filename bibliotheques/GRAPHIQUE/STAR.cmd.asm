;@NOM STAR
;@GENRE COMMANDE
;@VALEUR X, -2000, 2000, position horizontale du centre (repère natif, comme CIRCLE)
;@VALEUR Y, -2000, 2000, position verticale du centre (0 en haut, 199 en bas)
;@VALEUR R1, 0, 1000, rayon interne : du centre au creux entre deux pointes (en lignes)
;@VALEUR R2, 0, 1000, rayon externe : du centre au bout des pointes (en lignes)
;@VALEUR POINTES, 2, 100, nombre de pointes
;@OPTION STYLO, 0, 15, -1, couleur du contour (sautée : stylo du tracé)
;@OPTION REMPLISSAGE, 0, 15, -1, couleur de l'intérieur (pas encore : ignorée)
;@OPTION ANGLE, -32768, 32767, 0, rotation en degrés (0 : une pointe en haut ; positif : vers la gauche)
;@AIDE Dessine une étoile : STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle].
;@AIDE Avec un rayon interne assez grand, on obtient un polygone (R1 = R2 x cosinus(180 / pointes) : 6 pointes et R1 = R2 x 0,866 donnent un hexagone).
;@AIDE Même repère et même largeur que CIRCLE (vrai moniteur Amstrad). Le stylo et la position du tracé ne changent pas.
;@AUTEUR CPC BASIC IDE (D150, D151)

; Sommet k (de 0 à 2 x pointes - 1) : rayon externe si k est pair, interne s'il est impair,
; angle (en centièmes de degré) = 9000 + 100 x ANGLE + k x 18000 / pointes.
; Écart vertical = arrondi(rayon x sinus) en lignes ; écart horizontal = arrondi(rayon x cosinus x 69/64 x 0,5 / 1 / 2 selon le MODE).
; Les traits sont tracés par le firmware (GRA LINE ABSOLUTE), avec l'origine remise un moment en 0,0.

        LD HL,(POINTES)         ; valeurs calculées hors limites : rien n'est dessiné
        LD A,H
        OR A
        RET NZ
        LD A,L
        CP 2
        RET C
        CP 101
        RET NC
        LD (NB),A
        LD HL,(R1)
        CALL RAYONOK
        RET NC
        LD HL,(R2)
        CALL RAYONOK
        RET NC

        CALL &BC11              ; SCR GET MODE
        CP 3
        JR C,MODEOK
        XOR A
MODEOK: LD (MODE),A

        LD HL,18000             ; pas de l'angle : 18000 / pointes (centièmes de degré), et son reste
        LD A,(NB)
        LD C,A
        CALL DIV8
        LD (PAS),HL
        LD (RESTE),A

        LD HL,(ANGLE)           ; angle ramené de 0 à 359
        LD DE,360
ANG1:   BIT 7,H
        JR Z,ANG2
        ADD HL,DE
        JR ANG1
ANG2:   OR A
        SBC HL,DE
        JR NC,ANG2
        ADD HL,DE
        LD DE,100               ; en centièmes de degré
        CALL MUL16
        LD DE,9000              ; + 90 degrés : la première pointe en haut
        ADD HL,DE
        LD (TETA),HL
        XOR A
        LD (ERREUR),A
        LD (K),A

        CALL &BBE1              ; GRA GET PEN : gardé pour la fin
        LD (ANCIEN),A
        LD HL,(STYLO)
        BIT 7,H
        JR NZ,GARDE             ; stylo sauté (-1) : stylo du tracé
        LD A,L
        CALL &BBDE              ; GRA SET PEN
GARDE:  CALL &BBC6              ; GRA ASK CURSOR : position gardée
        LD (CURX),DE
        LD (CURY),HL
        CALL &BBCC              ; GRA GET ORIGIN : origine gardée
        LD (ORIX),DE
        LD (ORIY),HL
        LD DE,0
        LD HL,0
        CALL &BBC9              ; GRA SET ORIGIN 0,0

SOMMET: LD A,(K)                ; rayon : externe (pointe) si k est pair, interne (creux) sinon
        RRA
        LD HL,(R2)
        JR NC,SOM1
        LD HL,(R1)
SOM1:   LD (RAYON),HL

        LD HL,(TETA)            ; vertical : y = Y - arrondi(rayon x sinus), puis (199 - y) x 2 pour le firmware
        CALL SINUS
        LD DE,(RAYON)
        LD A,14
        CALL ECART
        EX DE,HL
        LD HL,(Y)
        OR A
        SBC HL,DE
        EX DE,HL
        LD HL,199
        OR A
        SBC HL,DE
        ADD HL,HL
        LD (LY),HL

        LD HL,(TETA)            ; horizontal : x = X + arrondi(rayon x cosinus x 69/64 x 0,5 / 1 / 2)
        LD DE,9000
        ADD HL,DE
        CALL SINUS
        CALL FOIS69
        LD DE,(RAYON)
        LD A,(MODE)
        LD B,A
        LD A,15
        SUB B
        CALL ECART
        LD DE,(X)
        ADD HL,DE
        LD A,(MODE)             ; x 4 / 2 / 1 pour le firmware (640 points de large)
        LD B,A
        LD A,2
        SUB B
        JR Z,LX2
        LD B,A
LX1:    ADD HL,HL
        DJNZ LX1
LX2:    EX DE,HL
        LD HL,(LY)

        LD A,(K)
        OR A
        JR NZ,TRAIT
        LD (PREMX),DE           ; premier sommet : on s'y place
        LD (PREMY),HL
        CALL &BBC0              ; GRA MOVE ABSOLUTE
        JR SUIVANT
TRAIT:  CALL &BBF6              ; GRA LINE ABSOLUTE

SUIVANT: LD HL,(TETA)           ; angle suivant : + 18000 / pointes ; le reste est gardé (pas d'erreur qui s'ajoute)
        LD DE,(PAS)
        ADD HL,DE
        LD A,(RESTE)
        LD B,A
        LD A,(ERREUR)
        ADD A,B
        LD B,A
        LD A,(NB)
        LD C,A
        LD A,B
        CP C
        JR C,SUIV1
        SUB C
        INC HL
SUIV1:  LD (ERREUR),A
        LD DE,36000             ; un tour de plus : on le retire
        OR A
        SBC HL,DE
        JR NC,SUIV2
        ADD HL,DE
SUIV2:  LD (TETA),HL
        LD A,(K)
        INC A
        LD (K),A
        LD B,A
        LD A,(NB)
        ADD A,A
        CP B
        JP NZ,SOMMET

        LD DE,(PREMX)           ; on referme sur le premier sommet
        LD HL,(PREMY)
        CALL &BBF6
        LD DE,(ORIX)            ; origine, position et stylo remis comme avant
        LD HL,(ORIY)
        CALL &BBC9
        LD DE,(CURX)
        LD HL,(CURY)
        CALL &BBC0
        LD A,(ANCIEN)
        CALL &BBDE
        RET

; Retenue si HL (sans signe) vaut au plus 1000
RAYONOK: LD DE,1001
        OR A
        SBC HL,DE
        RET

; HL / C (C de 1 à 127) : quotient dans HL, reste dans A
DIV8:   XOR A
        LD B,16
DIV81:  ADD HL,HL
        RLA
        CP C
        JR C,DIV82
        SUB C
        INC L
DIV82:  DJNZ DIV81
        RET

; HL x DE (sans signe) : résultat sur 32 bits dans DE (poids fort) et HL (poids faible)
MUL16:  LD B,H
        LD C,L
        LD HL,0
        LD A,16
MUL1:   ADD HL,HL
        EX DE,HL
        ADC HL,HL
        EX DE,HL
        JR NC,MUL2
        ADD HL,BC
        JR NC,MUL2
        INC DE
MUL2:   DEC A
        JR NZ,MUL1
        RET

; HL = -HL
NEGHL:  XOR A
        SUB L
        LD L,A
        SBC A,A
        SUB H
        LD H,A
        RET

; Sinus : HL = angle en centièmes de degré (positif) ; rend HL = sinus x 16384 (table par degré, centièmes entre deux)
SINUS:  LD DE,36000
SIN1:   OR A
        SBC HL,DE
        JR NC,SIN1
        ADD HL,DE
        LD C,0
        LD DE,18000
        OR A
        SBC HL,DE
        JR NC,SIN2
        ADD HL,DE
        JR SIN3
SIN2:   LD C,1
SIN3:   PUSH BC
        PUSH HL
        LD DE,9001
        OR A
        SBC HL,DE
        POP HL
        JR C,SIN4
        EX DE,HL
        LD HL,18000
        OR A
        SBC HL,DE
SIN4:   LD C,100
        CALL DIV8
        PUSH AF
        ADD HL,HL
        LD DE,TABLE
        ADD HL,DE
        LD E,(HL)
        INC HL
        LD D,(HL)
        INC HL
        LD A,(HL)
        INC HL
        LD H,(HL)
        LD L,A
        OR A
        SBC HL,DE
        POP AF
        PUSH DE
        PUSH HL
        LD L,A                  ; centièmes x 655 (= centièmes / 100 x 65536)
        LD H,0
        LD DE,655
        CALL MUL16
        POP DE
        CALL MUL16              ; DE = écart x centièmes / 100
        POP HL
        ADD HL,DE
        POP BC
        DEC C
        RET NZ
        JP NEGHL

; HL = valeur signée, DE = rayon, A = décalage : HL = arrondi(rayon x |valeur| / 2 puissance décalage), avec le signe
ECART:  LD (DECAL),A
        LD A,H
        LD (SIGNE),A
        BIT 7,H
        CALL NZ,NEGHL
        CALL MUL16
        LD A,(DECAL)
        DEC A
        LD B,A
ECA1:   SRL D
        RR E
        RR H
        RR L
        DJNZ ECA1
        INC HL
        SRL H
        RR L
        LD A,(SIGNE)
        BIT 7,A
        RET Z
        JP NEGHL

; HL = HL x 69 / 64 (signé, arrondi vers 0)
FOIS69: LD A,H
        PUSH AF
        BIT 7,H
        CALL NZ,NEGHL
        LD DE,69
        CALL MUL16
        LD B,6
F691:   SRL D
        RR E
        RR H
        RR L
        DJNZ F691
        POP AF
        BIT 7,A
        RET Z
        JP NEGHL

; Sinus de 0 à 90 degrés x 16384 (et une case de plus, jamais utilisée pour le calcul)
TABLE:  DEFW 0,286,572,857,1143,1428,1713,1997,2280,2563
        DEFW 2845,3126,3406,3686,3964,4240,4516,4790,5063,5334
        DEFW 5604,5872,6138,6402,6664,6924,7182,7438,7692,7943
        DEFW 8192,8438,8682,8923,9162,9397,9630,9860,10087,10311
        DEFW 10531,10749,10963,11174,11381,11585,11786,11982,12176,12365
        DEFW 12551,12733,12911,13085,13255,13421,13583,13741,13894,14044
        DEFW 14189,14330,14466,14598,14726,14849,14968,15082,15191,15296
        DEFW 15396,15491,15582,15668,15749,15826,15897,15964,16026,16083
        DEFW 16135,16182,16225,16262,16294,16322,16344,16362,16374,16382
        DEFW 16384,16384

; Cases de travail
NB:     DEFB 0
MODE:   DEFB 0
RESTE:  DEFB 0
ERREUR: DEFB 0
K:      DEFB 0
ANCIEN: DEFB 0
DECAL:  DEFB 0
SIGNE:  DEFB 0
PAS:    DEFW 0
TETA:   DEFW 0
RAYON:  DEFW 0
LY:     DEFW 0
PREMX:  DEFW 0
PREMY:  DEFW 0
CURX:   DEFW 0
CURY:   DEFW 0
ORIX:   DEFW 0
ORIY:   DEFW 0
