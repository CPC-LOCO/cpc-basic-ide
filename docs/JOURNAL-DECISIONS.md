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
