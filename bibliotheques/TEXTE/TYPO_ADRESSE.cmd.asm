;@NOM TYPO_ADRESSE
;@GENRE ROUTINE
;@AIDE A = code d'un caractère -> HL = adresse de ses 3 octets dans TYPO_FONTE (caractère inconnu : l'espace).
;@AIDE Modifie A et BC ; DE est gardé. Sert à TYPO et à DEFTYPO.
;@AUTEUR RetroPoke (RSX TYPO de 2020), calcul direct par CPC BASIC IDE (D170)

; adresse = TYPO_FONTE + 3 x (code - 32), sans boucle
        SUB 32
        CP 93
        JR C,CONNU
        XOR A                   ; avant l'espace ou après ù : l'espace
CONNU:  LD C,A
        LD B,0
        LD H,B
        LD L,C
        ADD HL,HL
        ADD HL,BC
        LD BC,TYPO_FONTE
        ADD HL,BC
        RET
