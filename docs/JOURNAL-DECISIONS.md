# Journal des décisions

Une entrée par décision importante, avec la date, le choix et la raison. On n'efface jamais une entrée : si une décision change, on en ajoute une nouvelle qui renvoie à l'ancienne.

États possibles : **Décidé**, **Provisoire** (à confirmer), **Ouvert** (à trancher).

---

## D001 : Plateformes (Décidé, 7 nov. 2026)
Éditeur et compilateur sur ordinateur moderne. Windows obligatoire ; Linux et macOS souhaitables, sans retarder la livraison Windows. Le copier/coller fonctionne dans l'éditeur.

## D002 : Machines et formats cibles (Décidé, 7 nov. 2026)
Menu de choix de la cible : CPC 464, 664, 6128, 464+, 6128+, GX4000. Menu de choix de la ROM et de la disposition clavier (AZERTY, QWERTY, autres variantes nationales à inventorier). Sorties : .dsk, cassette, cartouche GX4000. La cible filtre les formats, les commandes (matériel Plus) et la mémoire.

## D003 : Mots-clés en anglais (Décidé, 7 nov. 2026)
Tous les mots-clés, anciens comme nouveaux, sont en anglais (CIRCLE, STAR, PAINT, SELECT CASE...). Effets sonores proposés : VROOM, BOOM, SHOT, FALL, WAVE (noms à confirmer et à vérifier contre les mots réservés).

## D004 : Liaison sélective (Décidé, 7 nov. 2026)
Le binaire ne contient que les routines des commandes utilisées. La bibliothèque est faite de modules Z80 indépendants avec dépendances déclarées. C'est aussi le mécanisme d'union avec le futur projet de commandes Z80.

## D005 : Langues (Décidé, 7 nov. 2026)
Interface (menus et messages) en français, anglais et espagnol, via des fichiers de langue. Bible en français seulement pour la V1. Traductions faites avec l'aide d'une IA, relues par la communauté.

## D006 : Bible en Markdown (Décidé, 7 nov. 2026)
Écrite une seule fois en Markdown, puis générée en HTML, PDF et aide intégrée. Les exemples sont compilés automatiquement.

## D007 : Diffusion et contributions (Décidé, 7 nov. 2026)
Projet gratuit et libre, porté par une seule personne. Pas de contributions de code directes au début : des testeurs et conseillers donnent des retours, qui sont intégrés par le porteur avec l'aide de l'IA. Ouverture possible plus tard, avec des règles écrites.

## D008 : Langage de développement (Décidé, 7 nov. 2026)
C# avec .NET et Avalonia, en trois couches (Core, Cli, Ide). Versions installées : .NET 10.0.403, Git 2.56.0.
**Essai réussi :** fenêtre Avalonia avec éditeur AvaloniaEdit (paquet Avalonia.AvaloniaEdit), numéros de ligne, coloration des mots-clés classiques, des nouvelles commandes, des nombres (décimaux, &hexa, &X binaires), des chaînes et des commentaires, barre d'état, copier/coller. Remplace l'état « Provisoire » d'origine. Alternative de repli, non retenue : Python avec Qt.
**Point à améliorer :** la colonne des numéros de ligne de l'éditeur colle aux numéros de ligne du BASIC (« 1 » puis « 10 » se lisent « 110 »). Ajouter un espace ou une couleur de fond distincte.

## D018 : Antivirus (Ouvert, 7 nov. 2026)
L'antivirus du porteur a bloqué l'accès aux fichiers du projet pendant l'essai. Les exécutables non signés produits par .NET déclenchent souvent de fausses alertes. À prévoir : documenter une exclusion du dossier du projet pour le développement, et étudier pour la distribution le dépôt des exécutables sur une plateforme connue (GitHub Releases), la publication des sommes de contrôle et, si possible, une signature de code.

## D009 : Prototype J0, adresse de chargement (Décidé, 7 nov. 2026)
Le programme Z80 du prototype est chargé et exécuté à &4000.
**Constat :** avec le BASIC, HIMEM par défaut interdit de charger un binaire à cette adresse (« Memory full »). Il faut `MEMORY &3FFF` avant `LOAD`.

## D010 : Chargeur BASIC généré (Décidé, 7 nov. 2026)
Le compilateur écrit un chargeur BASIC tokenisé (`MEMORY`, `LOAD`, `CALL`) en plus du binaire. `RUN"nom"` suffit à lancer le jeu. Le format tokenisé a été validé sur WinAPE : `LIST` affiche les lignes correctement. Le même chargeur servira au mode cassette.

## D011 : Fin de programme (Décidé, 7 nov. 2026)
`RUN` direct sur un binaire ne mémorise pas d'adresse de retour. Un programme compilé ne se termine jamais par `RET` : il finit par une boucle ou une attente de touche. Un `RET` reste possible seulement quand le programme est appelé par `CALL`.

## D012 : Format .dsk du prototype (Décidé, 7 nov. 2026)
Format standard, 40 pistes, une face, 9 secteurs de 512 octets, secteurs C1 à C9 sans entrelacement, remplissage &E5, répertoire sur les blocs 0 et 1, fichiers à partir du bloc 2, en-tête AMSDOS de 128 octets. Validé sur WinAPE.
**À revoir :** entrelacement des secteurs pour le matériel réel ; fichiers de plus de 16 Ko (plusieurs entrées de répertoire).

