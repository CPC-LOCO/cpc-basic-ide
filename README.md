# CPC BASIC IDE (nom provisoire)

Un éditeur coloré et un compilateur pour un **BASIC évolué**, dérivé du Locomotive BASIC, qui crée des jeux pour **Amstrad CPC** : disquettes `.dsk`, cassettes et cartouches GX4000.

Pensé pour celles et ceux qui veulent créer des jeux sans bien connaître la programmation, avec une bible d'instructions illustrée d'exemples réels.

> **État du projet : jalon J0 terminé, développement en cours.** Rien n'est encore utilisable pour créer un jeu. Le dépôt contient les premiers prototypes et les documents de conception.

## Ce qui existe aujourd'hui

- **Prototype `.dsk`** (`prototypes/CpcProto`) : un petit programme Z80 et son chargeur BASIC sont écrits dans une disquette `.dsk`, qui se lance dans WinAPE avec `RUN"HELLO"`.
- **Essai d'éditeur** (`src/CpcIde`) : une fenêtre Avalonia avec coloration syntaxique du BASIC, numéros de ligne et copier/coller.

## Objectifs

- Éditeur coloré, avec copier/coller, pour Windows (obligatoire), Linux et macOS (souhaitables)
- BASIC évolué avec de nombreuses commandes ajoutées : CIRCLE, STAR, PAINT, sprites, collisions, défilement, effets sonores, SELECT CASE, messages d'erreur explicites...
- Compilateur binaire : `.dsk`, cassette, cartouche GX4000
- Choix de la machine cible : CPC 464, 664, 6128, 464+, 6128+, GX4000, et de la disposition du clavier
- Le binaire ne contient que les commandes réellement utilisées
- Bible explicative en français, avec des exemples qui sont compilés automatiquement
- Interface en français, anglais et espagnol
- Architecture ouverte, pour s'unir plus tard à un projet de création de nouvelles commandes en Z80

## Organisation du dépôt

| Dossier | Contenu |
|---|---|
| `docs` | Cahier des charges, architecture, journal des décisions |
| `prototypes` | Prototypes de la phase J0 |
| `src` | Code de l'outil |

Pour comprendre le projet, commencez par :

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) : comment le projet est construit
- [`docs/JOURNAL-DECISIONS.md`](docs/JOURNAL-DECISIONS.md) : les décisions prises et pourquoi

## Participer

Le projet est porté par une seule personne, avec l'aide d'une IA. Les retours sont les bienvenus : tests sur émulateurs et sur machines réelles, conseils sur le Z80 et le firmware CPC, relectures. Ouvrez une discussion ou un ticket dans l'onglet **Issues** du dépôt.

Pour l'instant, le code n'accepte pas de contributions directes (voir le journal des décisions, entrée D007).

## Licence

Le code est publié sous licence **GNU GPL v3**. Voir le fichier [`LICENSE`](LICENSE).

---

## In English

An editor and compiler for an enhanced BASIC dialect derived from Locomotive BASIC, aimed at making Amstrad CPC games: `.dsk` disk images, tape files and GX4000 cartridges. Designed for beginners, with a documented command reference.

**Status:** milestone J0 (prototypes and design documents) completed; development in progress. Nothing is ready for game creation yet. The documentation is currently in French. Feedback from the CPC community is very welcome through the repository's Issues tab. Licensed under GPL v3.
