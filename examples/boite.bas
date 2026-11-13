10 REM Rectangles BOX : origine en haut a gauche, 200 lignes
20 REM Chaque cadre doit toucher les quatre bords de l ecran
30 MODE 0
40 BOX 0, 0, 159, 199, 1
50 BOX 20, 20, 139, 179, 2
60 BOX 40, 40, 119, 159, 3
70 FOR J = 1 TO 6 : FOR I = 1 TO 30000 : NEXT : NEXT
80 MODE 1
90 BOX 0, 0, 319, 199, 1
100 BOX 40, 20, 279, 179, 2
110 BOX 80, 40, 239, 159, 3
120 REM Coins donnes a l envers et en partie hors ecran : deux lignes pleine largeur
130 BOX 340, 95, -20, 105, 3
140 FOR J = 1 TO 6 : FOR I = 1 TO 30000 : NEXT : NEXT
150 MODE 2
160 BOX 0, 0, 639, 199, 1
170 BOX 80, 20, 559, 179, 1
180 BOX 160, 40, 479, 159, 1
190 LOCATE 30, 12
200 PRINT "APPUYEZ SUR UNE TOUCHE"
210 END
