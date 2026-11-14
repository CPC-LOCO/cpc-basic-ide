10 REM @ : adresse d'une variable
20 MODE 1
30 A% = 1234
40 PRINT "A% = "; A%
50 P = @A%
60 PRINT "OCTET BAS : "; PEEK(P); " OCTET HAUT : "; PEEK(P + 1)
70 POKE @A%, 0 : POKE @A% + 1, 1
80 PRINT "APRES LES POKE, A% = "; A%
90 DIM T%(3)
100 PRINT "@T%(1) - @T%(0) = "; @T%(1) - @T%(0)
110 FOR J = 1 TO 1 : FOR I = 1 TO 30000 : NEXT : NEXT
120 PRINT "APPUYEZ SUR UNE TOUCHE"
130 END
