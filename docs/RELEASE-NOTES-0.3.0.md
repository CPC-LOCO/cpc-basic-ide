# CPC BASIC IDE 0.3.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.2.0

**Le langage : presque tout le Locomotive BASIC est compilé**
- Textes (chaînes), nombres à virgule (calculs faits par la ROM du CPC, mêmes résultats que le vrai BASIC), tableaux, DATA / READ, DEF FN, PRINT USING.
- Écran et graphismes : MODE, WINDOW, PEN, PAPER, INK, PLOT, DRAW, ORIGIN, TEST, et le BASIC 1.1 (FILL, MASK, GRAPHICS PEN / PAPER, FRAME, CURSOR, COPYCHR$, DEC$), qui marche aussi sur le 464.
- Sons (SOUND, ENV, ENT), clavier (INKEY$, INPUT, KEY), joystick.
- Fichiers sur disquette (OPENOUT, OPENIN, SAVE, LOAD, CAT), imprimante (PRINT #8), RUN "nom" vers un autre programme compilé, mis automatiquement sur la même disquette.
- Interruptions (EVERY, AFTER, ON SQ, ON BREAK), erreurs (ON ERROR GOTO, RESUME, ERR, ERL), TRON / TROFF.
- Un programme compilé marche sur tous les CPC (464, 664, 6128).
- Ajouts du projet : PAINT (colorie la zone d'une encre), BOX, BYTE, DO … LOOP, REPEAT … UNTIL, IF … END IF sur plusieurs lignes.

**L'éditeur**
- Barre de menus Fichier, Édition, Compiler, Options, Aide (touche Alt ; F1 : raccourcis).
- Commandes du CPC tapées sans numéro puis Entrée : NEW, AUTO, EDIT, RENUM (les GOTO, GOSUB… suivent), DELETE, LIST ; un numéro seul efface la ligne (avec une question). Ctrl+Z annule.
- Messages avec ✗ et ✓, panneau réglable, barre d'état lisible, lignes longues affichées sur plusieurs lignes comme le CPC.
- Réglages : F8 (variables à virgule comme le CPC), F9 (touche ESC), F11 (texte du programme sur la disquette pour comparer avec le vrai BASIC).
- Un problème imprévu ne ferme plus l'éditeur ; le détail est noté dans `%APPDATA%\CpcBasicIde\erreurs.txt`.

## Pas encore là
Sortie cassette, cartouche GX4000, sprites. Les lettres accentuées s'affichent « ? ».

## Somme de contrôle
SHA256 de `CpcBasic-0.3.0-win-x64.zip` : `8786eb637915be912950f1c270eb214e9bb1ba15095484d78fc14453ca2b1bcb`