## D013 : Dépôt Git (Décidé, 7 nov. 2026)
Dépôt local créé dans `CpcProto`, branche `master`. Premier commit : « J0 : chargeur BASIC tokenisé, RUN HELLO fonctionne ». Le `.gitignore` standard .NET exclut `bin` et `obj`. L'adresse e-mail apparaît dans les commits : acceptée.

## D014 : Licence du code (Décidé, 7 nov. 2026)
Le code de l'éditeur et du compilateur est publié sous **GPL v3** : tout projet dérivé reste libre et ouvert, ce qui correspond à la philosophie de partage du projet. Le texte officiel de la licence se trouve dans un fichier `LICENSE` à la racine du dépôt, et chaque fichier source porte une courte mention de licence.
Licence de la bible : Creative Commons BY-SA 4.0 proposée (même esprit que la GPL pour la documentation), à confirmer.

## D017 : Licence de la bibliothèque Z80 (Décidé, 7 nov. 2026)
Les modules de `/runtime` sont copiés dans les programmes compilés par les utilisateurs. Sous GPL v3 pure, les jeux créés pourraient être considérés comme des œuvres dérivées et devraient eux-mêmes être publiés en GPL.
**Décision :** les modules de la bibliothèque sont publiés sous **GPL v3 avec une exception de liaison** : les jeux produits par le compilateur gardent la licence choisie par leur auteur (y compris commerciale). Tout le projet reste ainsi sous une seule famille de licences. La LGPL v3 a été écartée.
**Texte :** un projet de texte est dans `docs/RUNTIME-EXCEPTION.md`. Il s'inspire de l'exception de la bibliothèque d'exécution de GCC, que la Free Software Foundation fournit comme modèle. **À faire relire par une source compétente (FSF, forum de logiciel libre ou juriste) avant la première publication d'un module.**
**Règle d'application :** chaque fichier du dossier `/runtime` portera une mention de licence qui renvoie à cette exception.

## D019 : Lignes d'erreur et numéros de ligne BASIC (Décidé, 7 nov. 2026)
Les messages d'erreur indiquent la **ligne de l'éditeur** (à partir de 1), pas le numéro BASIC écrit au début de la ligne : cela marche aussi pour les programmes sans numéros. Les numéros de ligne BASIC sont acceptés mais facultatifs. Le colorateur et le compilateur lisent les lignes de la même façon. À revoir quand GOTO et GOSUB arriveront (étiquettes).

## D020 : Premier langage reconnu (Décidé, 7 nov. 2026)
Pour J1, le compilateur reconnaît `PRINT "texte"` (avec `;` final pour supprimer le saut de ligne), `PRINT` seul et `END`, avec `:` pour séparer les instructions. Les mots sont insensibles à la casse. Le compilateur signale toutes les erreurs d'un coup (reprise à l'instruction suivante). Les textes des messages sont provisoirement en français dans le code ; ils iront dans les fichiers de langue.

## D021 : Génération du code Z80 en J1 (Décidé, 7 nov. 2026)
- **Assembleur interne :** J1 utilise un petit assembleur écrit dans le projet (`Z80Emitter`), avec étiquettes et sauts relatifs. Il n'a que les instructions nécessaires ; on en ajoute au fil des besoins. La décision D015 (assembleur interne ou externe) reste ouverte pour les modules plus complexes.
- **Fin de programme :** conformément à D011, le programme compilé attend une touche (KM WAIT KEY, &BB18) puis redémarre la machine (`JP 0`). `END` saute à cette fin.
- **Organisation du binaire :** instructions du programme, fin de programme, modules de la bibliothèque utilisés, puis textes terminés par 0. Adresse de chargement et d'exécution : &4000.
- **Liaison sélective en action :** un programme sans `PRINT` ne contient aucun module de la bibliothèque.
- **Limite connue :** les caractères hors ASCII (accents) sont remplacés par « ? » dans les textes affichés. La table des caractères du CPC reste à gérer.
- **Fichiers produits :** un chargeur BASIC (`NOM.BAS`) et le binaire (`NOM.BIN`), noms de 8 caractères au plus. Lancement : `RUN"NOM"`.

## D022 : Éditeur : ouvrir, enregistrer, compiler (Décidé, 7 nov. 2026)
- Trois boutons (Ouvrir Ctrl+O, Enregistrer Ctrl+S, Compiler F5) et un panneau de messages sous l'éditeur.
- **Compiler enregistre toujours le programme d'abord**, puis crée le `.dsk` à côté du fichier `.bas`, avec le même nom.
- Un clic sur une erreur du panneau place le curseur sur la ligne concernée.
- Le nom du fichier `.bas` donne le nom des fichiers de la disquette : 8 caractères au plus.
- **Limite connue :** l'éditeur ne demande pas encore de confirmation quand on ouvre un fichier alors que le programme en cours contient des modifications non enregistrées.

