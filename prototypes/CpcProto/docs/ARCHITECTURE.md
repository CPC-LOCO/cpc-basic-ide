# Architecture du projet

Statut : version 0.1, rédigée à la fin du prototype J0 (7 novembre 2026).
Ce document décrit comment le projet est organisé. Il est mis à jour à chaque décision importante, qui est elle-même consignée dans `JOURNAL-DECISIONS.md`.

## 1. Principe général

Un BASIC évolué dérivé du Locomotive BASIC, écrit dans un éditeur sur PC, est **compilé** en code Z80, puis empaqueté dans un format exploitable sur Amstrad CPC : disquette `.dsk`, cassette, cartouche GX4000.

Le projet repose sur trois idées :

1. **Séparer le moteur de l'interface.** Le compilateur ne dépend d'aucune fenêtre.
2. **Bibliothèque de modules Z80 indépendants.** Chaque commande est une routine avec ses dépendances. Seules les routines atteintes depuis le programme sont liées au binaire (liaison sélective).
3. **Cibles décrites par des données.** Les différences entre machines (mémoire, formats, matériel Plus) sont dans des profils de cible, pas dispersées dans le code.

## 2. Les trois couches

| Couche | Rôle | Dépend de |
|---|---|---|
| **Core** (bibliothèque) | Analyse du BASIC, génération Z80, liaison sélective, écriture des formats de sortie | Rien |
| **Cli** (ligne de commande) | Compile un fichier source, lance les tests des exemples de la bible, sert de point d'entrée au futur projet Z80 | Core |
| **Ide** (éditeur graphique) | Édition, coloration, menus, aide contextuelle, affichage des erreurs | Core |

Organisation prévue du dépôt (provisoire) :

```
/src
  /CpcBasic.Core        moteur du compilateur
  /CpcBasic.Cli         outil en ligne de commande
  /CpcBasic.Ide         éditeur graphique
/runtime                modules Z80 de la bibliothèque (un fichier par commande)
/targets                profils de cible (464, 664, 6128, 464+, 6128+, GX4000)
/lang                   fichiers de langue de l'interface (fr, en, es)
/bible                  documentation en Markdown, exemples compilables
/tests                  tests automatiques
/docs                   ce document, le journal des décisions, le cahier des charges
/prototypes             prototype J0 (CpcProto)
```

## 3. Chaîne de compilation

```
Source BASIC
   -> Analyse lexicale (mots-clés, nombres, chaînes)
   -> Analyse syntaxique (arbre du programme)
   -> Vérifications (variables, types, commandes autorisées pour la cible)
   -> Génération de code Z80 (appels aux modules de la bibliothèque)
   -> Liaison sélective (seuls les modules utilisés)
   -> Mise en forme selon la cible
   -> Écriture : .dsk / cassette / cartouche
```

Les messages d'erreur sont produits dès les étapes d'analyse et de vérification. Ils citent la ligne, la variable ou la valeur en cause, et proposent une correction. Leurs textes viennent des fichiers de langue.

## 4. Bibliothèque de modules Z80

Chaque commande (CIRCLE, STAR, BOOM...) est un module Z80 indépendant. Un module déclare :

- son nom et la commande BASIC qu'il implémente ;
- ses dépendances (CIRCLE peut dépendre de PLOT) ;
- les cibles qui le supportent (par exemple, sprites matériels : Plus et GX4000 seulement) ;
- sa taille et ses besoins mémoire.

Le format exact de cette déclaration sera fixé en J1. Il devra être assez simple pour que le futur projet de commandes Z80 puisse ajouter un module sans modifier le compilateur.

## 5. Profils de cible

Un profil par machine décrit :

- la mémoire disponible (64 Ko ou 128 Ko) ;
- les formats de sortie permis ;
- la présence du matériel Plus (palette 4096 couleurs, sprites matériels, son DMA) ;
- les adresses système utiles (HIMEM par défaut, zones réservées).

