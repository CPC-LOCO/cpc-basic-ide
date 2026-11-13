# Architecture du projet

Statut : version 0.3, rédigée à la fin du jalon J2 (8 novembre 2026).
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
   -> Parser         instructions et expressions ; reprise après erreur ; vérifications
                     (déclarations BYTE, blocs FOR / WHILE / REPEAT / DO / IF bien refermés)
   -> CodeGenerator  octets Z80 via Z80Emitter ; modules liés par Linker
   -> DiskImage      NOM.BAS (chargeur BASIC) + NOM.BIN (programme Z80)
   -> fichier .dsk
```

### Langage reconnu aujourd'hui (fin de J2)

| Domaine | Ce qui est reconnu |
|---|---|
| Affichage | `PRINT` de textes et de nombres, plusieurs éléments séparés par `;`, `;` final pour supprimer le saut de ligne, `?` comme `PRINT` |
| Variables | entiers signés 16 bits (`A` ou `A%`, sans déclaration), octets 0 à 255 déclarés par `BYTE a, b` ; affectation avec ou sans `LET` |
| Expressions | nombres, variables, `+`, `-`, moins unaire, parenthèses ; calcul sur 16 bits, résultat qui boucle en cas de dépassement |
| Boucles | `FOR`/`NEXT` (avec `STEP`), `WHILE`/`WEND`, `REPEAT`/`UNTIL`, `DO`/`LOOP` (avec `WHILE` ou `UNTIL` en haut ou en bas), `EXIT` et `CONTINUE` (avec ou sans le type de boucle) |
| Conditions | `IF`/`THEN`/`ELSE` sur une ligne ou en bloc (`END IF`), comparaisons `=` `<>` `<` `<=` `>` `>=`, reliées par `AND` et `OR` |
| Divers | `END`, commentaires `REM` et `'`, `:` pour séparer les instructions, numéros de ligne facultatifs |

Les erreurs sont toutes signalées d'un coup, avec la ligne de l'éditeur (pas le numéro BASIC) et une explication de la cause. Une erreur dans l'en-tête d'un bloc (`FOR`, `IF`, `WHILE`...) ne provoque pas d'erreurs en cascade sur sa fermeture.

### Pas encore faits

Multiplication, division et `MOD` ; `NOT` ; `GOTO` et `GOSUB` ; tableaux ; chaînes de caractères et réels (`$` et `!` sont refusés avec un message) ; `INPUT` ; graphiques, sons et nouvelles commandes ; `DATA`/`READ` ; conditions sans comparaison (`IF a THEN`) ; profils de cible ; cassette ; cartouche GX4000.

## 4. Génération du code

- **Expressions :** le résultat est toujours dans `HL`. Un opérande de droite qui est un nombre va directement dans `DE` ; sinon, l'opérande de gauche est empilé. Un octet est étendu à 16 bits à la lecture (`LD A,(var)`, `LD L,A`, `LD H,0`) et tronqué à l'écriture.
- **Variables :** allouées statiquement à la fin du binaire, initialisées à 0, seulement si elles sont utilisées (1 octet pour `BYTE`, 2 pour un entier). Le type vient de la déclaration `BYTE`, qui doit précéder la première utilisation.
- **Comparaisons :** pour `=` et `<>`, un `SBC HL,DE` suffit. Pour les autres, le bit 15 des deux valeurs est retourné, ce qui transforme la comparaison signée en comparaison non signée par la retenue ; `>` et `<=` s'obtiennent en échangeant les deux valeurs. Les nombres écrits dans le programme sont transformés à la compilation.
- **`AND` et `OR` :** évalués de gauche à droite avec arrêt dès que le résultat est connu (sauts conditionnels, aucune valeur booléenne calculée).
- **`FOR`/`NEXT` :** le test se fait au `NEXT`, donc le corps s'exécute au moins une fois. La limite et le pas sont des nombres écrits dans le programme. Un débordement de l'entier (limite 32767) termine la boucle au lieu de la relancer.
- **Boucles et `IF` :** le générateur garde une pile des boucles ouvertes (avec les étiquettes « début », « suite » et « sortie ») et une pile des `IF` ouverts. `EXIT` et `CONTINUE` sont de simples sauts : rien à nettoyer sur la pile du Z80.
- **Vérification des blocs :** le parser vérifie, dans l'ordre du programme, que chaque bloc est refermé par son mot (`NEXT`, `WEND`, `UNTIL`, `LOOP`, `END IF`) et que les blocs s'imbriquent proprement. Le générateur peut donc supposer le programme correct.

## 5. Bibliothèque de modules Z80

Chaque commande est un module Z80 indépendant qui déclare son nom et ses dépendances. Deux modules existent :

