# Reprise du projet CPC BASIC IDE dans une nouvelle conversation

Ce fichier sert à reprendre le travail sans rien perdre. À envoyer en premier dans la nouvelle conversation (ou à faire lire par Claude dans le dépôt, voir chapitre 6). Il est à tenir à jour : à la fin de chaque étape, remplacer le chapitre 3.

## 1. Le projet en quelques lignes

Un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. L'éditeur (Avalonia, C#, .NET 10) tourne sur Windows. Le compilateur transforme le programme en code Z80 et l'écrit sur une disquette `.dsk`, qu'on essaie dans l'émulateur WinAPE (ou SugarBox) avec `RUN"NOM"`.

Dépôt (public) : https://github.com/CPC-LOCO/cpc-basic-ide
Dossier de travail : `E:\projets amstrad ide\cpc-basic-ide`
Auteur des commits : RetroPoke (pseudo de l'utilisateur sur les forums Amstrad).
Documents de référence :
- `docs\ARCHITECTURE.md` : comment c'est fait ;
- `docs\JOURNAL-DECISIONS.md` : toutes les décisions, D001 à D177 ;
- `docs\COMPATIBILITE-LOCOMOTIVE.md` : la liste de tous les mots du Locomotive BASIC et leur état, avec l'ordre de travail par lots (A à J) ;
- `docs\PROGRAMME-DE-REFERENCE-RSX.md` : le programme de l'utilisateur (commandes RSX en binaire) qui doit un jour se compiler tel quel ;
- `docs\IDEES.md` : idées en attente ;
- `.github\TESTEURS.md` : aide pour les testeurs (astuce F8 et `FOR K%` ajoutée le 17 nov. 2026).

## 2. Comment travailler avec l'utilisateur (important)

- **Langue :** français, avec des mots simples. L'utilisateur n'aime pas le jargon (« charabia ») : expliquer pas à pas, en disant exactement quoi cliquer ou taper.
- **Demander un fichier :** toujours donner son chemin complet depuis la racine du dépôt (par exemple `src\CpcBasic.Core\Language\Parser.cs`). Attention aux noms exacts : le code de la fenêtre s'appelle `MainWindow.axaml.cs` (l'envoi le renomme parfois en `MainWindow_axaml.cs`).
- **Livrer un fichier :** si Claude a accès au dossier du dépôt, écrire directement dedans. Sinon, donner un tableau avec la destination de chaque fichier.
- **Accessibilité :** l'utilisateur est attentif aux personnes mal voyantes. Éviter « cliquez sur le petit bouton » ; préférer le clavier et les commandes écrites.
- **Honnêteté :** dire clairement ce que Claude a pu faire ou non (compiler, lancer les tests, pousser sur GitHub). Si Claude ne peut pas lancer `dotnet test`, l'utilisateur le lance et colle le résultat.
- **Ne rien inventer :** ne pas créer de commande ni de mot réservé sans demande de l'utilisateur ; proposer d'abord. `PAUSE` est réservé à la gestion du temps (`PAUSE [heure,][minute,] seconde`, plus tard). Idée notée : `CLIC code_ascii`.
- **Pauses :** quand l'utilisateur dit qu'il revient plus tard (« j'en ai pour 10 minutes »), Claude s'arrête et ne corrige rien tant qu'il n'a pas dit « je suis là, on continue » : ses essais peuvent ne pas être finis.
- **Méthode d'une étape :**
  1. rappeler la conception en peu de mots ; vérifier les mots-clés dans WinAPE (taper `MOT=5` : s'il n'y a pas de `Syntax error`, le mot est libre) ;
  2. livrer le code, les tests xUnit et un petit programme d'exemple dans `examples\` (nom de 8 caractères au plus) ;
  3. l'utilisateur lance `dotnet test`, compile l'exemple avec F5 dans l'éditeur (il compile vers un répertoire provisoire), le lance dans WinAPE ;
  4. Claude donne à l'avance l'écran attendu ; l'utilisateur confirme ;
  5. écrire l'entrée de journal (D0xx), mettre à jour `docs\COMPATIBILITE-LOCOMOTIVE.md`, puis commit : double-clic sur `packaging\commit.cmd` (tests, liste des fichiers, message, push). Les programmes d'essai de l'utilisateur vont dans `essais\`, jamais dans `examples\`. Pas de longues séries de commandes à copier.
- **Push :** après chaque commit, Claude s'occupe aussi de l'envoi sur GitHub. S'il ne peut pas (réseau refusé), le dire et donner la marche à suivre (`packaging\commit.cmd` propose le push).
- **Accès de Claude (constaté le 13 nov. 2026) :** quand l'application Claude de l'ordinateur est ouverte et le dossier du dépôt ajouté, Claude lit et écrit directement les fichiers. Il ne peut ni lancer `dotnet test` (pas de .NET de son côté), ni faire le commit (git doit effacer des fichiers temporaires, ce qui lui est interdit sans permission) : l'utilisateur garde `packaging\commit.cmd`. Sans ce branchement, Claude peut toujours lire le dépôt public sur GitHub.
- **Programmes d'exemple :** (règle du 18 nov. 2026, D130) attendre la **barre d'espace** avant chaque changement d'écran et à la fin, avec le texte « APPUYEZ SUR ESPACE » : `WHILE INKEY(47):WEND:WHILE INKEY(47)=0:WEND` (la seconde boucle attend que la touche soit relâchée). Sinon le tampon du clavier fait passer les écrans trop vite pour les capturer.
- **Une chose à la fois (16 nov. 2026) :** l'utilisateur fait ses essais pas à pas ; ne demander qu'une action à la fois (« Prochaine étape, une seule : … ») et ne pas enchaîner plusieurs demandes dans le même message. **Rappel du 18 nov. 2026 (« tu vas trop vite ») :** quand l'utilisateur envoie deux messages proches, attendre sa réponse claire avant de changer le code une deuxième fois.
- **Références :** avant chaque étape, faire lancer un petit programme par le vrai BASIC (SugarBox ou WinAPE) et relever l'écran ; les tests et l'exemple doivent donner le même résultat. Depuis D101, mieux que de le faire taper : le mettre dans un fichier .bas, compiler avec le réglage F11 sur « oui », puis `LOAD"NOM.TXT"` et `RUN` dans l'émulateur.
- **Lire les tests :** l'utilisateur lance `dotnet test > essais\tests.txt` ; Claude lit ce fichier (lignes « réussite », « error CS », « Échoué »).
- **Journal :** on n'efface jamais une entrée ; si une décision change, on en ajoute une nouvelle qui renvoie à l'ancienne.
- **Mots réservés :** chaque nouveau mot-clé empêche d'utiliser ce nom comme variable ; à signaler et à noter dans le journal.
- **La forme exacte (21 nov. 2026) :** quand l'utilisateur donne l'écriture d'une commande, la reprendre **exactement**, sans valeur en plus. Erreur à ne pas refaire : il avait écrit « FPRINT texte$ (plus de paramètres) » = plus aucune valeur, et Claude avait ajouté `[, col, lig]` (D175). En cas de doute, demander.
- **Plus de nouvelles commandes pour l'instant (21 nov. 2026) :** « ajouter des commandes se fera après que tout soit terminé ». Ne pas proposer de nouvelles commandes de bibliothèque (FPRINT attend dans `essais\fprint\`).
- **La vitesse d'abord (D174) :** à choix égal, garder ce qui est rapide (exemple : « / » entre deux entiers reste une division entière).
- **Simulateur des tests :** `MiniZ80` connaît tout le Z80 (D176) ; un test qui échoue vient donc du code testé ou du test, plus d'une instruction manquante.
- **Commandes à taper :** l'utilisateur tape les commandes dans une fenêtre CMD ouverte dans le dossier du dépôt ; donner la ligne exacte à coller (par exemple `packaging\build-release.cmd`). Attention aux redirections `>` : `dotnet test > essais\tests.txt` seulement (le 21 nov. 2026, `build-release.cmd` avait été écrasé par une sortie de tests, D177).

## 3. État au moment de la reprise (21 novembre 2026, matin)

- **Version 0.6.0 publiée** (D177) : paquet `release\CpcBasic-0.6.0-win-x64.zip`, SHA256 25cefaff6132f91416253f66db2d694a1fa043ac3d05fd1b9ffdbbb3e01e055e, essayé par l'utilisateur (typo.bas compilé depuis le paquet). Notes : `docs\RELEASE-NOTES-0.6.0.md`. **Publiée** le 21 nov. 2026 : pre-release GitHub v0.6.0, commit 250ab58.
- **Depuis la 0.5.0 :** assembleur seul (D163, D164, D166) ; extension `.cmd.asm` définitive (D165) ; zone réservée au programmeur &0400 à &2FFF (D167, test `ProgrammerZoneTests`) ; programmes BASIC sans chargeur (menu Options, D168) ; valeur TEXTE dans les bibliothèques (D169) ; bibliothèque TEXTE : TYPO et DEFTYPO (D170 à D173) ; place des commandes dans la fenêtre des bibliothèques (D172) ; division entière gardée, astuce CREAL (D174) ; FPRINT écrit puis mis de côté (D175) ; Z80 complet pour les tests (D176).
- **Après la 0.6.0 : sortie cassette (D178)** : F5 écrit aussi NOM.cdt ; essayée dans Caprice (464) et WinAPE (6128). Sans chargeur sur cassette fait (D179) ; RUN "nom" sur cassette fait (D180) : **sortie cassette terminée** (pas d'essai sur un vrai CPC : l'utilisateur n'en a pas). **Cartouche GX4000 : chemin A choisi** (notre propre système, aucune ROM d'Amstrad, D181) ; étape 1 faite : fichier .cpr et cartouche d'essai (4 bandes de couleur), essayée dans SugarBox en 6128 PLUS. Étape 2 faite (D182) : notre jeu de caractères et le texte (PRINT, LOCATE, MODE, CLS) ; commande `dotnet run --project src\CpcBasic.Cli -- --gx4000 programme.bas sortie.cpr`, essai dans SugarBox (6128 PLUS, « Default cartridge »). Étape 3 faite (D183) : les manettes et la table des touches (tir 1 = espace, tir 2 = ENTRÉE, flèches ; autres touches d'INKEY placées par le compilateur, avec un message) ; cartouches de 128 Ko ; essais des cartouches dans **AMSpiriT** (manette au clavier). Étape 4 faite (D184) : saisie à la manette pour INPUT et LINE INPUT (haut / bas lettre, tir 2 groupe, droite écrit, gauche efface, tir 1 valide ; tir 1 = bouton principal, bit 4). Étape 5 faite (D185) : **Ctrl+F5** (menu Compiler, « Cartouche GX4000 ») fait NOM.cpr depuis l'éditeur. Étape 6 faite (D186) : les couleurs (PEN, PAPER, INK sans clignotement, BORDER). Étape 7 faite (D187) : le cœur du dessin (PLOT, DRAW, MOVE, TEST, CLG, ORIGIN, GRAPHICS PEN / PAPER ; BOX, CIRCLE, ELLIPSE marchent). Étape 8 faite (D188) : la fin du dessin (XOR / AND / OR, fenêtres graphiques, MASK, FILL, PAINT, FRAME) : **dessin complet**. Étape 9 faite (D189) : programmes de plus de 16 Ko (jusqu'à 30 976 octets, recopiés page après page). Étape 10 faite (D190) : les fenêtres de texte (WINDOW, flux #0 à #7, WINDOW SWAP). Étape 11 faite (D191) : les encres clignotantes (INK et BORDER à deux couleurs, SPEED INK, changées au retour du balayage). Étape 12 faite (D192) : le son, étape 1 (SOUND sur 3 canaux, bruit, files de 4 sons, SQ). Étape 13 faite (D193) : les enveloppes ENV et ENT. Étape 14 faite (D194) : TIME, AFTER, EVERY, ON SQ GOSUB ; les nombres à virgule sont refusés sur la cartouche (ROM du CPC). Étape 15 faite (D195) : sons retenus, RELEASE, rendez-vous : **son complet**. Étape 16 faite (D196) : les nombres à virgule (nos propres calculs). **Console complète.** **Version 0.7.0 publiée** (D197, pre-release GitHub v0.7.0, commit fed48dd). **Sprites** : étape 1 faite (D198 : DEFSPR, SPRITE n, x, y, SPRITE n), étape 2 faite (D199 : au point près) ; étape 3 faite (D200 : COLLIDE), étape 4 faite (D201 : sprites qui se croisent, le plus grand numéro devant) : **sprites terminés** ; sur cartouche, les sprites vont bien. Traits rapides sur cartouche : tout droits (D202) et en biais (D203) ; DRAW et DRAWR rapides aussi sur CPC (D204, notre module au lieu de la ROM ; D205 : écriture directe dans l'écran quand le mode d'écriture ne change jamais : illusion.bas 25,6 s au lieu de 58 à 63 s). **SELECT CASE** fait (D206). **RUN "nom" sur la cartouche** fait (D207 : programmes rangés dans la cartouche, CAS IN OPEN / DIRECT de notre système) ; **LOAD "nom",adresse sur la cartouche** fait (D208 : fichiers du dossier rangés dans la cartouche, en-tête AMSDOS lu) ; **Fichiers de LOAD aussi sur la disquette** (D209, F5 ; fichiers de plus de 16 Ko acceptés sur la disquette).
- **Tests :** 2151, tous réussis (23 nov. 2026). **FALL** fait (D217 ; mot réservé ; 1 chute puis le sol, 2 vie perdue ; ENV 2 et 3, ENT 4 et 5). **VROOM** fait (D213 ; mot réservé ; ENV 12 à 15, ENT 11 à 15). **BOOM** fait (D214 ; mot réservé ; ENV 9 et 10, ENT 8 à 10). **SHOT** fait (D215 ; mot réservé ; ENV 4 à 8, ENT 6 et 7 ; numéros d'enveloppes : D216, un programme ne perd que ceux des effets qu'il utilise, numéro pris refusé avec les numéros libres). **MOVE et MOVER rapides avec PLOT** (D212). **Fichiers de LOAD aussi sur la cassette** (D211). **PLOT et PLOTR rapides sur CPC** (D210 : 1,7 fois plus rapides ; SYNC et FLUSH ajoutés par le compilateur).
- **Idées en attente (IDEES.md) :** sortie cassette, cartouche GX4000, écrans et musique, sprites ; FPRINT et les autres commandes de bibliothèque, quand tout le reste sera terminé.

### Bibliothèques de commandes (19 et 20 nov. 2026, D144 à D161, publiées dans la 0.5.0)

- **Idée de l'utilisateur (D144) :** les programmeurs ajoutent leurs commandes et fonctions, écrites en assembleur, rangées par bibliothèque ; le compilateur ne prend que celles utilisées.
- **Fait :** assembleur Z80 style WinAPE (`src\CpcBasic.Core\Assembly\Z80Assembler.cs`, D145) ; fichier d'une commande `NOM.cmd.asm` (extension .cmd.asm, définitive, D165) : fiche en lignes « ;@ » (NOM, GENRE, VALEUR, OPTION facultative, AIDE, AUTEUR) puis le code (D146, D150) ; catalogue des deux dossiers (projet : `bibliotheques\` du dépôt, copié à côté de CpcIde.exe, protégé ; programmeur : `Documents\CPC BASIC IDE\bibliotheques`, D147, D149) ; le compilateur cherche les mots inconnus au catalogue, range les valeurs dans leurs cases, ajoute la commande et les routines qu'elle appelle (D148) ; éditeur : commandes colorées (D154), fenêtre des bibliothèques à onglets Maj+F4 (D155, D156), questionnaire « Nouvelle commande » avec repère d'écriture en BASIC (D157, D158), éditeur d'assembleur avec Vérifier F5 et Enregistrer Ctrl+S, « Voir le code » pour les commandes protégées (D159), « Modifier la fiche » et « Effacer » (D160). Valeur TEXTE : `;@TEXTE NOM, rôle`, la case reçoit l'adresse de la fiche du texte (D169). Bibliothèque TEXTE : TYPO texte [, x, y], écriture directe dans l'écran (2,6 à 6,4 fois plus vite que par le firmware, D172, D173), et DEFTYPO "x", l1, l2, l3 (petits caractères 4×8 de l'utilisateur, D170, D171 ; exemple `typo.bas`). Fenêtre des bibliothèques : place de chaque commande (D172).
- **Première commande du projet :** STAR (bibliothèque GRAPHIQUE, `bibliotheques\GRAPHIQUE\STAR.cmd.asm`) : `STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle]`, 2 à 16 pointes, contour et intérieur, essayée dans WinAPE (examples\etoiles.bas ; D150 à D153). Vérifiée avec un simulateur Python du texte assembleur (`essais\star\`). Taille : 2174 octets. POLYGON abandonnée (STAR fait les polygones).
- **Répertoire de distribution fait (D161)** : menu Fichier, « Préparer un dossier à distribuer... ». Étapes B1 à B5 toutes faites.
- **Reste :** autres bibliothèques du projet (texte, son...) ; idée « Assembleur seul » (IDEES.md).
- **Assembleur seul (D163, D164, D166, après la 0.5.0) :** menu Assembleur de l'éditeur : fenêtre pour écrire un programme entièrement en assembleur (.asm), ORG obligatoire (&0400 à &A67B), RUN (lancement), WRITE acceptée sans effet, routines des bibliothèques appelées par leur nom ; F5 écrit NOM.dsk (NOM.BIN et chargeur NOM.BAS ; ou NOM.BIN seul si la case « Avec chargeur BASIC » est décochée : lancé directement, il ne doit jamais finir par RET, D166) ; essayé dans WinAPE.
- **Tests :** 1856, tous réussis (20 nov. 2026) ; 1904 le 21 nov. 2026.

### État au 16 novembre 2026 (pour mémoire)

- **Version publique :** 0.6.0 (pre-release GitHub v0.6.0, 21 nov. 2026, commit 250ab58 ; D177) ; avant : 0.5.0 (pre-release GitHub v0.5.0, Windows 64 bits, 20 nov. 2026, commit 885ded1 ; D162) ; avant : 0.4.0 (19 nov. 2026, D143). Le numéro de version vient de `Directory.Build.props` (racine) et de `VERSION` dans `packaging\build-release.cmd` : garder les deux identiques.
- **Dernier commit sur GitHub :** « Version 0.4.0 (D143) » (19 nov. 2026, **1673 tests**, tous réussis).
- **Règle de l'utilisateur :** 100 % compatible avec le Locomotive BASIC ; finir l'inventaire avant toute commande nouvelle (CIRCLE, ELLIPSE, STAR, POLYGON, 3D… attendent).
- **Grande étape du 16 nov. 2026 :** le programme de référence de l'utilisateur (`docs\PROGRAMME-DE-REFERENCE-RSX.md`) **se compile et donne le bon dessin** dans WinAPE (D089).

### Fait et validé (résumé ; le détail est dans le journal)

- **Lots A à H** : déroulement, saisie, tableaux, DATA, opérateurs, textes, nombres à virgule, son, fenêtres, PRINT (zones, TAB, SPC, WRITE, USING), SYMBOL, TAG, édition de la ligne d'INPUT (D054 à D076). FOR avec limite et pas calculés (D078).
- **Lot I (interruptions et erreurs, D079 à D083)** : AFTER, EVERY, REMAIN, DI, EI ; ON ERROR GOTO, RESUME (3 formes), ERROR, ERR, ERL ; « Break in N » de STOP, « Unexpected RETURN » ; ON SQ GOSUB ; ON BREAK CONT / STOP / GOSUB et **réglage F9** de l'éditeur (touche ESC comme le CPC, par défaut, ou ignorée).
- **Lot J (disquette et cassette, D084 à D087)** : OPENOUT, OPENIN, PRINT #9, WRITE #9, INPUT #9, LINE INPUT #9, EOF, CLOSEOUT, CLOSEIN ; SAVE "nom",B,… ; LOAD ; CAT ; SPEED WRITE ; RUN "nom" (lance un autre programme compilé) ; CHAIN et MERGE refusés (écart voulu) ; **compilation en chaîne** (idée de l'utilisateur, D087 : F5 compile aussi les programmes lancés par RUN "nom" et les met sur la même disquette).
- **Lot K (terminé)** : K1 TRON / TROFF (D088) ; K2 commandes RSX `|NOM,valeurs` et CALL avec valeurs (D089, règle : A = nombre de valeurs, IX = liste, **DE = dernière valeur**) ; K3 CLEAR, RUN, RUN ligne (D090) ; K4 XPOS, YPOS, TEST, TESTR, SPEED INK, SPEED KEY (D091) ; K5 KEY, KEY DEF (D092) ; correction : LINE INPUT affiche « ? » comme INPUT (D093) ; K6 HIMEM, FRE, UNT, WAIT, et adresses calculées à virgule de -32768 à 65535 (MEMORY HIMEM-1000, D094) ; K7 DEF FN et FN (D095 : valeurs locales cachées, calcul recopié à chaque appel ; message avec la solution quand une virgule serait perdue) ; K8 ERASE (D096 : place rendue, tableaux suivants déplacés, textes compris) ; K9 TIME complet avec F8 et ON x GOTO hors de 0 à 255 (D097) ; K10 PRINT USING F$ (D098 : format lu à l'exécution) ; K11 DERR (D099) ; K12 imprimante, PRINT #8, WIDTH, POS(#8) (D100). **Lot K terminé.**
- **Réglage F11 (D101)** : « Texte sur la disquette » : F5 ajoute aussi NOM.TXT (texte du programme) ; dans l'émulateur, `LOAD"NOM.TXT"` puis `RUN` lance le vrai BASIC. Sert aux références : plus besoin de recopier les programmes dans SugarBox ou WinAPE.

### Prochaine étape

Reste à faire (voir `docs\COMPATIBILITE-LOCOMOTIVE.md`, lignes « À faire » et « Partielle ») :
- **Lot L (terminé) : BASIC 1.1.** L1 FRAME, CURSOR, COPYCHR$ (D102), L2 DEC$ (D103), L3 ORIGIN avec fenêtre graphique et PEN transparent (D104) faits. L4a GRAPHICS PEN / PAPER (D106) et L4b FILL et PAINT (D107, D108 ; accélérés en D109 : deux fois plus rapides que le FILL du 6128) faits. L4c MASK et GRAPHICS PEN stylo, mode (D110 : SCR WRITE détourné, points exactement ceux du firmware) fait. **Lot L terminé.** Nos propres routines (D105) : lecture et écriture directes de l'écran et calcul des lignes voisines (D109), réutilisables pour CIRCLE, POLYGON…
- **Un caractère = un code de la ROM choisie, partout (D133) :** l'éditeur montre ce qu'afficherait le CPC du pays choisi (CPC français : à pour @, ù pour |, ç pour \\) ; le compilateur lit le code (ài = @i, ùDISC = |DISC). **Pour en parler à l'utilisateur : décrire ce qu'on voit à l'écran, pas les codes internes.**
- **Lettres accentuées (D126, D127, faites) :** table relevée dans les ROM (français par défaut, anglais), F7 dans l'éditeur ; les textes montrent ce que le CPC affichera. ROM de l'utilisateur dans essais\roms (jamais publiées). D'autres pays plus tard (https://www.cpcwiki.eu/index.php/ROM_List).
- **Inventaire du Locomotive :** plus aucune remarque « Partielle » (18 nov. 2026) ; POKE et OUT avec valeur calculée hors de 0 à 255 : Improper argument (D128) ; commandes AMSDOS essayées dans un programme compilé, identiques au vrai BASIC (D129, 1520 tests) ; **pas de version 0.4.0 pour l'instant** (choix de l'utilisateur : plus d'essais d'abord) ; CURSOR ,1 et MASK ,1 sans première valeur faits (D123) ; @ avec CALL et RSX vérifié, « Faite » (D124) ; MEMORY « Memory full » comme le CPC, « Faite » (D125, 1467 tests) ; MASK vérifié sur un 464 (D110, 18 nov. 2026 : identique au 6128).
- **Réglage F7 retiré (D111)** : un programme compilé marche toujours sur tous les CPC ; messages d'incompatibilité notés dans IDEES.md.
- **Remodelage de l'éditeur (en cours)** : étape 1 faite, barre de menus Fichier / Compiler / Options / Aide à la place des boutons, F1 = raccourcis (D112). Étape 2 faite : barre d'état lisible, panneau des messages réglable, lignes longues à la ligne comme le CPC (D113). Étape 3 faite : icônes ✓/✗ devant les messages (D114). Suite possible : se souvenir de la hauteur du panneau.
- **Commandes de l'éditeur (D077)** : étape 1 faite (D115) : NEW, EDIT et les explications de CLEAR, MEMORY, CONT, LOAD, SAVE, RUN, MERGE, CAT, CHAIN, AMSDOS, tapées sans numéro puis Entrée. Étape 2 faite : AUTO (D116). RENUM fait (D117). Numéro seul = effacer la ligne, avec confirmation ; numéro déjà pris = remplacer ? (D118). Protection : un problème imprévu ne ferme plus l'éditeur, détail dans %APPDATA%\CpcBasicIde\erreurs.txt (D119). DELETE fait (D120). Menu Édition (D121). LIST fait (D122) : **toutes les commandes de l'éditeur (D077) sont faites.**
- Idée notée (D087) : mettre aussi sur la disquette les fichiers binaires prêts du dossier (LOAD "ECRAN" -> ECRAN.BIN). **Faite (D209).**

### Questions en attente (à reposer au bon moment)

- `INPUT` avec une ligne vide : que fait exactement le Locomotive ? (D055)
- Coordonnées des commandes ajoutées en MODE 0 : x de 0 à 159 (à confirmer).
- Gestion de projets (plusieurs programmes, une disquette commune) : à reparler.

### En attente jusqu'à la fin des lots

- **CIRCLE : fait (D130)**, largeur ×0,5 / ×1 / ×2 selon le MODE (le ×0,6 / ×1,2 / ×2,4 noté avant était faux). Toute commande à écriture directe doit couper la ROM haute comme `rt_box` (D048). Méthode à reprendre pour les formes suivantes : routine écrite une fois dans essais\circle\routine.py, vérifiée en Python contre une référence, puis code C# tiré de cette description.
- **Liste des variables :** A faite (F4, D131), B faite (adresses, D132), C faite (commande DEBUG et réglage « Mise au point » F2, D136) ; `DEBUG A,B$` fait (ordre écrit, D137) ; `DEBUG T(I)` fait (une case, vrai numéro, D138). Liste des variables terminée.
- **ELLIPSE : fait (D139)**, ovale droit ou incliné, même méthode de travail que CIRCLE (essais\ellipse\). Facteur de largeur : vrai moniteur Amstrad, ×69/64, sans option (D140). Réglage **F3 « Priorité : vitesse / place »** (D141) : avec « place » et ELLIPSE dans le programme, CIRCLE passe par ELLIPSE (mêmes points) ; règle pour les commandes à venir. **ISIN et ICOS faits (D142)** : sinus et cosinus rapides en entiers, table commune avec ELLIPSE (module SinTable).
- `PAINT`, ELLIPSE, STAR, POLYGON, calcul 3D en fil de fer (SIN et COS par tableau de 256 angles, virgule fixe, racine carrée entière, projection).
- Ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80).
- `PAUSE [heure,][minute,] seconde`, `CLIC code_ascii`.

## 4. Avant la prochaine publication

- D016 : vérifier qu'aucune ROM Amstrad n'est livrée.
- D017 : relecture juridique de l'exception de liaison (`docs\RUNTIME-EXCEPTION.md`).
- **Version 0.3.0 préparée (18 nov. 2026)** : LISEZMOI refait, TESTEURS.md mis à jour, version 0.3.0 aux deux endroits, `docs\RELEASE-NOTES-0.3.0.md` ; le paquet contient tous les exemples et COMPATIBILITE.md. Paquet construit et essayé par l'utilisateur (version 0.3.0 affichée, ouverture et compilation OK). **Publiée sur GitHub (pre-release v0.3.0, 18 nov. 2026).** Ensuite : CURSOR ,1 et MASK ,1 sans première valeur (fait, D123), @ fait (D124), MEMORY fait (D125), lettres accentuées faites (D126, D127). **Inventaire du Locomotive terminé** : plus aucun mot « À faire » ni « Partielle » dans COMPATIBILITE-LOCOMOTIVE.md (seulement des « Écart voulu »). CIRCLE fait (D130). **Suite décidée par l'utilisateur : la liste des variables A, B, C (IDEES.md), puis l'ovale incliné.**

## 5. Points techniques à connaître

- Chaîne du compilateur : `Lexer` (mots et symboles) -> `Parser` (instructions, expressions, vérifications) -> `CodeGenerator` (octets Z80 avec `Z80Emitter`) -> `Compiler` (enchaîne tout, vérifie la limite de `MEMORY`, écrit la disquette).
- Les modules Z80 sont écrits en C# dans `RuntimeLibrary.cs` ; chacun déclare ses dépendances, seuls les modules utilisés sont liés (liaison sélective). Modules actuels : PrintString, PrintInt, Input, Mul16, Div16, Abs16, Sgn16, Min16, Max16, Rand16, Randomize, RandomRange, Pixel, Box, Array (D058), RunError et Data (D059).
- Tableaux (D058) : un descripteur par tableau à la fin du binaire (adresse des cases, puis nombre de cases par dimension) ; les cases sont prises au lancement après le programme (`rt_heap_start`), jusqu'à &A67B ou MEMORY. Erreurs à l'exécution : module commun `RunError` (texte du Locomotive + « in » + numéro de ligne rangé dans `rt_err_line`), puis attente d'une touche et redémarrage (D059). `AstWalker` (dans `Ast.cs`) parcourt les expressions d'une instruction.
- **Sans chargeur BASIC (D168) :** menu Options, case « Chargeur BASIC sur la disquette » ; décochée : NOM.BIN seul, et le programme rebranche la disquette au départ (KL ROM WALK si &BC77 vaut encore &CF).
- **Zone du programmeur (D167) :** de &0400 à &2FFF, le compilateur ne met jamais rien (test `ProgrammerZoneTests`) ; &3000 à &3FFF libre sauf fichiers et RUN"nom".
- Le programme compilé commence en &4000. Expressions sur 16 bits, résultat dans `HL`. Variables réservées à la fin du binaire. Les octets (`BYTE`) sont étendus à 16 bits en lecture. Fonctions : `NOM(valeurs)` (table `Functions` du parser) ; `TIME` sans parenthèses.
- Nombres entiers de -32768 à 32767 ; une adresse écrite seule (POKE, PEEK, CALL, MEMORY…) peut aller jusqu'à 65535. Un nombre écrit plus grand, ou avec virgule ou exposant, est un nombre à virgule (D068).
- Nombres à virgule (D068 à D070) : 5 octets du Locomotive (`LocoReal.cs`). Calculs par la ROM à travers une table de sauts `rt_fm_...` (`FirmwareMaths`, adresses 464 et 664/6128 ; « Tous les CPC » : KL PROBE ROM au départ). Un calcul se fait dans des cases `ftmp_k` (`EmitRealInto`, `realFree`). Modules dans `RealRuntime.cs` : RealOps (conversions, opérations, Overflow), RealPrint (affichage exact, aiguillage `rt_fp_out` pour STR$), RealParse (INPUT A!, VAL), RealRnd, DataReal. Piège connu : REAL INT / REAL FIX de la ROM rendent un entier de 4 octets (D069).
- Sans DEF ni réglage F8, `A` est un entier à part (D026) ; DEFINT/DEFREAL/DEFSTR et F8 ajoutent le signe aux noms avant la lecture (`ApplyDefaultTypes` dans le parser).
- Son (D071) : routines de la ROM &BCAA, &BCAD, &BCB3, &BCBC, &BCBF ; `EnvelopeStatement` pour ENV et ENT.
- Imprimante de référence : WinAPE, Settings, Other, Printer « File » -> `essais\imprimante.txt` (écrit en quittant WinAPE, ajouté à la fin). Pour lancer un programme avec le vrai BASIC sans le recopier : réglage F11 de l'éditeur (D101), puis `LOAD"NOM.TXT"` et `RUN`.
- Vérifier contre le vrai BASIC : faire taper un petit programme à l'utilisateur dans **SugarBox** (WinAPE déforme le collage), relever l'écran, puis comparer avec l'exemple compilé.
- Sauts (D054) : chaque ligne numérotée reçoit une étiquette `line_N` (zéro octet) ; GOTO = JP, GOSUB = CALL, RETURN = RET.
- Blocs (FOR, WHILE, REPEAT, DO, IF) : pile commune dans le parser et dans le générateur. `EXIT` et `CONTINUE` sont de simples sauts.
- Comparaisons signées : on retourne le bit 15 des deux valeurs puis on compare avec la retenue.
- Commandes d'écran, de tracé, de clavier et de machine : code écrit sur place qui appelle le firmware (&BC0E, &BB6C, &BB75, &BB90, &BB96, &BC32, &BC38, &BB1E, &BB24, &BB09, &BB06…). Les valeurs écrites en nombres sont vérifiées par le parser ; les valeurs calculées non.
- Écriture directe dans l'écran (BOX, PEEK au-dessus de &C000) : couper la ROM haute (&B903) puis la remettre (&B90C).
- Fin de programme : attend une touche puis `JP 0` (pas de retour au BASIC).
- Nom du fichier `.bas` = nom sur la disquette : 8 caractères au plus.
- Messages d'erreur en français dans `Diagnostic.cs` ; chacun commence par « ERREUR en ligne N : » et peut nommer la commande (champ `Context`, D042).
- Tests xUnit. `tests\CpcBasic.Tests\MiniZ80.cs` est un mini-simulateur Z80 : il signale une instruction inconnue (l'y ajouter), connaît `ScreenMode` (&BC11), `UpperRomEnabled`, les touches, les manettes et les ports ; sa pile est à &BF00.
- Début de chaque instruction BASIC : `BasicProgram.Starts` (noté par le parser) ; sert à ON ERROR (fiche par instruction, D080), aux minuteries et à la touche ESC (appel entre deux instructions, D079, D083), à TRON (D088).
- Événements (D079, D082, D083) : un compteur du firmware (KL ADD TICKER) ; modules Events, EventsSound, EventsBreak, EventsSoundBreak selon ce que le programme utilise.
- Fichiers (D084 à D086) : zones de 2 Ko du firmware en &3000 (lecture) et &3800 (écriture) ; PRINT #9 détourne 4 entrées du firmware (TXT OUTPUT, GET CURSOR, VALIDATE, GET WINDOW) le temps de l'instruction ; RUN "nom" recopie un petit chargeur en &3800.
- MiniZ80 simule aussi : fichiers (CAS…), files sonores (ON SQ), touches tapées à un moment donné (ESC), commandes RSX (IX, KL FIND COMMAND), petit modèle du graphisme (XPOS, TEST).
- Réglages de l'éditeur : `EditorSettings.cs`, fichier `%APPDATA%\CpcBasicIde\reglages.json`. F6 passe de l'éditeur aux messages (D041).

## 6. Comment donner les fichiers à la nouvelle conversation

**Économiser la conversation (demande de l'utilisateur, 15 nov. 2026) :** ne pas relire tous les fichiers. Lire ce fichier, puis seulement les parties utiles à l'étape en cours (chercher avec `grep`, lire quelques dizaines de lignes) ; ne lire le journal que pour la décision voulue (`grep -n "## D07"`).

**Le plus simple :** le dépôt est public. Après avoir poussé le dernier commit sur GitHub, écrire à Claude « clone le dépôt CPC-LOCO/cpc-basic-ide et lis docs\REPRISE.md ». Claude peut alors lire tous les fichiers lui-même. (Il ne peut pas forcément pousser : c'est l'utilisateur qui pousse avec `packaging\commit.cmd`.)

**Sinon :** double-cliquer sur `packaging\preparer-transfert.cmd`, qui rassemble les fichiers ci-dessous dans `E:\transfert`, puis les joindre au premier message. Chemins depuis `E:\projets amstrad ide\cpc-basic-ide` :

1. `docs\REPRISE.md` (ce fichier)
2. `docs\ARCHITECTURE.md`
3. `docs\JOURNAL-DECISIONS.md`
4. `docs\COMPATIBILITE-LOCOMOTIVE.md`
5. `docs\PROGRAMME-DE-REFERENCE-RSX.md`
6. `docs\IDEES.md`
7. `src\CpcBasic.Core\Language\Lexer.cs`
8. `src\CpcBasic.Core\Language\Parser.cs`
9. `src\CpcBasic.Core\Language\Ast.cs`
10. `src\CpcBasic.Core\Language\Diagnostic.cs`
11. `src\CpcBasic.Core\Language\BasicAnalyzer.cs`
12. `src\CpcBasic.Core\Codegen\Z80Emitter.cs`
13. `src\CpcBasic.Core\Codegen\CodeGenerator.cs`
14. `src\CpcBasic.Core\Codegen\RuntimeLibrary.cs`
15. `src\CpcBasic.Core\Compiler.cs`
16. `src\CpcIde\MainWindow.axaml`
17. `src\CpcIde\MainWindow.axaml.cs`
18. `src\CpcIde\BasicColorizer.cs`
19. `src\CpcIde\EditorSettings.cs`
20. `src\CpcIde\SaveChangesDialog.cs`
21. `tests\CpcBasic.Tests\MiniZ80.cs`

À envoyer seulement quand le sujet le demande (Claude les réclame avec leur chemin) : `packaging\build-release.cmd`, `packaging\LISEZMOI.txt`, `src\CpcBasic.Core\Output\DiskImage.cs`, `src\CpcBasic.Core\Output\BasicLoader.cs`, les fichiers `.csproj`, un fichier de tests précis.

## 7. Message de démarrage suggéré

> Je reprends le projet CPC BASIC IDE (dossier E:\projets amstrad ide\cpc-basic-ide). Lis d'abord docs\REPRISE.md (surtout les chapitres 2 et 3), et seulement les fichiers utiles ensuite. La version 0.7.0 est publiée (D197) ; depuis : sprites (D198 à D201), traits rapides sur cartouche et sur CPC (D202 à D205), SELECT CASE (D206) ; 2073 tests passent. Propose-moi la suite (IDEES.md : effets sonores VROOM, BOOM..., RUN "nom" sur cartouche, SCROLL, PLOT rapide...), une étape à la fois, une seule action par message.
