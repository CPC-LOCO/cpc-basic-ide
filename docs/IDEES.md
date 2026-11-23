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
- SCROLL, VROOM, BOOM, SHOT, FALL, WAVE : déjà colorées dans l'éditeur, à ne pas utiliser comme noms de variables (COMPATIBILITE-LOCOMOTIVE.md). STAR est faite (bibliothèque GRAPHIQUE, D151 ; reste le remplissage) ; POLYGON abandonnée (STAR dessine aussi les polygones, D150).
- `PAUSE [heure,][minute,] seconde` (PAUSE réservé à la gestion du temps) et `CLIC code_ascii` (idées de l'utilisateur, REPRISE.md).

### Outils et éditeur
- **Bibliothèques de commandes ajoutées par les programmeurs** (idée de l'utilisateur, 19 nov. 2026 ; répond aux questions ouvertes D015 et D028) : un fichier de bibliothèque (par exemple FORMES.LIB) déclare des commandes et des fonctions (nom, valeurs, limites, places vides, façon de les recevoir : cases mémoire comme CIRCLE ou registres comme ISIN) avec leur code Z80 en assembleur ; le programme dit quelles bibliothèques il utilise (leurs mots ne sont réservés que dans ce cas) ; liaison sélective comme aujourd'hui. Demande un assembleur Z80 dans le compilateur (lecture du texte ; la fabrication des octets existe dans Z80Emitter), puis un éditeur d'assembleur dans l'éditeur (couleurs, vérification, essai sur le mini-Z80). Ordre conseillé : format et assembleur, puis STAR et POLYGON comme premières bibliothèques, puis l'éditeur d'assembleur. Les commandes déjà faites restent dans le compilateur. Aujourd'hui, le code machine d'un programmeur passe par les RSX (D089).
- Livraison finale (D067) : programme d'installation Windows, signé (certificat de signature de code) pour ne pas être bloqué par les antivirus ; avant de le créer, l'utilisateur dira ce qu'il veut ajouter. Ensuite, peut-être une version Linux.
- Adoptée : ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80), pour voir où s'arrête l'écran du CPC. `MODE` existe maintenant (D046) : l'éditeur peut lire le MODE du programme (par exemple le premier `MODE n` écrit en nombre) pour placer la ligne à la colonne 20, 40 ou 80.
- Commande « Nouveau programme » (avec la confirmation de D043) ; ouverture d'un fichier par glisser-déposer.
- Commande « Mettre en forme » dans l'éditeur (remplace ? par PRINT, etc.).
- Messages d'incompatibilité (idée de l'utilisateur, D111, 17 nov. 2026) : un programme compilé marche toujours sur tous les CPC ; quand une commande ne peut pas marcher partout, le compilateur le dit (par exemple : mémoire en plus du 6128, commandes du CPC Plus ou de la GX4000, rappel que les fichiers vont sur la cassette sur un 464 sans lecteur de disquette). Aujourd'hui, aucune commande n'est dans ce cas.
- Barre de titre avec le dossier du fichier ouvert (essai de la 0.3.0, 18 nov. 2026 : la fenêtre « Ouvrir » de Windows reprend le dernier dossier et ne montre pas le début du chemin ; on ne voyait pas qu'on avait ouvert l'exemple du projet et non celui du paquet). Par exemple : « CPC BASIC IDE - formats.bas (E:\...\examples) ».

