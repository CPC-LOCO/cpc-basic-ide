# Participer aux essais

Merci de votre aide ! Ce projet est un éditeur et un compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC. Il est en cours de création : vos essais permettent de trouver ce qui ne va pas, et de choisir ce qui compte le plus.

## 1. Télécharger la version de test

1. Ouvrez la page des versions : https://github.com/CPC-LOCO/cpc-basic-ide/releases
2. Cherchez la version la plus récente, marquée « Pre-release ».
3. En dessous, dans « Assets », cliquez sur le fichier qui se termine par `.zip` pour le télécharger. Un compte GitHub n'est pas nécessaire pour télécharger.
4. Décompressez tout le dossier, puis double-cliquez sur `CpcIde.exe`. Rien ne s'installe.

Le fichier n'est pas signé numériquement : Windows ou votre antivirus peut le signaler à tort. Le fichier `LISEZMOI.txt` du dossier explique quoi faire.

## 2. Que faut-il essayer ?

- Ouvrez les programmes du dossier `examples` et compilez-les avec F5. Un fichier `.dsk` apparaît à côté.
- Lancez le `.dsk` dans un émulateur (par exemple WinAPE) avec `RUN"NOM"`.
- Écrivez vos propres petits programmes avec ce que décrit le `LISEZMOI.txt` (variables, calculs, boucles, conditions, fonctions, hasard).
- Essayez de faire des erreurs exprès : les messages sont-ils clairs ? Disent-ils où est le problème et comment le corriger ?

Les graphismes, les sons et le clavier ne sont pas encore là : il est inutile de les tester.

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
