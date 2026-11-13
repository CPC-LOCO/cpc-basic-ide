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
- Commandes d'écran : vérifier les valeurs calculées (MODE, PEN, PAPER, INK, BORDER) ; limites de LOCATE selon le MODE (20, 40 ou 80 colonnes) ; WINDOW ; TAG et GRAPHICS PEN ; MODE avec effacement choisi.
- Fonctions de calcul : ABS, SGN, MIN, MAX (existent dans le Locomotive), HIBYTE, LOBYTE, CLAMP, RANDOM, RANDOMIZE.
- Calcul en 3D : SIN, COS par table, multiplication en virgule fixe, racine carrée entière, projection.
- Conditions sans comparaison (IF a THEN), NOT, GOTO et GOSUB, tableaux, chaînes, réels.
- OPTION EXPLICIT : déclaration obligatoire de toutes les variables.
- FOR avec limite et pas variables.
- Erreur à l'exécution pour la division par zéro (aujourd'hui : résultat 0, sans message).
- Multiplication et division par une puissance de 2 : décalages plus rapides.
- Tableaux d'octets (demande de l'utilisateur, D058) : `BYTE T` avant `DIM T(100)` déclarera un tableau dont les cases vont de 0 à 255 (moitié moins de mémoire). Aucune commande nouvelle (pas de DIMBYTE) : la façon d'écrire existe déjà, la compilation la refuse pour l'instant avec un message clair.
- Messages d'erreur pendant l'exécution (« Subscript out of range in 30 ») : gardés en anglais comme le Locomotive pour comparer dans WinAPE ; une version française pourra venir avec les fichiers de langue.

## Outils
- Adoptée : ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80), pour voir où s'arrête l'écran du CPC. `MODE` existe maintenant (D046) : l'éditeur peut lire le MODE du programme (par exemple le premier `MODE n` écrit en nombre) pour placer la ligne à la colonne 20, 40 ou 80.
- Commande « Nouveau programme » (avec la confirmation de D043) ; ouverture d'un fichier par glisser-déposer.
- Commande « Mettre en forme » dans l'éditeur (remplace ? par PRINT, etc.).
