# CPC BASIC IDE 0.6.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.5.0

**Assembleur seul** (menu Assembleur)
- Une fenêtre pour écrire un programme entièrement en assembleur Z80, à la façon de WinAPE, enregistré en `.asm` : couleurs, F5 compile vers une disquette `.dsk`, Maj+F5 vérifie, la liste des fautes amène à la ligne.
- `ORG` obligatoire au début (de &0400 à &A67B), `RUN` pour l'adresse de départ ; les routines des bibliothèques s'appellent par leur nom.
- Case « Avec chargeur BASIC » : décochée, la disquette n'a que `NOM.BIN`, lancé directement par `RUN"NOM"`.

**Programmes BASIC sans chargeur**
- Menu Options, « Chargeur BASIC sur la disquette (NOM.BAS) » : décoché, F5 ne met que `NOM.BIN`, et `RUN"NOM"` le lance directement. Le programme rebranche lui-même la disquette au départ : fichiers et `RUN"nom"` marchent comme avant (vérifié dans WinAPE).

**Mémoire réservée au programmeur**
- De &0400 à &2FFF (11 Ko), le compilateur ne met jamais rien, ni aujourd'hui ni plus tard : pour vos `POKE`, vos données (`LOAD"DONNEES",&1000`) ou vos routines. De &3000 à &3FFF : libre aussi, sauf avec les fichiers ou `RUN"nom"`.

**Bibliothèques**
- Une valeur peut être un **texte** (`;@TEXTE` dans le fichier, case « texte » dans le questionnaire) : `TYPO "BONJOUR"`, `TYPO A$`...
- La fenêtre des bibliothèques montre la **place** que prend chaque commande dans un programme.
- Nouvelle bibliothèque **TEXTE**, d'après les RSX de RetroPoke : `TYPO texte [, x, y]` écrit en petits caractères de 4 points sur 8 (40 colonnes en MODE 0, 80 en MODE 1, 160 en MODE 2), directement dans l'écran (2,6 à 6,4 fois plus vite que par le firmware) ; `DEFTYPO "x", l1, l2, l3` redessine un petit caractère. Exemple : `typo.bas`.

**Documentation**
- `LISEZMOI.txt` : l'assembleur seul (point 7), le lancement sans chargeur, la mémoire réservée, l'astuce `CREAL(A)/B` pour une division à virgule sans le réglage F8.

## Pas encore là
Sortie cassette, cartouche GX4000, sprites.

## Somme de contrôle
SHA256 de `CpcBasic-0.6.0-win-x64.zip` : `25cefaff6132f91416253f66db2d694a1fa043ac3d05fd1b9ffdbbb3e01e055e`
