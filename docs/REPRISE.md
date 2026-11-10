# Reprise du projet CPC BASIC IDE dans une nouvelle conversation

Ce fichier sert à reprendre le travail sans rien perdre. À envoyer en premier dans la nouvelle conversation, avec les fichiers listés au chapitre 6. Il est à tenir à jour : à la fin de chaque étape, remplacer le chapitre 3.

## 1. Le projet en quelques lignes

Un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. L'éditeur (Avalonia, C#, .NET 10) tourne sur Windows. Le compilateur transforme le programme en code Z80 et l'écrit sur une disquette `.dsk`, qu'on essaie dans l'émulateur WinAPE avec `RUN"NOM"`.

Dépôt : https://github.com/CPC-LOCO/cpc-basic-ide
Dossier de travail : `E:\projets amstrad ide\cpc-basic-ide`
Documents de référence : `docs\ARCHITECTURE.md` (comment c'est fait), `docs\JOURNAL-DECISIONS.md` (toutes les décisions, D001 à D038), `docs\IDEES.md` (idées en attente).

## 2. Comment travailler avec l'utilisateur (important)

- **Langue :** français, avec des mots simples. L'utilisateur n'aime pas le jargon : expliquer pas à pas, en disant exactement quoi cliquer ou taper, sans sigles inutiles.
- **Demander un fichier :** toujours donner son chemin complet depuis la racine du dépôt (par exemple `src\CpcBasic.Core\Language\Parser.cs`). L'utilisateur trouve difficilement ce qui est vague.
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
- **Mots réservés :** chaque nouveau mot-clé (BYTE, EXIT, DO...) empêche d'utiliser ce nom comme variable ; à signaler et à noter dans le journal.

## 3. État au moment de la reprise (9 novembre 2026)

- **Version publique :** 0.2.0 (pre-release GitHub, Windows 64 bits), fabriquée avec `packaging\build-release.cmd`. Le numéro de version vient de `Directory.Build.props` (à la racine) et de `VERSION` dans `build-release.cmd` : garder les deux identiques. **À confirmer avec l'utilisateur : la publication sur GitHub est-elle faite ?** (somme SHA256 de l'archive testée : `4f8655aecb1ecea38f6f26a31bb0a2dea436619394225473b979a5af0065407a`).
- **J1 et J2 terminés.** Le compilateur reconnaît : `PRINT`, variables entières (`A`, `A%`) et octets (`BYTE a`), expressions, `FOR/NEXT`, `WHILE/WEND`, `REPEAT/UNTIL`, `DO/LOOP`, `EXIT`, `CONTINUE`, `IF/THEN/ELSE` (une ligne ou bloc avec `END IF`), comparaisons, `AND`, `OR`.
- **J3a terminé et validé :** `*`, `/`, `\`, `MOD` (D037), 205 tests, écran WinAPE conforme (`examples\calculs.bas`).
- **J3b : fonctions `ABS`, `SGN`, `MIN`, `MAX`, `HIBYTE`, `LOBYTE`, `CLAMP` (D038).** Les 223 tests passent. **Reste à confirmer avec l'utilisateur :** l'essai de `examples\maths.bas` dans WinAPE. Écran attendu :
  ```
   5  7 
  -1  0  1 
   3  8 
   2  9 
   3  232 
   255 
   9  50  100 
   10 
  fin
  ```
  (chaque nombre est précédé d'un espace s'il est positif, et suivi d'un espace.)
- **Entrées de journal D037 et D038 :** dans `JOURNAL-J3.md`, à ajouter à la fin de `docs\JOURNAL-DECISIONS.md` (avant « Notes de fin de session »). De même `JOURNAL-J2.md` (D026 à D036) si ce n'est pas déjà fait.

## 4. Suite prévue

1. **J3c :** `RANDOM(min, max)` et `RANDOMIZE`. Générateur de nombres entiers (rapide, sans virgule) ; point de départ pris dans l'horloge du CPC (routine du firmware, adresse à vérifier avant de s'en servir) ; `RANDOMIZE n` fixe la suite pour rejouer. Écrire D039.
2. **Accessibilité de l'éditeur** (demandée par l'utilisateur, avant la prochaine publication) : taille du texte réglable (Ctrl + molette, Ctrl+ et Ctrl-, avec mémorisation), thème à fort contraste, boutons plus grands, tout utilisable au clavier. Demander `src\CpcIde\MainWindow.cs` et le fichier `.axaml` correspondant.
3. **Graphismes de base** (MODE, INK, PLOT, DRAW...) par les routines du firmware.
4. **Calcul 3D en fil de fer** (demandé) : SIN et COS par tableau (256 angles pour un tour), multiplication en virgule fixe avec calcul intermédiaire sur 32 bits, racine carrée entière, projection d'un point sur l'écran. Dépend des graphismes (il faut pouvoir tracer des lignes). Le tableau des sinus n'entre que dans les programmes qui s'en servent.
5. **Ensuite :** `GOTO`/`GOSUB`, tableaux, chaînes, réels, `INPUT`, clavier, sons, sprites, accents (table de caractères du CPC), choix de la machine cible, cassette, cartouche GX4000.
6. **Avant la prochaine publication :** D016 (vérifier qu'aucune ROM Amstrad n'est livrée) et la relecture juridique de l'exception de liaison (D017, texte dans `docs\RUNTIME-EXCEPTION.md`).

## 5. Points techniques à connaître

- Le compilateur est découpé : `Lexer` (mots et symboles) -> `Parser` (instructions, expressions, vérifications) -> `CodeGenerator` (octets Z80 avec `Z80Emitter`) -> `Compiler` (enchaîne tout et écrit la disquette).
- Les modules Z80 sont écrits en C# avec l'assembleur interne, dans `RuntimeLibrary.cs`. Chaque module déclare son nom et ses dépendances ; seuls les modules utilisés sont liés au programme (liaison sélective).
- Les expressions sont calculées sur 16 bits, résultat dans `HL`. Les variables sont réservées à la fin du binaire. Les octets (`BYTE`) sont étendus à 16 bits en lecture.
- Les blocs (FOR, WHILE, REPEAT, DO, IF) sont suivis par une pile commune dans le parser (vérification) et dans le générateur (étiquettes). `EXIT` et `CONTINUE` sont de simples sauts.
- Les comparaisons signées retournent le bit 15 des deux valeurs puis comparent avec la retenue (même méthode dans `Min16`, `Max16`, les boucles `FOR` et les conditions).
- Les tests sont en xUnit. `tests\CpcBasic.Tests\MiniZ80.cs` est un mini-simulateur Z80 qui exécute les modules ; pour tester un nouveau module, y ajouter les instructions qu'il utilise si besoin.
- Fin de programme : le programme compilé attend une touche puis redémarre (`JP 0`) : un programme compilé ne revient pas au BASIC.
- Le nom du fichier `.bas` donne le nom sur la disquette : 8 caractères au plus.
- Le texte des messages d'erreur est en français dans `Diagnostic.cs`, provisoirement (il rejoindra des fichiers de langue).

## 6. Fichiers à envoyer dans la nouvelle conversation

Indispensables, dans cet ordre (chemins depuis `E:\projets amstrad ide\cpc-basic-ide`) :

1. `docs\REPRISE.md` (ce fichier)
2. `docs\ARCHITECTURE.md`
3. `docs\JOURNAL-DECISIONS.md`
4. `src\CpcBasic.Core\Language\Lexer.cs`
5. `src\CpcBasic.Core\Language\Parser.cs`
6. `src\CpcBasic.Core\Language\Ast.cs`
7. `src\CpcBasic.Core\Language\Diagnostic.cs`
8. `src\CpcBasic.Core\Language\BasicAnalyzer.cs`
9. `src\CpcBasic.Core\Codegen\Z80Emitter.cs`
10. `src\CpcBasic.Core\Codegen\CodeGenerator.cs`
11. `src\CpcBasic.Core\Codegen\RuntimeLibrary.cs`
12. `src\CpcBasic.Core\Compiler.cs`

À envoyer seulement quand le sujet le demande (Claude doit les réclamer avec leur chemin) :

- Éditeur : `src\CpcIde\MainWindow.cs`, le fichier `MainWindow.axaml`, `src\CpcIde\BasicColorizer.cs`
- Tests (pour le style) : `tests\CpcBasic.Tests\MiniZ80.cs` et un autre fichier de ce dossier
- Publication : `packaging\build-release.cmd`, `packaging\LISEZMOI.txt`
- Disquette : `src\CpcBasic.Core\Output\DiskImage.cs`, `src\CpcBasic.Core\Output\BasicLoader.cs`

Important : envoyer les versions **après** avoir copié les fichiers livrés par cette conversation (étape J3b), pour que la nouvelle conversation travaille sur le code à jour.

## 7. Message de démarrage suggéré

> Je reprends le projet CPC BASIC IDE. Lis d'abord REPRISE.md, puis les autres fichiers joints. Les fichiers de la section « état » sont copiés et les tests J3b sont [passés / pas encore lancés]. La suite : [ce que vous voulez faire].