## D023 : Raccourcis du Locomotive BASIC (Décidé pour « ? », Ouvert pour le reste, 7 nov. 2026)
- Le caractère `?` est accepté à la place de `PRINT`, comme sur le CPC (`10 ?"HELLO"`). Il est coloré comme un mot-clé. Le compilateur le traite exactement comme `PRINT`.
- **Le texte du programme reste tel que l'utilisateur l'a tapé** : l'éditeur ne remplace pas `?` par `PRINT`. Sur le CPC, c'est l'inverse : le BASIC range le programme sous forme de codes et `LIST` réécrit le mot complet. Une commande « Mettre en forme » (qui remplacerait les raccourcis par les mots complets) pourra être ajoutée plus tard.
- **Autres raccourcis à inventorier :** `'` à la place de `REM` est déjà accepté. Les autres abréviations du Locomotive BASIC (ou leur absence) restent à vérifier dans la documentation du firmware et par des essais dans WinAPE avant de les ajouter.
- **Mode direct :** taper une commande seule pour l'exécuter tout de suite (`?"COUCOU"`) est une fonction de l'interpréteur du CPC. Notre outil est un compilateur : le mode direct n'existe pas dans le projet, et aucune décision n'est prise à ce sujet.

## D024 : Bilan de J1 (Décidé, 7 nov. 2026)
- **J1 est terminé.** Critère atteint : un programme écrit dans l'éditeur se compile en `.dsk` et se lance dans WinAPE. 46 tests automatiques.
- **Les boucles passent en J2.** Le critère initial de J1 citait « PRINT, boucles » ; seuls `PRINT` et `END` existent. `FOR`/`NEXT` demandent des variables et des nombres, travail de J2.
- **Modules de la bibliothèque écrits en C# pour l'instant** (lambdas avec l'assembleur interne, dans `RuntimeLibrary.cs`). Le format de déclaration sous forme de fichiers, utilisable par le projet de commandes Z80, reste à définir (question ouverte, avant d'ouvrir le projet d'extension).

## D025 : Première version publique 0.1.0 (Décidé, 7 nov. 2026)
- **Publiée dès la fin de J1**, en version de test (« pre-release » sur GitHub), pour recueillir des retours tôt.
- **Windows 64 bits seulement.** Linux et macOS restent souhaitables mais ne sont pas livrés.
- **Dossier autonome** (`dotnet publish` avec .NET inclus) : aucune installation ni .NET pour l'utilisateur. Choix d'un dossier plutôt que d'un exécutable unique, pour limiter les fausses alertes antivirus. Taille importante (environ la centaine de Mo avant compression), acceptée pour cette version.
- **Non signée** : fausse alerte possible (D018). Mesures : somme de contrôle SHA256 dans la description de la version, code source public, note dans le `LISEZMOI.txt`.
- **Fabrication :** `packaging\build-release.cmd` produit `release\CpcBasic-0.1.0-win-x64.zip` ; le dossier `release` n'est pas enregistré dans Git (liste `.gitignore`).
- Le dossier contient l'éditeur, `examples\hello.bas`, `LISEZMOI.txt` et `LICENSE`. L'outil en ligne de commande n'est pas inclus dans cette version.

## D015 : Assembleur Z80 interne ou externe (Ouvert)
Écrire un assembleur dans le projet, ou s'appuyer sur un outil libre existant. À trancher en J1.

## D016 : ROM (Ouvert)
Ne pas redistribuer les ROM Amstrad. Comment les fournir : demander à l'utilisateur, ou ROM libres de remplacement. À trancher avant la publication.

## D026 : Types de J2 (Décidé, 8 nov. 2026)
Deux types de variables : entier signé 16 bits (`A` ou `A%`, sans déclaration) et octet non signé 8 bits (voir D032). Le suffixe fait partie du nom : `A` et `A%` sont des variables distinctes, comme dans le Locomotive BASIC. Différence assumée : dans le Locomotive, `A` est un réel. Réels et chaînes : étapes ultérieures (voir D016 pour les ROM).
Dépassement : un littéral hors intervalle est une erreur de compilation ; à l'exécution, le calcul boucle (modulo 256 pour l'octet, modulo 65536 pour l'entier), comportement natif du Z80, sans test coûteux.

## D027 : Variables et expressions (Décidé, 8 nov. 2026)
Variables allouées statiquement après les textes : 1 octet pour un octet, 2 octets (petit-boutiste) pour un entier. Seules les variables utilisées sont réservées ; valeur initiale 0. Expressions évaluées dans HL, opérande de gauche empilé. Lecture d'un octet : LD A,(var) / LD L,A / LD H,0. Écriture : LD A,L / LD (var),A. Les modules PrintInt, Mul16 et Div16 ne connaissent donc que HL. Les futurs tableaux hériteront du type de leur élément.

## D028 : Format de déclaration des modules (Ouvert, 8 nov. 2026)
Précise la question laissée ouverte par D024 : format de déclaration des modules de la bibliothèque sous forme de fichiers (source Z80 et métadonnées). À trancher avant l'ouverture du projet de commandes Z80.

## D029 : Suffixe du type octet (Remplacée par D032, 8 nov. 2026)
Le suffixe « ~ » avait été retenu provisoirement (« @ » écarté : adresse de variable du Locomotive BASIC). Abandonné : « ~ » demande AltGr sur clavier AZERTY. Voir D032.

