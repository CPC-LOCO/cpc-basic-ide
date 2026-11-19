# Idées à étudier

Une idée par ligne, avec la date. Quand une idée est décidée, elle passe dans le journal des décisions ; quand elle est faite, elle descend dans « Faites », avec le numéro de sa décision (rien n'est effacé). Ménage du 19 nov. 2026 : chaque ligne vérifiée dans le journal et dans le code.

## À faire

### Accessibilité (suites de D041)
- Essai avec un lecteur d'écran (Narrateur de Windows) : les noms sont posés mais jamais vérifiés. Annoncer le résultat d'une compilation sans que la personne cherche.
- Taille des boutons qui suit la taille du texte ? (aujourd'hui fixe : 48 points de haut).

### Messages d'erreur (suites de D042)
- Nommer la commande dans le message du guillemet manquant (erreur trouvée dans le lexer).
- Étendre l'aide « telle commande attend... » aux autres messages (UnexpectedToken, BlocksNotNested...).
- Passer les messages dans les fichiers de langue (français, anglais, espagnol).
- Messages d'erreur pendant l'exécution (« Subscript out of range in 30 ») : gardés en anglais comme le Locomotive pour comparer dans WinAPE ; une version française pourra venir avec les fichiers de langue.

### Langage et compilateur
- Commandes d'écran : vérifier les valeurs calculées (MODE, PEN, PAPER, INK, BORDER : « Improper argument » comme le Locomotive ?) ; limites de LOCATE selon le MODE (20, 40 ou 80 colonnes ; aujourd'hui 80 x 25 partout, D046) ; MODE avec effacement choisi. (WINDOW fait : D072 ; TAG : D075 ; GRAPHICS PEN : D106.)
- Calcul en 3D en fil de fer : multiplication en virgule fixe, racine carrée entière, projection. (SIN et COS par table : faits, ISIN et ICOS, D142.)
- OPTION EXPLICIT : déclaration obligatoire de toutes les variables.
- Multiplication et division par une puissance de 2 : décalages plus rapides.
- Tableaux d'octets (demande de l'utilisateur, D058) : `BYTE T` avant `DIM T(100)` déclarera un tableau dont les cases vont de 0 à 255 (moitié moins de mémoire). Aucune commande nouvelle (pas de DIMBYTE) : la façon d'écrire existe déjà, la compilation la refuse pour l'instant avec un message clair.
- Accélérer le dessin des lignes d'ELLIPSE (D141 : un rond par ELLIPSE est 3 fois plus lent que par CIRCLE) ; profiterait à tous les ovales.

### Commandes nouvelles déjà annoncées
- SPRITE, COLLIDE, SCROLL, SELECT et CASE, VROOM, BOOM, SHOT, FALL, WAVE : déjà colorées dans l'éditeur, à ne pas utiliser comme noms de variables (COMPATIBILITE-LOCOMOTIVE.md). STAR est faite (bibliothèque GRAPHIQUE, D151 ; reste le remplissage) ; POLYGON abandonnée (STAR dessine aussi les polygones, D150).
- `PAUSE [heure,][minute,] seconde` (PAUSE réservé à la gestion du temps) et `CLIC code_ascii` (idées de l'utilisateur, REPRISE.md).

### Outils et éditeur
- **Bibliothèques de commandes ajoutées par les programmeurs** (idée de l'utilisateur, 19 nov. 2026 ; répond aux questions ouvertes D015 et D028) : un fichier de bibliothèque (par exemple FORMES.LIB) déclare des commandes et des fonctions (nom, valeurs, limites, places vides, façon de les recevoir : cases mémoire comme CIRCLE ou registres comme ISIN) avec leur code Z80 en assembleur ; le programme dit quelles bibliothèques il utilise (leurs mots ne sont réservés que dans ce cas) ; liaison sélective comme aujourd'hui. Demande un assembleur Z80 dans le compilateur (lecture du texte ; la fabrication des octets existe dans Z80Emitter), puis un éditeur d'assembleur dans l'éditeur (couleurs, vérification, essai sur le mini-Z80). Ordre conseillé : format et assembleur, puis STAR et POLYGON comme premières bibliothèques, puis l'éditeur d'assembleur. Les commandes déjà faites restent dans le compilateur. Aujourd'hui, le code machine d'un programmeur passe par les RSX (D089).
- Livraison finale (D067) : programme d'installation Windows, signé (certificat de signature de code) pour ne pas être bloqué par les antivirus ; avant de le créer, l'utilisateur dira ce qu'il veut ajouter. Ensuite, peut-être une version Linux.
- Adoptée : ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80), pour voir où s'arrête l'écran du CPC. `MODE` existe maintenant (D046) : l'éditeur peut lire le MODE du programme (par exemple le premier `MODE n` écrit en nombre) pour placer la ligne à la colonne 20, 40 ou 80.
- Commande « Nouveau programme » (avec la confirmation de D043) ; ouverture d'un fichier par glisser-déposer.
- Commande « Mettre en forme » dans l'éditeur (remplace ? par PRINT, etc.).
- Messages d'incompatibilité (idée de l'utilisateur, D111, 17 nov. 2026) : un programme compilé marche toujours sur tous les CPC ; quand une commande ne peut pas marcher partout, le compilateur le dit (par exemple : mémoire en plus du 6128, commandes du CPC Plus ou de la GX4000, rappel que les fichiers vont sur la cassette sur un 464 sans lecteur de disquette). Aujourd'hui, aucune commande n'est dans ce cas.
- Barre de titre avec le dossier du fichier ouvert (essai de la 0.3.0, 18 nov. 2026 : la fenêtre « Ouvrir » de Windows reprend le dernier dossier et ne montre pas le début du chemin ; on ne voyait pas qu'on avait ouvert l'exemple du projet et non celui du paquet). Par exemple : « CPC BASIC IDE - formats.bas (E:\...\examples) ».