### Cassette (suites de D178 à D180)
- Messages de la cassette (idée de l'utilisateur, 21 nov. 2026) : un « ! » devant le nom du fichier sur cassette (comme RUN"!NOM" ou LOAD"!NOM.BIN") supprime les messages du firmware (« Press PLAY then any key », « Loading... »). À étudier : dans le chargeur BASIC, pour RUN "nom" (la compilation en chaîne devra alors reconnaître le nom sans le « ! »), peut-être un réglage.
### Saisie limitée (idée de l'utilisateur, 21 nov. 2026, à lui rappeler quand on fera de nouvelles commandes)
- Une forme d'INPUT qui n'accepte que certains caractères : `INPUT "0123456789"+CHR$(13); "Votre âge : "; A$` (premier texte = les caractères permis, puis la question, puis la variable). `INPUT "Question : "; A$` reste la forme normale du Locomotive. Le premier texte peut être une expression (le Locomotive n'accepte qu'un texte écrit en clair après INPUT, donc pas de conflit avec ses programmes).
- Sur CPC : les autres touches sont ignorées pendant la saisie. Sur la cartouche GX4000 : la saisie à la manette (D184) ne fait défiler que ces caractères.
- À préciser avec l'utilisateur : le rôle de CHR$(13) dans la liste, LINE INPUT, la forme exacte de l'écriture.

### Cartouche GX4000 (suites de D181 à D196)
- Nombres à virgule sur la console (faits, D196) : encore plus rapides si besoin. Pistes : multiplication par une table de carrés (a x b = ((a + b)^2 - (a - b)^2) / 4), SIN et COS sans division (x 2 / pi), polynômes plus courts.
- Place libre dans la cartouche (question de l'utilisateur, 22 nov. 2026) : 128 Ko au moins, 512 Ko au plus, mais le programme est limité à 30 464 octets (il est recopié dans les 64 Ko de la console). Le reste pourrait garder des écrans dessinés ou de la musique, lus dans la cartouche au moment voulu (rejoint « écrans et musique »).
- **Plusieurs programmes dans une cartouche** (demande de l'utilisateur, 22 nov. 2026 : « à quoi sert d'avoir 512 Ko si on ne peut en utiliser que 64 ? ») : `RUN "nom"` et `CHAIN` (RUN "nom" : fait, D207 ; CHAIN reste refusé, D086) iraient chercher le programme suivant dans les pages de la cartouche au lieu de la disquette, comme les vrais jeux qui branchent une page de 16 Ko à la demande. Un jeu en plusieurs parties (menu, niveaux...) pourrait remplir toute la cartouche ; aucune commande nouvelle, et le même programme marche sur disquette. À faire bientôt. Ensuite, peut-être : des données (écrans, sprites, musique) rangées dans la cartouche et lues au moment voulu (commandes nouvelles à proposer d'abord).
- Traits plus rapides sur la console : faits (D202 tout droits, D203 en biais). Reste éventuellement : un trait en biais dont un bout dépasse de la fenêtre garde le chemin lent (on pourrait le couper aux bords d'abord).

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

- **Assembleur seul : fait (D163, D164).** Restent : plusieurs ORG, READ / INCLUDE, écrire les cases d'une commande à valeurs depuis l'assembleur, ORG au-dessus de &A67B (écran). Idée d'origine (question de l'utilisateur, 20 nov. 2026, après la 0.5.0) : un menu qui ouvre une fenêtre d'édition libre (assembleur Z80), avec enregistrer / ouvrir un .asm et compiler vers une disquette (.dsk). ORG obligatoire au début (adresse de chargement) ; adresse de lancement : celle de ORG ou un mot comme ENT (à vérifier dans WinAPE) ; plus tard plusieurs ORG, READ / INCLUDE. Ce qui existe déjà : l'assembleur (D145), la fenêtre d'édition (D159), la disquette et le chargeur BASIC. Environ 3 ou 4 étapes.

- **Mémoire sous &4000 et chargeur BASIC** (questions de l'utilisateur, 20 nov. 2026) : de &0170 à &2FFF, rien du compilateur une fois le programme lancé (seulement le petit chargeur BASIC) ; &3000 à &3FFF : fichiers et RUN "nom" seulement. **Zone réservée au programmeur : faite (D167)**, de &0400 à &2FFF. Idée de l'utilisateur : se passer du chargeur BASIC (le programme ne revient jamais au BASIC) : RUN"NOM" d'AMSDOS lance aussi un fichier binaire qui a son adresse de lancement ; **fait pour l'assembleur seul (D166)** : vérifié dans WinAPE ; le programme ne doit jamais finir par RET (le CPC redémarrerait). **Fait aussi pour les programmes BASIC compilés (D168)** : ils ne revenaient déjà jamais au BASIC ; sans chargeur, ils rebranchent la disquette au départ. Reste : essayer sur 464 et 664.
- **Pages d'écran, musiques et autres données** (idée de l'utilisateur) : comment les lire depuis un programme (LOAD"ECRAN",&C000 existe déjà, D085) ; un « compilateur d'écran » (image vers écran CPC, avec la palette) et un « compilateur de musique » (par exemple vers un lecteur de musique en assembleur) ; les données pourraient aller sous &4000.

- **Bibliothèque TEXTE : TYPO et DEFTYPO** (idée de l'utilisateur, 20 nov. 2026, d'après sa RSX TYPO de 2020) : petits caractères de 4×8, deux par caractère du CPC (40 colonnes en MODE 0, 80 en MODE 1). `TYPO texte [, x, y]`, `DEFTYPO "x", xx, xx, xx`. Étape 1 faite : valeur TEXTE (D169) ; étape 2 faite : TYPO et `typo.bas` (D170) ; DEFTYPO fait (D171) ; écriture directe dans l'écran (D172, D173). **Terminé.**

- **Division de deux entiers** (vu le 20 nov. 2026, D172) : `ROUND(T1/T2,1)` avec T1 et T2 entiers affiche 2 au lieu de 2.1 ; le Locomotive donne un nombre à virgule pour « / ». **Tranché (D174) : on garde, la vitesse d'abord** ; astuce CREAL(A)/B dans le LISEZMOI.

- **Affichage rapide du texte normal** (idée de l'utilisateur, 20 nov. 2026, après TYPO) : un écran plein en MODE 1 par PRINT prend plus de 3 secondes ; l'utilisateur a écrit une RSX plus rapide (MODE 0, 1 et 2). Commande FPRINT texte$ écrite (D175), puis **mise de côté** dans `essais\fprint\` : les nouvelles commandes viendront quand tout le reste sera terminé (choix de l'utilisateur).

- **Simulateur Z80 complet pour les tests : fait (D176)** (idée de l'utilisateur, 21 nov. 2026) : MiniZ80 a été complété au fil des besoins ; mettre d'un coup toutes les instructions du Z80 (avec CB, ED, DD, FD, DDCB, FDCB) et tous les indicateurs (S, Z, H, P/V, N, C).
