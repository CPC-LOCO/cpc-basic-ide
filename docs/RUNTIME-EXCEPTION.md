# Exception de liaison pour la bibliothèque d'exécution (PROJET)

> **Statut : projet de texte, non relu par un juriste.** Il est écrit pour exprimer l'intention du projet (décision D017). Avant la première publication d'un module, il doit être relu par une source compétente. Modèle d'inspiration : l'exception de la bibliothèque d'exécution de GCC (GCC Runtime Library Exception), publiée par la Free Software Foundation.

## Intention

Le compilateur copie des modules Z80 de la bibliothèque (dossier `runtime`) à l'intérieur des programmes créés par les utilisateurs. Cette exception garantit que **les jeux créés avec l'outil restent la propriété de leur auteur**, qui choisit librement leur licence, y compris une licence commerciale et fermée.

## Texte proposé

Les fichiers du dossier `runtime` sont publiés sous la licence publique générale GNU, version 3, avec l'exception supplémentaire suivante, accordée au titre de la section 7 de cette licence.

**Définitions.**

- Un **module de la bibliothèque** est un fichier du dossier `runtime`.
- Un **programme généré** est un fichier produit par le compilateur du projet (par exemple une disquette `.dsk`, un fichier cassette, une cartouche ou un fichier binaire) à partir d'un code source écrit par un utilisateur, et qui contient des copies de modules de la bibliothèque.

**Exception.** Vous pouvez copier, distribuer et modifier un programme généré, et le publier sous les conditions de votre choix, sans obligation de fournir son code source ni de le placer sous la licence GPL, pourvu que votre programme ne soit pas lui-même une bibliothèque ou un outil destiné à remplacer ou à copier le compilateur, l'éditeur ou la bibliothèque d'exécution du projet.

**Ce que l'exception ne couvre pas.**

- Les modules de la bibliothèque eux-mêmes : si vous les modifiez ou les distribuez séparément, hors d'un programme généré, ils restent sous GPL v3 (avec la présente exception pour les programmes générés qui les contiennent).
- L'éditeur et le compilateur, qui restent sous GPL v3 sans exception.
- Le code source de l'utilisateur : il lui appartient, quelle que soit la licence choisie.

## Mention à placer en tête de chaque module Z80

```
; Copyright (C) 2026 RetroPoke
; Ce module fait partie de CPC BASIC IDE.
; Il est publié sous la licence publique générale GNU, version 3, avec l'exception
; de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
; Les programmes générés par le compilateur peuvent être publiés sous la licence
; de votre choix. Voir aussi le fichier LICENSE.
```

## Points à faire vérifier

1. La formulation de la condition « ne pas être une bibliothèque ou un outil de remplacement » : est-elle assez claire et assez limitée ?
2. La compatibilité avec la section 7 de la GPL v3 (termes supplémentaires permis).
3. Le sort des modules ajoutés plus tard par le projet de commandes Z80 : leur auteur devra accepter la même licence et la même exception.
