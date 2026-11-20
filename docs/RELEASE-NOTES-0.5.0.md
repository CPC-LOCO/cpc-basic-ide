# CPC BASIC IDE 0.5.0 (version de test)

Éditeur coloré et compilateur pour un BASIC évolué, dérivé du Locomotive BASIC, qui crée des programmes en langage machine pour Amstrad CPC. Windows 64 bits, rien à installer.

## Nouveautés depuis 0.4.0

**Bibliothèques de commandes**
- Ajoutez vos propres commandes et fonctions au BASIC, écrites en assembleur Z80 (à la façon de l'assembleur de WinAPE), rangées par bibliothèque. Le compilateur ne met dans un programme que celles qu'il utilise, avec les routines qu'elles appellent.
- **Maj+F4** (menu Bibliothèques) : une fenêtre à onglets, un par bibliothèque ; la liste des commandes, leur écriture, leur aide, leurs valeurs.
- **Nouvelle commande** : un questionnaire (nom, commande, fonction ou routine, bibliothèque, aide, valeurs avec leurs limites, valeurs facultatives) ; un cadre montre comment la commande s'écrira en BASIC.
- **Éditeur d'assembleur** : couleurs, Vérifier (F5) avec la liste des fautes, Enregistrer (Ctrl+S) ; « Voir le code » des commandes livrées avec l'éditeur. Modifier la fiche, effacer une commande.
- Vos commandes sont rangées dans `Documents\CPC BASIC IDE\bibliotheques` : une mise à jour n'y touche jamais.
- **Fichier, Préparer un dossier à distribuer** : le programme et vos commandes qu'il utilise, avec un LISEZMOI, pour le donner à quelqu'un qui pourra le compiler.

**Nouvelle commande : STAR** (bibliothèque GRAPHIQUE, livrée avec l'éditeur)
- `STAR x, y, rayon interne, rayon externe, pointes [, stylo] [, remplissage] [, angle]` : étoiles de 2 à 16 pointes, pleines ou vides, tournées de l'angle voulu ; avec un rayon interne assez grand, un polygone (hexagone...). Exemple : `etoiles.bas`.

**L'éditeur**
- Les commandes des bibliothèques sont colorées comme les commandes du projet.

## Pas encore là
Sortie cassette, cartouche GX4000, sprites ; un « assembleur seul » (idée pour la suite).

## Somme de contrôle
SHA256 de `CpcBasic-0.5.0-win-x64.zip` : `7560edacc72a78fd07117dea2b0b48e632d8e20102fc961b0b34c2084a0cd7b6`
