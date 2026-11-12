10 REM Dessin de base
20 MODE 1
30 BORDER 0
40 INK 0, 0
50 CLG
60 REM Cadre jaune, tout autour de l ecran
70 MOVE 0, 0
80 DRAW 639, 0, 1
90 DRAW 639, 399
100 DRAW 0, 399
110 DRAW 0, 0
120 REM Les deux diagonales : cyan puis rouge
130 MOVE 0, 0
140 DRAW 639, 399, 2
150 MOVE 0, 399
160 DRAW 639, 0, 3
170 REM Origine au centre, puis un carre jaune de 200 de cote
180 ORIGIN 320, 200
190 MOVE -100, -100
200 DRAW 100, -100, 1
210 DRAW 100, 100
220 DRAW -100, 100
230 DRAW -100, -100
240 REM Un L rouge dans le carre, avec des ecarts
250 MOVE 0, 0
260 MOVER -50, -50
270 DRAWR 100, 0, 3
280 DRAWR 0, 100
290 REM Une ligne de points cyan au-dessus du carre
300 MOVE -100, 120
310 FOR I = 1 TO 20
320 PLOTR 10, 0, 2
330 NEXT
340 REM Un point jaune au centre
350 PLOT 0, 0, 1
360 REM Une rangee de 21 points jaunes sous le carre
370 FOR I = -100 TO 100 STEP 10
380 PLOT I, -120, 1
390 NEXT
400 LOCATE 1, 24
410 PRINT "APPUYEZ SUR UNE TOUCHE"
420 END