## Faites (gardées pour mémoire)
- **Fait (D038, D039)** — Fonctions de calcul : ABS, SGN, MIN, MAX (existent dans le Locomotive), HIBYTE, LOBYTE, CLAMP, RANDOM, RANDOMIZE.
- **Fait (D054, D058, D060, D062, D068)** — Conditions sans comparaison (IF a THEN), NOT, GOTO et GOSUB, tableaux, chaînes, réels.
- **Fait (D078)** — FOR avec limite et pas variables.
- **Fait (D080 : « Division by zero » comme le Locomotive, le programme continue ou ON ERROR)** — Erreur à l'exécution pour la division par zéro (aujourd'hui : résultat 0, sans message).
- **Fait (D076, lot H5)** — Éditeur de saisie complet pour INPUT et LINE INPUT, comme le Locomotive (rappel de l'utilisateur, 14 nov. 2026) : flèches gauche et droite dans la ligne, CLR (efface sous le curseur), curseur de copie déplacé avec SHIFT + flèches et touche COPY qui recopie le caractère placé sous lui. Aujourd'hui seuls les caractères, DEL et Entrée sont gérés. La saisie du vrai BASIC est dans sa ROM : il faut la refaire dans le module Input.
- **Fait (D115 à D122)** — Commandes du mode direct dans l'éditeur (D077, décision du 15 nov. 2026) : AUTO, LIST, EDIT, RENUM, DELETE, NEW, et CLEAR / MEMORY qui affichent leur rôle en BASIC. Détails dans le journal.
- **Fait (D131 à D138)** — Liste des variables (A fait : touche F4, D131 ; B fait : adresses, D132 ; C fait : commande DEBUG et réglage F2, D136 ; DEBUG A,B$ fait, D137 ; DEBUG T(3) fait, une seule case, D138 ; DEBUG T() pour tout le tableau abandonné sauf besoin) (demande de l'utilisateur, 18 nov. 2026 ; « on fera A, B et C, le choix permettra à chacun de s'y retrouver ») : A) dans un menu de l'éditeur, sans lancer le programme : nom, type (entier, à virgule, texte, octet, tableau et dimensions) et lignes où la variable est remplie ou lue ; B) la même liste avec l'adresse de chaque variable dans la mémoire du CPC (fournie par le compilateur), pour lire la valeur avec le débogueur de WinAPE ; C) voir les vraies valeurs pendant l'exécution, sur l'écran du CPC : commande ou touche nouvelle, à décider après l'inventaire (coûte les noms des variables et une routine, seulement si le programme s'en sert).
- **Fait (ELLIPSE, D139)** — Ovale avec un angle d'inclinaison (demande de l'utilisateur, 18 nov. 2026, après CIRCLE et la liste des variables) : largeur, hauteur et angle ; nom à choisir et à vérifier dans WinAPE.
- **Fait (D140 : vrai moniteur, ×69/64, sans option)** — Facteur de largeur des ronds (question de l'utilisateur, 18 nov. 2026, D130) : choisir entre « émulateur » (×1 en MODE 1, le choix actuel) et « vrai moniteur » (×1,08), soit par une commande dans le programme (nom à choisir, par exemple ASPECT), soit par un réglage de l'éditeur. À décider avec l'ovale, qui devra suivre le même facteur. (D139 : ELLIPSE suit le facteur de CIRCLE ; le réglage viendra pour les deux à la fois.)
- **Fait (Maj+F5, D134)** — Vérifier la compilation sans enregistrer le programme (remarque de l'utilisateur, 18 nov. 2026 : F5 enregistre toujours, ce qui gêne quand on veut seulement savoir si ça passe) : par exemple Maj+F5 « Vérifier sans enregistrer ».
- **Fait (Ctrl+Maj+S, D135)** — « Enregistrer sous » (remarque de l'utilisateur, 18 nov. 2026) : garder un programme ouvert (format.bas) et enregistrer ses changements sous un autre nom.
- **Fait (ISIN et ICOS, D142)** — Fonctions rapides en nombres entiers pour les angles (question de l'utilisateur, 19 nov. 2026, D141) : par exemple `ISIN(angle, échelle)` et `ICOS(angle, échelle)` (noms à choisir et à vérifier dans WinAPE), avec une table de sinus au degré près (environ 180 octets, peut-être partagée avec ELLIPSE) ; SIN et COS restent ceux du Locomotive (règle 100 % compatible). Mesuré le 19 nov. 2026 (essais\vitsin.bas, programme compilé, WinAPE à 108 %) : SIN et COS environ 14 ms chacun, une multiplication à virgule 0,9 ms, lecture d'un tableau de sinus en BASIC 0,6 ms (environ 25 fois plus rapide que SIN) ; une routine Z80 avec table serait, selon l'estimation de Claude, 30 à 60 fois plus rapide que SIN.
