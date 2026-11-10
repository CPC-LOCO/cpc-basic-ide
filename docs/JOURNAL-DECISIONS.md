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

## D026 : Types de J2 (DÃ©cidÃ©, 8 nov. 2026)
Deux types de variables : entier signÃ© 16 bits (`A` ou `A%`, sans dÃ©claration) et octet non signÃ© 8 bits (voir D032). Le suffixe fait partie du nom : `A` et `A%` sont des variables distinctes, comme dans le Locomotive BASIC. DiffÃ©rence assumÃ©e : dans le Locomotive, `A` est un rÃ©el. RÃ©els et chaÃ®nes : Ã©tapes ultÃ©rieures (voir D016 pour les ROM).
DÃ©passement : un littÃ©ral hors intervalle est une erreur de compilation ; Ã  l'exÃ©cution, le calcul boucle (modulo 256 pour l'octet, modulo 65536 pour l'entier), comportement natif du Z80, sans test coÃ»teux.

## D027 : Variables et expressions (DÃ©cidÃ©, 8 nov. 2026)
Variables allouÃ©es statiquement aprÃ¨s les textes : 1 octet pour un octet, 2 octets (petit-boutiste) pour un entier. Seules les variables utilisÃ©es sont rÃ©servÃ©es ; valeur initiale 0. Expressions Ã©valuÃ©es dans HL, opÃ©rande de gauche empilÃ©. Lecture d'un octet : LD A,(var) / LD L,A / LD H,0. Ã‰criture : LD A,L / LD (var),A. Les modules PrintInt, Mul16 et Div16 ne connaissent donc que HL. Les futurs tableaux hÃ©riteront du type de leur Ã©lÃ©ment.

## D028 : Format de dÃ©claration des modules (Ouvert, 8 nov. 2026)
PrÃ©cise la question laissÃ©e ouverte par D024 : format de dÃ©claration des modules de la bibliothÃ¨que sous forme de fichiers (source Z80 et mÃ©tadonnÃ©es). Ã€ trancher avant l'ouverture du projet de commandes Z80.

## D029 : Suffixe du type octet (RemplacÃ©e par D032, 8 nov. 2026)
Le suffixe Â« ~ Â» avait Ã©tÃ© retenu provisoirement (Â« @ Â» Ã©cartÃ© : adresse de variable du Locomotive BASIC). AbandonnÃ© : Â« ~ Â» demande AltGr sur clavier AZERTY. Voir D032.

## D030 : NumÃ©ro non utilisÃ©
RÃ©servÃ© pendant la discussion sur le suffixe, finalement sans objet.

## D031 : Bilan de l'Ã©tape 2a (DÃ©cidÃ©, 8 nov. 2026)
- Variables entiÃ¨res (A, A%) implicites ; variables octet dÃ©clarÃ©es avec BYTE (voir D032). Affectation avec ou sans LET, expressions avec + et - (parenthÃ¨ses, moins unaire), PRINT d'expressions, PRINT avec plusieurs Ã©lÃ©ments sÃ©parÃ©s par Â« ; Â». La virgule dans PRINT n'est pas gÃ©rÃ©e.
- Les types $ et ! sont refusÃ©s avec un message (UnsupportedVariableType).
- Nouveau module PrintInt, liÃ© seulement si un nombre est affichÃ©. Format vÃ©rifiÃ© sur WinAPE : espace devant (ou Â« - Â»), chiffres, espace final, identique au BASIC rÃ©el (`PRINT 5;"X"` donne Â« 5 X Â»).
- ExpectedString n'est plus produit (PRINT accepte les expressions).
- ValidÃ© sur WinAPE avec `examples/variable.bas`.
- Limite : le nom du fichier .bas est limitÃ© Ã  8 caractÃ¨res (D022).

## D032 : Type octet par dÃ©claration BYTE (DÃ©cidÃ©, 8 nov. 2026)
Remplace D029. Une variable octet (0 Ã  255) se dÃ©clare avec `BYTE nom [, nom ...]`, sans suffixe, avant sa premiÃ¨re utilisation. Les entiers (A, A%) restent implicites.
Choix de BYTE plutÃ´t que CHAR : CHAR Ã©voque un caractÃ¨re de texte. BYTE n'est pas un mot rÃ©servÃ© du Locomotive BASIC (vÃ©rifiÃ© dans WinAPE : `BYTE=5` fonctionne), mais un programme du Locomotive qui nomme une variable BYTE n'est plus compilable.
Erreurs : double dÃ©claration, dÃ©claration aprÃ¨s usage, suffixe dans la dÃ©claration, littÃ©ral hors de 0 Ã  255. Un calcul qui dÃ©passe 255 boucle (modulo 256). Option OPTION EXPLICIT (dÃ©claration obligatoire pour tous les types) possible plus tard. ComplÃ¨te D026 et D027.

