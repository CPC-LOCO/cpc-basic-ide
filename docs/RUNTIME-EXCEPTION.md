# Exception de liaison pour la bibliothèque d'exécution

> **Statut : en vigueur depuis la version 0.4.0 (décision D143, qui applique D017). Texte en attente de relecture par une source compétente** (par exemple le service des licences de la Free Software Foundation, licensing@fsf.org). Une relecture pourra améliorer la formulation ; elle ne retirera pas la permission donnée aux auteurs de programmes. Modèle d'inspiration : l'exception de la bibliothèque d'exécution de GCC (GCC Runtime Library Exception), publiée par la Free Software Foundation.

## En bref

Les jeux et programmes que vous créez avec CPC BASIC IDE **vous appartiennent**. Vous pouvez les diffuser, les donner ou les vendre, sous la licence de votre choix, sans fournir leur code source, même s'ils contiennent des routines Z80 du projet copiées par le compilateur.

## Intention

Le compilateur copie des routines Z80 du projet (la bibliothèque d'exécution) à l'intérieur des programmes créés par les utilisateurs. Cette exception garantit que **les jeux créés avec l'outil restent la propriété de leur auteur**, qui choisit librement leur licence, y compris une licence commerciale et fermée.

## Texte

La bibliothèque d'exécution est publiée sous la licence publique générale GNU, version 3, avec l'exception supplémentaire suivante, accordée au titre de la section 7 de cette licence.

**Définitions.**

- La **bibliothèque d'exécution** est le code Z80 que le compilateur du projet copie dans les programmes qu'il produit. Aujourd'hui, ce code est écrit dans les fichiers `src/CpcBasic.Core/Codegen/RuntimeLibrary.cs` et `src/CpcBasic.Core/Codegen/RealRuntime.cs`, ainsi que dans les petites suites d'instructions que le compilateur (`src/CpcBasic.Core/Codegen/CodeGenerator.cs`) écrit directement dans les programmes. Les bibliothèques de commandes publiées plus tard par le projet en feront partie.
- Un **programme généré** est un fichier produit par le compilateur du projet (par exemple une disquette `.dsk`, un fichier cassette, une cartouche ou un fichier binaire) à partir d'un code source écrit par un utilisateur, et qui contient des copies de la bibliothèque d'exécution.

**Exception.** Vous pouvez copier, distribuer et modifier un programme généré, et le publier sous les conditions de votre choix, sans obligation de fournir son code source ni de le placer sous la licence GPL, pourvu que votre programme ne soit pas lui-même une bibliothèque ou un outil destiné à remplacer ou à copier le compilateur, l'éditeur ou la bibliothèque d'exécution du projet.

**Ce que l'exception ne couvre pas.**

- La bibliothèque d'exécution elle-même, hors d'un programme généré : si vous la modifiez ou la distribuez séparément, elle reste sous GPL v3 (avec la présente exception pour les programmes générés qui la contiennent).
- L'éditeur et le compilateur, qui restent sous GPL v3 sans exception.
- Le code source de l'utilisateur : il lui appartient, quelle que soit la licence choisie.
- Les bibliothèques de commandes écrites par d'autres personnes que le projet : leur auteur choisit leur licence.

## Mention placée en tête des fichiers de la bibliothèque d'exécution

```
// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.
```

## Points à faire vérifier lors de la relecture

1. La formulation de la condition « ne pas être une bibliothèque ou un outil de remplacement » : est-elle assez claire et assez limitée ?
2. La compatibilité avec la section 7 de la GPL v3 (termes supplémentaires permis).
3. Les bibliothèques de commandes ajoutées plus tard : celles du projet suivent cette exception ; celles d'autres auteurs gardent la licence de leur auteur, qui doit permettre de les copier dans les programmes générés.
4. La définition de la bibliothèque d'exécution par des noms de fichiers du code source (le code Z80 est écrit en C# depuis D024, et non dans un dossier `runtime` comme prévu au début).
