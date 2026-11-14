# Reprise du projet CPC BASIC IDE dans une nouvelle conversation

Ce fichier sert à reprendre le travail sans rien perdre. À envoyer en premier dans la nouvelle conversation (ou à faire lire par Claude dans le dépôt, voir chapitre 6). Il est à tenir à jour : à la fin de chaque étape, remplacer le chapitre 3.

## 1. Le projet en quelques lignes

Un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. L'éditeur (Avalonia, C#, .NET 10) tourne sur Windows. Le compilateur transforme le programme en code Z80 et l'écrit sur une disquette `.dsk`, qu'on essaie dans l'émulateur WinAPE (ou SugarBox) avec `RUN"NOM"`.

Dépôt (public) : https://github.com/CPC-LOCO/cpc-basic-ide
Dossier de travail : `E:\projets amstrad ide\cpc-basic-ide`
Auteur des commits : RetroPoke (pseudo de l'utilisateur sur les forums Amstrad).
Documents de référence :
- `docs\ARCHITECTURE.md` : comment c'est fait ;
- `docs\JOURNAL-DECISIONS.md` : toutes les décisions, D001 à D060 ;
- `docs\COMPATIBILITE-LOCOMOTIVE.md` : la liste de tous les mots du Locomotive BASIC et leur état, avec l'ordre de travail par lots (A à J) ;
- `docs\PROGRAMME-DE-REFERENCE-RSX.md` : le programme de l'utilisateur (commandes RSX en binaire) qui doit un jour se compiler tel quel ;
- `docs\IDEES.md` : idées en attente ;
- `TESTEURS.md` (à la racine) : aide pour les testeurs.

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
- **Programmes d'exemple :** une attente avant chaque changement d'écran et avant le texte final « APPUYEZ SUR UNE TOUCHE », chacune un peu plus longue que la précédente, mais **courtes** (boucles 1, 2, 3…).
- **Journal :** on n'efface jamais une entrée ; si une décision change, on en ajoute une nouvelle qui renvoie à l'ancienne.
- **Mots réservés :** chaque nouveau mot-clé empêche d'utiliser ce nom comme variable ; à signaler et à noter dans le journal.

## 3. État au moment de la reprise (13 novembre 2026)

- **Version publique :** 0.2.0 (pre-release GitHub, Windows 64 bits). Tout ce qui suit D040 n'est pas encore publié. Le numéro de version vient de `Directory.Build.props` (racine) et de `VERSION` dans `packaging\build-release.cmd` : garder les deux identiques.
- **Dernier commit sur GitHub :** « Reprise a jour apres lot C partie 1 » (13 nov. 2026, 481 tests).
- **DIM (D058) validé dans WinAPE le 13 nov. 2026** : 534 tests. Commit « Lot C : DIM, tableaux d'entiers ».
- **DATA, READ, RESTORE, INPUT T(I) (D059) validés le 14 nov. 2026** : 568 tests, `examples\donnees.bas` conforme. **Le lot C est terminé** (sauf ERASE, gardé pour plus tard).
- **Lot D (D060) validé le 14 nov. 2026** : &C9, &X1010, AND/OR/XOR bit à bit, NOT, ^, comparaisons comme valeurs, `IF A THEN`. 609 tests, `examples\logique.bas` conforme.
- **Règle de l'utilisateur :** le projet doit être 100 % compatible avec le Locomotive BASIC. **Finir tous les lots de l'inventaire (A à J) avant toute commande nouvelle** (CIRCLE, ELLIPSE, STAR, POLYGON, 3D… attendent). On avance lot par lot.

### Fait et validé

- **J1, J2, J3 :** `PRINT`, disquette ; variables entières (`A`, `A%`) et octets (`BYTE a`) ; `FOR/NEXT`, `WHILE/WEND`, `REPEAT/UNTIL`, `DO/LOOP`, `EXIT`, `CONTINUE`, `IF/THEN/ELSE` ; `*`, `/`, `\`, `MOD` ; `ABS`, `SGN`, `MIN`, `MAX`, `HIBYTE`, `LOBYTE`, `CLAMP` ; `RANDOM`, `RANDOMIZE` ; `TIME` (D037 à D040).
- **Éditeur :** accessibilité (D041 : taille du texte, contraste Ctrl+H, F6, clavier), messages d'erreur qui nomment la commande (D042), confirmation avant de perdre des modifications (D043), `packaging\commit.cmd` et dossier `essais\` (D044), numéros séparés, boutons de 36 points, `[Larg : N]` (D045).
- **Écran (D046) :** `MODE`, `CLS`, `LOCATE`, `PEN`, `PAPER`, `INK`, `BORDER`.
- **Tracé Locomotive (D047) :** `MOVE(R)`, `PLOT(R)`, `DRAW(R)`, `CLG`, `ORIGIN`, stylo et mode facultatifs ; le mode graphique (XOR, AND, OR) reste actif jusqu'à `MODE`.
- **BOX (D048, D051, D052) :** `BOX x1,y1,x2,y2 [,stylo [,remplissage]]`, coordonnées natives (origine en haut à gauche, y 0 à 199, x 0 à 159 / 319 / 639). Modèle à suivre pour toutes les futures formes. Rappel « fermez l'émulateur » (D050). D049 (PAUSE) annulé.
- **Inventaire (D053) :** `docs\COMPATIBILITE-LOCOMOTIVE.md`, ordre de travail par lots, suivi par l'utilisateur.
- **Lot A (D054) :** `GOTO`, `GOSUB`, `RETURN`, `ON … GOTO/GOSUB`, `STOP`. `examples\sauts.bas`.
- **Lot B (D055) :** `INKEY(n)`, `JOY(0/1)`, `CLEAR INPUT`, `INPUT` (nombres entiers). `examples\clavier.bas`, `examples\saisie.bas`. `INKEY$` et `LINE INPUT` attendent les chaînes (lot E).
- **Lot C, tableaux (D058) :** `DIM` (une ou plusieurs dimensions, taille écrite ou calculée, création automatique 0 à 10 sans DIM), vérification des cases avec les messages du Locomotive (« Subscript out of range in 80 », « Array already dimensioned », « Memory full »). `examples\tableaux.bas`, `examples\horstab.bas`.
- **Lot C, première partie (D056, D057) :** `POKE`, `PEEK` (relit bien la RAM au-dessus de &C000, vérifié dans SugarBox), `CALL`, `OUT`, `INP`, `MEMORY` (protège le haut de la mémoire : le compilateur vérifie que le programme tient dessous). `examples\machine.bas`.

### Prochaine étape : fin du lot C

1. `@variable` (adresse d'une variable ou d'une case de tableau ; demandé par l'utilisateur, petite étape D061) ;
2. lot E : chaînes de caractères (A$, textes dans DATA, MID$, VAL, +…), indispensable au programme RSX de référence.

Puis lot D (nombres `&` hexadécimaux et `&X` binaires, `XOR`, `NOT`, `^`, comparaisons comme valeurs), lot E (chaînes), F (réels et fonctions mathématiques), G (son), H (fenêtres et mise en forme), I (interruptions et erreurs), J (disquette et cassette).

### Questions en attente (à reposer au bon moment)

- `HIMEM` : quelle valeur de départ dans un programme compilé ? Doit-elle changer après `MEMORY` ? (D057)
- Commandes RSX `|NOM,params` et paramètre `@variable` : ce sont des mots du Locomotive ; leur fonctionnement dans un programme compilé demande un choix (D057, `docs\PROGRAMME-DE-REFERENCE-RSX.md`).
- `INPUT` avec une ligne vide : que fait exactement le Locomotive ? (à comparer sur un vrai CPC ; D055)
- Coordonnées des commandes ajoutées en MODE 0 : x de 0 à 159 (l'utilisateur avait écrit 179, à confirmer).

### En attente jusqu'à la fin des lots

- **CIRCLE :** vrai rond corrigé en largeur selon le MODE (rayon en lignes ; largeur ×2,4 en MODE 2, ×1,2 en MODE 1, ×0,6 en MODE 0), contour et remplissage comme BOX, très rapide. Toute commande à écriture directe doit couper la ROM haute comme `rt_box` (D048).
- `PAINT`, ELLIPSE, STAR, POLYGON, calcul 3D en fil de fer (SIN et COS par tableau de 256 angles, virgule fixe, racine carrée entière, projection).
- Ligne verticale discrète dans l'éditeur à la colonne de la largeur du MODE (20, 40 ou 80).
- `PAUSE [heure,][minute,] seconde`, `CLIC code_ascii`.

## 4. Avant la prochaine publication

- D016 : vérifier qu'aucune ROM Amstrad n'est livrée.
- D017 : relecture juridique de l'exception de liaison (`docs\RUNTIME-EXCEPTION.md`).
- Refaire `packaging\LISEZMOI.txt` (nouveautés depuis 0.2.0) et changer le numéro de version aux deux endroits.

## 5. Points techniques à connaître

- Chaîne du compilateur : `Lexer` (mots et symboles) -> `Parser` (instructions, expressions, vérifications) -> `CodeGenerator` (octets Z80 avec `Z80Emitter`) -> `Compiler` (enchaîne tout, vérifie la limite de `MEMORY`, écrit la disquette).
- Les modules Z80 sont écrits en C# dans `RuntimeLibrary.cs` ; chacun déclare ses dépendances, seuls les modules utilisés sont liés (liaison sélective). Modules actuels : PrintString, PrintInt, Input, Mul16, Div16, Abs16, Sgn16, Min16, Max16, Rand16, Randomize, RandomRange, Pixel, Box, Array (D058), RunError et Data (D059).
- Tableaux (D058) : un descripteur par tableau à la fin du binaire (adresse des cases, puis nombre de cases par dimension) ; les cases sont prises au lancement après le programme (`rt_heap_start`), jusqu'à &A67B ou MEMORY. Erreurs à l'exécution : module commun `RunError` (texte du Locomotive + « in » + numéro de ligne rangé dans `rt_err_line`), puis attente d'une touche et redémarrage (D059). `AstWalker` (dans `Ast.cs`) parcourt les expressions d'une instruction.
- Le programme compilé commence en &4000. Expressions sur 16 bits, résultat dans `HL`. Variables réservées à la fin du binaire. Les octets (`BYTE`) sont étendus à 16 bits en lecture. Fonctions : `NOM(valeurs)` (table `Functions` du parser) ; `TIME` sans parenthèses.
- Nombres : entiers de -32768 à 32767. Une adresse écrite seule (POKE, PEEK, CALL, MEMORY…) peut aller jusqu'à 65535 ; dans un calcul, non (écrire `-16384 + I`). Cela changera avec les lots D et F.
- Sauts (D054) : chaque ligne numérotée reçoit une étiquette `line_N` (zéro octet) ; GOTO = JP, GOSUB = CALL, RETURN = RET.
- Blocs (FOR, WHILE, REPEAT, DO, IF) : pile commune dans le parser et dans le générateur. `EXIT` et `CONTINUE` sont de simples sauts.
- Comparaisons signées : on retourne le bit 15 des deux valeurs puis on compare avec la retenue.
- Commandes d'écran, de tracé, de clavier et de machine : code écrit sur place qui appelle le firmware (&BC0E, &BB6C, &BB75, &BB90, &BB96, &BC32, &BC38, &BB1E, &BB24, &BB09, &BB06…). Les valeurs écrites en nombres sont vérifiées par le parser ; les valeurs calculées non.
- Écriture directe dans l'écran (BOX, PEEK au-dessus de &C000) : couper la ROM haute (&B903) puis la remettre (&B90C).
- Fin de programme : attend une touche puis `JP 0` (pas de retour au BASIC).
- Nom du fichier `.bas` = nom sur la disquette : 8 caractères au plus.
- Messages d'erreur en français dans `Diagnostic.cs` ; chacun commence par « ERREUR en ligne N : » et peut nommer la commande (champ `Context`, D042).
- Tests xUnit. `tests\CpcBasic.Tests\MiniZ80.cs` est un mini-simulateur Z80 : il signale une instruction inconnue (l'y ajouter), connaît `ScreenMode` (&BC11), `UpperRomEnabled`, les touches, les manettes et les ports ; sa pile est à &BF00.
- Réglages de l'éditeur : `EditorSettings.cs`, fichier `%APPDATA%\CpcBasicIde\reglages.json`. F6 passe de l'éditeur aux messages (D041).

## 6. Comment donner les fichiers à la nouvelle conversation

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

> Je reprends le projet CPC BASIC IDE. Clone le dépôt CPC-LOCO/cpc-basic-ide (ou lis les fichiers joints) et lis d'abord docs\REPRISE.md. Le dernier commit est le lot C partie 1 (POKE, PEEK, CALL, OUT, INP, MEMORY), validé ; les tests passent (481). On continue le lot C : DIM, puis DATA / READ / RESTORE.
