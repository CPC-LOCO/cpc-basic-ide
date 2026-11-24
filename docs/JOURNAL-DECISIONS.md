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

## D047 : Commandes de tracé du Locomotive BASIC (Décidé, 12 nov. 2026)
- **Choix :** même écriture et même sens que le Locomotive BASIC, pour la compatibilité : `MOVE`, `MOVER`, `PLOT`, `PLOTR`, `DRAW`, `DRAWR` (`x, y [, [stylo] [, mode]]`, le stylo peut rester vide : `PLOT 1, 2, , 1`), `CLG [stylo]`, `ORIGIN x, y`. Coordonnées de 0 à 639 en largeur et 0 à 399 en hauteur dans tous les modes, origine en bas à gauche.
- **Hors écran :** aucune vérification ; le firmware coupe le tracé comme le vrai BASIC. Le stylo (0 à 15) et le mode (0 à 3) écrits en nombres sont vérifiés à la compilation (`ValueOutOfRange`).
- **Réalisation :** code en ligne, aucun module. Firmware : `GRA MOVE ABSOLUTE` &BBC0, `MOVE RELATIVE` &BBC3, `SET ORIGIN` &BBC9, `CLEAR WINDOW` &BBDB, `SET PEN` &BBDE, `SET PAPER` &BBE4, `PLOT ABSOLUTE` &BBEA, `PLOT RELATIVE` &BBED, `LINE ABSOLUTE` &BBF6, `LINE RELATIVE` &BBF9, `SCR ACCESS` &BC59 (mode d'écriture). Nouvelle instruction `DrawStatement`. Nouveaux mots réservés : MOVE, MOVER, PLOT, PLOTR, DRAW, DRAWR, CLG, ORIGIN.
- **Constaté dans WinAPE :** le mode graphique (XOR, AND, OR) reste actif jusqu'à ce qu'on le change, et **MODE ne le remet pas à zéro** (vérifié par l'utilisateur avec le vrai BASIC) : notre `MODE` fait donc comme le vrai. Un point isolé de PLOT est d'un seul pixel : invisible au croisement de deux lignes.
- **Essayé à la main par l'utilisateur :** `examples\dessin.bas` (cadre, diagonales, carré, L, points) et `examples\xor.bas` conformes ; 323 tests réussis.
- **Décision pour les futures commandes (CIRCLE, PAINT, 3D...) :** coordonnées propres, origine en **haut à gauche**, **200 lignes** dans les 3 modes ; largeur 0 à 639 (MODE 2), 0 à 319 (MODE 1), MODE 0 à confirmer (l'écran fait 160 points : 0 à 159). Ces commandes devront connaître le MODE en cours à l'exécution : `MODE` devra le mémoriser.
- **Pas encore fait :** `ORIGIN` à quatre valeurs de fenêtre, `GRAPHICS PEN`, `TEST`, `TESTR`, `XPOS`, `YPOS`, `FILL`, `MASK`.

## D048 : Tracé direct dans la mémoire écran et commande BOX (Décidé, 12 nov. 2026)
- **Question de l'utilisateur :** les coordonnées propres (origine en haut à gauche, 200 lignes, largeur selon le MODE) rendraient-elles CIRCLE, PAINT, BOX, etc. plus rapides ?
- **Réponse et choix :** oui, mais seulement si ces commandes écrivent **directement dans la mémoire écran** : le firmware travaille en 640 x 400 et reconvertit chaque point en pixel réel à chaque appel ; avec des coordonnées natives, l'adresse de l'octet se calcule sans conversion. Les nouvelles commandes (hors Locomotive) utilisent donc : origine en **haut à gauche**, y de **0 à 199**, x de **0 à 159** (MODE 0, confirmé par l'utilisateur le 12 nov.), **0 à 319** (MODE 1), **0 à 639** (MODE 2). Les commandes du Locomotive (D047) gardent leur repère (origine en bas à gauche, 640 x 400). Ce qui sort de l'écran est ignoré, rien n'est écrit hors de l'écran.
- **Remplace une décision de D047 :** il n'est **pas nécessaire que `MODE` mémorise le mode** : la routine le demande au firmware à chaque commande (`SCR GET MODE`, &BC11 : A = 0, 1 ou 2). `MODE` n'a pas changé.
- **BOX :** `BOX x1, y1, x2, y2 [, stylo]` trace le **contour** d'un rectangle. Stylo de 0 à 15 (1 s'il est omis). Les coins peuvent être donnés dans n'importe quel ordre (ils sont rangés avec `Min16` et `Max16`) et peuvent sortir de l'écran. Nouveau mot réservé : **BOX** (déjà coloré dans l'éditeur comme nouvelle commande). C'est un `ScreenStatement` de commande « BOX ». Les valeurs peuvent être des nombres, des variables ou des calculs.
- **Réalisation :** deux nouveaux modules Z80, liés seulement si le programme utilise BOX (avec `Min16` et `Max16`) :
  - `Pixel` : `rt_px_setup` (A = stylo ; lit le MODE et prépare des tables), `rt_pixel` (un point), `rt_px_hline` (ligne horizontale **par octets entiers**, avec octets partiels aux deux bouts), `rt_px_vline` (ligne verticale qui descend d'une ligne écran à la fois : +&0800, puis +&C050 au passage à la ligne de caractères). Tables de 16 stylos par MODE, masques « du point p à la fin de l'octet » et « du début au point p ». Le point de chaque octet suit l'entrelacement des bits du CPC (MODE 0 : bits 7,5,3,1 et 6,4,2,0 ; MODE 1 : 7-4 et 3-0 ; MODE 2 : un bit par point).
  - `Box` : `rt_box` coupe la ROM haute, appelle `rt_box_draw`, puis la remet (états conservés avec `PUSH AF` / `POP AF`).
- **ROM haute (important) :** un programme lancé depuis le BASIC a la ROM du BASIC visible de &C000 à &FFFF **en lecture** (l'écriture va dans la RAM). Les routines relisent l'octet de l'écran pour les points partiels : sans précaution, elles fusionneraient des octets de ROM dans l'image. `BOX` appelle donc `KL U ROM DISABLE` (&B903, rend l'état précédent dans A) avant le tracé et `KL ROM RESTORE` (&B90C, A = état précédent) après. **Toute future commande à écriture directe devra faire pareil.**
- **Limites connues :** l'écran est supposé à &C000 sans défilement matériel : après un `PRINT` qui fait défiler l'écran vers le haut, les coordonnées seraient décalées ; le contour n'est pas rempli (BOX rempli viendra) ; les coins du contour sont écrits deux fois (sans effet en écriture normale, à revoir quand le mode XOR existera pour ces commandes) ; le mode d'écriture (XOR, AND, OR) n'existe pas encore pour les commandes à écriture directe ; le stylo est pris modulo 16 (valeur calculée).
- **Vérification (sans compilateur .NET sur place) :** les deux modules ont été écrits une seule fois dans une description à part, dont ont été tirés le code C# et les octets, puis exécutés dans un simulateur Z80 écrit séparément (décodage par règles du processeur) et comparés à une description indépendante de la mémoire écran du CPC : 2010 points (dont tous les bords et les points hors écran), 138 rectangles (coins inversés, hors écran), 2400 lignes horizontales et verticales, dans les 3 MODES, tous identiques ; aucune écriture hors de l'écran. Contre-épreuve : sans la coupure de la ROM haute, les rectangles de test sont faux. **Vitesse** (en instructions Z80, rectangle plein écran en MODE 2) : 144 240 avec un point à la fois, **5 716** avec les lignes par octets, soit environ 25 fois moins. Le C# a ensuite été compilé par l'utilisateur sans aucune erreur.
- **Autres changements :** `Z80Emitter` : 26 instructions ajoutées (AND, ADD A, ADC A, CPL, SRL, RR E, CP B, SUB C, JR C, PUSH/POP AF, RET NZ, LD DE,(nn), etc.). `MiniZ80` : mêmes instructions, plus `ScreenMode` (réponse de &BC11), `UpperRomEnabled` (masque de la ROM haute en lecture, coupée par &B903, remise par &B90C), et la pile placée à &BF00 (elle était à &F000, c'est-à-dire dans l'écran).
- **Tests :** nouveau fichier `tests\CpcBasic.Tests\BoxTests.cs`, 28 cas (module `Pixel` et `Box` sur le mini-Z80 ; analyse, messages d'erreur et octets de BOX). Total attendu : 351.
- **Essayé à la main par l'utilisateur :** 351 tests réussis ; `examples\boite.bas` conforme dans WinAPE : MODE 0 et MODE 1, trois cadres (jaune, cyan, rouge) dont le jaune touche les quatre bords ; MODE 1, deux lignes rouges pleine largeur (coins à l'envers et hors écran) ; MODE 2, trois cadres jaunes et le texte centré. La ROM haute et les trois adresses du firmware (&BC11, &B903, &B90C) sont donc confirmées.
- **Pas encore fait :** BOX rempli, CIRCLE, ELLIPSE, PAINT, STAR, POLYGON (ils s'appuieront sur `Pixel`) ; mode XOR pour ces commandes ; tenir compte du défilement matériel (SCR DOT POSITION) ; ligne verticale dans l'éditeur à la largeur du MODE (idée adoptée dans D045).

## D049 : PAUSE et CLIC, réservés pour plus tard (Annulé, 12 nov. 2026)
- **Ce qui s'est passé :** j'avais créé de ma propre initiative une commande `PAUSE` (attendre une touche). L'utilisateur ne l'avait pas demandée, et elle ne fonctionnait pas. Elle est **retirée** (analyseur, générateur, coloration, tests) ; `examples\boite.bas` retrouve ses boucles de temps. Total de tests attendu : retour à 351.
- **Règle :** ne pas ajouter de commande ni de mot réservé sans que l'utilisateur l'ait demandé.
- **`PAUSE` est réservé à la gestion du temps :** `PAUSE [heure,][minute,] seconde`. À faire plus tard, avec la gestion de l'heure.
- **Idée notée, pas à faire maintenant :** `CLIC code_ascii` (code à partir de 31) : attend l'appui sur la touche de code ASCII donné. À reprendre quand l'utilisateur le décidera.
- **Pour les captures d'écran en attendant :** le bouton pause (II) de WinAPE.

---

## D050 : rappel « fermez l'émulateur » (Décidé, 12 nov. 2026)
- **Problème :** si l'émulateur a déjà le `.dsk` ouvert, Windows refuse l'écriture et le programme lancé est l'ancien (cause de l'échec apparent de `PAUSE`). Le message d'erreur était en anglais, coupé et sans conseil.
- **Choix (idée de l'utilisateur) :** pas de détection d'émulateur (il y en a trop). L'éditeur écrit un message en français sur plusieurs lignes, et rappelle après chaque compilation réussie de fermer l'émulateur avant de recompiler. Même phrase dans `packaging\LISEZMOI.txt`.
- **Fichiers :** `src\CpcIde\MainWindow.axaml.cs`, `packaging\LISEZMOI.txt`. Aucun test (interface).
- **Validé par l'utilisateur (13 nov.) :** le message d'erreur en français s'affiche avec WinAPE ouvert, et la 4e ligne de rappel s'affiche après une compilation réussie.

---

## D051 : BOX, contour et remplissage (Décidé, 12 nov. 2026)
- **Choix (demande de l'utilisateur) :** `BOX x1, y1, x2, y2 [, stylo [, remplissage]]`.
  - 4 valeurs : contour seul, avec le **stylo courant du tracé** (celui de PLOT et DRAW), lu par `GRA GET PEN` (&BBE1, A = stylo).
  - 5 valeurs : contour seul avec le stylo donné.
  - 6 valeurs : contour avec le 5e nombre, intérieur rempli avec le 6e (de 0 à 15). Aucun nouveau mot réservé.
- **Pourquoi :** ce sont des commandes nouvelles, elles doivent être très rapides et efficaces.
- **Réalisation :** le programme range les coins, le stylo du contour (`rt_box_pen`), le stylo de l'intérieur (`rt_box_fillpen`) et l'indicateur `rt_box_dofill`. L'intérieur est rempli d'abord (strictement entre les côtés, découpé à l'écran, une ligne horizontale par ligne par octets entiers), puis le contour par-dessus : **aucun point n'est écrit deux fois**. Les coins sont ramenés dans [-1, max + 1] avant tout calcul, donc aucun dépassement même vers ±32767. Même coupure de la ROM haute que le contour seul.
- **Validé par l'utilisateur :** 357 tests réussis ; `examples\plein.bas` conforme dans WinAPE (3 MODES), y compris BOX à 4 valeurs : le contour prend bien le stylo réglé par `MOVE 0, 0, 2` (cyan). L'adresse &BBE1 (GRA GET PEN, documentation cpcwiki) est donc confirmée.
- **Vérifié par simulation (hors .NET) :** 480 rectangles (contour seul et avec remplissage, MODE 0, 1, 2, coins inversés, hors écran, très fins, aux limites ±32768) : aucun écart avec la référence. Plein écran MODE 2 : environ 83 000 instructions Z80 (contour seul : 5 700).
- **Tests :** `tests\CpcBasic.Tests\BoxTests.cs`. Total attendu : 357 (351 + 6).
- **Exemple :** `examples\plein.bas`.
- **Limites :** même hypothèse d'écran à &C000 ; pas de mode XOR.

## D052 : règle pour toutes les futures formes (Décidé, 12 nov. 2026)
- Les nouvelles commandes de dessin (CIRCLE, ELLIPSE, STAR, POLYGON, et celles qui suivront) suivent le modèle de BOX : sans stylo, contour au stylo courant ; avec un stylo, contour dans ce stylo ; avec un second stylo, contour et remplissage.
- Elles doivent être très rapides (écriture directe, octets entiers quand c'est possible, aucun point écrit deux fois) et exactes (découpe à l'écran, aucun dépassement).
- Chaque forme est vérifiée contre une référence écrite à part, dans les trois MODES.

## D053 : inventaire de compatibilité avec le Locomotive BASIC (À valider, 13 nov. 2026)
- **Problème (question de l'utilisateur) :** on ajoute des commandes nouvelles (BOX, bientôt CIRCLE…) avant d'avoir toutes celles du Locomotive BASIC. Risque de s'embrouiller, et de réserver des mots qui gênent des programmes Locomotive.
- **Choix :** créer `docs\COMPATIBILITE-LOCOMOTIVE.md` : toutes les instructions, fonctions et opérateurs du Locomotive (liste de cpcwiki), avec leur état relevé dans le code : faite, partielle, écart voulu, à faire, mode direct. Ajouts du projet et mots réservés dans une section à part. Ordre de travail **proposé** (lots A à J), à valider.
- **Règles proposées :** un nom de commande nouvelle ne peut pas être un mot du Locomotive ; chaque nouvelle commande est ajoutée à la section « Ajouts du projet » du fichier, qui est mis à jour au même moment.
- **Résultat de l'inventaire :** 157 entrées du Locomotive : 19 faites, 12 partielles, 2 écarts voulus, 117 à faire, 7 en mode direct (proposées « sans objet »). Les blocages principaux : GOTO/GOSUB, chaînes, réels, tableaux et données, accès machine, clavier et manettes.
- **Réponse à la question :** pas de danger à ajouter CIRCLE avant, tant que ces règles sont suivies ; les commandes de dessin prennent des entiers qui resteront valables avec les réels.

## D054 : lot A, sauts et sous-programmes (Décidé, validé le 13 nov. 2026)
- **Demande :** « Les lots en priorité » : GOTO, GOSUB, RETURN, ON … GOTO / GOSUB, STOP avant CIRCLE. Rien d'inventé : tous ces mots existent dans le Locomotive BASIC.
- **Choix :** chaque ligne numérotée reçoit une étiquette `line_N` dans le code produit (zéro octet). `GOTO n` = JP, `GOSUB n` = CALL, `RETURN` = RET, `STOP` = comme END. `ON x GOTO/GOSUB a,b,c` : chaîne de comparaisons de x avec 1, 2, 3… (sauts absolus, donc aucune limite sur le nombre de lignes).
- **Règles :** `GO TO` et `GO SUB` en deux mots acceptés ; `IF … THEN 100 ELSE 200` accepté ; numéro entre 1 et 65529 ; deux lignes de même numéro, ou un saut vers une ligne absente, sont refusés avec un message en français (ex. « ERREUR en ligne 1 : GOTO : la ligne 500 n'existe pas. Vérifiez le numéro de ligne. »).
- **Limites connues :** cible = nombre écrit en chiffres (pas de GOTO calculé, qui n'existe pas non plus en Locomotive) ; RETURN sans GOSUB non détecté ; ON avec valeur hors de 1 à N : le programme continue ; STOP sans message « Break » ; ON BREAK / ON ERROR / ON SQ refusés (lots G et I).
- **Fichiers :** Ast.cs, Diagnostic.cs, Parser.cs, CodeGenerator.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (4 sauts conditionnels ajoutés), `tests\CpcBasic.Tests\JumpTests.cs` (39 tests), `examples\sauts.bas`.
- **Validé par l'utilisateur le 13 nov. 2026** (396 tests ; `examples\sauts.bas` dans WinAPE).

## D055 : lot B, clavier et manettes (INKEY, JOY, CLEAR INPUT validés ; INPUT validés, 13 nov. 2026)
- **Demande :** terminer tous les lots de l'inventaire avant d'ajouter d'autres commandes. Lot B = INKEY, INKEY$, JOY, INPUT.
- **Fait dans cette étape :** `INKEY(n)` (-1 si la touche n'est pas appuyée, sinon 0, 32 Shift, 128 Ctrl, 160 les deux), `JOY(0)` / `JOY(1)` (bits : 1 haut, 2 bas, 4 gauche, 8 droite, 16 tir 2, 32 tir 1), `CLEAR INPUT`. Appels au firmware : KM TEST KEY &BB1E, KM GET JOYSTICK &BB24, KM READ CHAR &BB09 (aucun module de bibliothèque : le code est écrit sur place).
- **Confirmé dans WinAPE (13 nov. 2026) :** INKEY(47) = -1 sans appui, 0 avec appui ; JOY(0) = 0 sans appui. Hypothèse de départ : KM TEST KEY rend Z = touche non appuyée, et C = Shift (bit 5) / Ctrl (bit 7) ; KM GET JOYSTICK rend H = manette 0, L = manette 1. Je n'ai pas pu relire la documentation du firmware (accès refusé) : c'est de mémoire. `examples\clavier.bas` sert à le vérifier.
- **INPUT (nombres entiers, validé le 13 nov. 2026 : 450 tests, `examples\saisie.bas` conforme) :** `INPUT [;] ["texte" ; ou ,] var [, var ...]`. Texte suivi de `;` : « ? » après le texte ; suivi de `,` : pas de « ? » ; sans texte : « ? ». Écho des touches, Suppr efface, Entrée valide, 80 caractères au plus. Entiers de -32768 à 32767, avec signe et espaces permis. Plusieurs nombres séparés par des virgules ; s'il en manque, « ?? » demande la suite. Texte faux ou trop grand : « ?Redo from start » et on recommence. Reste de ligne non lu : « ?Extra ignored ». Variable BYTE : refus au-dessus de 255. Un `;` juste après INPUT garde le curseur sur la ligne. Module `Input` de la bibliothèque (KM WAIT CHAR &BB06).
- **Limites d'INPUT :** pas de nombres à virgule (lot F) ni de chaînes (lot E) ; une ligne vide donne « ?Redo from start » (je ne sais pas ce que fait exactement le Locomotive dans ce cas : à comparer sur un vrai CPC si possible).
- **Reste du lot B :** `INKEY$` attend les chaînes (lot E) car il rend du texte ; `LINE INPUT` aussi.
- **Limites :** `JOY` avec une valeur autre que 0 ou 1 lit la manette 1 (le Locomotive donnerait « Improper argument »). Mots maintenant réservés : INKEY, JOY, CLEAR.
- **Fichiers :** Ast.cs, Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (adresses), `tests\CpcBasic.Tests\MiniZ80.cs` (touches, manettes, LD H,n), `tests\CpcBasic.Tests\KeyboardTests.cs` (18 tests), `examples\clavier.bas`, et pour INPUT : `tests\CpcBasic.Tests\InputTests.cs` (36 tests), `examples\saisie.bas`.

## D056 : lot C, première partie : accès à la machine (Décidé, validé le 13 nov. 2026)
- **Fait :** `POKE adresse, valeur`, `PEEK(adresse)`, `CALL adresse`, `OUT port, valeur`, `INP(port)`. Mots réservés ajoutés : POKE, PEEK, CALL, OUT, INP.
- **Adresses et ports :** de 0 à 65535 comme sur le CPC (un nombre seul de 32768 à 65535 vaut le même mot de 16 bits : 49152 = -16384), ou de -32768 à 32767. Dans un calcul, un nombre au-dessus de 32767 reste refusé (nos entiers vont de -32768 à 32767) : écrire `-16384 + I` et non `49152 + I`. Cela change avec les nombres hexadécimaux et les réels (lots D et F).
- **Valeur de POKE et OUT :** de 0 à 255, contrôlée à la compilation si c'est un nombre écrit ; si elle est calculée, seul l'octet de poids faible est écrit (le Locomotive donnerait « Improper argument »).
- **CALL :** adresse seule ; les paramètres facultatifs du Locomotive (`CALL adr, p1, p2…`) ne sont pas gérés (message « CALL adresse »). Le sous-programme rend la main avec RET.
- **ROM haute :** POKE écrit toujours dans la RAM. À partir de &C000, le Z80 lirait la ROM du BASIC au lieu de la mémoire (écran) : PEEK coupe donc la ROM haute un instant (KL U ROM DISABLE &B903) puis la remet comme avant (&B90C), comme BOX. Au-dessous de &C000, PEEK lit directement. (Découvert par un test en échec : le mini-Z80 simule cette ROM ; PEEK(65535) rendait 90 au lieu de 9. **Confirmé par l'utilisateur dans SugarBox (13 nov. 2026) :** `POKE -1, 9` puis `PRINT PEEK(65535)` donne 9 : le Locomotive BASIC lit bien la RAM à ces adresses.)
- **Pas encore fait dans le lot C :** `DIM` (tableaux), `DATA` / `READ` / `RESTORE` (`MEMORY` : voir D057).
- **Fichiers :** Z80Emitter.cs (JP (HL), IN, OUT), Parser.cs, CodeGenerator.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (JP (HL), ports), `tests\CpcBasic.Tests\MachineTests.cs` (21 tests), `examples\machine.bas`.

## D057 : MEMORY, et programme de référence RSX (Décidé, validé le 13 nov. 2026)
- **Demande de l'utilisateur :** MEMORY doit être acceptée et exécutée, car un programme BASIC peut contenir du binaire (exemple : programme en deux lignes qui crée des commandes RSX avec `MEMORY p-1` puis POKE et CALL). Objectif : « compatibilité maximale ». Le programme est gardé dans `docs\PROGRAMME-DE-REFERENCE-RSX.md` avec la liste de ce qui lui manque.
- **Choix :** un programme compilé n'a pas de mémoire BASIC à régler (le BASIC ne tourne plus après le chargement). L'effet utile de MEMORY est donc de **protéger le haut de la mémoire** : `MEMORY adresse` est accepté (adresse de 0 à 65535, comme POKE) et le compilateur **vérifie** que le programme compilé (code, textes, variables, qui commence en &4000) finit au plus tard à cette adresse ; sinon : « MEMORY : le programme compilé occupe de &4000 à &xxxx, ce qui dépasse la limite &yyyy demandée ». La vérification n'est possible que si l'adresse est un nombre écrit ; avec un calcul (`MEMORY p-1`), la commande est acceptée sans contrôle. Avec plusieurs MEMORY, la limite la plus basse compte. Aucun octet n'est ajouté au programme.
- **À décider plus tard (question à l'utilisateur) :** la fonction `HIMEM` (valeur de départ ? la relire après MEMORY ?) ; les commandes RSX `|NOM,params` et le paramètre `@variable` : ce sont des mots du Locomotive (mécanisme d'extension), pas des inventions, mais leur fonctionnement dans un programme compilé demande un choix.
- **Validé par l'utilisateur le 13 nov. 2026** (D056 et D057 : tests réussis, `examples\machine.bas` conforme, commit « Lot C : POKE, PEEK, CALL, OUT, INP, MEMORY »).
- **Fichiers :** Diagnostic.cs (ProgramTooLarge), Parser.cs, CodeGenerator.cs (GeneratedProgram.MemoryLimit), Compiler.cs, `tests\CpcBasic.Tests\MemoryTests.cs` (10 tests), `docs\PROGRAMME-DE-REFERENCE-RSX.md`.

## D058 : lot C, tableaux : DIM (Décidé, validé le 13 nov. 2026)
- **Demande de l'utilisateur :** « la compatibilité avant tout » : taille écrite ou calculée (`DIM A(N)`), comme le Locomotive ; un numéro de case hors du tableau est **toujours vérifié** et arrête le programme avec un message, comme le Locomotive ; un exemple qui montre l'erreur.
- **Fait :** `DIM A(10)` (cases 0 à 10), plusieurs dimensions (`DIM T(2,3)`, `DIM C(1,2,3)`), plusieurs tableaux par DIM, taille écrite ou calculée. Lecture `A(I)` dans tout calcul, écriture `A(I) = valeur` (avec ou sans LET). Un tableau utilisé sans DIM reçoit les cases 0 à 10 dans chaque dimension, comme le Locomotive. Le tableau `A()` et la variable `A` sont distincts. Tableaux d'entiers seulement. Mot réservé ajouté : DIM.
- **Mémoire :** les cases (2 octets, à 0 au départ) sont prises au lancement dans la mémoire libre, juste après le programme, jusqu'à &A67B (le HIMEM du BASIC avec la disquette). Le fichier sur la disquette ne grossit donc pas. `MEMORY adresse` abaisse cette limite pendant l'exécution (si le programme a des tableaux).
- **Erreurs pendant l'exécution (mêmes textes que le Locomotive) :** « Subscript out of range in 30 » (case hors du tableau, numéro négatif, `DIM A(-1)` calculé), « Array already dimensioned in 20 » (deuxième DIM, ou DIM après une utilisation sans DIM), « Memory full in 10 » (tableau trop grand, ou MEMORY sous les tableaux déjà créés). Le message va à la ligne si le curseur n'est pas au début ; puis le programme attend une touche et redémarre, comme à la fin. Sans numéros de ligne, c'est le numéro de ligne de l'éditeur qui est affiché. Seules les instructions qui touchent un tableau notent leur numéro de ligne (6 octets).
- **Vérifications à la compilation :** même nombre de dimensions partout pour un même tableau ; taille écrite de 0 à 32766 ; un nom déclaré BYTE ne peut pas encore être un tableau ; un mot du Locomotive pas encore disponible (SIN, ERASE, READ…) ne peut pas devenir un nom de tableau (sinon `SIN(3)` aurait été pris pour une case, sans message) ; tableaux `$` et `!` refusés pour l'instant.
- **Rapidité :** une dimension : un appel court (vérification + adresse) ; plusieurs dimensions : une routine générale avec multiplications.
- **Plus tard :** tableaux d'octets avec `BYTE T` avant `DIM T(100)` (aucune commande nouvelle ; voir IDEES.md) ; `ERASE` ; `INPUT A(I)` et `READ A(I)` (avec DATA / READ, prochaine étape) ; tableaux de textes et de réels (lots E et F).
- **Validé par l'utilisateur le 13 nov. 2026 :** `examples\tableaux.bas` et `examples\horstab.bas` conformes dans WinAPE (« Subscript out of range in 80 », la ligne 90 ne s'affiche pas). Le mini-Z80 des tests a reçu ADC HL,DE et JP PE (utilisés par NEXT, jamais simulés avant) : 534 tests.
- **Fichiers :** Ast.cs (DimStatement, ArrayElementExpression, ArrayAssignStatement, AstWalker), Diagnostic.cs (4 messages), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (module Array), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (instructions et TXT GET CURSOR &BB78), `tests\CpcBasic.Tests\ArrayTests.cs` (53 tests), `examples\tableaux.bas`, `examples\horstab.bas` (erreur voulue).

## D059 : lot C, DATA, READ, RESTORE (Décidé, validé le 14 nov. 2026)
- **Fait :** `DATA nombre [, nombre ...]`, `READ cible [, cible ...]`, `RESTORE [ligne]`, et `INPUT` dans une case de tableau (`INPUT T(I)`). Une cible est une variable ou une case de tableau (`READ A, T(I)`). Mots réservés ajoutés : DATA, READ, RESTORE.
- **Comme le Locomotive :** les DATA peuvent être n'importe où dans le programme (même après END) et sont lus dans l'ordre des lignes ; `RESTORE` repart au premier DATA ; `RESTORE 200` repart au premier DATA de la ligne 200 ou après (une ligne absente est signalée à la compilation, comme pour GOTO) ; plus rien à lire : « DATA exhausted in 50 », puis attente d'une touche et redémarrage.
- **Nombres :** entiers de -32768 à 65535 ; de 32768 à 65535, le même mot de 16 bits que sur le CPC (comme POKE, D056) : `DATA 40000` relu dans A donne -25536, et `POKE A, …` vise bien l'adresse 40000. Dans une variable BYTE, READ garde l'octet de poids faible.
- **Pas encore :** textes dans DATA (`DATA "ABC"` ou `DATA BONJOUR` : message clair ; viendront avec les chaînes, lot E, nécessaires au programme RSX de référence) ; nombres hexadécimaux `&C9` (lot D) ; réels (lot F).
- **Réalisation :** tous les nombres des DATA forment une table de mots de 16 bits à la fin du programme (seulement si le programme contient READ ou RESTORE) ; le module `Data` (`rt_read`) lit le mot suivant. L'affichage des erreurs pendant l'exécution est devenu un module commun `RunError` (utilisé par Array et Data), avec la ligne en cours dans `rt_err_line`.
- **Validé par l'utilisateur le 14 nov. 2026 :** 568 tests ; `examples\donnees.bas` conforme dans WinAPE. (Un test de D058 utilisait READ comme exemple de mot pas encore disponible : remplacé par SOUND.)
- **Fichiers :** Ast.cs (DataStatement, ReadStatement, RestoreStatement, InputStatement.Indexes), Diagnostic.cs (3 messages), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (modules RunError et Data), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (LD D,(HL), LD E,(HL)), `tests\CpcBasic.Tests\DataTests.cs` (34 tests), `examples\donnees.bas`.

## D060 : lot D, nombres &, AND, OR, XOR, NOT, ^, comparaisons comme valeurs (Décidé, validé le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** 609 tests ; `examples\logique.bas` conforme dans WinAPE (le ^ s'affiche en flèche vers le haut : c'est la police du CPC).
- **Fait, comme le Locomotive BASIC :**
  - nombres `&C9`, `&HC9` (hexadécimal) et `&X1010` (binaire), de &0 à &FFFF, partout où un nombre est permis (calculs, FOR, DATA, commandes d'écran). De &8000 à &FFFF, ils valent le même mot de 16 bits qu'un entier négatif : `&FFFF` = -1, comme sur le CPC. Cela règle aussi les adresses au-dessus de 32767 dans un calcul : `P = &A400 : POKE P + 1, 9` fonctionne ;
  - `AND`, `OR`, `XOR` bit à bit sur 16 bits, et `NOT` (inverse tous les bits : NOT 0 = -1) ;
  - les comparaisons sont des valeurs : -1 si vraie, 0 sinon (`A = B > 3`, `PRINT 5 > 3`) ;
  - une valeur seule est une condition, vraie si elle n'est pas 0 : `IF A THEN`, `WHILE A`, `IF A AND 1 THEN` (avant, « IF A THEN » était refusé ; le test correspondant a été adapté) ;
  - `^` : puissance entière, de gauche à droite (`2^3^2` = 64), et avant le signe moins (`-2^2` = -4).
- **Ordre des opérations (du plus fort au plus faible) :** `^`, signe moins, `*` `/` `\` `MOD`, `+` `-`, comparaisons, `NOT`, `AND`, `OR`, `XOR`.
- **Rapidité des conditions :** des comparaisons reliées par AND, OR et NOT restent des sauts directs (NOT est retourné : NOT (A=1 OR A=3) devient A<>1 AND A<>3) ; une autre valeur est testée « différente de 0 ». `IF 1 AND 2 THEN` est faux, comme le Locomotive (1 AND 2 = 0).
- **Limites (jusqu'aux réels, lot F) :** `^` donne un entier sur 16 bits sans erreur s'il déborde (comme `*`) ; un exposant négatif donne 0 (sauf 1 et -1). Mots réservés ajoutés : XOR, NOT.
- **Fichiers :** Lexer.cs (HexNumber, Caret, HexValue), Ast.cs (BinaryOperator And/Or/Xor/Power, CompareExpression, NotExpression), Diagnostic.cs (2 messages), Parser.cs (niveaux d'expression, ToCondition), CodeGenerator.cs, RuntimeLibrary.cs (module Pow16), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (OR D, BIT 0,E), `tests\CpcBasic.Tests\LotDTests.cs` (41 tests), `tests\CpcBasic.Tests\ConditionsTests.cs` (1 test adapté), `examples\logique.bas`.

## D061 : @variable, adresse d'une variable (Décidé, validé le 14 nov. 2026)
- **Demande de l'utilisateur :** le caractère « @ » du Locomotive (adresse d'une variable), après le lot D.
- **Fait :** `@A`, `@A%` : adresse où la variable est rangée dans le programme compilé (elle ne change jamais, contrairement au BASIC où les variables peuvent bouger) ; `@T(3)` : adresse d'une case de tableau (la case est vérifiée, et le tableau créé s'il n'existe pas encore, comme pour une lecture). `@` s'utilise dans tout calcul : `PEEK(@A)`, `POKE @A + 1, 0`.
- **Taille :** une variable entière occupe 2 octets (octet bas, puis octet haut), comme A% dans le Locomotive ; une variable BYTE, 1 octet ; les cases d'un tableau se suivent de 2 octets, la dernière dimension changeant le plus vite (comme T%() dans le Locomotive). Différence avec le Locomotive : A (sans %) y est un nombre à virgule de 5 octets ; ici A est un entier de 2 octets (écart voulu, D026).
- **Pas encore :** `@a$` (chaînes, lot E) ; donner `@A` à une routine en langage machine (`CALL adresse, @A`, `|NOM, @A`) : paramètres de CALL et RSX, à venir.
- **Validé par l'utilisateur le 14 nov. 2026 :** 622 tests ; `examples\adresse.bas` conforme dans WinAPE (« @ » s'affiche « à » : jeu de caractères français du CPC, caractère 64). Un ancien test (LanguageTests) utilisait « @ » comme caractère inconnu : remplacé par « { ».
- **Fichiers :** Lexer.cs (At), Ast.cs (AddressOfExpression), Parser.cs, CodeGenerator.cs, `tests\CpcBasic.Tests\AddressTests.cs` (13 tests), `tests\CpcBasic.Tests\LanguageTests.cs` (1 test adapté), `examples\adresse.bas`.

## D062 : lot E, étape E1 : textes (Décidé, validé le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** 657 tests ; `examples\textes.bas` conforme dans WinAPE (prénom tapé en minuscules : « BONJOUR Philippe ! »).
- **Choix de l'utilisateur :** ranger les textes comme le Locomotive (« façon 1 ») plutôt qu'une case fixe de 256 octets par variable : moins de mémoire, et surtout `@a$` donnera la même fiche que le Locomotive (programme RSX de référence, `|GPRINT,@a$`). Découpage du lot E en trois étapes : E1 (variables, +, comparaisons, LEN, PRINT, INPUT), E2 (fonctions LEFT$, MID$, CHR$, VAL, STR$...), E3 (DATA texte, READ A$, INKEY$, LINE INPUT, tableaux de textes, MID$ = , @a$).
- **Fait (E1) :** variables `A$` ; textes entre guillemets dans les calculs (255 caractères au plus) ; `+` met deux textes bout à bout ; comparaisons de textes (=, <>, <, >, <=, >=, dans l'ordre des codes des caractères ; un texte plus court qui commence pareil est plus petit) dans IF et comme valeur ; `LEN` ; `PRINT A$` ; `INPUT A$` (entre guillemets, le texte peut contenir des virgules). `A$` et `A` sont deux variables différentes. Mot réservé ajouté : LEN.
- **Types :** un texte et un nombre ne se mélangent pas : `A$ = 1`, `A = "X"`, `"A" * 2`, `IF A$ THEN`, `FOR A$`… sont refusés à la compilation avec un message clair (le Locomotive donne « Type mismatch » pendant l'exécution).
- **Rangement (comme le Locomotive) :** chaque variable texte a une fiche de 3 octets (longueur, adresse des lettres). Un texte écrit dans le programme n'est pas recopié : la fiche pointe dessus. Les autres textes sont rangés en haut de la mémoire libre, en descendant ; les tableaux montent depuis la fin du programme. Chaque bloc de lettres est suivi de sa longueur et de l'adresse de sa fiche. Quand la place manque, un ménage garde les blocs encore pointés par leur fiche, les remonte, et oublie les autres. `A$ = B$` copie le texte (chaque variable garde le sien). Pendant une instruction, les textes intermédiaires (A$ + B$ + C$) ont des fiches temporaires (16 au plus), rendues au début de l'instruction suivante qui en crée.
- **Erreurs pendant l'exécution (textes du Locomotive) :** « String too long » (plus de 255 caractères), « String expression too complex » (plus de 16 textes intermédiaires dans une instruction), « Memory full ».
- **MEMORY :** fonctionne avant le premier texte rangé ; après, « Memory full » (déplacer les textes déjà rangés n'est pas encore fait). La gestion de la mémoire libre est devenue un module commun `Heap` (tableaux et textes).
- **Pas encore (E2, E3) :** fonctions de textes, DATA texte et READ A$, tableaux de textes, INKEY$, LINE INPUT, `@a$`. Un DIM qui manque de place ne lance pas le ménage des textes (« Memory full » directement).
- **Fichiers :** Ast.cs (VarType.String, StringLiteralExpression, ConcatExpression, Types), Diagnostic.cs (3 messages), Parser.cs (types dans les calculs, PRINT, LEN), CodeGenerator.cs, RuntimeLibrary.cs (modules Heap, Strings, InputString), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs`, `tests\CpcBasic.Tests\StringTests.cs` (35 tests), 4 anciens tests adaptés ($ n'est plus refusé partout : VariablesTests, ArrayTests, DataTests, AddressTests), `examples\textes.bas`.

## D063 : lot E, étape E2 : fonctions de textes (Décidé, validé le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** 685 tests ; `examples\textes2.bas` conforme dans WinAPE.
- **Fait, comme le Locomotive :** `LEFT$`, `RIGHT$`, `MID$` (fonction), `CHR$`, `ASC`, `STR$` (espace ou « - » devant, comme PRINT, sans espace après), `VAL` (décimal, `&` et `&H` hexadécimal, `&X` binaire ; s'arrête au premier caractère qui ne convient pas ; « » donne 0), `STRING$` (le caractère peut être un code ou un texte, dont on prend la première lettre), `SPACE$`, `UPPER$` et `LOWER$` (lettres de A à Z), `INSTR([début,] texte, cherché)` (0 si absent), `HEX$` et `BIN$` (nombre de chiffres facultatif, 0 à 16). Mots réservés ajoutés : ces 14 fonctions.
- **Erreur pendant l'exécution :** « Improper argument » (comme le Locomotive) pour une valeur hors limites : longueur ou code hors de 0 à 255, `MID$(A$, 0)`, `ASC("")`, `INSTR(0, ...)`, plus de 16 chiffres pour HEX$ et BIN$.
- **Vérifications à la compilation :** chaque fonction déclare le genre de chacune de ses valeurs (texte, nombre, ou l'un ou l'autre) ; un mauvais genre donne le message « un texte et un nombre ne se mélangent pas ».
- **Programme RSX de référence :** sa ligne `POKE p+a, VAL("&"+MID$(a$, a*2+1, 2))` fonctionne maintenant (test dédié).
- **Limites :** VAL donne un entier sur 16 bits (`VAL("40000")` = -25536, réels avec le lot F) ; `INSTR(texte, "")` donne le début s'il est dans le texte, sinon 0 (cas du Locomotive à comparer).
- **Fichiers :** Parser.cs (modèles de valeurs des fonctions), Ast.cs (Types : une fonction en $ donne un texte), CodeGenerator.cs, RuntimeLibrary.cs (module StrFunctions), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs`, `tests\CpcBasic.Tests\StringFunctionsTests.cs` (28 tests), `examples\textes2.bas`.

## D064 : lot E, étape E3a : textes dans DATA, READ A$, INKEY$, LINE INPUT, @A$ (Décidé, validé le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** `examples\textes3.bas` conforme dans WinAPE (DEL efface bien à l'écran ; INKEY$ après CLEAR INPUT : « TOUCHE : p  CODE 112 »). Rappel de l'utilisateur : la touche COPY du Locomotive (noté dans IDEES.md).
- **Découpage :** l'étape E3 est coupée en deux : E3a (ici) puis E3b (tableaux de textes et `MID$(A$, …) = `).
- **DATA, comme le Locomotive :** les éléments sont gardés tels qu'ils sont écrits (le lexique prend la suite de DATA jusqu'à « : » ou la fin de la ligne, en respectant les guillemets). Un élément est un nombre (décimal de -32768 à 65535, &hexadécimal, &Xbinaire, avec un signe possible, entre guillemets ou non) ou un texte (entre guillemets, avec ses virgules, ou tel qu'il est écrit, sans les espaces autour). Un élément vide vaut 0 (ou un texte vide) : `DATA 1,,2` et `DATA` seul sont maintenant acceptés (avant, c'était une erreur ; tests adaptés).
- **READ :** `READ A$` lit un texte, ou un nombre tel qu'il est écrit (`DATA 012` donne "012", `DATA &FF` donne "&FF") ; la fiche pointe directement dans la table des DATA (rien n'est copié). `READ A` sur un texte qui n'est pas un nombre : « Type mismatch », comme le Locomotive.
- **Table des DATA :** un nombre = 0, sa valeur (2 octets), puis la longueur et les lettres de son écriture ; un texte = 1, longueur, lettres. Module `DataString` (`rt_read_str`).
- **INKEY$ :** sans parenthèses, la touche tapée (sans attendre) ou "" (KM READ CHAR).
- **LINE INPUT [;] ["texte" ; ou ,] A$ :** toute la ligne tapée, virgules et guillemets compris (« ? » comme INPUT : corrigé en D093).
- **@A$ :** l'adresse de la fiche de 3 octets (longueur, adresse des lettres), comme le Locomotive : c'est ce qu'attend une routine RSX (`|GPRINT,@a$`).
- **Correction trouvée à l'essai (WinAPE) :** pendant INPUT et LINE INPUT, la touche DEL effaçait bien la lettre dans la mémoire mais affichait le caractère 127 (un damier) ; elle affiche maintenant « recule, espace, recule » comme l'éditeur du Locomotive (le test d'INPUT correspondant est adapté). Dans `examples\textes3.bas`, `CLEAR INPUT` vide le clavier avant d'attendre une touche avec INKEY$ (sinon une touche Entrée répétée peut rester en attente et INKEY$ la lit : « CODE 13 »).
- **Correction :** une variable qui reçoit un texte situé hors de la zone des textes (programme, DATA) pointe dessus, même s'il passe par une fiche temporaire.
- **Programme RSX de référence :** le chemin `READ a$` puis `POKE p+a, VAL("&"+MID$(a$, a*2+1, 2))` fonctionne (test dédié). Restent : nombres au-dessus de 32767 pour MEMORY p-1 avec p=&A400 (déjà possible avec &), `SAVE` (lot J), commandes RSX `|…` et `CALL` avec paramètres.
- **Fichiers :** Lexer.cs (DataItem), Ast.cs (DataItem, LineInputStatement), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (modules Data, DataString, Strings, StrFunctions, InputString), `tests\CpcBasic.Tests\StringDataTests.cs` (16 tests), DataTests et AddressTests adaptés, `examples\textes3.bas`.

## D065 : lot E, étape E3b : tableaux de textes et MID$(...) = (Décidé, validé le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** 721 tests ; `examples\tabtexte.bas` conforme dans WinAPE. **Le lot E est terminé** (sauf DEC$, BASIC 1.1).
- **Fait, comme le Locomotive :** tableaux de textes (`DIM NOM$(10)`, plusieurs dimensions, création automatique 0 à 10 sans DIM) ; lecture et écriture des cases dans tous les calculs ; `READ N$(I)`, `INPUT N$(I)`, `LINE INPUT N$(I)`, `@N$(I)` (adresse de la fiche de 3 octets). `MID$(A$, début [, longueur]) = texte` (variable ou case de tableau) : remplace des lettres sans changer la longueur ; « Improper argument » si le début est 0 ou après la fin.
- **Rangement :** une case d'un tableau de textes est une fiche de 3 octets (longueur, adresse des lettres), à 0 au départ (texte vide). Les routines des tableaux connaissent la taille d'une case (`rt_arr_esize` : 2 ou 3) ; si le programme a des tableaux de textes, chaque accès indique d'abord cette taille (5 octets), sinon rien ne change. Le ménage des textes suit les fiches des cases comme celles des variables.
- **MID$ = :** si le texte de A$ est encore celui écrit dans le programme (ou dans les DATA), il est d'abord recopié dans la zone des textes : le programme n'est jamais modifié, et une autre variable qui pointait sur le même texte n'est pas touchée.
- **Correction trouvée par les tests :** `INPUT N$(I)` essayait d'abord de lire un nombre ; la saisie d'un texte dans une case est maintenant traitée avant, comme `INPUT A$`.
- **Limite :** `MID$(A$, 2) = A$` (un texte qui se recopie sur lui-même en se décalant) peut donner un résultat différent du Locomotive.
- **Fichiers :** Ast.cs (MidAssignStatement, LineInputStatement.Indexes), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (Array : taille des cases ; StrFunctions : rt_midset), Z80Emitter.cs, `tests\CpcBasic.Tests\MiniZ80.cs` (LD L,B), `tests\CpcBasic.Tests\StringArrayTests.cs` (23 tests), ArrayTests et StringTests adaptés, `examples\tabtexte.bas`.
- **Le lot E (chaînes) est terminé** une fois cette étape validée (DEC$ du BASIC 1.1 reste à faire).

## D066 : lot F, nombres à virgule : choix de départ, et étape F1a (réglages de l'éditeur) (Décidé, F1a validée le 14 nov. 2026)
- **Validé par l'utilisateur le 14 nov. 2026 :** boutons « Machine (F7) » et « Sans signe (F8) » essayés dans l'éditeur : « C'est ok pour les boutons ».
- **Choix 1C de l'utilisateur :** une variable sans signe (`A`) reste un entier par défaut (D026, vitesse), mais un réglage **par programme** dans l'éditeur, « Sans signe : à virgule (F8) », la rend nombre à virgule comme le Locomotive (A% reste entier, A! est toujours à virgule). Le réglage est mémorisé dans `%APPDATA%\CpcBasicIde\reglages.json` pour chaque fichier .bas (chemin complet) ; rien n'est écrit dans le .bas.
- **Choix 2A :** les calculs à virgule utiliseront les routines de la ROM du CPC (celles du vrai BASIC : résultats identiques ; aucune ROM n'est livrée, on appelle celle de la machine). Leurs adresses diffèrent entre le 464 (bloc de saut des maths en &BD3D) et le 664 / 6128 (en &BD61). Nos propres routines ne serviront que plus tard, pour la cartouche GX4000 (qui n'a pas la ROM du CPC : chantier à part, qui concernera aussi l'affichage et le clavier).
- **Machine cible (demandée par l'utilisateur) :** réglage de l'éditeur « Machine (F7) », mémorisé pour tous les programmes : **Tous les CPC** (par défaut : le programme reconnaît la machine au démarrage, seulement s'il en a besoin, par KL PROBE ROM &B915 sur la ROM 0 : H = 0 pour un 464, 1 pour un 664, 2 pour un 6128, 4 pour un Plus), **CPC 464** ou **CPC 664/6128** (adresses écrites directement : un peu plus court, mais le programme ne marche que sur cette machine).
- **Format :** les nombres à virgule seront rangés sur 5 octets, au format du Locomotive (4 octets de mantisse, puis l'exposant), pour que `@A!` et PEEK donnent la même chose qu'en BASIC.
- **Découpage du lot F :** F1a (ici : réglages de l'éditeur et du compilateur), F1b (nombres à virgule : écriture 3.14, 1E5 ; A! ; DEFINT/DEFREAL/DEFSTR ; + - * / ^ et comparaisons ; passage entier ↔ virgule ; PRINT et INPUT), F2 (SIN, COS, TAN, ATN, SQR, EXP, LOG, LOG10, PI, DEG, RAD, INT, FIX, CINT, CREAL, ROUND, RND ; ABS, SGN, MIN, MAX à virgule), F3 (tableaux à virgule, READ, VAL, STR$, FOR avec pas à virgule). Un programme sans nombre à virgule ne grossit pas et ne ralentit pas.
- **Fait (F1a) :** `CompilerOptions` (machine, variables sans signe) transmis de l'éditeur à l'analyse et au générateur ; deux boutons dans l'éditeur, « Machine : … (F7) » et « Sans signe : entiers / à virgule (F8) », avec un message d'explication à chaque changement, l'état dans la barre d'état et dans le message de compilation réussie. Les réglages n'ont pas encore d'effet sur le programme compilé (ils serviront dès F1b).
- **Fichiers :** `src\CpcBasic.Core\CompilerOptions.cs` (nouveau), Compiler.cs, BasicAnalyzer.cs, CodeGenerator.cs, `src\CpcIde\EditorSettings.cs`, `src\CpcIde\MainWindow.axaml`, `src\CpcIde\MainWindow.axaml.cs`, `tests\CpcBasic.Tests\OptionsTests.cs` (5 tests).

## D067 : forme finale du projet (Décidé, 15 nov. 2026)
- **Choix de l'utilisateur :** viser d'abord **Windows**, avec un **programme d'installation** (raccourci, désinstallation) au lieu de l'archive .zip actuelle, et un programme **signé** pour qu'un antivirus ne le bloque pas (Avast signale aujourd'hui l'exécutable non signé).
- **Quand :** une fois que tout fonctionnera, et après que l'utilisateur aura indiqué les choses à ajouter avant de créer le fichier d'installation (il le dira à ce moment-là).
- **Ensuite, peut-être :** Linux (l'éditeur utilise Avalonia, prévu pour Windows, Linux et macOS ; seuls les scripts de développement sont propres à Windows).
- **À savoir pour la signature :** il faut un certificat de signature de code délivré par une autorité (en général payant, avec vérification d'identité) ; des offres gratuites existent pour les logiciels libres. À étudier au moment venu, avec les tarifs et conditions du moment.

## D068 : lot F1b, nombres à virgule : fondations, F1b1 et F1b2 (Validé le 15 nov. 2026 : lot F1b terminé)
- **Format :** les nombres à virgule sont ceux du Locomotive, sur 5 octets (4 octets de mantisse, dont le bit de signe, puis l'exposant + 128 ; 0 = zéro). Le compilateur sait les écrire (`LocoReal`, dans `src\CpcBasic.Core\Codegen\LocoReal.cs`), par exemple pour les nombres écrits dans le programme (3.14).
- **Calculs :** faits par les routines de la ROM du CPC (addition, multiplication, division, comparaison, SIN, SQR...), comme le vrai BASIC, donc avec exactement les mêmes résultats. Leurs adresses ne sont pas les mêmes sur le 464 et sur le 664/6128 (par exemple &BD88 est la division sur le 6128 mais SIN sur le 464).
- **Table de sauts :** le programme appelle ces routines à travers une petite table en mémoire (`rt_fm_...`), remplie au départ par `rt_fm_init` :
  - réglage « Tous les CPC » : la table du 664/6128 est en place ; au départ, KL PROBE ROM (&B915) dit quelle machine tourne et, si c'est un 464, la table du 464 est recopiée par-dessus ;
  - réglage « CPC 464 » ou « CPC 664/6128 » (F7) : la bonne table directement, sans test.
  Le départ met aussi les angles en radians (comme le BASIC à l'allumage).
- **Tests :** le simulateur Z80 des tests imite ces routines (aux adresses du 464 ou du 664/6128 selon la machine choisie) et KL PROBE ROM ; nouveaux essais dans `tests\CpcBasic.Tests\RealTests.cs` (23 tests). Fondations vérifiées le 15 nov. 2026 : 749 tests réussis.
- **Ce qu'affiche le vrai BASIC (capture de l'utilisateur, SugarBox réglé comme WinAPE, 15 nov. 2026) :**
  - 1/3 ; 2/3 ; 10/3 ; 100/3 → ` 0.333333333  0.666666667  3.33333333  33.3333333` : 9 chiffres significatifs, le dernier arrondi, les zéros de fin enlevés ; un espace devant un nombre positif, un espace après chaque nombre (comme les entiers) ;
  - 1000000/3 ; 1E8/3 ; 1/7 ; 0.5 → ` 333333.333  33333333.3  0.142857143  0.5` ;
  - 1E9 ; 123456789 ; 1234567890 → ` 1E+09  123456789  1.23457E+09` : écriture avec exposant au-delà de 9 chiffres, et alors 6 chiffres significatifs au plus ;
  - 0.1 ; 0.01 ; 0.001 ; 0.0001 → ` 0.1  0.01  0.001  0.0001` ;
  - -1.5 ; 1E-10 ; 1E20 ; -2.5E-5 → `-1.5  1E-10  1E+20 -0.000025` ;
  - PI écrit en entier ; 2^0.5 → ` 3.14159265  1.41421356` ;
  - STR$(1/3) et STR$(-0.5) → `[ 0.333333333]` et `[-0.5]` : espace devant si positif, pas d'espace après ;
  - A%=2.5 : B%=-2.5 : C%=3.5 → ` 3 -3  4` : arrondi au plus près, le « ,5 » s'éloignant de zéro ;
  - PRINT 1E38*10 → `Overflow` sur une ligne, puis ` 1.70141E+38`, et le programme **continue** : pour les nombres à virgule, Overflow n'est pas une erreur qui arrête le programme, c'est un avertissement, et le résultat devient le plus grand nombre possible (avec son signe).
  - Deuxième capture (contrôle) : 1E-5 … 1E-9 → ` 0.00001  0.000001  0.0000001  0.00000001  0.000000001` ; 1.5E-6 ; 1.234E-7 ; 0.000123456789 → ` 0.0000015  1.234E-07  1.23457E-04` ; 999999999 ; 999999999.6 ; 123456789.4 ; 12345678.95 → ` 999999999  1E+09  123456789  12345679` ; 1E10/3 ; -1E15/7 ; 2^40 → ` 3.33333E+09 -1.42857E+14  1.09951E+12` ; 100000 ; 1E5 ; 1000000 ; 1E6 → tous écrits sans exposant ; -0 ; 0 ; -0.000001 → ` 0  0 -0.000001` ; PRINT 1/0;"SUITE" → `Division by zero` sur une ligne, puis ` 1.70141E+38 SUITE`, et le programme continue jusqu'à FIN.
- **Règle d'affichage retenue (déduite des deux captures) :**
  1. arrondir le nombre à 9 chiffres significatifs ;
  2. l'écrire sans exposant si cela tient en 9 chiffres au plus, sans compter le « 0 » de « 0, » : au plus 9 chiffres avant la virgule pour un nombre ≥ 1 (999999999 oui, 1E+09 non), au plus 9 chiffres après la virgule pour un nombre < 1 (0.000000001 oui, 1E-10 non ; 0.0000001234 non) ; les zéros de fin sont enlevés, puis la virgule si plus rien ne la suit ;
  3. sinon, écriture avec exposant : 6 chiffres significatifs (arrondis, zéros de fin enlevés), puis `E+` ou `E-` et l'exposant sur 2 chiffres (1E+09, 1.234E-07) ;
  4. un signe « - » ou un espace devant, comme pour les entiers ; -0 s'écrit 0.
- **Overflow et Division by zero** avec des nombres à virgule : le message s'affiche sur sa propre ligne, le résultat devient ±1.70141E+38, et le programme continue (ce ne sont pas des erreurs qui l'arrêtent).
- **Étape F1b1 (validée le 15 nov. 2026 : tests réussis ; `virgule.bas` compilé donne dans WinAPE exactement le même écran que le même programme tapé dans le vrai BASIC de SugarBox) :**
  - nombres écrits avec une virgule ou un exposant (3.14, .5, 1E5, 2.5E-3) ; un nombre entier trop grand pour un entier (40000, -40000) devient un nombre à virgule, comme dans le Locomotive ; au-delà de 1.7E+38 : erreur à la compilation ;
  - variables A! (pas encore en tableau, FOR, INPUT, READ ni avec @ : F1b2 et F3) ;
  - + - * / ^ et le signe moins : dès qu'un côté est à virgule, le calcul se fait à virgule (par la ROM) ; entre deux entiers, il reste entier (7/2 = 3, choix D066) ;
  - comparaisons (= <> < > <= >=) entre nombres à virgule, ou entre un entier et un nombre à virgule ;
  - nombre à virgule là où il faut un entier (A% = B!, BOX X!, ...) : arrondi au plus près, « ,5 » s'éloignant de zéro ; s'il ne tient pas de -32768 à 32767 : « Overflow in N » et le programme s'arrête (écrit dans le programme, A% = 40000.4 : erreur dès la compilation) ;
  - PRINT d'un nombre à virgule selon la règle ci-dessus ; Overflow et Division by zero : message sur sa ligne, ±1.70141E+38, et le programme continue ;
  - fichiers : `src\CpcBasic.Core\Codegen\RealRuntime.cs` (nouveau : modules RealOps et RealPrint), Lexer.cs, Parser.cs, Ast.cs, CodeGenerator.cs, Z80Emitter.cs, RuntimeLibrary.cs, `tests\CpcBasic.Tests\MiniZ80.cs`, `tests\CpcBasic.Tests\RealProgramTests.cs` (nouveau), VariablesTests.cs (PRINT 40000 et PRINT B! sont maintenant permis) ; exemple `examples\virgule.bas`.
- **Étape F1b2 (validée le 15 nov. 2026 : tests réussis ; `types.bas` donne les mêmes écrans dans WinAPE et SugarBox. Avec une virgule tapée à la française (100,2), le CPC prend 100 puis affiche « ?Extra ignored » : c'est bien le comportement du vrai BASIC, où la virgule sépare deux valeurs et le point sert pour les décimales) :**
  - **DEFINT, DEFREAL, DEFSTR** lettre ou plage (A-Z), séparées par des virgules. Ils valent pour **tout le programme**, où qu'ils soient écrits (pour une même lettre, le dernier l'emporte). Une variable sans signe prend alors le signe de sa lettre : après DEFINT A-Z, A et A% sont la même variable, comme dans le Locomotive. Sans aucun DEF, rien ne change (A reste un entier à part, D026). Une variable BYTE garde son type. Liste de lettres fausse : erreur à la compilation.
  - **Réglage F8 « Sans signe : à virgule »** : comme un DEFREAL A-Z au début (les DEF du programme passent avant) ; « / » divise alors à virgule même entre deux entiers (7/2 = 3.5), comme le Locomotive.
  - **FOR X!** : départ calculé, limite et pas écrits dans le programme (entiers ou à virgule) ; à chaque NEXT, X! + pas puis comparaison avec la limite par la ROM.
  - **INPUT A!** : nombre tapé comme dans un programme (1.5, -2.5E-3, .5) ; sinon « ?Redo from start ».
  - **@A!** : adresse des 5 octets du nombre.
  - Pas encore : tableaux de nombres à virgule, READ A!, fonctions (F2, F3). Avec le réglage F8, un tableau sans signe (DIM T(10)) est donc encore refusé jusqu'à F3.
  - Fichiers : Parser.cs, Ast.cs, Diagnostic.cs (nouveau message ExpectedLetterRange), BasicAnalyzer.cs, CodeGenerator.cs, RealRuntime.cs (module InputReal), RuntimeLibrary.cs, Z80Emitter.cs, MiniZ80.cs, `tests\CpcBasic.Tests\RealTypesTests.cs` (nouveau) ; exemple `examples\types.bas`.

## D069 : lot F2, fonctions mathématiques (Validé le 15 nov. 2026 : tests réussis ; `fonction.bas` compilé donne dans WinAPE les mêmes chiffres que le vrai BASIC, après la correction de INT, FIX et ROUND)
- **Fonctions :** SIN, COS, TAN, ATN, SQR, EXP, LOG, LOG10, PI (sans parenthèses), INT, FIX, CINT, CREAL, ROUND(x [, décimales]), RND [(x)] ; ABS, SGN, MIN et MAX acceptent maintenant les nombres à virgule. Commandes DEG et RAD (unité des angles ; radians au départ).
- **Calcul :** par les routines de la ROM (comme le vrai BASIC), sauf ROUND (x × 10^n, + 0,5 sur la valeur sans signe, partie entière, signe remis, ÷ 10^n) et RND.
- **Piège trouvé au premier essai sur WinAPE :** INT(-2.7) et FIX(-2.7) affichaient 2, ROUND(3.14159,2) affichait 2.56000037. Les routines REAL INT et REAL FIX de la ROM ne rendent pas un nombre à virgule : elles rangent en place un **entier sans signe de 4 octets** (le 5e octet n'est pas touché) et le signe dans B (le calcul 256 + 314/2^32 × 512 / 100 = 2.56000037 le confirme). Correction : partie entière du nombre sans signe par REAL FIX, puis BINARY TO REAL (&BD67 / 464 : &BD43), puis signe remis ; pour INT d'un négatif qui n'est pas entier, 1 de plus. Le simulateur des tests imite maintenant ce comportement réel.
- **Types :** SIN, COS, TAN, ATN, SQR, EXP, LOG, LOG10, PI, RND et CREAL donnent toujours un nombre à virgule ; INT, FIX, ABS et ROUND(x) gardent un entier tel quel (INT(5) = 5) ; ROUND(x, n) est toujours à virgule (ROUND(1234, -2) = 1200) ; CINT et SGN donnent un entier ; MIN et MAX sont à virgule si une valeur l'est.
- **Ce qu'a montré le vrai BASIC (capture SugarBox, 15 nov. 2026) :** ` 0.841470985  0.540302306  1.55740772  0.785398163` ; en DEG ` 0.5  0.5  45` ; ` 1.41421356  2.71828183  2.30258509  3  3.14159265` ; INT/FIX/CINT ` 2 -3  2 -2  3 -3` ; ROUND ` 3.14  1200  3 -3` ; ` 2.5 -1  1.5  2` ; ` 5  1200  2.5` ; EXP(100) : `Overflow` puis ` 1.70141E+38` et le programme continue ; SQR(-1) : `Improper argument in 110` et le programme s'arrête.
- **Erreurs :** SQR d'un nombre négatif, LOG et LOG10 d'un nombre négatif ou nul : « Improper argument in N », arrêt. Résultat trop grand (EXP(100)) : « Overflow », ±1.70141E+38, et on continue. CINT trop grand : « Overflow in N », arrêt.
- **RND :** de 0 (compris) à 1 (non compris), fait de deux tirages du générateur entier (D050) : RANDOMIZE agit aussi sur RND. RND(0) redonne le dernier nombre, RND(négatif) repart d'une graine tirée de ce nombre. La suite n'est pas celle du vrai CPC (ce sont des nombres au hasard).
- **Fichiers :** Parser.cs (fonctions, PI et RND sans parenthèses, DEG et RAD), Ast.cs (types des fonctions), CodeGenerator.cs, RealRuntime.cs (ROUND, ABS, contrôles, module RealRnd), RuntimeLibrary.cs, Z80Emitter.cs, MiniZ80.cs, `tests\CpcBasic.Tests\RealFunctionsTests.cs` (nouveau), ArrayTests.cs (SIN existe maintenant : l'essai utilise FRE) ; exemple `examples\fonction.bas`.

## D070 : lot F3, nombres à virgule partout (Validé le 15 nov. 2026 : F3a et F3b, `tabreel.bas` et `valstr.bas` identiques dans WinAPE et dans le vrai BASIC de SugarBox ; lot F terminé)
- **Découpage :** F3a = tableaux de nombres à virgule, nombres à virgule dans DATA, READ A! ; F3b = VAL et STR$ avec les nombres à virgule.
- **Tableaux (F3a) :** DIM T!(10), T!(I) = ..., INPUT T!(I), READ T!(I), @T!(I) ; plusieurs dimensions ; sans DIM : cases 0 à 10, comme les autres tableaux. Une case fait 5 octets (le module Array sait maintenant multiplier par 2, 3 ou 5). Avec le réglage F8, DIM T(10) est un tableau à virgule.
- **DATA (F3a) :** 3.14, .5, -1E3, ou un entier trop grand (70000, -32769) sont rangés comme nombres à virgule : élément « 2 », ses 5 octets, puis son écriture (pour READ A$). Plus de 1.7E+38 : erreur à la compilation. Les entiers de -32768 à 65535 restent comme avant (D059).
- **READ (F3a) :** READ A! lit un nombre à virgule, ou un entier converti ; READ A (entier) sur un nombre à virgule l'arrondit (« Overflow in N » s'il ne tient pas) ; READ A$ rend l'écriture du nombre ; READ A! sur un texte : « Type mismatch ».
- **Fichiers (F3a) :** Ast.cs (tous les types de variables existent, DataItem.IsReal), Parser.cs (DATA), CodeGenerator.cs, RuntimeLibrary.cs (Array : cases de 5 octets ; Data, DataString : élément 2 ; modules DataReal / DataNoReal), RealRuntime.cs, `tests\CpcBasic.Tests\RealArraysTests.cs` (nouveau), ArrayTests.cs, VariablesTests.cs, DataTests.cs (tableaux à virgule et grands nombres dans DATA maintenant permis) ; exemple `examples\tabreel.bas`.
- **F3b (validée le 15 nov. 2026) : VAL et STR$.** Capture du vrai BASIC (SugarBox) : ` 3.14 -2500  0.5  100` ; VAL("12ABC"), VAL("ABC"), VAL(""), VAL("1.2.3") → ` 12  0  0  1.2` ; VAL("&FF"), VAL("&X101"), VAL("1E"), VAL("- 5") → ` 255  5  1 -5` ; VAL("40000") → ` 40000` ; VAL("1E39") → « Overflow » écrit **à la suite** de la ligne (` 40000 Overflow`), puis le vrai BASIC s'arrête sur « Type mismatch in 50 ».
  - VAL donne maintenant toujours un nombre à virgule (lecture souple : on s'arrête au premier caractère qui ne va pas ; espaces permis après le signe ; « &FF » et « &X101 » par l'ancien VAL entier). Dans un entier, il est arrondi (A% = VAL("3.7") → 4). Conséquence : un programme qui utilise VAL emporte les modules des nombres à virgule.
  - Différence connue : VAL("1E39") donne ici « Overflow » puis 1.70141E+38 et le programme continue ; le vrai BASIC s'arrête ensuite sur « Type mismatch ».
  - **Messages Overflow et Division by zero :** écrits là où est le curseur, puis passage à la ligne (le vrai BASIC ne va pas d'abord à la ligne) ; changé pour tous ces messages.
  - STR$ d'un nombre à virgule : l'écriture de PRINT sans l'espace de fin (STR$(3.14) = " 3.14", STR$(-0.5) = "-0.5"). L'affichage passe par un aiguillage (rt_fp_out) : l'écran pour PRINT, un texte pour STR$.
  - La lecture des nombres (INPUT A!) et VAL partagent le même module RealParse ; INPUT A! accepte aussi « - 5 ».
  - Fichiers : RealRuntime.cs (RealParse, RealVal, RealStr, message sans retour à la ligne avant), Ast.cs, CodeGenerator.cs, RuntimeLibrary.cs, MiniZ80.cs (JP &BB5A), `tests\CpcBasic.Tests\RealStrValTests.cs` (nouveau) ; exemple `examples\valstr.bas`.

## D071 : lot G1, le son (Validé le 15 nov. 2026 : 909 tests réussis ; `musique.bas` compilé sonne comme le même programme dans le vrai BASIC, dans plusieurs émulateurs ; écran « FINI. SQ(1) = 4 »)
- **Découpage :** G1 = SOUND, ENV, ENT, RELEASE, SQ. ON SQ GOSUB est reporté au lot I (interruptions : même mécanisme qu'EVERY, AFTER, ON BREAK) ; il reste refusé pour l'instant.
- **Par la ROM, comme le vrai BASIC :** SOUND QUEUE (&BCAA, bloc de 9 octets : canal, enveloppe de volume, enveloppe de ton, période, bruit, volume, durée), SOUND CHECK (&BCAD, SQ), SOUND RELEASE (&BCB3), SOUND AMPL ENVELOPE (&BCBC, ENV), SOUND TONE ENVELOPE (&BCBF, ENT). Si la file du canal est pleine, SOUND attend qu'une place se libère.
- **SOUND canal, période [, durée [, volume [, env. volume [, env. ton [, bruit]]]]]** : valeurs vides permises même au milieu (SOUND 1,200,,5) ; par défaut durée 20 (centièmes de seconde), volume 12, enveloppes 0, bruit 0. Valeurs écrites vérifiées à la compilation : canal 1 à 255, période 0 à 4095, volume 0 à 15, enveloppes 0 à 15, bruit 0 à 31.
- **ENV n, parties / ENT n, parties** : 5 parties au plus, de 3 valeurs (pas, taille, pause) ; ENV n de 1 à 15, ENT n de -15 à 15 sauf 0 (négatif : l'enveloppe se répète, bit 7 du nombre de parties). Parties avec « = » : ENV =forme, période (octet &80 + forme, puis la période sur 2 octets) ; ENT =période, pause (octet &F0 + haut de la période, bas, pause). Chaque ENV/ENT a son bloc de 16 octets dans le programme.
- **RELEASE canaux** (0 à 7) ; **SQ(canal)** (1, 2 ou 4) : l'octet d'état de la ROM (places libres, rendez-vous, attente, canal actif en bit 7).
- **Vérification :** le simulateur des tests note les blocs envoyés à la ROM ; la vraie vérification est à l'oreille : `examples\musique.bas` (Frère Jacques à deux voix, avec une enveloppe de volume et un vibrato), compilé dans WinAPE, comparé au même programme tapé dans SugarBox.
- **Fichiers :** Ast.cs (EnvelopeStatement), Parser.cs, CodeGenerator.cs, MiniZ80.cs, `tests\CpcBasic.Tests\SoundTests.cs` (nouveau) ; exemple `examples\musique.bas`.

## D072 : lot H1, fenêtres et numéros de fenêtre (Validé le 15 nov. 2026 : 943 tests réussis ; `fenetres.bas` compilé identique au même programme dans le vrai BASIC de SugarBox ; POS et VPOS au bord droit : 1 et 2 comme le vrai BASIC)
- **Découpage du lot H :** H1 fenêtres et #n ; H2 mise en page de PRINT (virgule, ZONE, TAB, SPC, WRITE) ; H3 PRINT USING ; H4 SYMBOL, SYMBOL AFTER, TAG, TAGOFF ; H5 édition complète de la ligne d'INPUT (flèches, CLR, COPY). Les flux #8 (imprimante) et #9 (disquette, cassette) vont au lot J.
- **Par le firmware, comme le vrai BASIC :** TXT STR SELECT (&BBB4) choisit la fenêtre, TXT WIN ENABLE (&BB66) fixe ses bords, TXT SWAP STREAMS (&BBB7) échange deux fenêtres, TXT GET CURSOR (&BB78) et TXT VALIDATE (&BB87) donnent POS et VPOS.
- **#n sur PRINT, LOCATE, CLS, PEN, PAPER, WINDOW, INPUT, LINE INPUT :** la commande est entourée du choix de la fenêtre n puis du retour à la fenêtre #0 (le Locomotive revient à #0 après chaque commande). Sans « # », le code produit ne change pas. Virgule obligatoire après le numéro (sauf PRINT #1 et CLS #1 seuls). Numéro écrit : 0 à 7, vérifié à la compilation ; numéro calculé : vérifié pendant l'exécution (« Improper argument in N »).
- **WINDOW [#n,] gauche, droite, haut, bas** (à partir de 1 ; le firmware remet dans l'ordre et ramène dans l'écran) ; **WINDOW SWAP a [, b]** (b vaut 0 s'il est omis).
- **POS(#n), VPOS(#n)** : « # » obligatoire comme le Locomotive (message clair sinon) ; position où s'écrira le caractère suivant ; la fenêtre de la commande en cours est gardée (PRINT #2, POS(#1) écrit dans #2). Au bord droit (40 caractères écrits en MODE 1), le vrai BASIC donne POS 1 et VPOS 2 : c'est ce que donne TXT VALIDATE.
- **Erreurs pendant l'exécution :** le message s'affiche maintenant toujours dans la fenêtre #0.
- **Remarque :** `MODE` remet les fenêtres en plein écran mais garde leurs couleurs ; après `WINDOW SWAP 0,2`, un second `RUN` dans le vrai BASIC commence donc avec les couleurs de l'ancienne fenêtre 2 (constaté dans SugarBox). Un programme compilé démarre toujours sur une machine neuve.
- **Mots réservés :** WINDOW, SWAP, POS, VPOS ne peuvent plus servir de noms de variables (ils étaient déjà des mots du Locomotive).
- **Fichiers :** Lexer.cs (jeton #), Ast.cs (StreamStatement), Diagnostic.cs (deux messages), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (module Stream), MiniZ80.cs, `tests\CpcBasic.Tests\WindowTests.cs` (nouveau) ; exemple `examples\fenetres.bas`.

## D073 : lot H2, mise en page de PRINT (Validé le 15 nov. 2026 : 977 tests réussis ; `colonnes.bas`, `essais\fin.bas` et `essais\locate.bas` compilés identiques au vrai BASIC)
- **Référence :** le programme de `examples\colonnes.bas`, tapé dans le vrai BASIC (SugarBox, 15 nov. 2026), en MODE 1.
- **Virgule :** va au début de la zone suivante (colonnes 1, 14, 27… avec ZONE 13), toujours après la colonne actuelle (PRINT ,"A" écrit colonne 14). Si la zone ne tient pas entière dans la fenêtre (colonne 40 + 13 > 40), passage à la ligne. Une virgule à la fin garde la ligne.
- **ZONE n** (1 à 255, 13 au départ) ; valeur calculée hors limites : « Improper argument ».
- **TAB(n)** : espaces jusqu'à la colonne n ; colonne déjà dépassée : passage à la ligne puis colonne n ; n plus grand que la largeur : ramené dedans ; n inférieur à 1 : 1. **SPC(n)** : n espaces (négatif : aucun ; plus grand que la largeur : reste de la division). TAB et SPC peuvent être suivis directement d'un élément (PRINT TAB(10)"X").
- **WRITE [#n,] valeurs** : textes entre guillemets, nombres sans espace (STR$ sans son espace du début), séparés par « , », puis passage à la ligne ; « ; » accepté entre les valeurs.
- **Largeur et colonne :** celles de la fenêtre choisie (TXT GET WINDOW &BB69, TXT GET CURSOR &BB78), donc tout marche aussi avec PRINT #n.
- **Module :** PrintFormat (rt_zone, rt_tab, rt_spc, rt_zone_set), lié seulement s'il sert. Les PRINT sans virgule, TAB ni SPC ne changent pas.
- **Mots réservés :** ZONE, TAB, SPC, WRITE (mots du Locomotive).
- **Élément qui ne tient pas dans la fin de la ligne :** il passe entier à la ligne suivante, sauf en colonne 1 (constaté dans SugarBox avec `PRINT "X";TAB(38);"ABCD"` ; l'utilisateur l'avait déjà remarqué au début du projet, mais ce n'était pas écrit ici). Vérification faite avant chaque texte de 2 caractères ou plus, chaque texte calculé, chaque entier (signe + chiffres) et chaque nombre à virgule (caractères comptés avant l'affichage). La colonne est celle de TXT VALIDATE (&BB87) : après une ligne remplie, c'est 1, donc pas de ligne vide (5 × « BONJOUR  » puis le 6e en colonne 1). Module PrintFit (rt_pf_fit), lié dès qu'un PRINT écrit un tel élément.
- **Taille :** un programme qui écrit un texte de 2 caractères ou plus, ou un nombre, contient le module PrintFit (127 octets) : PRINT "HELLO" passe de 29 à 161 octets.
- **Fichiers :** Ast.cs (PrintZoneStatement, PrintTabStatement, NoLeadingSpace), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (PrintFormat, PrintFit), RealRuntime.cs (rt_fprint_fit), MiniZ80.cs (TXT GET WINDOW, TXT VALIDATE), `tests\CpcBasic.Tests\PrintFormatTests.cs` (nouveau) ; exemple `examples\colonnes.bas`.

## D074 : lot H3, PRINT USING (Validé le 15 nov. 2026 : 1005 tests réussis ; `format.bas` compilé identique au vrai BASIC de SugarBox)
- **Référence :** `examples\format.bas`, tapé dans le vrai BASIC (SugarBox, 15 nov. 2026). Dans SugarBox, la touche « \ » d'un clavier français n'arrive pas telle quelle : passer par CHR$(92).
- **Écriture :** PRINT [#n,] [éléments ;] USING "format"; valeur {; ou , valeur} [; ou ,]. USING prend toute la suite du PRINT.
- **Format découpé à la compilation :** texte autour des emplacements écrit tel quel ; chaque valeur prend l'emplacement suivant ; à la fin du format on reprend au début ; après la dernière valeur, le texte qui suit est écrit jusqu'à l'emplacement suivant (« PRIX ###.## EUR »). Format sans emplacement : erreur (le vrai BASIC : Improper argument). Texte dans un emplacement de nombre (ou l'inverse) : « Type mismatch ».
- **Limite pour l'instant :** le format doit être écrit entre guillemets ; un format rangé dans une variable (PRINT USING F$; X) est refusé avec un message clair. À ajouter si besoin (découpage pendant l'exécution).
- **Textes :** « ! » première lettre ; « \   \ » 2 + nombre d'espaces lettres (coupé ou complété d'espaces) ; « & » tout le texte.
- **Nombres :** [+] [** [$] | $$] # et , [. #] [^^^^] [+ ou -]. Le nombre est découpé en 9 chiffres (même routine que PRINT, `rt_fp_conv`), arrondi au dernier chiffre écrit (,5 vers le haut), puis écrit : signe devant (« + » demandé, ou « - » s'il n'y a pas de signe derrière), « $ », chiffres groupés par 3 si « , », point, décimales, exposant E+nn, signe derrière (« - » ou espace ; « + » ou « - »). Partie entière nulle : « 0 » ajouté s'il reste de la place (0.50). Plus court que l'emplacement : espaces à gauche (étoiles avec « ** ») ; plus long : « % » suivi du nombre (« %123456 », « %1,234,567 »). Avec exposant : une place de moins avant le point quand aucun signe n'est demandé (« #.##^^^^ » : « .12E+05 »).
- **Modules :** PrintUsing (nombres, avec les calculs par la ROM) et PrintUsingStr (textes) ; vérification de fin de ligne (D073) avant chaque valeur.
- **Mot réservé :** USING (mot du Locomotive).
- **Fichiers :** Ast.cs (PrintUsingNumberStatement, PrintUsingStringStatement, UsingFlags), Diagnostic.cs (deux messages), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs, RealRuntime.cs (rt_fp_conv), `tests\CpcBasic.Tests\UsingTests.cs` (nouveau) ; exemple `examples\format.bas`.

## D075 : lot H4, SYMBOL, SYMBOL AFTER, TAG, TAGOFF (Validé le 15 nov. 2026 : 1023 tests réussis ; `symboles.bas` compilé identique au vrai BASIC de SugarBox)
- **Référence :** `examples\symboles.bas`, tapé dans le vrai BASIC (SugarBox, 15 nov. 2026).
- **SYMBOL caractère, ligne1 [, … ligne8]** : lignes absentes à 0 (SYMBOL 242,&FF : un trait en haut). Par TXT SET MATRIX (&BBA8) ; caractère pas redessinable : « Improper argument in N », comme le vrai BASIC (SYMBOL 65,255 après SYMBOL AFTER 240).
- **Caractères 240 à 255 redessinables dès le départ**, comme dans le vrai BASIC : le programme compilé est lancé depuis le BASIC, dont la table (SYMBOL AFTER 240) reste en place au-dessus de &A67B.
- **SYMBOL AFTER n** (0 à 256) : les dessins actuels des caractères n à 255 sont copiés (TXT GET MATRIX &BBA5, lus avec la ROM basse ouverte, KL L ROM ENABLE &B906) dans une table placée à la fin du programme, puis donnée au firmware (TXT SET M TABLE &BBAB). Taille de la table : (256 - plus petit n écrit) × 8 octets ; n calculé : 2 Ko. SYMBOL AFTER 256 : plus de table.
- **TAG [#n] / TAGOFF [#n]** : TXT SET GRAPHIC (&BB63). Le texte s'écrit au curseur graphique, dans la couleur du tracé ; **le curseur texte (LOCATE) ne bouge pas** (remarque de l'utilisateur, vérifiée : FIN s'écrit sous AVANT). Un PRINT sans « ; » dessine les retours à la ligne comme deux petits symboles (← ↓), comme le vrai BASIC.
- **Non vérifié :** sous TAG, la vérification de fin de ligne (D073), la virgule et TAB se servent encore de la colonne du curseur texte.
- **Mots réservés :** SYMBOL, TAG, TAGOFF (mots du Locomotive).
- **Fichiers :** Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (modules Symbol, SymbolAfter), MiniZ80.cs, `tests\CpcBasic.Tests\SymbolTests.cs` (nouveau) ; exemple `examples\symboles.bas`.

## D076 : lot H5, édition de la ligne tapée (INPUT, LINE INPUT) (Validé le 15 nov. 2026 : 1040 tests réussis ; `edition.bas` dans WinAPE : BONJXOUR, BONJUR, BONOUR et COPY comme le vrai BASIC ; lot H terminé)
- **Demande de l'utilisateur (14 nov. 2026, IDEES.md) :** retrouver l'éditeur de saisie du vrai BASIC, avec la touche COPY.
- **Référence (SugarBox, 15 nov. 2026) :** BONJOUR, 3 × flèche gauche, X -> BONJXOUR (la lettre s'insère) ; CLR -> BONJUR (efface sous le curseur) ; DEL -> BONOUR (efface à gauche) ; SHIFT + flèches puis COPY recopie ce qui est affiché (« Ready »). Touches du PC : COPY = Alt gauche (WinAPE et SugarBox), CLR = Suppr (SugarBox).
- **Touches :** caractères (32 à 126) insérés ; flèches gauche / droite ; haut / bas : une largeur de fenêtre en arrière / en avant dans la ligne ; CTRL + gauche / droite : début / fin ; DEL, CLR ; SHIFT + flèches : curseur de copie (il part de la position du curseur, reste dans la fenêtre) ; COPY : lit le caractère sous lui (TXT RD CHAR &BB60), l'avance d'une place (au bord droit : début de la ligne suivante) et l'insère. Entrée : fin, le curseur est remis après le dernier caractère. 255 caractères au plus (80 avant).
- **Affichage :** les deux curseurs sont montrés pendant l'attente d'une touche (TXT PLACE CURSOR &BB8A / TXT REMOVE CURSOR &BB8D). Après une insertion ou un effacement, la fin de la ligne est réécrite (avec un espace en plus après un effacement). La position à l'écran de chaque caractère est calculée depuis celle du premier, recalée après chaque affichage pour suivre le défilement ; le curseur de copie suit, et disparaît s'il sort par le haut.
- **Codes des touches (KM WAIT CHAR) :** &F0 à &F3 flèches, &F4 à &F7 avec SHIFT, &FA / &FB CTRL + gauche / droite, &E0 COPY, &10 CLR, &7F DEL.
- **Test modifié :** InputTests (Suppr) : la trace attendue devient « ? 12 3 » (le curseur est replacé par TXT SET CURSOR au lieu de reculer avec le code 8).
- **Fichiers :** RuntimeLibrary.cs (module Input), Z80Emitter.cs (INC H), MiniZ80.cs, `tests\CpcBasic.Tests\EditLineTests.cs` (nouveau), InputTests.cs ; exemple `examples\edition.bas`.

## D077 : les commandes du mode direct deviennent des commandes de l'éditeur (décision de l'utilisateur, 15 nov. 2026, à faire)
- **AUTO :** numérotation automatique des lignes, avec le pas demandé.
- **LIST :** affiche le programme dans l'éditeur comme sur le CPC.
- **EDIT :** place le curseur sur la ligne demandée.
- **RENUM :** renumérote ; doit provoquer une erreur si des étiquettes sont utilisées.
- **DELETE :** même rôle que le vrai BASIC ; avertissement si des étiquettes sont utilisées, puis confirmation.
- **NEW :** efface le programme en cours, avec avertissement s'il a été modifié.
- **CLEAR, MEMORY (et d'autres, liste à compléter) :** tapées dans l'éditeur, elles affichent à quoi la commande correspond dans le langage BASIC.
- **Réponses de l'utilisateur (15 nov. 2026) :** les commandes se tapent **dans le texte du programme, sans numéro de ligne** (par exemple `LIST` seul sur une ligne). Les « étiquettes » sont des noms à la place des numéros de ligne (GOTO DEPART) : pas encore dans le langage, à décider le moment venu. **CONT** affiche son rôle, comme CLEAR. Le « ... » : l'utilisateur ne connaît pas toutes les commandes directes ; Claude complète la liste quand l'étape viendra.
- **Liste connue des commandes du mode direct du Locomotive :** AUTO, CONT, DELETE, EDIT, LIST, NEW, RENUM ; souvent tapées en direct mais aussi utilisables dans un programme : RUN, LOAD, SAVE, MERGE, CAT, CHAIN ; commandes AMSDOS : |DISC, |TAPE, |A, |B, |DIR, |ERA, |REN, |USER.
- **Décision (15 nov. 2026) :** tapées dans l'éditeur sans numéro de ligne, RUN, LOAD, SAVE, MERGE, CAT, CHAIN et les commandes AMSDOS (|DISC, |TAPE, |A, |B, |DIR, |ERA, |REN, |USER) **affichent leur explication en BASIC**, comme CLEAR, MEMORY et CONT. (Leur rôle dans un programme compilé reste à traiter au lot J.)

## D078 : FOR avec une limite et un pas calculés (fin du lot A) (Validé le 16 nov. 2026 : 1060 tests réussis ; `boucle2.bas` compilé identique au vrai BASIC de SugarBox)
- **Constat (relecture de l'inventaire, 16 nov. 2026) :** FOR I = 1 TO N était refusé (limite et pas : nombres écrits seulement, D037). L'utilisateur a choisi de le finir avant le lot I.
- **Référence (SugarBox, 16 nov. 2026, `examples\boucle2.bas`) :** après la boucle, la variable vaut la valeur suivante (1 TO 5 : 6 ; 10 TO 1 STEP -3 : -2) ; une boucle déjà finie au départ ne tourne pas du tout (FOR I=5 TO 1 : I reste 5) ; la limite est calculée une seule fois (changer N dans la boucle ne change rien) ; changer I dans la boucle compte (1 3 / 5) ; idem avec les nombres à virgule (0 TO 1 STEP 0.25 : / 1.25).
- **Réalisation :** une limite ou un pas calculés sont rangés au début de la boucle dans des variables cachées (@L, @S ; pour FOR X! aussi @D, le signe du pas, par la ROM). Avec un pas calculé, le sens de la comparaison se décide pendant l'exécution selon son signe. Nouveau **test au début** de la boucle : s'il y a un calcul, il est fait pendant l'exécution ; si tout est écrit en nombres, il est décidé à la compilation (aucun octet de plus pour FOR I = 1 TO 10).
- **Inchangé :** STEP 0 écrit reste refusé ; un pas calculé qui vaut 0 fait une boucle sans fin, comme le vrai BASIC.
- **Fichiers :** Ast.cs (ForStatement : LimitValue, StepValue), Parser.cs, CodeGenerator.cs, `tests\CpcBasic.Tests\ForTests.cs` (nouveau), LoopsTests.cs (le test « limite écrite obligatoire » devient « limite et pas calculés ») ; exemple `examples\boucle2.bas`.

## D079 : lot I1, minuteries AFTER, EVERY, REMAIN, DI, EI (Validé le 16 nov. 2026 : 1074 tests réussis ; `minuter.bas` compilé identique au vrai BASIC de SugarBox, R= 24 compris)
- **Découpage du lot I :** I1 minuteries ; I2 erreurs (ON ERROR GOTO, RESUME, ERROR, ERR, ERL, plus « Break in » de STOP et « Unexpected RETURN ») ; I3 ON BREAK et ON SQ GOSUB.
- **Référence (SugarBox, 16 nov. 2026, `examples\minuter.bas`) :** REMAIN(3) = 0 sans minuterie ; EVERY 25,1 : un TIC toutes les 1/2 s ; AFTER 100,2 : un appel à 2 s, **avant** le TIC 4 qui tombe au même moment (minuterie 2 plus prioritaire que 1) ; REMAIN(1) juste après le TIC 6 : 24 (sur 25), et plus aucun TIC ensuite.
- **AFTER / EVERY durée [, minuterie] GOSUB ligne :** durée en 1/50 s ; minuterie 0 à 3 (0 si omise ; 3 la plus prioritaire), écrite : vérifiée à la compilation, calculée : « Improper argument ». **REMAIN(n)** rend le compte restant (0 si la minuterie ne tournait pas) et l'arrête. **DI / EI** retiennent / laissent partir les appels.
- **Réalisation :** au départ du programme, un compteur du firmware (KL INIT EVENT &BCEF, événement express ; KL ADD TICKER &BCE9, tous les 1/50 s) fait descendre les 4 minuteries et marque celles qui arrivent au bout (recharge pour EVERY, arrêt pour AFTER). Le programme regarde l'indicateur **au début de chaque ligne et avant chaque fin de boucle** (NEXT, WEND, UNTIL, LOOP) — 7 octets, pas au milieu d'un PRINT #n — et lance les sous-programmes de la minuterie 3 à 0. Pendant un sous-programme, seules les minuteries plus prioritaires peuvent partir. Rien n'est ajouté aux programmes sans AFTER ni EVERY.
- **Écart possible :** le vrai BASIC regarde entre chaque instruction (même entre deux « : ») ; ici au début de chaque ligne et en fin de boucle. À revoir si un programme le montre.
- **Mots réservés :** AFTER, EVERY, REMAIN, DI, EI.
- **Fichiers :** Ast.cs (TimerStatement), Diagnostic.cs, Parser.cs, Z80Emitter.cs (CALL NZ, DI, EI), CodeGenerator.cs, RuntimeLibrary.cs (module Events), MiniZ80.cs (« tops » simulés), `tests\CpcBasic.Tests\TimerTests.cs` (nouveau) ; exemple `examples\minuter.bas`.

## D080 : lot I2a, ON ERROR GOTO, RESUME, ERROR, ERR, ERL (Validé le 16 nov. 2026 : 1091 tests réussis ; `erreurs.bas` compilé identique au vrai BASIC de SugarBox)
- **Découpage de I2 :** I2a (ici) ; I2b : « Break in N » de STOP et « Unexpected RETURN in N » (vus dans SugarBox).
- **Référence (SugarBox, 16 nov. 2026, `examples\erreurs.bas`) :** ERR= 9 ERL= 40 puis RESUME NEXT ; ERR= 11 ERL= 60 puis RESUME 70 ; ERROR 200 : ERR= 200 ; ERR= 11 ERL= 110, N corrigé puis RESUME (l'instruction est refaite : 5) ; après ON ERROR GOTO 0, ERROR 5 : « Improper argument in 140 ». Dans l'exemple, 1/0 devient 1/Z (Z=0) : une division par zéro écrite en nombres reste refusée à la compilation.
- **Réalisation :** chaque instruction BASIC (début de ligne ou après « : », noté par le parser : BasicProgram.Starts) range à son début la pile et l'adresse de sa fiche (ligne, début, instruction suivante) — 10 octets, seulement dans un programme qui contient ON ERROR GOTO. Toute erreur passe par rt_error, qui saute à l'adresse d'interception si elle est rangée. Le numéro est retrouvé par le texte du message dans le tableau des 32 messages du Locomotive (module ErrorCodes) ; ERROR n garde son numéro. La pile est remise comme au début de l'instruction, puis saut à la ligne du ON ERROR GOTO. Erreur pendant le traitement d'une erreur : message et arrêt. La fiche et la pile de l'instruction fautive sont mises de côté au moment de l'erreur (rt_oe_failrec, rt_oe_failsp) : RESUME et RESUME NEXT s'en servent (premier essai : le RESUME écrasait la fiche). RESUME sans erreur : « Unexpected RESUME ».
- **Erreurs qui laissent continuer :** Overflow et Division by zero (nombres à virgule, et maintenant aussi la division entière par une valeur nulle) : interceptées par ON ERROR GOTO, sinon le message s'écrit et le programme continue (division entière : résultat 0).
- **ERROR n** (1 à 255) : message du Locomotive, « Unknown error » s'il n'y en a pas.
- **Minuteries (D079) :** l'appel regarde maintenant au début de chaque instruction BASIC (et en fin de boucle), comme le vrai BASIC.
- **Mots réservés :** ERROR, RESUME, ERR, ERL.
- **Fichiers :** Ast.cs (OnErrorStatement, ResumeStatement, ErrorStatement, BasicProgram.Starts), Parser.cs, Z80Emitter.cs (LD (nn),SP ; LD SP,(nn)), CodeGenerator.cs, RuntimeLibrary.cs (RunError, Div16, ErrorCodes, OnError), RealRuntime.cs, MiniZ80.cs, `tests\CpcBasic.Tests\OnErrorTests.cs` (nouveau), JumpTests.cs ; exemple `examples\erreurs.bas`.

## D081 : lot I2b, « Break in N » de STOP et « Unexpected RETURN » (Validé le 16 nov. 2026 : 1093 tests réussis ; `retour.bas` correct dans WinAPE)
- **Référence (SugarBox, 16 nov. 2026) :** `10 GOSUB 100:PRINT "A"` / `20 STOP` / `30 RETURN` / `100 RETURN` : « A », « Break in 20 » ; après CONT : « Unexpected RETURN in 30 ».
- **STOP :** affiche « Break in N » (à la ligne si le curseur n'est pas au début), puis attend une touche et termine comme END. Pas intercepté par ON ERROR. Il n'y a pas de CONT dans un programme compilé.
- **RETURN sans GOSUB :** GOSUB, ON … GOSUB et l'appel d'une minuterie comptent les appels en cours (un octet, module Gosub) ; RETURN sans appel en cours donne l'erreur 3, « Unexpected RETURN in N », que ON ERROR GOTO peut intercepter. Coût : 4 octets par GOSUB, RETURN devient un saut vers rt_return.
- **Exemple :** `examples\retour.bas` (A, ERREUR 3 LIGNE 40, puis Break in 50).
- **Fichiers :** CodeGenerator.cs, RuntimeLibrary.cs (module Gosub, message Break), JumpTests.cs (les octets de GOSUB et STOP ont changé).

## D082 : lot I3a, ON SQ GOSUB ; choix pour la touche ESC (Validé le 16 nov. 2026 : 1103 tests réussis ; `sonfile.bas` correct dans WinAPE)
- **Découpage de I3 :** I3a (ici) : ON SQ(canal) GOSUB ; I3b : ON BREAK CONT / GOSUB / STOP et la touche ESC.
- **Référence ON SQ (SugarBox, 16 nov. 2026, `examples\sonfile.bas`) :** 5 « SQ » tout de suite (le son en cours et 4 en attente dans la file), puis un à chaque fin de son ; « FIN 11 » avec la boucle de 3000. Compilée, cette boucle est trop rapide (« FIN 5 ») : l'exemple attend donc 11 appels (WHILE C<11:WEND) ; WinAPE : 5 d'un coup puis un par fin de son, « FIN 11 ».
- **Réalisation :** ON SQ(canal) range la ligne et arme l'événement du firmware (SOUND ARM EVENT &BCB0, événement express asynchrone). Le firmware l'appelle une seule fois, dès qu'une place est libre (tout de suite si la file en a déjà une) : le sous-programme refait ON SQ, comme dans le vrai BASIC. L'appel se fait entre deux instructions, avec les minuteries ; priorité des canaux sonores = celle de la minuterie 2 (seule la minuterie 3 peut les interrompre, d'après cpcwiki) ; DI les retient aussi. Canal 1, 2 ou 4, sinon « Improper argument ».
- **Taille :** module EventsSound (Events + canaux sonores), lié à la place d'Events seulement si le programme contient ON SQ.
- **Touche ESC (décision de l'utilisateur, 16 nov. 2026) :** réglage dans l'éditeur, par défaut comme le vrai BASIC (ESC : pause ; ESC ESC : « Break in N ») ; l'autre choix l'ignore (programme plus petit et plus rapide). Fait en I3b.
- **Référence ON BREAK GOSUB (SugarBox, 16 nov. 2026) :** premier ESC : pause (curseur affiché) ; une autre touche : reprise sans appel ; deuxième ESC : appel du sous-programme, puis le programme continue ; marche à chaque fois (BREAK 1, 2, 3…).
- **Fichiers :** Ast.cs (OnSqStatement), Parser.cs, Diagnostic.cs (messages de ON ERROR et ON SQ), CodeGenerator.cs, RuntimeLibrary.cs (EmitEvents, EventsSound), MiniZ80.cs (files sonores), `tests\CpcBasic.Tests\OnSqTests.cs` (nouveau), JumpTests.cs.

## D083 : lot I3b, ON BREAK CONT / STOP / GOSUB et réglage de la touche ESC (Validé le 16 nov. 2026 : 1115 tests réussis ; `touchesc.bas` correct dans WinAPE)
- **Références (SugarBox, 16 nov. 2026) :** ON BREAK GOSUB : premier ESC = pause (curseur affiché), une autre touche = reprise sans appel, ESC encore = appel puis reprise, à chaque fois. ON BREAK CONT : ESC sans aucun effet (pas de pause). ON BREAK STOP : comportement normal, « Break in 30 » à la ligne puis arrêt.
- **Réglage F9 de l'éditeur (décision de l'utilisateur, D082) :** « ESC : comme le CPC » (par défaut) ou « ESC : ignorée », mémorisé pour chaque programme (comme F8). Ignorée : rien n'est ajouté au programme, sauf s'il contient ON BREAK (alors ESC est ignorée jusqu'au premier ON BREAK). Dans la bibliothèque (CompilerOptions.BreakKey), la valeur par défaut reste « ignorée », pour garder les programmes de test identiques.
- **Réalisation :** le compteur des minuteries (tous les 1/50 s) regarde la touche ESC (KM TEST KEY 66) ; un nouvel appui est marqué, et traité entre deux instructions avant tout le reste (DI ne le retient pas) : file du clavier vidée ; CONT : rien ; sinon pause (TXT PLACE CURSOR, KM WAIT CHAR) ; ESC : « Break in N » (STOP) ou appel (GOSUB, au-dessus de toutes les minuteries) ; autre touche : on continue. Chaque ligne range son numéro (6 octets par ligne) pour « Break in N ». Modules : EventsBreak, EventsSoundBreak.
- **Limites :** ESC n'est vue qu'entre deux instructions : pas pendant INPUT, une attente de touche ou un CALL ; après un retour de GOSUB au milieu d'une ligne, « Break in » peut donner la ligne du sous-programme.
- **Exemple :** `examples\touchesc.bas`. Premier essai : un appel tombé entre `LOCATE 1,5` et `PRINT N` a fait écrire le compteur sous « BREAK » (le vrai BASIC peut faire pareil) ; la ligne 50 place maintenant le curseur et écrit en une seule instruction (PRINT CHR$(31);CHR$(1);CHR$(5);N;).
- **Fichiers :** CompilerOptions.cs, Ast.cs (OnBreakStatement, BreakMode), Parser.cs, Diagnostic.cs, CodeGenerator.cs, RuntimeLibrary.cs, `src\CpcIde\MainWindow.axaml`, `MainWindow.axaml.cs`, `EditorSettings.cs` ; MiniZ80.cs (touches tapées à un moment donné), `tests\CpcBasic.Tests\BreakTests.cs` (nouveau), JumpTests.cs.

## D084 : lot J1, fichiers de données (Validé le 16 nov. 2026 : 1133 tests réussis ; `fichiers.bas` correct dans WinAPE, `SCORES.` créé sur la disquette)
- **Découpage du lot J :** J1 (ici) fichiers de données ; J2 : CAT, SAVE et LOAD de fichiers binaires (écrans…), SPEED WRITE ; J3 : enchaîner des programmes (RUN "fichier", CHAIN) ; LOAD / MERGE d'un programme BASIC : à discuter. Les commandes AMSDOS (|DISC, |TAPE, |A…) sont des RSX : lot K.
- **Références (SugarBox, 16 nov. 2026) :** PRINT #9 range les virgules en zones de 13 comme à l'écran (« BONJOUR », 12 en colonne 14, 3.5 en colonne 27) ; PRINT #9,"FIN";-7 donne « FIN-7 » ; WRITE #9 : `"A,B",42`. INPUT #9,A$,B,C : A$ reçoit toute la ligne 1 (pas de virgule), B et C lisent les lignes suivantes, et un texte qui n'est pas un nombre donne 0 (pas de message). CAT : `TEST.` (sans extension). Erreurs : OPENIN d'un fichier absent : « ABSENT  .  not found » écrit par AMSDOS, puis arrêt sans « in N » ; INPUT #9 sans OPENIN : « File not open in 10 » ; lecture après la fin : « EOF met in 10 » ; deux OPENOUT : « File already open in 10 ».
- **Écriture :** pendant PRINT #9 / WRITE #9, quatre entrées du firmware sont détournées (remplacées par un saut, puis remises à la fin de l'instruction) : TXT OUTPUT écrit dans le fichier (CAS OUT CHAR ; l'entrée d'origine est remise pendant l'écriture, AMSDOS pouvant écrire un message), TXT GET CURSOR donne la colonne du fichier, TXT VALIDATE accepte tout, TXT GET WINDOW donne 255 colonnes. Les zones, TAB, SPC, PRINT USING et les nombres sortent donc comme à l'écran.
- **Lecture :** une ligne du fichier (CAS IN CHAR jusqu'au retour à la ligne ; le saut de ligne suivant est sauté, CAS RETURN sinon) va dans la ligne de saisie du clavier, découpée comme INPUT ; quand elle est finie, la suivante. Nombres lus souplement (comme VAL) : séparés par des virgules ou des espaces ; pas de chiffre : 0. LINE INPUT #9 : toute la ligne. EOF : -1 à la fin ou sans fichier ouvert, sinon 0.
- **Ouverture impossible :** arrêt sans message (AMSDOS a écrit le sien), attente d'une touche ; avec ON ERROR GOTO : erreur 32.
- **Mémoire :** les zones de 2 Ko du firmware sont à &3000 (lecture) et &3800 (écriture), sous le programme (&4000) : elles n'agrandissent pas le fichier.
- **Limites :** #9 seulement écrit tel quel (pas « #N » calculé) ; un fichier ouvert en écriture n'est pas fermé tout seul à la fin du programme (CLOSEOUT nécessaire, sinon la fin est perdue) ; « Disc full » et autres erreurs pendant l'écriture : seulement le message d'AMSDOS.
- **Mots réservés :** OPENOUT, OPENIN, CLOSEOUT, CLOSEIN, EOF.
- **Exemple :** `examples\fichiers.bas` (scores écrits avec WRITE #9 puis relus avec INPUT #9).
- **Fichiers :** Ast.cs (FileStatement), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (modules FileCommon, FileOut, FileIn), Z80Emitter.cs (INC B), MiniZ80.cs (disque simulé, entrées détournées), `tests\CpcBasic.Tests\FileTests.cs` (nouveau).

## D085 : lot J2, CAT, SAVE et LOAD binaires, SPEED WRITE (Validé le 16 nov. 2026 : 1150 tests réussis ; `binaire.bas` dans WinAPE : `ECRAN.BIN` de 17K, l'écran revient à l'identique)
- **Références (SugarBox, 16 nov. 2026) :** sans MEMORY, `LOAD "DONNEES",&9000` donne « Memory full in 50 » (le vrai BASIC ne charge qu'au-dessus de HIMEM) ; avec `MEMORY &7FFF` : SAVE, LOAD à une autre adresse, puis LOAD sans adresse (à l'adresse d'origine) fonctionnent ; AMSDOS nomme le fichier `DONNEES.BIN` et garde l'ancien en `.BAK`. LOAD d'un fichier absent : « not found » d'AMSDOS, arrêt sans « in N ». OPENIN d'un fichier binaire : « File type error in 10 ».
- **Choix (accepté par l'utilisateur) :** `SAVE "nom"` sans `,B` (ou `,A`, `,P`) enregistrerait le programme BASIC, qui n'existe plus une fois compilé : refusé à la compilation avec une explication. LOAD d'un programme BASIC ou d'un fichier texte : « File type error ».
- **Réalisation :** SAVE : CAS OUT OPEN, CAS OUT DIRECT (type binaire), CAS OUT CLOSE. LOAD : CAS IN OPEN (type et adresse d'origine), vérification de l'adresse (au moins la limite de MEMORY, ou &A67C par défaut, sinon « Memory full »), CAS IN DIRECT, CAS IN CLOSE. CAT : CAS CATALOG ; refusé (« File already open ») si un fichier est ouvert en lecture, comme le firmware (pas vérifié sur le vrai BASIC). SPEED WRITE 0 : 1000 bauds, 1 : 2000 bauds (CAS SET SPEED). OPENIN (J1) refuse maintenant un fichier qui n'est pas un texte (« File type error »).
- **Exemple :** `examples\binaire.bas` (l'écran est enregistré, effacé, puis rechargé).
- **Fichiers :** Ast.cs (SaveStatement, LoadStatement, SpeedWriteStatement), Diagnostic.cs (SaveProgram), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (module FileBinary ; FileCommon garde les fichiers ouverts), MiniZ80.cs, `tests\CpcBasic.Tests\FileBinaryTests.cs` (nouveau).

## D086 : lot J3, RUN "nom" ; CHAIN et MERGE refusés (Validé le 16 nov. 2026 : 1162 tests réussis ; `enchaine.bas` se relance deux fois dans WinAPE, « LANCEMENT NUMERO 3 » puis « FINI »)
- **Décisions de l'utilisateur (16 nov. 2026) :** RUN "nom" lance un autre programme compilé ; CHAIN refusé à la compilation (les variables ne peuvent pas passer d'un programme compilé à l'autre ; RUN "nom" et un fichier à la place). **Plusieurs programmes sur une disquette (idée de l'utilisateur, prévue en J4) :** en compilant un programme, le compilateur repère les RUN "nom" écrits en clair, compile aussi nom.BAS s'il est dans le même dossier (et ce qu'il lance à son tour, sans boucle), et met tout sur la même disquette ; peut-être aussi les fichiers binaires prêts (LOAD "ECRAN" -> ECRAN.BIN du dossier).
- **Refusés à la compilation, avec une explication :** CHAIN, CHAIN MERGE, MERGE (ajoutent des lignes BASIC). RUN sans nom et RUN ligne (relancer le programme) : pas encore (il faut remettre les variables à zéro).
- **Réalisation de RUN "nom" :** les fichiers ouverts sont fermés, le compteur du firmware retiré (KL DEL TICKER) et les sons arrêtés (SOUND RESET), la table des caractères redessinés rendue au firmware. Sans point dans le nom, « .BIN » est ajouté (le .BAS du même nom est le chargeur BASIC). Le fichier est ouvert (CAS IN OPEN) : absent, arrêt sans message comme LOAD (erreur 32 avec ON ERROR GOTO) ; pas binaire : « File type error ». Puis un chargeur de 20 octets est recopié en &3800, hors du programme : il charge le fichier à son adresse (CAS IN DIRECT) et saute à son adresse de lancement. Marche aussi pour un binaire qui n'est pas compilé ici.
- **Exemple :** `examples\enchaine.bas` : compte ses lancements dans un fichier et se relance lui-même deux fois (au premier lancement, AMSDOS écrit « COMPTE . not found », intercepté par ON ERROR GOTO).
- **Fichiers :** Ast.cs (RunFileStatement), Diagnostic.cs (NotForCompiled), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (rt_fb_run, rt_ev_stop), Z80Emitter.cs (LD C,B), MiniZ80.cs, `tests\CpcBasic.Tests\RunFileTests.cs` (nouveau).

## D087 : lot J4, compilation en chaîne (idée de l'utilisateur ; Validé le 16 nov. 2026 : 1167 tests réussis ; F5 sur partie1.bas met PARTIE1 et PARTIE2 sur la disquette, et la partie 2 reçoit le score 1200 dans WinAPE)
- **Idée de l'utilisateur (16 nov. 2026) :** quand le programme contient RUN "nom", le compilateur compile aussi « nom » et met tout sur la même disquette (un jeu en plusieurs parties).
- **Réalisation :** Compiler.CompileWithLinked. Chaque RUN "nom" écrit en clair (sans point, ou en .BIN) fait chercher nom.BAS dans le dossier du programme (F5, sans tenir compte des majuscules) ; il est compilé avec ses propres réglages F8 et F9, puis ceux qu'il lance à son tour, chacun une seule fois (même s'ils se relancent entre eux). Tous vont sur la disquette du programme de l'éditeur (nom.DSK), chacun avec son chargeur .BAS et son .BIN. Un programme introuvable est seulement signalé dans les messages ; une erreur dans un autre programme est donnée avec son nom (« ERREUR dans PARTIE2.BAS, ligne 1 : … », ligne du fichier comme dans l'éditeur) et aucune disquette n'est écrite.
- **Limites :** un nom calculé (RUN N$) n'est pas repéré ; RUN "nom.BAS" non plus (ce n'est pas un programme compilé). Les fichiers binaires prêts (LOAD "ECRAN" -> ECRAN.BIN du dossier) : pas encore (idée notée).
- **Exemple :** `examples\partie1.bas` (écrit un score dans un fichier puis RUN "PARTIE2") et `examples\partie2.bas` (relit le score).
- **Fichiers :** Compiler.cs (CompileWithLinked, LaunchedPrograms, ProgramSource, LinkedProgram), Diagnostic.cs (File), DiskImage.cs (FileNames), `src\CpcIde\MainWindow.axaml.cs`, `tests\CpcBasic.Tests\LinkedCompileTests.cs` (nouveau).

## D088 : lot K1, TRON et TROFF (Validé le 16 nov. 2026 : 1170 tests réussis ; `trace.bas` correct dans WinAPE)
- **Référence (SugarBox, 16 nov. 2026) :** `[20]A[30] 1  2 [40][60]B` : le numéro s'affiche entre crochets au début de chaque ligne exécutée, une seule fois quand NEXT revient dans la même ligne.
- **Réalisation :** seulement si le programme contient TRON : au début de chaque ligne, LD HL,numéro / CALL rt_tron (6 octets par ligne) ; rt_tron affiche « [n] » si TRON est en marche. TRON / TROFF changent un octet. Module Trace.
- **Exemple :** `examples\trace.bas`.
- **Fichiers :** Ast.cs (TraceStatement), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs, `tests\CpcBasic.Tests\TraceTests.cs` (nouveau).

## D089 : lot K2, commandes RSX et CALL avec valeurs (Validé dans WinAPE le 16 nov. 2026 : **le programme de référence de l'utilisateur se compile et donne le bon dessin** ; le fichier PACK-GPH.BIN qu'il enregistre fonctionne aussi depuis le BASIC : LOAD, CALL &A400, |GPRINT, |GPEN, |GPAS)
- **But :** compiler le programme de référence de l'utilisateur (`docs\PROGRAMME-DE-REFERENCE-RSX.md` : |GPEN,3, |GPAS,l,8, |GPRINT,@a$).
- **Règle du firmware (comme le Locomotive) :** les valeurs sont posées sur la pile dans l'ordre, IX pointe sur la dernière (2 octets chacune, la dernière en premier), A = nombre de valeurs (32 au plus), et **DE = la dernière valeur** (premier essai du programme de référence : sans DE, |GPRINT dessinait des carrés, car ses routines prennent l'adresse du texte dans DE, |GPEN la couleur dans E). Un texte passe l'adresse de sa fiche de 3 octets (comme @a$). Au retour, les valeurs quittent la pile.
- **|NOM :** le nom (dernière lettre avec le bit 7) est cherché par KL FIND COMMAND (&BCD4), puis appelé par KL FAR PCHL (&001B, HL = adresse, C = ROM) ; pas trouvé : « Unknown command » (erreur 28, interceptable par ON ERROR GOTO). **CALL adresse, valeurs :** même règle ; CALL adresse seul ne change pas.
- **Fichiers :** Lexer.cs (TokenKind.Bar), Ast.cs (ExternalCallStatement), Parser.cs (ParseRsx, TryParseExternalArguments, HasTopLevelComma), CodeGenerator.cs, RuntimeLibrary.cs (module Rsx), Z80Emitter.cs (LD IX,nn ; ADD IX,SP), MiniZ80.cs (IX, KL FIND COMMAND, KL FAR PCHL), `tests\CpcBasic.Tests\RsxTests.cs` (nouveau).

## D090 : lot K3, CLEAR, RUN et RUN ligne (Validé le 16 nov. 2026 : 1187 tests réussis ; `relance.bas` correct dans WinAPE)
- **Références (SugarBox, 16 nov. 2026) :** CLEAR puis DIM T(3) : « 0 [] 0 » ; RUN dans le programme (compteur en &9000) : « 7  0  7  1  7  2 FIN » (DATA remis au début).
- **CLEAR :** toutes les variables à 0 (un texte : sa fiche à 0, donc vide ; un nombre à virgule : 0), les tableaux effacés (DIM de nouveau permis), la mémoire libre comme au départ (textes et tableaux oubliés ; la limite de MEMORY reste). Les variables du programme sont rangées entre vars_start et la fin, les descripteurs de tableaux à partir d'arrays_start : un LDIR les remet à 0 (routine prog_clear).
- **RUN, RUN ligne :** comme CLEAR, plus : DATA au début, plus de GOSUB en cours ni de ON ERROR GOTO, fichiers fermés, compteur des minuteries retiré (il est rebranché au départ) ; la pile est remise comme au lancement (gardée au tout début du programme), puis le programme recommence sa mise en route ; RUN ligne saute ensuite à la ligne.
- **Fichiers :** Ast.cs (ClearStatement, RestartStatement), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (rt_file_closeall), `tests\CpcBasic.Tests\RestartTests.cs` (nouveau), RunFileTests.cs. Exemple : `examples\relance.bas`.

## D091 : lot K4, XPOS, YPOS, TEST, TESTR, SPEED INK, SPEED KEY (Validé le 16 nov. 2026 : 1195 tests réussis ; `points.bas` dans WinAPE : 101 points, curseur 638 150)
- **Référence (SugarBox, 16 nov. 2026) :** après PLOT 100,50,2 et MOVE 30,40 : XPOS;YPOS = 30 40 ; TEST(100,50) = 2 et le curseur graphique va en 100,50 ; TESTR(70,10) part donc de là (0) ; TEST(0,0) = 0. SWAP A,B : « Syntax error » : SWAP n'existe pas (seul WINDOW SWAP), noté en écart dans la liste.
- **Réalisation (firmware) :** XPOS, YPOS : GRA ASK CURSOR (&BBC6) ; TEST, TESTR : GRA TEST ABSOLUTE / RELATIVE (&BBF0, &BBF3) ; SPEED INK a,b : SCR SET FLASHING (&BC3E, H = a, L = b) ; SPEED KEY a,b : KM SET DELAY (&BB3F). Valeurs de SPEED : 1 à 255.
- **Fichiers :** Ast.cs (SpeedStatement), Parser.cs, CodeGenerator.cs, MiniZ80.cs (petit modèle du graphisme), `tests\CpcBasic.Tests\GraphicsQueryTests.cs` (nouveau). Exemple : `examples\points.bas`.

## D092 : lot K5, KEY et KEY DEF (Validé le 16 nov. 2026 : 1205 tests réussis ; `touches.bas` dans WinAPE : [BONJOUR?] ; SugarBox : même résultat, KEY 32 et KEY DEF 80 donnent « Improper argument in 10 »)
- **KEY n, texte :** n de 0 à 31 ou de 128 à 159 (les deux désignent la même touche : 128 + n) ; KM SET EXPAND (&BB0F, B = 128 + n, C = longueur, HL = lettres) ; plus de place : « Improper argument ».
- **KEY DEF touche, répétition [, normal [, shift [, ctrl]]] :** touche de 0 à 79, répétition 0 ou 1, valeurs de 0 à 255 ; KM SET REPEAT (&BB39, B = &FF ou 0), KM SET TRANSLATE (&BB27), KM SET SHIFT (&BB2D), KM SET CONTROL (&BB33), A = touche. Hors limites : refusé à la compilation (nombre écrit) ou « Improper argument ».
- **À savoir :** un programme compilé redémarre le CPC à la fin : les touches redeviennent normales.
- **Fichiers :** Ast.cs (KeyStatement, KeyDefStatement), Parser.cs, CodeGenerator.cs, RuntimeLibrary.cs (module Keys), Z80Emitter.cs (LD B,L ; OR H), MiniZ80.cs, `tests\CpcBasic.Tests\KeyDefinitionTests.cs` (nouveau). Exemple : `examples\touches.bas`.

## D093 : LINE INPUT affiche « ? » comme INPUT (correction de D064 ; Validé le 16 nov. 2026 : 1205 tests réussis ; `touches.bas` dans WinAPE : « ? BONJOUR? » comme SugarBox)
- **Référence (SugarBox, 16 nov. 2026) :** `LINE INPUT A$` : « ? ABC » ; `LINE INPUT "NOM";A$` : « NOM? ABC » ; `LINE INPUT "NOM",A$` : « NOMABC ». La même règle qu'INPUT : « ? » sans texte ou après « ; », pas après « , ». D064 disait « sans ? » : c'était faux (vu grâce à la capture de KEY, D092).
- **Fichiers :** Ast.cs (LineInputStatement.Question), Parser.cs, CodeGenerator.cs ; tests adaptés : StringArrayTests.cs, StringDataTests.cs, EditLineTests.cs (la saisie commence en colonne 3).

## D094 : lot K6, HIMEM, FRE, UNT, WAIT et adresses à virgule (Validé le 16 nov. 2026 : 1220 tests réussis ; `memoire.bas` dans WinAPE : 42619, -1, -32768, -22917, 41619, 77, LIBRE 21064 puis 21074 après le ménage)
- **Référence (SugarBox, 16 nov. 2026) :** `PRINT HIMEM` : 42619 ; `UNT(&FFFF)`, `UNT(&8000)`, `UNT(100)` : -1, -32768, 100 ; `FRE(0)` et `FRE("")` : 42167 ; après `MEMORY &9FFF`, HIMEM vaut 40959.
- **HIMEM** (sans parenthèses) : dernier octet libre = limite de la mémoire libre - 1 (&A67B au départ, ou l'adresse donnée par MEMORY). Répond à la question de D057 : la valeur dépasse 32767, donc HIMEM rend un **nombre à virgule**, comme le Locomotive (A% = HIMEM donne « Overflow », comme sur le CPC).
- **FRE(0)** : place entre le haut des tableaux et le bas des textes, en nombre à virgule. **FRE(texte)**, par exemple `FRE("")` : fait d'abord le ménage des textes (rt_str_gc). **Écart connu :** le nombre n'est pas celui du vrai CPC (le programme compilé n'est pas rangé comme un programme BASIC).
- **UNT(adresse)** : la même valeur sur 16 bits, en entier signé (UNT(&FFFF) = -1).
- **WAIT port, masque [, inversion]** : attend que (INP(port) XOR inversion) AND masque ne vaille plus 0. Masque et inversion de 0 à 255 (vérifiés à la compilation s'ils sont écrits en nombres). Comme sur le CPC, ESC n'interrompt pas l'attente.
- **Adresses à virgule (règle nouvelle) :** une adresse ou un port calculé avec un nombre à virgule (MEMORY HIMEM-1000, POKE HIMEM,77, PEEK(HIMEM), CALL, INP, OUT, UNT) va de -32768 à 65535, comme le vrai BASIC ; au-delà : « Overflow ». Routine `rt_fr2a` (RealRuntime).
- **Mémoire libre :** HIMEM et FRE utilisent le module Heap même sans tableau ni texte ; MEMORY met alors à jour la limite pendant l'exécution. Sans HIMEM ni FRE, rien ne change (MEMORY n'ajoute aucun octet).
- **Mots réservés :** aucun nouveau (mots du Locomotive).
- **Fichiers :** Parser.cs, Ast.cs (HIMEM et FRE sont à virgule), CodeGenerator.cs (EmitAddress, usesHeap, WAIT, UNT, HIMEM, FRE), RealRuntime.cs (rt_fr2a), `tests\CpcBasic.Tests\MemoryInfoTests.cs` (nouveau), ArrayTests.cs (FRE remplacé par DERR). Exemple : `examples\memoire.bas`.

## D095 : lot K7, DEF FN et FN (Validé le 16 nov. 2026 : 1238 tests réussis ; `fonctfn.bas` dans WinAPE : 42 100, BONJOUR!, 101 101, 106, 1.5, 302)
- **Référence (SugarBox, 16 nov. 2026) :** `X=100`, `DEF FNDOUBLE(X)=X*2`, `DEF FNA$(N$)=N$+"!"`, `DEF FNR=X+1`, `DEF FNM(A,B)=A*B+X` : `FNDOUBLE(21);X` = 42 100 ; `FNA$("BONJOUR")` = BONJOUR! ; `FNR;FN R` = 101 101 ; `FNM(2,3)` = 106 ; `FNDOUBLE(1.5)` = 3.
- **Conception :** DEF FN ne fait rien à l'exécution. À chaque appel, les valeurs sont rangées dans des variables cachées propres à la fonction (`FNDOUBLE.X`), puis le calcul est recopié sur place. Le X de la fonction ne touche donc pas le X du programme. Le calcul peut utiliser les variables du programme. Si une valeur après la première appelle une FN (`FNP(FNP(2,3),FNP(4,5))`), les valeurs passent d'abord par des variables propres à cet appel.
- **Écriture :** `FNNOM` ou `FN NOM` ; le lecteur coupe tout mot qui commence par FN (FNDOUBLE = FN DOUBLE), comme le Locomotive. Le type vient du nom, comme une variable : `FNA$` texte, `FNR!` à virgule ; DEFINT, DEFREAL, DEFSTR et F8 s'appliquent au nom (première lettre après FN) et aux valeurs.
- **Écarts voulus (validés par l'utilisateur) :** (1) sans signe, une valeur ou un nom de FN est entier (D026) : `FNDOUBLE(1.5)` est refusé à la compilation ; (2) fonction jamais définie, ou définie deux fois : erreur à la compilation (le Locomotive : « Unknown user function » à l'exécution) ; DEF FN peut être placé avant ou après l'appel ; (3) une FN qui s'appelle elle-même, même par une autre FN, est refusée.
- **Demande de l'utilisateur :** quand une virgule serait perdue, le message donne la solution : « FNDOUBLE reçoit un nombre à virgule pour X, mais X est un entier… écrivez DEF FNDOUBLE(X!)=…, et X! dans le calcul ». Même conseil quand le calcul d'une FN entière donne un nombre à virgule (écrire FNNOM!).
- **Mots réservés :** DEF et FN (déjà réservés par le Locomotive). Aucune variable ne peut commencer par FN (FNX se lit FN X), comme sur le CPC.
- **Fichiers :** Lexer.cs (FN coupé), Ast.cs (UserFunction, UserFunctionExpression, DefFnStatement ; IsReal, IsString, AstWalker), Parser.cs (ParseDefFn, ParseUserFunctionCall, CheckUserFunctions), Diagnostic.cs (6 messages), CodeGenerator.cs (EmitUserArguments), `tests\CpcBasic.Tests\UserFunctionTests.cs` (nouveau). Exemple : `examples\fonctfn.bas`.

## D096 : lot K8, ERASE (Validé le 16 nov. 2026 : 1247 tests réussis ; `efface.bas` dans WinAPE : LIBRE 21374 puis 21395, 33, 7, [], « Improper argument in 130 »)
- **Référence (SugarBox, 16 nov. 2026) :** `DIM T(5),N$(2),U(3)` puis `ERASE T,N$` : FRE monte (41966 puis 42023), U(1) garde 33, `DIM T(10)` est permis, N$(1) redevient vide (tableau recréé tout seul, 11 cases), `ERASE U:ERASE U` : « Improper argument in 90 ».
- **Conception :** module ArrayErase (rt_arr_erase : HL = descripteur, B = dimensions, C = taille d'une case). La place du tableau est rendue : les cases des tableaux créés après lui descendent (LDIR), rt_arr_heap baisse, leurs descripteurs sont corrigés, et le descripteur effacé revient à 0 (le tableau pourra être recréé par DIM ou tout seul). Le générateur écrit une table de tous les tableaux (rt_arr_table : descripteur, dimensions, taille d'une case) seulement si ERASE est utilisé.
- **Tableaux de textes :** chaque bloc de texte garde l'adresse de sa fiche (D062). Quand un tableau de textes descend, cette adresse est corrigée pour chaque fiche ; les blocs du tableau effacé reçoivent l'adresse d'une fiche vide (rt_er_dead) et sont jetés au ménage suivant. Les textes écrits dans le programme (sous rt_str_ptr) n'ont pas de bloc et ne sont pas touchés.
- **Erreur :** tableau jamais créé ou déjà effacé : « Improper argument in N », comme le Locomotive. Un nom seulement présent dans ERASE a un descripteur à une dimension.
- **Écart (validé) :** un tableau garde toujours le même nombre de dimensions (D058), même après ERASE. Le vrai BASIC permet `ERASE R:DIM R(5)` après `DIM R(2,2)` ; ici c'est refusé à la compilation avec le message de D058.
- **Mots réservés :** ERASE (déjà réservé par le Locomotive).
- **Fichiers :** Ast.cs (EraseStatement, UsesArrays), Parser.cs, Diagnostic.cs (ExpectedEraseName), RuntimeLibrary.cs (module ArrayErase), CodeGenerator.cs (ElementBytes, table), `tests\CpcBasic.Tests\EraseTests.cs` (nouveau), ArrayTests.cs (ERASE remplacé par FRAME). Exemple : `examples\efface.bas`.

## D097 : lot K9, TIME complet avec F8 et ON x GOTO hors limites (Validé le 16 nov. 2026 : 1256 tests réussis ; `temps.bas` dans WinAPE : TIME 4929, SUITE 0, SAUT AVEC I = 1, ON 255 continue, « Improper argument in 100 »)
- **Référence (SugarBox, 16 nov. 2026) :** `ON I GOTO 30,30` avec I = 0 : continue ; I = 1 : saute ; `ON 255 GOTO` : continue ; `ON 256 GOTO 30` : « Improper argument in 50 ».
- **ON x GOTO / GOSUB :** de 0 à 255, comme avant (0 ou plus grand que le nombre de lignes : on continue) ; en dehors (négatif ou plus de 255) : « Improper argument », comme le Locomotive. Module OnRange ; aucun octet ajouté quand la valeur est un nombre écrit de 0 à 255. Tests de JumpTests adaptés (-1 et 32767 donnaient « on continue »).
- **TIME (choix A de l'utilisateur) :** avec le réglage F8, TIME est complet (32 bits, nombre à virgule : KL TIME PLEASE puis BINARY TO REAL), comme le vrai BASIC ; là où il faut un entier (RANDOMIZE TIME, A% = TIME), il garde les 16 bits du bas, sans « Overflow ». Sans F8, rien ne change (16 bits, D026) : `T=TIME ... IF TIME-T>300` continue de marcher avec des entiers. Nom interne : CallExpression « TIME! ».
- **À savoir (constaté par l'utilisateur) :** avec F8, les boucles d'attente `FOR K = 1 TO 30000` deviennent des boucles à virgule, bien plus lentes (comme sur le vrai CPC). Pour une boucle rapide avec F8 : `FOR K% = 1 TO 30000`.
- **Fichiers :** Parser.cs, Ast.cs, RuntimeLibrary.cs (module OnRange), CodeGenerator.cs (OnNeedsCheck, TIME!), JumpTests.cs, `tests\CpcBasic.Tests\TimeFullTests.cs` (nouveau). Exemple : `examples\temps.bas`.

## D098 : lot K10, PRINT USING avec un format dans une variable (Validé le 17 nov. 2026 : 1279 tests réussis ; `formats.bas` dans WinAPE identique à SugarBox)
- **Référence (SugarBox, 17 nov. 2026) :** `F$="###.##"` : 3.14 ; `F$="PRIX ##.# EUR":PRINT USING F$;5;-7.25` : « PRIX  5.0 EURPRIX -7.3 EUR » (le format est repris au début pour la seconde valeur, sans passer à la ligne) ; « ! » : B ; « \  \ » : ABCD ; « & » avec "A"+"B" : AB ; `F$="X"` (aucun emplacement) : « X » est écrit, puis « Improper argument in 60 ». Le caractère « \ » se tape dans SugarBox avec CHR$(92).
- **Conception :** un format écrit entre guillemets reste découpé à la compilation (D074). Un format dans une variable ou un calcul (`PRINT USING F$; …`, `PRINT USING "##"+".#"; …`) donne une instruction PrintUsingDynamicStatement : le module UsingDyn lit le format pendant l'exécution avec les mêmes règles (« ! », « & », « \  \ », [+] [** [$] | $$] # et , [. #] [^^^^] [+ ou -]), écrit le texte entre les emplacements, puis appelle les mêmes routines d'affichage que D074 (modules UsingDynNum et UsingDynStr). La fiche du format est relue à chaque caractère (le ménage des textes peut déplacer ses lettres).
- **Erreurs à l'exécution :** format sans emplacement : son texte puis « Improper argument » ; texte dans un emplacement de nombre (ou l'inverse) : « Type mismatch ». À la compilation : format qui n'est pas un texte, ou pas suivi de « ; ».
- **Tests :** chaque format donne exactement le même affichage écrit entre guillemets ou rangé dans F$ (16 formats). UsingTests.cs : l'ancien test « format dans une variable pas encore possible » vérifie maintenant le « ; » obligatoire.
- **Fichiers :** Ast.cs, Parser.cs (ParseUsingValues, ParseUsingDynamic), Diagnostic.cs (message de UsingFormatNotLiteral), RuntimeLibrary.cs (modules UsingDyn, UsingDynNum, UsingDynStr), CodeGenerator.cs, `tests\CpcBasic.Tests\UsingDynamicTests.cs` (nouveau). Exemple : `examples\formats.bas`.

## D099 : lot K11, DERR (Validé le 17 nov. 2026 : 1283 tests réussis ; `derr.bas` dans WinAPE : 0, « ABSENT . not found », ERR 32 DERR 146)
- **Référence (SugarBox, BASIC 1.1, 17 nov. 2026) :** `PRINT DERR` au départ : 0 ; après `OPENIN "ABSENT"` avec ON ERROR GOTO : AMSDOS écrit « ABSENT . not found », puis ERR = 32 et DERR = 146.
- **Conception :** quand une opération sur fichier échoue, le firmware (AMSDOS) rend son code d'erreur dans A ; rt_file_fail (module FileCommon) le range dans rt_derr avant de traiter l'erreur. DERR (sans parenthèses) lit cet octet : 0 tant qu'aucune opération n'a échoué. DERR garde la dernière erreur (il n'est pas remis à 0 par une opération réussie).
- **À savoir :** DERR n'existe que dans le BASIC 1.1 (664, 6128) ; ici il marche sur tous les CPC, puisqu'il lit seulement le code rendu par AMSDOS.
- **Tests :** MiniZ80 rend 146 (&92) quand CAS IN OPEN ne trouve pas le fichier. ArrayTests.cs : DERR remplacé par MASK.
- **Fichiers :** Parser.cs, RuntimeLibrary.cs (rt_derr), CodeGenerator.cs, MiniZ80.cs, `tests\CpcBasic.Tests\DerrTests.cs` (nouveau). Exemple : `examples\derr.bas`.

## D100 : lot K12, imprimante : PRINT #8, WRITE #8, WIDTH, POS(#8) (Validé le 17 nov. 2026 : 1290 tests réussis ; `imprime.bas` dans WinAPE : fichier de l'imprimante identique octet pour octet au vrai BASIC)
- **Référence (vrai BASIC dans WinAPE, 17 nov. 2026) :** WinAPE, Settings, onglet Other, cadre Printer : « File », fichier `essais\imprimante.txt` (WinAPE l'écrit en quittant, et ajoute à la fin à chaque essai). Le programme de référence a été mis sur une disquette texte (`essais\imprim.dsk`, LOAD"IMPRIM") pour éviter de le recopier. Résultat : « BONJOURA B » (« ; » garde la position, TAB compte depuis le début de la ligne), WIDTH 20 coupe après 20 caractères (CR LF), POS(#8) vaut 1 en début de ligne, WRITE #8 : "X",5 ; WIDTH 255 : pas de retour automatique. Chaque ligne finit par CR LF.
- **Conception :** comme PRINT #9 (D084), pendant PRINT #8 et WRITE #8, quatre entrées du firmware sont détournées (module Printer) : TXT OUTPUT envoie par MC PRINT CHAR (&BD2B) en attendant que l'imprimante prenne le caractère (sans imprimante, le programme attend, comme le vrai BASIC) ; avant un caractère visible, si la ligne a déjà WIDTH caractères, CR LF est envoyé ; TXT GET CURSOR donne la colonne (POS(#8)), TXT GET WINDOW la largeur. WIDTH : 1 à 255, 132 au départ.
- **Limites :** #8 seulement avec PRINT, WRITE et POS, écrit en nombre (un numéro de flux calculé qui vaut 8 n'est pas géré).
- **Tests :** MiniZ80 a une imprimante (Printed). FileTests.cs et WindowTests.cs : PRINT #8 n'est plus refusé (remplacé par INPUT #8 et CLS #8).
- **Fichiers :** Parser.cs, RuntimeLibrary.cs (module Printer), CodeGenerator.cs, MiniZ80.cs, `tests\CpcBasic.Tests\PrinterTests.cs` (nouveau). Exemple : `examples\imprime.bas`.
- **Lot K terminé** (K1 à K12, D088 à D100).

## D101 : réglage F11 « Texte sur la disquette » (idée de l'utilisateur ; Validé le 17 nov. 2026 : 1291 tests réussis ; `fonctfn.dsk` : LOAD"FONCTFN.TXT" puis RUN dans WinAPE donne le même écran que le programme compilé)
- **Demande :** pouvoir lancer le même programme avec le vrai BASIC, sans le recopier dans l'émulateur (le copier-coller de WinAPE abîme les chiffres). Idée de l'utilisateur : une case « disquette virtuelle (on/off) » ; nom retenu : « Texte sur la disquette ».
- **Réglage de l'éditeur** (pas un mot du BASIC) : bouton « Texte sur la disquette : oui / non (F11) », à côté de F7, F8 et F9, mémorisé pour tous les programmes (EditorSettings.TextOnDisk), affiché dans la barre d'état. F10 n'est pas pris car Windows s'en sert pour les menus.
- **Effet :** quand il est sur « oui », F5 ajoute NOM.TXT sur la disquette : le texte du programme, sans en-tête AMSDOS (fichier ASCII), lignes en CR LF, caractères hors ASCII remplacés par « ? », &1A à la fin (DiskImage.AddTextFile). Dans l'émulateur : `LOAD"NOM.TXT"` puis `RUN` lance le vrai BASIC ; `RUN"NOM"` lance le programme compilé. Si le texte ne tient pas (plus de 16 Ko), la compilation réussit quand même, avec un message.
- **Fichiers :** DiskImage.cs (AddTextFile), EditorSettings.cs, MainWindow.axaml, MainWindow.axaml.cs, DiskImageTests.cs.

## D102 : lot L1, FRAME, CURSOR, COPYCHR$ (BASIC 1.1) (Validé le 17 nov. 2026 : 1301 tests réussis ; `basic11.bas` compilé dans WinAPE identique au vrai BASIC, curseur compris)
- **Lot L : les mots du BASIC 1.1 (664, 6128).** Deux familles : ceux dont la routine du firmware existe aussi sur le 464 (FRAME, CURSOR, COPYCHR$, DEC$ : marchent sur tous les CPC) ; ceux qui utilisent des routines absentes du 464 (FILL, MASK, GRAPHICS PEN / PAPER) : que faire sur un 464, à décider plus tard avec l'utilisateur.
- **Référence (vrai BASIC 1.1 dans WinAPE, 17 nov. 2026, disquette texte `essais\refl1.dsk`) :** après PRINT "ABC", LOCATE 2,1 : COPYCHR$(#0) = « B » (66) ; LOCATE 5,1 : un espace (32) ; 50 FRAME : 308 tops de TIME ; CURSOR 1 : curseur visible, CURSOR 0 : caché ; DEC$(3.14159,"##.##") = « 3.14 » avec un espace devant, DEC$(-5,"+###") = « -5 » avec deux espaces devant, DEC$(1234.5,"$$#,###.##") = « $1,234.50 » (pour L2).
- **FRAME :** MC WAIT FLYBACK (&BD19).
- **CURSOR système [, utilisateur] :** 0 ou 1 (vérifié à la compilation s'il est écrit en nombre ; une valeur calculée : tout ce qui n'est pas 0 vaut 1). Système : TXT CUR ON / OFF (&BB81 / &BB84) ; utilisateur : TXT CUR ENABLE / DISABLE (&BB7B / &BB7E). L'écriture « CURSOR ,1 » (sans la première valeur) n'est pas encore acceptée.
- **COPYCHR$(#n) :** TXT RD CHAR (&BB60) dans la fenêtre n, puis retour à la fenêtre d'avant ; caractère non reconnu : texte vide (choix, pas encore vérifié sur le vrai BASIC).
- **Fichiers :** Parser.cs, CodeGenerator.cs (EmitSwitch), MiniZ80.cs (FirmwareCalls), `tests\CpcBasic.Tests\Basic11Tests.cs` (nouveau), ArrayTests.cs (FRAME et CURSOR remplacés par FILL et GRAPHICS). Exemple : `examples\basic11.bas`.

## D103 : lot L2, DEC$ (Validé le 17 nov. 2026 : 1320 tests réussis ; `decimal.bas` dans WinAPE identique au vrai BASIC)
- **Référence (vrai BASIC 1.1 dans WinAPE, 17 nov. 2026, `essais\refl1.dsk` et `essais\refl2.dsk`) :** `DEC$(3.14159,"##.##")` = « 3.14 » avec un espace devant ; `DEC$(-5,"+###")` = « -5 » avec deux espaces devant ; `DEC$(1234.5,"$$#,###.##")` = « $1,234.50 » avec un espace devant (format de 10 caractères) ; `DEC$(123456,"##")` = « %123456 » ; `F$="#.#":DEC$(2.25,F$)` = « 2.3 » ; formats « A## », « ##A », « ! » et "" : erreur 5 (Improper argument).
- **Conception :** DEC$ appelle la routine de PRINT USING (D074) pendant que TXT OUTPUT, TXT GET CURSOR, TXT VALIDATE et TXT GET WINDOW sont détournés (module Dec) : les caractères vont dans une mémoire de 260 octets, au début d'une ligne de 255 colonnes (jamais de passage à la ligne), puis deviennent une fiche de texte temporaire (rt_sf_mem).
- **Format :** un seul emplacement de nombre, sans rien autour. Écrit entre guillemets : vérifié à la compilation (message DecFormat, Parser.DecField) ; dans une variable ou un calcul : lu pendant l'exécution par UsingDyn (D098, module DecDyn), « Improper argument » s'il ne convient pas.
- **Tests :** pour 9 formats, DEC$ avec le format écrit, DEC$ avec le format dans F$ et PRINT USING donnent exactement le même texte.
- **Fichiers :** Parser.cs (DecField), Diagnostic.cs (DecFormat), RuntimeLibrary.cs (modules Dec et DecDyn), CodeGenerator.cs, `tests\CpcBasic.Tests\DecTests.cs` (nouveau). Exemple : `examples\decimal.bas`.

## D104 : lot L3, ORIGIN avec fenêtre graphique, PEN transparent (Validé le 17 nov. 2026 : 1328 tests réussis ; `fenetreg.bas` dans WinAPE identique au vrai BASIC)
- **Référence (vrai BASIC 1.1 dans WinAPE, 17 nov. 2026, `essais\refl3.dsk`) :** `ORIGIN 0,0,100,300,300,100` puis `CLG 2` : seule la fenêtre est effacée ; `DRAW 639,399` est coupé aux bords de la fenêtre, mais XPOS;YPOS = 639 399 ; après `ORIGIN 320,200` (2 valeurs), la fenêtre reste en place (le trait parti de x = 320 n'apparaît pas) ; `PEN 3,1` : texte écrit sans fond, par-dessus le texte déjà là ; `PEN 3,0` : de nouveau opaque.
- **ORIGIN x, y [, gauche, droite, haut, bas] :** 2 ou 6 valeurs (3 à 5 : refusé). GRA SET ORIGIN, puis GRA WIN WIDTH (&BBCF, DE = gauche, HL = droite) et GRA WIN HEIGHT (&BBD2, DE = haut, HL = bas). Les bords sont en coordonnées absolues.
- **PEN stylo [, mode] :** mode 0 opaque, 1 transparent : TXT SET BACK (&BB9F). Ces routines existent aussi sur le 464 (pas « BASIC 1.1 seulement »).
- **Tests :** MiniZ80 connaît GRA SET ORIGIN et TXT SET PEN (rien à simuler) et note GRA WIN WIDTH / HEIGHT et TXT SET BACK (RegisterCalls).
- **Fichiers :** Parser.cs, CodeGenerator.cs, MiniZ80.cs, `tests\CpcBasic.Tests\GraphicsWindowTests.cs` (nouveau). Exemple : `examples\fenetreg.bas`.

## D105 : lot L4 (FILL, MASK, GRAPHICS PEN / PAPER) : nos propres routines (décision de l'utilisateur, 17 nov. 2026 ; pas encore codé)
- **Constat :** GRAPHICS PAPER (GRA SET PAPER) et GRAPHICS PEN stylo (GRA SET PEN) ont leur routine sur le 464 ; le mode transparent de GRAPHICS PEN (GRA SET BACK &BD46), MASK (GRA SET LINE MASK &BD4C, GRA SET FIRST &BD4F) et FILL (GRA FILL &BD52) n'existent que sur le 664 / 6128. Sur un vrai 464, ces mots donnent « Syntax error ».
- **Choix proposés :** A (ne rien faire sur un 464), B (arrêt avec un message sur un 464), C (écrire nos propres routines Z80, qui marchent sur tous les CPC).
- **Décision de l'utilisateur : C.** Il avait déjà prévu d'écrire nos propres routines : elles serviront aussi plus tard à PAINT et aux commandes de dessin nouvelles (CIRCLE, POLYGON…, D048, D052 : très rapides, écriture directe dans l'écran en coupant la ROM haute).
- **À faire au début de L4 :** relever une référence dans WinAPE (6128) avec une disquette texte (outil de Claude ou réglage F11) : FILL d'une zone fermée, d'une zone ouverte, avec la fenêtre graphique ; MASK avec plusieurs motifs et GRAPHICS PEN stylo, 1 (transparent) ; puis découper L4 en étapes (GRAPHICS PEN / PAPER d'abord, puis MASK et le mode transparent, puis FILL).

## D106 : lot L4a, GRAPHICS PEN et GRAPHICS PAPER (Validé le 17 nov. 2026 : 1334 tests réussis ; `grapen.bas` compilé et `GRAPEN.TXT` lancé par le vrai BASIC dans WinAPE : mêmes dessins)
- **GRAPHICS PEN stylo** (0 à 15) : GRA SET PEN (&BBDE) ; **GRAPHICS PAPER fond** (0 à 15) : GRA SET PAPER (&BBE4), utilisé par CLG. Ces routines existent aussi sur le 464. Le mode transparent (`GRAPHICS PEN stylo, 1`) est refusé pour l'instant : il viendra avec MASK (L4c, notre propre tracé de ligne, D105).
- **Référence (vrai BASIC dans WinAPE, `essais\refl4.dsk`, 17 nov. 2026)** pour la suite du lot : FILL ne déplace pas le curseur graphique (XPOS;YPOS restent 200 200) ; FILL s'arrête seulement sur l'encre du stylo graphique et sur l'encre de remplissage, et recouvre toutes les autres (fond, bordure d'une autre couleur, fond du texte) ; le firmware remplit par bandes verticales (l'utilisateur trouve sa façon de faire étrange, surtout en diagonale).
- **À savoir :** TEST d'un point hors de la fenêtre graphique rend l'encre de GRAPHICS PAPER (firmware ; constaté avec grapen.bas : TEST(200,200) = 0).
- **Tests :** MiniZ80 connaît GRA CLEAR WINDOW (rien à simuler) et note GRA SET PAPER. ArrayTests.cs : GRAPHICS remplacé par MASK.
- **Fichiers :** Parser.cs, CodeGenerator.cs, MiniZ80.cs, `tests\CpcBasic.Tests\GraphicsPenTests.cs` (nouveau). Exemple : `examples\grapen.bas`.

## D107 : PAINT encre, commande nouvelle (idée de l'utilisateur, 17 nov. 2026 ; pas encore codé, à faire avec FILL en L4b)
- **Demande de l'utilisateur** (il n'aime pas la façon de remplir de FILL) : `PAINT encre` lit l'encre du point sous le curseur graphique et ne colorie, avec l'encre donnée, que les points de cette même encre qui se touchent ; toute autre encre arrête la peinture.
- **Règles proposées et acceptées :** passage seulement par le haut, le bas, la gauche et la droite, jamais par un coin (un trait en diagonale ferme une zone) ; rien à faire si l'encre sous le curseur est déjà la bonne ; limité à la fenêtre graphique ; le curseur graphique ne bouge pas.
- **FILL** garde le comportement du vrai BASIC (s'arrête sur le stylo graphique et l'encre de remplissage) pour la compatibilité, mais avec notre propre routine (D105) ; FILL et PAINT partageront le même code.
- **Mot réservé nouveau :** PAINT (il était déjà prévu et coloré dans l'éditeur) ; à ajouter à la section 13 de COMPATIBILITE-LOCOMOTIVE.md quand il sera fait.

## D108 : lot L4b, FILL et PAINT, première version (Validé le 17 nov. 2026 : 1344 tests réussis ; `remplir.bas` dans WinAPE : PAINT ne remplit que le triangle du haut, FILL tout le carré)
- **Routine commune** (module Fill, D105, D107) : remplissage par lignes avec une pile dans la mémoire libre (entre rt_arr_heap et rt_str_ptr ; FILL et PAINT activent donc la mémoire libre) ; passage seulement par le haut, le bas, la gauche et la droite ; un pixel à la fois (4, 2 ou 1 unité en largeur selon le MODE, 2 en hauteur) ; limité à la fenêtre graphique (GRA GET W WIDTH / HEIGHT, moins l'origine) ; le curseur et le stylo graphiques sont remis comme avant. Pile pleine : des morceaux restent sans couleur (pas d'erreur).
- **PAINT encre** (commande nouvelle, D107) : seulement les points de l'encre trouvée sous le curseur. **FILL encre** : tous les points sauf ceux de l'encre du stylo graphique et de l'encre du remplissage (règle du vrai BASIC, D106).
- **Lenteur constatée par l'utilisateur :** cette version lit et colorie chaque point par le firmware (GRA TEST, GRA PLOT). Étape suivante proposée : lire et écrire directement dans la mémoire de l'écran (comme BOX, D048), en coupant la ROM haute.
- **Mot réservé nouveau :** PAINT (section 13 de COMPATIBILITE-LOCOMOTIVE.md).
- **Tests :** MiniZ80 simule GRA GET PEN, GRA GET ORIGIN, GRA GET W WIDTH / HEIGHT. Correction pendant l'étape : la fin de la routine appelait le firmware par un saut (JP), que le simulateur ne suit pas ; remplacé par CALL puis RET. ArrayTests.cs : FILL remplacé par MASK.
- **Fichiers :** RuntimeLibrary.cs (module Fill), Parser.cs, CodeGenerator.cs (usesHeap), MiniZ80.cs, `tests\CpcBasic.Tests\FillTests.cs` (nouveau). Exemple : `examples\remplir.bas`.

## D109 : FILL et PAINT accélérés (Validé le 17 nov. 2026 : 1346 tests réussis ; `essais\chrono.bas` dans WinAPE : FILL 569 et PAINT 565 tops de TIME, contre 1188 pour le FILL du 6128)
- **Mesure** (`essais\chrono.bas` : rectangle de 280 × 140 pixels en MODE 1, temps en tops de TIME) : FILL du 6128 (vrai BASIC, fichier .TXT du réglage F11) : 1188 ; notre première version (D108, GRA TEST et GRA PLOT pour chaque point) : 6474 et 6094 ; lecture et écriture directes de l'écran, point par point : 2271 et 1957 ; avec les octets entiers : **569 et 565**, deux fois plus rapide que le firmware.
- **Conception :** l'écran est lu et écrit directement (ROM haute coupée) ; encres codées une fois (SCR INK ENCODE) ; SCR DOT POSITION seulement au début de chaque morceau pris dans la pile ; chaque morceau est d'abord colorié d'un coup vers la droite, puis la ligne du dessus et celle du dessous sont parcourues sur la même largeur (deux états : on cherche un morceau, on est dedans) ; l'adresse de la ligne voisine est calculée sur place (comme SCR NEXT LINE / SCR PREV LINE : lignes de 80 octets, défilement de l'écran compris).
- **Idée de l'utilisateur, appliquée :** trouver les bords puis remplir la ligne par octets entiers. Un octet tout entier de l'encre à remplacer est écrit d'un coup (2, 4 ou 8 points selon le MODE) ; pendant la recherche, un octet déjà colorié, ou entièrement à colorier, est passé d'un coup. Pour FILL, seulement si l'encre trouvée au départ n'est ni celle du stylo graphique ni celle du remplissage (rt_pf_fast). Les bords se font point par point.
- **Tests :** MiniZ80 simule la mémoire de l'écran (disposition standard en &C000) : SCR DOT POSITION, SCR INK ENCODE, SCR NEXT / PREV LINE ; PLOT et TEST y écrivent et y lisent ; FillTests.cs compte les points dans la mémoire de l'écran, et vérifie aussi les MODE 0 et 2. Instructions ajoutées : AND C, DEC D.
- **Fichiers :** RuntimeLibrary.cs (module Fill réécrit, EmitFillMatch, EmitFillByte, EmitFillRight, EmitNextLine, EmitPrevLine), Z80Emitter.cs, MiniZ80.cs, FillTests.cs. Programme de mesure : `essais\chrono.bas`.

## D110 : lot L4c, MASK et GRAPHICS PEN stylo, mode (Validé le 17 nov. 2026 : 1352 tests réussis ; `masque.bas` dans WinAPE : les sept lignes de chiffres identiques au vrai BASIC). **Lot L terminé.**
- **Référence (vrai BASIC 1.1 dans WinAPE, `essais\refl5.dsk`, 17 nov. 2026)** : le programme lit chaque point avec TEST (1 trait, 0 fond opaque, 2 fond laissé). Le motif se lit du bit de gauche au bit de droite, un bit par point tracé, et continue d'un DRAW à l'autre ; fond opaque (encre de GRAPHICS PAPER) par défaut ; GRAPHICS PEN stylo, 1 : fond transparent ; MASK …,0 : le premier point de chaque ligne n'est pas tracé et ne prend pas de bit (le point commun de deux lignes n'est pas retracé) ; MASK …,1 (par défaut) : il est tracé et prend le bit suivant ; les lignes verticales suivent le même motif ; MASK ne touche pas PLOT (choix : PLOT n'est pas masqué).
- **Conception :** pas de tracé de ligne à nous : pendant DRAW et DRAWR, l'entrée détournable SCR WRITE (&BDE8, présente sur tous les CPC) est détournée vers notre routine (module Mask), qui applique le motif, le fond et la règle du premier point. Les points sont donc exactement ceux du firmware. Détour posé au premier MASK ou GRAPHICS PEN stylo, mode ; retiré avant RUN "nom".
- **Erreur corrigée pendant l'étape (constatée dans WinAPE) :** pour une ligne horizontale, le firmware écrit un octet entier d'un seul appel (plusieurs points dans le masque C) ; la routine découpe maintenant chaque appel point par point, de gauche à droite (masque du point de gauche et nombre de points par octet selon le MODE), puis écrit les points du stylo, puis ceux du fond. Le simulateur MiniZ80 regroupe lui aussi les points d'un même octet, comme le firmware, pour que les tests voient ce cas.
- **Vérifié sur 464 (18 nov. 2026, Caprice32 réglé en 464 avec lecteur de disquette) :** `masque.bas` compilé donne les sept lignes de chiffres et « PLOT : 1 » exactement comme le 6128 ; le firmware du 464 passe bien par SCR WRITE. Le vrai BASIC 1.0 du 464 ne connaît pas MASK (Syntax error) : le programme compilé l'apporte.
- **Tests :** MiniZ80 trace les lignes point par point (Bresenham) par &BDE8 quand il est détourné ; CLG remplit l'écran de l'encre de GRAPHICS PAPER ; GRA GET PAPER ; instruction SRL E. ArrayTests.cs : MASK remplacé par RENUM, AUTO et NEW ; GraphicsPenTests.cs : GRAPHICS PEN 1,1 accepté.
- **Fichiers :** RuntimeLibrary.cs (module Mask), Parser.cs, CodeGenerator.cs (usesMask, EmitDraw), MiniZ80.cs, `tests\CpcBasic.Tests\MaskTests.cs` (nouveau). Exemple : `examples\masque.bas`.

## D111 : plus de réglage « Machine (F7) » : un programme compilé marche sur tous les CPC (décision de l'utilisateur, 17 nov. 2026 ; remplace la partie « machine cible » de D066)
- **Question de l'utilisateur :** puisque le langage est compilé, pourquoi choisir la machine ? Le programme peut être compatible avec tous les CPC, et un message peut prévenir quand une instruction ne l'est pas.
- **Constat :** F7 ne servait plus qu'aux nombres à virgule (adresses des routines de calcul de la ROM, différentes sur le 464 et le 664/6128). « Tous les CPC » les trouve au démarrage (KL PROBE ROM), pour quelques dizaines d'octets ; choisir une machine faisait gagner ces octets, mais le programme se bloquait sur l'autre. Depuis le lot L (nos propres routines pour FILL, PAINT, MASK), toutes les commandes marchent sur tous les CPC.
- **Décision :** le bouton et la touche F7 sont retirés de l'éditeur ; l'éditeur compile toujours pour « tous les CPC ». Le compilateur garde l'option (CompilerOptions.Target) pour les tests et pour plus tard. Le message de compilation réussie le rappelle : « Machine : tous les CPC (le programme reconnaît la machine au démarrage) ».
- **Idée notée (IDEES.md) :** messages d'incompatibilité, le jour où une commande ne pourra pas marcher partout (mémoire en plus du 6128, CPC Plus, GX4000…).
- **Fichiers :** MainWindow.axaml, MainWindow.axaml.cs, EditorSettings.cs (la ligne « Machine » d'un ancien reglages.json est simplement ignorée).

## D112 : barre de menus Windows à la place des gros boutons (demande de l'utilisateur, 17 nov. 2026 ; première étape du remodelage de l'éditeur)
- **Demande :** l'éditeur ne faisait pas assez « pro » : boutons trop gros et peu jolis ; un menu à la Windows est préféré.
- **Décision :** une barre de menus remplace la rangée de boutons. **Fichier** (Ouvrir Ctrl+O, Enregistrer Ctrl+S, Quitter), **Compiler** (F5), **Options** (réglages F8, F9, F11, contraste élevé Ctrl+H, taille du texte), **Aide** (Raccourcis clavier, nouvelle touche **F1**, et À propos, affichés dans le panneau des messages pour rester lisibles par un lecteur d'écran). Alt ouvre les menus, les lettres soulignées les choisissent ; tous les raccourcis restent les mêmes.
- **Coches :** F9, F11 et Ctrl+H sont des « oui / non », cochés quand ils sont actifs. **F8 n'a pas de coche** (idée de l'utilisateur) : une coche devant « à virgule » était difficile à comprendre ; le texte dit l'état actuel : « Variables sans signe (A, X...) : entières » ou « : à virgule ».
- **Suite prévue :** barre d'état lisible (ligne, colonne, mode), panneau des messages sans phrase coupée.
- **Fichiers :** MainWindow.axaml, MainWindow.axaml.cs.

## D113 : barre d'état lisible, panneau des messages réglable, lignes longues à la ligne comme le CPC (17 nov. 2026 ; deuxième étape du remodelage)
- **Barre d'état :** claire comme sous Windows (noir sur gris clair ; blanc sur noir en contraste élevé). Elle ne montre plus que « Ligne, colonne » à gauche et « Largeur visible : N colonnes » à droite ; les réglages se lisent dans le menu Options, plus rien n'est coupé.
- **Panneau des messages :** une barre grise entre l'éditeur et les messages se tire à la souris pour agrandir ou réduire le panneau ; les longues phrases passent à la ligne ; un peu moins d'espace autour de chaque message. Le message sous la souris est blanc sur gris foncé (le thème clair le mettait en noir sur noir, illisible).
- **Lignes longues (demande de l'utilisateur) :** une ligne plus large que la fenêtre continue sur la ligne suivante de l'écran, comme sur le CPC ; le numéro de l'éditeur n'est affiché qu'une fois ; pas de défilement horizontal. Seul l'affichage change. Actif par défaut ; menu Options « Lignes longues sur plusieurs lignes, comme le CPC » (coche, réglage WordWrap enregistré).
- **Fichiers :** MainWindow.axaml, MainWindow.axaml.cs, EditorSettings.cs.

## D114 : icônes ✓ et ✗ devant les messages (17 nov. 2026 ; troisième étape du remodelage)
- **Décision :** ✗ rouge devant chaque erreur (compilation, fichier illisible, enregistrement impossible, nom trop long, .dsk ouvert dans l'émulateur) ; ✓ vert devant « Compilation réussie » ; rien devant les simples informations (aide F1, fichier ouvert, réglages).
- **Accessibilité :** un lecteur d'écran lit « Erreur : » ou « Réussi : » à la place de l'icône (texte du message).
- **Fichiers :** MainWindow.axaml.cs (MessageKind, ShowError).

## D115 : commandes du mode direct dans l'éditeur, première étape : NEW, EDIT et les explications (17 nov. 2026 ; suite de D077) (validé par l'utilisateur dans l'éditeur)
- **Règle :** la commande se tape seule sur une ligne, sans numéro, puis Entrée. La ligne de la commande disparaît du programme ; le résultat s'affiche dans les messages. Une ligne qui commence par un numéro (10 NEW) reste une ligne du programme ; le mot doit être entier (NEWS, LIST$, NEW2 ne sont pas des commandes).
- **NEW :** efface le programme ; s'il a été modifié depuis le dernier Ouvrir / Enregistrer, la même question qu'à la fermeture (Enregistrer, Ne pas enregistrer, Annuler). La ligne « NEW » tapée ne compte pas comme une modification (texte comparé à celui du dernier enregistrement).
- **EDIT n :** curseur à la fin de la ligne n ; sans numéro ou ligne absente : message ✗ (« Line does not exist », comme le CPC).
- **Explications :** RUN, CONT, CLEAR, MEMORY, LOAD, SAVE, MERGE, CAT, CHAIN (et CHAIN MERGE) et les commandes AMSDOS |DISC, |TAPE, |A, |B, |DIR, |ERA, |REN, |USER affichent ce qu'elles deviennent ici (F5, Ctrl+O, Ctrl+S...) et dans un programme compilé.
- **Déjà reconnues, pour les étapes suivantes :** AUTO, LIST, RENUM, DELETE.
- **Fichiers :** src\CpcBasic.Core\Editing\DirectCommands.cs (reconnaissance, testable), MainWindow.axaml.cs (Entrée, RunDirectCommandAsync), DirectCommandTests.cs.

## D116 : AUTO dans l'éditeur (17 nov. 2026 ; deuxième étape des commandes de l'éditeur) (validé par l'utilisateur dans l'éditeur)
- **Comme le CPC :** AUTO [début][,pas], 10 et 10 si absents (AUTO 100,5 ; AUTO ,5). L'éditeur écrit « 10 », puis le numéro suivant à chaque Entrée.
- **Arrêt :** ESC, ou Entrée sur une ligne sans rien après le numéro (le numéro seul disparaît, la ligne n'est pas créée, comme sur le CPC). Ouvrir un programme ou NEW arrête aussi AUTO.
- **Écart voulu :** si le numéro suivant existe déjà, AUTO s'arrête avec un message (« la ligne 30 existe déjà ; EDIT 30 pour y aller ») au lieu d'afficher l'ancienne ligne comme le CPC : l'éditeur ne crée jamais deux lignes du même numéro. Plus de 65535 : AUTO s'arrête.
- **Valeurs refusées (message ✗) :** 0, plus de 65535, pas nul, nombre négatif, autre chose qu'un nombre, plus de deux valeurs.
- **Correction pendant l'essai :** la première version fermait l'éditeur à la deuxième ligne (le curseur avait déjà suivi le texte ajouté, la ligne cherchée n'existait pas). Corrigé ; de plus, un problème imprévu dans une commande de l'éditeur arrête la commande avec un message ✗ au lieu de fermer l'éditeur.
- **Fichiers :** DirectCommands.cs (TryParseAuto), MainWindow.axaml.cs (StartAutoLine, ContinueAuto, StopAuto), DirectCommandTests.cs.

## D117 : RENUM dans l'éditeur (17 nov. 2026) (validé par l'utilisateur dans l'éditeur)
- **Comme le CPC :** RENUM [nouveau][,[ancien][,pas]] : 10, première ligne et 10 si absents (RENUM 100,,5 ; RENUM 1000,30). Les lignes avant « ancien » ne bougent pas.
- **Renvois corrigés :** numéros après GOTO, GOSUB (donc aussi ON ... GOTO/GOSUB 10,20,30, ON ERROR GOTO, ON BREAK GOSUB, EVERY/AFTER ... GOSUB, ON SQ ... GOSUB), THEN, ELSE, RESTORE, RESUME, RUN. ON ERROR GOTO 0 reste 0. Textes entre guillemets, REM, ' et DATA jamais touchés (le découpage passe par le Lexer du compilateur).
- **Renvoi vers une ligne absente :** gardé tel quel et signalé (✗ « Undefined line »), comme le CPC.
- **Refus (message ✗, rien ne change) :** deux lignes du même numéro, dernier numéro au-delà de 65535, nouvelle numérotation qui rejoindrait les lignes gardées (Improper argument), aucune ligne à partir de « ancien », aucune ligne numérotée.
- **Ctrl+Z** annule RENUM en entier (remplacement par le document).
- **Étiquettes (D077) :** pas encore dans le langage ; l'erreur prévue viendra avec elles.
- **Fichiers :** src\CpcBasic.Core\Editing\Renumber.cs, MainWindow.axaml.cs, RenumberTests.cs.

## D118 : numéro tapé seul, ou numéro déjà pris : confirmation (demande de l'utilisateur, 17 nov. 2026) (validé)
- **Constat de l'utilisateur :** taper « 100 » + Entrée pour effacer la ligne 100, comme sur le CPC, ne faisait rien : la ligne 100 restait (piège).
- **Numéro seul + Entrée :** si la ligne existe, question « Effacer la ligne 100 ? » avec son contenu (Oui : O ; Non : N, Entrée ou Échap). Le numéro tapé seul disparaît dans les deux cas. Si aucune autre ligne n'a ce numéro : le numéro seul est retiré, rien d'autre.
- **Ligne tapée avec un numéro déjà pris ailleurs :** question « Remplacer l'ancienne ligne ? » avec les deux lignes ; Non : les deux restent, message ✗ (changer le numéro de l'une avant de compiler).
- **Prudence :** le bouton choisi d'office est « Non » : une Entrée de trop n'efface rien. Ctrl+Z remet la ligne (un seul Ctrl+Z).
- **Pas pendant AUTO** (AUTO s'arrête déjà devant un numéro existant, D116).
- **Fichiers :** ConfirmDialog.cs (nouveau), DirectCommands.cs (LeadingNumber, FindLine avec exceptLine), MainWindow.axaml.cs (HandleExistingNumberAsync), DirectCommandTests.cs.

## D119 : un problème imprévu ne ferme plus l'éditeur (17 nov. 2026)
- **Constat :** l'éditeur s'est fermé d'un coup en ouvrant un fichier, après des effacements de lignes (D118) et une ligne en double gardée.
- **Protection générale :** toute erreur imprévue est rattrapée (App.axaml.cs, Dispatcher.UIThread.UnhandledException) : l'éditeur reste ouvert, le programme n'est pas perdu, un message ✗ « Problème interne » s'affiche, et le détail est ajouté à %APPDATA%\CpcBasicIde\erreurs.txt (à envoyer pour corriger).
- **Cause trouvée grâce à elle :** « No undo group should be open at this point » : le groupe d'annulation de D118 pouvait rester ouvert, et l'ouverture suivante (Editor.Text = ...) le refusait. Corrigé : groupe par RunUpdate (toujours refermé) et document neuf à chaque Ouvrir / NEW. Refait par l'utilisateur : ne se reproduit plus.
- **Fichiers :** App.axaml.cs, MainWindow.axaml.cs (ReportInternalError, SetText).

## D120 : DELETE dans l'éditeur (17 nov. 2026) (validé par l'utilisateur)
- **Comme le CPC :** DELETE 30 ; DELETE 10-50 ; DELETE -50 (du début à 50) ; DELETE 100- (de 100 à la fin) ; DELETE seul : tout le programme.
- **Toujours une question** (D077) : nombre de lignes, première et dernière ; bouton choisi d'office « Ne rien effacer ». Un seul Ctrl+Z remet tout.
- **Rien dans la plage :** message ✗, rien n'est effacé (DELETE 999 : « la ligne 999 n'existe pas »). Plage à l'envers (50-10), 0, autre chose que des numéros : message ✗ avec les formes acceptées.
- **Étiquettes (D077) :** l'avertissement prévu viendra avec elles.
- **Fichiers :** DirectCommands.cs (TryParseRange, LinesInRange, réutilisables pour LIST), MainWindow.axaml.cs (DeleteLinesAsync), DirectCommandTests.cs.

## D121 : menu Édition (idée de l'utilisateur, 17 nov. 2026) (validé)
- Annuler (Ctrl+Z), Rétablir (Ctrl+Y), Couper, Copier, Coller, Tout sélectionner ; entre Fichier et Compiler. Après le choix, le clavier revient dans l'éditeur.
- **Fichiers :** MainWindow.axaml, MainWindow.axaml.cs.

## D122 : LIST dans l'éditeur (choix 3 de l'utilisateur, 18 nov. 2026) (validé par l'utilisateur) ; fin des commandes de D077
- **Ranger :** les lignes passent par ordre de numéro ; les lignes vides disparaissent ; une ligne sans numéro est gardée à la fin, avec un message ✗.
- **Écrire comme le CPC :** mots du langage en majuscules (Parser.IsKeyword), « ? » écrit PRINT, &ff écrit &FF, un seul espace après le numéro, numéro sans zéro devant (010 -> 10). Gardés tels quels : noms de variables, textes entre guillemets, la suite de REM, ' et DATA, les autres espaces. FNdouble : FN en majuscules, le nom gardé.
- **LIST plage** (LIST 30, 10-50, -50, 100-) : même rangement, puis curseur sur la première ligne de la plage (message ✗ si aucune).
- **Ctrl+Z** annule LIST.
- **Numéro en double :** LIST et RENUM refusent ; le message dit « plusieurs lignes ont le numéro 10 » et liste les lignes de l'éditeur concernées (clic ou Entrée : le curseur y va). Constat de l'essai : essais\chrono.bas contenait le programme deux fois.
- **Vérifié sur le vrai CPC (WinAPE, 18 nov. 2026) :** `5 a=5: A=9` est relisté tel quel : le Locomotive garde l'écriture des noms de variables et les espaces tapés, comme notre LIST ; a et A sont la même variable (RUN : 9 et 9).
- **Fichiers :** src\CpcBasic.Core\Editing\Lister.cs, Parser.cs (IsKeyword), Renumber.cs (Duplicate), MainWindow.axaml.cs (ListProgram, ShowDuplicateError), ListerTests.cs.

## D123 : CURSOR ,1 et MASK ,1 sans première valeur (18 nov. 2026) (validé par l'utilisateur)
- **Vrai BASIC 1.1 (SugarBox, 18 nov. 2026) :** `MASK ,1` et `CURSOR ,1` sont acceptés (Ready) ; `MASK` seul et `CURSOR` seul donnent « Operand missing ».
- **Choix :** sans première valeur, seul le deuxième réglage change. `CURSOR ,n` appelle seulement TXT CUR ENABLE / DISABLE (&BB7B / &BB7E) ; l'interrupteur du système ne bouge pas. `MASK ,n` change seulement le premier point ; le motif garde sa valeur (et sa rotation).
- **Écrit :** deux entrées nouvelles du parser, « CURSOR USER » et « MASK FIRST », lues après la virgule (comme WINDOW SWAP). `CURSOR` et `MASK` seuls restent refusés (nombre de valeurs), `CURSOR ,`, `MASK ,`, `MASK ,2`, `CURSOR ,1,1` aussi.
- **Vérifié dans WinAPE (18 nov. 2026) :** examples\curmask.bas donne les mêmes points que les lignes 60 et 70 de la référence de MASK (D110), et le curseur s'éteint puis s'allume au bon moment.
- **Fichiers :** Parser.cs, CodeGenerator.cs, Basic11Tests.cs, MaskTests.cs, examples\curmask.bas. 1449 tests réussis.

## D124 : @ avec CALL et les commandes RSX (règle de l'utilisateur, 18 nov. 2026) (validé)
- **Constat de l'utilisateur :** sur le 464, le Locomotive exige @ devant un texte (`CALL @a$`, `|NOM,@a$`) ; sur le 6128, `CALL a$` fait la même chose que `CALL @a$`, et de même pour toutes les commandes RSX.
- **Règle (« on fait du propre ») :** pour un texte, @ n'est obligatoire sur aucun CPC : avec ou sans @, c'est l'adresse de la fiche de 3 octets qui part. Pour une variable de nombre, sans @ c'est sa valeur qui part ; avec @, son adresse (à mettre obligatoirement si la routine veut l'adresse).
- **État :** déjà fait ainsi depuis D089 ; ajout de 6 tests qui le vérifient (RsxTests.cs : texte avec et sans @, nombre avec et sans @, avec CALL et avec |NOM). 1455 tests réussis. `@` passe de « Partielle » à « Faite ».

## D125 : MEMORY trop haut ou trop bas : « Memory full » comme le CPC (18 nov. 2026) (validé par l'utilisateur)
- **Vrai BASIC 1.1 (SugarBox, 18 nov. 2026) :** HIMEM vaut 42619 (&A67B) au départ ; `MEMORY &1000` et `MEMORY &8000` : Ready (programme vide) ; `MEMORY HIMEM-1000` puis HIMEM : 31768 ; `MEMORY &A67B` : Ready ; `MEMORY &A67C` et `MEMORY &B000` : Memory full. Le plus haut permis est donc le HIMEM de départ.
- **Choix (accord de l'utilisateur) :** un nombre écrit plus haut que &A67B (`MEMORY &B000`, `MEMORY 50000`, `MEMORY -1`) est une erreur de compilation (message MemoryTooHigh qui rappelle « Memory full »). Une adresse calculée est vérifiée à l'exécution : plus haute que &A67B, ou limite sous la fin du programme compilé (ou sous les tableaux déjà créés) : « Memory full in N », ERR = 7, interceptée par ON ERROR. Remplace la règle de D094 qui ramenait en silence une adresse trop haute à &A67B. Une adresse écrite en nombre n'ajoute toujours aucun octet (D057).
- **Écrit :** Parser.cs (MemoryHighest), Diagnostic.cs (MemoryTooHigh), RuntimeLibrary.cs (module MemoryCheck `rt_mem_check` quand il n'y a ni tableaux ni textes ; module Heap : erreur au lieu de ramener), CodeGenerator.cs (numéro de ligne rangé pour MEMORY calculé ; étiquette rt_heap_start posée aussi pour MemoryCheck), MemoryTests.cs (12 tests ; un ancien test avec MEMORY 50000 passe à 42000).
- **Vérifié dans WinAPE (18 nov. 2026) :** examples\memfull.bas : HIMEM 42619, après MEMORY HIMEM-1000 : 41619, MEMORY &A67C : ERR 7 (Memory full) attrapé par ON ERROR. 1467 tests réussis.
- **Idée notée :** liste des variables A, B et C dans IDEES.md (demande de l'utilisateur).

## D126 : lettres accentuées et version du CPC (français par défaut) (18 nov. 2026) ; partie 1 (compilateur) validée
- **Demande de l'utilisateur :** les accents dépendent de la ROM choisie (menu de choix de la ROM prévu au début, D001). Version **française par défaut** (« le projet vient de moi »), anglaise en deuxième ; d'autres pays plus tard (liste des ROM : https://www.cpcwiki.eu/index.php/ROM_List).
- **Relevé dans les ROM** (OS6128.ROM et OS6128-AZERTY.rom de l'utilisateur, copiées dans essais\roms, jamais publiées : essais\ est exclu de git) : dessins des caractères en &3800. Seuls 7 codes changent : 64 @/à, 92 \\/ç, 94 ↑/^, 123 {/é, 124 |/ù, 125 }/è, 162 ¨/°. Vérifié dans WinAPE, ROM AZERTY : PRINT CHR$(123) affiche é. Pas de ê, â, ô, Ç sur aucun CPC.
- **Pas besoin d'inclure les ROM** (question de l'utilisateur) : l'éditeur ne fait pas tourner de CPC ; il lui suffit d'une petite table écrite par nous (CharacterSet.cs). D016 reste ouvert seulement pour un futur émulateur intégré (autorisation donnée autrefois par Amstrad aux émulateurs : conditions à vérifier).
- **Règles (accord de l'utilisateur) :** un caractère de l'alphabet anglais garde son code (comme une touche du CPC) ; un autre caractère prend le code qui l'affiche sur le CPC choisi, sinon sur un autre CPC (é sur un CPC anglais : 123, qui affiche « { ») ; un caractère absent de tous les CPC est une erreur à la compilation (« le caractère « ê » n'existe pas sur le CPC français, ni sur un autre CPC. Écrivez par exemple « e » »). Les signes communs aux deux ROM ont leur vrai code (£ © § ¶ ¼ ½ ¾ ± ÷ ¬ ¿ ¡, lettres grecques, ☺ ♣ ♦ ♥ ♠ ○ ● □ ■, flèches) au lieu de « ? ».
- **Partie 1 faite :** CharacterSet.cs (nouveau), CompilerOptions (Language, français par défaut), BasicAnalyzer (textes et DATA vérifiés), Diagnostic (UnknownCharacter), CodeGenerator (Encode), DiskImage (NOM.TXT du réglage F11 avec les mêmes codes). Tests : CharacterSetTests.cs (30), CodegenTests et DiskImageTests mis à jour (é donnait « ? »). 1497 tests réussis. Vérifié dans WinAPE (ROM AZERTY) : examples\accents.bas affiche à ù é ° ç è, puis £ ½ © § ♥ ♠ → α π.
- **Partie 2 à faire (éditeur) :** touche F7 et menu Options « Version du CPC : français (AZERTY) / anglais (QWERTY) », gardée dans les réglages ; l'éditeur montre les textes comme le CPC choisi les affiche (en anglais, é tapé devient « { » ; en français, « { » tapé devient é) et convertit les textes quand on change de version.

## D127 : lettres accentuées, partie 2 : éditeur, touche F7 (18 nov. 2026) (validé par l'utilisateur) ; suite de D126
- **F7 et menu Options** « Version du CPC : français (AZERTY) / anglais (QWERTY) », français par défaut, gardé dans reglages.json (CpcVersion), commun à tous les programmes. La barre d'état dit « CPC français » ou « CPC anglais » ; F1 explique F7.
- **L'éditeur montre ce que le CPC affichera** dans les textes entre guillemets : une touche tapée entre guillemets prend le dessin du CPC choisi (français : « { » donne é ; anglais : é donne « { », ^ donne ↑) ; à l'ouverture d'un fichier, même chose ; F7 convertit les textes du programme (chaque caractère garde son code et prend le dessin de l'autre version ; le reste du programme ne change pas ; Ctrl+Z annule ; le curseur ne bouge pas). F5 et le fichier NOM.TXT (F11) utilisent la version choisie.
- **Limites connues :** le texte collé (Ctrl+V) n'est converti qu'au prochain F7 ou à la prochaine ouverture (les codes compilés sont justes de toute façon) ; sur le CPC anglais ^ et ↑ s'affichent pareil, donc un ↑ tapé en anglais redevient ^ en français.
- **Fichiers :** CharacterSet.cs (Shown, ConvertProgram, IsInString), EditorSettings.cs, MainWindow.axaml, MainWindow.axaml.cs, CharacterSetTests.cs (14 tests de plus), LISEZMOI.txt. 1511 tests réussis.
- **Essayé par l'utilisateur (18 nov. 2026) :** examples\accents.bas, F7 deux fois : les textes passent à « Bonjour @ toi ! », « O| est le caf{ ? Il fait 20¨. » (comme le vrai CPC anglais), puis reviennent en français. La remarque « Partielle » de PRINT est levée : **l'inventaire du Locomotive n'a plus de « Partielle »**.

## D128 : POKE et OUT, valeur calculée hors de 0 à 255 : « Improper argument » (18 nov. 2026) (validé par l'utilisateur) ; remplace une règle de D056
- **Trouvé en relisant D056** (question de l'utilisateur sur OUT et INP) : une valeur calculée hors de 0 à 255 écrivait en silence l'octet du bas (D056), ce qui n'était pas noté comme écart.
- **Vrai BASIC 1.1 (WinAPE, 18 nov. 2026) :** `A=256:POKE &9000,A` et `A=-1:POKE &9000,A` : Improper argument.
- **Fait :** POKE et OUT avec une valeur calculée vérifient l'octet du haut ; s'il n'est pas 0 : « Improper argument in N » (ERR = 5, interceptée par ON ERROR), rien n'est écrit. Une valeur écrite en nombre reste vérifiée à la compilation (aucun octet ajouté). Module ByteCheck (`rt_byte_bad`), numéro de ligne rangé pour ces instructions.
- **Vérifié dans WinAPE :** examples\poke256.bas : « POKE 255 : 255 », « POKE 256 : ERREUR 5 (Improper argument) ». Tests : MachineTests.cs (l'ancien test « octet du bas » remplacé par 7 tests). 1517 tests réussis.

## D129 : commandes AMSDOS essayées dans un programme compilé (18 nov. 2026) (validé par l'utilisateur)
- **Choix 4 de l'utilisateur.** Aucune ligne de code à changer : elles passent par le mécanisme général des commandes RSX (D089) ; le nom peut contenir un point (|DISC.IN).
- **examples\amsdos.bas** : |DIR,"*.DAT", |REN, |ERA (et fichier absent : « ABSENT  .DAT not found », le programme continue), |USER,1 puis |USER,0, |TAPE, |DISC, |A. **examples\amsdos2.bas** : variables texte sans @ (|REN,N$,F$) et avec @ (|ERA,@N$), |DRIVE,D$, |USER,U, |TAPE.OUT, |DISC.OUT, |TAPE.IN, |DISC.IN.
- **Méthode :** vrai BASIC 1.1 (LOAD"AMSDOS.TXT" avec le réglage F11) puis programme compilé, sur la même disquette (WinAPE, 18 nov. 2026) : **écrans identiques**. Astuce de l'utilisateur pour les captures : finir par `WHILE INKEY(47):WEND` (attend la barre d'espace).
- **Constat :** « ERA avec @ » s'affiche « ERA avec à » avec la ROM française, et l'éditeur réglé sur « CPC français » l'a montré et enregistré ainsi (D127) : comportement voulu.
- **Pas essayés :** |B (demande une disquette dans le lecteur B, sinon « disc missing ») et |CPM (quitte le BASIC pour CP/M).
- **Tests :** 3 écritures de plus dans RsxTests.cs (Ecritures_acceptees).
- **Version 0.4.0 :** pas maintenant (choix de l'utilisateur : la 0.3.0 vient d'être publiée ; d'abord plus d'essais).

## D130 : CIRCLE (18 nov. 2026) (validé par l'utilisateur) ; première commande nouvelle après l'inventaire
- **Mot vérifié dans WinAPE :** `CIRCLE=5` donne Ready, ce n'est pas un mot du Locomotive. Nouveau mot réservé : **CIRCLE**.
- **Écriture :** `CIRCLE x, y, rayon [, stylo [, remplissage]]`, sur le modèle de BOX (D048, D052) : sans stylo, contour au stylo courant du tracé (GRA GET PEN) ; avec un second stylo, intérieur rempli. Repère natif (origine en haut à gauche, y de 0 à 199). Rayon compté en lignes, de 0 à 1000 (nombre écrit vérifié à la compilation ; calculé hors de 0 à 1000 : « Improper argument in N »). Centre : n'importe quelle valeur sur 16 bits (ce qui sort de l'écran est ignoré).
- **Forme :** demi-largeur de la ligne à la distance d du centre = partie entière de k × racine((r + ½)² − d²). Le « + ½ » (règle habituelle) donne un haut et un bas plats ; sans lui, un point seul dépassait en haut et en bas (premier essai). Contour de chaque ligne de s = min(w(d+1) + 1, w(d)) à w(d), donc raccordé à la ligne voisine ; intérieur entre les deux : aucun point écrit deux fois.
- **Facteur de largeur, choix A de l'utilisateur :** k = 0,5 (MODE 0), 1 (MODE 1), 2 (MODE 2) : rond parfait dans WinAPE (points affichés carrés en MODE 1), presque rond sur un vrai moniteur (calcul : point du MODE 1 environ 8 % plus étroit que haut, donc ×1,08 serait exact). **Remplace** le ×0,6 / ×1,2 / ×2,4 noté au début (REPRISE, D048) : il était mal calculé et donnait des ovales (premier essai dans WinAPE : 432 × 360). L'utilisateur dira si le résultat ne convient pas. Idée notée : réglage ou commande du facteur (IDEES.md).
- **Réalisation :** module Circle (`rt_circle`), sans racine ni multiplication : sommes sur 32 bits de la ligne d = r à d = 0 ; tracé par lignes entières avec `rt_px_hline` (D048) ; ROM haute coupée puis remise. Écrit une fois dans une description à part (essais\circle\routine.py), exécutée et comparée à une référence (504 ronds dans les 3 MODES, centres extrêmes, rayon 0 à 1000), dont le code C# est tiré. Vitesse : environ 31 000 instructions Z80 de calcul pour un rayon de 99, plus le tracé des lignes.
- **Tests :** CircleTests.cs (25 tests : ronds comparés point par point à une référence écrite à part, avec et sans remplissage, ROM haute, écritures acceptées et refusées, Improper argument). 1545 tests réussis.
- **Vérifié dans WinAPE (18 nov. 2026) :** examples\cercles.bas dans les 3 MODES : « très rapide, et rond de rond à l'œil ». (L'encre 14 clignote par défaut : un rond dans ce stylo peut sembler absent sur une capture.)
- **Règle nouvelle pour les programmes d'exemple (demande de l'utilisateur) :** attendre la barre d'espace entre les écrans et à la fin (« APPUYEZ SUR ESPACE », `WHILE INKEY(47):WEND:WHILE INKEY(47)=0:WEND`), sinon le tampon du clavier fait passer les écrans trop vite pour les capturer.
- **Commandes suivantes demandées :** la liste des variables (A, B, C), puis plus tard un ovale avec un angle d'inclinaison.

## D131 : liste des variables, solution A (touche F4) (18 nov. 2026) (validé par l'utilisateur)
- **Demande de l'utilisateur :** liste des variables, solutions A, B et C (IDEES.md) ; A d'abord.
- **Fait :** menu Compiler, « Liste des variables » (F4) ; la liste s'affiche dans le panneau des messages (lisible par un lecteur d'écran), une variable par ligne, par ordre alphabétique : nom, type (entier, nombre à virgule, texte, octet, tableau avec ses tailles), lignes où elle **reçoit une valeur** et lignes où elle est **utilisée** (mots choisis avec l'utilisateur à la place de « remplie » et « lue »). F6 puis Entrée sur une ligne : le curseur va à la première ligne de la variable.
- **⚠ à vérifier :** variable utilisée mais qui ne reçoit jamais de valeur (« elle vaut toujours 0 » ou « un texte vide »), avec un nom proche proposé quand il existe (une lettre de différence ou deux lettres échangées, noms de 3 lettres ou plus : « Faute de frappe pour SCORE ? ») ; variable qui reçoit une valeur mais n'est jamais utilisée.
- **Règles :** reçoivent une valeur : A=, FOR (compte aussi comme utilisée : la boucle s'en sert), INPUT, LINE INPUT, READ, DIM, T(I)=, MID$(A$...)= (aussi utilisée). A et A() sont deux variables, comme sur le CPC. Les valeurs d'une fonction DEF FN ne sont pas des variables du programme. Numéros BASIC ; sans numéro, ligne de l'éditeur. Avec des erreurs dans le programme, un avertissement dit que la liste peut être incomplète.
- **Fichiers :** src\CpcBasic.Core\Editing\VariableList.cs (nouveau), MainWindow.axaml, MainWindow.axaml.cs, VariableListTests.cs (12 tests), examples\varliste.bas (faute de frappe exprès), LISEZMOI.txt. 1557 tests réussis.
- **Essayé par l'utilisateur (18 nov. 2026) :** examples\varliste.bas : F4 affiche les 5 variables dont 2 à vérifier ; F6, flèches, Entrée : le curseur va à la bonne ligne.
- **Suite :** solution B (adresse de chaque variable dans la mémoire du CPC), puis C.

## D132 : liste des variables, solution B : adresses (18 nov. 2026) (validé par l'utilisateur)
- **Fait :** la liste F4 (D131) donne aussi l'adresse de chaque variable dans la mémoire du CPC, et sa taille : entier 2 octets (poids faible en premier), octet 1 octet, nombre à virgule 5 octets (format du Locomotive), texte : fiche de 3 octets (longueur, puis adresse des lettres), tableau : sa fiche (adresse de ses cases, puis leur nombre). F4 compile le programme en mémoire (sans disquette) ; avec une erreur, pas d'adresse, et le message le dit. Aucun octet ajouté au programme compilé.
- **Présentation (choix de l'utilisateur) :** l'adresse **au début** de la ligne (après ⚠), en gras, jaune sur bleu comme l'écran du CPC (noir sur blanc en contraste élevé) ; la taille à la fin, entre parenthèses. Exemple : `⚠ &4A4A  SCOER : entier. Utilisée ligne 50, mais ne reçoit jamais de valeur (elle vaut toujours 0). Faute de frappe pour SCORE ? (2 octets, poids faible en premier)`.
- **Pour lire une valeur :** débogueur de WinAPE (menu Debug), mémoire à cette adresse, pendant que le programme tourne.
- **Fichiers :** CodeGenerator.cs (GeneratedProgram.Addresses), VariableList.cs, MainWindow.axaml.cs (MessageItem.Highlight), VariableListTests.cs (3 tests, dont un qui lance le programme sur le mini-Z80 et relit la valeur à l'adresse annoncée). 1560 tests réussis.
- **Remarque de l'utilisateur :** « tu vas trop vite » : attendre sa réponse avant de changer une deuxième fois ; une chose à la fois (rappel de la règle du 16 nov.).
- **Suite :** solution C (voir les valeurs pendant l'exécution, sur l'écran du CPC) : commande ou touche nouvelle, à décider avec l'utilisateur.

## D133 : un caractère est un code de la ROM choisie, partout dans le programme (18 nov. 2026) (validé par l'utilisateur) ; complète D126 et D127
- **Constat de l'utilisateur :** `PRINT HEX$(ài,4)` (à pour @, tapé comme sur un CPC français) affichait &0000 une fois compilé, mais la bonne adresse avec le vrai BASIC (NOM.TXT). Cause : hors des textes, le compilateur prenait « ài » pour une variable « ÀI » (lettres accentuées acceptées dans les noms), qui valait 0.
- **Règle de l'utilisateur :** « les caractères doivent correspondre aux codes des ROM » ; « celui qui tape dans l'éditeur doit voir exactement la même chose que s'il tapait sur l'Amstrad de son pays ».
- **Vérifié sur le vrai CPC 6128 AZERTY de l'utilisateur (18 nov. 2026, photos) :** la touche ù affiche ù ; la touche « @ \\ $ » donne $, avec SHIFT à (code 64), avec CONTROL ç (code 92). La ROM française n'a aucun dessin de @ ni de \\ (vérifié dans OS6128-AZERTY.rom) : un CPC français ne montre jamais @.
- **Fait :** l'éditeur montre partout (pas seulement dans les textes) le caractère de la ROM choisie : sur le CPC français, @ tapé s'affiche à, | s'affiche ù (commandes RSX : ùDISC) ; sur le CPC anglais, ^ s'affiche ↑. F7 convertit tout le programme. Le compilateur lit chaque caractère par son code dans la ROM (`CharacterSet.ToKeyboardCodes` avant l'analyse) : ài = @i, ùDISC = |DISC. Les noms de variables ne prennent que des lettres sans accent (A à Z), comme le CPC ; une lettre accentuée sans code (fête) est refusée avec un message clair.
- **Vérifié :** plein.bas de l'utilisateur, compilé : « ADRESSE &4F36 », la même adresse que la liste F4 pour I. Tests : CharacterSetTests.cs (ài et @i, ùDISC et |DISC, 2↑3 et 2^3 donnent le même programme compilé ; lettre accentuée refusée). 1567 tests réussis.
- **Pour expliquer à l'utilisateur :** parler de ce qu'on voit à l'écran, pas des codes internes (mes explications sur « @i » et « |DISC » l'avaient embrouillé).
- **Idée de l'utilisateur notée :** vérifier la compilation sans enregistrer le programme (F5 enregistre toujours) : par exemple Maj+F5 (IDEES.md).

## D134 : Maj+F5, vérifier sans enregistrer (18 nov. 2026) (validé par l'utilisateur)
- **Idée de l'utilisateur :** F5 enregistre toujours le programme ; on veut parfois seulement savoir si la compilation passe.
- **Fait :** menu Compiler, « Vérifier sans enregistrer » (Maj+F5) : compile en mémoire comme F5 (programmes RUN "nom" du même dossier compris, lus seulement), sans enregistrer le .bas ni écrire de .dsk. Erreurs affichées comme avec F5, puis « Rien n'a été enregistré » ; sans erreur : « Vérification réussie : aucune erreur. Programme Z80 : N octets. Rien n'a été enregistré. » et un rappel si le programme n'est pas enregistré. L'étoile du titre reste. Aide F1 et LISEZMOI.txt mis à jour.
- **Essayé par l'utilisateur :** programme sans titre avec une erreur (55 goto remarque) : l'erreur s'affiche, rien n'est enregistré, « sans titre * » reste.
- **Vérifié en passant :** après NEW, le programme est « sans titre » : F5 et Ctrl+S demandent un nom, le programme d'avant (format.bas) ne peut pas être écrasé. **Remarque suivante de l'utilisateur :** il manque « Enregistrer sous » (garder format.bas et enregistrer ses changements sous un autre nom).

## D135 : Enregistrer sous (Ctrl+Maj+S) (18 nov. 2026) (validé par l'utilisateur)
- **Remarque de l'utilisateur (D134) :** on ne pouvait pas enregistrer un programme sous un autre nom (il voulait garder format.bas).
- **Fait :** menu Fichier, « Enregistrer sous... » (Ctrl+Maj+S) : fenêtre de Windows avec le dossier et le nom actuels proposés ; nom de plus de 8 caractères refusé tout de suite (disquette CPC), rien n'est enregistré ; l'éditeur travaille ensuite sur le nouveau fichier, qui garde les réglages F8 et F9 ; message : « L'ancien fichier (...) n'a pas changé. » Aide F1 et LISEZMOI.txt mis à jour.
- **Essayé par l'utilisateur :** examples\formats.bas, ligne ajoutée, enregistré sous essais\essai.bas : titre « essai.bas », formats.bas identique à la version du dépôt (vérifié).

## D136 : DEBUG et le réglage « Mise au point » (F2) (18 nov. 2026) (validé par l'utilisateur) ; solution C de la liste des variables
- **Choix de l'utilisateur, une question à la fois :** une commande à écrire dans le programme (A, pas une touche) ; nom **DEBUG** (« ça servira aussi pour déboguer » ; `DEBUG=5` donne Ready dans WinAPE : mot libre ; nouveau mot réservé) ; un réglage « Mise au point » (B) : coché, les DEBUG marchent ; décoché, ils sont ignorés et n'ajoutent rien ; touches ESPACE (suite) et S (stop : cette liste et tous les DEBUG suivants), sans touche « fin » (penser aux programmes avec énormément de DEBUG).
- **Affichage sur le CPC :** au début d'une ligne, « DEBUG ligne N » (numéro BASIC, ou ligne de l'éditeur sans numéros), puis chaque variable par ordre alphabétique avec les PRINT habituels : `I= 1  NOM$="CPC"  SCORE= 10`, nombres à virgule, octets ; un tableau : son nombre de cases (DIM écrit en nombres) ou « tableau ». Les valeurs des fonctions DEF FN ne sont pas montrées. Arrêt à chaque fenêtre pleine (lignes comptées avec TXT GET CURSOR : ligne + défilements) et à la fin, avec l'invite `ESPACE:suite S:stop` (19 caractères : tient en MODE 0), effacée après ESPACE comme après S (demande de l'utilisateur après le premier essai).
- **Réglage :** menu Options, « Mise au point (DEBUG) », touche **F2** (d'abord F12, mais F12 ouvre les outils d'Avalonia quand l'éditeur est lancé avec dotnet run), pour tous les programmes, décoché au départ. F5 rappelle le nombre de DEBUG actifs ou ignorés ; F1 explique F2.
- **Réalisation :** DebugStatement (parser) ; avec le réglage, le générateur ajoute à la fin du programme, après une fin de programme, une routine faite de PRINT ordinaires (DebugBody, DebugPage, DebugEnd) ; chaque DEBUG appelle le titre puis cette routine. Module Debug (titre, arrêt, attente, indicateur rt_dbg_off). Sans le réglage, le programme compilé est identique à celui sans DEBUG (test).
- **Tests :** DebugTests.cs (12 tests : écritures, rien d'ajouté sans le réglage, affichage, S, autres touches ignorées, 150 variables sur plusieurs écrans, S pendant un écran plein, DEF FN). MiniZ80 : TXT GET CURSOR rend 0 défilement. 1579 tests réussis.
- **Essayé dans WinAPE (18 nov. 2026) :** examples\debug.bas : deux arrêts avec les bonnes valeurs (I= 1, SCORE= 10 puis I= 2, SCORE= 20), S arrête les suivants et efface l'invite.
- **Plus tard, peut-être :** `DEBUG T()` pour voir un tableau en entier, `DEBUG A,B$` pour quelques variables.

## D137 : DEBUG A,B$ : seulement quelques variables (19 nov. 2026) (validé par l'utilisateur) ; suite de D136
- **Choix de l'utilisateur :** `DEBUG A,B$` en premier (avant `DEBUG T()` et l'ovale incliné) ; les variables sont montrées **dans l'ordre écrit** (choix 1 : on met la plus importante en premier), alors que `DEBUG` seul garde l'ordre alphabétique.
- **Écriture :** `DEBUG` seul, ou suivi de noms de variables simples séparés par des virgules. Refusés à la compilation : un nom utilisé nulle part dans le programme (« vérifiez son nom et son signe »), un nom de tableau, avec ou sans parenthèses (DEBUG seul donne son nombre de cases), une virgule en trop ou un nom manquant. Les noms suivent DEFINT, DEFSTR, DEFREAL et F8 (DEBUG N après DEFSTR N montre N$).
- **Même fonctionnement que D136 :** titre « DEBUG ligne N », arrêts ESPACE et S (S arrête aussi les DEBUG seuls), rien d'ajouté sans le réglage F2.
- **Réalisation :** DebugStatement reçoit la liste des noms ; la recherche des variables est mise en commun (DebugVariables.Collect, dans Ast.cs) entre la vérification du parser (CheckDebugNames) et le générateur ; une routine d'affichage par liste de noms différente (étiquette dbg_body:A,B$), la même liste écrite deux fois ne donne qu'une routine.
- **Tests :** 14 de plus dans DebugTests.cs (1593 tests réussis).
- **Essayé dans WinAPE (19 nov. 2026) :** examples\debugvar.bas : `DEBUG I,SCORE,VIES` deux fois puis `DEBUG NOM$,X!`, écrans conformes à ceux annoncés.
- **Plus tard, peut-être :** `DEBUG T()` (remplacé par D138).

## D138 : DEBUG T(3) : une seule case de tableau (19 nov. 2026) (validé par l'utilisateur) ; suite de D137
- **Idée de l'utilisateur (à la place de `DEBUG T()`, le tableau entier) :** `DEBUG T(26)` ne montre que cette case, `DEBUG T(26,2)` aussi, car un tableau peut avoir beaucoup de cases. `DEBUG T()` (tout le tableau) n'est plus prévu ; à reprendre seulement en cas de besoin.
- **Numéro calculé accepté :** `DEBUG T(I)` ; affichage avec **le vrai numéro de la case** (choix 2 de l'utilisateur) : `T(3)= 25`, sans espace dans les parenthèses ; `M(1,2)= 7` ; tableau de textes : `N$(2)="CPC"`. Mélange possible avec les variables, dans l'ordre écrit : `DEBUG I,T(I),M(I,I)`.
- **Refusés à la compilation :** case qui n'existe pas quand DIM et le numéro sont écrits en nombres (`DEBUG T(11)` avec `DIM T(10)` : « le tableau va de 0 à 10 ») ; mauvais nombre de numéros ; `DEBUG X(3)` quand X n'est pas un tableau ; variable du numéro utilisée nulle part ; `DEBUG T()`. Avec un DIM calculé, un numéro trop grand donne « Subscript out of range » pendant l'exécution, comme toute lecture.
- **Réalisation :** DebugStatement garde des expressions (variables ou cases) et la liste telle qu'écrite (clé de la routine) ; ces expressions ne sont pas parcourues par AstWalker, pour que DEBUG ne compte pour rien sans le réglage F2 ; vérifications dans CheckDebugNames ; le numéro est affiché avec STR$ sans l'espace du début (comme WRITE).
- **Tests :** 13 de plus dans DebugTests.cs (1606 tests réussis).
- **Essayé dans WinAPE (19 nov. 2026) :** examples\debugtab.bas : `DEBUG I,T(I),M(I,I)` trois fois puis `DEBUG T(0),T(10)`, écrans conformes.

## D139 : ELLIPSE, ovale droit ou incliné (19 nov. 2026) (validé par l'utilisateur)
- **Mot vérifié dans WinAPE :** `ELLIPSE=5` donne Ready (mot libre). Nouveau mot réservé : **ELLIPSE** (il était seulement coloré dans l'éditeur).
- **Écriture (choix de l'utilisateur, une question à la fois) :** `ELLIPSE x, y, rayon x, rayon y [, stylo [, remplissage [, angle]]]` : le début comme CIRCLE, l'angle à la fin (choix 2) ; c'est la place qui compte et une place peut rester vide, comme MOVE x,y,,mode : `ELLIPSE 160,100,50,20,,,30`. Rayon x et rayon y comptés comme le rayon de CIRCLE (0 à 1000 ; calculé hors limites : Improper argument) ; deux rayons égaux = exactement le rond de CIRCLE (vérifié par un test). Angle en degrés (choix 1), n'importe quel entier, ramené entre 0 et 179 (370 = 10, -30 = 150) ; sens choisi par Claude à la demande de l'utilisateur : le côté droit monte quand l'angle est positif (sens inverse des aiguilles d'une montre à l'écran). Stylo omis : stylo courant du tracé ; remplissage omis : pas d'intérieur.
- **Facteur de largeur (choix 1) :** le même que CIRCLE (×0,5 / ×1 / ×2 selon le MODE, D130) ; le réglage « vrai moniteur » viendra plus tard pour les deux commandes à la fois (IDEES.md).
- **Méthode :** un ovale incliné, ligne par ligne, est un ovale droit dont chaque ligne glisse sur le côté. Ligne à la distance d du centre : points dont l'écart au centre glissé (sigma × d) est au plus racine(T), T = P - Q d², chaque bord arrondi une seule fois (sinon des marches de travers en MODE 0, vu sur l'aperçu). Contour : points qui touchent l'extérieur, prolongés jusqu'à la ligne voisine quand elles ne se touchent pas (ovales très fins et très penchés : pas de trou). Calcul de départ entièrement en nombres entiers (table des sinus de 0 à 90 degrés × 16384, multiplications, divisions et une racine sur plusieurs octets), pour que le vrai CPC et les tests donnent exactement les mêmes points ; puis lignes de d = D à 0 avec une racine qui ne fait que grandir (une soustraction et deux additions par pas, dans les registres). Le calcul de départ est gardé si l'ovale suivant a les mêmes rayons, angle et MODE.
- **Façon de faire (comme CIRCLE) :** essais\ellipse\modele.py (méthode avec virgule, aperçu apercu.png montré à l'utilisateur : « chapeau ! »), entiers.py (même méthode en entiers : 4788 ronds identiques à CIRCLE), routine.py (la routine Z80, instruction par instruction), check.py (petit émulateur : 522 ovales identiques à entiers.py), tocs.py (écrit le module C#). Vitesse : environ 19 000 instructions pour dessiner un ovale de 40 × 20 (CIRCLE de rayon 99 : 31 000), plus environ 37 000 pour le calcul de départ quand il change. Deux erreurs trouvées en route : la division (longueur du reste) et une retenue oubliée (DEC HL ne la donne pas) ; puis deux noms d'étiquettes en double (code et données), signalés par le compilateur ; l'émulateur Python les refuse maintenant aussi.
- **Réalisation :** module Ellipse (`rt_ellipse`, RuntimeLibrary.cs) ; deux instructions Z80 nouvelles dans Z80Emitter et MiniZ80 : ADC A,(HL) et SBC A,(HL) ; EllipseStatement (Ast.cs), ParseEllipse (Parser.cs), génération dans CodeGenerator.cs.
- **Tests :** EllipseTests.cs (37 tests : ovales comparés point par point à une référence recopiée à part dans les 3 MODES, avec et sans remplissage, hors de l'écran, rond = CIRCLE, angle + 180, côté droit qui monte, calcul gardé puis refait, changement de MODE, ROM haute, écritures acceptées et refusées, Improper argument). 1643 tests réussis.
- **Essayé dans WinAPE (19 nov. 2026) :** examples\ovales.bas : six ovales (0, 30, 60, 90, 120 degrés rempli, ovale fin) puis « un ovale qui tourne » (18 angles, et un rond rempli au centre) : conformes ; « comme c'est beau... ».
- **Question de l'utilisateur :** SQR serait-il plus rapide ? Même idée (une racine par ligne au lieu de SIN et COS par point), mais la racine de la ROM sur 5 octets coûte plusieurs millisecondes par appel ; ici la racine avance de quelques pas d'une ligne à l'autre, en entiers.


## D140 : ronds et ovales pour un vrai moniteur Amstrad (19 nov. 2026) (validé par l'utilisateur) ; remplace le choix A de D130
- **Question de l'utilisateur :** les autres émulateurs ont-ils le même problème ? Réponse : cela dépend de l'émulateur et de ses réglages (la plupart montrent des points carrés ; certains imitent un écran 4:3 ; une fenêtre étirée change tout) ; sur un vrai CPC, un point du MODE 1 est environ 8 % plus étroit que haut (cpcwiki).
- **Choix de l'utilisateur :** CIRCLE et ELLIPSE dessinent pour un vrai moniteur Amstrad, sans option (ni commande ni réglage) : « le programme est adapté pour un vrai CPC et non pas pour des émulateurs ». Remplace le choix A de D130 (ronds parfaits dans l'émulateur) ; les programmes déjà écrits dessinent des ronds un peu plus larges.
- **Facteur :** 69/64 = 1,078 (au lieu de 1,08 : la différence de 0,2 % ne se voit pas, et ce nombre garde les calculs exacts en entiers). Largeur ×0,54 / ×1,078 / ×2,16 selon le MODE. Demi-largeur de CIRCLE : partie entière de k × 69/64 × racine((r + 1/2)² - d²). ELLIPSE : P × 4761/4096 et glissement × 69/64 au calcul de départ. Deux rayons égaux redonnent toujours exactement CIRCLE (vérifié par les tests).
- **Réalisation :** CIRCLE : sommes sur 40 bits au lieu de 32 (valeurs de départ × 4761 = 69² par une petite multiplication), environ 26 600 instructions pour un rayon de 99 (du même ordre qu'avant), environ 755 octets au lieu de 610. ELLIPSE : trois lignes du calcul de départ. Erreur trouvée par les tests C# : un saut court (JR) devenu trop long dans CIRCLE ; les vérifications Python mesurent maintenant la longueur des sauts (essais\circle\jumps.py, essais\ellipse\jumps.py). Nouveau : essais\circle\tocs.py (écrit le module C# de CIRCLE).
- **Dans WinAPE (réglage « Display » de l'utilisateur) :** traduit pour l'utilisateur ; aucun réglage de proportions ; avec « DirectX Stretch » coché, l'image suit la taille de la fenêtre (on peut la rétrécir en largeur pour imiter le moniteur : Alt + Espace, Taille, flèches) ; décoché, la fenêtre garde sa taille et ses points carrés.
- **Essayé dans WinAPE (19 nov. 2026) :** examples\moniteur.bas (carré de 108 points sur 100 lignes, CIRCLE, ELLIPSE de deux rayons égaux) : environ 8 % plus larges que hauts, comme prévu.
- **Suite décidée avec l'utilisateur (étape 2) :** quand un programme utilise CIRCLE et ELLIPSE, CIRCLE se sert du dessin des lignes d'ELLIPSE (place gagnée) ; CIRCLE seul garde sa petite routine ; les points restent exactement les mêmes.

## D141 : priorité vitesse ou place (F3) ; rond rapide dans ELLIPSE (19 nov. 2026) (validé par l'utilisateur)
- **Mesure qui a changé le plan de D140 :** un rond dessiné par ELLIPSE est environ 3 fois plus lent que par CIRCLE (78 000 instructions contre 26 600 pour un rayon de 99), et non 30 à 50 % comme annoncé : le dessin des lignes d'ELLIPSE est fait pour des lignes qui glissent. Dit tel quel à l'utilisateur.
- **Choix de l'utilisateur :** une option pour que le programmeur choisisse, et une règle pour les commandes à venir qui auront le même genre de choix. Réalisé comme **un seul réglage de l'éditeur**, « Priorité : vitesse / place », menu Options, touche **F3** (libre ; F10 et F12 gênés), **vitesse par défaut**, commun à tous les programmes. Il ne change jamais le dessin, seulement la vitesse et la taille du programme compilé (différent de D140, où l'utilisateur ne voulait pas d'option parce que le résultat à l'écran changeait).
- **Aujourd'hui :** avec la priorité « place » et ELLIPSE dans le programme, chaque CIRCLE x,y,r devient ELLIPSE x,y,r,r (angle 0) et le module Circle n'est pas ajouté (environ 750 octets de moins, ronds environ 3 fois plus lents, mêmes points) ; sinon CIRCLE garde sa routine. Le message de F5 rappelle la priorité seulement quand le programme contient CIRCLE et ELLIPSE ; F1 et LISEZMOI l'expliquent.
- **Règle pour la suite :** toute nouvelle commande qui peut partager une routine au prix de la vitesse suit ce même réglage ; le journal, F1 et le message de F5 disent ce qu'il change.
- **Rond rapide dans ELLIPSE :** avec deux rayons égaux, la racine et les deux divisions du calcul de départ sont connues d'avance (H = a x 2^14, X = Y = a x 4096 + 2048, Q = 4761 x 4096) : environ 9 000 instructions au lieu de 37 000 ; aussi pour un ELLIPSE x,y,r,r écrit à la main.
- **Réalisation :** CompilerOptions.PreferSize ; CodeGenerator (CIRCLE vers les étiquettes du module Ellipse) ; EditorSettings.PreferSize ; MainWindow (menu, F3, messages) ; essais\ellipse\routine.py (raccourci rond, vérifié : 252 cas). Problème rencontré : la copie de fichiers vers le dossier de l'utilisateur écrivait parfois une ancienne version ; Claude a refait les changements directement dans le dossier et comparé les empreintes (md5).
- **Tests :** 5 de plus dans EllipseTests.cs (mêmes points avec les deux priorités et programme plus petit, CIRCLE seul garde sa routine, Improper argument). 1648 tests réussis. **Essayé par l'utilisateur :** l'éditeur se compile ; F3 et le message de F5 avec examples\moniteur.bas sont conformes.
- **Ensuite (demande de l'utilisateur) :** mesurer la vitesse de SIN et COS du Locomotive dans un programme compilé, avant de décider de fonctions rapides en nombres entiers (par exemple ISIN, ICOS avec une table, à côté de SIN et COS qui restent ceux du Locomotive).

## D142 : ISIN et ICOS, sinus et cosinus rapides en entiers (19 nov. 2026) (validé par l'utilisateur)
- **Question de l'utilisateur :** SIN et COS du Locomotive ne sont pas rapides ; des commandes à nous prendraient-elles trop de place, et que gagnerait-on ? **Mesure** (essais\vitsin.bas, programme compilé, WinAPE à 108 %) : SIN et COS environ 14 ms chacun, une multiplication à virgule 0,9 ms, lecture d'un tableau de sinus en BASIC 0,6 ms. SIN et COS restent ceux du Locomotive (règle 100 % compatible) ; les fonctions rapides viennent à côté.
- **Choix de l'utilisateur :** noms **ISIN** et **ICOS** (I comme entier ; `isin=1` et `icos=1` donnent Ready dans WinAPE : mots libres ; deux nouveaux mots réservés) ; angle **au degré près** (choix 1 : table de 91 sinus).
- **Écriture :** `ISIN(angle, échelle)`, `ICOS(angle, échelle)` : sinus ou cosinus x échelle, arrondi au plus près, en entier. Angle en degrés, n'importe quel entier, ramené de 0 à 359 (DEG et RAD n'y changent rien) ; échelle de -32768 à 32767 ; seul cas hors des entiers : ISIN(270,-32768) = 32768, rendu comme 32767. Exemple : `PLOT 320+ICOS(A,100),200+ISIN(A,100)`.
- **Réalisation :** table des sinus x 16384 dans un module commun **SinTable** (`rt_sintab`), partagé avec ELLIPSE (qui n'a plus sa propre copie ; vérifié en Python : 162 cas) ; module **ISin** (`rt_isin`, `rt_icos`) : angle ramené par soustractions de 360 x 2^k, demi-tours et quart de tour, une multiplication 16 bits x 16 bits, arrondi. Instruction RL E ajoutée (Z80Emitter, MiniZ80). Une fonction ne peut plus recevoir de valeur (`ISIN=5` refusé ; règle étendue à toutes les fonctions, aucun ancien test touché). ISIN et ICOS colorés dans l'éditeur.
- **Erreur trouvée par les tests :** la réduction de l'angle écrasait l'échelle dans DE avant qu'elle soit mise de côté (résultats 180, 360...) ; l'échelle est maintenant poussée sur la pile d'abord.
- **Tests :** ISinTests.cs (tous les angles de -720 à 720, 13 échelles, ISIN et ICOS, comparés à une référence à part ; angles extrêmes ; valeurs connues ; table identique ; programme complet ; table liée une seule fois avec ELLIPSE ; écritures refusées). 1673 tests réussis.
- **Essayé dans WinAPE (19 nov. 2026) :** examples\angles.bas : 360 points avec SIN et COS en 11,3 s, avec ISIN et ICOS en 0,75 s (dont la plus grande part est le PLOT de chaque point) ; ronds identiques ; « y a pas photo ».
- **Place :** table 182 octets (une seule fois), fonctions environ 150 octets, seulement si le programme s'en sert.

## D143 : préparation de la version 0.4.0 ; exception de liaison en vigueur (19 nov. 2026) ; applique D016 et D017
- **Idée de l'utilisateur :** publier une version avant le chantier des bibliothèques (idée notée dans IDEES.md), pour avoir des retours et un point stable.
- **D016 (ROM) vérifié par Claude :** aucun fichier suivi par git ne ressemble à une ROM ; essais\ (où sont les ROM de l'utilisateur) est exclu par .gitignore ; le paquet ne contient que l'éditeur compilé, les exemples .bas, COMPATIBILITE.md, LISEZMOI.txt, LICENSE (et maintenant EXCEPTION-LIAISON.md) ; aucun tableau d'octets recopié d'une ROM dans le code (la table des lettres accentuées ne contient que des numéros de caractères).
- **D017 (exception de liaison) relue par Claude, qui n'est pas juriste :** le texte visait un dossier `runtime` qui n'existe pas (les modules sont écrits en C# depuis D024) ; les en-têtes des fichiers disaient l'exception appliquée, le README parlait de « futurs modules » ; l'exception n'était pas dans le paquet.
- **Choix de l'utilisateur (choix 1) :** l'exception s'applique dès la 0.4.0 (« si c'est pour dire que les jeux créés avec mon projet peuvent être utilisés et distribués, alors c'est oui »), avec la mention « texte en attente de relecture ». Texte corrigé sans changer son intention : bibliothèque d'exécution définie comme le code Z80 copié par le compilateur (RuntimeLibrary.cs, RealRuntime.cs, suites écrites par CodeGenerator.cs, et les futures bibliothèques du projet) ; paragraphe « En bref » ; les bibliothèques d'autres auteurs gardent leur licence. README et LISEZMOI accordés ; le paquet contient EXCEPTION-LIAISON.md. Relecture conseillée : licensing@fsf.org ou un forum de logiciel libre.
- **Numéro de version :** 0.4.0 dans Directory.Build.props et packaging\build-release.cmd.
- **packaging\build-release.cmd :** lancé par un double-clic, il partait du dossier packaging\ et échouait aussitôt (fenêtre refermée) ; il se place maintenant lui-même à la racine du dépôt (`cd /d "%~dp0.."`) et attend une touche à la fin. Au deuxième essai, le paquet a été construit, mais la fenêtre s'est encore refermée (cause inconnue) : Claude a calculé la somme de contrôle sur le fichier.
- **Paquet :** release\CpcBasic-0.4.0-win-x64.zip (environ 75 Mo, 81 exemples, EXCEPTION-LIAISON.md, aucune ROM) ; SHA256 0d3af79aebb4571beef93f7c061fcf55ff7d7015a194d55aeb3c510ca917584e (dans RELEASE-NOTES-0.4.0.md). **Essayé par l'utilisateur :** décompressé hors du projet ; Avast analyse CpcIde.exe puis le laisse passer (fichier non signé, prévu dans LISEZMOI) ; examples\ovales.bas du paquet compilé et lancé dans WinAPE.
- **Publiée sur GitHub (19 nov. 2026) :** pre-release v0.4.0 « CPC BASIC IDE 0.4.0 (version de test) », commit 9f8811c, description tirée de docs\RELEASE-NOTES-0.4.0.md.

## D144 : bibliothèques de commandes, premiers principes (19 nov. 2026) (décidé par l'utilisateur ; conception en cours) ; répond en partie à D015 et D028
- **Idée de l'utilisateur :** les programmeurs pourront ajouter leurs propres commandes et fonctions, rangées dans des bibliothèques par thème (graphique, texte, son, et d'autres), avec un éditeur d'assembleur pour les écrire (voir IDEES.md).
- **Pas de mot à écrire dans le programme :** un **catalogue** (liste de toutes les commandes et fonctions des bibliothèques, avec le nom de leur bibliothèque) est mis à jour à chaque ajout ou suppression d'une commande ; le compilateur le consulte **avant** d'afficher qu'un mot est inconnu (seulement pour un mot employé comme une commande, ou comme une fonction avec parenthèses) ; une variable du même nom reste une variable (`STAR=5`, `PRINT STAR`).
- **Seule la routine utilisée est ajoutée au programme** (avec les petites routines dont elle a besoin), pas toute la bibliothèque : la liaison sélective d'aujourd'hui, routine par routine.
- **Un nom n'existe que dans une seule bibliothèque :** à l'ajout d'une commande dont le nom est déjà au catalogue, l'outil le signale et refuse.
- **Fonction et tableau du même nom :** erreur claire (« le nom FLOU est déjà une fonction de la bibliothèque GRAPHIQUE : choisissez un autre nom pour le tableau »).
- **Valeurs des commandes :** déclarées par un **questionnaire** à la création d'une commande (nom, commande ou fonction, nombre de valeurs, rôle et limites de chacune) ; l'outil écrit la description, le programmeur n'a pas à l'écrire à la main.
- **Message de F5 :** il dit quelles bibliothèques et quelles commandes ont servi ; compilé sans la bibliothèque, le programme reçoit un message qui nomme le mot qui manque.
- **Rangement (question 2, réponse de l'utilisateur) :** le projet fournit au départ des bibliothèques par thème avec des commandes du projet, **protégées** (ni effacées ni modifiées) ; le programmeur **ajoute ses commandes dans ces mêmes bibliothèques** et peut modifier ou effacer les siennes. Sur le disque, chaque bibliothèque a deux parties : celle du projet, livrée avec l'éditeur et remplacée à chaque version, et celle du programmeur, dans son dossier personnel, jamais touchée par une mise à jour ; l'éditeur les montre comme **une seule bibliothèque** (commandes du projet marquées protégées).
- **Distribution (idée de l'utilisateur) :** une option « Répertoire de distribution » dépose dans un dossier choisi les listings .bas du programme et toutes les commandes ajoutées qu'il utilise, pour que celui qui le reçoit puisse le compiler ; les commandes du projet n'y sont pas (tout le monde les a).
- **Assembleur (question 3, choix 1) :** façon d'écrire de l'assembleur de WinAPE (style Maxam) : instructions Z80 habituelles, étiquettes, commentaires après « ; », nombres & ou # (hexadécimal) et % (binaire), DEFB/DB, DEFW/DW ; pas d'ORG (le compilateur place la routine) ; détails exacts à vérifier avant d'écrire l'assembleur.
- **Routines appelées depuis l'assembleur (idée de l'utilisateur) :** une bibliothèque contient des commandes et fonctions (utilisables en BASIC, déclarées par le questionnaire) et des **routines** (utilisables seulement depuis l'assembleur, déclarées par leur nom, par exemple « DECLARATION : MA_ROUTINE_PLOT ») ; un CALL ou un JP vers un nom qui n'est pas une étiquette de la routine est cherché dans le catalogue des routines, et la routine appelée est ajoutée au programme avec ses propres dépendances (comme les modules d'aujourd'hui) ; le projet pourra rendre utilisables certaines de ses routines (poser un point, multiplier...).
- **Valeurs des commandes (question 4, choix 1) :** des cases mémoire qui portent le nom des valeurs donné au questionnaire (comme CIRCLE et ELLIPSE) : le compilateur y range les valeurs avant d'appeler la routine, qui les lit par leur nom (`LD HL,(X)`) ; autant de valeurs qu'on veut ; une fonction rend son résultat dans HL. Plus tard peut-être, en option, la règle des RSX (D089) pour reprendre d'anciennes routines.
- **Fichiers (question 5, choix 1) :** un dossier par bibliothèque (par exemple GRAPHIQUE), un fichier texte par commande, fonction ou routine (réponses du questionnaire en tête, puis le code assembleur) ; l'extension sera choisie plus tard.
- **Licence :** déjà réglée par D143 : les bibliothèques du projet suivent l'exception de liaison ; celles des programmeurs gardent la licence de leur auteur.
- **Étapes proposées :** B1 assembleur (lecture du texte style WinAPE, avec ses tests) ; B2 fichier de commande, catalogue, recherche par le compilateur, cases des valeurs, liaison des routines appelées ; B3 première bibliothèque du projet (STAR, POLYGON) ; B4 questionnaire et éditeur d'assembleur dans l'éditeur (bibliothèques, commandes protégées) ; B5 répertoire de distribution.

## D145 : assembleur Z80 des bibliothèques, étape B1 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; applique D144
- **Fichier :** `src\CpcBasic.Core\Assembly\Z80Assembler.cs` ; tests dans `tests\CpcBasic.Tests\AssemblerTests.cs`.
- **Ce qu'il fait :** il lit le texte assembleur d'une commande et l'écrit dans le programme compilé (Z80Emitter) ; les adresses sont calculées à la fin, d'où l'absence d'ORG.
- **Écriture acceptée (style WinAPE / Maxam) :** étiquette au début de la ligne (ou suivie de « : ») ; commentaire après « ; » ; majuscules ou minuscules ; nombres 123, &FF, #FF, %1010, 'A', $ (adresse de l'instruction) ; calculs + - * / et parenthèses ; une étiquette ne peut être qu'augmentée ou diminuée d'un nombre (`TABLE+2`) ; directives NOM EQU valeur, DEFB/DB/BYTE (nombres et textes), DEFW/DW/WORD, DEFS/DS taille[,valeur], DEFM/TEXT ; toutes les instructions documentées du Z80, avec IX et IY ; RST 7 ou RST &38 ; EX HL,DE accepté.
- **Refusés avec un message clair :** ORG, READ/INCLUDE, macros, ALIGN ; une étiquette dans un octet ; un saut relatif vers un nombre ; une forme d'instruction qui n'existe pas.
- **Étiquettes :** celles d'une routine reçoivent un préfixe (deux routines peuvent avoir chacune une étiquette BOUCLE) ; un nom qui n'est pas une étiquette de la routine est demandé au catalogue (routine d'une bibliothèque, case d'une valeur) ; les noms utilisés sont notés pour ajouter les routines appelées (étape B2).
- **Erreurs :** une liste avec le numéro de ligne et l'explication en français ; une ligne fausse n'empêche pas de vérifier les suivantes.
- **Z80Emitter :** deux ajouts pour écrire « étiquette + décalage » : `AbsoluteWord` et `RelativeByte`.
- **Pas encore :** écart entre deux étiquettes (`FIN-DEBUT`), instructions non documentées (IXH, IXL...), EQU qui utilise $.

## D146 : fichier d'une commande de bibliothèque, étape B2 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; applique D144
- **Fichier :** `src\CpcBasic.Core\Libraries\LibraryCommand.cs` ; tests dans `tests\CpcBasic.Tests\LibraryCommandTests.cs`.
- **Forme du fichier :** en tête, les réponses du questionnaire sur des lignes « ;@ », puis le code assembleur. Pour un assembleur, les lignes « ;@ » sont des commentaires : le même fichier peut être essayé dans WinAPE.
- **Lignes de tête :** `;@NOM` (obligatoire, pareil que le nom du fichier) ; `;@GENRE COMMANDE`, `FONCTION` ou `ROUTINE` (obligatoire) ; `;@VALEUR nom, plus petite, plus grande, rôle` (une ligne par valeur, dans l'ordre où on les écrit en BASIC, 16 au plus) ; `;@AIDE` (une ou plusieurs lignes) ; `;@AUTEUR`.
- **Noms :** lettres sans accent, chiffres et _, une lettre au début, 16 caractères au plus ; pas un nom de registre ou de condition du Z80 (A, HL, NZ...) ; une valeur ne s'appelle pas comme la commande.
- **Routine :** pas de VALEUR, elle reçoit ses valeurs dans les registres (l'AIDE le dit).
- **Extension :** provisoire `.cmd.asm`, à choisir par l'utilisateur (D144) ; elle est écrite à un seul endroit (`LibraryCommandFile.Extension`). **Gardée définitivement (D165).**

## D147 : catalogue des bibliothèques, étape B2 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; applique D144 et D146
- **Fichier :** `src\CpcBasic.Core\Libraries\LibraryCatalog.cs` ; tests dans `tests\CpcBasic.Tests\LibraryCatalogTests.cs`.
- **Deux dossiers :** celui du projet (commandes protégées) puis celui du programmeur ; dans chacun, un sous-dossier par bibliothèque et un fichier par commande. Une bibliothèque présente dans les deux dossiers n'en fait qu'une.
- **Un nom, une seule fois :** le second fichier qui donne un nom déjà pris est refusé ; le dossier du projet est lu en premier, ses commandes passent donc avant.
- **Refusés avec un message (fichier, ligne, explication) :** un fichier faux, un nom de dossier qui ne convient pas, un fichier rangé hors d'une bibliothèque. Les autres fichiers restent au catalogue.
- **Complément à D146 :** une COMMANDE ou une FONCTION ne peut pas contenir _ (le BASIC ne l'accepte pas dans un nom), ni commencer par FN (DEF FN), ni être déjà un mot du BASIC (PLOT...) ; une ROUTINE peut contenir _ (`MA_ROUTINE_PLOT`).
- **Pas encore :** l'emplacement des deux dossiers sur le disque (il sera choisi avec l'éditeur, étape B4).

## D148 : le compilateur utilise les bibliothèques, étape B2 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; applique D144, D146 et D147
- **Fichiers :** `src\CpcBasic.Core\Libraries\LibraryLinker.cs` (nouveau) ; Parser, Ast, Diagnostic, BasicAnalyzer, CompilerOptions (réglage `Libraries`, le catalogue) et CodeGenerator ; tests dans `tests\CpcBasic.Tests\LibraryCompileTests.cs`.
- **Commande :** un mot en début d'instruction qui n'est pas un mot du BASIC est cherché au catalogue avant l'erreur « commande inconnue » ; `STAR 100,50,20` donne toutes les valeurs dans l'ordre du questionnaire ; `STAR = 5` reste une variable.
- **Fonction :** `LUNE(3)` dans un calcul ; une fonction sans valeur s'écrit sans parenthèses, comme TIME ; elle donne un entier (HL).
- **Vérifié à la compilation :** le nombre de valeurs (le message donne la bonne écriture, par exemple « STAR x, y, rayon ») ; un nombre écrit hors des limites du questionnaire ; un tableau qui porte le nom d'une fonction ; le code assembleur de chaque commande utilisée et des routines qu'elle appelle (l'erreur est donnée à la ligne BASIC qui utilise la commande, avec la ligne du fichier). Une commande fautive que le programme n'utilise pas ne gêne pas.
- **Pas vérifié pendant que le programme tourne :** une valeur calculée hors des limites (la routine reçoit la valeur telle quelle).
- **Dans le programme compilé :** chaque valeur a sa case de 2 octets ; le programme y range les valeurs puis fait CALL ; plusieurs valeurs passent d'abord par la pile (`SOMME(1, SOMME(4, 5))` ne mélange pas les cases). Seules les commandes utilisées sont ajoutées, avec les routines qu'elles appellent, dans un ordre fixe ; elles sont nommées « BIBLIOTHÈQUE:NOM » dans la liste des modules.
- **Pas encore :** l'éditeur ne donne pas encore le catalogue au compilateur (étape B4) ; le message de F5 ne nomme pas encore les bibliothèques utilisées.

## D149 : l'éditeur trouve les bibliothèques (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; début de l'étape B4, applique D144 et D148
- **Bibliothèques du projet :** dossier `bibliotheques` du dépôt, copié à côté de CpcIde.exe (et dans la version publiée) ; remplacé à chaque version ; commandes protégées.
- **Bibliothèques du programmeur :** `Documents\CPC BASIC IDE\bibliotheques`, jamais touché par une mise à jour (Documents plutôt qu'AppData : facile à trouver).
- **Quand :** les deux dossiers sont relus à chaque F5, Maj+F5 et F4 : une commande ajoutée sert tout de suite, sans relancer l'éditeur.
- **Message de F5 :** une ligne « Bibliothèques utilisées : GRAPHIQUE (STAR, POLYGON) » ; les modules du compilateur restent sur leur ligne. Un fichier de commande refusé par le catalogue est signalé (en rouge) à la fin du compte rendu, avec son dossier, sa ligne et l'explication.
- **Fichiers :** `src\CpcIde\LibraryFolders.cs` (nouveau), `MainWindow.axaml.cs`, `CpcIde.csproj`, `bibliotheques\LISEZMOI.txt`.

## D150 : valeurs facultatives des commandes de bibliothèque ; la commande STAR (19 nov. 2026) (validé par l'utilisateur ; forme du fichier choisie par l'assistant) ; complète D146 et D148
- **Demande de l'utilisateur :** pouvoir sauter des valeurs, comme avec ELLIPSE (choix 1).
- **Dans le fichier :** `;@OPTION nom, plus petite, plus grande, valeur si sautée, rôle`. La valeur si sautée peut être hors des limites : la routine sait ainsi que la valeur a été sautée (par exemple -1 pour « stylo du moment »). Les OPTION viennent après toutes les VALEUR.
- **En BASIC :** une OPTION se saute par une place vide (`STAR 320,200,30,80,5,,2`) ou en s'arrêtant avant (`STAR 320,200,30,80,5`) ; une virgule à la fin est une faute. Une fonction dont toutes les valeurs sont facultatives peut s'écrire sans parenthèses.
- **Message d'erreur :** la bonne écriture montre les OPTION entre crochets : « STAR x, y, r1, r2, pointes [, stylo] [, remplissage] [, angle] ».
- **STAR (décision de l'utilisateur) :** une seule commande pour les étoiles avec le nombre de pointes voulu ; les polygones s'obtiennent avec un rayon interne assez grand ; pas de commande POLYGON. Forme : `STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle]` ; nom STAR gardé (« simple à retenir »). Le contour d'abord, le remplissage ensuite.

## D151 : STAR, première commande de la bibliothèque GRAPHIQUE (contour) (19 nov. 2026) (forme décidée par l'utilisateur, D150 ; réalisation choisie par l'assistant)
- **Fichier :** `bibliotheques\GRAPHIQUE\STAR.cmd.asm` (commande protégée du projet), écrit en assembleur style WinAPE et lu par le catalogue comme celles des programmeurs.
- **Écriture :** `STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle]` ; repère natif et largeur de CIRCLE (vrai moniteur, ×69/64 en MODE 1, D140) ; rayons de 0 à 1000 lignes ; 2 à 100 pointes ; angle en degrés, n'importe quel entier : 0 = une pointe en haut, positif = l'étoile tourne vers la gauche (même sens qu'ELLIPSE) ; stylo sauté : stylo du tracé ; **remplissage : accepté mais pas encore dessiné** (étape suivante).
- **Méthode :** sommet k (0 à 2 × pointes - 1) au rayon externe si k est pair, interne sinon ; angle en centièmes de degré = 9000 + 100 × angle + k × 18000 / pointes (le reste de la division est gardé : pas d'erreur qui s'ajoute) ; sinus par une table de 0 à 90 degrés × 16384 (la même que le compilateur) et les centièmes entre deux degrés ; écarts arrondis. Traits tracés par le firmware (GRA LINE ABSOLUTE) avec l'origine remise un moment en 0,0 ; stylo, origine et position du tracé remis comme avant. Valeur calculée hors limites (pointes, rayons) : rien n'est dessiné.
- **Première façon essayée :** angle en dixièmes de degré : jusqu'à 3,9 points d'écart pour les très grandes étoiles en MODE 2 ; en centièmes : moins d'un point partout.
- **Vérification :** `essais\star\sim.py` exécute le texte assembleur (indicateurs C et Z, firmware imité) ; `essais\star\test_star.py` : 407 étoiles (3 MODES, angles, rayons et pointes au hasard) à moins de 0,92 point du calcul exact, étoile refermée, stylo, origine et position remis, valeurs hors limites. Environ 15 000 instructions pour une étoile à 5 pointes.
- **Tests C# :** `tests\CpcBasic.Tests\StarTests.cs` (12 tests : traits identiques au simulateur Python dans les 3 MODES, écart au calcul exact, valeurs hors limites, messages). MiniZ80 : RRA, INC L, RR H, BIT 7,A ajoutés, et la liste des traits tracés (Lines). 1830 tests réussis.
- **Essayé dans WinAPE (19 nov. 2026) :** examples\etoiles.bas : six figures (5, 3, 4 pointes ; 6 pointes tournée de 30 degrés ; hexagone ; 8 pointes fines), une étoile qui tourne (12 étoiles, rosace), MODE 0 et MODE 2 : conformes ; message de F5 « Bibliothèques utilisées : GRAPHIQUE (STAR) » ; « Il y a de quoi être fier ! ».

## D152 : intérieur de STAR ; 32 pointes au plus (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; complète D151
- **Méthode :** tous les sommets sont d'abord calculés et gardés (repère natif) ; intérieur ligne par ligne (de 0 à 199 seulement) : pour chaque bord (y1 <= ligne < y2, bords à plat sautés), croisement x = x1 + (ligne - y1) × (x2 - x1) / (y2 - y1) calculé exactement (multiplication 16 × 16 bits, division 32 / 16 bits) ; croisements rangés ; remplissage du point entier à droite du 1er au point entier à gauche du 2e, du 3e au 4e... (règle pair-impair, juste pour une étoile) ; lignes tracées par le firmware au stylo du remplissage. Puis le contour par-dessus, au stylo du contour.
- **32 pointes au plus (au lieu de 100, D151) :** les sommets et les croisements sont gardés dans des cases (3 × 128 octets) ; au-delà, une étoile ressemble à un rond.
- **Vitesse :** environ 330 000 instructions pour une étoile à 5 pointes de 160 lignes de haut avec intérieur (contour seul : 15 000) ; le plus long : les multiplications et divisions de chaque croisement, et la lecture des sommets. À accélérer si l'essai dans WinAPE le montre (croisements suivis d'une ligne à la suivante sans division).
- **Vérification :** `essais\star\test_star2.py` : 312 étoiles (3 MODES, centres hors de l'écran, angles et rayons au hasard), chaque morceau de chaque ligne identique à la même règle calculée en fractions exactes ; tests C# : StarTests.cs (5 de plus : intérieur comparé à la règle écrite en nombres exacts). MiniZ80 : LD BC,(nn) et SRA H ajoutés.
- **Exemple :** examples\etoiles.bas, nouvel écran « ETOILES REMPLIES ».
- **Essayé dans WinAPE (19 nov. 2026) :** quatre étoiles remplies (5 et 8 pointes, hexagone, 4 pointes tournée de 45 degrés) : intérieur et contour justes, sans trou ; « le remplissage est un peu lent, sinon c'est super cette instruction » : accélération à faire.

## D153 : intérieur de STAR plus rapide ; 16 pointes au plus (19 nov. 2026) (choix 2 de l'utilisateur) ; remplace la méthode et la limite de D152
- **Demande de l'utilisateur :** « le remplissage est un peu lent » (D152). Choix proposés : rapide avec 32 pointes (environ 600 octets de plus), rapide avec 16 pointes (presque rien de plus), ne rien changer ; **choix 2**.
- **Méthode :** chaque bord est noté une fois (13 octets : y1 et y2 gardés entre 0 et 200, x, reste, Q, R, y2 - y1, signe) avec de combien il se décale d'une ligne à la suivante (Q + R / (y2 - y1), une seule division par bord) ; ligne par ligne, chaque bord qui croise la ligne donne sa clé puis avance par des additions (plus de multiplication ni de division par ligne) ; comparaisons sur un octet. Un bord qui commence au-dessus de l'écran est avancé d'un coup jusqu'à la ligne 0. Les sommets ne sont plus gardés : ils sont recalculés pour le contour (environ 15 000 instructions).
- **Gain :** étoile à 5 pointes de 160 lignes avec intérieur : environ 96 000 instructions au lieu de 330 000 (3,4 fois moins) ; le tracé des lignes par le firmware n'a pas changé (il compte maintenant pour une bonne part du temps : une écriture directe dans l'écran serait l'étape suivante, si besoin).
- **Place :** listes de 416 (bords) et 64 octets (croisements) au lieu de 3 × 128.
- **Vérification :** test_star2.py : 312 étoiles toujours identiques à la règle exacte ; MiniZ80 : LD C,(IX+d), LD (IX+d),L, LD (IX+d),H, CP (IX+d), ADD IX,DE ajoutés.
- **Essayé dans WinAPE (19 nov. 2026) :** examples\etoiles.bas, écran « ETOILES REMPLIES » : « c'est bien plus rapide ! Super ». 1835 tests réussis.

## D154 : l'éditeur colore les commandes des bibliothèques, étape B4 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur)
- **Ce qui change :** les commandes et les fonctions du catalogue (pas les routines, qui ne servent qu'en assembleur) prennent la couleur des nouvelles commandes du projet ; catalogue lu à l'ouverture de l'éditeur, puis relu à chaque F5, Maj+F5 et F4 (une commande ajoutée est colorée dès la compilation suivante).
- **Liste fixe de l'éditeur :** STAR en est retirée (elle vient maintenant du catalogue) ; POLYGON aussi (abandonnée, D150).
- **Fichiers :** `src\CpcIde\BasicColorizer.cs` (LibraryWords), `src\CpcIde\MainWindow.axaml.cs` (UseLibraries).

## D155 : liste des bibliothèques (Maj+F4), étape B4 (19 nov. 2026) (choix laissés à l'assistant par l'utilisateur)
- **Où :** nouveau menu « Bibliothèques », « Liste des bibliothèques », raccourci **Maj+F4** (à côté de F4, la liste des variables ; F6, F10 et F12 étaient pris ou gênants : F6 passe aux messages, F10 ouvre le menu de Windows, F12 sert aux outils de mise au point d'Avalonia).
- **Comme F4 :** la liste s'affiche dans le panneau des messages (lisible par un lecteur d'écran) : les deux dossiers (projet, protégé ; le vôtre, Documents\CPC BASIC IDE\bibliotheques, « pas encore créé » s'il n'existe pas), puis pour chaque bibliothèque ses commandes : bonne écriture (« STAR x, y, r1, r2, pointes [, stylo] [, remplissage] [, angle] »), genre, du projet (protégée) ou à vous, auteur, lignes d'aide, et chaque valeur avec ses limites, sa valeur si elle est sautée et son rôle ; à la fin, les fichiers refusés.
- **Code :** la bonne écriture vient de `LibraryCommandFile.Signature` (Core), utilisée aussi par les messages d'erreur du compilateur.
- **F1 :** ligne des raccourcis complétée.

## D156 : fenêtre des bibliothèques (19 nov. 2026) (idée de l'utilisateur) ; remplace l'affichage de D155 dans le panneau des messages
- **Remarque de l'utilisateur :** avec beaucoup de commandes, la liste dans les messages deviendrait illisible ; « une fenêtre détachée avec en haut Graphique-Texte-Bruitage-etc et en dessous les commandes avec une barre défilante à droite ».
- **Fenêtre (Maj+F4 ou menu Bibliothèques) :** un onglet par bibliothèque ; à gauche, ses commandes (nom et genre, barre de défilement toujours visible) ; à droite, la commande choisie : bonne écriture, genre, protégée ou à vous, auteur, aide, chaque valeur (limites, valeur si sautée, rôle) ; onglet « Fichiers refusés » s'il y en a ; en bas, les deux dossiers et « Fermer (Échap) ». Texte de 15 points au départ (18 au premier essai : « un peu trop grosse ») ; Ctrl et + / Ctrl et - dans la fenêtre pour le changer, taille gardée dans les réglages (LibraryFontSize). Fenêtre à part : l'éditeur reste utilisable ; redemandée, elle est remise à jour et passe devant (onglet gardé).
- **Essayé (19 nov. 2026) :** « ça marche très bien comme ça ».
- **Fichier :** `src\CpcIde\LibraryWindow.cs` ; plus tard : boutons « Nouvelle commande », modifier, effacer.

## D157 : questionnaire « Nouvelle commande », étape B4 (20 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; applique D144
- **Où :** bouton « Nouvelle commande... » en bas de la fenêtre des bibliothèques (Maj+F4).
- **Questionnaire :** nom ; genre (commande, fonction, routine) ; bibliothèque (proposée : l'onglet ouvert ; la liste des bibliothèques existantes est rappelée ; un nom nouveau crée une bibliothèque) ; aide (plusieurs lignes) ; auteur ; valeurs, une ligne chacune : nom, de, à, case « facultative » et ce que reçoit la routine quand elle est sautée (-1 proposé), rôle, bouton « Retirer » ; bouton « Ajouter une valeur » (16 au plus) ; une routine n'a pas de valeurs (partie grisée, avec l'explication). Créer (Entrée) / Annuler (Échap). Texte à la taille de la fenêtre des bibliothèques.
- **Vérifications :** nom donné et pas déjà au catalogue (le message nomme la bibliothèque qui l'a), nom de bibliothèque correct, nombres entiers de -32768 à 65535 (&FF accepté) ; puis le texte du fichier est relu par le lecteur du compilateur (mêmes règles que pour un fichier écrit à la main : mot du BASIC, _, FN, registres, ordre des facultatives...), ses messages sont montrés en rouge sous le questionnaire.
- **Fichier écrit :** `Documents\CPC BASIC IDE\bibliotheques\BIBLIOTHÈQUE\NOM.cmd.asm` (dossiers créés au besoin, UTF-8 sans BOM), avec un code de départ : commentaires (comment lire les valeurs, LD HL,(X) ; résultat d'une fonction dans HL ; registres d'une routine) puis RET. La fenêtre des bibliothèques se met à jour et montre la commande créée ; ses couleurs dans l'éditeur aussi.
- **Core :** `LibraryCommandFile.Compose` (texte du fichier à partir des réponses) et `StarterCode` ; tests : 3 de plus dans LibraryCommandTests.cs (le fichier se relit sans faute et son code de départ s'assemble).
- **Essayé (20 nov. 2026) :** commande ESSAI créée dans la bibliothèque ESSAIS : « l'ensemble marche plutôt bien » ; trois remarques de l'utilisateur, voir D158.
- **Pas encore :** écrire et vérifier le code assembleur dans l'éditeur (étape suivante) ; modifier ou effacer une commande.

## D158 : questionnaire plus clair (20 nov. 2026) (idées de l'utilisateur) ; complète D157
- **Repère (idée 1) :** un cadre rouge sous le haut du questionnaire montre l'écriture en BASIC, mise à jour à chaque frappe : « Écriture en BASIC : LUNE x [, stylo] », « A = LUNE(x) » pour une fonction, « CALL LUNE » pour une routine ; un nom qui ne convient pas (par exemple « Axe X », avec une espace) y est marqué «Axe X»? avec la raison (« À corriger : ... »), avant même de cliquer sur Créer.
- **Titres gris (idée 2) :** au-dessus des cases de chaque valeur : « nom (ex : X, STYLO) », « de », « à », « si sautée », « rôle (ex : couleur du contour) ».
- **Le -1 (question 3) :** expliqué en clair sous le titre des valeurs : une valeur facultative peut être sautée en BASIC, sa case reçoit alors le nombre « si sautée » ; hors des limites (-1), il dit à la routine que la valeur a été sautée (STAR prend alors le stylo du moment) ; dans les limites (0), c'est une valeur par défaut. Le nom d'une valeur : lettres sans accent, chiffres et _, sans espace (c'est aussi le nom de sa case en assembleur).

## D159 : éditeur d'assembleur des commandes (20 nov. 2026) (choix laissés à l'assistant par l'utilisateur) ; suite de D157
- **Où :** bouton « Modifier le code... » de la fenêtre des bibliothèques, pour la commande choisie ; « Voir le code... » pour une commande du projet (protégée : lecture seule, on peut seulement vérifier ; utile pour apprendre en regardant STAR). Une fenêtre à part par commande (on peut en ouvrir plusieurs).
- **Ce qu'on y écrit :** seulement le code ; la fiche (lignes « ;@ » du haut du fichier) est gardée telle quelle et rappelée en haut : écriture en BASIC, cases des valeurs (« par exemple LD HL,(X) »), ce que doit faire le code (RET ; résultat d'une fonction dans HL ; registres d'une routine). Même papier bleu et même police que l'éditeur BASIC ; couleurs : étiquette rose, instruction bleu clair, nombres blancs, textes verts, commentaires gris (AsmColorizer).
- **Vérifier (F5) :** la fiche relue puis le code assemblé comme par le compilateur (valeurs, la commande elle-même et les commandes et routines du catalogue reconnues), sauts courts trop longs compris ; liste des fautes avec la ligne du code (Entrée ou double clic : aller à la ligne) ; sans faute : taille du code en octets, cases des valeurs en plus, routines appelées.
- **Enregistrer (Ctrl+S) :** écrit le fichier (fiche + code, UTF-8 sans BOM), relit les bibliothèques (fenêtre et couleurs de l'éditeur à jour) et vérifie ; une étoile dans le titre tant que ce n'est pas enregistré ; Fermer (Échap) avec des changements : la question habituelle (enregistrer, ne pas enregistrer, annuler).
- **Core :** chaque commande garde le chemin de son fichier (`LibraryCommand.FilePath`, `CatalogSource.Path`).
- **Essayé (20 nov. 2026) :** STAR vue et vérifiée (« Aucune faute. Code : 2174 octets, plus 16 pour les cases des valeurs ») ; LUNE : faute ENCRES trouvée à la ligne 4 ; « c'est titanesque ! ». Petite faute corrigée ensuite : « Appelle aussi : . » écrit quand le code n'appelle aucune routine.
- **Taille réelle de STAR (mesurée ici) :** 2174 octets avec l'intérieur (dont 480 de listes et 184 de table des sinus) ; les estimations données plus tôt (environ 600 octets pour le contour seul, « presque rien de plus » pour D153) étaient trop basses : la partie intérieur (D152, D153) pèse à elle seule plus de 1000 octets.
- **Pas encore :** « Modifier la fiche » et « Effacer ».

## D160 : modifier la fiche, effacer une commande (20 nov. 2026) (demande de l'utilisateur ; façon de faire choisie par l'assistant) ; termine D157 et D159
- **Demande de l'utilisateur :** « il faudra prévoir la possibilité de modifier/supprimer une commande ».
- **« Modifier la fiche... » :** le questionnaire de D157, déjà rempli (nom, genre, bibliothèque, aide, auteur, valeurs) ; « Enregistrer » ; le code assembleur est gardé tel quel ; nom ou bibliothèque changé : le fichier est écrit à sa nouvelle place puis l'ancien est supprimé (refusé si un fichier existe déjà à la nouvelle place) ; même vérifications que pour une nouvelle commande (un nom déjà pris par une autre commande est refusé, le sien non).
- **« Effacer » :** question « Effacer la commande X de la bibliothèque Y ? Son fichier sera supprimé : ... » avec « Garder » choisi au départ (comme D118) ; le dossier d'une bibliothèque devenue vide est supprimé aussi.
- **Protection :** ces deux boutons ne marchent que pour les commandes du programmeur ; grisés pour celles du projet.
- **Fenêtre :** les boutons du bas passent à la ligne si la fenêtre est étroite (Modifier le code, Modifier la fiche, Effacer, Nouvelle commande, Fermer).
- **Core :** `LibraryCommandFile.SplitHeader` (fiche et code séparés), utilisé aussi par l'éditeur d'assembleur ; 1 test de plus.
- **Essayé (20 nov. 2026) :** STAR grisée ; fiche de LUNE modifiée, code gardé ; ESSAI effacée : « tout marche comme tu l'as prévu ! C'est parfait ».

## D161 : répertoire de distribution, étape B5 (20 nov. 2026) (idée de l'utilisateur, D144 ; façon de faire choisie par l'assistant)
- **Où :** menu Fichier, « Préparer un dossier à distribuer... » ; le programme est d'abord enregistré, puis on choisit un dossier (pas celui du programme).
- **Ce qui est déposé :** le texte du programme (.BAS, copié tel quel) et celui des programmes qu'il lance par RUN "nom" trouvés dans son dossier (comme F5) ; les commandes, fonctions et routines **du programmeur** qu'ils utilisent, avec les routines qu'elles appellent, rangées comme chez soi (`bibliotheques\BIBLIOTHÈQUE\NOM.cmd.asm`) ; un LISEZMOI.txt (comment compiler, où copier le dossier bibliotheques : Documents\CPC BASIC IDE\bibliotheques, la liste des commandes données, et les commandes du projet utilisées). Les commandes du projet ne sont pas copiées (tout le monde les a).
- **Compte rendu :** dans le panneau des messages : le dossier, les fichiers déposés, les commandes du projet utilisées, les RUN "nom" introuvables (en rouge).
- **Core :** `Distribution.Plan` (src\CpcBasic.Core\Libraries\Distribution.cs) ; tests : DistributionTests.cs (3).
- **Essayé (20 nov. 2026) :** essais\lunejeu.bas (LUNE et STAR) déposé dans essais\distrib : lunejeu.bas, LISEZMOI.txt, bibliotheques\ESSAI\LUNE.cmd.asm ; STAR signalée, pas copiée : « c'est tout comme tu le dis ». 1842 tests réussis.
- **Étapes B1 à B5 de D144 : toutes faites.** Reste : choisir l'extension des fichiers de commande (provisoire .cmd.asm), d'autres bibliothèques du projet.

## D162 : préparation de la version 0.5.0 (20 nov. 2026) (demandée par l'utilisateur)
- **Contenu :** bibliothèques de commandes (D144 à D161) et STAR ; notes dans `docs\RELEASE-NOTES-0.5.0.md`.
- **Fichiers :** version 0.5.0 dans `Directory.Build.props` et `packaging\build-release.cmd` ; `packaging\LISEZMOI.txt` (titre resté à 0.3.0 corrigé ; ligne périmée sur les lettres accentuées retirée ; menus Bibliothèques et dossier à distribuer ; nouveau point 6 « Bibliothèques de commandes » ; ajouts du projet complétés) ; README et TESTEURS.md ; le dossier `bibliotheques` est aussi copié dans le paquet publié (CopyToPublishDirectory).
- **D016 :** aucune ROM dans le paquet (exemples .bas, documents, bibliothèque GRAPHIQUE seulement).
- **Idée notée pour après (IDEES.md) :** « Assembleur seul » (question de l'utilisateur).
- **Paquet construit (20 nov. 2026) :** `release\CpcBasic-0.5.0-win-x64.zip`, SHA256 7560edacc72a78fd07117dea2b0b48e632d8e20102fc961b0b34c2084a0cd7b6 (même somme chez l'utilisateur et ici) ; contenu vérifié (bibliotheques\GRAPHIQUE\STAR.cmd.asm, exemples dont etoiles.bas, pas de ROM) ; essayé par l'utilisateur : etoiles.bas compilé depuis le paquet, « Bibliothèques utilisées : GRAPHIQUE (STAR) », 5543 octets.
- **Publiée (20 nov. 2026) :** pre-release GitHub v0.5.0, « CPC BASIC IDE 0.5.0 (version de test) », commit 885ded1.

## D163 : « assembleur seul », le cœur (20 nov. 2026) (idée de l'utilisateur ; choix : nom du fichier .asm, ORG de &0400 à &A67B par défaut)
- **Idée de l'utilisateur :** un menu pour écrire un programme entièrement en assembleur, l'enregistrer en .asm et le compiler sur une disquette ; ORG obligatoire.
- **Façon de WinAPE (vérifiée sur cpcwiki) :** `org &8000`, `run &8000` (adresse de lancement), `write direct "a:nom.bin"` (où écrire le résultat).
- **Choix de l'utilisateur :** le nom vient du fichier : jeu.asm donne JEU.BIN (« comme les autres, la source .asm et le compilé en .bin ») ; WRITE est acceptée sans effet (un texte de WinAPE se colle sans changement). Adresses permises pour ORG : pas de réponse, choix conseillé appliqué et annoncé : de &0400 à &A67B (au-dessous, le chargeur BASIC serait écrasé ; au-dessus, la mémoire du système).
- **Règles :** ORG au début (avant la première instruction), une seule fois ; RUN facultatif (étiquette ou nombre ; sinon l'adresse de ORG), une seule fois ; le programme ne doit pas dépasser &A67B ; les commandes et routines des bibliothèques appelées par leur nom sont ajoutées après le code (avec celles qu'elles appellent).
- **Disquette :** NOM.BIN (chargement à ORG, lancement à RUN) et NOM.BAS (MEMORY ORG-1, LOAD, CALL lancement) : RUN"NOM" le lance ; un RET à la fin revient au BASIC.
- **Code :** `Z80Assembler` (ORG, RUN, WRITE permis seulement avec `StandaloneInfo` ; refusés dans une commande de bibliothèque comme avant), `AsmProgram` (Build et ToDisk) ; tests : AsmProgramTests.cs (14).
- **Ensuite :** la fenêtre et le menu « Assembleur » (nouveau, ouvrir, enregistrer, vérifier, compiler).

## D164 : fenêtre « Assembleur seul » (20 nov. 2026) (façon de faire choisie par l'assistant) ; suite de D163
- **Où :** nouveau menu « Assembleur » de l'éditeur : « Nouveau programme assembleur », « Ouvrir un programme assembleur... » ; une fenêtre par programme.
- **Fenêtre :** barre de menus Fichier (Nouveau Ctrl+N, Ouvrir Ctrl+O, Enregistrer Ctrl+S, Enregistrer sous Ctrl+Maj+S, Fermer Échap) et Compiler (Compiler F5, Vérifier Maj+F5), comme l'éditeur BASIC ; même papier bleu, même police et mêmes couleurs que l'éditeur des bibliothèques ; liste des fautes (Entrée ou double clic : aller à la ligne) ; titre avec une étoile tant que ce n'est pas enregistré, question avant de perdre des changements.
- **Nouveau programme :** un petit exemple déjà écrit (ORG &8000, RUN DEBUT, affiche « BONJOUR DE L'ASSEMBLEUR ! » par TXT OUTPUT, RET).
- **Compiler (F5) :** enregistre le .asm, puis écrit NOM.dsk à côté (NOM.BIN et NOM.BAS) ; compte rendu : adresses de début et de fin, taille, adresse de lancement, routines des bibliothèques ajoutées, « RUN"NOM" » ; nom de 8 caractères au plus ; .dsk ouvert dans l'émulateur : message clair. Vérifier (Maj+F5) : même chose sans rien écrire.
- **Fichiers :** `src\CpcIde\AsmProgramWindow.cs` (nouveau), MainWindow.axaml et .cs (menu).
- **Essayé dans WinAPE (20 nov. 2026) :** l'exemple vérifié (&8000 à &8027, 40 octets), compilé en essais\bonjour.dsk (BONJOUR.BAS et BONJOUR.BIN, 1K chacun) ; RUN"BONJOUR" affiche « BONJOUR DE L'ASSEMBLEUR ! » puis Ready (retour au BASIC par RET).

## D165 : extension des fichiers de commande : .cmd.asm, définitive (20 nov. 2026) (choix de l'utilisateur) ; tranche la question laissée ouverte par D144 et D146
- **Choix proposés :** .asm (le plus simple), .cmd.asm (garder), .bib (court et français) ; **choix 2 : garder .cmd.asm** : on voit tout de suite que c'est une commande, et c'est de l'assembleur (tout éditeur de texte l'ouvre).
- **Rien ne change dans le code** (`LibraryCommandFile.Extension`) ; ce n'est plus provisoire.

## D166 : « assembleur seul » sans chargeur BASIC (20 nov. 2026) (idée de l'utilisateur ; façon de faire choisie par l'assistant) ; complète D163 et D164
- **Essai dans WinAPE :** `RUN"BONJOUR.BIN"` lance bien le programme (l'en-tête AMSDOS porte l'adresse de lancement), mais le CPC abandonne le BASIC : le `RET` final fait redémarrer la machine. `essais\bonjour2.asm` (attente sans fin au lieu de `RET`) : le texte reste affiché. Vérifié par l'utilisateur.
- **Choix :** une case « Avec chargeur BASIC » dans la fenêtre « Assembleur seul », cochée au départ. Décochée : la disquette n'a que `NOM.BIN` ; `RUN"NOM"` le trouve seul (AMSDOS essaie NOM, puis NOM.BAS, puis NOM.BIN). Le choix est gardé tant que l'éditeur reste ouvert.
- **Cœur :** `AsmProgram.ToDisk(program, name, withLoader = true)` ; un test de plus.
- **Vérifié par l'utilisateur dans WinAPE :** case décochée, la disquette n'a que BONJOUR2.BIN et `RUN"BONJOUR2"` le lance. (Si la disquette est ouverte dans l'émulateur, l'écriture est refusée avec un message clair : fermer, puis recompiler.)

## D167 : la zone du programmeur, &0400 à &2FFF (20 nov. 2026) (idée de l'utilisateur ; adresses proposées par l'assistant, acceptées par l'utilisateur)
- **Règle, pour toujours :** dans un programme BASIC compilé, le compilateur ne met jamais rien de &0400 à &2FFF (11 Ko), ni aujourd'hui ni plus tard. Le programmeur y range des POKE, des données (LOAD"DONNEES",&1000) ou des routines en assembleur.
- **&3000 à &3FFF (4 Ko) :** libre aussi, sauf si le programme utilise les fichiers (zones du firmware en &3000 et &3800, D084) ou RUN"nom" (petit chargeur en &3800, D086).
- **&0000 à &03FF :** réservé (système du CPC ; le petit chargeur BASIC en &0170, qui doit encore exister quand le programme revient au BASIC).
- **Gardien :** `tests\CpcBasic.Tests\ProgrammerZoneTests.cs` : les adresses du compilateur restent au-dessus de la zone, et des programmes qui tournent (textes, tableaux, GOSUB) laissent intact un motif écrit dans la zone. Si un de ces tests échoue un jour, c'est le compilateur qu'il faut changer.
- **Écrit pour l'utilisateur :** `packaging\LISEZMOI.txt`, point 5.

## D168 : programme BASIC compilé sans chargeur BASIC (20 nov. 2026) (idée de l'utilisateur ; façon de faire choisie par l'assistant) ; suite de D166
- **Constat :** un programme compilé ne revient jamais au BASIC (fin : une touche, puis redémarrage). `RUN"CERCLES.BIN"` marche tel quel. Mais `RUN"FICHIERS.BIN"` demande la cassette (« Press REC and PLAY ») : lancé ainsi, le firmware est remis à zéro et la disquette (AMSDOS) n'est plus branchée. Essais de l'utilisateur dans WinAPE.
- **Correctif :** réglage `CompilerOptions.WithLoader` (vrai par défaut). Faux : au départ, si l'entrée CAS IN OPEN (&BC77) est encore celle du firmware (&CF, RST 1), le programme appelle KL ROM WALK (&BCCB) avec DE = &0040 et HL = &ABFF, comme le BASIC (AMSDOS reprend le haut, HIMEM &A67B). Déjà branchée (lancé par RUN "nom") : rien. 16 octets de plus, seulement sans chargeur.
- **Disquette :** sans chargeur, NOM.BIN seul (aussi pour les programmes lancés par RUN "nom" et compilés avec lui) ; RUN"NOM" le trouve (AMSDOS essaie NOM, NOM.BAS, puis NOM.BIN).
- **Éditeur :** menu Options, « Chargeur BASIC sur la disquette (NOM.BAS) », coché au départ, commun à tous les programmes (`EditorSettings.WithLoader`).
- **Vérifié par l'utilisateur dans WinAPE :** CERCLES, FICHIERS (écriture et lecture d'un fichier), PARTIE1 puis PARTIE2 (RUN "nom" et fichier SCORE) : tout marche sans chargeur.
- **Tests :** `NoLoaderTests` (5).

## D169 : une valeur TEXTE dans les bibliothèques (20 nov. 2026) (demande de l'utilisateur, pour sa commande TYPO ; façon de faire choisie par l'assistant)
- **Pourquoi :** l'utilisateur veut mettre sa RSX TYPO (petits caractères de 4×8, 2020) dans une bibliothèque TEXTE ; elle reçoit un texte. Forme choisie par l'utilisateur : `TYPO texte [, x, y]` et `DEFTYPO "x", xx, xx, xx` (étapes suivantes).
- **Fiche :** `;@TEXTE NOM, rôle` ; toujours obligatoire, donc avant les OPTION. `LibraryParameter.IsText`. Écriture montrée : `message$`.
- **Ce que reçoit la routine :** dans la case, l'adresse de la fiche du texte (1 octet : longueur ; 2 octets : adresse des lettres), comme `@A$` pour une RSX : le code d'une RSX existante se garde. Le texte peut être écrit, une variable, une case de tableau ou un calcul (A$ + "!", LEFT$...) : une fiche temporaire, valable le temps de l'instruction.
- **Vérification :** un nombre à la place d'un TEXTE, ou l'inverse : « un texte et un nombre ne se mélangent pas ».
- **Éditeur :** case « texte » sur chaque valeur du questionnaire ; fenêtre des bibliothèques : « un texte ».
- **Tests :** `LibraryTextTests` (14).

## D170 : TYPO, première commande de la bibliothèque TEXTE (20 nov. 2026) (idée et fonte de l'utilisateur, RSX TYPO de 2020 ; forme choisie par l'utilisateur : `TYPO texte [, x, y]`)
- **Fichiers :** `bibliotheques\TEXTE\TYPO.cmd.asm` (commande), `TYPO_ADRESSE.cmd.asm` (routine : A = code -> HL = ses 3 octets), `TYPO_FONTE.cmd.asm` (routine qui n'est que des données : les 93 petits caractères de l'utilisateur, recopiés à l'identique).
- **Principe gardé :** deux petits caractères de 4×8 (3 octets, 6 lignes) forment le caractère 255 (poids fort du 1er, poids faible du 2e ; lignes 1 et 8 vides), écrit par GRA WR CHAR au curseur graphique : les 3 MODE, tous les CPC ; un nombre impair finit par un espace ; un caractère inconnu devient un espace.
- **Changements :** adresse calculée (table + 3 × numéro) au lieu d'une boucle ; plus d'IX / IY ; le dessin de 255 est gardé puis remis ; si 255 n'est pas redessinable (programme sans chargeur), TYPO lui donne une petite table (TXT SET M TABLE) ; x et y facultatifs, chacun (sautée : celle du curseur graphique) ; couleurs par GRAPHICS PEN / PAPER (`|ENCRE` n'est plus utile).
- **Vérifié par l'utilisateur dans WinAPE :** `examples\typo.bas`, MODE 1 (80 colonnes, trois couleurs, à é ù, A$, suite au curseur, STR$), MODE 0 (40 colonnes, 15 couleurs ; l'encre 14 clignote, d'où une ligne parfois invisible), MODE 2 (160 colonnes).
- **Tests :** `TypoTests` (8) ; MiniZ80 : GRA WR CHAR, retenue de TXT GET MATRIX, LD A,(BC), INC BC, RRCA, RLCA, BIT 7,B, LD D,B, LD E,C.
- **Reste :** DEFTYPO (forme choisie par l'utilisateur : `DEFTYPO "x", xx, xx, xx`).

## D171 : DEFTYPO (20 nov. 2026) (idée de l'utilisateur, RSX DEFTYPO de 2020 ; forme choisie par l'utilisateur : `DEFTYPO "x", xx, xx, xx`, comme SYMBOL)
- **Fichier :** `bibliotheques\TEXTE\DEFTYPO.cmd.asm` : TEXTE CARACTERE (le premier caractère compte), VALEUR L1, L2, L3 (0 à 255) ; les 3 octets remplacent ceux du caractère dans TYPO_FONTE (par TYPO_ADRESSE), jusqu'à la fin du programme.
- **Différence avec la RSX :** plus de texte hexadécimal à décoder ("x=F99F66") ; un caractère hors de la fonte (avant l'espace, après ù) ou un texte vide est ignoré (sinon TYPO_ADRESSE aurait redessiné l'espace).
- **Vérifié par l'utilisateur dans WinAPE :** `typo.bas`, ligne 145, trois cœurs à la place de ###.
- **Tests :** `TypoTests` (+4). La bibliothèque TEXTE (TYPO, DEFTYPO) est terminée.

## D172 : TYPOVITE, écriture directe dans l'écran, et place des commandes dans la fenêtre des bibliothèques (20 nov. 2026) (demande de l'utilisateur : plus rapide, avec mesure de la vitesse et de la place avant de remplacer TYPO)
- **Place :** `LibraryLinker.Size` : la commande seule (cases et code) et avec les routines qu'elle appelle ; montrée dans la fenêtre des bibliothèques (« Place dans un programme : … octets »).
- **Principe :** sans GRA WR CHAR ni caractère 255 : les 8 lignes sont faites dans 8 octets à elle, puis écrites dans l'écran : 4 octets par ligne en MODE 0, 2 en MODE 1, 1 en MODE 2 ; octet = fond XOR ((stylo XOR fond) AND masque). Le firmware ne sert plus qu'à se repérer (curseur, origine, MODE, couleurs, adresse du premier caractère).
- **Première version (gardée : `essais\TYPOVITE-v1.cmd.asm.txt`) :** SCR NEXT LINE à chaque ligne et un aiguillage par ligne : 480 octets, 2 fois plus rapide seulement en MODE 1 (501 contre 1046 trois-centièmes de seconde, 20 lignes de 63 caractères).
- **Version optimisée :** ligne suivante et octet suivant calculés ici (écran de 16 Ko en &C000, 8 blocs de 2 Ko qui tournent sur eux-mêmes : le défilement est suivi), une routine par MODE choisie une fois, couleurs écrites dans le code (AND / XOR) : 613 octets (911 avec la fonte et TYPO_ADRESSE ; TYPO par le firmware : 229 et 527).
- **Mesures de l'utilisateur dans WinAPE** (`essais\typocomp\typocomp.bas`, 20 lignes de 40 caractères, en trois-centièmes de seconde) : MODE 0 : 672 contre 258 (2,6 fois plus vite) ; MODE 1 : 656 contre 148 (4,4) ; MODE 2 : 649 contre 101 (6,4).
- **Différences acceptées :** x arrondi vers la gauche à un multiple de 8 ; fenêtre graphique et mode de GRAPHICS PEN pas suivis ; caractère hors de l'écran pas écrit ; écran en &C000.
- **MiniZ80 :** LD (nn),BC, EX (SP),HL, RLC C, XOR C ; et toutes les instructions LD r,r' et calculs A,r décodées d'après le numéro du registre (51 manquaient).
- **Remarque de l'essai :** `ROUND(T1/T2,1)` a affiché 2 au lieu de 2.1 : la division de deux variables entières reste entière ici, alors que le Locomotive donne un nombre à virgule. À vérifier (IDEES).

## D173 : TYPOVITE devient TYPO (20 nov. 2026) (choix de l'utilisateur : « y a pas photo »)
- L'ancien TYPO (par GRA WR CHAR et le caractère 255, D170) est retiré, gardé dans `essais\TYPO-v1-firmware.cmd.asm.txt` ; TYPOVITE prend le nom TYPO : même écriture, `TYPO texte [, x, y]`, mêmes couleurs, même fonte, DEFTYPO inchangé.
- **Tests :** `TypoTests` réécrits sur les points de l'écran (16).

## D174 : « / » entre deux entiers reste entier (20 nov. 2026) (choix de l'utilisateur : « la rapidité est une priorité ») ; confirme D037
- Question venue de D172 (`ROUND(T1/T2,1)` affichait 2). Choix proposés : garder ; faire comme le Locomotive (toujours à virgule) ; un réglage. **Choix 1 : garder.**
- Pour une division à virgule sans F8 : `CREAL(A)/B`, une variable à virgule (A!), ou le réglage F8. Écrit dans `packaging\LISEZMOI.txt` (point 5).

## D175 : FPRINT, affichage rapide du texte (20 nov. 2026, en cours) (idée et RSX de l'utilisateur : SPEED1 MODE 1, 2024 ; RAPIDO MODE 2, 2026 ; forme choisie par l'utilisateur : `FPRINT texte$`, sans autre valeur ; couleurs de PEN et PAPER)
- **Fichier :** `bibliotheques\TEXTE\FPRINT.cmd.asm` : au curseur du texte (TXT GET CURSOR, VALIDATE, fenêtre), puis curseur après le texte ; dessins lus en &3800 + 8 x code (ROM basse ouverte une fois) ou dans la table de SYMBOL AFTER (TXT GET M TABLE une fois) ; 8 lignes de &800 en &800 (SPEED1), MODE 2 déroulé par les bits 3 à 5 de H (RAPIDO) ; coupé au bord droit, pas de défilement.
- **Erreur de l'assistant :** il avait d'abord ajouté `[, col, lig]` ; l'utilisateur avait demandé `FPRINT texte$` seul (« plus de paramètres » = plus aucune valeur). Corrigé.
- **MiniZ80 :** TXT GET / SET PEN et PAPER, SCR CHAR POSITION, TXT GET M TABLE, tous les BIT / RES / SET n,r.
- **Tests :** 1904 réussis (21 nov. 2026).
- **Mis de côté (choix de l'utilisateur, 21 nov. 2026) :** « ajouter des commandes se fera après que tout soit terminé ». FPRINT et ses tests sont rangés dans `essais\fprint\` ; à reprendre plus tard (essai dans WinAPE, mesure de la vitesse). Les ajouts au simulateur restent.

## D176 : un Z80 complet pour les tests (21 nov. 2026) (idée de l'utilisateur : « pourquoi ne pas mettre tous les opcodes du Z80, vraiment tous, d'un coup ? »)
- **Constat :** MiniZ80 avait été complété instruction par instruction ; beaucoup d'« échecs » de tests venaient d'une instruction manquante, pas du code testé.
- **Fait :** `tests\CpcBasic.Tests\MiniZ80.Core.cs` : toutes les instructions, décodées par les bits du code (x, y, z, p, q) ; préfixes CB, ED, DD, FD, DDCB, FDCB ; non documentées (IXH, IXL, IYH, IYL, SLL, DDCB qui recopie dans un registre, ED sans effet) ; tous les indicateurs (S, Z, H, P/V, N, C, bits 3 et 5) ; registres de rechange (EX AF,AF', EXX), IY, I, R, mode d'interruption ; ports (IN, OUT, INI, OTIR...). Les répétitions (LDIR, CPIR...) en une seule étape, comme avant. Toutes les lectures de données passent par la ROM haute quand elle est ouverte.
- **Gardé :** l'imitation du firmware, reprise sans réécriture dans `CallTo` (CALL, et maintenant aussi CALL cc) et `JumpTo` (JP) ; MiniZ80.cs ne garde que la mémoire, la pile et le firmware (1592 -> 1083 lignes).
- **Résultat :** les 1896 tests passent du premier coup (les 8 tests de FPRINT sont rangés avec lui, D175) ; `Z80CoreTests` (8) vérifie des indicateurs et des instructions connus (DAA, SBC HL, IXH / IYL, DDCB, EXX, CPIR, RLD, NEG).

## D177 : préparation de la version 0.6.0 (21 nov. 2026) (demandée par l'utilisateur)
- **Contenu :** assembleur seul (D163, D164, D166), lancement sans chargeur BASIC (D168), zone réservée au programmeur (D167), valeur TEXTE des bibliothèques (D169), place des commandes (D172), bibliothèque TEXTE : TYPO et DEFTYPO (D170 à D173), astuce CREAL (D174) ; notes dans `docs\RELEASE-NOTES-0.6.0.md`.
- **Fichiers :** version 0.6.0 dans `Directory.Build.props` et `packaging\build-release.cmd` ; `packaging\LISEZMOI.txt` (titre, menu Assembleur, réglage chargeur, nouveau point 7 « Assembleur seul », points suivants renumérotés) ; README et TESTEURS.md.
- **Incident :** `packaging\build-release.cmd` avait été remplacé par la sortie d'une série de tests (redirection) ; remis depuis le commit 885ded1, avec la version 0.6.0 et des fins de ligne Windows.
- **Paquet construit (21 nov. 2026) :** `release\CpcBasic-0.6.0-win-x64.zip`, SHA256 25cefaff6132f91416253f66db2d694a1fa043ac3d05fd1b9ffdbbb3e01e055e (même somme chez l'utilisateur et ici) ; contenu vérifié : bibliothèques GRAPHIQUE (STAR) et TEXTE (TYPO, DEFTYPO, TYPO_ADRESSE, TYPO_FONTE), 83 exemples dont typo.bas, pas de ROM, pas de FPRINT.
- **Publiée (21 nov. 2026) :** pre-release GitHub v0.6.0, « CPC BASIC IDE 0.6.0 (version de test) », commit 250ab58.

## D178 : sortie cassette, fichier .cdt (21 nov. 2026) (choix de l'utilisateur : la cassette avant la cartouche GX4000)
- **Pourquoi d'abord la cassette :** le programme compilé garde la ROM du CPC (affichage, clavier, calculs à virgule, son) ; seule la sortie change. La GX4000 n'a pas cette ROM : chantier bien plus gros, après.
- **Fait :** `src\CpcBasic.Core\Output\TapeImage.cs` écrit une cassette au format .cdt (TZX 1.20, blocs n° &11), comme le firmware : fichier coupé en morceaux de 2 Ko ; chaque morceau = un en-tête (octet &2C, 64 octets complétés à 256 : nom sur 16 caractères, numéro, premier / dernier morceau, type, longueur, adresses) et les données (octet &16) ; paquets de 256 octets suivis de leur somme de contrôle (CRC-16 CCITT, départ &FFFF, inversée, octet fort d'abord) ; amorce de 2048 bits à 1, un bit à 0, 32 bits à 1 de fin ; vitesse de SPEED WRITE 1 (2000 bauds).
- **Noms sur la bande :** avec chargeur, `NOM` (le chargeur BASIC, lu par RUN"") puis `NOM.BIN` ; sans chargeur (D168), `NOM` seul (le programme).
- **Silences (corrigé après essai) :** entre deux morceaux, le CPC arrête le moteur, le remet en marche et attend environ 2 secondes avant d'écouter ; avec 0,1 s de silence, le morceau 2 était perdu (essai `CAT` : « block 1 Ok » puis plus rien). Maintenant : 2,5 s de silence au début de la bande et après chaque morceau ; 20 ms entre l'en-tête et ses données (le moteur ne s'arrête pas).
- **Éditeur :** F5 écrit aussi `NOM.cdt` à côté de `NOM.dsk`, avec le message : sur un 464, RUN"" ; sur un 664 ou un 6128, ùTAPE (|TAPE en version anglaise) puis RUN"". Pas de réglage nouveau. Aucun mot réservé ajouté.
- **Incident :** le premier envoi de la correction des silences n'était pas arrivé dans le dépôt (fichier resté dans sa première version) ; renvoyé et vérifié par la taille. Rappel : `dotnet test` ne reconstruit pas l'éditeur ; le relancer avec `dotnet run --project src\CpcIde` pour essayer un changement du compilateur.
- **Essais (21 nov. 2026) :** `examples\cassette.bas` (3279 octets, 2 morceaux) chargé depuis la cassette et conforme dans Caprice Forever (CPC 464 sans lecteur de disquette, environ 31 s) et dans WinAPE (6128, ùTAPE). Tests : 1913 réussis (9 nouveaux : `TapeImageTests`).
- **Reste (étapes suivantes de la cassette) :** programme sans chargeur sur cassette (aujourd'hui il rebranche la disquette au départ quand le firmware a été remis à zéro, D168 : à éviter pour la cassette) ; compilation en chaîne RUN "nom" (D087) et fichiers (OPENOUT...) sur cassette ; essai sur un vrai CPC.

## D179 : programme sans chargeur sur cassette (21 nov. 2026) (accord de l'utilisateur : « fais pour le mieux »)
- **Problème :** sans chargeur (D168), le programme rebranche la disquette au départ quand le firmware vient d'être remis à zéro (LD A,(&BC77) / CP &CF / JR NZ,+9 / … / CALL &BCCB). Venu d'une cassette sur un 664 ou un 6128, ses fichiers seraient partis sur la disquette.
- **Fait :** un seul programme compilé ; la disquette garde l'original, la cassette reçoit une copie (`Compiler.TapeCopy`) où JR NZ (&20) devient JR (&18) : le rebranchement est toujours sauté, les fichiers restent sur la cassette, comme sur un 464. Copie inchangée pour un programme avec chargeur. Aucun réglage ni mot nouveau.
- **Exemple :** `examples\cassette.bas` attend maintenant aussi la barre d'espace à la fin (règle D130 oubliée au départ : sans chargeur, le programme fini fait redémarrer le CPC et « FIN DE L ESSAI » s'effaçait aussitôt).
- **Essais (21 nov. 2026) :** sans chargeur, cassette lue par ùTAPE puis RUN"" dans WinAPE (6128) : écran conforme, « FIN DE L ESSAI » affiché jusqu'à la barre d'espace. Tests : 1917 réussis (4 nouveaux dans `TapeImageTests`).
- **Reste pour la cassette :** compilation en chaîne RUN "nom" (D087) et fichiers (OPENOUT...) sur cassette ; essai sur un vrai CPC.

## D180 : RUN "nom" sur cassette (21 nov. 2026) (accord de l'utilisateur : « fais pour le mieux »)
- **Constat :** RUN "nom" passe par le firmware (CAS IN OPEN sur NOM.BIN, D086) : il marche donc aussi sur cassette, à condition que le programme lancé s'appelle NOM.BIN sur la bande.
- **Fait :** sur la cassette, chaque programme lancé par RUN "nom" (compilation en chaîne, D087) est mis seul après le programme principal, sous le nom `NOM.BIN`, sans chargeur BASIC (inutile, et du temps de bande en moins) ; sans chargeur, c'est la copie de la cassette (D179). Le programme principal et la disquette ne changent pas.
- **Fichiers (OPENOUT, OPENIN...) :** rien à changer, ils passent par le firmware et vont sur la cassette comme avec le vrai BASIC. Limite, la même que sur un 464 : la bande ne se rembobine pas toute seule (un programme qui se relance, comme enchaine.bas, demande de rembobiner à la main).
- **Exemples :** `examples\bande1.bas` (lance BANDE2 après la barre d'espace) et `examples\bande2.bas` (MODE 0, attend la barre d'espace) ; partie1 / partie2 gardés tels quels (leur fichier SCORE demanderait d'enregistrer sur la bande).
- **Essai (21 nov. 2026) :** Caprice Forever (464), RUN"" : BANDE1 puis, après la barre d'espace, « Press PLAY then any key: » puis BANDE2 chargé et lancé ; écrans conformes. Tests : 1919 réussis (2 nouveaux dans `TapeImageTests`).
- **Idée de l'utilisateur pour plus tard (notée dans IDEES.md) :** un « ! » devant le nom du fichier sur cassette (comme RUN"!NOM") supprime les messages du firmware (« Press PLAY then any key »...).
- **Sortie cassette terminée (D178 à D180).** Reste seulement l'essai sur un vrai CPC.

## D181 : cartouche GX4000, chemin A et première cartouche d'essai (21 nov. 2026) (choix de l'utilisateur)
- **Questions de l'utilisateur avant de choisir :** les conversions de jeux pour GX4000 passent surtout par NoCart (Kevin Thacker) : la cartouche contient les ROM du 6128 (OS, BASIC, AMSDOS retouché pour lire la cartouche) et l'image de la disquette ; les touches sont ensuite remplacées à la main par la manette. Droits : Amstrad (Cliff Lawson, 1999) autorise la redistribution de ses ROM en gardant le message de droit d'auteur, même modifiées ; une partie est à Locomotive Software ; certains lisent l'accord comme limité aux émulateurs. Réécrire soi-même les routines, sans recopier le code, est permis en principe (Claude n'est pas juriste). D016 rappelée : ne pas redistribuer les ROM.
- **Idée d'un émulateur CPC et GX4000 pour nos essais (question de l'utilisateur) :** possible (1 mois à 1 mois et demi estimé), **abandonnée** par l'utilisateur : trop long pour le gain.
- **Choix (chemin A) :** notre propre petit système dans la cartouche (écran, texte avec notre jeu de caractères, couleurs, manette, son ; nombres à virgule plus tard) : aucune ROM d'Amstrad, jeux librement distribuables. Le chemin B (ROM du CPC dans la cartouche, comme NoCart) est écarté. Le compilateur dira ce qui ne marche pas encore sur GX4000.
- **Fait (étape 1) :** `src\CpcBasic.Core\Output\CartridgeImage.cs` (fichier .cpr : RIFF, « AMS! », une partie « cbNN » de 16 Ko par page, complétée par des &FF) ; `src\CpcBasic.Core\Output\Gx4000Test.cs` : cartouche d'essai en assembleur (notre assembleur, D145) qui tourne en &0000 : Gate Array en MODE 1 (ROM haute coupée), 14 registres du CRTC, encres, 4 bandes verticales (bleu, jaune, bleu ciel, rouge) et bordure verte ; `src\CpcBasic.Cli\Program.cs` : `--essai-gx4000 fichier.cpr`. Tests : 1923 réussis (4 nouveaux, `CartridgeImageTests`).
- **Essai (21 nov. 2026) :** `dotnet run --project src\CpcBasic.Cli -- --essai-gx4000 essais\gx4000.cpr`, puis SugarBox en 6128 PLUS (« Default cartridge » remplacée par la cartouche d'essai ; WinAPE n'a pas la cartouche système du Plus chez l'utilisateur) : écran conforme (bordure verte, 4 bandes). Un 6128 Plus lance une cartouche comme la GX4000.
- **Suite prévue :** le texte (PRINT, LOCATE) avec notre propre jeu de caractères, puis la manette, puis MODE, INK, CLS, dessin, son...

## D182 : cartouche GX4000, le texte avec notre jeu de caractères (21 nov. 2026)
- **Jeu de caractères (choix A de l'utilisateur : dessiné par Claude) :** `src\CpcBasic.Core\Output\Gx4000Font.cs`, codes 32 à 126, 8 x 8 points, traits verticaux de 2 points, rien de recopié de la ROM d'Amstrad ; 7 colonnes utiles et la 8e vide (un point entre deux lettres) ; 8e ligne vide sauf g, j, p, q, y, la virgule et « _ » (gardé ainsi, choix de l'utilisateur). Approuvé sur images (« C'est parfait »). Lettres accentuées : plus tard.
- **Méthode :** notre système installe ses routines aux adresses de la ROM du CPC (&BB5A TXT OUTPUT, &BB6C TXT CLEAR WINDOW, &BB75 TXT SET CURSOR, &BB78 TXT GET CURSOR, &BB69 TXT GET WINDOW, &BB87 TXT VALIDATE, &BC0E SCR SET MODE, &BBB4 TXT STR SELECT, clavier en attente) ; les autres adresses de &BB00 à &BDFF répondent par un retour. Le programme compilé ne change pas. Liste établie en relevant les appels de BANDE2.BIN.
- **Fait :** `src\CpcBasic.Core\Output\Gx4000System.cs` (en assembleur, assemblé par notre assembleur) : démarrage (CRTC, encres du CPC, routines branchées, programme recopié en &4000 et lancé, écran gardé à la fin) ; texte en MODE 0, 1 et 2 (stylo 1 sur papier 0), CR, LF, CLS (code 12), gauche, droite, fin de ligne, écran qui remonte en bas ; routines dans la cartouche (ROM basse gardée), variables en &BE00, entrées fixes en &0040 pour les tests, interruption en &0038 (EI, RET). `Compiler.CompileToCartridge` ; outil en ligne de commande : `--gx4000 programme.bas [sortie.cpr]`. Limite actuelle : système et programme dans une seule page de 16 Ko.
- **Vitesse (question de l'utilisateur) :** l'affichage devrait être plus rapide que la ROM (écriture directe, comme TYPO, D172) ; à mesurer.
- **Pas encore faits :** la manette (KM WAIT KEY attend sans fin, KM TEST KEY « pas appuyée ») ; PEN, PAPER, INK, fenêtres, codes de contrôle à valeurs, son ; le compilateur ne signale pas encore les commandes absentes ; F5 ne fait pas encore la cartouche.
- **Essai (21 nov. 2026) :** `examples\gxtexte.bas` (MODE 1, LOCATE, PRINT, boucle FOR) en cartouche dans SugarBox (6128 PLUS) : écran conforme. Tests : 1931 réussis (8 nouveaux, `Gx4000SystemTests` ; une erreur d'écriture dans un test corrigée).

## D183 : cartouche GX4000, les manettes et la table des touches (21 nov. 2026)
- **Correspondance de base (choix de l'utilisateur) :** tir 1 = espace (47), tir 2 = ENTRÉE (18 et 6), directions = flèches (0, 2, 8, 1) ; touches de la manette elles-mêmes : 72 à 77 (manette 1), 48 à 53 (manette 2).
- **Autres touches d'INKEY(n) (idée de l'utilisateur) :** chaque touche écrite en clair reçoit un bouton libre, dans l'ordre : tir 2, tirs 1 et 2 ensemble, puis la manette 2 (haut, bas, gauche, droite, tir 1, tir 2, tirs 1 et 2) ; un message par touche (« la touche 67 correspondra au tir 2 de la manette 1 », avec le rappel que le tir 2 sert aussi d'ENTRÉE) ; plus de bouton libre : refus, avec la touche qui coince. INKEY avec un numéro calculé, INPUT, LINE INPUT : avertissement (la cartouche est faite).
- **Fait :** `Gx4000System.cs` : lecture des deux manettes par la grille du clavier (lignes 9 et 6, PPI et AY, comme le CPC) ; KM TEST KEY (&BB1E) consulte la table des touches (dans la cartouche) ; KM GET JOYSTICK (&BB24) ; KM READ CHAR / READ KEY (&BB09, &BB1B) : un caractère par nouvel appui (tir 1 = espace, tir 2 = 13, flèches &F0 à &F3) ; KM WAIT CHAR / WAIT KEY (&BB06, &BB18) attendent un appui. `Compiler.CompileToCartridge` rend un `CartridgeResult` (Notes, Problems). Outil en ligne de commande : affiche les messages. Exemple `examples\gxmanet.bas`.
- **Cartouches de 128 Ko au moins :** WinAPE ne lançait pas une cartouche d'une seule page ; `CartridgeImage` complète jusqu'à 8 pages vides (&FF), comme les vraies cartouches. (WinAPE est ensuite resté sur le 6128 normal chez l'utilisateur ; non poursuivi.)
- **Erreurs de l'assistant corrigées :** une écriture de plage refusée par C# dans un test ; deux variables `result` dans `src\CpcBasic.Cli\Program.cs` (non vu par les tests, qui ne compilent pas cet outil) ; message « correspondra à le tir 2 » ; texte faux dans l'exemple (Q = tir 2 seul).
- **Émulateurs :** SugarBox lance les cartouches mais plante ou s'emballe avec la manette de l'utilisateur (problème de Windows : la manette marche sur un autre ordinateur) ; **AMSpiriT** retenu pour les essais de cartouches, manette jouée au clavier (flèches, ESPACE = tir 1, CTRL = tir 2).
- **Essai (21 nov. 2026) :** `gxmanet.cpr` dans AMSpiriT, au clavier : « avec le clavier c'est parfait ». Tests : 1943 réussis (12 nouveaux dans `Gx4000SystemTests`).
- **Suite décidée :** la saisie à la manette pour INPUT et LINE INPUT (idée de l'utilisateur : gauche / droite font défiler les caractères, tir 1 valide la lettre, tir 2 valide la saisie ; un signe « effacer » dans la liste ; caractères selon le type demandé ; routine ajoutée seulement si le programme utilise INPUT).

## D184 : cartouche GX4000, saisie à la manette pour INPUT et LINE INPUT (21 nov. 2026)
- **Schéma de l'utilisateur (remplace celui proposé par Claude) :** haut / bas = lettre précédente / suivante (défile en restant appuyé : une lettre, puis après environ 15/50 s une toutes les 4/50 s) ; tir 2 = groupe suivant (majuscules, minuscules, chiffres, espace et signes, puis retour) ; droite = écrit la lettre ; gauche = efface (DEL, 127) ; tir 1 = valide (13). Droite et gauche : un seul effet par appui.
- **Ce qu'on voit est ce qu'on obtient (après essai : le 2 de « 62 » proposé mais pas écrit, le D proposé réapparu à la question suivante) :** la lettre proposée est montrée en couleurs inversées là où elle va s'écrire ; après une lettre écrite et au début de chaque INPUT, un carré vide ; droite sur le carré vide écrit un espace (idée de l'utilisateur) ; haut / bas repartent de la dernière lettre (la toute première fois, A montré sans bouger) ; tir 1 avec une lettre montrée : la lettre est écrite puis la saisie validée.
- **Tirs échangés (erreur de Claude, vue à l'essai dans AMSpiriT) :** le tir 1 est le bouton principal, bit 4 (valeur 16 de JOY, le bouton unique des manettes du CPC, que les jeux testent avec JOY(0) AND 16) ; le tir 2 est le bit 5 (32). Correspondance de base corrigée en conséquence (espace = bit 4, ENTRÉE = bit 5), ainsi que l'ordre des boutons libres pour INKEY(n). Les numéros de touches 76 et 77 restent ceux du CPC.
- **Fait :** `Gx4000System.cs` : KM WAIT CHAR (&BB06) devient la saisie à la manette (MOLETTE), seulement si le programme contient INPUT ou LINE INPUT (environ 340 octets ; sinon l'ancienne attente) ; dessin en couleurs inversées dans TXT OUTPUT (&BE0F) ; variables &BE0A à &BE12, mises à zéro au démarrage (&BE00 à &BE1F) ; entrée fixe &004C (un passage de la boucle) pour les tests ; pause d'environ 1/50 s par boucle de calcul. `Compiler.CompileToCartridge` : un message par programme qui explique la saisie (à la place de l'avertissement). Exemple `examples\gxsaisie.bas`.
- **Essai (21 nov. 2026) :** `gxsaisie.cpr` dans AMSpiriT (manette au clavier) : « PHILIPPE », « 62 », « Dans 10 ans, vous aurez 72 ans » ; « c'est parfait et finalement intuitif ». Le vrai comportement du BASIC (« ?Redo from start » pour des lettres à la place d'un nombre) est gardé. Tests : 1952 réussis.
- **Idée de l'utilisateur notée dans IDEES.md (à lui rappeler quand on fera de nouvelles commandes) :** une saisie limitée, `INPUT "0123456789"+CHR$(13); "Votre âge : "; A$`, qui n'accepte que les caractères donnés (sur CPC et à la manette).

## D185 : cartouche GX4000 depuis l'éditeur, Ctrl+F5 (21 nov. 2026) (choix de l'utilisateur)
- **Constat :** F7 sert à la version du CPC (D126) et le réglage Machine (D066) n'est plus dans l'éditeur ; Maj+F5 est pris (D134).
- **Fait :** menu Compiler, « Cartouche GX4000 », raccourci **Ctrl+F5** : enregistre le programme, compile avec les réglages de l'éditeur, écrit NOM.cpr à côté du programme ; messages : réussite et taille, messages de la cartouche (touches placées, saisie à la manette), rappel pour AMSpiriT ou SugarBox, ce qui n'est pas encore sur la console ; erreurs comme pour F5 (fichier ouvert dans l'émulateur : message). F5 ne change pas (disquette et cassette). Aide de démarrage complétée. Pas de réglage par programme.
- **Essai (21 nov. 2026) :** `examples\gxsaisie.bas`, Ctrl+F5 : « Cartouche GX4000 réussie » (3884 octets avec la touche ESC comme le CPC), cartouche identique à l'essai dans AMSpiriT. Tests inchangés : 1952 (l'éditeur n'est pas couvert par les tests).

## D186 : cartouche GX4000, les couleurs : PEN, PAPER, INK, BORDER (21 nov. 2026) (ordre conseillé, choisi par l'utilisateur)
- **Fait :** `Gx4000System.cs` : TXT SET / GET PEN (&BB90, &BB93), TXT SET / GET PAPER (&BB96, &BB99), SCR SET / GET INK (&BC32, &BC35), SCR SET / GET BORDER (&BC38, &BC3B). Le texte s'écrit stylo sur papier dans les 3 modes : masque des points de la lettre selon le mode, puis papier XOR ((stylo XOR papier) ET masque) ; octets « toutes les encres » par mode (tables TENC0, TENC1, TENC2). CLS, MODE et l'écran qui remonte prennent la couleur du papier. Les 27 couleurs du CPC en valeurs du Gate Array ; couleur de chaque stylo et de la bordure notée en &BE20 à &BE30. Au démarrage : les 16 encres et la bordure du CPC allumé (1, 24, 20, 6, 26, 0, 2, 8, 10, 12, 14, 16, 18, 22, 1, 16 ; bordure 1), stylo 1, papier 0. Variables mises à zéro de &BE00 à &BE3F.
- **Pas encore :** la seconde couleur d'INK (encre clignotante) ; le message de Ctrl+F5 le dit.
- **Tests :** 1957 réussis (5 nouveaux). Les anciens tests appelaient les routines sans le démarrage : stylo 0 = papier, lettres invisibles ; ils mettent maintenant le stylo 1, comme le démarrage.
- **Essai (21 nov. 2026) :** `examples\gxcoul.bas` (Ctrl+F5) dans AMSpiriT : MODE 0, 15 stylos de couleurs différentes sur fond noir ; MODE 1, jaune sur rouge, bleu sur vert, jaune sur bleu, bordure rouge : conformes.

## D187 : cartouche GX4000, le cœur du dessin (21 nov. 2026) (découpage accepté : le cœur d'abord)
- **Constat :** toutes les commandes de dessin passent par les routines GRA et SCR de la ROM (y compris BOX, CIRCLE, ELLIPSE, STAR, qui demandent le mode, la place d'un point et le codage d'une encre) ; aucune variable de la ROM n'est lue directement.
- **Fait :** `Gx4000System.cs` répond à GRA MOVE / PLOT / TEST / LINE (absolus et relatifs), ASK CURSOR, SET / GET ORIGIN, GET W WIDTH / HEIGHT (tout l'écran), CLEAR WINDOW (CLG, curseur à l'origine), SET / GET PEN et PAPER graphiques, SCR GET MODE (retenue en MODE 0, zéro en MODE 1), SCR DOT POSITION, SCR INK ENCODE. Repère du CPC (640 x 400, 0,0 en bas à gauche) ; points de base selon le mode ; traits de Bresenham, les deux bouts compris ; rien hors de l'écran. Réglages par mode copiés en &BE60 par SCR SET MODE (qui remet aussi l'origine et le curseur à 0,0) ; variables du dessin en &BE40 à &BE66 ; mises à zéro de &BE00 à &BE7F ; stylo graphique 1 au démarrage. Les adresses de &B900 à &BAFF répondent aussi par un retour (avant : non préparées).
- **Pas encore (étape 2) :** modes d'écriture (XOR, AND, OR), fenêtres graphiques, MASK, FILL, PAINT.
- **Tests :** 1970 réussis (13 nouveaux ; une attente fausse de Claude corrigée : un trait en biais allume aussi le point voisin au bout).
- **Essai (21 nov. 2026) :** `examples\gxdessin.bas` (Ctrl+F5) dans AMSpiriT : MODE 1, points, cadre, éventail, carrés en DRAWR et ORIGIN ; MODE 0, CLG, BOX, CIRCLE et ELLIPSE pleins, diagonale, « TEST : OK » : conformes. L'utilisateur trouve le dessin beaucoup plus rapide que sur disquette (à mesurer).

## D188 : cartouche GX4000, la fin du dessin : XOR, AND, OR, fenêtres graphiques, MASK, FILL (21 nov. 2026)
- **Constat :** sur le CPC, chaque point d'un tracé passe par l'entrée détournable SCR WRITE (&BDE8), selon SCR ACCESS ; notre MASK (D110) la détourne. FILL et PAINT (D107) n'utilisent que GRA et SCR, plus KL U ROM DISABLE / KL ROM RESTORE (un simple retour suffit sur la cartouche).
- **Fait :** `Gx4000System.cs` : SCR WRITE (&BDE8, un saut vers notre routine posé au démarrage ; chaque point de PLOT et DRAW y passe) avec les modes 0 normal, 1 XOR, 2 AND, 3 OR ; SCR ACCESS (&BC59) ; le mode dure jusqu'à MODE (règle de l'utilisateur). Fenêtres graphiques : GRA WIN WIDTH (&BBCF) et WIN HEIGHT (&BBD2), bords dans n'importe quel ordre, gardés dans l'écran, gauche et droite arrondis à l'octet ; GET W WIDTH / HEIGHT les rendent ; rien n'est dessiné hors de la fenêtre ; TEST hors de la fenêtre rend le papier ; CLG ne remplit que la fenêtre (ligne par ligne, seulement les octets visibles). MODE remet la fenêtre à tout l'écran. MC WAIT FLYBACK (&BD19, FRAME) attend le retour du balayage (port B du PPI). Variables &BE67 à &BE6F.
- **Tests :** 1977 réussis (7 nouveaux ; un ancien test de CLG vérifiait un octet jamais affiché, corrigé). Les tests posent le saut de SCR WRITE comme le démarrage.
- **Essai (21 nov. 2026) :** `examples\gxxor.bas` (Ctrl+F5) dans AMSpiriT : éventail en XOR puis effacé ; fenêtre graphique (CLG bleu ciel, traits coupés aux bords), trait en pointillés (MASK 170), triangle rempli (FILL) : conformes.
- **Le dessin est complet sur la cartouche.** Restent pour la console : les fenêtres de texte (WINDOW), le son, les encres clignotantes, les programmes de plus de 16 Ko.

## D189 : cartouche GX4000, programmes de plus de 16 Ko (21 nov. 2026)
- **Constat :** jusqu'ici, le programme devait tenir dans la page 0 de la cartouche (16 Ko) avec notre système. La console a 64 Ko de mémoire vive : le programme peut y être recopié de &4000 à &B900, soit 30 976 octets au plus.
- **Fait :** `Gx4000System.cs` : `SplitProgram` coupe le programme (la page 0 garde le système et le début, la suite va dans les pages 1, 2...) ; `Build` fabrique la cartouche entière. Au démarrage, le système recopie le début, puis branche chaque page suivante à &C000 (`OUT &DFxx`, ROM haute allumée par &7F81), la recopie à la suite, et éteint la ROM haute (&7F89). Un programme trop gros est refusé avec un message clair (« la mémoire en accepte 30976 au plus »), et aucun fichier n'est créé.
- **Tests :** 1982 réussis (5 nouveaux : découpage pour 3 000, 20 000 et 40 000 octets ; cartouche de 30 000 octets en 3 pages, octets dans l'ordre ; refus d'un programme trop gros). Le branchement des pages n'est pas imité par les tests : vérifié dans AMSpiriT.
- **Essai (21 nov. 2026) :** `examples\gxgrand.bas` (200 lignes, environ 24 Ko, Ctrl+F5) dans AMSpiriT : les 200 lignes s'affichent, « Lignes executees : 200 », « TOUT LE PROGRAMME A ETE RECOPIE : OK ». Le premier exemple (400 lignes, 48 431 octets) était trop gros et a été refusé proprement.
- Restent pour la console : les fenêtres de texte (WINDOW), le son, les encres clignotantes.

## D190 : cartouche GX4000, les fenêtres de texte (22 nov. 2026) (« fais pour le mieux » : les fenêtres avant les encres clignotantes et le son)
- **Fait :** `Gx4000System.cs` : TXT WIN ENABLE (&BB66, WINDOW : bords remis dans l'ordre et ramenés dans l'écran, curseur en haut à gauche), TXT GET WINDOW (&BB69), TXT STR SELECT (&BBB4 : les 8 flux #0 à #7, chacun avec son curseur, sa fenêtre, son stylo et son papier, rangés en &BE80 + flux x 8), TXT SWAP STREAMS (&BBB7, WINDOW SWAP), TXT VALIDATE (&BB87 : après la fin de la ligne, début de la ligne suivante ; sous la fenêtre, sa dernière ligne). Le curseur est compté dans la fenêtre ; CLS n'efface que la fenêtre (tout l'écran : d'un seul coup comme avant) ; en bas de la fenêtre, seule la fenêtre remonte. MODE remet toutes les fenêtres sur l'écran entier (stylos et papiers gardés). Variables du système : &BE00 à &BEFF mises à zéro au démarrage.
- Le message de Ctrl+F5 devient : « Pas encore sur la console : le son, les encres clignotantes. »
- **Tests :** 1990 réussis (8 nouveaux : texte dans la fenêtre, passage à la ligne, défilement de la fenêtre seule, CLS, les flux, WINDOW SWAP et MODE, TXT VALIDATE, un programme compilé).
- **Essai (22 nov. 2026) :** `examples\gxfenetre.bas` (Ctrl+F5) dans AMSpiriT : deux fenêtres colorées qui défilent chacune de leur côté, puis WINDOW SWAP 0,1 et CLS dans la fenêtre seule.
- Restent pour la console : les encres clignotantes, le son.

## D191 : cartouche GX4000, les encres clignotantes (22 nov. 2026)
- **Fait :** `Gx4000System.cs` : l'interruption (&0038, 300 fois par seconde) n'est plus ignorée ; les interruptions sont permises avant de lancer le programme. SCR SET INK (&BC32) et SCR SET BORDER (&BC38) notent les deux couleurs (&BE20 et &BEC0 + stylo, la bordure étant le stylo 16) ; SCR GET INK rend les deux ; SCR SET FLASHING (&BC3E, SPEED INK) et SCR GET FLASHING (&BC41) ; 10,10 au démarrage, comme le CPC. Le choix du stylo et sa couleur sont envoyés au Gate Array sans interruption entre les deux.
- **Constat (essai de l'utilisateur) :** en changeant les couleurs sur n'importe quelle interruption, la bordure changeait parfois au milieu de l'image (un léger décalage). **Corrigé :** seule l'interruption du retour du balayage compte (PPI, port B, bit 0, lu en &F500), comme le firmware du CPC : les couleurs changent quand rien n'est affiché.
- Le message de Ctrl+F5 devient : « Pas encore sur la console : le son. »
- **Tests :** 1993 réussis (3 nouveaux : INK à deux couleurs, le rythme de SPEED INK image par image, un programme compilé ; le test des entrées fixes attend maintenant un saut en &0038).
- **Essai (22 nov. 2026) :** `examples\gxclign.bas` (Ctrl+F5) dans AMSpiriT : textes rouge et jaune, bleu et blanc qui clignotent, puis SPEED INK 3,3 et la bordure qui clignote, sans décalage.
- Reste pour la console : le son.

## D192 : cartouche GX4000, le son, étape 1 (22 nov. 2026) (découpage en 3 étapes ; « tu codes, alors tu décides pour le mieux »)
- **Fait :** `Gx4000System.cs` : SOUND QUEUE (&BCAA, le bloc de 9 octets du compilateur), SOUND CHECK (&BCAD, SQ), SOUND RESET (&BCA7). Trois canaux, le bruit (registre 6 et registre 7 de la puce), une file de 4 sons par canal ; le bit 7 de l'état vide d'abord les files. Les sons avancent 100 fois par seconde (une interruption sur 3) ; durée 0 ou négative : 2 secondes en attendant les enveloppes. La puce est écrite par le PPI ; la lecture des manettes (LIRE) se fait sans interruption, puisqu'elle passe par les mêmes ports. Variables du son en &B800 à &B8FF : le programme peut faire 30 720 octets au plus (au lieu de 30 976).
- Le message de Ctrl+F5 devient : « Pas encore sur la console : les enveloppes du son (ENV, ENT), RELEASE, les rendez-vous entre canaux. »
- **Tests :** 1998 réussis (5 nouveaux : file de 4 sons, durée puis son suivant, deux canaux et bruit, bit 7, un programme compilé).
- **Essai (22 nov. 2026) :** `examples\gxson.bas` (Ctrl+F5) dans AMSpiriT : gamme, accord sur 3 canaux, explosion, long son de 3 secondes avec SQ(1) à 132 pendant le son. Le premier SQ(1) vaut 3 : le son commence au centième suivant. La boucle d'attente de l'exemple était trop courte pour voir la fin du son : remplacée par FRAME.
- Restent pour le son : les enveloppes (étape 2), RELEASE, les sons retenus, les rendez-vous, ON SQ GOSUB (étape 3).

## D193 : cartouche GX4000, le son, étape 2 : les enveloppes ENV et ENT (22 nov. 2026)
- **Fait :** `Gx4000System.cs` : SOUND AMPL ENVELOPE (&BCBC) et SOUND TONE ENVELOPE (&BCBF) notent l'adresse du bloc de 16 octets du compilateur (gardé dans le programme ; tables en &B8C2 et &B8E0, vidées par SOUND RESET, donc aussi par RUN "nom"). ENV : pas de volume (de -128 à 127, volume pris modulo 16 comme le CPC), pause (0 = 256 centièmes), section à 0 pas = volume donné tel quel, enveloppe de la puce (=forme,période : registres 11 à 13, volume 16). ENT : pas de période, période donnée (=période,pause), ENT négatif qui recommence tant que le son dure. Durée de SOUND : positive = donnée (le volume reste à la fin de l'enveloppe) ; 0 = celle de l'enveloppe de volume ; négative = l'enveloppe répétée. Sans enveloppe : 2 secondes au même volume (enveloppe par défaut, comme le CPC). Chaque canal a maintenant 62 octets (&B800 à &B8B9), les variables communes sont en &B8BA à &B8C1.
- Le message de Ctrl+F5 devient : « Pas encore sur la console : RELEASE et les rendez-vous entre canaux, TIME, AFTER, EVERY. »
- **Tests :** 2002 réussis (4 nouveaux : ENV et sa durée, durée donnée et volume qui reste, ENT, un programme compilé avec ENV, ENT négatif et période donnée).
- **Essai (22 nov. 2026) :** `examples\gxenv.bas` (Ctrl+F5) dans AMSpiriT : son qui monte puis descend, sirène, tir laser, cloche 3 fois ; « les sons représentent bien ce que tu décris ».
- Restent pour le son : RELEASE, les sons retenus, les rendez-vous entre canaux, ON SQ GOSUB (étape 3).

## D194 : cartouche GX4000, TIME, AFTER, EVERY, ON SQ ; les nombres à virgule refusés (22 nov. 2026) (avant RELEASE et les rendez-vous : plus utiles dans les jeux)
- **Fait :** `Gx4000System.cs` : un petit noyau d'événements. KL TIME PLEASE (&BD0D) et KL TIME SET (&BD10) : temps sur 32 bits, +1 à chaque interruption (300 fois par seconde). KL INIT EVENT (&BCEF), KL ADD TICKER (&BCE9), KL DEL TICKER (&BCEC) : 4 minuteries, qui comptent à chaque début d'image (50 fois par seconde) et appellent leur événement (express, tout de suite). SOUND ARM EVENT (&BCB0) : l'événement arrive une fois, quand la file du canal a une place (ON SQ GOSUB) ; SOUND RESET l'oublie. Variables en &BE36 (temps), &BED2 (événements des canaux), &BED8 (minuteries).
- **Constats et corrections :**
  - Dans les tests, la pile (&BF00) écrasait ces variables, d'abord rangées en &BEE0 : déplacées loin de la pile (8 tests en échec).
  - Essai de l'utilisateur : « Overflow » et un son continu. Les nombres à virgule passent par les routines de calcul de la ROM du CPC, absente de la console (ici le +0.5 de la ligne 70) ; ces appels ont abîmé la mémoire. **Ctrl+F5 refuse maintenant un programme qui en a besoin**, avec un message clair (garder des nombres entiers ; rappel : notre compilateur garde entières les variables sans signe et la division / entre entiers, D174).
  - Essai suivant : la mélodie s'arrêtait sur la 3e note, qui ne finissait plus. LIRE (manettes) gardait l'état des interruptions avec LD A,I, que le Z80 fausse quand une interruption arrive à ce moment-là : les interruptions restaient coupées. Remplacé par une marque en &BE31, posée par l'interruption elle-même.
- Le message de Ctrl+F5 devient : « Pas encore sur la console : RELEASE et les rendez-vous entre canaux. »
- **Tests :** 2007 réussis (5 nouveaux : TIME, une minuterie (départ, recharge, retrait), ON SQ une seule fois, un programme compilé avec TIME, AFTER, EVERY, ON SQ, le refus des nombres à virgule).
- **Essai (22 nov. 2026) :** `examples\gxtemps.bas` (Ctrl+F5) dans AMSpiriT : un point par seconde, AFTER au bout de 5 secondes, « TIME A MESURE : 5 SECONDES », mélodie qui tourne en boucle (« ok ça marche »). À la fin d'un programme, une touche relance la cartouche (comme le CPC qui redémarre).

## D195 : cartouche GX4000, le son, étape 3 : sons retenus, RELEASE, rendez-vous (22 nov. 2026)
- **Fait :** `Gx4000System.cs` : le premier son d'une file ne commence que s'il n'est pas retenu (bit 6 de l'état) et si chaque canal de son rendez-vous (bits 3 à 5) l'attend aussi, sans son en cours ; ils commencent alors ensemble. SOUND RELEASE (&BCB3) libère le son retenu des canaux donnés. Un son donné pour plusieurs canaux (SOUND 3,...) reçoit dans chaque file un rendez-vous avec les autres, comme le CPC : il attend que tous ses canaux soient libres. SOUND CHECK rend aussi les bits 3 à 6 du son qui attend. À la fin d'un son, le canal se tait avant que le suivant commence.
- Le message de Ctrl+F5 devient : « Pas encore sur la console : les nombres à virgule. »
- **Tests :** 2011 réussis (4 nouveaux : son retenu et RELEASE, rendez-vous entre A et C, son sur deux canaux qui attend, un programme compilé).
- **Essai (22 nov. 2026) :** `examples\gxrdv.bas` (Ctrl+F5) dans AMSpiriT : son retenu jusqu'au tir 1, 3 notes puis accord avec C, B seul puis A et B ensemble (« oui ça correspond »).
- **Le son de la console est complet.** Reste pour la console : les nombres à virgule.

## D196 : cartouche GX4000, les nombres à virgule (22 nov. 2026) (choix de l'utilisateur : finir les nombres avant de publier)
- **Fait :** `Gx4000System.cs` : nos propres routines de calcul, au format du Locomotive BASIC (5 octets), branchées aux adresses de la ROM du 664 / 6128 : INTEGER TO REAL, BINARY TO REAL, REAL TO INTEGER, REAL FIX, REAL *10^A, addition, soustraction à l'envers, multiplication, division, comparaison, changement de signe, signe, mode des angles, PI, SQR, puissance, LOG, LOG10, EXP, SIN, COS, TAN, ATN. KL PROBE ROM (&B915) répond H = 4 (CPC Plus) : le programme prend les adresses du 664 / 6128. Une cartouche demandée pour le 464 (F7) est compilée pour tous les CPC. Calcul interne sur 40 bits (8 bits de garde, arrondi au rangement). Fonctions : réduction puis polynômes (ln par z = (m - 1) / (m + 1), exp par 2^n x e^r avec ln 2 en deux parties, sin et cos par quarts de tour avec pi / 2 en deux parties, en degrés par 90, atn par 1 / x et pi / 6, puissance entière exacte par carrés). Travail en &B700 à &B7FF : le programme peut faire 30 464 octets au plus. Le refus de D194 est retiré.
- **Vérification avant envoi :** un petit simulateur de Z80 écrit en Python (hors du projet) a comparé des milliers de calculs aux bonnes valeurs : écart relatif sous 5E-10 (le CPC affiche 9 chiffres).
- **Vitesse (essai de l'utilisateur, exemple du cercle, 126 points avec SIN et COS) :** 13 secondes d'abord ; puis multiplication et division dans les registres du Z80 (octets à zéro sautés, division sans retour en arrière), polynômes dans l'accumulateur, décalages par octets, premier essai de SQR à 3 % près : **8 secondes**.
- **Tests :** 2029 réussis (19 nouveaux : les quatre opérations, division par zéro et comparaison, entiers et 10^A, les fonctions, degrés et puissance, un programme compilé ; le test du refus retiré).
- **Essai (22 nov. 2026) :** `examples\gxreel.bas` (Ctrl+F5) dans AMSpiriT : toutes les valeurs justes au dernier chiffre (3.75, 3.375, 3.33333333, 1.41421356, 3.14159265, 0.841470985, 0.540302306, 2.71828183, 2.30258509, 1024, 0.5, 45), cercle dessiné. Rappel : sans signe, une variable est entière dans notre compilateur ; l'exemple utilise A!, B!, T!.
- **La console fait maintenant tout ce que fait le CPC** dans nos programmes (sauf la disquette et la cassette, qu'elle n'a pas).

## D197 : préparation de la version 0.7.0 (22 nov. 2026) (choix de l'utilisateur : publier une fois les nombres finis)
- **Contenu :** sortie cassette (D178 à D180) et cartouche GX4000 complète (D181 à D196) ; notes dans `docs\RELEASE-NOTES-0.7.0.md`.
- **Fichiers :** version 0.7.0 dans `Directory.Build.props` et `packaging\build-release.cmd` ; `packaging\LISEZMOI.txt` (cassette au point 4, nouveau point 8 « Cartouche GX4000 », points suivants renumérotés) ; README et TESTEURS.md.
- **Paquet construit (22 nov. 2026) :** `release\CpcBasic-0.7.0-win-x64.zip`, SHA256 bdeafc67d16851f3c1d7b3f6de40917fa9050a2616ecd9e2e7a955b694566fbf (calculée ici, la fenêtre de build-release.cmd s'étant fermée avant la copie) ; contenu vérifié : 336 fichiers, 100 exemples dont les 14 `gx*.bas`, bibliothèques GRAPHIQUE et TEXTE, pas de ROM, pas de .dsk, .cdt ni .cpr.
- **Publiée (22 nov. 2026) :** pre-release GitHub v0.7.0, « CPC BASIC IDE 0.7.0 (version de test) », commit fed48dd.

## D198 : sprites, étape 1 : DEFSPR et SPRITE (22 nov. 2026) (forme proposée puis choisie par l'utilisateur)
- **Écriture (choix de l'utilisateur) :** `DEFSPR n, "ligne", "ligne", ...` (n de 1 à 16 ; un caractère par point : 0 à 9 et A à F = stylo, « . » = le décor reste visible ; au plus 16 x 16 points ; les DEFSPR du même numéro qui se suivent s'ajoutent, de haut en bas ; textes écrits en clair) ; `SPRITE n, x, y` montre ou déplace ; `SPRITE n` cache. Coordonnées du MODE, coin en haut à gauche, comme BOX (choix A de l'utilisateur). Mot nouveau : DEFSPR (proposé par l'utilisateur) ; SPRITE et COLLIDE étaient déjà réservés.
- **Fait :** `Parser.cs` (DEFSPR, SPRITE, regroupement des DEFSPR qui se suivent, messages SpriteDefinition, SpriteRow, SpriteSize), `CodeGenerator.cs`, `RuntimeLibrary.cs`, nouveau `Codegen\SpriteRuntime.cs` : le compilateur prépare l'image des trois MODE (pour chaque octet : l'image et ce qui reste du décor) ; le module, écrit en assembleur, garde le décor sous le sprite et le remet quand il bouge ou se cache (écriture directe dans l'écran, comme BOX). Un sprite qui dépasse de l'écran n'est pas montré. Pour l'instant, la position en largeur avance par octet (2, 4 ou 8 points selon le MODE). Marche aussi sur la cartouche GX4000 (même module).
- **Vérification avant envoi :** le module essayé dans le simulateur Python (trois MODE, décor rendu octet pour octet, sortie de l'écran).
- **Tests :** 2042 réussis (13 nouveaux).
- **Essai (22 nov. 2026) :** `examples\sprite.bas` dans WinAPE (F5) et dans AMSpiriT (Ctrl+F5) : l'envahisseur traverse le décor sans l'abîmer, puis disparaît (« C'est ok »).
- **Suite prévue :** position au point près en largeur (étape 2), COLLIDE (étape 3), sprites qui se croisent (étape 4).

## D199 : sprites, étape 2 : la position au point près (22 nov. 2026)
- **Fait :** `SpriteRuntime.cs` : quand x n'est pas au début d'un octet, l'image est décalée de s points vers la droite (un octet de plus par ligne) ; chaque image décalée est préparée une seule fois, à sa première utilisation, puis gardée (réserve prévue par le compilateur derrière chaque DEFSPR : 1, 3 ou 7 décalages selon le MODE ; environ 200 octets pour un 8 x 8, 670 pour un 16 x 16) ; un changement de MODE les fait refaire. La description d'un sprite gagne 4 octets (adresse des images décalées, leur MODE, celles qui sont prêtes).
- **Vérification avant envoi :** simulateur Python : 4 dessins, 3 MODE, positions 0 à 19 et bords de l'écran, image au point près et décor rendu.
- **Tests :** 2043 réussis (1 nouveau, 1 complété).
- **Essai (22 nov. 2026) :** `examples\sprite.bas` : mouvement fluide de 2 points par image ; une petite saccade vers les trois quarts du trajet (« ça passe bien ») : sans doute le rythme de l'émulateur, à regarder si elle revient ailleurs.

## D200 : sprites, étape 3 : COLLIDE (22 nov. 2026)

- `COLLIDE(a, b)` vaut -1 (vrai) si les sprites a et b sont tous les deux visibles et que leurs rectangles se touchent ou se chevauchent, sinon 0.
- Comparaison des rectangles (position et taille de chaque sprite), dans les coordonnées du MODE en cours ; pas de comparaison point par point (rapide, suffisant pour la plupart des jeux).
- Un sprite caché ou jamais montré ne touche rien. Numéros hors de 1 à 16 : erreur à la compilation si connus.
- Exemple : examples\collide.bas (deux envahisseurs avancent l'un vers l'autre et s'arrêtent au contact). Essayé dans WinAPE : ils s'arrêtent bien en se touchant.
- Tests : 2045.

## D201 : sprites, étape 4 : des sprites qui se croisent (22 nov. 2026)

- Le plus grand numéro passe devant (choix de Claude, « tu codes, alors tu décides »).
- Avant de changer un sprite (SPRITE n, x, y ou SPRITE n), les sprites montrés de numéro plus grand qui touchent la zone changée sont enlevés (du plus grand au plus petit), puis remis après (du plus petit au plus grand) : chacun garde le bon décor. La zone est un rectangle qui grandit avec chaque sprite enlevé, avec une marge de 7 points de chaque côté (octets de l'écran partagés).
- Chemin court : si aucun sprite n'est montré au-dessus, ou si aucun ne touche la zone, le sprite est changé comme avant. Une première version, trop lente (environ 3 fois le temps d'avant), faisait disparaître le sprite bleu dans WinAPE : son effacement tombait pile au passage du balayage de l'écran. Corrigé : environ 2 000 instructions pour les deux sprites de l'exemple, contre 1 460 avant cette étape.
- Vérification : simulateur Python (400 changements au hasard, 6 sprites, 3 MODE, écran exact à chaque fois ; l'ancienne version se trompe dès les premiers croisements) ; 1 test C# nouveau (2046).
- Essai (22 nov. 2026) : examples\croise.bas dans WinAPE : les deux sprites se croisent, les rayures restent intactes. Sur cartouche, c'est lent et saccadé (à regarder).
- Les exemples recommencent au début après la dernière attente d'ESPACE (demande de l'utilisateur) : croise.bas et collide.bas.

## D202 : cartouche GX4000, traits tout droits rapides (22 nov. 2026) (demande de l'utilisateur)

- Constat : dans AMSpiriT, les rayures de croise.bas se traçaient très lentement : notre DRAW calculait chaque point un par un (fenêtre, adresse, écriture), environ 120 instructions par point.
- Fait : `Gx4000System.cs`, GRA LINE (&BBF6, &BBF9) : trait horizontal (même y) écrit octet par octet par SCR WRITE (&BDE8), plusieurs points d'un coup, les octets pleins en une fois ; trait vertical (même x) ligne après ligne, sans recalculer l'adresse. Bouts ramenés dans la fenêtre graphique. Les traits en biais ne changent pas.
- MASK (D110) détourne SCR WRITE et compte les points dans l'ordre : dans ce cas, les traits verticaux et les traits horizontaux tracés vers la gauche gardent l'ancien chemin (motif exactement comme avant) ; vers la droite, le détournement reçoit plusieurs points de gauche à droite, comme avec le firmware.
- Tests : 2049 (1 nouveau, en 3 MODE : 180 traits au hasard, décor au hasard, avec ou sans fenêtre graphique, écriture normale ou XOR, bouts hors de l'écran ; l'écran doit être le même, octet pour octet, que par PLOT point par point). Le premier jet du test durait 3 min 37 (adresse des routines recherchée à chaque point) : ramené à 9 s pour tous les tests.
- Essai (22 nov. 2026) : croise.bas en cartouche dans AMSpiriT : « c'est largement mieux ».
- Reste (IDEES) : les traits en biais, et les ronds d'ELLIPSE, toujours point par point.

## D203 : cartouche GX4000, traits en biais rapides (22 nov. 2026) (choix de l'utilisateur)

- Fait : `Gx4000System.cs`, GRA LINE : quand les deux bouts sont dans la fenêtre graphique (donc tout le trait), le même calcul que le chemin normal donne les mêmes points, mais l'adresse dans l'écran et le masque du point suivent d'un point au suivant (un point à droite ou à gauche, une ligne de l'écran plus haut ou plus bas) au lieu d'être recalculés, et sans vérifier la fenêtre à chaque point. Un bout dehors : chemin normal. Les points passent par SCR WRITE (&BDE8) dans le même ordre qu'avant : MASK marche pareil. Masque du point de droite d'un octet gardé en &BE70.
- Tests : 2052 (1 nouveau, en 3 MODE : 180 traits en biais au hasard, comparés à PLOT des points du même calcul, décor au hasard, fenêtre graphique, XOR, bouts dehors). Un ancien test supposait qu'un programme de 40 000 octets tient sur 2 pages en plus : le système dépasse maintenant 9 Ko, il en faut 3 ; ramené à 30 000 octets (de toute façon, 30 464 au plus).
- Essai (22 nov. 2026) : `examples\gxdessin.bas` en cartouche dans AMSpiriT : dessin identique, « c'est très rapide ».

## D204 : DRAW et DRAWR rapides sur CPC (22 nov. 2026) (« le projet est de compiler pour justement rendre plus rapide ! »)

- Constat : `examples\illusion.bas` (programme de concours de l'utilisateur, 2 lignes) allait à la même vitesse compilé qu'en BASIC : 400 tours en 63 s compilé, 58 s en BASIC (`examples\illtemps.bas`, chronomètre ; les traits changent à chaque lancement à cause de RND). Presque tout le temps passe dans GRA LINE de la ROM, la même routine pour les deux.
- Fait : `Codegen\LineRuntime.cs` (nouveau) : DRAW et DRAWR appellent notre module au lieu de &BBF6 / &BBF9. Il lit les réglages du firmware (GRA ASK CURSOR, GRA GET ORIGIN, GRA GET PEN + SCR INK ENCODE, GRA GET W WIDTH / HEIGHT, SCR GET MODE, SCR DOT POSITION), puis trace avec le calcul de la cartouche (D202, D203 : adresse et masque suivis d'un point au suivant, horizontaux octet par octet) ; chaque écriture passe par SCR WRITE (&BDE8, donc XOR, AND, OR respectés) ; curseur mis au bout par GRA MOVE ABSOLUTE. La ROM garde : un bout hors de la fenêtre graphique, écran décalé ou ailleurs qu'en &C000 (SCR GET LOCATION, &BC0B ; ajouté à la cartouche : toujours &C000), et tout programme avec MASK (le motif suit l'ordre des points de la ROM, D110).
- Piège (essai dans WinAPE : écran rempli de couleur, programme qui repart) : SCR WRITE est une routine de la ROM basse appelée directement ; il faut brancher la ROM basse (KL L ROM ENABLE) et débrancher la haute (KL U ROM DISABLE) pendant le tracé, puis KL ROM RESTORE. Les simulateurs ne le voyaient pas.
- Piège des tests : MiniZ80 ne connaît pas CCF ; INSIDE réécrit avec des comparaisons simples. MiniZ80 : réglage UseInstalledFirmware (les sauts posés par le test passent avant le firmware simulé).
- Tests : 2056 (comparaison avec GRA LINE de notre système sur 240 traits au hasard : origine, fenêtre, XOR, DRAWR, bouts dehors ; DRAW et DRAWR appellent le module ; avec MASK, la ROM). Les points sont ceux de notre calcul : pas encore comparés point par point avec ceux de la vraie ROM (à l'œil, dans WinAPE : « tout me semblait normal »).
- Essai (22 nov. 2026) : illtemps.bas : 37,8 s dans WinAPE (6128), 33,7 s en cartouche dans AMSpiriT (au lieu de 58 à 63 s) ; « c'est très joli en étant plus rapide ».
- Suite possible : écrire directement dans l'écran au lieu de SCR WRITE (en écriture normale), et faire passer BOX, CIRCLE, ELLIPSE par le module.

## D205 : DRAW encore plus rapide : écriture directe dans l'écran (22 nov. 2026)

- Fait : `LineRuntime.cs` : nouvelles entrées LINEN et LINERN, choisies par le compilateur quand le programme ne change jamais le mode d'écriture (aucune 4e valeur à DRAW, PLOT..., aucun CALL, aucune commande de bibliothèque : `directLines` dans CodeGenerator.cs). Les points sont alors écrits directement dans l'écran, sans SCR WRITE : octet = ((ancien XOR encre) AND masque inversé) XOR encre ; boucles en registres (DE adresse, C masque inversé, B encre, HL reste du calcul ; le masque inversé tourne par RRC / RLC). Les traits horizontaux s'écrivent aussi directement. Sinon, SCR WRITE comme en D204.
- Erreur trouvée par les tests C# : le reste de départ écrasé par le petit écart. Mon simulateur Python ne la voyait pas : ses deux machines partageaient la même mémoire (comparaison toujours juste) ; corrigé (mémoire propre à chaque machine), il retrouve l'erreur sur l'ancienne version.
- Tests : 2056 (le test de D204 passe aussi par LINEN et LINERN en écriture normale).
- Essai (22 nov. 2026) : illtemps.bas dans WinAPE : 400 tours en 25,6 s (au lieu de 37,8 s avec D204, 58 à 63 s avant) ; « C'est comme ça que ce programme se savoure. C'est trop beau ».

## D206 : SELECT CASE (23 nov. 2026) (forme proposée par Claude, acceptée par l'utilisateur)

- Écriture (sur le modèle du BASIC de Microsoft) : `SELECT valeur` (nombre ou texte ; `SELECT CASE valeur` accepté aussi), puis des `CASE` : une valeur, un intervalle `4 TO 6`, une comparaison `> 6` (<, <=, >=, =, <>), plusieurs séparés par des virgules (`CASE 1, 4 TO 6, > 10`) ; `CASE ELSE` facultatif, en dernier ; `END SELECT`. Le premier CASE qui convient est exécuté, puis on passe après END SELECT. Pas de mot `IS` (`CASE IS > 6` chez Microsoft) : un mot réservé de moins. Aucun mot nouveau : SELECT et CASE étaient déjà réservés, TO, ELSE et END existent.
- Fait : `Ast.cs` (SelectStatement, CaseItem, CaseStatement, CaseElseStatement, EndSelectStatement), `Parser.cs` (lecture ; vérification des blocs avec les autres : CASE sans SELECT, CASE après CASE ELSE, autre chose qu'un CASE juste après SELECT, SELECT jamais refermé, END SELECT seul, texte et nombre mélangés, bloc ouvert dans un CASE et pas refermé avant le suivant), `Diagnostic.cs` (6 messages), `CodeGenerator.cs` : la valeur est calculée une fois et gardée dans une variable cachée (§1, §1!, §1$ selon le type et la profondeur) ; chaque CASE devient une condition (OR de ses éléments) qui saute au cas suivant, chaque fin de cas saute après END SELECT.
- Tests : 2073 (`SelectCaseTests.cs` : nombres, textes, nombres à virgule, mélanges, SELECT dans un SELECT, CASE ELSE, 11 erreurs).
- Essai (23 nov. 2026) : `examples\choix.bas` (chiffre : zéro / impair / pair ; lettre : voyelle / consonne avec `CASE "A" TO "Z"` ; autre touche) dans WinAPE : « c'est bon ».

## D207 : RUN "nom" sur la cartouche GX4000 (23 nov. 2026) (demande de l'utilisateur du 22 nov. 2026 ; Validé le 23 nov. 2026 : 2083 tests réussis ; cartmenu.cpr dans AMSpiriT : les deux programmes se lancent l'un l'autre)

- Idée : « à quoi sert d'avoir 512 Ko si on ne peut en utiliser que 64 ? » (IDEES.md). Aucun mot nouveau : le même programme marche sur disquette (F5) et sur cartouche (Ctrl+F5). Question de l'utilisateur : LOAD "nom",adresse pour des données ; réponse : même mécanisme, commande du Locomotive déjà faite sur disquette (D085), étape suivante.
- Ctrl+F5 compile aussi en chaîne (comme D087) : chaque RUN "nom" écrit en clair fait compiler nom.BAS du même dossier, puis ceux qu'il lance ; tous sont rangés dans la cartouche, à la suite du premier programme, à partir d'une nouvelle page (un fichier peut passer d'une page à la suivante). Le premier programme y est rangé aussi si l'un des autres y revient. Les touches placées sur la manette (D183) et la saisie à la manette (D184) valent pour tous. Programme introuvable : signalé, la cartouche est faite quand même. Cartouche de plus de 512 Ko : refusée avec un message.
- Notre système (`Gx4000System.cs`) : table FICHIERS (21 octets par fichier : nom rangé comme AMSDOS sur 8 + 3 lettres, type, adresses de chargement et de lancement, longueur, page, place dans la page) ; CAS IN OPEN (&BC77 : majuscules, sans extension : essaie sans, puis .BAS, puis .BIN, comme AMSDOS ; absent : « NOM not found », code 146 pour DERR), CAS IN DIRECT (&BC83 : chaque page branchée en &C000 et lue, l'écriture va toujours dans la mémoire vive, même dans l'écran ; interruptions coupées pendant la copie), CAS IN CLOSE et CAS IN ABANDON (&BC7A, &BC7D). Entrée fixe &004F (LANCER) : recopie le fichier ouvert à son adresse (il remplace le programme en cours : la routine tourne depuis la cartouche), oublie les minuteries et ON SQ de l'ancien programme, puis le lance. Variables en &BEE0 à &BEF5.
- Compilateur : réglage interne `Cartridge` (CompilerOptions) ; sur la cartouche, RUN "nom" passe par le module `FileRunCartridge` (rt_fbc_run : nom gardé dans le programme, « .BIN » ajouté, CAS IN OPEN, puis saut en &004F) au lieu du petit chargeur en &3800 (la cartouche occupe &0000 à &3FFF : il n'y serait pas lu). L'ancien module n'est plus ajouté sur la cartouche (trouvé par un test).
- Simulateur des tests : `MiniZ80` connaît les pages d'une cartouche (port &DF, ROM haute branchée ou coupée par le Gate Array).
- Tests : 2083 (`CartridgeFilesTests.cs` : fichier sur deux pages, fichier recopié dans l'écran, fichier absent, RUN qui recopie et lance, code de RUN sur cartouche, compilation en chaîne, programme introuvable, erreur dans un programme lancé, cartouche trop pleine).
- Exemples : `examples\cartmenu.bas` (MODE 1) lance `examples\cartjeu.bas` (MODE 0, 15 bandes de couleur), qui revient au premier.
- Suite : LOAD "nom"[,adresse] sur la cartouche (fichiers de données du dossier rangés dans la cartouche).

## D208 : LOAD "nom"[,adresse] sur la cartouche GX4000 (23 nov. 2026) (question de l'utilisateur pendant D207 ; Validé le 23 nov. 2026 : 2088 tests réussis ; charge.cpr dans AMSpiriT : l'image apparaît d'un coup)

- Aucun mot nouveau : LOAD du Locomotive (D085), même routine rt_fb_load ; sur la console, CAS IN OPEN / DIRECT / CLOSE de notre système (D207) lisent le fichier dans la cartouche.
- Ctrl+F5 : pour chaque LOAD "nom" écrit en clair (dans tous les programmes de la cartouche), le fichier est cherché dans le dossier du programme : « NOM » puis « NOM.BIN » (comme AMSDOS, sans .BAS pour ne pas prendre un programme), ou le nom exact s'il a une extension. Il est rangé dans la cartouche après les programmes. Avec un en-tête AMSDOS (128 octets, somme des octets 0 à 66 en 67 et 68) : type, adresses et longueur lus dans l'en-tête, l'en-tête n'est pas rangé. Sans en-tête : fichier binaire, LOAD doit donner l'adresse (sinon message : « Écrivez-la après le nom, par exemple LOAD "X",&C000 »). Fichier absent : signalé, la cartouche est faite. LOAD avec un nom calculé : un avertissement. Un même fichier n'est rangé qu'une fois.
- Fait : `Compiler.cs` (DataFile, ReadAmsdos, CartridgeResult.Data et MissingData), éditeur (`MainWindow.axaml.cs`) et ligne de commande (fichiers du même dossier).
- Tests : 2088 (5 nouveaux dans `CartridgeFilesTests.cs`, dont un programme compilé qui charge l'écran depuis la cartouche dans le simulateur).
- Exemple : `examples\charge.bas` et `examples\IMAGE.BIN` (écran MODE 1 de 16 Ko avec en-tête AMSDOS, chargé en &C000 : cadre et anneaux de couleur ; ovales au premier essai, refait ronds pour le vrai moniteur, facteur de D140).
- Reste (étape suivante) : sur disquette (F5), mettre aussi ces fichiers sur la disquette (idée D087), pour que le même programme marche partout.

## D209 : les fichiers de LOAD "nom" sur la disquette, et fichiers de plus de 16 Ko (23 nov. 2026) (idée de l'utilisateur notée en D087 ; Validé le 23 nov. 2026 : 2091 tests réussis ; charge.dsk dans WinAPE : l'image est lue sur la disquette)

- F5 : pour chaque LOAD "nom" écrit en clair (dans tous les programmes compilés en chaîne), le fichier du dossier est cherché comme pour la cartouche (D208 : « NOM » puis « NOM.BIN », ou le nom exact) et mis sur la disquette avec son en-tête AMSDOS (adresses gardées ; sans en-tête : fichier binaire, adresse 0, et le même message d'aide si LOAD n'a pas d'adresse). Fichier absent : signalé, la disquette est faite. Pas encore sur la cassette (le message le dit). Le même programme marche donc sur disquette et sur cartouche.
- Disquette (`DiskImage.cs`) : un fichier de plus de 16 Ko prend plusieurs entrées du répertoire, comme AMSDOS (octet 12 : numéro de la partie, octet 15 : enregistrements de 128 octets de la partie) ; avant, il était refusé (un écran de 16 Ko avec son en-tête ne passait pas). Les programmes de plus de 16 Ko en profitent aussi.
- Fait : `Compiler.cs` (CompileWithLinked reçoit findData ; CompileResult.Data, MissingData, DataNotes), éditeur (messages de F5).
- Tests : 2091 (`DiskDataTests.cs`, 3 tests ; le test « fichier de plus de 16 Ko refusé » remplacé par la vérification des deux entrées du répertoire).
- Essai : `examples\charge.bas` dans WinAPE (F5, RUN"CHARGE") : l'image ronde apparaît après la lecture de la disquette.
- Reste : ces fichiers sur la cassette (dans l'ordre de lecture) ; RUN"!NOM" (IDEES.md).

## D210 : PLOT et PLOTR rapides sur CPC (23 nov. 2026) (demande de l'utilisateur : « PLOT rapide », IDEES.md)

- Mesure d'abord (`examples\plottemp.bas`, chronomètre dans WinAPE, 32 000 points ; TIME 16 bits : passage par zéro corrigé dans le programme) : avant, `PLOT x,y` 20,42 s, `PLOT x,y,stylo` 60,12 s ; boucle seule 2,29 s ; MOVE 6,58 s. Constat : un appel à la ROM coûte cher (MOVE : environ 4,3 s pour 32 000) ; le PLOT de la ROM n'est pas beaucoup plus lent qu'un MOVE.
- Première version (réglages gardés, mais GRA MOVE ABSOLUTE et lecture du stylo à chaque point) : 19,1 s et 80 s : trop peu, et plus lent avec un stylo. Récrite.
- Fait (`LineRuntime.cs`, entrées PLOTN, PLOTRN) : aucun appel à la ROM pour un point. Réglages lus une fois (écran en &C000 sans décalage, sinon la ROM ; MODE, origine, fenêtre graphique) ; curseur (PX, PY) et stylo (PENSET : PLOT x,y,stylo) gardés dans le module, stylo codé par nous (comme SCR INK ENCODE) ; vérification de la fenêtre en octets pour la hauteur ; adresse calculée ((ligne AND &F8) x 10, + (ligne AND 7) x &800) ; ROM haute coupée juste pour lire et écrire l'octet. Le compilateur (`CodeGenerator.cs`) appelle SYNC avant toute instruction qui pourrait changer les réglages (MODE, ORIGIN, PRINT, CALL..., liste SafeForPlot : calculs, boucles, sauts, tracés : pas d'appel) : curseur et stylo donnés à la ROM, réglages à relire ; FLUSH avant MOVE, MOVER, DRAW, DRAWR et toute instruction avec XPOS, YPOS, TEST, TESTR.
- Gardent le PLOT de la ROM : cartouche (notre système est déjà rapide), MASK, mode d'écriture (4e valeur), EVERY, AFTER, ON SQ, ON BREAK, TRON, écran déplacé.
- Tests : 2099 (comparaison point par point avec GRA PLOT de notre système de cartouche, 3 MODE, 60 réglages au hasard de 40 points : origine, fenêtre, stylo gardé, PLOTR, curseur et stylo rendus à la ROM ; choix du compilateur). MiniZ80 : SCR GET LOCATION simulé (par défaut « écran ailleurs », pour que les anciens tests de programmes gardent les routines GRA simulées).
- Résultat (WinAPE, 23 nov. 2026) : `PLOT x,y` 12,72 s (20,42 avant : sans la boucle, 1,7 fois plus rapide), `PLOT x,y,stylo` 45,75 s (60,12 avant ; le calcul de la couleur pèse lourd), MOVE 7,37 s (6,58 : FLUSH ajouté avant chaque MOVE dans un programme avec PLOT).

## D211 : les fichiers de LOAD "nom" sur la cassette (23 nov. 2026) (suite de D209 ; Validé le 23 nov. 2026 : 2100 tests réussis ; charge.cdt dans Caprice (464) : « C'est comme tu le dis »)

- F5 : chaque fichier de LOAD "nom" écrit en clair (trouvé comme en D209) va aussi sur la cassette, juste après son programme, dans l'ordre de ses LOAD, sous le nom écrit dans LOAD (« IMAGE ») ; un fichier lu plusieurs fois n'y est mis qu'une fois. Message de F5 : pour relire un fichier déjà passé, il faut rembobiner, comme sur un vrai CPC.
- Fait : `Compiler.cs` (fichiers cherchés pour chaque programme avant d'écrire la disquette et la cassette ; la disquette les reçoit aussi juste après leur programme), éditeur (messages).
- Tests : 2100 (`DiskDataTests.cs` : ordre exact sur la cassette avec deux programmes enchaînés et trois fichiers).
- Essai : `examples\charge.cdt` dans Caprice (464) : « Press PLAY then any key », « Loading IMAGE block... » puis l'image ; au tour suivant, la cassette attend d'être rembobinée (normal).
- Le même programme `charge.bas` marche maintenant sur disquette, cassette et cartouche.

## D212 : MOVE et MOVER sans appel à la ROM dans un programme avec PLOT rapide (23 nov. 2026) (suite de D210 ; Validé le 23 nov. 2026 : 2102 tests réussis)

- Constat (D210) : dans un programme avec PLOT rapide, MOVE passait par FLUSH puis par la ROM : 7,37 s pour 32 000 MOVE (6,58 s avant D210).
- Fait : `LineRuntime.cs`, entrées MOVEN et MOVERN : MOVE et MOVER sans stylo changent seulement le curseur gardé par nos points (PX, PY), donné à la ROM plus tard (FLUSH, avant DRAW, DRAWR, XPOS, YPOS, TEST, TESTR, ou SYNC). Le compilateur n'appelle plus FLUSH avant MOVE et MOVER. MOVE avec un stylo garde la ROM (après SYNC). Les programmes sans PLOT ne changent pas.
- Tests : 2102 (la comparaison point par point de D210 mélange MOVE et MOVER aux PLOT et PLOTR ; appel du module par MOVE et MOVER).
- Résultat (`examples\plottemp.bas`, WinAPE) : 32 000 MOVE en 3,69 s (boucle comprise, 2,29 s) au lieu de 6,58 s avant D210 : sans la boucle, environ 3 fois plus rapide. Les autres temps ne changent pas (12,72 s ; 45,75 s).

## D213 : VROOM son (23 nov. 2026) (demande de l'utilisateur : forme et sons choisis par lui ; Validé le 23 nov. 2026 : 2111 tests réussis ; examples\vroom.bas dans WinAPE : « Parfait »)

- Forme donnée par l'utilisateur : VROOM 1 (moteur qui monte), VROOM 2 (ralenti), VROOM 3 (moteur qui descend). Il avait écrit « VROUM » ; il a laissé Claude choisir (« l'informatique c'est en anglais ») : VROOM, le mot déjà prévu et coloré, comme BOOM, SHOT, FALL, WAVE. **Mot réservé nouveau : VROOM.**
- Sons choisis à l'oreille d'abord en BASIC normal (`examples\vroomref.bas`, SOUND, ENV, ENT) : premier essai « sirène » pour 1 et 3 (son pur qui glisse) ; refait sur le modèle du ralenti (volume qui pulse, bruit 25, montée qui reste dans le grave) ; 3 joué en 3 morceaux de plus en plus faibles (volume 12, 8, 4 ; une enveloppe de volume n'a que 5 parties et le volume repart en haut sous zéro), demande de l'utilisateur.
  - 1 : SOUND 1,900,-13,12 avec ENV 2,-6,2,2,6,2 et ENT 100,-5,1, bruit 25.
  - 2 : SOUND 1,900,-6,12 avec ENV 3,-4,4,3,4,4 et ENT (qui se répète) 5,8,1,5,-8,1, bruit 25.
  - 3 : SOUND 1,400,-4,12 ; 1,565,-4,8 ; 1,730,-5,4 (pulsation de plus en plus douce, ENT 33 ou 34 pas de +5), bruit 25.
- Fait : `Codegen\VroomRuntime.cs` (sons et enveloppes rangés dans le programme ; enveloppes posées juste avant chaque son : ENV 12 à 15, ENT 11 à 15 ; canal A, en arrière-plan, attente d'une place comme SOUND), `Parser.cs` (VROOM son de 1 à 3, vérifié à la compilation), `CodeGenerator.cs` (nombre calculé hors de 1 à 3 : « Improper argument »). Marche aussi sur la cartouche.
- Tests : 2111 (`VroomTests.cs` : les trois sons et leurs enveloppes, valeur calculée, erreurs, cartouche).
- Exemple : `examples\vroom.bas` (touches 1 à 3, et 4 : ralenti, montée, ralenti, descente).

## D214 : BOOM son [, durée] (23 nov. 2026) (demande de l'utilisateur : forme et sons choisis par lui ; Validé le 23 nov. 2026 : 2126 tests réussis ; examples\boom.bas dans WinAPE : « c'est trop top ! »)

- Forme donnée par l'utilisateur : BOOM 1 (une voiture qui explose), BOOM 2 (le boom d'un feu d'artifice), BOOM 3 (un pétard, un coup de fusil), BOOM 4 (un marteau qui frappe), BOOM 5 (le boom d'un bonus, surtout malus), et BOOM x, y avec y la durée (facultative) ; durée en centièmes de seconde comme SOUND (proposé par Claude, accepté). **Mot réservé nouveau : BOOM.**
- Sons choisis à l'oreille d'abord en BASIC normal (`examples\boomref.bas`), acceptés au premier essai (« c'est bien ») :
  - 1 : claquement (bruit 6, 8 centièmes), puis bruit 31 sur A et grondement grave (période 3000) sur B, éteints en 15 marches (ENV 15,-1,13 ; 200 centièmes).
  - 2 : sifflement qui monte (période 300, ENT 60,-3,1, volume 8, 60 centièmes), boom (bruit 15, ENV 15,-1,2), crépitement (bruit 4, ENV 5,-3,3,5,3,3,5,-3,3, 45 centièmes).
  - 3 : bruit 6, ENV 15,-1,2 (30 centièmes).
  - 4 : période 180 et bruit 10 sur A, période 90 sur B, ENV 15,-1,1 et ENT 20,8,1 (15 centièmes).
  - 5 : périodes 200 et 203 sur A et B, ENV 15,-1,6 et ENT 60,6,1 (90 centièmes).
- Durée donnée : les parties principales durent la durée ; le volume s'éteint en 15 marches de durée / 15 (de 1 à 255) ; la descente du malus fait 60 pas de durée / 60. Sans durée : la durée écoutée.
- Fait : `Codegen\BoomRuntime.cs` (enveloppes ENV 9 et 10, ENT 8 à 10, posées juste avant chaque son ; VROOM garde les siennes), `Parser.cs` (son de 1 à 5, durée de 1 à 32767, vérifiés à la compilation), `CodeGenerator.cs` (valeurs calculées hors limites : « Improper argument » ; nouveau JP M dans Z80Emitter). Marche aussi sur la cartouche.
- Tests : 2126 (`BoomTests.cs`, 15 tests : les cinq sons, durée donnée, valeurs calculées, erreurs, cartouche).
- Exemple : `examples\boom.bas` (touches 1 à 5 ; 6 : BOOM 1,500 ; 7 : BOOM 5,300).

## D215 : SHOT son (23 nov. 2026) (demande de l'utilisateur : forme et sons choisis par lui ; Validé le 23 nov. 2026 : 2136 tests réussis ; examples\shot.bas dans WinAPE : « c'est génial... super le 5 »)

- Forme donnée par l'utilisateur : SHOT 1 (un tir laser), SHOT 2 (un missile), SHOT 3 (une rafale), SHOT 4 (une vitre qui casse) ; **pas de durée** (« Non pas de durée pour shot »). **Mot réservé nouveau : SHOT.**
- Sons choisis à l'oreille d'abord en BASIC normal (`examples\shotref.bas`), acceptés au premier essai (« C'est extra ») :
  - 1 : période 30 qui plonge (ENT 30,9,1), ENV 15,-1,2 (30 centièmes).
  - 2 : souffle (bruit 3) et son qui descend (période 100, ENT 90,3,1) sur A et B, ENV 10,1,4,15,-1,4 (1 seconde).
  - 3 : six tirs de bruit 5, volume 14, ENV 7,-2,1 (9 centièmes chacun).
  - 4 : fracas (bruit 1, ENV 15,-1,4, 60 centièmes) sur A, six tintements sur B (périodes 40, 55, 35, 60, 45, 50, ENV 6,-2,1, 7 centièmes chacun).
- Fait : `Codegen\ShotRuntime.cs` (enveloppes ENV 4 à 8, ENT 6 et 7, posées juste avant chaque son), `Parser.cs` (son de 1 à 4), `CodeGenerator.cs` (traité avec VROOM ; valeur calculée hors de 1 à 4 : « Improper argument »). Marche aussi sur la cartouche.
- **À reparler avant FALL et WAVE :** avec VROOM (ENV 12 à 15, ENT 11 à 15), BOOM (ENV 9, 10, ENT 8 à 10) et SHOT (ENV 4 à 8, ENT 6, 7), il ne reste que ENV 1 à 3 et ENT 1 à 5 pour les programmes ; chaque effet a ses numéros pour que deux effets puissent jouer en même temps (moteur et tirs).
- Tests : 2136 (`ShotTests.cs`, 10 tests).
- Exemple : `examples\shot.bas` (touches 1 à 4 ; 5 : SHOT 2:BOOM 1, missile puis explosion).

## D216 : numéros d'enveloppes des effets sonores (23 nov. 2026) (point noté en D215 ; décision de Claude ; Validé le 23 nov. 2026 : 2142 tests réussis ; message vu dans l'éditeur)

- Chaque effet garde ses numéros (deux effets peuvent jouer en même temps : moteur et tirs) : VROOM ENV 12 à 15, ENT 11 à 15 ; BOOM ENV 9, 10, ENT 8 à 10 ; SHOT ENV 4 à 8, ENT 6, 7 (liste `BasicAnalyzer.EffectEnvelopes`). Un programme ne perd que les numéros des effets qu'il utilise.
- ENV ou ENT écrit en clair avec un numéro pris par un effet du même programme : erreur à la compilation (nouveau code EnvelopeTaken), avec les numéros libres : « ENV 13 : ce numéro est déjà pris par VROOM, qui se sert de ENV 12 à 15 dans ce programme. Choisissez un autre numéro : 1 à 11. » Un numéro calculé n'est pas vérifié.
- Tests : 2142 (`EffectEnvelopeTests.cs`, 6 tests). Essai dans l'éditeur (23 nov. 2026) : le message apparaît ; avec ENV 5, la compilation réussit.
- Pour FALL et WAVE : ajouter leurs numéros à la liste ; s'ils ne tiennent plus dans les numéros restants, partager avec l'effet le moins souvent joué en même temps.

## D217 : FALL son (23 nov. 2026) (sons proposés par Claude, choisis par l'utilisateur ; Validé le 23 nov. 2026 : 2151 tests réussis ; examples\fall.bas dans WinAPE : « c'est bon »)

- L'utilisateur ne connaissait pas FALL (« tomber ») ; quatre sons proposés (bombe qui tombe, chute dans le vide puis le sol, objet qui dégringole un escalier, vie perdue), sans durée comme SHOT. Essai à l'oreille (`examples\fallref.bas`) : la bombe ressemblait à « un feu d'artifice inversé », refaite (aiguë, descente lente, de plus en plus forte) ; choix final de l'utilisateur : garder seulement la chute (devient FALL 1) et la vie perdue (devient FALL 2) ; « les autres ne vont pas dans FALL ». **Mot réservé nouveau : FALL.**
  - 1 : période 100 qui plonge (ENT 100,6,1, volume 12, 1 seconde), puis le sol : période 2000, bruit 25, ENV 15,-1,1 (15 centièmes).
  - 2 : notes de périodes 239, 253, 268 (25 centièmes chacune, volume 12), puis 284 pendant 60 centièmes, qui tremble (ENT qui se répète 2,2,1,2,-2,1) et s'éteint (ENV 12,-1,5).
- Fait : `Codegen\FallRuntime.cs` (préfixe rt_fa_ : rt_fl_ est déjà pris par la lecture de fichiers ; enveloppes ENV 2 et 3, ENT 4 et 5), `Parser.cs` (son 1 ou 2), `CodeGenerator.cs` (avec VROOM et SHOT), `BasicAnalyzer.EffectEnvelopes` (D216). Marche aussi sur la cartouche.
- Tests : 2151 (`FallTests.cs`, 9 tests).
- Exemple : `examples\fall.bas` (touches 1 et 2 ; 3 : SHOT 1, BOOM 3, FALL 2).
- Idée de l'utilisateur à retenir : « Nouveau » dans le menu Fichier (IDEES.md) attendra la prochaine retouche de l'éditeur (« l'éditeur sera retouché avec d'autres options »).

## D218 : réserve commune des numéros d'enveloppes des effets sonores (23 nov. 2026) (remplace D216 ; décision de Claude, pour WAVE ; Validé le 23 nov. 2026 : 2163 tests réussis)

- Constat : WAVE demande 12 enveloppes de volume (4 sons à l'endroit et à l'envers, la grosse vague et le plouf en 2 morceaux) ; les autres effets en avaient déjà 13 des 15 du CPC (D216 : numéros fixes par effet).
- Nouvelle règle : le compilateur range au départ du programme (module `Codegen\EffectsRuntime.cs`, préfixe rt_fx_) la liste des numéros d'ENV et d'ENT que le programme n'écrit pas lui-même en clair, du plus grand au plus petit ; chaque enveloppe d'un effet prend le numéro suivant (NEWENV, NEWENT), chacun son tour, et on recommence au bout. Les numéros du programme ne sont jamais touchés ; un programme sans ENV laisse ses 15 numéros aux effets ; un son en attente ou joué garde son enveloppe tant que la réserve n'a pas fait le tour. Un numéro calculé (ENV N, ...) ne peut pas être connu.
- VROOM, BOOM, SHOT et FALL passent par la réserve : **mêmes sons**, numéros choisis au moment de jouer. Les modules appellent NEWENV et NEWENT par le résolveur de l'assembleur (EffectsRuntime.Resolve).
- Seul refus restant (code EnvelopeTaken, nouveau message) : le programme écrit tant de numéros qu'il n'en reste pas assez pour un effet (au plus à la fois : VROOM 3 ENV et 3 ENT, BOOM, SHOT et WAVE 2 ENV et 1 ENT, FALL 1 et 1 ; `BasicAnalyzer.EffectNeeds`).
- Tests : `EffectEnvelopeTests.cs` refait (numéros du programme jamais pris, tour de la réserve, refus) ; tests des quatre effets mis aux nouveaux numéros.

## D219 : WAVE son [, envers] (23 nov. 2026) (demande de l'utilisateur : forme et sons choisis par lui ; Validé le 23 nov. 2026 : 2163 tests réussis ; examples\wave.bas dans WinAPE : « c'est bon ça marche bien »)

- Forme donnée par l'utilisateur : 3 ou 4 sortes de vagues pour une intro (FOR a=1 TO 100:WAVE 1+RND*3:NEXT, une île), WAVE 4 gros plouf (personne ou objet qui tombe dans l'eau), et « tous les WAVE devront être joués à l'envers » : WAVE n, 1 (de la fin vers le début). Retenu : WAVE son [, envers], son de 1 à 4, envers 0 ou 1. **Mot réservé nouveau : WAVE.**
- Remarque faite à l'utilisateur : comme sur le CPC, WAVE 1+RND*3 arrondit et donne parfois 4 (le plouf) ; pour les vagues seules : WAVE 1+INT(RND*3).
- Sons choisis à l'oreille (`examples\waveref.bas`) : 1, 2, 4 validés au premier essai ; la fin du 3 (souffle très aigu, bruit 6, 1,5 seconde) faisait « un bruit étrange » : refaite (bruit 16, 1 seconde) ; « c'est adopté ».
  - 1 : bruit 12, volume 4, ENV 8,1,4,12,-1,6 (104 centièmes) ; à l'envers : volume 0, ENV 12,1,6,8,-1,4.
  - 2 : bruit 18, volume 5, ENV 10,1,5,15,-1,8 (170) ; à l'envers : volume 0, ENV 15,1,8,10,-1,5.
  - 3 : bruit 28, volume 3, ENV 12,1,4 (48), puis bruit 16, volume 15, ENV 15,-1,7 (105) ; à l'envers : bruit 16 qui monte (ENV 15,1,7), puis bruit 28 qui s'éteint (ENV 12,-1,4).
  - 4 : période 60 qui plonge (ENT 15,16,1), volume 14, ENV 7,-2,2 (15) sur A ; gerbe (bruit 4, ENV 15,-1,3, 45) sur B ; à l'envers : la gerbe monte sur B, 30 centièmes de silence sur A, puis la période 300 qui remonte (ENT 15,-16,1, ENV 7,2,2).
- Fait : `Codegen\WaveRuntime.cs` (préfixe rt_wv_ ; une table de 8 entrées : 4 sons, 4 à l'envers), `Parser.cs`, `CodeGenerator.cs` (valeurs calculées hors limites : « Improper argument »). Marche aussi sur la cartouche.
- Tests : 2163 (`WaveTests.cs`).
- Exemple : `examples\wave.bas` (1 à 4 ; 5 à 8 : à l'envers ; 9 : une île, 20 vagues au hasard).
- **Les cinq effets sonores sont faits** (VROOM, BOOM, SHOT, FALL, WAVE). Reste dans la liste du début : SCROLL.

## D220 : préparation de la version 0.8.0 (24 nov. 2026) (choix de l'utilisateur : « on lance la v0.8.0 »)

- **Contenu :** sprites (D198 à D201), traits rapides (D202 à D205), SELECT CASE (D206), RUN "nom" et LOAD sur cartouche (D207, D208), fichiers de LOAD sur disquette et cassette, fichiers de plus de 16 Ko (D209, D211), PLOT, PLOTR, MOVE, MOVER rapides (D210, D212), effets sonores VROOM, BOOM, SHOT, FALL, WAVE et réserve des enveloppes (D213 à D219) ; notes dans `docs\RELEASE-NOTES-0.8.0.md`.
- **Fichiers :** version 0.8.0 dans `Directory.Build.props` et `packaging\build-release.cmd` ; `packaging\LISEZMOI.txt` (fichiers de données au point 4 ; SELECT CASE, sprites, effets sonores et vitesse au point 5 ; RUN "nom" et LOAD au point 8) ; README (français et anglais) et TESTEURS.md ; IDEES.md (lignes faites marquées).
- **Oubli corrigé :** le paquet ne contenait que les exemples `.bas` ; `build-release.cmd` copie aussi les `.bin` des exemples (IMAGE.BIN, pour charge.bas).
- **Paquet construit (24 nov. 2026) :** `release\CpcBasic-0.8.0-win-x64.zip`, SHA256 105204520c2b525042ad0ec0370ea8ab684694a28730a585a6d6ef505ffee319 ; contenu vérifié : 357 fichiers, 120 exemples et IMAGE.BIN, bibliothèques GRAPHIQUE et TEXTE, pas de ROM, pas de .dsk, .cdt ni .cpr. Essayé par l'utilisateur depuis le dossier du paquet (charge.bas : IMAGE.BIN mis sur la disquette et la cassette).
- **Publiée (24 nov. 2026) :** pre-release GitHub v0.8.0, « CPC BASIC IDE 0.8.0 (version de test) », commit 60ce1d1.

## D221 : tableaux d'octets (24 nov. 2026) (demande de l'utilisateur, D058 ; choix : « le tableau » ; Validé le 24 nov. 2026 : 2169 tests réussis ; examples\octets.bas dans WinAPE : écran attendu)

- Forme décidée en D058, **aucun mot nouveau** : `BYTE T` écrit avant `DIM T(100)` fait de T un tableau d'octets ; chaque case va de 0 à 255 et prend 1 octet au lieu de 2. Le refus « pas encore » (code ByteArrayNotYet) est retiré.
- Mêmes règles qu'une variable BYTE : une valeur calculée trop grande ne garde que son octet de poids faible (300 donne 44) ; une valeur écrite en clair hors de 0 à 255 est refusée à la compilation (ByteOutOfRange) ; `BYTE T` après l'usage du tableau : « déclarée après usage » (DeclaredAfterUse). INPUT dans un tableau d'octets redemande la valeur si elle dépasse 255.
- Marchent aussi : plusieurs dimensions, READ, INPUT, `@T(3)`, ERASE, DEBUG, liste des variables (F4), cartouche.
- Fait : `Parser.cs` (vérifications), `CodeGenerator.cs` (taille de case 1 : lecture `LD L,(HL)` puis `LD H,0`, écriture du seul octet faible), `RuntimeLibrary.cs` (les trois calculs d'adresse ne doublent pas quand la case fait 1 octet).
- Tests : 2169 (`ArrayTests.cs` : l'ancien test du refus remplacé par 7 tests).
- Exemple : `examples\octets.bas` (5001 entiers : 10002 octets ; 5001 octets : 5001 octets).

## D222 : PAUSE et CLIC (24 nov. 2026) (idées de l'utilisateur notées en D049 ; choix : « les 2 en même temps » ; Validé le 24 nov. 2026 : 2190 tests réussis ; examples\pause.bas dans WinAPE : 303 centièmes pour PAUSE 3, compteur à 25, BRAVO après la touche A)

- Suite de D049 (PAUSE et CLIC réservés pour plus tard). **Mots réservés nouveaux : PAUSE, CLIC** (forme donnée par l'utilisateur).
- `PAUSE [heure,][minute,] seconde` : une valeur = secondes, deux = minutes, secondes, trois = heures, minutes, secondes ; chaque valeur de 0 à 32767 (en clair : vérifiée à la compilation ; calculée et négative : « Improper argument »). Temps de KL TIME PLEASE (1/300 s) ; chaque seconde est comptée depuis le départ (pas de dérive) ; total sur 32 bits.
- `CLIC code` : attend la touche de ce code ASCII, de 31 à 255 (calculé hors limites : « Improper argument ») ; les touches tapées avant sont oubliées ; majuscule et minuscule sont deux codes différents (CLIC 65 : A, CLIC 97 : a). Sur la cartouche : les codes de la manette (tir 1 = 32, flèches = 240 à 243).
- Pendant l'attente : EVERY, AFTER, ON SQ et la touche ESC sont traités comme entre deux lignes (si le programme s'en sert) ; les sons continuent. Marche aussi sur la cartouche.
- Fait : `Codegen\PauseRuntime.cs` (préfixe rt_pz_), `Parser.cs`, `CodeGenerator.cs` (boucle d'attente écrite dans le programme, avec le regard sur les minuteries), `RuntimeLibrary.cs`, couleurs de l'éditeur (`BasicColorizer.cs`).
- Tests : 2190 (`PauseTests.cs` ; le mini-Z80 a une horloge qui avance, `ClockStep`).
- Exemple : `examples\pause.bas`.
- Idées notées le même jour (IDEES.md, outils et éditeur) : planches des caractères et des couleurs dans l'Aide ; éditeur de caractères et de sprites qui ajoute une ligne de données au programme.

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
