;@NOM STAR
;@GENRE COMMANDE
;@VALEUR X, -2000, 2000, position horizontale du centre (repère natif, comme CIRCLE)
;@VALEUR Y, -2000, 2000, position verticale du centre (0 en haut, 199 en bas)
;@VALEUR R1, 0, 1000, rayon interne : du centre au creux entre deux pointes (en lignes)
;@VALEUR R2, 0, 1000, rayon externe : du centre au bout des pointes (en lignes)
;@VALEUR POINTES, 2, 32, nombre de pointes
;@OPTION STYLO, 0, 15, -1, couleur du contour (sautée : stylo du tracé)
;@OPTION REMPLISSAGE, 0, 15, -1, couleur de l'intérieur (sautée : pas d'intérieur)
;@OPTION ANGLE, -32768, 32767, 0, rotation en degrés (0 : une pointe en haut ; positif : vers la gauche)
;@AIDE Dessine une étoile : STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle].
;@AIDE Avec un rayon interne assez grand, on obtient un polygone (R1 = R2 x cosinus(180 / pointes) : 6 pointes et R1 = R2 x 0,866 donnent un hexagone).
;@AIDE Même repère et même largeur que CIRCLE (vrai moniteur Amstrad). Le stylo et la position du tracé ne changent pas.
;@AUTEUR CPC BASIC IDE (D150, D151, D152)

; Sommet k (de 0 à 2 x pointes - 1) : rayon externe si k est pair, interne s'il est impair,
; angle (en centièmes de degré) = 9000 + 100 x ANGLE + k x 18000 / pointes.
; Écart vertical = arrondi(rayon x sinus) en lignes ; écart horizontal = arrondi(rayon x cosinus x 69/64 x 0,5 / 1 / 2 selon le MODE).
; Les sommets sont d'abord tous calculés (repère natif). Intérieur : ligne par ligne, on cherche où la ligne croise
; les bords et on remplit entre deux croisements. Puis le contour par-dessus. Traits et lignes tracés par le firmware
; (GRA LINE ABSOLUTE), avec l'origine remise un moment en 0,0.

        LD HL,(POINTES)         ; valeurs calculées hors limites : rien n'est dessiné
        LD A,H
        OR A
        RET NZ
        LD A,L
        CP 2
        RET C
        CP 33
        RET NC
        LD (NB),A
        ADD A,A
        LD (NB2),A
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

; ----- Calcul des sommets

SOMMET: LD A,(K)                ; rayon : externe (pointe) si k est pair, interne (creux) sinon
        RRA
        LD HL,(R2)
        JR NC,SOM1
        LD HL,(R1)
SOM1:   LD (RAYON),HL

        LD HL,(TETA)            ; vertical : y = Y - arrondi(rayon x sinus)
        CALL SINUS
        LD DE,(RAYON)
        LD A,14
        CALL ECART
        EX DE,HL
        LD HL,(Y)
        OR A
        SBC HL,DE
        EX DE,HL
        LD A,(K)
        PUSH DE
        LD DE,VY
        CALL INDEX
        POP DE
        LD (HL),E
        INC HL
        LD (HL),D

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
        EX DE,HL
        LD A,(K)
        PUSH DE
        LD DE,VX
        CALL INDEX
        POP DE
        LD (HL),E
        INC HL
        LD (HL),D

        LD HL,(TETA)            ; angle suivant : + 18000 / pointes ; le reste est gardé (pas d'erreur qui s'ajoute)
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
        LD A,(NB2)
        CP B
        JP NZ,SOMMET

; ----- Dessin

        CALL &BBE1              ; GRA GET PEN : gardé pour la fin
        LD (ANCIEN),A
        CALL &BBC6              ; GRA ASK CURSOR : position gardée
        LD (CURX),DE
        LD (CURY),HL
        CALL &BBCC              ; GRA GET ORIGIN : origine gardée
        LD (ORIX),DE
        LD (ORIY),HL
        LD DE,0
        LD HL,0
        CALL &BBC9              ; GRA SET ORIGIN 0,0

        LD HL,(REMPLISSAGE)     ; intérieur d'abord (sauté : -1, pas d'intérieur)
        BIT 7,H
        JR NZ,CONTOUR
        LD A,L
        CALL &BBDE              ; GRA SET PEN
        CALL REMPLIR

CONTOUR: LD HL,(STYLO)          ; contour par-dessus (stylo sauté : celui du tracé)
        LD A,(ANCIEN)
        BIT 7,H
        JR NZ,CONT0
        LD A,L
CONT0:  CALL &BBDE
        XOR A
        LD (K),A
CONT1:  LD A,(K)
        CALL LIREV
        CALL LOGIQ
        LD A,(K)
        OR A
        JR NZ,CONT2
        CALL &BBC0              ; premier sommet : GRA MOVE ABSOLUTE
        JR CONT3
CONT2:  CALL &BBF6              ; GRA LINE ABSOLUTE
CONT3:  LD A,(K)
        INC A
        LD (K),A
        LD B,A
        LD A,(NB2)
        CP B
        JR NZ,CONT1
        XOR A                   ; on referme sur le premier sommet
        CALL LIREV
        CALL LOGIQ
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

