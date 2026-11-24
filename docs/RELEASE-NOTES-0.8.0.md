# CPC BASIC IDE 0.8.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC et pour la console GX4000. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.7.0

**Sprites**
- `DEFSPR n, "ligne", "ligne"...` dessine un sprite (un caractère par point : le stylo, ou « . » pour le fond) ; `SPRITE n, x, y` le montre ou le déplace (coordonnées du MODE, coin en haut à gauche, comme BOX), au point près, en gardant le décor ; `SPRITE n` le cache ; `COLLIDE(a, b)` dit si deux sprites se touchent. 16 sprites, jusqu'à 16 × 16 points ; le plus grand numéro passe devant. Aussi sur cartouche. Exemples : `collide.bas`, `croise.bas`.

**SELECT CASE**
- `SELECT valeur` (nombre ou texte), puis `CASE 1`, `CASE 4 TO 6`, `CASE > 10`, plusieurs séparés par des virgules, `CASE ELSE`, et `END SELECT`. Exemple : `choix.bas`.

**Effets sonores** (en arrière-plan comme SOUND, sur CPC et sur cartouche ; sons choisis à l'oreille)
- `VROOM n` : 1 moteur qui monte, 2 ralenti, 3 moteur qui descend.
- `BOOM n [, durée]` : 1 voiture qui explose, 2 feu d'artifice, 3 pétard ou coup de fusil, 4 marteau qui frappe, 5 malus ; durée en centièmes de seconde.
- `SHOT n` : 1 laser, 2 missile, 3 rafale, 4 vitre qui casse.
- `FALL n` : 1 chute dans le vide puis le sol, 2 vie perdue.
- `WAVE n [, 1]` : 1 petite vague, 2 vague moyenne, 3 grosse vague, 4 gros plouf ; avec `, 1` le même son joué à l'envers. Pour une île : `FOR A=1 TO 100:WAVE 1+INT(RND*3):NEXT`.
- Les effets prennent leurs enveloppes parmi les numéros que vos `ENV` et `ENT` n'utilisent pas. Nouveaux mots réservés : `VROOM`, `BOOM`, `SHOT`, `FALL`, `WAVE`. Exemples : `vroom.bas`, `boom.bas`, `shot.bas`, `fall.bas`, `wave.bas`.

**Plus rapide sur CPC**
- `DRAW` et `DRAWR` écrivent directement dans l'écran : `illusion.bas` passe de 58 à 63 secondes à 25,6 secondes.
- `PLOT` et `PLOTR` environ 1,7 fois plus rapides, `MOVE` et `MOVER` environ 3 fois dans un programme avec des PLOT (chronomètre : `plottemp.bas`). Mêmes points que la ROM. La ROM garde les cas particuliers (MASK, mode d'écriture XOR..., événements, TRON).
- Sur cartouche, les traits tout droits et en biais sont aussi bien plus rapides.

**Fichiers de données**
- `LOAD "nom", adresse` écrit en clair : F5 met aussi le fichier du dossier du programme (`NOM` ou `NOM.BIN`, avec ou sans en-tête AMSDOS) sur la disquette et sur la cassette (juste après son programme, dans l'ordre des LOAD). Exemple : `charge.bas` et `IMAGE.BIN` (un écran chargé d'un coup).
- Les fichiers de plus de 16 Ko passent maintenant sur la disquette.

**Cartouche GX4000**
- `RUN "nom"` : Ctrl+F5 range aussi dans la cartouche les programmes lancés par RUN "nom", qui sont lus et lancés depuis la cartouche (exemple : `cartmenu.bas` avec `cartjeu.bas`).
- `LOAD "nom", adresse` : les fichiers de données du dossier sont rangés dans la cartouche et lus de là (exemple : `charge.bas`). Jusqu'à 512 Ko en tout.

## Pas encore là
`SCROLL` (gardé pour plus tard).

## Somme de contrôle
SHA256 de `CpcBasic-0.8.0-win-x64.zip` : `105204520c2b525042ad0ec0370ea8ab684694a28730a585a6d6ef505ffee319`
