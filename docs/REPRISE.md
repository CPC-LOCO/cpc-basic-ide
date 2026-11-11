# Reprise du projet CPC BASIC IDE dans une nouvelle conversation

Ce fichier sert à reprendre le travail sans rien perdre. À envoyer en premier dans la nouvelle conversation, avec les fichiers listés au chapitre 6. Il est à tenir à jour : à la fin de chaque étape, remplacer le chapitre 3.

## 1. Le projet en quelques lignes

Un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. L'éditeur (Avalonia, C#, .NET 10) tourne sur Windows. Le compilateur transforme le programme en code Z80 et l'écrit sur une disquette `.dsk`, qu'on essaie dans l'émulateur WinAPE avec `RUN"NOM"`.

Dépôt : https://github.com/CPC-LOCO/cpc-basic-ide
Dossier de travail : `E:\projets amstrad ide\cpc-basic-ide`
Documents de référence : `docs\ARCHITECTURE.md` (comment c'est fait), `docs\JOURNAL-DECISIONS.md` (toutes les décisions, D001 à D040), `docs\IDEES.md` (idées en attente), `TESTEURS.md` (aide pour les testeurs, à la racine).

## 2. Comment travailler avec l'utilisateur (important)

- **Langue :** français, avec des mots simples. L'utilisateur n'aime pas le jargon : expliquer pas à pas, en disant exactement quoi cliquer ou taper, sans sigles inutiles.
- **Demander un fichier :** toujours donner son chemin complet depuis la racine du dépôt (par exemple `src\CpcBasic.Core\Language\Parser.cs`). Attention aux noms exacts : le code de la fenêtre s'appelle `MainWindow.axaml.cs` (l'envoi le renomme parfois en `MainWindow_axaml.cs`).
- **Livrer un fichier :** indiquer pour chacun sa destination dans le dépôt, dans un tableau. Les fichiers générés vont dans `/mnt/user-data/outputs`.
- **Accessibilité :** l'utilisateur est attentif aux personnes mal voyantes (zones minuscules à viser, textes peu lisibles). Éviter les instructions du type « cliquez sur le petit bouton » ; préférer le clavier et les commandes écrites.
- **Honnêteté :** Claude n'a pas d'accès au dépôt et ne peut ni compiler ni lancer les tests. Le dire à chaque livraison ; l'utilisateur lance `dotnet test` et colle le résultat.
- **Méthode d'une étape :**
  1. proposer ou rappeler la conception en peu de mots, vérifier les mots-clés dans WinAPE (taper `MOT=5` : s'il n'y a pas de `Syntax error`, le mot est libre) ;
  2. livrer le code, les tests xUnit et un petit programme d'exemple dans `examples\` (nom de 8 caractères au plus) ;
  3. l'utilisateur copie, lance `dotnet test`, compile l'exemple avec F5 dans l'éditeur, le lance dans WinAPE ;
  4. Claude donne à l'avance l'écran attendu ; l'utilisateur confirme ;
  5. écrire l'entrée de journal (D0xx), puis commit : `git add -A` puis `git commit -m "..."`.
- **Journal :** on n'efface jamais une entrée ; si une décision change, on en ajoute une nouvelle qui renvoie à l'ancienne.
- **Mots réservés :** chaque nouveau mot-clé (BYTE, EXIT, DO, MOD, RANDOM...) empêche d'utiliser ce nom comme variable ; à signaler et à noter dans le journal.

## 3. État au moment de la reprise (10 novembre 2026)