; ----- Intérieur : lignes de l'écran entre le plus haut et le plus bas sommet (de 0 à 199)

REMPLIR: LD HL,(VY)
        LD (YMIN),HL
        LD (YMAX),HL
        LD A,1
YMM:    LD (K),A
        LD DE,VY
        CALL INDEX
        LD E,(HL)
        INC HL
        LD D,(HL)
        PUSH DE
        EX DE,HL
        LD DE,(YMIN)
        CALL CMPS               ; retenue si y < ymin
        POP DE
        JR NC,YMM1
        LD (YMIN),DE
YMM1:   PUSH DE
        LD HL,(YMAX)
        CALL CMPS               ; retenue si ymax < y
        POP DE
        JR NC,YMM2
        LD (YMAX),DE
YMM2:   LD A,(K)
        INC A
        LD B,A
        LD A,(NB2)
        CP B
        LD A,B
        JR NZ,YMM

        LD HL,(YMAX)            ; tout au-dessus de l'écran : rien
        BIT 7,H
        RET NZ
        LD HL,(YMIN)
        BIT 7,H
        JR Z,CLIP1
        LD HL,0
        LD (YMIN),HL
CLIP1:  LD DE,200               ; tout au-dessous de l'écran : rien
        OR A
        SBC HL,DE
        RET NC
        LD HL,(YMAX)
        OR A
        SBC HL,DE
        JR C,CLIP2
        LD HL,199
        LD (YMAX),HL
CLIP2:  LD HL,(YMIN)
        LD (LIGNE),HL

LIG:    XOR A                   ; une ligne : croisements avec chaque bord, rangés du plus à gauche au plus à droite
        LD (CNT),A
        LD (ARETE),A
ARE:    LD A,(ARETE)            ; bord du sommet a au sommet b = a + 1 (le dernier revient au premier)
        CALL LIREV
        LD (X1),DE
        LD (Y1),HL
        LD A,(ARETE)
        INC A
        LD B,A
        LD A,(NB2)
        CP B
        LD A,B
        JR NZ,ARE1
        XOR A
ARE1:   CALL LIREV
        LD (X2),DE
        LD (Y2),HL
        LD DE,(Y1)
        OR A
        SBC HL,DE
        JP Z,AREN               ; bord à plat : sauté
        BIT 7,H
        JR Z,ARE2
        LD HL,(X1)              ; on le prend toujours de haut en bas
        LD DE,(X2)
        LD (X1),DE
        LD (X2),HL
        LD HL,(Y1)
        LD DE,(Y2)
        LD (Y1),DE
        LD (Y2),HL
ARE2:   LD HL,(LIGNE)           ; la ligne croise ce bord si y1 <= ligne < y2
        LD DE,(Y1)
        CALL CMPS
        JP C,AREN
        LD HL,(LIGNE)
        LD DE,(Y2)
        CALL CMPS
        JP NC,AREN

        LD HL,(Y2)              ; x = x1 + (ligne - y1) x (x2 - x1) / (y2 - y1)
        LD DE,(Y1)
        OR A
        SBC HL,DE
        LD (DEN),HL
        LD HL,(X2)
        LD DE,(X1)
        OR A
        SBC HL,DE
        LD A,H
        LD (SIGNE),A
        BIT 7,H
        CALL NZ,NEGHL
        PUSH HL
        LD HL,(LIGNE)
        LD DE,(Y1)
        OR A
        SBC HL,DE
        POP DE
        CALL MUL16
        LD BC,(DEN)
        CALL DIV32              ; HL = quotient, DE = reste
        LD A,D
        OR E
        JR Z,ARE3
        LD A,1
ARE3:   LD (INEX),A             ; 1 : x n'est pas un nombre entier
        LD A,(SIGNE)
        BIT 7,A
        JR Z,ARE4
        CALL NEGHL