## D030 : Numéro non utilisé
Réservé pendant la discussion sur le suffixe, finalement sans objet.

## D031 : Bilan de l'étape 2a (Décidé, 8 nov. 2026)
- Variables entières (A, A%) implicites ; variables octet déclarées avec BYTE (voir D032). Affectation avec ou sans LET, expressions avec + et - (parenthèses, moins unaire), PRINT d'expressions, PRINT avec plusieurs éléments séparés par « ; ». La virgule dans PRINT n'est pas gérée.
- Les types $ et ! sont refusés avec un message (UnsupportedVariableType).
- Nouveau module PrintInt, lié seulement si un nombre est affiché. Format vérifié sur WinAPE : espace devant (ou « - »), chiffres, espace final, identique au BASIC réel (`PRINT 5;"X"` donne « 5 X »).
- ExpectedString n'est plus produit (PRINT accepte les expressions).
- Validé sur WinAPE avec `examples/variable.bas`.
- Limite : le nom du fichier .bas est limité à 8 caractères (D022).

## D032 : Type octet par déclaration BYTE (Décidé, 8 nov. 2026)
Remplace D029. Une variable octet (0 à 255) se déclare avec `BYTE nom [, nom ...]`, sans suffixe, avant sa première utilisation. Les entiers (A, A%) restent implicites.
Choix de BYTE plutôt que CHAR : CHAR évoque un caractère de texte. BYTE n'est pas un mot réservé du Locomotive BASIC (vérifié dans WinAPE : `BYTE=5` fonctionne), mais un programme du Locomotive qui nomme une variable BYTE n'est plus compilable.
Erreurs : double déclaration, déclaration après usage, suffixe dans la déclaration, littéral hors de 0 à 255. Un calcul qui dépasse 255 boucle (modulo 256). Option OPTION EXPLICIT (déclaration obligatoire pour tous les types) possible plus tard. Complète D026 et D027.

