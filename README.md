# CPC BASIC IDE (nom provisoire)

Un éditeur coloré et un compilateur pour un **BASIC évolué**, dérivé du Locomotive BASIC, qui crée des jeux pour **Amstrad CPC** : disquettes `.dsk` aujourd'hui, cassettes et cartouches GX4000 à venir.

Pensé pour celles et ceux qui veulent créer des jeux sans bien connaître la programmation, avec une bible d'instructions illustrée d'exemples réels.

> **État du projet : jalon J1 terminé, développement en cours.** On peut écrire un tout petit programme, le compiler en `.dsk` et le lancer dans un émulateur. Le langage ne reconnaît pour l'instant que `PRINT` (ou `?`) et `END`. Ce n'est pas encore utilisable pour créer un jeu.

## Ce qui fonctionne aujourd'hui

- **Éditeur** : fenêtre avec coloration du BASIC, numéros de ligne, copier/coller, ouvrir, enregistrer, compiler (F5), et panneau d'erreurs où un clic mène à la ligne fautive.
- **Compilateur** : analyse le programme, génère du code Z80, ne lie que les routines utilisées, et crée un `.dsk` avec un chargeur BASIC. Lancement dans l'émulateur par `RUN"NOM"`.
- **Messages d'erreur explicites** : la ligne, la cause, et une piste de correction.
- **Outil en ligne de commande** pour compiler sans l'éditeur.
- **46 tests automatiques** qui protègent le projet à chaque modification.

Exemple de programme (`examples/hello.bas`) :

```
10 REM Mon premier programme compile
20 PRINT "HELLO CPC !"
30 PRINT "CE PROGRAMME A ETE COMPILE PAR MON OUTIL"
40 PRINT
50 PRINT "APPUYEZ SUR UNE TOUCHE...";
60 END
```

Les lettres accentuées ne sont pas encore gérées : elles s'affichent sous forme de « ? ».

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

Le code de l'éditeur et du compilateur est publié sous licence **GNU GPL v3**. Voir le fichier [`LICENSE`](LICENSE). Les futurs modules Z80 de la bibliothèque seront sous GPL v3 avec une exception qui laisse les jeux créés avec l'outil à leurs auteurs (voir [`docs/RUNTIME-EXCEPTION.md`](docs/RUNTIME-EXCEPTION.md), texte à faire relire).

---

## In English

An editor and compiler for an enhanced BASIC dialect derived from Locomotive BASIC, aimed at making Amstrad CPC games: `.dsk` disk images today, tape files and GX4000 cartridges planned. Designed for beginners, with a documented command reference.

**Status:** milestone J1 completed. You can write a tiny program (`PRINT` or `?`, and `END` only for now), compile it to a `.dsk` and run it in an emulator such as WinAPE. Not yet usable to create games. Accented letters are not supported yet. The documentation is currently in French. Feedback from the CPC community is very welcome through the repository's Issues tab. Licensed under GPL v3.
