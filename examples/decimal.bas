10 MODE 1
20 PRINT DEC$(3.14159,"##.##");"/";DEC$(-5,"+###");"/";DEC$(1234.5,"$$#,###.##")
30 PRINT "[";DEC$(123456,"##");"]"
40 F$="#.#":PRINT "[";DEC$(2.25,F$);"]"
50 A$=DEC$(7,"###"):PRINT "LONGUEUR";LEN(A$);"[";A$;"]"
60 FOR J = 1 TO 1 : FOR I = 1 TO 30000 : NEXT : NEXT
70 PRINT "FORMAT A## DANS F$ :"
80 F$="A##":PRINT DEC$(5,F$)
