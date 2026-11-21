10 REM Essai de la saisie a la manette sur la cartouche GX4000 (D184)
20 MODE 1
30 LOCATE 1,2:PRINT "SAISIE A LA MANETTE"
40 LOCATE 1,4:PRINT "Haut/bas : lettre   Tir 2 : groupe"
50 LOCATE 1,5:PRINT "Droite : ecrire     Gauche : effacer"
60 LOCATE 1,6:PRINT "Tir 1 : valider"
70 LOCATE 1,9:INPUT "Votre nom";N$
80 LOCATE 1,11:INPUT "Votre age";A
90 LOCATE 1,14:PRINT "Bonjour ";N$;" !"
100 PRINT "Dans 10 ans, vous aurez";A+10;"ans."
110 LOCATE 1,25:PRINT "FIN DE L ESSAI";