Le choix de la cible filtre les commandes disponibles : une commande réservée au Plus est refusée, avec un message clair, pour un CPC classique.

## 6. Ce que le prototype J0 a établi

Le prototype (`CpcProto`) prouve la chaîne « octets Z80 -> .dsk -> lancement dans WinAPE ». Il fixe les points suivants.

**Format .dsk produit**

- Format standard « MV - CPCEMU Disk-File », 40 pistes, une face, 9 secteurs de 512 octets par piste.
- Identifiants de secteurs C1 à C9, dans l'ordre, sans entrelacement.
- Octet de remplissage &E5.
- Répertoire AMSDOS : 64 entrées de 32 octets sur les blocs 0 et 1 ; blocs de 1 Ko ; les fichiers démarrent au bloc 2.
- Chaque fichier est précédé d'un en-tête AMSDOS de 128 octets (type, adresse de chargement, adresse d'exécution, longueur, somme de contrôle sur les octets 0 à &42).

**Chargement et exécution**

- Un programme binaire chargé à &4000 impose de baisser HIMEM : sans `MEMORY &3FFF`, le BASIC répond « Memory full ».
- Le compilateur ajoute donc à la disquette un **chargeur BASIC tokenisé**, nommé comme le jeu, avec ces lignes : `MEMORY &3FFF`, `LOAD"nom.BIN"`, `CALL &4000`. L'utilisateur lance le tout par `RUN"nom"`.
- Codes du format tokenisé vérifiés : &AA = MEMORY, &A8 = LOAD, &83 = CALL, &1C = constante hexadécimale sur 2 octets, &22 = guillemet. Chaque ligne : longueur (2 octets), numéro (2 octets), codes, 0 ; le programme se termine par deux octets à 0.
- Un `RUN` direct sur un binaire ne mémorise pas d'adresse de retour : un `RET` final plante la machine. Un jeu compilé ne revient pas au BASIC : il finit par une boucle ou une attente. Lancé par `CALL` depuis le chargeur, un programme de test peut en revanche se terminer par `RET`.
- Les routines du firmware (par exemple &BB5A, TXT OUTPUT) sont utilisables telles quelles pour un premier `PRINT`.

**Limites du prototype**

- Un fichier ne dépasse pas 16 Ko (une seule entrée de répertoire par fichier).
- Pas d'entrelacement des secteurs : l'ordre séquentiel convient aux émulateurs, mais peut ralentir les chargements sur un vrai lecteur. À revoir avant les tests sur matériel réel.

## 7. Textes et langues

Tous les textes visibles (menus, messages d'erreur, aide) sont dans des fichiers de langue, un par langue : français, anglais, espagnol. Ajouter une langue revient à ajouter un fichier. Les messages d'erreur utilisent des emplacements pour les éléments variables (par exemple `{variable}`, `{ligne}`).

## 8. Bible et tests

La bible est écrite en Markdown, une page par instruction : description, syntaxe, exemples. Les exemples sont du vrai code : l'outil en ligne de commande les compile à chaque modification, pour détecter immédiatement un exemple cassé. Les formats HTML et PDF, ainsi que l'aide intégrée à l'éditeur, sont générés automatiquement à partir de ces fichiers.

## 9. Questions ouvertes (à trancher en J0 et J1)

1. Langage de l'outil : C# avec .NET et Avalonia (recommandé, à confirmer par un essai d'interface).
2. Assembleur Z80 : écrire le nôtre ou s'appuyer sur un outil libre existant.
3. Formats de sortie : cassette (blocs, en-têtes, vitesses) et cartouche GX4000 (structure, banques) à étudier dans la documentation existante.
4. Format de déclaration des modules de la bibliothèque.
5. Compatibilité avec le Locomotive BASIC : liste précise de ce qui est identique et de ce qui diffère (nombres flottants, chaînes, mémoire).
6. ROM et dispositions de clavier : comment les fournir sans les redistribuer.
7. Licence du code et de la bible.
