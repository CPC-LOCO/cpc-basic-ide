# Idées à étudier

Une idée par ligne, avec la date. Quand une idée est décidée, elle passe dans le journal des décisions.

## Accessibilité (suites de D041)
- Essai avec un lecteur d'écran (Narrateur de Windows) : les noms sont posés mais jamais vérifiés. Annoncer le résultat d'une compilation sans que la personne cherche.
- Taille des boutons qui suit la taille du texte ? (aujourd'hui fixe : 48 points de haut).

## Messages d'erreur (suites de D042)
- Nommer la commande dans le message du guillemet manquant (erreur trouvée dans le lexer).
- Étendre l'aide « telle commande attend... » aux autres messages (UnexpectedToken, BlocksNotNested...).
- Passer les messages dans les fichiers de langue (français, anglais, espagnol).

## Langage
- Fonctions de calcul : ABS, SGN, MIN, MAX (existent dans le Locomotive), HIBYTE, LOBYTE, CLAMP, RANDOM, RANDOMIZE.
- Calcul en 3D : SIN, COS par table, multiplication en virgule fixe, racine carrée entière, projection.
- Conditions sans comparaison (IF a THEN), NOT, GOTO et GOSUB, tableaux, chaînes, réels.
- OPTION EXPLICIT : déclaration obligatoire de toutes les variables.
- FOR avec limite et pas variables.
- Erreur à l'exécution pour la division par zéro (aujourd'hui : résultat 0, sans message).
- Multiplication et division par une puissance de 2 : décalages plus rapides.

## Outils
- Adoptée : ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80), pour voir où s'arrête l'écran du CPC. À faire avec `MODE` (l'éditeur devra savoir quel MODE le programme utilise).
- Commande « Nouveau programme » (avec la confirmation de D043) ; ouverture d'un fichier par glisser-déposer.
- Commande « Mettre en forme » dans l'éditeur (remplace ? par PRINT, etc.).
