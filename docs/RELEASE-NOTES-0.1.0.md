## Version 0.1.0 (test)

Première version publique de **CPC BASIC IDE** (nom provisoire), un éditeur coloré et un compilateur pour un BASIC évolué dérivé du Locomotive BASIC, qui crée des programmes pour Amstrad CPC.

**C'est une version de test, publiée pour recueillir des retours. Elle n'est pas encore utilisable pour créer un jeu.**

### Ce qui fonctionne

- Éditeur avec coloration du BASIC, numéros de ligne, copier/coller, ouvrir, enregistrer
- Compilation en `.dsk` (touche F5) avec un chargeur BASIC : lancement par `RUN"NOM"` dans l'émulateur
- Instructions reconnues : `PRINT` (ou `?`), `PRINT` seul, `PRINT "texte";` sans saut de ligne, `END`, commentaires `REM` et `'`
- Messages d'erreur explicites, avec la ligne ; un clic mène à la ligne fautive
- Liaison sélective : le binaire ne contient que les routines utilisées

### Ce qui ne fonctionne pas encore

Variables, boucles, conditions, graphismes, sons, sprites, sortie cassette, cartouche GX4000, choix de la machine cible. Les lettres accentuées sont remplacées par « ? » à l'affichage.

### Comment l'essayer

1. Téléchargez `CpcBasic-0.1.0-win-x64.zip` ci-dessous et décompressez tout le dossier.
2. Double-cliquez sur `CpcIde.exe` (Windows 64 bits, sans installation, .NET n'est pas à installer).
3. Ouvrez `examples\hello.bas`, appuyez sur F5, puis dans WinAPE insérez le `.dsk` créé et tapez `RUN"HELLO"`.

Testé avec l'émulateur WinAPE sous Windows.

### Antivirus et Windows

Le programme **n'est pas signé numériquement** : Windows (SmartScreen) ou votre antivirus peut le signaler ou bloquer son démarrage. C'est une fausse alerte fréquente pour les programmes libres non signés. Vous pouvez vérifier l'intégrité de l'archive avec sa somme de contrôle SHA256 :

```
SHA256 : (à compléter avec la valeur affichée par le script de préparation)
```

Pour la calculer vous-même sous Windows : `certutil -hashfile CpcBasic-0.1.0-win-x64.zip SHA256`. Le code source complet est public dans ce dépôt.

### Vos retours

Tests, problèmes, idées, conseils sur le Z80 et le firmware CPC : ouvrez un ticket dans l'onglet **Issues**.

### Licence

GNU GPL v3.
