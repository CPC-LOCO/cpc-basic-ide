# Participer aux essais

Merci de votre aide ! Ce projet est un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. Il est en cours de création : vos essais permettent de trouver ce qui ne va pas, et de choisir ce qui compte le plus.

## 1. Télécharger la version de test

1. Ouvrez la page des versions : https://github.com/CPC-LOCO/cpc-basic-ide/releases
2. Cherchez la version la plus récente, marquée « Pre-release ».
3. En dessous, dans « Assets », cliquez sur le fichier qui se termine par `.zip` pour le télécharger. Un compte GitHub n'est pas nécessaire pour télécharger.
4. Décompressez tout le dossier, puis double-cliquez sur `CpcIde.exe`. Rien ne s'installe.

Le fichier n'est pas signé numériquement : Windows ou votre antivirus peut le signaler à tort. Le fichier `LISEZMOI.txt` du dossier explique quoi faire.

## 2. Que faut-il essayer ?

Presque tout le Locomotive BASIC est maintenant compilé : textes, nombres à virgule, tableaux, graphismes, sons, clavier, fichiers sur disquette, interruptions (EVERY, AFTER), erreurs (ON ERROR GOTO). Le fichier `COMPATIBILITE.md` du dossier donne la liste, commande par commande.

- **Vos anciens programmes BASIC :** c'est l'essai le plus utile. Collez-les dans l'éditeur, compilez avec F5 et comparez avec le vrai BASIC. Le réglage F11 met aussi le texte sur la disquette (`LOAD"NOM.TXT"` puis `RUN`) pour comparer dans le même émulateur.
- **Les exemples** du dossier `examples` : ouvrez-les, compilez-les avec F5, lancez le `.dsk` dans un émulateur (WinAPE, Caprice32, SugarBox...) avec `RUN"NOM"`.
- **Le nouvel éditeur :** barre de menus (touche Alt), panneau des messages, et les commandes du CPC tapées sans numéro puis Entrée : `NEW`, `AUTO`, `EDIT 20`, `RENUM`, `DELETE 10-50`, `LIST`, et un numéro seul pour effacer une ligne. F1 affiche les raccourcis.
- **Les nouveautés de la 0.5.0 :** les bibliothèques de commandes (Maj+F4 : liste, nouvelle commande, éditeur d'assembleur, modifier, effacer ; Fichier, Préparer un dossier à distribuer) et la commande `STAR` (exemple `etoiles.bas`).
- **Les nouveautés de la 0.4.0 :** `CIRCLE` et `ELLIPSE` (exemples `cercles.bas`, `ovales.bas`, `moniteur.bas`), `ISIN` et `ICOS` (exemple `angles.bas`), `DEBUG` avec le réglage F2 (exemples `debug.bas`, `debugvar.bas`, `debugtab.bas`), la liste des variables (F4), les lettres accentuées (F7).
- **Les ronds sur un vrai CPC :** CIRCLE et ELLIPSE sont faits pour un vrai moniteur Amstrad ; dans un émulateur à points carrés, ils paraissent environ 8 % plus larges que hauts. Si vous avez un vrai CPC, une photo de `moniteur.bas` (le carré doit paraître carré, les ronds ronds) nous aiderait beaucoup.
- **Sur plusieurs machines :** un programme compilé doit marcher sur le 464, le 664 et le 6128. Dites-nous si ce n'est pas le cas.
- **Les erreurs exprès :** les messages sont-ils clairs ? Disent-ils où est le problème et comment le corriger ?

**Astuce : le réglage F8 et les boucles rapides.** Avec le réglage F8 (menu Options, « Variables sans signe : à virgule »), toutes les variables sans signe deviennent des nombres à virgule, comme sur le vrai CPC. Une boucle d'attente comme `FOR K = 1 TO 30000` devient alors beaucoup plus lente, car les calculs à virgule passent par la ROM. Pour la garder rapide, écrivez le compteur avec le signe des entiers : `FOR K% = 1 TO 30000`.

Pas encore là, inutile de les tester : sortie cassette, cartouche GX4000, sprites.

Si l'éditeur affiche « Problème interne », joignez à votre message le fichier `%APPDATA%\CpcBasicIde\erreurs.txt`.

## 3. Signaler un problème ou donner une idée

Il faut un compte GitHub (gratuit).

1. Allez sur https://github.com/CPC-LOCO/cpc-basic-ide/issues
2. Cliquez sur le bouton vert « New issue ».
3. Choisissez « Signaler un problème » ou « Proposer une idée », puis remplissez les cases. Quelques mots suffisent.

Ce qui nous aide le plus :

- la version essayée (elle est écrite au début du `LISEZMOI.txt`) ;
- ce que vous avez fait, et ce qui s'est passé ;
- le texte du programme BASIC en cause (copiez-collez-le) ;
- votre version de Windows, votre antivirus, et l'émulateur utilisé.

Tout ce que vous écrivez est public : n'y mettez pas d'informations personnelles.

## 4. Ce que le projet accepte pour l'instant

Les retours, les idées, les conseils sur le Z80 et le firmware du CPC sont très bienvenus. Le code est écrit par une seule personne, avec une aide logicielle : pour l'instant, les propositions de code (« pull requests ») ne sont pas acceptées. Cela pourra changer plus tard, avec des règles écrites.
