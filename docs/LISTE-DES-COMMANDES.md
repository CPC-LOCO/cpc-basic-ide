# Liste des commandes (22 nov. 2026, après la version 0.7.0 et les sprites)

Toutes les commandes et fonctions que le compilateur accepte aujourd'hui, avec ce qu'elles deviennent :

- **CPC** : programme compilé avec F5, en disquette (.dsk) ou en cassette (.cdt), pour le 464, le 664 et le 6128 ;
- **Cartouche** : programme compilé avec Ctrl+F5 pour la console GX4000 (.cpr), avec notre propre petit système (aucune ROM d'Amstrad).

Légende : **Oui** = marche ; **Manette** = marche, avec la manette à la place du clavier ; **Non** = accepté par le compilateur, mais sans effet sur la console (elle n'a ni clavier, ni lecteur de disquette, ni imprimante).
Les mots marqués ★ sont des ajouts du projet (ils n'existent pas dans le Locomotive BASIC).

## 1. Déroulement du programme

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `GOTO`, `GOSUB`, `RETURN` | aller à une ligne, sous-programme | Oui | Oui |
| `ON … GOTO`, `ON … GOSUB` | aller à une ligne selon une valeur | Oui | Oui |
| `IF … THEN … ELSE` | condition | Oui | Oui |
| `END IF` / `ENDIF` ★ | fin d'un IF sur plusieurs lignes | Oui | Oui |
| `SELECT`, `CASE`, `CASE ELSE`, `END SELECT` ★ | choisir entre plusieurs cas | Oui | Oui |
| `FOR … TO … STEP … NEXT` | boucle comptée | Oui | Oui |
| `WHILE … WEND` | boucle tant que | Oui | Oui |
| `REPEAT … UNTIL` ★ | boucle jusqu'à | Oui | Oui |
| `DO … LOOP` ★ | boucle sans fin ou avec condition | Oui | Oui |
| `EXIT`, `CONTINUE` ★ | sortir d'une boucle, passer au tour suivant | Oui | Oui |
| `END`, `STOP` | fin du programme | Oui | Oui |
| `ON ERROR GOTO`, `RESUME`, `ERROR`, `ERR`, `ERL`, `DERR` | erreurs | Oui | Oui (DERR : pas de disquette) |
| `ON BREAK GOSUB / CONT / STOP` | touche ESC | Oui | Oui |
| `TRON`, `TROFF` | suivre les numéros de ligne | Oui | Oui |
| `DEBUG` ★ | montrer les variables (mise au point, F2) | Oui | Oui |
| `CALL` | lancer une routine en langage machine | Oui | Oui pour vos routines ; les adresses de la ROM du CPC sont presque toutes sans effet |
| `RUN`, `RUN ligne` | recommencer le programme (au début ou à une ligne) | Oui | Oui |
| `RUN "nom"`, `CHAIN`, `CHAIN MERGE`, `MERGE` | lancer un autre programme | Oui | Non (prévu : chercher le programme dans la cartouche, voir IDEES.md) |

## 2. Variables, données et mémoire

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `LET`, `DIM`, `ERASE` | variables et tableaux | Oui | Oui |
| `BYTE` ★ | variable sur un octet | Oui | Oui |
| `DEFINT`, `DEFREAL`, `DEFSTR` | genre des variables selon la lettre | Oui | Oui |
| `DEF FN`, `FN` | fonction écrite par vous | Oui | Oui |
| `DATA`, `READ`, `RESTORE` | données dans le programme | Oui | Oui |
| `PEEK`, `POKE` | lire, écrire la mémoire | Oui | Oui (la mémoire n'est pas rangée pareil) |
| `INP`, `OUT`, `WAIT` | ports de la machine | Oui | Oui |
| `MEMORY`, `HIMEM`, `FRE`, `CLEAR` | mémoire | Oui | Oui |

## 3. Texte et fenêtres

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `PRINT`, `PRINT USING`, `?` | écrire | Oui | Oui |
| `WRITE` | écrire avec des virgules et des guillemets | Oui | Oui |
| `LOCATE`, `CLS` | placer le curseur, effacer | Oui | Oui |
| `MODE` | MODE 0, 1 ou 2 | Oui | Oui |
| `PEN`, `PAPER` | stylo et papier du texte | Oui | Oui |
| `INK`, `BORDER`, `SPEED INK` | couleurs, aussi clignotantes | Oui | Oui |
| `WINDOW`, `WINDOW SWAP` | fenêtres de texte, flux #0 à #7 | Oui | Oui |
| `POS`, `VPOS`, `TAB`, `SPC`, `ZONE` | position et mise en page | Oui | Oui |
| `TAG`, `TAGOFF` | texte au curseur graphique | Oui | Non |
| `SYMBOL`, `SYMBOL AFTER` | redessiner des caractères | Oui | Non |
| `CURSOR` | montrer ou cacher le curseur | Oui | Non |
| `COPYCHR$` | lire un caractère de l'écran | Oui | Non |
| `PRINT #8`, `WIDTH` | imprimante | Oui | Non |

## 4. Dessin

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `PLOT`, `PLOTR` | un point | Oui | Oui |
| `DRAW`, `DRAWR` | un trait | Oui | Oui |
| `MOVE`, `MOVER`, `ORIGIN` | curseur graphique, origine | Oui | Oui |
| `XPOS`, `YPOS`, `TEST`, `TESTR` | position, couleur d'un point | Oui | Oui |
| `CLG`, `GRAPHICS PEN`, `GRAPHICS PAPER` | effacer, stylos du dessin | Oui | Oui |
| `MASK`, `FILL`, `FRAME` | pointillés, remplissage, attente du balayage | Oui | Oui |
| `BOX` ★ | rectangle (contour et remplissage) | Oui | Oui |
| `CIRCLE` ★ | rond (contour et remplissage) | Oui | Oui |
| `ELLIPSE` ★ | ovale droit ou incliné | Oui | Oui |
| `PAINT` ★ | colorier une zone | Oui | Oui |
| `STAR` ★ | étoile, polygone (bibliothèque GRAPHIQUE) | Oui | Oui |
| `DEFSPR` ★ | dessin d'un sprite (16 x 16 points au plus) | Oui | Oui |
| `SPRITE` ★ | montrer, déplacer, cacher un sprite (16 sprites) | Oui | Oui |
| `COLLIDE` ★ | deux sprites se touchent ? | Oui | Oui |

## 5. Son

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `SOUND`, `SQ` | jouer un son, état d'un canal | Oui | Oui |
| `ENV`, `ENT` | enveloppes de volume et de ton | Oui | Oui |
| `RELEASE` | libérer les sons retenus | Oui | Oui |
| `ON SQ GOSUB` | sous-programme quand un canal a de la place | Oui | Oui |

## 6. Clavier, manettes et saisie

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `INKEY` | une touche est-elle appuyée ? | Oui | Manette (tir 1 = espace, tir 2 = ENTRÉE, flèches ; les autres touches reçoivent un bouton libre) |
| `INKEY$` | la touche appuyée | Oui | Manette |
| `JOY` | manettes | Oui | Oui |
| `INPUT`, `LINE INPUT` | saisir un texte ou un nombre | Oui | Manette (haut et bas : lettre, droite : écrire, gauche : effacer, tir 1 : valider) |
| `KEY`, `KEY DEF`, `SPEED KEY` | touches et répétition | Oui | Non |

## 7. Disquette et cassette

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `OPENIN`, `OPENOUT`, `CLOSEIN`, `CLOSEOUT`, `EOF` | fichiers | Oui | Non |
| `INPUT #9`, `LINE INPUT #9`, `PRINT #9`, `WRITE #9` | lire, écrire un fichier | Oui | Non |
| `CAT`, `LOAD`, `SAVE` | disquette, cassette | Oui | Non |
| `SPEED WRITE` | vitesse de la cassette | Oui | Non |

## 8. Textes (chaînes de caractères)

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `LEN`, `LEFT$`, `RIGHT$`, `MID$` | longueur, morceaux | Oui | Oui |
| `CHR$`, `ASC` | caractère et son code | Oui | Oui |
| `STR$`, `VAL`, `DEC$`, `HEX$`, `BIN$` | nombre en texte et l'inverse | Oui | Oui |
| `STRING$`, `SPACE$`, `UPPER$`, `LOWER$`, `INSTR` | répéter, majuscules, chercher | Oui | Oui |

## 9. Nombres et calculs

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `+ - * / \ ^ MOD` | calculs | Oui | Oui |
| `AND`, `OR`, `XOR`, `NOT` | opérations logiques | Oui | Oui |
| `ABS`, `SGN`, `INT`, `FIX`, `CINT`, `CREAL`, `ROUND` | valeur, signe, arrondis | Oui | Oui |
| `MIN`, `MAX` | plus petit, plus grand | Oui | Oui |
| `SQR`, `EXP`, `LOG`, `LOG10`, `PI` | racine, puissances, logarithmes | Oui | Oui |
| `SIN`, `COS`, `TAN`, `ATN`, `DEG`, `RAD` | angles | Oui | Oui |
| `RND`, `RANDOMIZE` | hasard | Oui | Oui |
| `UNT` | nombre sans signe en nombre avec signe | Oui | Oui |
| `ISIN`, `ICOS` ★ | sinus et cosinus rapides en entiers | Oui | Oui |
| `HIBYTE`, `LOBYTE` ★ | octet fort, octet faible | Oui | Oui |
| `CLAMP` ★ | garder une valeur entre deux bornes | Oui | Oui |
| `RANDOM` ★ | nombre au hasard entre deux bornes | Oui | Oui |

## 10. Temps et interruptions

| Commande | Rôle | CPC | Cartouche |
|---|---|---|---|
| `TIME` | temps écoulé (300 par seconde) | Oui | Oui |
| `AFTER`, `EVERY`, `REMAIN` | sous-programmes lancés par le temps | Oui | Oui |
| `DI`, `EI` | couper, remettre ces sous-programmes | Oui | Oui |

## 11. Commandes de l'éditeur (tapées sans numéro de ligne)

`NEW`, `AUTO`, `EDIT`, `RENUM`, `DELETE`, `LIST` agissent dans l'éditeur. Elles ne font pas partie du programme compilé.

## Mots réservés pour plus tard ★

`VROOM son` : bruit de moteur sur le canal A : 1 moteur qui monte, 2 ralenti, 3 moteur qui descend (D213 ; se sert des enveloppes ENV 12 à 15 et ENT 11 à 15).

`BOOM son [, durée]` : explosions et chocs : 1 voiture qui explose, 2 feu d'artifice, 3 pétard ou coup de fusil, 4 marteau qui frappe, 5 malus ; durée facultative en centièmes de seconde (D214 ; se sert des enveloppes ENV 9 et 10, ENT 8 à 10).

Déjà colorés dans l'éditeur, à ne pas prendre comme noms de variables : `SCROLL`, `SHOT`, `FALL`, `WAVE`.
