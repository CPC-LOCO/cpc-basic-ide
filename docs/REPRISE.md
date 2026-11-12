# Reprise du projet CPC BASIC IDE dans une nouvelle conversation

Ce fichier sert à reprendre le travail sans rien perdre. À envoyer en premier dans la nouvelle conversation, avec les fichiers listés au chapitre 6. Il est à tenir à jour : à la fin de chaque étape, remplacer le chapitre 3.

## 1. Le projet en quelques lignes

Un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. L'éditeur (Avalonia, C#, .NET 10) tourne sur Windows. Le compilateur transforme le programme en code Z80 et l'écrit sur une disquette `.dsk`, qu'on essaie dans l'émulateur WinAPE avec `RUN"NOM"`.

Dépôt : https://github.com/CPC-LOCO/cpc-basic-ide
Dossier de travail : `E:\projets amstrad ide\cpc-basic-ide`
Documents de référence : `docs\ARCHITECTURE.md` (comment c'est fait), `docs\JOURNAL-DECISIONS.md` (toutes les décisions, D001 à D046), `docs\IDEES.md` (idées en attente), `TESTEURS.md` (aide pour les testeurs, à la racine).

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
  5. écrire l'entrée de journal (D0xx), puis commit : double-clic sur `packaging\commit.cmd` (il lance les tests, montre les fichiers, demande le message, propose le push). Les programmes d'essai de l'utilisateur vont dans `essais\`, jamais dans `examples\`. Ne pas donner de longues séries de commandes à copier : l'utilisateur les trouve difficiles à suivre.
- **Journal :** on n'efface jamais une entrée ; si une décision change, on en ajoute une nouvelle qui renvoie à l'ancienne.
- **Mots réservés :** chaque nouveau mot-clé (BYTE, EXIT, DO, MOD, RANDOM...) empêche d'utiliser ce nom comme variable ; à signaler et à noter dans le journal.

## 3. État au moment de la reprise (11 novembre 2026)

- **Version publique :** 0.2.0 (pre-release GitHub, Windows 64 bits). Les testeurs peuvent la télécharger et signaler des problèmes (formulaires dans `.github\ISSUE_TEMPLATE\`). Le numéro de version vient de `Directory.Build.props` (à la racine) et de `VERSION` dans `packaging\build-release.cmd` : garder les deux identiques. Les nouveautés ci-dessous ne sont pas encore publiées.
- **Jalons terminés et validés (295 tests, écrans WinAPE conformes) :**
  - J1 : `PRINT`, fabrication de la disquette.
  - J2 : variables entières (`A`, `A%`) et octets (`BYTE a`), `FOR/NEXT`, `WHILE/WEND`, `REPEAT/UNTIL`, `DO/LOOP`, `EXIT`, `CONTINUE`, `IF/THEN/ELSE` (une ligne ou bloc), comparaisons, `AND`, `OR`.
  - J3 : `*`, `/`, `\`, `MOD` (D037) ; fonctions `ABS`, `SGN`, `MIN`, `MAX`, `HIBYTE`, `LOBYTE`, `CLAMP` (D038) ; `RANDOM`, `RANDOMIZE` (D039) ; `TIME` (D040).
  - **Accessibilité de l'éditeur (D041, validée à la main le 11 novembre) :** taille du texte réglable et mémorisée (Ctrl + molette, Ctrl+plus et moins, Ctrl+0, boutons), contraste élevé (Ctrl+H), gros boutons, navigation au clavier (F6, flèches, Entrée, Échap), sélection jaune et noir dans le panneau de messages. Nouveau fichier `src\CpcIde\EditorSettings.cs`.
  - **Confirmation avant de perdre des modifications (D043, validée à la main) :** boîte Enregistrer / Ne pas enregistrer / Annuler à l'ouverture d'un fichier et à la fermeture de la fenêtre. Nouveau fichier `src\CpcIde\SaveChangesDialog.cs`.
  - **Commandes d'écran (D046, validées dans WinAPE) :** `MODE`, `CLS`, `LOCATE`, `PEN`, `PAPER`, `INK`, `BORDER`, en code en ligne qui appelle le firmware (aucun module). Nouveaux mots réservés. `examples\ecran.bas` et `examples\modes.bas`.
  - **Numéros séparés, boutons compacts, largeur en colonnes (D045, validés à la main) :** espace entre les numéros de l'éditeur (gris) et le texte, boutons de 36 points, `[Larg : N]` dans la barre d'état.
  - **Script `packaging\commit.cmd` (D044) :** double-clic = tests, liste des fichiers, commit, push. Dossier `essais\` (ignoré par git) pour les programmes d'essai.
  - **Messages d'erreur qui nomment la commande (D042) :** « ERREUR en ligne N : PRINT attend ici une valeur... La ligne est incomplète. » Champ `Context` dans `Diagnostic`, rempli par `ParseStatement` dans `Parser.cs`.
- **Journal :** `docs\JOURNAL-DECISIONS.md` doit contenir D001 à D046 ; il est en UTF-8 (les accents avaient été réparés le 11 novembre).
- **Prochaine étape :** le dessin : `MOVE`, `PLOT`, `DRAW`, `CLG`... (voir chapitre 4). Rien n'est commencé : commencer par la conception.

## 4. Suite prévue

1. **Dessin** (`MOVE`, `PLOT`, `DRAW`, `CLG`, `ORIGIN`...) par les routines du firmware. L'écran et les couleurs (`MODE`, `CLS`, `LOCATE`, `PEN`, `PAPER`, `INK`, `BORDER`) sont faits (D046).
2. **Calcul 3D en fil de fer** (demandé) : SIN et COS par tableau (256 angles pour un tour), multiplication en virgule fixe avec calcul intermédiaire sur 32 bits, racine carrée entière, projection d'un point sur l'écran. Dépend des graphismes (tracer des lignes). Le tableau des sinus n'entre que dans les programmes qui s'en servent.
3. **Ensuite :** `GOTO`/`GOSUB`, tableaux, chaînes, réels, `INPUT`, clavier, sons, sprites, accents (table de caractères du CPC), choix de la machine cible, cassette, cartouche GX4000. Division par zéro avec un diviseur calculé : pas d'erreur à l'exécution pour l'instant (voir `IDEES.md`).
4. **Avant la prochaine publication :** D016 (vérifier qu'aucune ROM Amstrad n'est livrée) et la relecture juridique de l'exception de liaison (D017, texte dans `docs\RUNTIME-EXCEPTION.md`). Penser à refaire `LISEZMOI.txt` (nouveautés : fonctions, RANDOM, TIME, accessibilité de l'éditeur, confirmation avant de perdre des modifications, messages d'erreur) et le numéro de version.

## 5. Points techniques à connaître

- Le compilateur est découpé : `Lexer` (mots et symboles) -> `Parser` (instructions, expressions, vérifications) -> `CodeGenerator` (octets Z80 avec `Z80Emitter`) -> `Compiler` (enchaîne tout et écrit la disquette).
- Les modules Z80 sont écrits en C# avec l'assembleur interne, dans `RuntimeLibrary.cs`. Chaque module déclare son nom et ses dépendances ; seuls les modules utilisés sont liés au programme (liaison sélective). Modules actuels : PrintString, PrintInt, Mul16, Div16, Abs16, Sgn16, Min16, Max16, Rand16, Randomize, RandomRange.
- Les expressions sont calculées sur 16 bits, résultat dans `HL`. Les variables sont réservées à la fin du binaire. Les octets (`BYTE`) sont étendus à 16 bits en lecture. Les fonctions s'écrivent `NOM(valeurs)` (table `Functions` du parser) ; `TIME` s'écrit sans parenthèses.
- Les blocs (FOR, WHILE, REPEAT, DO, IF) sont suivis par une pile commune dans le parser (vérification) et dans le générateur (étiquettes). `EXIT` et `CONTINUE` sont de simples sauts.
- Les comparaisons signées retournent le bit 15 des deux valeurs puis comparent avec la retenue (même méthode dans `Min16`, `Max16`, les boucles `FOR` et les conditions).
- Les tests sont en xUnit. `tests\CpcBasic.Tests\MiniZ80.cs` est un mini-simulateur Z80 qui exécute les modules ; pour tester un nouveau module, y ajouter les instructions qu'il utilise si besoin (il le dit quand il rencontre une instruction inconnue).
- Les commandes d'écran (D046) sont du code en ligne : `ScreenStatement` dans l'analyse, `EmitScreen` dans le générateur, routines du firmware &BC0E, &BB6C, &BB75, &BB90, &BB96, &BC32, &BC38. Les valeurs écrites en nombres sont vérifiées par le parser (table `ScreenCommands`), pas les valeurs calculées.
- Fin de programme : le programme compilé attend une touche puis redémarre (`JP 0`) : un programme compilé ne revient pas au BASIC.
- Le nom du fichier `.bas` donne le nom sur la disquette : 8 caractères au plus.
- Le texte des messages d'erreur est en français dans `Diagnostic.cs`, provisoirement (il rejoindra des fichiers de langue). Chaque message commence par « ERREUR en ligne N : » et peut nommer la commande (champ `Context` de `Diagnostic`, rempli par `ParseStatement`, D042).
- Réglages de l'éditeur (taille du texte, contraste) : `EditorSettings.cs`, fichier `%APPDATA%\CpcBasicIde\reglages.json`. Dans la fenêtre, F6 passe de l'éditeur aux messages ; le clavier doit être posé sur un message précis, et la touche Entrée est interceptée par la fenêtre (D041).

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
17. `src\CpcIde\EditorSettings.cs`
18. `src\CpcIde\SaveChangesDialog.cs`

Pour le style des tests :

19. `tests\CpcBasic.Tests\MiniZ80.cs`

À envoyer seulement quand le sujet le demande (Claude doit les réclamer avec leur chemin) : `packaging\build-release.cmd`, `packaging\LISEZMOI.txt`, `src\CpcBasic.Core\Output\DiskImage.cs`, `src\CpcBasic.Core\Output\BasicLoader.cs`, les fichiers `.csproj`.

## 7. Message de démarrage suggéré

> Je reprends le projet CPC BASIC IDE. Lis d'abord REPRISE.md, puis les autres fichiers joints. Tout est copié dans le dépôt, les 295 tests passent. On choisit la prochaine étape.
