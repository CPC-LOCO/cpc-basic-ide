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
