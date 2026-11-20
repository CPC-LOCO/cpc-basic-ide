;@NOM TYPO
;@GENRE COMMANDE
;@TEXTE MESSAGE, le texte à écrire
;@OPTION X, 0, 639, -1, position horizontale, comme MOVE (sautée : là où est le curseur graphique)
;@OPTION Y, 0, 399, -1, position verticale, comme MOVE (sautée : là où est le curseur graphique)
;@AIDE Écrit un texte en petits caractères de 4 points sur 8 : deux petits caractères par caractère du CPC.
;@AIDE 40 colonnes en MODE 0, 80 en MODE 1, 160 en MODE 2. TYPO texte [, x, y] : au curseur graphique, ou en x, y.
;@AIDE Couleurs : GRAPHICS PEN et GRAPHICS PAPER. Un nombre impair de caractères est complété par un espace.
;@AIDE Caractères : de l'espace à z, avec à é ù (version française du CPC) ; les autres deviennent des espaces.
;@AIDE Le dessin du caractère 255 est emprunté le temps d'écrire, puis remis comme avant. DEFTYPO change un petit caractère.
;@AUTEUR RetroPoke (Philippe Moulin, RSX TYPO de 2020), adaptée par CPC BASIC IDE (D170)

; Chaque petit caractère tient en 3 octets (TYPO_FONTE) : 6 lignes de 4 points, deux lignes par octet (poids fort,
; puis poids faible). Pour deux petits caractères, chaque ligne prend le poids fort du 1er (moitié gauche) et le poids
; faible du 2e (moitié droite) ; les lignes 1 et 8 restent vides. Le caractère 255 reçoit ce dessin et GRA WR CHAR
; l'écrit au curseur graphique : cela marche dans les 3 MODE et sur tous les CPC.

        LD A,255
        CALL &BBA5              ; TXT GET MATRIX : HL = dessin de 255 (retenue : redessinable)
        JR C,MATRICE
        LD DE,255               ; 255 n'est pas redessinable : une petite table à nous pour lui seul
        LD HL,TABLE
        CALL &BBAB              ; TXT SET M TABLE
        LD A,255
        CALL &BBA5
MATRICE:
        LD (MAT),HL
        LD DE,GARDE
        LD BC,8
        LDIR                    ; le dessin de 255 est gardé, pour le remettre à la fin
        LD HL,(MAT)
        LD (HL),0               ; ligne 1 vide
        LD DE,7
        ADD HL,DE
        LD (HL),0               ; ligne 8 vide

        LD HL,(X)               ; X et Y sautées (-1 toutes les deux) : on écrit au curseur graphique
        LD DE,(Y)
        LD A,H
        AND D
        INC A
        JR Z,ECRIRE
        CALL &BBC6              ; GRA ASK CURSOR : DE = x, HL = y (pour celle qui est sautée)
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

ECRIRE: LD HL,(MESSAGE)         ; fiche du texte : longueur, puis adresse des lettres
        LD A,(HL)
        OR A
        JR Z,FINI
        LD (COMPTE),A
        INC HL
        LD E,(HL)
        INC HL
        LD D,(HL)
        LD (LETTRE),DE

PAIRE:  LD HL,(LETTRE)          ; deux lettres
        LD A,(HL)
        INC HL
        LD C,(HL)
        INC HL
        LD (LETTRE),HL
        PUSH BC
        CALL TYPO_ADRESSE       ; HL = dessin de la 1re
        EX DE,HL                ; DE = dessin de la 1re
        POP BC
        LD A,(COMPTE)
        DEC A
        JR NZ,DEUX
        LD C,32                 ; nombre impair : un espace à droite
        INC A
DEUX:   DEC A
        LD (COMPTE),A
        LD A,C
        CALL TYPO_ADRESSE       ; HL = dessin de la 2e (DE est gardé)
        LD B,H
        LD C,L                  ; BC = dessin de la 2e
        LD HL,(MAT)
        INC HL                  ; lignes 2 à 7 du caractère 255
        CALL UNIR
        CALL UNIR
        CALL UNIR
        LD A,255
        CALL &BBFC              ; GRA WR CHAR : écrit 255 au curseur graphique, qui avance
        LD A,(COMPTE)
        OR A
        JR NZ,PAIRE

FINI:   LD HL,GARDE             ; le dessin de 255 remis comme avant
        LD DE,(MAT)
        LD BC,8
        LDIR
        RET

; un octet de chaque dessin donne deux lignes : DE = 1er, BC = 2e, HL = ligne du caractère 255
UNIR:   LD A,(BC)               ; poids fort du 2e -> moitié droite
        RRCA
        RRCA
        RRCA
        RRCA
        AND &0F
        LD (HL),A
        LD A,(DE)               ; poids fort du 1er -> moitié gauche
        AND &F0
        OR (HL)
        LD (HL),A
        INC HL
        LD A,(BC)               ; poids faible du 2e -> moitié droite
        AND &0F
        LD (HL),A
        LD A,(DE)               ; poids faible du 1er -> moitié gauche
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

MAT:    DS 2                    ; adresse du dessin de 255
LETTRE: DS 2                    ; prochaine lettre du texte
COMPTE: DS 1                    ; lettres qui restent
GARDE:  DS 8                    ; dessin de 255 d'avant
TABLE:  DS 8                    ; table pour 255 quand il n'est pas redessinable
