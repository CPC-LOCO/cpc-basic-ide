# CPC BASIC IDE (nom provisoire)

Un éditeur coloré et un compilateur pour un **BASIC évolué**, dérivé du Locomotive BASIC, qui crée des jeux pour **Amstrad CPC** : disquettes `.dsk` aujourd'hui, cassettes et cartouches GX4000 à venir.

Pensé pour celles et ceux qui veulent créer des jeux sans bien connaître la programmation, avec une bible d'instructions illustrée d'exemples réels.

> **État du projet : version de test 0.5.0.** Presque tout le Locomotive BASIC est compilé en langage machine (l'inventaire est terminé : voir [`docs/COMPATIBILITE-LOCOMOTIVE.md`](docs/COMPATIBILITE-LOCOMOTIVE.md)), avec des commandes en plus (CIRCLE, ELLIPSE, ISIN, ICOS, BOX, PAINT, DEBUG, STAR...) et des **bibliothèques de commandes** que chacun peut écrire en assembleur Z80 (Maj+F4). Sorties cassette et cartouche GX4000, sprites : pas encore. Téléchargement : onglet **Releases** ; aide pour les testeurs : [`.github/TESTEURS.md`](.github/TESTEURS.md).

## Ce qui fonctionne aujourd'hui

- **Éditeur** : coloration du BASIC, barre de menus, commandes du CPC tapées sans numéro (NEW, AUTO, RENUM, LIST...), compilation (F5) ou simple vérification (Maj+F5), panneau des messages où Entrée mène à la ligne fautive, liste des variables (F4), lettres accentuées telles que le CPC les affichera (F7).
- **Compilateur** : textes, nombres à virgule (mêmes résultats que le vrai BASIC), tableaux, graphismes, sons, fichiers, interruptions, erreurs ; ne lie que les routines utilisées ; crée un `.dsk` avec un chargeur BASIC. Un programme compilé marche sur le 464, le 664 et le 6128. Lancement dans l'émulateur par `RUN"NOM"`.
- **Messages d'erreur explicites** : la ligne, la cause, et une piste de correction.
- **Outil en ligne de commande** pour compiler sans l'éditeur.
- **Plus de 1600 tests automatiques** qui protègent le projet à chaque modification.

Exemple de programme (`examples/hello.bas`) :

```
10 REM Mon premier programme compile
20 PRINT "HELLO CPC !"
30 PRINT "CE PROGRAMME A ETE COMPILE PAR MON OUTIL"
40 PRINT
50 PRINT "APPUYEZ SUR UNE TOUCHE...";
60 END
```

Les lettres accentuées s'écrivent normalement : l'éditeur montre ce que le CPC choisi (français ou anglais, F7) affichera.

## L'essayer depuis le code source

Prérequis : le SDK .NET 10 et Git. Testé avec l'émulateur WinAPE sous Windows.

```
git clone https://github.com/CPC-LOCO/cpc-basic-ide.git
cd cpc-basic-ide
dotnet test
dotnet run --project src/CpcIde
```

Pour compiler sans l'éditeur :

```
dotnet run --project src/CpcBasic.Cli -- examples/hello.bas
```

Insérez ensuite le `.dsk` créé dans l'émulateur, puis tapez `RUN"HELLO"`.

## Objectifs

- Éditeur coloré, avec copier/coller, pour Windows (obligatoire), Linux et macOS (souhaitables)
- BASIC évolué avec de nombreuses commandes ajoutées : CIRCLE, STAR, PAINT, sprites, collisions, défilement, effets sonores, SELECT CASE...
- Compilateur binaire : `.dsk`, cassette, cartouche GX4000
- Choix de la machine cible : CPC 464, 664, 6128, 464+, 6128+, GX4000, et de la disposition du clavier
- Le binaire ne contient que les commandes réellement utilisées
- Bible explicative en français, avec des exemples qui sont compilés automatiquement
- Interface en français, anglais et espagnol
- Architecture ouverte, pour s'unir plus tard à un projet de création de nouvelles commandes en Z80

## Organisation du dépôt

| Dossier | Contenu |
|---|---|
| `src` | Code : cœur du compilateur, outil en ligne de commande, éditeur |
| `tests` | Tests automatiques |
| `examples` | Programmes BASIC d'exemple |
| `docs` | Cahier des charges, architecture, journal des décisions |
| `prototypes` | Prototype de la phase J0 |

Pour comprendre le projet :

- [`docs/CAHIER-DES-CHARGES.md`](docs/CAHIER-DES-CHARGES.md) : ce que le projet doit faire
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) : comment il est construit
- [`docs/JOURNAL-DECISIONS.md`](docs/JOURNAL-DECISIONS.md) : les décisions prises et pourquoi

## Participer

Le projet est porté par une seule personne, avec l'aide d'une IA. Les retours sont les bienvenus : tests sur émulateurs et sur machines réelles, conseils sur le Z80 et le firmware CPC, relectures. Ouvrez une discussion ou un ticket dans l'onglet **Issues** du dépôt.

Pour l'instant, le code n'accepte pas de contributions directes (voir le journal des décisions, entrée D007).

## Licence

Le code de l'éditeur et du compilateur est publié sous licence **GNU GPL v3**. Voir le fichier [`LICENSE`](LICENSE). Les routines Z80 que le compilateur copie dans vos programmes sont sous GPL v3 avec une **exception de liaison**, en vigueur depuis la version 0.4.0 : **les jeux créés avec l'outil appartiennent à leur auteur**, qui peut les diffuser ou les vendre sous la licence de son choix (voir [`docs/RUNTIME-EXCEPTION.md`](docs/RUNTIME-EXCEPTION.md) ; texte en attente de relecture).

---

## In English

An editor and compiler for an enhanced BASIC dialect derived from Locomotive BASIC, aimed at making Amstrad CPC games: `.dsk` disk images today, tape files and GX4000 cartridges planned. Designed for beginners, with a documented command reference.

**Status:** test version 0.5.0. Almost all of Locomotive BASIC is compiled to machine code (the inventory is complete), plus extra commands (CIRCLE, ELLIPSE, ISIN, ICOS, BOX, PAINT, DEBUG, STAR...) and user command libraries written in Z80 assembly (Shift+F4). Compiled programs run on the 464, 664 and 6128. Tape and GX4000 cartridge output, sprites: not yet. The documentation is currently in French. Feedback from the CPC community is very welcome through the repository's Issues tab. Licensed under GPL v3; the games you create belong to you (runtime library exception, see `docs/RUNTIME-EXCEPTION.md`).