- **Version publique :** 0.2.0 (pre-release GitHub, Windows 64 bits). Les testeurs peuvent la télécharger et signaler des problèmes (formulaires dans `.github\ISSUE_TEMPLATE\`). Le numéro de version vient de `Directory.Build.props` (à la racine) et de `VERSION` dans `packaging\build-release.cmd` : garder les deux identiques.
- **Jalons terminés et validés (254 tests, écrans WinAPE conformes) :**
  - J1 : `PRINT`, fabrication de la disquette.
  - J2 : variables entières (`A`, `A%`) et octets (`BYTE a`), `FOR/NEXT`, `WHILE/WEND`, `REPEAT/UNTIL`, `DO/LOOP`, `EXIT`, `CONTINUE`, `IF/THEN/ELSE` (une ligne ou bloc), comparaisons, `AND`, `OR`.
  - J3 : `*`, `/`, `\`, `MOD` (D037) ; fonctions `ABS`, `SGN`, `MIN`, `MAX`, `HIBYTE`, `LOBYTE`, `CLAMP` (D038) ; `RANDOM`, `RANDOMIZE` (D039) ; `TIME` (D040). L'adresse de l'horloge &BD0D est confirmée.
- **Journal :** vérifier que `docs\JOURNAL-DECISIONS.md` contient D026 à D040 (les entrées viennent de `JOURNAL-J2.md`, `JOURNAL-J3.md`, `JOURNAL-D039.md`, `JOURNAL-D040.md`).
- **Prochaine étape demandée :** l'accessibilité de l'éditeur (voir chapitre 4). L'utilisateur a déjà envoyé `MainWindow.axaml` et `MainWindow.axaml.cs` dans l'ancienne conversation, mais il faut les renvoyer : la nouvelle conversation ne les a pas. Le code de la fenêtre contient : boutons Ouvrir, Enregistrer, Compiler ; raccourcis Ctrl+O, Ctrl+S, F5 ; panneau de messages cliquable ; barre d'état ; éditeur AvaloniaEdit avec `BasicColorizer`.

## 4. Suite prévue

1. **Accessibilité de l'éditeur** (avant la prochaine publication) : taille du texte réglable (Ctrl + molette, Ctrl+ et Ctrl-, avec mémorisation du choix), thème à fort contraste, police plus grande pour le panneau de messages, boutons plus grands, tout utilisable au clavier. Dette connue dans la même fenêtre : la colonne des numéros de ligne de l'éditeur colle aux numéros BASIC (D008), pas de confirmation à l'ouverture d'un fichier modifié (D022).
2. **Graphismes de base** (MODE, INK, PLOT, DRAW...) par les routines du firmware.
3. **Calcul 3D en fil de fer** (demandé) : SIN et COS par tableau (256 angles pour un tour), multiplication en virgule fixe avec calcul intermédiaire sur 32 bits, racine carrée entière, projection d'un point sur l'écran. Dépend des graphismes (tracer des lignes). Le tableau des sinus n'entre que dans les programmes qui s'en servent.
4. **Ensuite :** `GOTO`/`GOSUB`, tableaux, chaînes, réels, `INPUT`, clavier, sons, sprites, accents (table de caractères du CPC), choix de la machine cible, cassette, cartouche GX4000. Division par zéro avec un diviseur calculé : pas d'erreur à l'exécution pour l'instant (voir `IDEES.md`).
5. **Avant la prochaine publication :** D016 (vérifier qu'aucune ROM Amstrad n'est livrée) et la relecture juridique de l'exception de liaison (D017, texte dans `docs\RUNTIME-EXCEPTION.md`). Penser à refaire `LISEZMOI.txt` (nouveautés : fonctions, RANDOM, TIME) et le numéro de version.

## 5. Points techniques à connaître

- Le compilateur est découpé : `Lexer` (mots et symboles) -> `Parser` (instructions, expressions, vérifications) -> `CodeGenerator` (octets Z80 avec `Z80Emitter`) -> `Compiler` (enchaîne tout et écrit la disquette).
- Les modules Z80 sont écrits en C# avec l'assembleur interne, dans `RuntimeLibrary.cs`. Chaque module déclare son nom et ses dépendances ; seuls les modules utilisés sont liés au programme (liaison sélective). Modules actuels : PrintString, PrintInt, Mul16, Div16, Abs16, Sgn16, Min16, Max16, Rand16, Randomize, RandomRange.
- Les expressions sont calculées sur 16 bits, résultat dans `HL`. Les variables sont réservées à la fin du binaire. Les octets (`BYTE`) sont étendus à 16 bits en lecture. Les fonctions s'écrivent `NOM(valeurs)` (table `Functions` du parser) ; `TIME` s'écrit sans parenthèses.
- Les blocs (FOR, WHILE, REPEAT, DO, IF) sont suivis par une pile commune dans le parser (vérification) et dans le générateur (étiquettes). `EXIT` et `CONTINUE` sont de simples sauts.
- Les comparaisons signées retournent le bit 15 des deux valeurs puis comparent avec la retenue (même méthode dans `Min16`, `Max16`, les boucles `FOR` et les conditions).
- Les tests sont en xUnit. `tests\CpcBasic.Tests\MiniZ80.cs` est un mini-simulateur Z80 qui exécute les modules ; pour tester un nouveau module, y ajouter les instructions qu'il utilise si besoin (il le dit quand il rencontre une instruction inconnue).
- Fin de programme : le programme compilé attend une touche puis redémarre (`JP 0`) : un programme compilé ne revient pas au BASIC.
- Le nom du fichier `.bas` donne le nom sur la disquette : 8 caractères au plus.
- Le texte des messages d'erreur est en français dans `Diagnostic.cs`, provisoirement (il rejoindra des fichiers de langue).

## 6. Fichiers à envoyer dans la nouvelle conversation

Le script `packaging\preparer-transfert.cmd` les rassemble tous dans le dossier `E:\transfert`. Chemins depuis `E:\projets amstrad ide\cpc-basic-ide` :

Pour tout travail :

1. `docs\REPRISE.md` (ce fichier)
2. `docs\ARCHITECTURE.md`
3. `docs\JOURNAL-DECISIONS.md`
4. `docs\IDEES.md`
5. `src\CpcBasic.Core\Language\Lexer.cs`
6. `src\CpcBasic.Core\Language\Parser.cs`
7. `src\CpcBasic.Core\Language\Ast.cs`
8. `src\CpcBasic.Core\Language\Diagnostic.cs`
9. `src\CpcBasic.Core\Language\BasicAnalyzer.cs`
10. `src\CpcBasic.Core\Codegen\Z80Emitter.cs`
11. `src\CpcBasic.Core\Codegen\CodeGenerator.cs`
12. `src\CpcBasic.Core\Codegen\RuntimeLibrary.cs`
13. `src\CpcBasic.Core\Compiler.cs`

Pour l'éditeur (accessibilité) :

14. `src\CpcIde\MainWindow.axaml`
15. `src\CpcIde\MainWindow.axaml.cs`
16. `src\CpcIde\BasicColorizer.cs`

Pour le style des tests :

17. `tests\CpcBasic.Tests\MiniZ80.cs`

À envoyer seulement quand le sujet le demande (Claude doit les réclamer avec leur chemin) : `packaging\build-release.cmd`, `packaging\LISEZMOI.txt`, `src\CpcBasic.Core\Output\DiskImage.cs`, `src\CpcBasic.Core\Output\BasicLoader.cs`, les fichiers `.csproj`.

## 7. Message de démarrage suggéré

> Je reprends le projet CPC BASIC IDE. Lis d'abord REPRISE.md, puis les autres fichiers joints. Tout est copié dans le dépôt, les 254 tests passent. On commence par l'accessibilité de l'éditeur.
