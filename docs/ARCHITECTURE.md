# Architecture du projet

Statut : version 0.2, rédigée à la fin du jalon J1 (7 novembre 2026).
Ce document décrit comment le projet est organisé, et distingue ce qui existe de ce qui est prévu. Il est mis à jour à chaque décision importante, qui est elle-même consignée dans `JOURNAL-DECISIONS.md`.

## 1. Principe général

Un BASIC évolué dérivé du Locomotive BASIC, écrit dans un éditeur sur PC, est **compilé** en code Z80, puis empaqueté dans un format exploitable sur Amstrad CPC : disquette `.dsk` (aujourd'hui), cassette et cartouche GX4000 (prévus).

Trois idées guident le projet :

1. **Séparer le moteur de l'interface.** Le compilateur ne dépend d'aucune fenêtre.
2. **Bibliothèque de modules Z80 indépendants.** Chaque commande est une routine avec ses dépendances. Seules les routines atteintes depuis le programme sont liées au binaire (liaison sélective).
3. **Cibles décrites par des données.** Les différences entre machines (mémoire, formats, matériel Plus) seront dans des profils de cible. Ce n'est pas encore fait : tout est fixé pour un CPC à lecteur de disquette.

## 2. Couches et organisation du dépôt

| Couche | Rôle | Dépend de |
|---|---|---|
| **CpcBasic.Core** (bibliothèque) | Analyse du BASIC, génération Z80, liaison sélective, écriture de la disquette | Rien |
| **CpcBasic.Cli** (ligne de commande) | Compile un fichier `.bas` en `.dsk` | Core |
| **CpcIde** (éditeur graphique Avalonia) | Édition, coloration, ouvrir, enregistrer, compiler, panneau de messages | Core |

Organisation actuelle :

```
cpc-basic-ide/
  CpcBasic.slnx
  src/
    CpcBasic.Core/
      Language/    Lexer, Parser, Ast, Diagnostic, BasicAnalyzer
      Codegen/     Z80Emitter, CodeGenerator, RuntimeLibrary (modules et liaison)
      Output/      DiskImage, BasicLoader, ByteUtil
      Compiler.cs  enchaîne analyse, génération, disquette
    CpcBasic.Cli/  outil en ligne de commande
    CpcIde/        MainWindow, BasicColorizer, App
  tests/CpcBasic.Tests/   tests automatiques
  examples/               programmes BASIC d'exemple
  docs/                   cahier des charges, architecture, journal des décisions
  prototypes/CpcProto/    prototype J0 (octets Z80 écrits à la main dans un .dsk)
```

Dossiers prévus, pas encore créés : `bible` (documentation en Markdown), `lang` (fichiers de langue), `targets` (profils de cible).

## 3. Chaîne de compilation

État actuel :

```
Source BASIC
   -> Lexer          jetons (mots en majuscules, "?" devient PRINT, commentaires ignorés)
   -> Parser         instructions : PRINT, END ; reprise après erreur
   -> CodeGenerator  octets Z80 via Z80Emitter ; modules liés par Linker
   -> DiskImage      NOM.BAS (chargeur BASIC) + NOM.BIN (programme Z80)
   -> fichier .dsk
```

Langage reconnu aujourd'hui : `PRINT "texte"` (avec `;` final pour supprimer le saut de ligne), `PRINT` seul, `?` comme `PRINT`, `END`, `REM` et `'` pour les commentaires, `:` pour séparer les instructions, numéros de ligne facultatifs.

Pas encore faits : variables et types, boucles (`FOR`/`NEXT`, prévues en J2), conditions, vérifications avant génération (variables, commandes autorisées pour la cible), profils de cible, cassette, cartouche GX4000.

Les erreurs sont toutes signalées d'un coup, avec la ligne de l'éditeur (pas le numéro BASIC) et une explication de la cause.

## 4. Bibliothèque de modules Z80

Chaque commande sera un module Z80 indépendant qui déclare son nom et ses dépendances. En J1, un seul module existe (`PrintString`, qui affiche un texte par la routine du firmware &BB5A).

**Choix actuel :** les modules sont écrits directement en C# avec l'assembleur interne (`Z80Emitter`), dans `RuntimeLibrary.cs`. C'est le plus simple pour démarrer et il permet de tester chaque module.

**À trancher avant le projet de commandes Z80 :** le format de déclaration de modules sous forme de fichiers (source Z80 et métadonnées), assez simple pour que le projet d'extension ajoute un module sans modifier le compilateur. Voir l'entrée D024 du journal.

Chaque module de `RuntimeLibrary.cs` porte la mention de licence « GPL v3 avec exception de liaison » (voir `RUNTIME-EXCEPTION.md`).

## 5. Profils de cible

Pas encore implémentés. Aujourd'hui, le compilateur produit toujours : adresse de chargement &4000, `MEMORY &3FFF` dans le chargeur, fichiers `.BAS` et `.BIN` sur disquette. Un profil par machine décrira plus tard : mémoire, formats de sortie permis, matériel Plus, adresses système. Le choix de la cible filtrera les commandes disponibles.

## 6. Ce que le prototype et J1 ont établi

**Format .dsk produit**

- Format standard « MV - CPCEMU Disk-File », 40 pistes, une face, 9 secteurs de 512 octets par piste, secteurs C1 à C9 dans l'ordre, sans entrelacement, remplissage &E5.
- Répertoire AMSDOS : 64 entrées de 32 octets sur les blocs 0 et 1 ; blocs de 1 Ko ; les fichiers démarrent au bloc 2 ; chaque fichier a un en-tête AMSDOS de 128 octets (type, adresse de chargement, adresse d'exécution, longueur, somme de contrôle sur les octets 0 à &42).
- Limites actuelles : un fichier ne dépasse pas 16 Ko ; pas d'entrelacement (à revoir pour un vrai lecteur).

**Chargement et exécution**

- Un binaire chargé à &4000 impose de baisser HIMEM : sans `MEMORY &3FFF`, le BASIC répond « Memory full ». Le compilateur ajoute donc un chargeur BASIC tokenisé : `MEMORY &3FFF`, `LOAD"NOM.BIN"`, `CALL &4000`. L'utilisateur lance le tout par `RUN"NOM"`.
- Codes du format tokenisé vérifiés : &AA = MEMORY, &A8 = LOAD, &83 = CALL, &1C = constante hexadécimale sur 2 octets, &22 = guillemet. Chaque ligne : longueur (2 octets), numéro (2 octets), codes, 0 ; le programme se termine par deux octets à 0.
- Un `RUN` direct sur un binaire ne mémorise pas d'adresse de retour : un `RET` final plante la machine. Un programme compilé ne revient pas au BASIC : il attend une touche (KM WAIT KEY, &BB18) puis redémarre (`JP 0`).
- Routines du firmware utilisées : &BB5A (TXT OUTPUT) et &BB18 (KM WAIT KEY).

**Organisation du binaire produit :** instructions du programme, fin de programme, modules utilisés, puis textes terminés par 0. Un programme sans `PRINT` ne contient aucun module (liaison sélective vérifiée par les tests).

**Limite connue :** les caractères hors ASCII (accents) sont remplacés par « ? » à l'affichage. La table des caractères du CPC reste à gérer.

## 7. Textes et langues

Prévu : tous les textes visibles (menus, messages d'erreur, aide) dans des fichiers de langue, un par langue (français, anglais, espagnol). **État actuel :** les messages d'erreur sont écrits en français dans `Diagnostic.cs`, ainsi que les textes de l'éditeur dans `MainWindow`. Ils rejoindront les fichiers de langue avant l'ajout de l'anglais et de l'espagnol.

## 8. Tests et bible

**Tests :** 46 tests automatiques (`dotnet test`) couvrent la fabrication de la disquette, le chargeur BASIC, l'analyse du langage, l'assembleur Z80, le générateur de code et le compilateur. Ils protègent le projet à chaque modification.

**Bible :** pas encore commencée. Prévue en Markdown, une page par instruction, avec des exemples compilés automatiquement.

## 9. Questions ouvertes

1. Format de déclaration des modules de la bibliothèque sous forme de fichiers (D024).
2. Assembleur Z80 : celui du projet suffit-il pour les modules complexes, ou faut-il un outil externe (D015) ?
3. Formats cassette et cartouche GX4000 : à étudier dans la documentation existante.
4. Compatibilité avec le Locomotive BASIC : liste précise de ce qui est identique et de ce qui diffère (flottants, chaînes, mémoire, abréviations de commandes).
5. ROM et dispositions de clavier : comment les fournir sans les redistribuer (D016).
6. Jeu de caractères du CPC : correspondance avec les accents.
