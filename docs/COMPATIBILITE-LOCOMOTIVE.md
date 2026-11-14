# Compatibilité avec le Locomotive BASIC

Cette page recense **toutes les instructions, fonctions et opérateurs** du Locomotive BASIC et dit où en est le compilateur pour chacun. Elle sert à garder une vue d'ensemble avant d'ajouter des commandes nouvelles (règle D053). **Elle doit être mise à jour à chaque nouvelle commande.**

Date de l'inventaire : 13 novembre 2026 (mis à jour après le lot A). État relevé dans le code (`Parser.cs`, `Lexer.cs`, `CodeGenerator.cs`), pas de mémoire. Version du compilateur : 357 tests réussis avant le lot A (396 avec le lot A, 450 avec le lot B, 481 avec le lot C (parties 1 et MEMORY), 534 avec DIM, 568 avec DATA / READ / RESTORE, 609 avec le lot D).

**Source de la liste :** table des mots du Locomotive BASIC et index des commandes et fonctions de cpcwiki ([Technical information about Locomotive BASIC](https://www.cpcwiki.eu/index.php/Technical_information_about_Locomotive_BASIC), [Locomotive BASIC](https://cpcwiki.eu/index.php/BASIC)). Les mots marqués « BASIC 1.1 » n'existent que sur les CPC 664 et 6128.

## Légende

- **Faite** : fonctionne comme dans le Locomotive.
- **Partielle** : fonctionne, mais une partie de la syntaxe manque (voir la remarque).
- **Écart voulu** : fonctionne autrement, volontairement.
- **À faire** : pas encore reconnue par le compilateur (message « commande inconnue » ou refus).
- **Mode direct** : n'a de sens que dans l'éditeur ligne à ligne du CPC. Proposé : sans objet.
- **Ajout du projet** : n'existe pas dans le Locomotive.

## Résumé

Sur **157 entrées** du Locomotive BASIC : **28** faites, **19** partielles, **2** écarts voulus, **101** à faire, **7** en mode direct. Le projet ajoute en plus **9** entrées nouvelles.

Une entrée peut regrouper plusieurs mots proches (par exemple « MOVE / MOVER ») : ces nombres comptent des lignes du tableau, pas des mots du dictionnaire.

## Ce qui bloque le plus de choses

1. **Les étiquettes de saut** : GOTO, GOSUB, RETURN, ON … GOTO / GOSUB : faites (lot A, à valider).
2. **Les chaînes de caractères** : toute la section 8, INPUT, DATA/READ avec du texte, TAG, etc.
3. **Les réels** : fonctions mathématiques, nombres à virgule.
4. **Les tableaux et les données** : DIM, DATA, READ, RESTORE.
5. **L'accès à la machine** : PEEK, POKE, CALL, INP, OUT, MEMORY.
6. **Le clavier et les manettes** : INKEY, INKEY$, JOY, qui conditionnent tout jeu.

## 1. Structure du programme et déroulement

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `REM et '` | commentaire | Faite |  |
| `:` | sépare deux instructions sur une ligne | Faite |  |
| `LET (et affectation sans LET)` | donne une valeur à une variable | Faite | Entiers 16 bits seulement. |
| `END` | termine le programme | Faite | Le programme attend une touche puis redémarre. |
| `STOP` | arrête le programme | Partielle | Se comporte comme END : pas de message « Break in … ». |
| `FOR … TO … STEP … NEXT` | boucle comptée | Partielle | Limite et pas : nombres écrits dans le programme. La variable garde sa dernière valeur dans la boucle (le Locomotive : la valeur suivante). |
| `WHILE … WEND` | boucle tant que | Faite | Condition : toute valeur (vraie si elle n'est pas 0), comme le Locomotive. D060. |
| `IF … THEN … ELSE` | condition | Faite | Condition : toute valeur (vraie si elle n'est pas 0), D060. `THEN 100` et `ELSE 200` (numéro de ligne seul) sont acceptés. Le bloc sur plusieurs lignes avec END IF est un ajout du projet. |
| `GOTO / GO TO` | saut à une ligne | Faite | Numéro de ligne écrit en chiffres (1 à 65529) ; ligne inexistante refusée à la compilation. |
| `GOSUB / GO SUB` | appel d'un sous-programme | Faite | Comme GOTO. |
| `RETURN` | retour d'un sous-programme | Partielle | Un RETURN sans GOSUB n'est pas détecté (le CPC afficherait « Unexpected RETURN »). |
| `ON x GOTO` | saut selon une valeur | Partielle | Valeur hors de 1 à N : le programme continue (pas d'erreur pour une valeur négative). |
| `ON x GOSUB` | appel selon une valeur | Partielle | Comme ON x GOTO. |
| `CALL` | appelle un programme en langage machine | Partielle | Adresse seule, sans paramètres. |
| `CHAIN` | charge et lance un autre programme | À faire |  |
| `CHAIN MERGE` | charge un autre programme en gardant les variables | À faire |  |
| `RUN` | lance un programme (aussi RUN "fichier") | À faire | À décider : RUN dans un programme compilé. |
| `CLEAR` | efface les variables | À faire |  |
| `CLEAR INPUT` | vide le tampon du clavier | Faite |  |
| `TRON / TROFF` | suivi des lignes (débogage) | À faire | Utilité à décider. |
| `ON BREAK CONT / GOSUB / STOP` | réaction à la touche ESC | À faire |  |
| `ON ERROR GOTO` | réaction à une erreur | À faire |  |
| `RESUME` | reprise après une erreur | À faire |  |
| `ERROR` | provoque une erreur | À faire |  |
| `ERL` | numéro de la ligne en erreur | À faire |  |
| `ERR` | numéro de l'erreur | À faire |  |
| `DERR` | erreur du disque | À faire | BASIC 1.1 seulement (664, 6128). |

## 2. Variables, constantes, données et mémoire

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `Variables entières (A, A%)` | nombre entier | Écart voulu | Le Locomotive : A est un réel. Ici A et A% sont deux variables entières 16 bits distinctes. |
| `Variables réelles (A!)` | nombre à virgule | À faire | Refusé à la compilation pour l'instant. |
| `Variables chaînes (A$)` | texte | À faire | Refusé à la compilation pour l'instant. |
| `Nombres décimaux entiers` | constantes 0 à 32767 (et négatifs) | Faite |  |
| `Nombres hexadécimaux et binaires (&H, &X)` | constantes écrites en base 16 ou 2 | Faite | &C9, &HC9, &X1010, de &0 à &FFFF (&FFFF = -1). D060. |
| `Nombres réels (3.14, 1E5)` | constantes à virgule | À faire |  |
| `DEFINT / DEFREAL / DEFSTR` | type des variables selon la première lettre | À faire | À décider : DEFINT a peu de sens si tout est entier. |
| `DIM` | tableaux | Faite | Tableaux d'entiers, une ou plusieurs dimensions, taille écrite ou calculée, création automatique (0 à 10) sans DIM, vérification des cases (« Subscript out of range »). Textes et réels avec les lots E et F. D058. |
| `ERASE` | efface un tableau | À faire |  |
| `DATA` | données dans le programme | Partielle | Nombres entiers (-32768 à 65535), aussi en hexadécimal (&C9, D060). Textes avec le lot E. D059. |
| `READ` | lit les données | Faite | Dans une variable ou une case de tableau ; « DATA exhausted » s'il n'y a plus rien. D059. |
| `RESTORE` | relit les données depuis une ligne | Faite | RESTORE seul ou RESTORE ligne. D059. |
| `DEF FN / FN` | fonction définie par le programmeur | À faire |  |
| `SWAP` | échange deux variables | À faire |  |
| `MEMORY` | limite la mémoire du BASIC | Partielle | Protège le haut de la mémoire : le compilateur vérifie que le programme tient dessous (si l'adresse est un nombre écrit). D057. |
| `HIMEM` | haut de la mémoire du BASIC | À faire |  |
| `FRE` | mémoire libre | À faire |  |
| `PEEK` | lit un octet en mémoire | Faite | Adresse de 0 à 65535. |
| `POKE` | écrit un octet en mémoire | Faite | Valeur de 0 à 255 ; adresse de 0 à 65535. |
| `INP` | lit un port | Faite | Port de 0 à 65535. |
| `OUT` | écrit sur un port | Faite | Valeur de 0 à 255. |
| `WAIT` | attend un état sur un port | À faire |  |
| `UNT` | convertit une adresse en entier signé | À faire |  |
| `| (commandes RSX)` | appelle une extension | À faire |  |
| `DI / EI` | coupe / remet les interruptions | À faire |  |
| `SYMBOL` | redéfinit un caractère | À faire |  |
| `SYMBOL AFTER` | réserve les caractères redéfinissables | À faire |  |

## 3. Texte et fenêtres d'affichage

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `PRINT et ?` | affiche du texte et des nombres | Partielle | Séparateur « ; » seulement. Pas de virgule (zones), de TAB, SPC, USING, ni de flux #. Les accents sont remplacés par « ? ». |
| `TAB / SPC` | positionne ou saute des colonnes dans PRINT | À faire |  |
| `USING` | format d'affichage des nombres (PRINT USING) | À faire |  |
| `WRITE` | affiche avec guillemets et virgules | À faire |  |
| `LOCATE` | place le curseur de texte | Partielle | Sans le numéro de fenêtre #n. |
| `CLS` | efface l'écran de texte | Partielle | Sans le numéro de fenêtre #n. |
| `PEN` | couleur du texte | Partielle | Sans #n ni mode transparent. |
| `PAPER` | couleur du fond du texte | Partielle | Sans #n. |
| `INK` | couleur associée à un stylo (avec clignotement) | Faite |  |
| `BORDER` | couleur du cadre (avec clignotement) | Faite |  |
| `MODE` | mode d'écran 0, 1 ou 2 | Faite |  |
| `WINDOW` | fenêtre de texte | À faire |  |
| `WINDOW SWAP` | échange deux fenêtres | À faire |  |
| `CURSOR` | affiche ou cache le curseur | À faire | BASIC 1.1 seulement (664, 6128). |
| `WIDTH` | largeur de ligne de l'imprimante | À faire |  |
| `ZONE` | largeur des zones de PRINT | À faire |  |
| `TAG` | texte aux coordonnées graphiques | À faire |  |
| `TAGOFF` | fin de TAG | À faire |  |
| `SPEED INK` | vitesse du clignotement des couleurs | À faire |  |
| `FRAME` | attend le balayage de l'écran | À faire | BASIC 1.1 seulement (664, 6128). |
| `POS` | colonne du curseur | À faire |  |
| `VPOS` | ligne du curseur | À faire |  |
| `COPYCHR$` | caractère lu à l'écran | À faire | BASIC 1.1 seulement (664, 6128). |

## 4. Graphismes

Ces commandes utilisent le repère du Locomotive : 640 × 400, origine en bas à gauche (D047).

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `MOVE / MOVER` | déplace le curseur graphique | Faite | Stylo et mode facultatifs. |
| `PLOT / PLOTR` | allume un point | Faite | Stylo et mode facultatifs. |
| `DRAW / DRAWR` | trace une ligne | Faite | Stylo et mode facultatifs. |
| `ORIGIN` | origine des coordonnées graphiques | Partielle | ORIGIN x, y seulement. Pas encore la fenêtre graphique (gauche, droite, haut, bas). |
| `CLG` | efface l'écran graphique | Faite | Avec un stylo facultatif. |
| `GRAPHICS PEN` | stylo et mode de remplissage des tracés | À faire | BASIC 1.1 seulement (664, 6128). |
| `GRAPHICS PAPER` | fond des tracés | À faire | BASIC 1.1 seulement (664, 6128). |
| `MASK` | motif des lignes | À faire | BASIC 1.1 seulement (664, 6128). |
| `FILL` | remplit une zone | À faire | BASIC 1.1 seulement (664, 6128). |
| `TEST / TESTR` | lit la couleur d'un point | À faire |  |
| `XPOS / YPOS` | position du curseur graphique | À faire |  |

## 5. Son

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `SOUND` | joue un son | À faire |  |
| `ENT` | enveloppe de hauteur | À faire |  |
| `ENV` | enveloppe de volume | À faire |  |
| `RELEASE` | libère les files de son | À faire |  |
| `SQ (fonction)` | état d'une file de son | À faire |  |
| `ON SQ GOSUB` | appel quand une file se libère | À faire |  |

## 6. Clavier, manettes et saisie

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `INKEY` | état d'une touche | Faite | À confirmer dans WinAPE (`examples\clavier.bas`). |
| `INKEY$` | touche appuyée | À faire |  |
| `JOY` | état de la manette | Faite | JOY avec une valeur autre que 0 ou 1 lit la manette 1. |
| `KEY` | définit une touche de fonction | À faire |  |
| `KEY DEF` | définit le comportement d'une touche | À faire |  |
| `SPEED KEY` | vitesse de répétition des touches | À faire |  |
| `INPUT` | saisie d'une valeur au clavier | Partielle | Nombres entiers seulement (pas de réels ni de chaînes pour l'instant), dans une variable ou une case de tableau. |
| `LINE INPUT` | saisie d'une ligne de texte | À faire |  |

## 7. Disquette et cassette

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `CAT` | catalogue | À faire |  |
| `LOAD` | charge un fichier | À faire |  |
| `SAVE` | enregistre un fichier | À faire |  |
| `MERGE` | fusionne un programme | À faire |  |
| `OPENIN / CLOSEIN` | ouvre / ferme un fichier en lecture | À faire |  |
| `OPENOUT / CLOSEOUT` | ouvre / ferme un fichier en écriture | À faire |  |
| `EOF` | fin de fichier | À faire |  |
| `PRINT # / INPUT # / LINE INPUT # / WRITE #` | lecture et écriture dans un flux | À faire |  |
| `SPEED WRITE` | vitesse d'écriture sur cassette | À faire |  |

## 8. Chaînes de caractères

Tout cela attend les variables chaînes (A$).

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `ASC` | code d'un caractère | À faire |  |
| `CHR$` | caractère d'un code | À faire |  |
| `LEFT$` | début d'un texte | À faire |  |
| `RIGHT$` | fin d'un texte | À faire |  |
| `MID$ (fonction et instruction)` | milieu d'un texte, remplacement | À faire |  |
| `LEN` | longueur | À faire |  |
| `INSTR` | cherche un texte dans un autre | À faire |  |
| `LOWER$ / UPPER$` | minuscules / majuscules | À faire |  |
| `STR$` | nombre en texte | À faire |  |
| `VAL` | texte en nombre | À faire |  |
| `STRING$` | répète un caractère | À faire |  |
| `SPACE$` | suite d'espaces | À faire |  |
| `BIN$` | nombre en binaire | À faire |  |
| `HEX$` | nombre en hexadécimal | À faire |  |
| `DEC$` | nombre formaté | À faire | BASIC 1.1 seulement (664, 6128). |

## 9. Mathématiques et nombres au hasard

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `ABS` | valeur absolue | Faite |  |
| `SGN` | signe | Faite |  |
| `MIN / MAX` | plus petite / plus grande valeur | Faite | Deux valeurs ou plus. |
| `RANDOMIZE` | initialise le hasard | Faite | Avec ou sans nombre. |
| `RND` | nombre au hasard | À faire | Le projet propose RANDOM(min, max) à la place (ajout). |
| `INT / FIX / CINT / ROUND / CREAL` | arrondis et conversions | À faire | Surtout utiles avec les réels. |
| `SQR` | racine carrée | À faire |  |
| `EXP / LOG / LOG10` | exponentielle, logarithmes | À faire |  |
| `SIN / COS / TAN / ATN` | trigonométrie | À faire |  |
| `PI` | valeur de pi | À faire |  |
| `DEG / RAD` | degrés ou radians | À faire |  |

## 10. Opérateurs

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `+  -  *` | addition, soustraction, multiplication | Faite | Entiers 16 bits. |
| `/` | division | Écart voulu | Le Locomotive donne un réel. Ici la division est entière. |
| `\` | division entière | Faite |  |
| `MOD` | reste de la division | Faite |  |
| `^` | puissance | Partielle | Entière (sur 16 bits) ; exposant négatif : 0 sauf pour 1 et -1. Réels avec le lot F. D060. |
| `=  <>  <  >  <=  >=` | comparaisons | Faite | Aussi comme valeur : -1 si vraie, 0 sinon. D060. |
| `AND / OR` | et / ou | Faite | Bit à bit, comme le Locomotive. D060. |
| `XOR` | ou exclusif | Faite | Bit à bit. D060. |
| `NOT` | contraire | Faite | Inverse tous les bits. D060. |

## 11. Temps et interruptions

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `TIME` | compteur de temps (300 par seconde) | Partielle | Renvoie seulement les 16 bits de poids faible. |
| `AFTER` | action après un délai | À faire |  |
| `EVERY` | action répétée | À faire |  |
| `REMAIN` | temps restant d'un compteur | À faire |  |

## 12. Mode direct : éditer et gérer le programme

Un compilateur n'a pas de mode direct. Proposition : « sans objet », à confirmer par vous.

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `AUTO` | numérotation automatique | Mode direct |  |
| `CONT` | reprend après STOP | Mode direct |  |
| `DELETE` | supprime des lignes | Mode direct |  |
| `EDIT` | modifie une ligne | Mode direct |  |
| `LIST` | affiche le programme | Mode direct |  |
| `NEW` | efface le programme | Mode direct |  |
| `RENUM` | renumérote | Mode direct | L'éditeur pourrait offrir cette fonction plus tard. |

## 13. Ajouts du projet (hors Locomotive BASIC)

Ces mots sont **réservés** : une variable portant ce nom ne compile plus. Un programme écrit pour le Locomotive qui utiliserait l'un d'eux comme nom de variable devra être modifié.

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `BOX` | rectangle : contour et remplissage | Ajout du projet | BOX x1, y1, x2, y2 [, stylo [, remplissage]], repère natif (D048, D051). |
| `BYTE` | variable sur un octet | Ajout du projet | Déclaration obligatoire avant usage. |
| `EXIT / CONTINUE` | sortie / suite d'une boucle | Ajout du projet |  |
| `END IF / ENDIF` | fin d'un IF en bloc | Ajout du projet |  |
| `REPEAT … UNTIL` | boucle jusqu'à | Ajout du projet | REPEAT et UNTIL sont de nouveaux mots réservés (le Locomotive n'a que WHILE … WEND). |
| `DO … LOOP` | boucle sans fin ou avec condition | Ajout du projet |  |
| `HIBYTE / LOBYTE` | octet de poids fort / faible | Ajout du projet |  |
| `CLAMP` | borne une valeur | Ajout du projet | CLAMP(valeur, min, max). |
| `RANDOM` | nombre au hasard entre deux bornes | Ajout du projet | RANDOM(min, max). |

**Prévus, pas encore faits** (déjà colorés dans l'éditeur, à ne pas utiliser comme noms de variables) : CIRCLE, ELLIPSE, STAR, PAINT, POLYGON, SPRITE, COLLIDE, SCROLL, SELECT, CASE, VROOM, BOOM, SHOT, FALL, WAVE.

## Règles pour les prochaines commandes

- **Le nom d'une commande ne doit pas être celui d'un mot du Locomotive.** Si un mot est pris par le Locomotive, il garde le sens du Locomotive (exemple : `PAUSE` est réservé à la gestion du temps, D049).
- **Toute nouvelle commande est ajoutée dans la section 13**, avec son état, et la liste des mots réservés est tenue à jour.
- **Les commandes de dessin nouvelles** suivent la règle D052 (contour puis remplissage, très rapides).

## Ordre de travail proposé (à valider)

Ce n'est pas une décision : c'est ma proposition, à discuter.

1. **Lot A : sauts et sous-programmes (fait, validé).** GOTO, GOSUB, RETURN, ON … GOTO / GOSUB, STOP.
2. **Lot B : clavier et manettes (INKEY, JOY, CLEAR INPUT, INPUT (entiers) faits ; INKEY$ après les chaînes).** INKEY, INKEY$, JOY, puis INPUT pour les nombres.
3. **Lot C : données et mémoire.** DIM, DATA / READ / RESTORE, PEEK, POKE, CALL, INP, OUT, MEMORY.
4. **Lot D : nombres hexadécimaux, XOR, NOT, ^, comparaisons comme valeurs (fait, validé, D060).**
5. **Lot E : chaînes de caractères.**
6. **Lot F : réels et fonctions mathématiques.**
7. **Lot G : son.** SOUND, ENT, ENV, RELEASE, SQ, ON SQ GOSUB.
8. **Lot H : fenêtres et mise en forme.** WINDOW, TAB, SPC, USING, ZONE, SYMBOL, TAG, flux #.
9. **Lot I : interruptions et erreurs.** AFTER, EVERY, REMAIN, ON BREAK, ON ERROR GOTO, RESUME.
10. **Lot J : disquette et cassette.** CAT, LOAD, SAVE, OPENIN, OPENOUT, flux.

Les commandes nouvelles (CIRCLE, ELLIPSE, STAR, PAINT, POLYGON, SPRITE…) peuvent s'intercaler à tout moment : elles n'empêchent aucun de ces lots, car elles prennent des nombres entiers qui resteront valables quand les réels arriveront.