## D033 : Boucles FOR/NEXT, EXIT, CONTINUE (DÃ©cidÃ©, 8 nov. 2026)
- Syntaxe : `FOR v = dÃ©part TO limite [STEP pas] ... NEXT [v]`. Limite et pas : nombres Ã©crits dans le programme (pas de variables pour l'instant). Pas non nul.
- Test au NEXT (corps exÃ©cutÃ© au moins une fois), comme sur le CPC. Comparaison signÃ©e (bit 15 retournÃ© puis comparaison non signÃ©e). DÃ©bordement de l'entier : fin de boucle.
- Variable de boucle : entier ou BYTE dÃ©clarÃ©. BYTE : limite dans 0..255, pas dans -255..255.
- EXIT et CONTINUE : sauts rÃ©solus Ã  la compilation (pile de boucles dans le gÃ©nÃ©rateur, aucun nettoyage sur la pile du Z80).
- Ã‰cart avec le Locomotive : aprÃ¨s la boucle, la variable garde sa derniÃ¨re valeur (le CPC lui laisse la valeur suivante).
- Mots rÃ©servÃ©s ajoutÃ©s : FOR, NEXT, TO, STEP, EXIT, CONTINUE (EXIT n'est pas utilisÃ© par le Locomotive, vÃ©rifiÃ© dans WinAPE).
- ValidÃ© sur WinAPE avec `examples/boucles.bas`.

## D034 : Comparaisons et IF...THEN...ELSE (DÃ©cidÃ©, 8 nov. 2026)
- Comparaisons = <> < <= > >= entre deux expressions, signÃ©es sur 16 bits (bit 15 retournÃ© puis comparaison non signÃ©e par la retenue ; > et <= en Ã©changeant les opÃ©randes ; = et <> par SBC seul). AND/OR entre comparaisons, Ã©valuation de gauche Ã  droite avec arrÃªt anticipÃ© ; AND passe avant OR.
- IF sur une ligne (compatible Locomotive) : tout le reste de la ligne appartient au IF, ELSE se rattache au IF le plus proche. IF en bloc (THEN en fin de ligne, ELSE, END IF ou ENDIF) : extension du projet, absente du Locomotive.
- Le parser Ã©met IfStatement / ElseStatement / EndIfStatement ; la fin d'un IF sur une ligne est ajoutÃ©e automatiquement. VÃ©rification des blocs avec une pile commune.
- Limites : condition = comparaison (pas de nombre seul), pas de NOT, pas de parenthÃ¨ses autour d'une condition, pas de IF ... THEN numÃ©ro (GOTO viendra plus tard).
- Mots rÃ©servÃ©s ajoutÃ©s : IF, THEN, ELSE, ENDIF, AND, OR (ENDIF seul n'existe pas dans le Locomotive).
- ValidÃ© sur WinAPE avec `examples/cond.bas`.

## D035 : WHILE, REPEAT, DO...LOOP, EXIT/CONTINUE par type (DÃ©cidÃ©, 8 nov. 2026)
- WHILE c ... WEND (test en haut), REPEAT ... UNTIL c (test en bas, sortie quand c est vrai), DO [WHILE c | UNTIL c] ... LOOP [WHILE c | UNTIL c] (une seule condition par DO).
- EXIT / CONTINUE [FOR | WHILE | REPEAT | DO] : saut rÃ©solu Ã  la compilation vers la boucle la plus proche du type demandÃ© (ou la plus proche tout court). CONTINUE revient au test.
- Le gÃ©nÃ©rateur garde une pile de boucles commune Ã  tous les types ; l'Ã©metteur Z80 reÃ§oit AliasLabel pour donner deux noms Ã  une mÃªme adresse.
- Pile de vÃ©rification commune Ã  FOR, WHILE, REPEAT, DO et IF : ClosingWithoutOpening, UnclosedLoop, DoConditionTwice, BlocksNotNested.
- Mots rÃ©servÃ©s ajoutÃ©s : WHILE, WEND (Locomotive), REPEAT, UNTIL, DO, LOOP (Ã  vÃ©rifier dans WinAPE comme EXIT).
- ValidÃ© sur WinAPE avec `examples/loops.bas`.

## D036 : Bilan de J2 (DÃ©cidÃ©, 8 nov. 2026)
- **J2 est terminÃ© cÃ´tÃ© langage.** Un programme avec variables, calculs simples, boucles et conditions se compile et se lance dans WinAPE. 175 tests automatiques, trois programmes d'exemple vÃ©rifiÃ©s Ã  l'Ã©cran (`boucles.bas`, `cond.bas`, `loops.bas`).
- **Ã‰carts avec le Locomotive BASIC** : consignÃ©s dans le tableau de la section 8 de `ARCHITECTURE.md` (version 0.3).
- **Pas encore faits, par ordre de prioritÃ© proposÃ© pour J3** : multiplication, division et MOD (modules Mul16 et Div16) ; NOT ; GOTO et GOSUB avec Ã©tiquettes ; tableaux ; chaÃ®nes ; rÃ©els ; INPUT et clavier ; graphiques et sons ; DATA/READ.
- **Dettes connues** : accents (table des caractÃ¨res du CPC), confirmation Ã  l'ouverture d'un fichier modifiÃ© dans l'Ã©diteur, colonne des numÃ©ros de ligne collÃ©e aux numÃ©ros BASIC, entrelacement du .dsk, fichiers de plus de 16 Ko, messages d'erreur Ã  passer dans des fichiers de langue.
- **Ã€ traiter avant la prochaine publication** : D016 (ROM : vÃ©rifier que rien n'est redistribuÃ©) et relecture juridique de l'exception de liaison (D017).
## D037 : Multiplication, division et MOD (D�cid�, 9 nov. 2026)
- Op�rateurs `*`, `/` et `MOD` ; le signe `\` du Locomotive BASIC est accept� comme synonyme de `/`. `*`, `/` et `MOD` passent avant `+` et `-`. Le moins unaire se lie � son op�rande.
- `/` est une division enti�re (7 / 2 = 3), tronqu�e vers 0. �cart avec le Locomotive, o� `/` donne un r�el. `MOD` : le reste a le signe du dividende (-7 MOD 3 = -1), comme `\` et MOD du Locomotive.
- Diviseur �gal � un 0 �crit dans le programme : erreur de compilation (DivisionByZero). Diviseur calcul� �gal � 0 : pas d'erreur � l'ex�cution pour l'instant, le quotient vaut 0 et le reste vaut le dividende (� am�liorer, voir IDEES.md).
- Modules Mul16 (les 16 bits de poids faible du produit : m�me r�sultat sign� ou non) et Div16 (quotient dans HL, reste dans DE ; valeurs absolues puis signes corrig�s), li�s seulement s'ils sont utilis�s.
- D�passement : un produit trop grand boucle (200 * 200 = -25536), comme le reste des calculs (D026).
- Tests : un mini-�mulateur Z80 (`tests/CpcBasic.Tests/MiniZ80.cs`) ex�cute les modules et compare leurs r�sultats � C# sur des milliers de valeurs. Il ne conna�t que les instructions utilis�es ; s'il en rencontre une inconnue, il le dit.
- Mot r�serv� ajout� : MOD (d�j� un mot du Locomotive).
- Valid� sur WinAPE avec `examples/calculs.bas`. 205 tests.

## D038 : Fonctions de calcul (D�cid�, 9 nov. 2026)
- Syntaxe `NOM(valeur, ...)` dans une expression. Fonctions : ABS, SGN, MIN, MAX (existent dans le Locomotive, ici sur des entiers), HIBYTE, LOBYTE, CLAMP (nouvelles).
- HIBYTE(n) : octet de poids fort, 0 � 255. LOBYTE(n) : octet de poids faible, 0 � 255. Code court �crit directement dans le programme, sans module.
- CLAMP(valeur, mini, maxi) = MIN(MAX(valeur, mini), maxi) : ram�ne la valeur dans l'intervalle. Si mini > maxi, le r�sultat est maxi.
- MIN et MAX acceptent plus de deux valeurs : MIN(9, 4, 6) devient MIN(MIN(9, 4), 6) d�s l'analyse.
- ABS(-32768) reste -32768 (d�passement). SGN donne -1, 0 ou 1.
- Modules Abs16, Sgn16, Min16, Max16 (comparaison sign�e par retournement du bit 15), li�s seulement s'ils sont utilis�s. CLAMP lie Max16 et Min16.
- Messages d'erreur : fonction sans parenth�ses (ExpectedOpenParenthesis), mauvais nombre de valeurs (WrongArgumentCount) ; les deux montrent la bonne �criture, par exemple � CLAMP(valeur, min, max) �.
- Mots r�serv�s ajout�s : ABS, SGN, MIN, MAX, HIBYTE, LOBYTE, CLAMP. HIBYTE, LOBYTE, CLAMP (et RANDOM) v�rifi�s libres dans WinAPE.
- Pr�vu ensuite : RANDOM(min, max) et RANDOMIZE (D039), puis les graphismes de base et le calcul 3D en fil de fer.
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
