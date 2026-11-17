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
