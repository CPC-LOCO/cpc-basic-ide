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
