# Compatibilité avec le Locomotive BASIC

Cette page recense **toutes les instructions, fonctions et opérateurs** du Locomotive BASIC et dit où en est le compilateur pour chacun. Elle sert à garder une vue d'ensemble avant d'ajouter des commandes nouvelles (règle D053). **Elle doit être mise à jour à chaque nouvelle commande.**

Date de l'inventaire : 13 novembre 2026 (mis à jour après le lot A). État relevé dans le code (`Parser.cs`, `Lexer.cs`, `CodeGenerator.cs`), pas de mémoire. Version du compilateur : 357 tests réussis avant le lot A (396 avec le lot A, 450 avec le lot B, 481 avec le lot C (parties 1 et MEMORY), 534 avec DIM, 568 avec DATA / READ / RESTORE, 609 avec le lot D, 622 avec @, 657 avec les textes E1, 685 avec E2, 700 avec E3a, 721 avec E3b : lot E terminé).

**Source de la liste :** table des mots du Locomotive BASIC et index des commandes et fonctions de cpcwiki ([Technical information about Locomotive BASIC](https://www.cpcwiki.eu/index.php/Technical_information_about_Locomotive_BASIC), [Locomotive BASIC](https://cpcwiki.eu/index.php/BASIC)). Les mots marqués « BASIC 1.1 » n'existent que sur les CPC 664 et 6128.

## Légende

- **Faite** : fonctionne comme dans le Locomotive.
- **Partielle** : fonctionne, mais une partie de la syntaxe manque (voir la remarque).
- **Écart voulu** : fonctionne autrement, volontairement.
- **À faire** : pas encore reconnue par le compilateur (message « commande inconnue » ou refus).
- **Mode direct** : n'a de sens que dans l'éditeur ligne à ligne du CPC. Proposé : sans objet.
- **Ajout du projet** : n'existe pas dans le Locomotive.

## Résumé

Mise à jour le 16 nov. 2026, après le lot I. Sur **151 entrées** du Locomotive BASIC (hors mode direct) : **101** faites, **11** partielles, **2** écarts voulus, **37** à faire. Les **7** commandes du mode direct deviennent des commandes de l'éditeur (D077), à faire. Le projet ajoute en plus des entrées nouvelles (section 13).

Une entrée peut regrouper plusieurs mots proches (par exemple « MOVE / MOVER ») : ces nombres comptent des lignes du tableau, pas des mots du dictionnaire.

## Ce qui bloque le plus de choses

Mise à jour le 16 nov. 2026 :

1. **FOR avec une limite ou un pas calculés** (`FOR I = 1 TO N`) : fait et validé le 16 nov. 2026 (D078).
2. **Les interruptions et les erreurs** (lot I) : AFTER, EVERY, ON ERROR GOTO, ON SQ, ON BREAK : faits le 16 nov. 2026 (D079 à D083).
3. **La disquette et la cassette** (lot J) : LOAD, SAVE, fichiers de données.
4. **Les commandes RSX et CALL avec des valeurs** : nécessaires au programme de référence de l'utilisateur (`docs\PROGRAMME-DE-REFERENCE-RSX.md`).

## 1. Structure du programme et déroulement

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `REM et '` | commentaire | Faite |  |
| `:` | sépare deux instructions sur une ligne | Faite |  |
| `LET (et affectation sans LET)` | donne une valeur à une variable | Faite | Entiers 16 bits seulement. |
| `END` | termine le programme | Faite | Le programme attend une touche puis redémarre. |
| `STOP` | arrête le programme | Faite | « Break in N » puis fin (D081). Pas de CONT dans un programme compilé. |
| `FOR … TO … STEP … NEXT` | boucle comptée | Faite | Limite et pas écrits ou calculés (D078) ; test au début ; après la boucle, la variable vaut la valeur suivante, comme le Locomotive. |
| `WHILE … WEND` | boucle tant que | Faite | Condition : toute valeur (vraie si elle n'est pas 0), comme le Locomotive. D060. |
| `IF … THEN … ELSE` | condition | Faite | Condition : toute valeur (vraie si elle n'est pas 0), D060. `THEN 100` et `ELSE 200` (numéro de ligne seul) sont acceptés. Le bloc sur plusieurs lignes avec END IF est un ajout du projet. |
| `GOTO / GO TO` | saut à une ligne | Faite | Numéro de ligne écrit en chiffres (1 à 65529) ; ligne inexistante refusée à la compilation. |
| `GOSUB / GO SUB` | appel d'un sous-programme | Faite | Comme GOTO. |
| `RETURN` | retour d'un sous-programme | Faite | RETURN sans GOSUB : « Unexpected RETURN in N » (D081). |
| `ON x GOTO` | saut selon une valeur | Faite | 0 ou plus de N : le programme continue ; hors de 0 à 255 : « Improper argument » (D097). |
| `ON x GOSUB` | appel selon une valeur | Faite | Comme ON x GOTO (D097). |
| `CALL` | appelle un programme en langage machine | Faite | Avec valeurs : CALL adresse, v1, v2… (D089). |
| `CHAIN` | charge et lance un autre programme | Écart voulu | Refusé à la compilation (les variables ne peuvent pas passer d'un programme compilé à l'autre) : RUN "nom" et un fichier (D086). |
| `CHAIN MERGE` | charge un autre programme en gardant les variables | Écart voulu | Refusé : ajoute des lignes BASIC (D086). |
| `RUN` | lance un programme (aussi RUN "fichier") | Faite | RUN "nom" lance un autre programme compilé (nom.BIN), D086 ; RUN et RUN ligne recommencent le programme (D090). |
| `CLEAR` | efface les variables | Faite | D090. |
| `CLEAR INPUT` | vide le tampon du clavier | Faite |  |
| `TRON / TROFF` | suivi des lignes (débogage) | Faite | D088. |
| `ON BREAK CONT / GOSUB / STOP` | réaction à la touche ESC | Faite | D083. Touche ESC : réglage F9 de l'éditeur (comme le CPC par défaut). |
| `ON ERROR GOTO` | réaction à une erreur | Faite | D080. |
| `RESUME` | reprise après une erreur | Faite | RESUME, RESUME NEXT, RESUME ligne (D080). |
| `ERROR` | provoque une erreur | Faite | D080. |
| `ERL` | numéro de la ligne en erreur | Faite | D080. |
| `ERR` | numéro de l'erreur | Faite | D080. |
| `DERR` | erreur du disque | Faite | D099 : code d'erreur d'AMSDOS (146 : fichier absent), 0 au départ. Marche aussi sur le 464. |

## 2. Variables, constantes, données et mémoire

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `Variables entières (A, A%)` | nombre entier | Écart voulu | Le Locomotive : A est un réel. Ici A et A% sont deux variables entières 16 bits distinctes. |
| `Variables réelles (A!)` | nombre à virgule | Faite | Lot F (D068 à D070) : calculs, comparaisons, PRINT, FOR, INPUT, @, tableaux, DATA, READ, VAL, STR$. |
| `Variables chaînes (A$)` | texte | Faite | Variables, +, comparaisons, PRINT, INPUT (D062), tableaux de textes (D065). |
| `Nombres décimaux entiers` | constantes 0 à 32767 (et négatifs) | Faite |  |
| `Nombres hexadécimaux et binaires (&H, &X)` | constantes écrites en base 16 ou 2 | Faite | &C9, &HC9, &X1010, de &0 à &FFFF (&FFFF = -1). D060. |
| `Nombres réels (3.14, 1E5)` | constantes à virgule | Fait | Lot F1b1 (D068) : calculs par la ROM (464 et 664/6128), affichage identique au vrai BASIC, Overflow et Division by zero qui laissent continuer. Entre deux entiers, le calcul reste entier (7/2 = 3, D066). |
| `DEFINT / DEFREAL / DEFSTR` | type des variables selon la première lettre | Fait | Lot F1b2 (D068) : valent pour tout le programme ; après DEFINT A-Z, A et A% sont la même variable. Réglage F8 = DEFREAL A-Z. |
| `DIM` | tableaux | Faite | Tableaux d'entiers, une ou plusieurs dimensions, taille écrite ou calculée, création automatique (0 à 10) sans DIM, vérification des cases (« Subscript out of range »). Textes et réels avec les lots E et F. D058. |
| `ERASE` | efface un tableau | Faite | Lot K8 (D096) : la place est rendue, les tableaux suivants descendent (textes compris). Écart : le nombre de dimensions ne peut pas changer (D058). |
| `DATA` | données dans le programme | Faite | Nombres entiers (-32768 à 65535, &hexadécimal), textes entre guillemets ou non, éléments vides (D059, D060, D064). Réels avec le lot F. |
| `READ` | lit les données | Faite | Dans une variable, une case de tableau ou une variable texte (READ A$ rend un nombre tel qu'il est écrit) ; « DATA exhausted », « Type mismatch ». D059, D064. |
| `RESTORE` | relit les données depuis une ligne | Faite | RESTORE seul ou RESTORE ligne. D059. |
| `DEF FN / FN` | fonction définie par le programmeur | Faite | Lot K7 (D095) : FNNOM ou FN NOM, valeurs locales, textes et nombres à virgule. Écarts : sans signe, valeurs entières (D026, message avec la solution) ; fonction inconnue, en double ou qui s'appelle elle-même : refusée à la compilation. |
| `SWAP` | échange deux variables | Écart voulu | N'existe pas dans le Locomotive BASIC (SugarBox, 16 nov. 2026 : « Syntax error ») ; seul WINDOW SWAP existe (fait, D072). |
| `MEMORY` | limite la mémoire du BASIC | Partielle | Protège le haut de la mémoire : le compilateur vérifie que le programme tient dessous (si l'adresse est un nombre écrit). D057. Adresse calculée à virgule permise (MEMORY HIMEM-1000, D094). |
| `HIMEM` | haut de la mémoire du BASIC | Faite | Lot K6 (D094) : nombre à virgule (42619, ou l'adresse de MEMORY). |
| `FRE` | mémoire libre | Faite | Lot K6 (D094) : FRE(0), FRE("") avec ménage des textes. Le nombre diffère du vrai CPC (programme compilé). |
| `PEEK` | lit un octet en mémoire | Faite | Adresse de 0 à 65535. |
| `POKE` | écrit un octet en mémoire | Faite | Valeur de 0 à 255 ; adresse de 0 à 65535. |
| `INP` | lit un port | Faite | Port de 0 à 65535. |
| `OUT` | écrit sur un port | Faite | Valeur de 0 à 255. |
| `WAIT` | attend un état sur un port | Faite | Lot K6 (D094) : WAIT port, masque [, inversion]. |
| `UNT` | convertit une adresse en entier signé | Faite | Lot K6 (D094). |
| `| (commandes RSX)` | appelle une extension | Faite | |NOM, valeurs (D089) ; inconnue : « Unknown command ». |
| `@` | adresse d'une variable | Partielle | `@A`, `@A%`, `@T(3)` (D061), `@A$` = adresse de la fiche de 3 octets (D064). Passage à CALL et aux RSX à venir. |
| `DI / EI` | coupe / remet les interruptions | Faite | Retient / laisse partir les appels des minuteries (D079). |
| `SYMBOL` | redéfinit un caractère | Faite | D075. |
| `SYMBOL AFTER` | réserve les caractères redéfinissables | Faite | D075. 240 à 255 redessinables dès le départ. |

## 3. Texte et fenêtres d'affichage

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `PRINT et ?` | affiche du texte et des nombres | Partielle | Séparateurs « ; » et « , » (zones), TAB, SPC (D073), fenêtres #0 à #7 (D072). USING (D074, D098). Les accents sont remplacés par « ? ». |
| `TAB / SPC` | positionne ou saute des colonnes dans PRINT | Faite | D073. |
| `USING` | format d'affichage des nombres (PRINT USING) | Faite | D074 : tous les formats ; D098 : format dans une variable ou un calcul, lu à l'exécution. |
| `WRITE` | affiche avec guillemets et virgules | Faite | Avec #0 à #7 (D073). |
| `LOCATE` | place le curseur de texte | Faite | Avec #n (D072). |
| `CLS` | efface l'écran de texte | Faite | Avec #n (D072). |
| `PEN` | couleur du texte | Faite | Avec #n (D072) ; mode transparent PEN stylo, 1 (D104). |
| `PAPER` | couleur du fond du texte | Faite | Avec #n (D072). |
| `INK` | couleur associée à un stylo (avec clignotement) | Faite |  |
| `BORDER` | couleur du cadre (avec clignotement) | Faite |  |
| `MODE` | mode d'écran 0, 1 ou 2 | Faite |  |
| `WINDOW` | fenêtre de texte | Faite | D072. |
| `WINDOW SWAP` | échange deux fenêtres | Faite | D072. |
| `CURSOR` | affiche ou cache le curseur | Faite | D102 (BASIC 1.1, marche sur tous les CPC). « CURSOR ,1 » pas encore accepté. |
| `WIDTH` | largeur de ligne de l'imprimante | Faite | D100 : 1 à 255 (255 : pas de retour automatique), 132 au départ. |
| `ZONE` | largeur des zones de PRINT | Faite | D073. |
| `TAG` | texte aux coordonnées graphiques | Faite | Avec #n (D075). |
| `TAGOFF` | fin de TAG | Faite | Avec #n (D075). |
| `SPEED INK` | vitesse du clignotement des couleurs | Faite | D091. |
| `FRAME` | attend le balayage de l'écran | Faite | D102 (BASIC 1.1, marche sur tous les CPC). |
| `POS` | colonne du curseur | Faite | POS(#0) à POS(#7), D072 ; POS(#8), D100. |
| `VPOS` | ligne du curseur | Faite | D072. |
| `COPYCHR$` | caractère lu à l'écran | Faite | D102 (BASIC 1.1, marche sur tous les CPC). |

## 4. Graphismes

Ces commandes utilisent le repère du Locomotive : 640 × 400, origine en bas à gauche (D047).

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `MOVE / MOVER` | déplace le curseur graphique | Faite | Stylo et mode facultatifs. |
| `PLOT / PLOTR` | allume un point | Faite | Stylo et mode facultatifs. |
| `DRAW / DRAWR` | trace une ligne | Faite | Stylo et mode facultatifs. |
| `ORIGIN` | origine des coordonnées graphiques | Faite | ORIGIN x, y [, gauche, droite, haut, bas] : fenêtre graphique (D104). |
| `CLG` | efface l'écran graphique | Faite | Avec un stylo facultatif. |
| `GRAPHICS PEN` | stylo et mode de remplissage des tracés | Partielle | GRAPHICS PEN stylo (D106, tous les CPC) ; le mode transparent viendra avec MASK. |
| `GRAPHICS PAPER` | fond des tracés | Faite | D106 (tous les CPC). |
| `MASK` | motif des lignes | À faire | BASIC 1.1 seulement (664, 6128). |
| `FILL` | remplit une zone | À faire | BASIC 1.1 seulement (664, 6128). |
| `TEST / TESTR` | lit la couleur d'un point | Faite | Le curseur graphique va au point, comme le vrai BASIC (D091). |
| `XPOS / YPOS` | position du curseur graphique | Faite | D091. |

## 5. Son

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `SOUND` | joue un son | Fait | Lot G1 (D071), par la ROM ; valeurs vides permises. |
| `ENT` | enveloppe de hauteur | Fait | Lot G1 (D071), avec la forme « = » ; négatif : se répète. |
| `ENV` | enveloppe de volume | Fait | Lot G1 (D071), avec la forme « = ». |
| `RELEASE` | libère les files de son | Fait | Lot G1 (D071). |
| `SQ (fonction)` | état d'une file de son | Fait | Lot G1 (D071). |
| `ON SQ GOSUB` | appel quand une file se libère | Faite | D082. |

## 6. Clavier, manettes et saisie

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `INKEY` | état d'une touche | Faite | À confirmer dans WinAPE (`examples\clavier.bas`). |
| `INKEY$` | touche appuyée | Faite | D064. |
| `JOY` | état de la manette | Faite | JOY avec une valeur autre que 0 ou 1 lit la manette 1. |
| `KEY` | définit une touche de fonction | Faite | D092. |
| `KEY DEF` | définit le comportement d'une touche | Faite | D092. |
| `SPEED KEY` | vitesse de répétition des touches | Faite | D091. |
| `INPUT` | saisie d'une valeur au clavier | Faite | Entiers, nombres à virgule et textes, dans des variables ou des cases de tableau (D055, D059, D062, D065, D068) ; fenêtres #0 à #7 (D072) ; édition de la ligne comme le vrai BASIC, avec COPY (D076). |
| `LINE INPUT` | saisie d'une ligne de texte | Faite | Dans une variable texte ou une case d'un tableau de textes. D064, D065. Fenêtres #0 à #7 (D072). |

## 7. Disquette et cassette

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `CAT` | catalogue | Faite | D085. |
| `LOAD` | charge un fichier | Faite | Fichiers binaires (écrans…), D085. Un programme BASIC : « File type error ». |
| `SAVE` | enregistre un fichier | Faite | `SAVE "nom",B,début,longueur`, D085. Sans `,B` : refusé (le programme BASIC n'existe plus). |
| `MERGE` | fusionne un programme | Écart voulu | Refusé : ajoute des lignes BASIC (D086). |
| `OPENIN / CLOSEIN` | ouvre / ferme un fichier en lecture | Faite | D084. |
| `OPENOUT / CLOSEOUT` | ouvre / ferme un fichier en écriture | Faite | D084. CLOSEOUT nécessaire avant la fin. |
| `EOF` | fin de fichier | Faite | D084. |
| `PRINT # / INPUT # / LINE INPUT # / WRITE #` | lecture et écriture dans un flux | Faite | #9 (fichier), D084. #8 (imprimante) avec PRINT, WRITE et POS, D100. |
| `SPEED WRITE` | vitesse d'écriture sur cassette | Faite | D085. |

## 8. Chaînes de caractères

Tout cela attend les variables chaînes (A$).

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `ASC` | code d'un caractère | Faite | D063. |
| `CHR$` | caractère d'un code | Faite | D063. |
| `LEFT$` | début d'un texte | Faite | D063. |
| `RIGHT$` | fin d'un texte | Faite | D063. |
| `MID$ (fonction et instruction)` | milieu d'un texte, remplacement | Faite | Fonction (D063) et `MID$(A$, début [, longueur]) = texte` (D065). |
| `LEN` | longueur | Faite | D062. |
| `INSTR` | cherche un texte dans un autre | Faite | D063. |
| `LOWER$ / UPPER$` | minuscules / majuscules | Faite | Lettres de A à Z. D063. |
| `STR$` | nombre en texte | Faite | D063 ; nombres à virgule : D070. |
| `VAL` | texte en nombre | Faite | Décimal, &hexadécimal, &Xbinaire (D063) ; nombre à virgule depuis D070, lu comme le vrai BASIC. |
| `STRING$` | répète un caractère | Faite | D063. |
| `SPACE$` | suite d'espaces | Faite | D063. |
| `BIN$` | nombre en binaire | Faite | D063. |
| `HEX$` | nombre en hexadécimal | Faite | D063. |
| `DEC$` | nombre formaté | Faite | D103 (BASIC 1.1, marche sur tous les CPC) : format écrit ou dans une variable. |

## 9. Mathématiques et nombres au hasard

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `ABS` | valeur absolue | Faite |  |
| `SGN` | signe | Faite |  |
| `MIN / MAX` | plus petite / plus grande valeur | Faite | Deux valeurs ou plus. |
| `RANDOMIZE` | initialise le hasard | Faite | Avec ou sans nombre. |
| `RND` | nombre au hasard | Fait | Lot F2 (D069) : RND, RND(0), RND(négatif). Suite différente du vrai CPC. RANDOM(min, max) reste disponible (ajout). |
| `INT / FIX / CINT / ROUND / CREAL` | arrondis et conversions | Fait | Lot F2 (D069). La ROM rend la partie entière sur 4 octets : reconvertie par BINARY TO REAL. |
| `SQR` | racine carrée | Fait | Lot F2 (D069) : Improper argument si négatif. |
| `EXP / LOG / LOG10` | exponentielle, logarithmes | Fait | Lot F2 (D069). |
| `SIN / COS / TAN / ATN` | trigonométrie | Fait | Lot F2 (D069). |
| `PI` | valeur de pi | Fait | Lot F2 (D069). |
| `DEG / RAD` | degrés ou radians | Fait | Lot F2 (D069). |

## 10. Opérateurs

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `+  -  *` | addition, soustraction, multiplication | Faite | Entiers 16 bits. |
| `/` | division | Écart voulu | Le Locomotive donne un réel. Ici la division est entière. |
| `\` | division entière | Faite |  |
| `MOD` | reste de la division | Faite |  |
| `^` | puissance | Faite | À virgule dès qu'un côté l'est (D068) ; entre deux entiers, le calcul reste entier sur 16 bits (choix D066, D060). |
| `=  <>  <  >  <=  >=` | comparaisons | Faite | Aussi comme valeur : -1 si vraie, 0 sinon. D060. |
| `AND / OR` | et / ou | Faite | Bit à bit, comme le Locomotive. D060. |
| `XOR` | ou exclusif | Faite | Bit à bit. D060. |
| `NOT` | contraire | Faite | Inverse tous les bits. D060. |

## 11. Temps et interruptions

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `TIME` | compteur de temps (300 par seconde) | Faite | Avec le réglage F8 : complet (32 bits, à virgule). Sans F8 : les 16 bits du bas, en entier (choix de l'utilisateur, D097). |
| `AFTER` | action après un délai | Faite | D079. |
| `EVERY` | action répétée | Faite | D079. |
| `REMAIN` | temps restant d'un compteur | Faite | D079. |

## 12. Mode direct : éditer et gérer le programme

Un programme compilé n'a pas de mode direct. **Décision de l'utilisateur (15 nov. 2026, D077) :** ces commandes deviennent des commandes **de l'éditeur**, avec le même rôle que sur le CPC. Elles se tapent dans le texte du programme, **sans numéro de ligne** (par exemple `LIST` seul sur une ligne).

| Mot | Rôle | État | Remarque |
|---|---|---|---|
| `AUTO` | numérotation automatique | Éditeur, à faire | Met automatiquement le numéro des lignes, avec le pas demandé. |
| `CONT` | reprend après STOP | Éditeur, à faire | Affiche son rôle en BASIC, comme CLEAR. |
| `DELETE` | supprime des lignes | Éditeur, à faire | Même rôle que le vrai BASIC ; avertissement si des étiquettes sont utilisées, puis confirmation. |
| `EDIT` | modifie une ligne | Éditeur, à faire | Place le curseur sur la ligne demandée. |
| `LIST` | affiche le programme | Éditeur, à faire | Affiche le programme dans l'éditeur comme sur le CPC. |
| `NEW` | efface le programme | Éditeur, à faire | Avec avertissement si le programme a été modifié (comme D043). |
| `RENUM` | renumérote | Éditeur, à faire | Erreur si des étiquettes sont utilisées. |
| `CLEAR`, `MEMORY`, `RUN`, `LOAD`, `SAVE`, `MERGE`, `CAT`, `CHAIN`, commandes AMSDOS (`|DISC`, `|TAPE`, `|A`, `|B`, `|DIR`, `|ERA`, `|REN`, `|USER`) | tapées dans l'éditeur sans numéro de ligne | Éditeur, à faire | Afficher à quoi la commande correspond dans le langage BASIC (D077). |

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
5. **Lot E : chaînes de caractères (fait, D062 à D065 ; reste DEC$).**
6. **Lot F : réels et fonctions mathématiques.**
7. **Lot G : son.** SOUND, ENT, ENV, RELEASE, SQ, ON SQ GOSUB.
8. **Lot H : fenêtres et mise en forme.** WINDOW, TAB, SPC, USING, ZONE, SYMBOL, TAG, flux #. Étapes (D072) : H1 fenêtres et #n (validée), H2 virgule, ZONE, TAB, SPC, WRITE (validée, D073) ; H3 PRINT USING (validée, D074) ; H4 SYMBOL, TAG (validée, D075) ; H5 édition de la ligne d'INPUT avec COPY (validée, D076). **Lot H terminé.**
9. **Lot I : interruptions et erreurs.** AFTER, EVERY, REMAIN, ON BREAK, ON ERROR GOTO, RESUME.
10. **Lot J : disquette et cassette.** CAT, LOAD, SAVE, OPENIN, OPENOUT, flux.

Les commandes nouvelles (CIRCLE, ELLIPSE, STAR, PAINT, POLYGON, SPRITE…) peuvent s'intercaler à tout moment : elles n'empêchent aucun de ces lots, car elles prennent des nombres entiers qui resteront valables quand les réels arriveront.

