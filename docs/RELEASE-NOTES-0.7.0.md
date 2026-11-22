# CPC BASIC IDE 0.7.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC et pour la console GX4000. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.6.0

**Cassette**
- F5 crée aussi `NOM.cdt`, à côté de la disquette. Sur un 464 : `RUN""` ; sur un 664 ou un 6128 : `|TAPE` (`ùTAPE` sur un CPC français) puis `RUN""`.
- Avec ou sans chargeur BASIC ; `RUN "nom"` marche aussi sur cassette. Essayée dans Caprice32 (464) et WinAPE (6128).

**Cartouche GX4000** (Ctrl+F5, menu Compiler)
- Crée `NOM.cpr`, une cartouche pour la GX4000 et les CPC 464+ et 6128+. Aucune ROM d'Amstrad n'y est copiée : la cartouche contient notre propre petit système, qui répond aux mêmes appels que la ROM du CPC.
- Manettes : tir 1 = ESPACE, tir 2 = ENTRÉE, directions = flèches, `JOY(0)` et `JOY(1)` ; les autres touches d'`INKEY` reçoivent un bouton libre (le message de Ctrl+F5 dit lequel).
- `INPUT` et `LINE INPUT` : saisie à la manette (haut et bas changent de lettre, le tir 2 change de groupe, droite écrit, gauche efface, le tir 1 valide).
- Comme sur le CPC : texte et fenêtres (`WINDOW`, `WINDOW SWAP`), couleurs et encres clignotantes (`SPEED INK`), tout le dessin (XOR, fenêtres graphiques, `MASK`, `FILL`), le son (`SOUND`, `ENV`, `ENT`, rendez-vous, `RELEASE`, `SQ`), `TIME`, `AFTER`, `EVERY`, `ON SQ GOSUB`, les nombres à virgule (`SIN`, `COS`, `SQR`, `EXP`, `LOG`, `^`...), calculés par nos propres routines (plus lentes que celles du CPC).
- Programmes de plus de 16 Ko : jusqu'à 30 464 octets, recopiés page après page au démarrage.
- Essayée dans AMSpiriT (machine GX4000). Exemples : `gxtexte.bas`, `gxmanet.bas`, `gxsaisie.bas`, `gxcoul.bas`, `gxdessin.bas`, `gxxor.bas`, `gxgrand.bas`, `gxfenetre.bas`, `gxclign.bas`, `gxson.bas`, `gxenv.bas`, `gxtemps.bas`, `gxrdv.bas`, `gxreel.bas`.
- La console n'a ni disquette ni cassette : pas de fichiers, pas de `RUN "nom"`. À la fin du programme, une touche relance la cartouche.

## Pas encore là
Sprites.

## Somme de contrôle
SHA256 de `CpcBasic-0.7.0-win-x64.zip` : `bdeafc67d16851f3c1d7b3f6de40917fa9050a2616ecd9e2e7a955b694566fbf`
