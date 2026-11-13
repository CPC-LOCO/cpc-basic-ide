# Programme de référence : RSX en deux lignes (concours)

Programme de l'utilisateur (RetroPoke), gardé ici comme **cas d'essai de compatibilité maximale**. Il ne se compile pas encore : il sert de but. Il ne doit **pas** être mis dans `examples\` (les exemples publiés doivent tous compiler).

```
10 MODE 1:p=&A400:MEMORY p-1:READ a$:FOR a=0 TO &74:POKE p+a,VAL("&"+MID$(a$,a*2+1,2)):NEXT:SAVE"pack-gph",b,&A400,&74:CALL &A400:|GPEN,3:a$="DEUULIGNE RSX 01":FOR l=-6 TO 8:|GPAS,l,8:MOVE 150-l*4,170-l*16:|GPRINT,@a$:NEXT:LOCATE 28,23:PRINT"RSX PACK #01"
20 DATA "0109A421F7BDC3D1BC1AA4C333A4C351A4C35FA4C36DA4C371A4475052494ED4475041D3475045CE474D4F44C547464F4EC400EB7EB7C847235E2356C51A13D5CDFCBB110000210400CDC3BBD1C110ECC9ED5347A4DD6603DD6E022244A4C93D28077BCDE4BBDD5E027BC3DEBB7BC359BC7BC39FBB"
```

## Ce qu'il faut pour le compiler (et dans quel lot)

| Besoin | Lot |
|---|---|
| Plusieurs instructions par ligne avec « : » (déjà fait) | fait |
| `MEMORY p-1` (accepté ; la limite n'est vérifiée que si c'est un nombre écrit) | C (fait, D057) |
| `POKE`, `CALL` avec une adresse (déjà faits) | C (fait) |
| `DATA` avec un texte, `READ a$` | C et E |
| Nombres hexadécimaux `&A400`, `&74` | D |
| Variables texte `a$`, `MID$`, `VAL`, concaténation `+`, texte en `DATA` | E |
| Variable de boucle qui change d'un tour à l'autre et `p+a` hors de 32767 (adresses au-dessus de &7FFF dans un calcul) | D et F |
| `SAVE "nom",b,adresse,longueur` (enregistre un bloc de mémoire) | J |
| Commandes RSX `|GPEN,3`, `|GPAS,l,8`, `|GPRINT,@a$` et le paramètre `@a$` (adresse d'une variable texte) | lot à créer : RSX et paramètres `@` (voir les questions dans le journal) |
| `MOVE` avec calcul et nombres négatifs (déjà fait) | fait |

Notes : le programme crée lui-même ses commandes RSX en mémoire (code Z80 écrit avec POKE) puis les appelle. Un compilateur qui produit du Z80 doit donc laisser ce code libre de s'installer (c'est le rôle de `MEMORY` : tenir le programme compilé sous &A400).
