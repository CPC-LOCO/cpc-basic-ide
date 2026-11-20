;@NOM DEFTYPO
;@GENRE COMMANDE
;@TEXTE CARACTERE, le petit caractère à redessiner (le premier du texte)
;@VALEUR L1, 0, 255, lignes 1 et 2 du dessin
;@VALEUR L2, 0, 255, lignes 3 et 4 du dessin
;@VALEUR L3, 0, 255, lignes 5 et 6 du dessin
;@AIDE Redessine un petit caractère de TYPO, comme SYMBOL : DEFTYPO "x", ligne1-2, ligne3-4, ligne5-6.
;@AIDE Un petit caractère a 6 lignes de 4 points ; chaque nombre en donne deux : le premier chiffre de &XY pour la ligne du haut, le second pour celle du dessous.
;@AIDE Dans un chiffre, &8 est le point de gauche, &1 celui de droite. Exemple, un coeur : DEFTYPO "#", &AE, &E4, &00.
;@AIDE Caractères possibles : de l'espace à z, avec à é ù ; un autre est ignoré. Le nouveau dessin reste jusqu'à la fin du programme.
;@AUTEUR RetroPoke (Philippe Moulin, RSX DEFTYPO de 2020), adaptée par CPC BASIC IDE (D171)

        LD HL,(CARACTERE)       ; fiche du texte : longueur, puis adresse des lettres
        LD A,(HL)
        OR A
        RET Z                   ; texte vide : rien
        INC HL
        LD E,(HL)
        INC HL
        LD D,(HL)
        LD A,(DE)               ; le premier caractère
        LD C,A
        SUB 32
        CP 93
        RET NC                  ; pas dans la fonte de TYPO : rien
        LD A,C
        CALL TYPO_ADRESSE       ; HL = ses 3 octets dans TYPO_FONTE
        LD A,(L1)
        LD (HL),A
        INC HL
        LD A,(L2)
        LD (HL),A
        INC HL
        LD A,(L3)
        LD (HL),A
        RET