## D033 : Boucles FOR/NEXT, EXIT, CONTINUE (Décidé, 8 nov. 2026)
- Syntaxe : `FOR v = départ TO limite [STEP pas] ... NEXT [v]`. Limite et pas : nombres écrits dans le programme (pas de variables pour l'instant). Pas non nul.
- Test au NEXT (corps exécuté au moins une fois), comme sur le CPC. Comparaison signée (bit 15 retourné puis comparaison non signée). Débordement de l'entier : fin de boucle.
- Variable de boucle : entier ou BYTE déclaré. BYTE : limite dans 0..255, pas dans -255..255.
- EXIT et CONTINUE : sauts résolus à la compilation (pile de boucles dans le générateur, aucun nettoyage sur la pile du Z80).
- Écart avec le Locomotive : après la boucle, la variable garde sa dernière valeur (le CPC lui laisse la valeur suivante).
- Mots réservés ajoutés : FOR, NEXT, TO, STEP, EXIT, CONTINUE (EXIT n'est pas utilisé par le Locomotive, vérifié dans WinAPE).
- Validé sur WinAPE avec `examples/boucles.bas`.

## D034 : Comparaisons et IF...THEN...ELSE (Décidé, 8 nov. 2026)
- Comparaisons = <> < <= > >= entre deux expressions, signées sur 16 bits (bit 15 retourné puis comparaison non signée par la retenue ; > et <= en échangeant les opérandes ; = et <> par SBC seul). AND/OR entre comparaisons, évaluation de gauche à droite avec arrêt anticipé ; AND passe avant OR.
- IF sur une ligne (compatible Locomotive) : tout le reste de la ligne appartient au IF, ELSE se rattache au IF le plus proche. IF en bloc (THEN en fin de ligne, ELSE, END IF ou ENDIF) : extension du projet, absente du Locomotive.
- Le parser émet IfStatement / ElseStatement / EndIfStatement ; la fin d'un IF sur une ligne est ajoutée automatiquement. Vérification des blocs avec une pile commune.
- Limites : condition = comparaison (pas de nombre seul), pas de NOT, pas de parenthèses autour d'une condition, pas de IF ... THEN numéro (GOTO viendra plus tard).
- Mots réservés ajoutés : IF, THEN, ELSE, ENDIF, AND, OR (ENDIF seul n'existe pas dans le Locomotive).
- Validé sur WinAPE avec `examples/cond.bas`.

## D035 : WHILE, REPEAT, DO...LOOP, EXIT/CONTINUE par type (Décidé, 8 nov. 2026)
- WHILE c ... WEND (test en haut), REPEAT ... UNTIL c (test en bas, sortie quand c est vrai), DO [WHILE c | UNTIL c] ... LOOP [WHILE c | UNTIL c] (une seule condition par DO).
- EXIT / CONTINUE [FOR | WHILE | REPEAT | DO] : saut résolu à la compilation vers la boucle la plus proche du type demandé (ou la plus proche tout court). CONTINUE revient au test.
- Le générateur garde une pile de boucles commune à tous les types ; l'émetteur Z80 reçoit AliasLabel pour donner deux noms à une même adresse.
- Pile de vérification commune à FOR, WHILE, REPEAT, DO et IF : ClosingWithoutOpening, UnclosedLoop, DoConditionTwice, BlocksNotNested.
- Mots réservés ajoutés : WHILE, WEND (Locomotive), REPEAT, UNTIL, DO, LOOP (à vérifier dans WinAPE comme EXIT).
- Validé sur WinAPE avec `examples/loops.bas`.

## D036 : Bilan de J2 (Décidé, 8 nov. 2026)
- **J2 est terminé côté langage.** Un programme avec variables, calculs simples, boucles et conditions se compile et se lance dans WinAPE. 175 tests automatiques, trois programmes d'exemple vérifiés à l'écran (`boucles.bas`, `cond.bas`, `loops.bas`).
- **Écarts avec le Locomotive BASIC** : consignés dans le tableau de la section 8 de `ARCHITECTURE.md` (version 0.3).
- **Pas encore faits, par ordre de priorité proposé pour J3** : multiplication, division et MOD (modules Mul16 et Div16) ; NOT ; GOTO et GOSUB avec étiquettes ; tableaux ; chaînes ; réels ; INPUT et clavier ; graphiques et sons ; DATA/READ.
- **Dettes connues** : accents (table des caractères du CPC), confirmation à l'ouverture d'un fichier modifié dans l'éditeur, colonne des numéros de ligne collée aux numéros BASIC, entrelacement du .dsk, fichiers de plus de 16 Ko, messages d'erreur à passer dans des fichiers de langue.
- **À traiter avant la prochaine publication** : D016 (ROM : vérifier que rien n'est redistribué) et relecture juridique de l'exception de liaison (D017).
## D037 : Multiplication, division et MOD (Décidé, 9 nov. 2026)
- Opérateurs `*`, `/` et `MOD` ; le signe `\` du Locomotive BASIC est accepté comme synonyme de `/`. `*`, `/` et `MOD` passent avant `+` et `-`. Le moins unaire se lie à son opérande.
- `/` est une division entière (7 / 2 = 3), tronquée vers 0. Écart avec le Locomotive, où `/` donne un réel. `MOD` : le reste a le signe du dividende (-7 MOD 3 = -1), comme `\` et MOD du Locomotive.
- Diviseur égal à un 0 écrit dans le programme : erreur de compilation (DivisionByZero). Diviseur calculé égal à 0 : pas d'erreur à l'exécution pour l'instant, le quotient vaut 0 et le reste vaut le dividende (à améliorer, voir IDEES.md).
- Modules Mul16 (les 16 bits de poids faible du produit : même résultat signé ou non) et Div16 (quotient dans HL, reste dans DE ; valeurs absolues puis signes corrigés), liés seulement s'ils sont utilisés.
- Dépassement : un produit trop grand boucle (200 * 200 = -25536), comme le reste des calculs (D026).
- Tests : un mini-émulateur Z80 (`tests/CpcBasic.Tests/MiniZ80.cs`) exécute les modules et compare leurs résultats à C# sur des milliers de valeurs. Il ne connaît que les instructions utilisées ; s'il en rencontre une inconnue, il le dit.
- Mot réservé ajouté : MOD (déjà un mot du Locomotive).
- Validé sur WinAPE avec `examples/calculs.bas`. 205 tests.

## D038 : Fonctions de calcul (Décidé, 9 nov. 2026)
- Syntaxe `NOM(valeur, ...)` dans une expression. Fonctions : ABS, SGN, MIN, MAX (existent dans le Locomotive, ici sur des entiers), HIBYTE, LOBYTE, CLAMP (nouvelles).
- HIBYTE(n) : octet de poids fort, 0 à 255. LOBYTE(n) : octet de poids faible, 0 à 255. Code court écrit directement dans le programme, sans module.
- CLAMP(valeur, mini, maxi) = MIN(MAX(valeur, mini), maxi) : ramène la valeur dans l'intervalle. Si mini > maxi, le résultat est maxi.
- MIN et MAX acceptent plus de deux valeurs : MIN(9, 4, 6) devient MIN(MIN(9, 4), 6) dès l'analyse.
- ABS(-32768) reste -32768 (dépassement). SGN donne -1, 0 ou 1.
- Modules Abs16, Sgn16, Min16, Max16 (comparaison signée par retournement du bit 15), liés seulement s'ils sont utilisés. CLAMP lie Max16 et Min16.
- Messages d'erreur : fonction sans parenthèses (ExpectedOpenParenthesis), mauvais nombre de valeurs (WrongArgumentCount) ; les deux montrent la bonne écriture, par exemple « CLAMP(valeur, min, max) ».
- Mots réservés ajoutés : ABS, SGN, MIN, MAX, HIBYTE, LOBYTE, CLAMP. HIBYTE, LOBYTE, CLAMP (et RANDOM) vérifiés libres dans WinAPE.
- Prévu ensuite : RANDOM(min, max) et RANDOMIZE (D039), puis les graphismes de base et le calcul 3D en fil de fer.

## D039 : RANDOM et RANDOMIZE (Décidé, 10 nov. 2026)
- `RANDOM(min, max)` : nombre entier de min à max compris (fonction à deux valeurs). `RANDOMIZE graine` fixe le départ : la même graine redonne toujours la même suite (utile pour tester un jeu). `RANDOMIZE` seul : le départ vient de l'horloge du CPC. Sans RANDOMIZE, le premier RANDOM prend aussi son départ dans l'horloge.
- Générateur : registre à décalage de 16 bits (polynôme &B400, période 65535, vérifiée par un test), avancé de 16 pas à chaque appel. Entiers seulement, aucun calcul à virgule : beaucoup plus court et plus rapide que le RND du Locomotive (pas de mesure chiffrée pour l'instant).
- Horloge : routine du firmware KL TIME PLEASE (&BD0D), qui rend le temps écoulé depuis l'allumage dans DEHL. Adresse à confirmer par l'essai (deux lancements de `hasard.bas` doivent donner des suites différentes sur la 3e ligne).
- Le résultat est le reste de la division d'un nombre de 15 bits par l'écart (max - min + 1). L'écart doit aller de 1 à 32767. Sinon (max plus petit que min, ou écart trop grand) le résultat est min et aucun nombre n'est consommé. Léger déséquilibre possible pour les très grands écarts, négligeable pour un dé ou un tirage de jeu.
- L'état du générateur est rangé dans le programme, juste après le code du module Rand16. État à 0 = « pas initialisé » : RANDOMIZE 0 devient la graine 1.
- Modules Rand16, Randomize (dépend de Rand16) et RandomRange (dépend de Rand16 et Div16), liés seulement s'ils sont utilisés.
- Mots réservés ajoutés : RANDOM (vérifié libre dans WinAPE) ; RANDOMIZE existe déjà dans le Locomotive (qui demande la graine au clavier quand elle manque : ici, l'horloge).
- Tests : le mini-émulateur exécute les modules et compare à la même suite écrite en C#.

## D040 : TIME, l'horloge du CPC (Décidé, 10 nov. 2026)
- `TIME` s'écrit sans parenthèses, comme dans le Locomotive BASIC. Il donne le temps écoulé depuis l'allumage de la machine, en 300 ièmes de seconde (routine du firmware KL TIME PLEASE, &BD0D). `RANDOMIZE TIME` fonctionne donc comme sur le CPC.
- Écart avec le Locomotive : TIME est ici un entier de 16 bits (les 16 bits de poids faible du compteur), alors que le Locomotive rend un réel qui ne repart presque jamais de zéro. Notre TIME recommence donc environ toutes les 3 minutes et 38 secondes, et passe aux nombres négatifs après 1 minute 49 secondes.
- Pour mesurer une durée, il faut écrire la différence : `t = TIME : WHILE TIME - t < 600 : WEND` attend 2 secondes, et reste exact même quand TIME repart de zéro, tant que la durée est inférieure à 1 minute 49 secondes. Écrire `WHILE TIME < t + 600` est faux près du retour à zéro.
- Code : un simple appel de la routine du firmware, sans module de la bibliothèque. On ne peut pas écrire `TIME = n` (erreur, comme sur le CPC).
- Mot réservé ajouté : TIME (déjà un mot du Locomotive).

## D041 : Accessibilité de l'éditeur (Décidé, 11 nov. 2026)
- **Taille du texte :** de 10 à 48 points, par pas de 2, 16 au départ. Ctrl + molette, Ctrl et « + » ou « - » (clavier ou pavé numérique), Ctrl+0 pour revenir à 16, ou les boutons « Texte plus petit » et « Texte plus grand ». Le choix est mémorisé dans `%APPDATA%\CpcBasicIde\reglages.json` (classe `EditorSettings`). Le panneau de messages reste à 16 points au moins, la barre d'état à 14.
- **Contraste élevé :** Ctrl+H ou bouton. Fond noir, texte blanc, numéros de ligne jaunes, couleurs vives (deuxième palette dans `BasicColorizer`). Choix mémorisé. La palette normale (papier bleu, stylo jaune, comme le MODE 1 du CPC) ne change pas.
- **Boutons :** 48 points de haut, texte de 18, avec leur raccourci écrit dans le nom. Ils passent à la ligne si la fenêtre est étroite. Trois boutons ajoutés : texte plus petit, texte plus grand, contraste.
- **Tout au clavier :** le curseur est dans l'éditeur dès l'ouverture. **F6** passe de l'éditeur au panneau de messages et inversement. Dans les messages : flèches haut et bas (le curseur de l'éditeur suit la ligne de l'erreur sans que le clavier quitte la liste), **Entrée** (retour dans l'éditeur sur la ligne de l'erreur), **Échap** (retour sans changer de ligne). Le message choisi est en jaune avec du texte noir, quel que soit le thème de Windows.
- **Leçons des essais :** le clavier doit être posé sur un message précis (pas sur la liste entière) pour que les flèches marchent ; la liste garde la touche Entrée pour elle, donc la fenêtre l'intercepte avant. Dans l'éditeur, Tab écrit une tabulation : on ne peut pas en sortir avec Tab, d'où F6.
- **Essayé à la main par l'utilisateur** : toutes les touches, le contraste, la mémorisation, F6, flèches, Entrée, Échap. Pas de test automatique (c'est de l'interface). Ctrl+« - » fonctionne sur le clavier AZERTY de l'utilisateur.
- **Pas encore fait :** essai avec un lecteur d'écran (des noms d'accessibilité sont posés sur l'éditeur, les messages et la barre d'état, sans vérification) ; numéros de ligne collés aux numéros BASIC (D008) ; confirmation à l'ouverture d'un fichier modifié (D022).

## D042 : Messages d'erreur qui nomment la commande (Décidé, 11 nov. 2026)
- **Principe :** un message dit quelle commande est concernée, ce qu'elle attend, et comment corriger. Il ne laisse pas penser à une contradiction : plus de « mais la fin de la ligne a été trouvée ». Idée de l'utilisateur.
- **Début commun :** « ERREUR en ligne N : » au lieu de « Ligne N : ». Le préfixe est ajouté en un seul endroit (`Diagnostic.Message`).
- **Commande :** `Diagnostic` reçoit un champ `Context` (nom de la commande : PRINT, FOR, IF...). Le parser le remplit : `ParseStatement` enveloppe `ParseStatementCore` et marque les erreurs sans commande avec le mot qui commence l'instruction (s'il est dans la liste `Commands`). Une instruction contenue dans une autre (`IF ... THEN PRINT ...`) garde sa propre commande.
- **Ligne incomplète :** quand l'erreur est à la fin de la ligne, rien n'est cité : « La ligne est incomplète. » Sinon : « « x » ne convient pas ici. »
- **Exemples :** `10 PRINT (` donne « PRINT attend ici une valeur (un nombre, une variable ou un calcul). La ligne est incomplète. » ; `10 PRINT (1 + 2` donne « Dans PRINT, il manque une parenthèse fermante « ) ». » ; `10 PRINT "BONJOUR` donne « un texte commence par un guillemet mais n'est jamais refermé. Ajoutez un guillemet à la fin du texte. »
- **Pas encore fait :** le message du guillemet manquant ne nomme pas la commande (l'erreur est trouvée avant l'analyse des commandes, dans le lexer) ; seules la valeur attendue (ExpectedExpression) et la parenthèse fermante (ExpectedClosingParenthesis) utilisent la commande, les autres messages gardent leur texte avec la nouvelle fin de phrase ; le texte reste en français dans `Diagnostic.cs` (fichiers de langue plus tard).
- **Tests :** `DiagnosticMessageTests.cs` (9 tests) ; un test de `LanguageTests.cs` attendait l'ancien début « Ligne 2 » et attend maintenant « en ligne 2 ». Total : 263 tests.

## D043 : Confirmation avant de perdre des modifications (Décidé, 11 nov. 2026)
- **Problème (D022) :** ouvrir un fichier, ou fermer la fenêtre, effaçait sans prévenir ce qui avait été tapé et pas enregistré.
- **Choix :** quand le programme est modifié (étoile `*` dans le titre), ouvrir un fichier ou fermer la fenêtre ouvre une boîte à trois gros boutons (texte de 18 points, 48 points de haut) : **Enregistrer** (Entrée), **Ne pas enregistrer** (touche N), **Annuler** (Échap). Un programme non modifié s'ouvre et se ferme sans question.
- **Sécurité :** fermer la boîte avec la croix compte comme « Annuler » (dans l'énumération `Choice`, `Cancel` est la première valeur, donc la valeur par défaut). Si l'enregistrement échoue ou si la personne annule le choix du nom du fichier, on ne continue pas : la fenêtre reste ouverte.
- **Réalisation :** nouvelle classe `SaveChangesDialog` (fenêtre écrite en C#, sans XAML) ; dans `MainWindow`, `ConfirmDiscardOrSaveAsync`, `OnClosing` (retient la fermeture le temps de la question, puis ferme avec `closeConfirmed`) et un appel au début de `OpenFileAsync`.
- **Essayé à la main par l'utilisateur** (ouverture, fermeture, N, Échap, croix, programme sans titre, programme non modifié) : aucun problème. Pas de test automatique (interface).
- **Pas encore fait :** pas de commande « Nouveau programme » dans l'éditeur ; pas d'ouverture par glisser-déposer. Reste de D022 : rien.

## D044 : Script commit.cmd et dossier essais (Décidé, 11 nov. 2026)
- **Problème :** les longues séries de commandes git à copier étaient difficiles à suivre (une ligne coupée a laissé deux fichiers hors d'un commit), et les fichiers d'essai de l'utilisateur apparaissaient dans `git status`.
- **Choix :** `packaging\commit.cmd`, à lancer par double-clic : lance `dotnet test` (arrêt si un test échoue), montre la liste des fichiers, demande confirmation, demande le message, fait `git add -A` puis `git commit`, propose `git push`. Le script crée le dossier `essais` et l'ajoute à `.gitignore` : les programmes d'essai s'y enregistrent, git les ignore. `examples\` reste réservé aux exemples du projet.
- **Essayé à la main par l'utilisateur** : premier commit réussi par ce script (« Ajout du script commit.cmd »).

## D045 : Numéros de ligne séparés, boutons compacts, largeur en colonnes (Décidé, 11 nov. 2026)
- **Numéros (D008) :** la colonne des numéros de l'éditeur était collée au numéro BASIC (« 1 » et « 10 » se lisaient « 110 »). Un espace de 14 points est ajouté entre cette colonne et le texte (un petit contrôle ajouté aux marges de gauche de l'éditeur), et les numéros de l'éditeur sont en gris (#B0B0B0, #C0C0C0 en contraste élevé), jamais de la couleur des numéros BASIC.
- **Boutons :** plus petits que dans D041 sur demande de l'utilisateur : 36 points de haut, texte de 16. Noms raccourcis : « Texte - (Ctrl+-) », « Texte + (Ctrl++) », « Contraste : non (Ctrl+H) ». La boîte de D043 garde ses gros boutons.
- **Largeur en colonnes :** la barre d'état affiche `[Larg : N]`, le nombre de caractères qui tiennent dans la largeur visible de la fenêtre d'édition (largeur de la zone de texte divisée par la largeur d'un caractère, la police étant à chasse fixe). Il se met à jour quand la fenêtre ou la taille du texte change. Raison : sur le CPC, on se sert de la largeur du MODE (20, 40 ou 80 colonnes) pour centrer le texte à l'écran.
- **Essayé à la main par l'utilisateur :** une ligne de 80 caractères tombe pile sur `[Larg : 80]`, numéros bien séparés, boutons satisfaisants.
- **Idée adoptée, pas faite :** une ligne verticale discrète dans l'éditeur à la colonne 20, 40 ou 80 selon le MODE (voir `IDEES.md`), quand `MODE` existera dans le langage.

## D046 : Commandes d'écran MODE, CLS, LOCATE, PEN, PAPER, INK, BORDER (Décidé, 12 nov. 2026)
- **Choix :** première étape des graphismes : l'écran et les couleurs, avec les mêmes mots et le même sens que dans le Locomotive BASIC. Le dessin (MOVE, PLOT, DRAW, CLG) viendra ensuite.
- **Écriture :** `MODE n` (0, 1 ou 2), `CLS`, `LOCATE colonne, ligne`, `PEN n` (0 à 15), `PAPER n` (0 à 15), `INK stylo, couleur [, couleur2]` (stylo 0 à 15, couleurs 0 à 26), `BORDER couleur [, couleur2]` (0 à 26). La seconde couleur, si elle est omise, est la même que la première (pas de clignotement). Les valeurs peuvent être des nombres, des variables ou des calculs, évalués de gauche à droite.
- **Réalisation :** aucun module Z80 : chaque commande est du code en ligne qui appelle le firmware. `SCR SET MODE` &BC0E (A = mode), `TXT CLEAR WINDOW` &BB6C, `TXT SET CURSOR` &BB75 (H = colonne, L = ligne), `TXT SET PEN` &BB90 (A), `TXT SET PAPER` &BB96 (A), `SCR SET INK` &BC32 (A = stylo, B et C = couleurs), `SCR SET BORDER` &BC38 (B et C). Toutes les adresses sont confirmées par les essais dans WinAPE. Un programme sans texte ne lie aucun module. Instruction ajoutée à `Z80Emitter` : `LdHE` (LD H,E).
- **Analyse :** nouvelle instruction `ScreenStatement(Command, Arguments)` ; table `ScreenCommands` du parser (écriture à montrer, nombre de valeurs, limites). Les valeurs **écrites en nombres** sont vérifiées à la compilation (nouveau code `ValueOutOfRange` : « MODE : la valeur 3 n'est pas permise. Valeurs possibles : 0 à 2. ») ; le mauvais nombre de valeurs réutilise `WrongArgumentCount` (« Écrivez MODE mode (0, 1 ou 2). »). Une valeur **calculée** n'est pas vérifiée : le firmware la reçoit telle quelle.
- **Limite de LOCATE :** colonne de 1 à 80 et ligne de 1 à 25 (maximum du MODE 2), car le compilateur ne connaît pas encore le MODE en cours : en MODE 0, `LOCATE 30, 1` passe la compilation.
- **Mots réservés (nouveaux) :** MODE, CLS, LOCATE, PEN, PAPER, INK, BORDER ne peuvent plus servir de noms de variables.
- **Constaté dans WinAPE :** changer de mode efface l'écran mais garde la palette (fond bleu et texte jaune dans les trois modes). En MODE 1, un caractère mesure 16 points de large et de haut sur la capture d'écran de WinAPE. 20, 40 et 80 chiffres remplissent exactement la largeur en MODE 0, 1 et 2. Après 80 caractères en MODE 2, le curseur passe déjà à la ligne : le `PRINT` suivant laisse une ligne vide.
- **Essayé à la main par l'utilisateur :** `examples\ecran.bas` (couleurs, positions, valeurs calculées) et `examples\modes.bas` (les trois modes, avec une règle de chiffres pour chaque mode). Tests : `ScreenCommandTests.cs`, 32 cas ; total 295 tests réussis.
- **Pas encore fait :** le dessin ; la vérification des valeurs calculées ; les limites de LOCATE selon le MODE ; la fenêtre de texte (WINDOW) ; l'éditeur qui lit le MODE du programme pour tracer la ligne verticale de la largeur d'écran (idée adoptée, voir `IDEES.md`).

---

## Notes de fin de session (modèle)

```
Date :
Objectif de la session :
Fait :
Reste à faire :
Problèmes rencontrés :
Décisions à consigner :
```
