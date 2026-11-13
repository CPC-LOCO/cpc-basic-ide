10 REM BOX x1, y1, x2, y2 [, stylo [, remplissage]]
20 REM 4 valeurs : contour au stylo courant ; 5 : contour ; 6 : contour et interieur
30 MODE 1
40 BOX 0, 0, 319, 199, 1, 2
50 BOX 40, 20, 279, 179, 3, 1
60 BOX 80, 40, 239, 159, 2, 3
70 REM Un plein qui deborde de l ecran, puis un contour seul par dessus
80 BOX -30, 90, 350, 110, 0, 1
90 BOX 100, 60, 219, 139, 1
100 REM Stylo du trace = 2 (MOVE ne dessine rien), puis BOX a 4 valeurs : contour cyan
110 MOVE 0, 0, 2
120 BOX 130, 75, 189, 124
130 FOR J = 1 TO 6 : FOR I = 1 TO 30000 : NEXT : NEXT
140 MODE 0
150 BOX 0, 0, 159, 199, 5, 9
160 BOX 20, 20, 139, 179, 14, 6
170 BOX 40, 40, 119, 159, 1, 3
180 FOR J = 1 TO 10 : FOR I = 1 TO 30000 : NEXT : NEXT
190 MODE 2
200 BOX 0, 0, 639, 199, 1, 0
210 BOX 160, 40, 479, 159, 0, 1
220 FOR J = 1 TO 14 : FOR I = 1 TO 30000 : NEXT : NEXT
230 LOCATE 30, 12
240 PRINT "APPUYEZ SUR UNE TOUCHE"
250 END