- `PrintString` : affiche un texte terminé par 0, par la routine du firmware &BB5A ;
- `PrintInt` : affiche un entier signé de `HL`, comme le BASIC (un espace devant s'il est positif, ou `-`, puis les chiffres sans zéros inutiles, puis un espace). Les registres sont protégés autour de chaque appel du firmware.

**Choix actuel :** les modules sont écrits directement en C# avec l'assembleur interne (`Z80Emitter`), dans `RuntimeLibrary.cs`. C'est le plus simple pour démarrer et il permet de tester chaque module.

**À trancher avant le projet de commandes Z80 :** le format de déclaration de modules sous forme de fichiers (source Z80 et métadonnées), assez simple pour que le projet d'extension ajoute un module sans modifier le compilateur. Voir l'entrée D028 du journal.

Chaque module de `RuntimeLibrary.cs` porte la mention de licence « GPL v3 avec exception de liaison » (voir `RUNTIME-EXCEPTION.md`).

## 6. Profils de cible

Pas encore implémentés. Aujourd'hui, le compilateur produit toujours : adresse de chargement &4000, `MEMORY &3FFF` dans le chargeur, fichiers `.BAS` et `.BIN` sur disquette. Un profil par machine décrira plus tard : mémoire, formats de sortie permis, matériel Plus, adresses système. Le choix de la cible filtrera les commandes disponibles.

## 7. Ce que le prototype et J1 ont établi

**Format .dsk produit**

- Format standard « MV - CPCEMU Disk-File », 40 pistes, une face, 9 secteurs de 512 octets par piste, secteurs C1 à C9 dans l'ordre, sans entrelacement, remplissage &E5.
- Répertoire AMSDOS : 64 entrées de 32 octets sur les blocs 0 et 1 ; blocs de 1 Ko ; les fichiers démarrent au bloc 2 ; chaque fichier a un en-tête AMSDOS de 128 octets (type, adresse de chargement, adresse d'exécution, longueur, somme de contrôle sur les octets 0 à &42).
- Limites actuelles : un fichier ne dépasse pas 16 Ko ; pas d'entrelacement (à revoir pour un vrai lecteur). Le nom du programme compte 8 caractères au plus.

**Chargement et exécution**

- Un binaire chargé à &4000 impose de baisser HIMEM : sans `MEMORY &3FFF`, le BASIC répond « Memory full ». Le compilateur ajoute donc un chargeur BASIC tokenisé : `MEMORY &3FFF`, `LOAD"NOM.BIN"`, `CALL &4000`. L'utilisateur lance le tout par `RUN"NOM"`.
- Codes du format tokenisé vérifiés : &AA = MEMORY, &A8 = LOAD, &83 = CALL, &1C = constante hexadécimale sur 2 octets, &22 = guillemet. Chaque ligne : longueur (2 octets), numéro (2 octets), codes, 0 ; le programme se termine par deux octets à 0.
- Un `RUN` direct sur un binaire ne mémorise pas d'adresse de retour : un `RET` final plante la machine. Un programme compilé ne revient pas au BASIC : il attend une touche (KM WAIT KEY, &BB18) puis redémarre (`JP 0`).
- Routines du firmware utilisées : &BB5A (TXT OUTPUT) et &BB18 (KM WAIT KEY).

**Organisation du binaire produit :** instructions du programme, fin de programme, modules utilisés, textes terminés par 0, puis variables (à 0). Un programme sans `PRINT` ne contient aucun module (liaison sélective vérifiée par les tests).

**Limite connue :** les caractères hors ASCII (accents) sont remplacés par « ? » à l'affichage. La table des caractères du CPC reste à gérer.

## 8. Écarts avec le Locomotive BASIC

| Sujet | Locomotive BASIC | Ce projet |
|---|---|---|
| Type de `A` | réel | entier 16 bits (`A` et `A%` sont deux variables distinctes) |
| Réels, chaînes | oui | pas encore (`$` et `!` refusés) |
| Variable octet | non | `BYTE a`, déclaration obligatoire avant usage |
| Valeur de la variable après `FOR` | valeur suivante (4 après `FOR I=1 TO 3`) | dernière valeur dans la boucle (3) |
| Limite et pas de `FOR` | expressions | nombres écrits dans le programme |
| `IF` | une ligne | une ligne, ou en bloc avec `END IF` |
| Condition d'un `IF` | n'importe quelle valeur | une comparaison, éventuellement avec `AND` et `OR` |
| `DO`, `LOOP`, `REPEAT`, `UNTIL`, `EXIT`, `CONTINUE`, `BYTE`, `ENDIF` | n'existent pas | nouveaux mots réservés (une variable portant ce nom ne compile plus) |
| Mode direct | oui | n'existe pas (c'est un compilateur) |
| Abréviations de commandes | à vérifier | seul `?` pour `PRINT` est accepté |

## 9. Textes et langues

Prévu : tous les textes visibles (menus, messages d'erreur, aide) dans des fichiers de langue, un par langue (français, anglais, espagnol). **État actuel :** les messages d'erreur sont écrits en français dans `Diagnostic.cs`, ainsi que les textes de l'éditeur dans `MainWindow`. Ils rejoindront les fichiers de langue avant l'ajout de l'anglais et de l'espagnol.

## 10. Tests et bible

**Tests :** 175 tests automatiques (`dotnet test`) couvrent la fabrication de la disquette, le chargeur BASIC, l'analyse du langage, l'assembleur Z80, le générateur de code (avec les octets Z80 exacts des constructions principales) et le compilateur. Ils protègent le projet à chaque modification. Les programmes d'`examples/` sont aussi vérifiés à la main dans WinAPE à la fin de chaque étape.

**Bible :** pas encore commencée. Prévue en Markdown, une page par instruction, avec des exemples compilés automatiquement.

## 11. Questions ouvertes

1. Format de déclaration des modules de la bibliothèque sous forme de fichiers (D028).
2. Assembleur Z80 : celui du projet suffit-il pour les modules complexes, ou faut-il un outil externe (D015) ?
3. Formats cassette et cartouche GX4000 : à étudier dans la documentation existante.
4. Compatibilité avec le Locomotive BASIC : compléter le tableau de la section 8 (flottants, chaînes, mémoire, abréviations de commandes). L'inventaire complet des instructions est dans `docs\COMPATIBILITE-LOCOMOTIVE.md` (D053).
5. ROM et dispositions de clavier : comment les fournir sans les redistribuer (D016).
6. Jeu de caractères du CPC : correspondance avec les accents.
7. Relecture juridique de l'exception de liaison de la bibliothèque (D017), à faire avant la prochaine publication.