ARE4:   LD DE,(X1)              ; clé = 2 x partie entière + (1 si x n'est pas entier)
        ADD HL,DE
        ADD HL,HL
        LD A,(INEX)
        OR A
        JR Z,ARE6
        LD A,(SIGNE)
        BIT 7,A
        JR NZ,ARE5
        INC HL
        JR ARE6
ARE5:   DEC HL
ARE6:   CALL INSERE

AREN:   LD A,(ARETE)
        INC A
        LD (ARETE),A
        LD B,A
        LD A,(NB2)
        CP B
        JP NZ,ARE

        CALL MORCEAUX           ; remplit entre le 1er et le 2e croisement, le 3e et le 4e...
        LD HL,(LIGNE)
        LD DE,(YMAX)
        OR A
        SBC HL,DE
        RET Z
        LD HL,(LIGNE)
        INC HL
        LD (LIGNE),HL
        JP LIG

; Range la clé HL dans la liste CLES, gardée en ordre croissant
INSERE: LD (CLE),HL
        LD A,(CNT)
        LD (POS),A
INS1:   LD A,(POS)
        OR A
        JR Z,INS2
        DEC A
        LD DE,CLES
        CALL INDEX
        LD E,(HL)
        INC HL
        LD D,(HL)
        PUSH DE
        LD HL,(CLE)
        CALL CMPS               ; retenue si clé < la précédente : on décale celle-ci
        POP DE
        JR NC,INS2
        LD A,(POS)
        PUSH DE
        LD DE,CLES
        CALL INDEX
        POP DE
        LD (HL),E
        INC HL
        LD (HL),D
        LD A,(POS)
        DEC A
        LD (POS),A
        JR INS1
INS2:   LD A,(POS)
        LD DE,CLES
        CALL INDEX
        LD DE,(CLE)
        LD (HL),E
        INC HL
        LD (HL),D
        LD A,(CNT)
        INC A
        LD (CNT),A
        RET

; Morceaux de la ligne : du point entier à droite du croisement 1 au point entier à gauche du croisement 2, etc.
MORCEAUX: XOR A
        LD (POS),A
MOR1:   LD A,(POS)
        INC A
        LD B,A
        LD A,(CNT)
        CP B
        RET C
        RET Z
        LD A,(POS)
        LD DE,CLES
        CALL INDEX
        LD E,(HL)
        INC HL
        LD D,(HL)
        INC HL
        LD A,(HL)
        INC HL
        LD H,(HL)
        LD L,A
        SRA H                   ; droite : partie entière
        RR L
        LD (DROITE),HL
        EX DE,HL
        INC HL                  ; gauche : arrondi au-dessus
        SRA H
        RR L
        LD (GAUCHE),HL
        EX DE,HL
        LD HL,(DROITE)
        CALL CMPS               ; retenue si droite < gauche : rien à remplir
        JR C,MOR2
        LD DE,(GAUCHE)
        LD HL,(LIGNE)
        CALL LOGIQ
        CALL &BBC0
        LD DE,(DROITE)
        LD HL,(LIGNE)
        CALL LOGIQ
        CALL &BBF6
MOR2:   LD A,(POS)
        ADD A,2
        LD (POS),A
        JR MOR1

; ----- Petites routines

; Retenue si HL (sans signe) vaut au plus 1000
RAYONOK: LD DE,1001
        OR A
        SBC HL,DE
        RET

; HL = DE + 2 x A
INDEX:  LD L,A
        LD H,0
        ADD HL,HL
        ADD HL,DE
        RET

; Sommet A : DE = x, HL = y (repère natif)
LIREV:  PUSH AF
        LD DE,VY
        CALL INDEX
        LD E,(HL)
        INC HL
        LD D,(HL)
        POP AF
        PUSH DE
        LD DE,VX
        CALL INDEX
        LD E,(HL)
        INC HL
        LD D,(HL)
        POP HL
        RET

; DE = x, HL = y (repère natif) : DE = x, HL = y pour le firmware (x 4 / 2 / 1 selon le MODE, (199 - y) x 2)
LOGIQ:  PUSH HL
        EX DE,HL
        LD A,(MODE)
        LD B,A
        LD A,2
        SUB B
        JR Z,LOG2
        LD B,A
LOG1:   ADD HL,HL
        DJNZ LOG1
LOG2:   EX DE,HL
        POP BC
        LD HL,199
        OR A
        SBC HL,BC
        ADD HL,HL
        RET

; Comparaison avec signe : retenue si HL < DE, zéro s'ils sont égaux (HL et DE sont abîmés)
CMPS:   LD A,H
        XOR &80
        LD H,A
        LD A,D
        XOR &80
        LD D,A
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

; DE:HL / BC (DE plus petit que BC) : quotient dans HL, reste dans DE
DIV32:  LD A,16
D321:   ADD HL,HL
        EX DE,HL
        ADC HL,HL
        JR C,D322
        OR A
        SBC HL,BC
        JR NC,D323
        ADD HL,BC
        EX DE,HL
        JR D324
D322:   OR A
        SBC HL,BC
D323:   EX DE,HL
        INC L
D324:   DEC A
        JR NZ,D321
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
NB2:    DEFB 0
MODE:   DEFB 0
RESTE:  DEFB 0
ERREUR: DEFB 0
K:      DEFB 0
ANCIEN: DEFB 0
DECAL:  DEFB 0
SIGNE:  DEFB 0
CNT:    DEFB 0
ARETE:  DEFB 0
POS:    DEFB 0
INEX:   DEFB 0
PAS:    DEFW 0
TETA:   DEFW 0
RAYON:  DEFW 0
CURX:   DEFW 0
CURY:   DEFW 0
ORIX:   DEFW 0
ORIY:   DEFW 0
YMIN:   DEFW 0
YMAX:   DEFW 0
LIGNE:  DEFW 0
X1:     DEFW 0
Y1:     DEFW 0
X2:     DEFW 0
Y2:     DEFW 0
DEN:    DEFW 0
CLE:    DEFW 0
GAUCHE: DEFW 0
DROITE: DEFW 0
VX:     DEFS 128
VY:     DEFS 128
CLES:   DEFS 128
