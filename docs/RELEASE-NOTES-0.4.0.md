# CPC BASIC IDE 0.4.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.3.0

**Le Locomotive BASIC est entièrement inventorié**
- Plus aucune commande « à faire » ni « partielle » dans `COMPATIBILITE.md` : il ne reste que des écarts voulus (CHAIN et MERGE).
- Derniers détails comme le vrai BASIC : `CURSOR ,1` et `MASK ,1` sans première valeur, `@` avec CALL et les RSX, `MEMORY` trop haut ou trop bas (« Memory full »), POKE et OUT avec une valeur hors de 0 à 255 (« Improper argument »).

**Lettres accentuées**
- Les textes montrent ce que le CPC affichera ; la version du CPC (française par défaut, ou anglaise) se choisit avec F7. Sur un CPC français, à s'écrit @, ù s'écrit | (et |DISC s'écrit ùDISC, comme sur la vraie machine).

**Nouvelles commandes**
- `CIRCLE x, y, rayon [, stylo [, remplissage]]` : rond, plein ou vide.
- `ELLIPSE x, y, rayon x, rayon y [, stylo [, remplissage [, angle]]]` : ovale droit ou incliné (angle en degrés ; une place peut rester vide : `ELLIPSE 160,100,50,20,,,30`).
- CIRCLE et ELLIPSE dessinent pour un **vrai moniteur Amstrad** : dans un émulateur qui montre des points carrés, les ronds paraissent environ 8 % plus larges que hauts, c'est normal.
- `ISIN(angle, échelle)` et `ICOS(angle, échelle)` : sinus et cosinus en entiers, environ 15 fois plus rapides que SIN et COS pour placer des points (`PLOT 320+ICOS(A,100),200+ISIN(A,100)`). SIN et COS restent ceux du Locomotive.
- `DEBUG` : affiche les variables pendant l'exécution, sur l'écran du CPC (avec le réglage F2) ; `DEBUG A,B$` pour quelques variables, `DEBUG T(I)` pour une case de tableau. Sans F2, les DEBUG ne coûtent rien.

**L'éditeur**
- F4 : liste des variables (type, lignes où elles reçoivent une valeur ou sont utilisées, adresse dans la mémoire du CPC), avec un ⚠ pour les fautes de frappe probables.
- Maj+F5 : vérifier la compilation sans rien enregistrer. Ctrl+Maj+S : enregistrer sous.
- F2 : mise au point (DEBUG). F3 : priorité vitesse ou place (le dessin reste le même).

**Licence**
- Les jeux que vous créez vous appartiennent : vous pouvez les diffuser ou les vendre sous la licence de votre choix (exception de liaison, fichier `EXCEPTION-LIAISON.md`, en vigueur depuis cette version, texte en attente de relecture).

## Pas encore là
Sortie cassette, cartouche GX4000, sprites.

## Somme de contrôle
SHA256 de `CpcBasic-0.4.0-win-x64.zip` : `0d3af79aebb4571beef93f7c061fcf55ff7d7015a194d55aeb3c510ca917584e`
