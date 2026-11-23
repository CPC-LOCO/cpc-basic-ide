// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// DRAW et DRAWR rapides (D204) : nos propres traits à la place de GRA LINE de la ROM, avec les réglages du firmware
/// (origine, curseur, stylo, fenêtre graphique ; SCR WRITE pour le mode d'écriture). Même calcul des points que la
/// cartouche (D202, D203) : l'adresse et le masque suivent d'un point au suivant, les traits horizontaux s'écrivent
/// octet par octet. La ROM garde les cas rares : un bout hors de la fenêtre graphique, écran déplacé (défilement du
/// texte). Pas utilisé avec MASK (le motif suit l'ordre des points de la ROM, D110).
/// </summary>
public static class LineRuntime
{
    public const string LineLabel = "rt_ln_LINE";       // DE = x, HL = y (repère de l'utilisateur)
    public const string RelativeLabel = "rt_ln_LINER";  // DE, HL = écart
    // D205 : pour un programme qui ne change jamais le mode d'écriture (pas de 4e valeur à DRAW, PLOT..., ni CALL, ni
    // commande de bibliothèque) : les points sont écrits directement dans l'écran, sans SCR WRITE
    public const string DirectLabel = "rt_ln_LINEN";
    public const string DirectRelativeLabel = "rt_ln_LINERN";
    // D210 : PLOT et PLOTR rapides (écriture normale) : réglages, curseur et stylo gardés d'un point à l'autre
    public const string PlotLabel = "rt_ln_PLOTN";
    public const string PlotRelativeLabel = "rt_ln_PLOTRN";
    public const string PlotValidLabel = "rt_ln_POK";
    public const string PlotPenValidLabel = "rt_ln_PNOK";
    public const string PlotPenLabel = "rt_ln_PENSET";   // A = stylo de PLOT x,y,stylo
    public const string PlotSyncLabel = "rt_ln_SYNC";    // avant une instruction qui pourrait changer les réglages
    public const string PlotFlushLabel = "rt_ln_FLUSH";  // avant DRAW, XPOS, TEST... : curseur et stylo donnés à la ROM
    // D212 : MOVE et MOVER (sans stylo) d'un programme avec PLOT rapide : seulement notre curseur gardé, aucun appel à la ROM
    public const string MoveLabel = "rt_ln_MOVEN";
    public const string MoveRelativeLabel = "rt_ln_MOVERN";

    const string Source = """
                ; ---------- DRAW et DRAWR rapides (D204) : nos traits à la place de GRA LINE de la ROM ----------
                ; Mêmes réglages que le firmware (origine, curseur, stylo, fenêtre graphique, mode d'écriture par SCR WRITE).
                ; La ROM garde les cas rares : un bout hors de la fenêtre graphique, écran déplacé (défilement du texte ou
                ; autre adresse que &C000). Points : même calcul que la cartouche (D202, D203).
                ; LINER : DE, HL = écart (repère de l'utilisateur) ; LINE : DE, HL = bout. LINERN, LINEN : pareil, pour un
                ; programme qui ne change jamais le mode d'écriture (le compilateur le sait) : points écrits directement
                ; dans l'écran, sans passer par SCR WRITE (D205)
        LINERN: LD A,1
                JR LR0
        LINER:  XOR A
        LR0:    LD (DIRECT),A
                PUSH HL
                PUSH DE
                CALL &BBC6              ; GRA ASK CURSOR
                POP BC
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                POP BC
                ADD HL,BC
                JR LN00
        LINEN:  LD A,1
                JR LL0
        LINE:   XOR A
        LL0:    LD (DIRECT),A
        LN00:   LD (ENDX),DE
                LD (ENDY),HL
                CALL &BC0B              ; SCR GET LOCATION : écran en &C000, sans décalage ?
                CP &C0
                JP NZ,ROM
                LD A,H
                OR L
                JP NZ,ROM
                CALL &BC11              ; SCR GET MODE : décalage x (2, 1, 0), masques du point de gauche et de droite
                AND 3
                CP 3
                JR NZ,LN0
                XOR A
        LN0:    LD E,A
                LD D,0
                LD HL,MODES
                ADD HL,DE
                ADD HL,DE
                ADD HL,DE
                LD A,(HL)
                LD (SHF),A
                INC HL
                LD A,(HL)
                LD (LMASK),A
                INC HL
                LD A,(HL)
                LD (RMASK),A
                CALL &BBCC              ; GRA GET ORIGIN
                LD (OX),DE
                LD (OY),HL
                CALL &BBC6              ; début du trait
                CALL TOBASE
                LD (X1),DE
                LD (Y1),HL
                LD DE,(ENDX)
                LD HL,(ENDY)
                CALL TOBASE
                LD (X2),DE
                LD (Y2),HL
                CALL &BBD5              ; GRA GET W WIDTH : DE = gauche, HL = droite (repère de l'écran)
                CALL XBASE
                LD (WR),HL
                EX DE,HL
                CALL XBASE
                LD (WL),HL
                CALL &BBD8              ; GRA GET W HEIGHT : DE = haut, HL = bas
                SRA H
                RR L
                LD (WB),HL
                EX DE,HL
                SRA H
                RR L
                LD (WT),HL
                LD DE,(X1)              ; les deux bouts dans la fenêtre, donc tout le trait
                LD HL,(Y1)
                CALL INSIDE
                JR C,ROM
                LD DE,(X2)
                LD HL,(Y2)
                CALL INSIDE
                JR NC,LN1
        ROM:    LD DE,(ENDX)              ; la ROM s'en charge
                LD HL,(ENDY)
                JP &BBF6
                ; SCR WRITE (&BDE8) est une routine de la ROM basse, appelée directement : la ROM basse est branchée (et la
                ; ROM haute débranchée, pour lire l'écran) pendant le tracé, puis remises comme avant
        LN1:    CALL &B906              ; KL L ROM ENABLE : A = état d'avant
                LD (ROMS),A
                CALL &B903              ; KL U ROM DISABLE
                CALL DRAW1
                LD A,(ROMS)
                JP &B90C                ; KL ROM RESTORE
        DRAW1:  CALL &BBE1              ; GRA GET PEN, puis SCR INK ENCODE
                CALL &BC2C
                LD (ENC),A
                LD DE,(ENDX)              ; le curseur au bout, comme la ROM
                LD HL,(ENDY)
                CALL &BBC0
                LD HL,(X2)              ; dx et sens en x
                LD DE,(X1)
                OR A
                SBC HL,DE
                LD A,1
                JR NC,LN2
                CALL NEGHL
                LD A,&FF
        LN2:    LD (DLX),HL
                LD (SGX),A
                LD HL,(Y2)              ; dy et sens en y
                LD DE,(Y1)
                OR A
                SBC HL,DE
                LD A,1
                JR NC,LN3
                CALL NEGHL
                LD A,&FF
        LN3:    LD (DLY),HL
                LD (SGY),A
                LD A,H
                OR L
                JP Z,HORIZ
                LD A,(DIRECT)           ; écriture normale : les boucles rapides (D205)
                OR A
                JP NZ,FAST
                LD DE,(X1)
                LD HL,(Y1)
                CALL &BC1D              ; SCR DOT POSITION : HL = adresse, C = masque du point
                LD A,(ENC)
                LD B,A
                PUSH HL
                LD HL,(DLY)
                LD DE,(DLX)
                OR A
                SBC HL,DE
                POP HL
                JR C,DX0
                JR Z,DX0
                LD DE,(DLY)              ; surtout vertical
                LD (CNT),DE
                SRL D
                RR E
                LD (ERRS),DE
        DY1:    CALL WRITE
                PUSH HL
                LD HL,(CNT)
                LD A,H
                OR L
                DEC HL
                LD (CNT),HL
                POP HL
                RET Z
                CALL STEPY
                PUSH HL
                LD HL,(ERRS)
                LD DE,(DLX)
                ADD HL,DE
                LD DE,(DLY)
                OR A
                SBC HL,DE
                JR NC,DY2
                ADD HL,DE
                LD (ERRS),HL
                POP HL
                JR DY1
        DY2:    LD (ERRS),HL
                POP HL
                CALL STEPX
                JR DY1
        DX0:    LD DE,(DLX)              ; surtout horizontal
                LD (CNT),DE
                SRL D
                RR E
                LD (ERRS),DE
        DX1:    CALL WRITE
                PUSH HL
                LD HL,(CNT)
                LD A,H
                OR L
                DEC HL
                LD (CNT),HL
                POP HL
                RET Z
                CALL STEPX
                PUSH HL
                LD HL,(ERRS)
                LD DE,(DLY)
                ADD HL,DE
                LD DE,(DLX)
                OR A
                SBC HL,DE
                JR NC,DX2
                ADD HL,DE
                LD (ERRS),HL
                POP HL
                JR DX1
        DX2:    LD (ERRS),HL
                POP HL
                CALL STEPY
                JR DX1
                ; trait horizontal : de gauche à droite, octet par octet (plusieurs points d'un coup)
        HORIZ:  LD A,(SGX)
                INC A
                JR NZ,HZ0
                LD HL,(X1)
                LD DE,(X2)
                LD (X1),DE
                LD (X2),HL
        HZ0:    LD HL,(DLX)
                INC HL
                LD (CNT),HL             ; nombre de points
                LD DE,(X1)
                LD HL,(Y1)
                CALL &BC1D              ; HL = adresse, C = masque, B = points par octet - 1
                LD A,B
                LD (PPB1),A
                LD A,(X1)
                AND B
                LD E,A
                LD A,B
                SUB E
                INC A
                LD E,A                  ; points de ce premier octet
                LD A,(ENC)
                LD B,A
                LD D,0
        HZ1:    LD A,D
                OR C
                LD D,A
                PUSH HL
                LD HL,(CNT)
                DEC HL
                LD (CNT),HL
                LD A,H
                OR L
                POP HL
                JR Z,HZ5
                DEC E
                JR Z,HZ2
                SRL C
                JR HZ1
        HZ2:    LD C,D                  ; octet fini
                CALL WRITE
                INC HL
        HZ3:    LD A,(PPB1)             ; octets pleins
                INC A
                LD E,A
                LD D,0
                PUSH HL
                LD HL,(CNT)
                OR A
                SBC HL,DE
                JR C,HZ4
                LD (CNT),HL
                LD A,H
                OR L
                POP HL
                PUSH AF
                LD C,&FF
                CALL WRITE
                POP AF
                RET Z
                INC HL
                JR HZ3
        HZ4:    POP HL
                LD A,(LMASK)
                LD C,A
                JR HZ1
        HZ5:    LD C,D
                ; un octet écrit : directement (écriture normale) ou par SCR WRITE ; HL, BC gardés
        WRITE:  LD A,(DIRECT)
                OR A
                JR Z,WRF
                LD A,C
                CPL
                AND (HL)
                LD (HL),A
                LD A,B
                AND C
                OR (HL)
                LD (HL),A
                RET
        WRF:    PUSH BC
                PUSH HL
                CALL &BDE8              ; SCR WRITE
                POP HL
                POP BC
                RET
                ; ---- boucles rapides en écriture normale (D205) : DE = adresse, C = masque inversé (points gardés),
                ; B = encre codée, HL = reste du calcul ; l'octet écrit vaut ((ancien XOR encre) AND C) XOR encre ----
        FAST:   LD DE,(X1)
                LD HL,(Y1)
                CALL &BC1D
                LD A,C
                CPL
                LD C,A
                EX DE,HL
                LD A,(LMASK)
                CPL
                LD (ILM),A
                LD A,(RMASK)
                CPL
                LD (IRM),A
                LD A,(ENC)
                LD B,A
                PUSH DE
                LD HL,(DLY)             ; surtout vertical si dy > dx
                LD DE,(DLX)
                OR A
                SBC HL,DE
                POP DE
                JR C,FX0
                JR Z,FX0
                LD HL,(DLX)             ; petit écart : dx, grand : dy
                LD (DMIN),HL
                LD HL,(DLY)
                CALL FPREP
        FYL:    LD A,(DE)
                XOR B
                AND C
                XOR B
                LD (DE),A
                LD A,(CNTL)
                DEC A
                LD (CNTL),A
                JR NZ,FY1
                LD A,(CNTH)
                DEC A
                LD (CNTH),A
                RET Z
        FY1:    CALL FSTY
                LD A,(DMIN)
                ADD A,L
                LD L,A
                LD A,(DMIN+1)
                ADC A,H
                LD H,A
                LD A,(NMAJ)
                ADD A,L
                LD L,A
                LD A,(NMAJ+1)
                ADC A,H
                LD H,A
                JR NC,FY2
                CALL FSTX
                JR FYL
        FY2:    LD A,(DMAJ)
                ADD A,L
                LD L,A
                LD A,(DMAJ+1)
                ADC A,H
                LD H,A
                JR FYL
        FX0:    LD HL,(DLY)             ; petit écart : dy, grand : dx
                LD (DMIN),HL
                LD HL,(DLX)
                CALL FPREP
        FXL:    LD A,(DE)
                XOR B
                AND C
                XOR B
                LD (DE),A
                LD A,(CNTL)
                DEC A
                LD (CNTL),A
                JR NZ,FX1
                LD A,(CNTH)
                DEC A
                LD (CNTH),A
                RET Z
        FX1:    CALL FSTX
                LD A,(DMIN)
                ADD A,L
                LD L,A
                LD A,(DMIN+1)
                ADC A,H
                LD H,A
                LD A,(NMAJ)
                ADD A,L
                LD L,A
                LD A,(NMAJ+1)
                ADC A,H
                LD H,A
                JR NC,FX2
                CALL FSTY
                JR FXL
        FX2:    LD A,(DMAJ)
                ADD A,L
                LD L,A
                LD A,(DMAJ+1)
                ADC A,H
                LD H,A
                JR FXL
                ; HL = grand écart : nombre de points (CNTL, CNTH comptés pour DEC), -écart, reste de départ = écart / 2
        FPREP:  LD (DMAJ),HL
                PUSH HL
                INC HL
                LD A,L
                LD (CNTL),A
                OR A
                LD A,H
                JR Z,FP1
                INC A
        FP1:    LD (CNTH),A
                POP HL
                PUSH HL
                CALL NEGHL
                LD (NMAJ),HL
                POP HL
                SRL H
                RR L
                RET
                ; un point à droite ou à gauche : DE, C (masque inversé)
        FSTX:   LD A,(SGX)
                DEC A
                JR NZ,FSXL
                LD A,(IRM)
                CP C
                JR Z,FSXR
                RRC C
                RET
        FSXR:   LD A,(ILM)
                LD C,A
                INC DE
                RET
        FSXL:   LD A,(ILM)
                CP C
                JR Z,FSXL2
                RLC C
                RET
        FSXL2:  LD A,(IRM)
                LD C,A
                DEC DE
                RET
                ; une ligne de l'écran plus haut ou plus bas : DE
        FSTY:   LD A,(SGY)
                DEC A
                JR NZ,FSYD
                LD A,D
                SUB 8
                LD D,A
                CP &C0
                RET NC
                LD A,E
                ADD A,&B0
                LD E,A
                LD A,D
                ADC A,&3F
                LD D,A
                RET
        FSYD:   LD A,D
                ADD A,8
                LD D,A
                RET NC
                LD A,E
                ADD A,&50
                LD E,A
                LD A,D
                ADC A,&C0
                LD D,A
                RET
                ; un point à droite (SX = 1) ou à gauche
        STEPX:  LD A,(SGX)
                DEC A
                JR NZ,SXL
                LD A,(RMASK)
                CP C
                JR Z,SXR
                SRL C
                RET
        SXR:    LD A,(LMASK)
                LD C,A
                INC HL
                RET
        SXL:    LD A,(LMASK)
                CP C
                JR Z,SXL2
                SLA C
                RET
        SXL2:   LD A,(RMASK)
                LD C,A
                DEC HL
                RET
                ; une ligne de l'écran plus haut (SY = 1) ou plus bas
        STEPY:  LD A,(SGY)
                DEC A
                JR NZ,SYD
                LD A,H
                SUB 8
                LD H,A
                CP &C0
                RET NC
                LD DE,&3FB0
                ADD HL,DE
                RET
        SYD:    LD A,H
                ADD A,8
                LD H,A
                RET NC
                LD DE,&C050
                ADD HL,DE
                RET
                ; DE = x, HL = y de l'utilisateur -> points de base (avec l'origine)
        TOBASE: PUSH HL
                LD HL,(OX)
                ADD HL,DE
                CALL XBASE
                EX DE,HL
                POP HL
                LD BC,(OY)
                ADD HL,BC
                SRA H
                RR L
                RET
                ; HL (repère de l'écran, en x) -> point de base
        XBASE:  LD A,(SHF)
                OR A
                RET Z
        XB1:    SRA H
                RR L
                DEC A
                JR NZ,XB1
                RET
                ; retenue si DE, HL (points de base) est hors de la fenêtre
        INSIDE: BIT 7,D
                SCF
                RET NZ
                BIT 7,H
                SCF
                RET NZ
                PUSH HL
                LD HL,(WR)              ; x > WR ?
                OR A
                SBC HL,DE
                JR C,INOUT
                LD H,D                  ; x < WL ?
                LD L,E
                LD BC,(WL)
                OR A
                SBC HL,BC
                JR C,INOUT
                POP HL
                PUSH HL
                LD B,H                  ; y > WT ?
                LD C,L
                LD HL,(WT)
                OR A
                SBC HL,BC
                JR C,INOUT
                POP HL
                PUSH HL
                LD BC,(WB)              ; y < WB ?
                OR A
                SBC HL,BC
                JR C,INOUT
                POP HL
                OR A
                RET
        INOUT:  POP HL
                SCF
                RET
        NEGHL:  XOR A
                SUB L
                LD L,A
                SBC A,A
                SUB H
                LD H,A
                RET
                ; ---------- PLOT et PLOTR rapides (D210) : DE = x, HL = y (ou l'écart) ; écriture normale ----------
                ; Aucun appel à la ROM pour un point. Réglages lus une fois (POK = 1 ; 2 : écran déplacé, la ROM dessine) :
                ; MODE, origine, fenêtre graphique. Le curseur (PX, PY) et le stylo (PVAL) sont gardés ici et donnés à la
                ; ROM seulement quand une autre instruction en a besoin (FLUSH) ; le programme appelle SYNC avant toute
                ; instruction qui pourrait changer les réglages (FLUSH, puis réglages à relire), FLUSH avant MOVE, DRAW,
                ; XPOS, TEST... Stylo codé ici (ENCODE), comme SCR INK ENCODE.
                ; MOVE et MOVER (D212) : le curseur gardé ici, donné à la ROM plus tard (FLUSH)
        MOVERN: CALL CURREL
        MOVEN:  LD (PX),DE
                LD (PY),HL
                LD A,1
                LD (DIRTY),A
                RET
                ; DE, HL = écart -> point, depuis le curseur : le nôtre, ou celui de la ROM
        CURREL: LD A,(DIRTY)
                OR A
                JR Z,CR1
                LD BC,(PX)
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                LD BC,(PY)
                ADD HL,BC
                RET
        CR1:    PUSH HL
                PUSH DE
                CALL &BBC6              ; GRA ASK CURSOR
                POP BC
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                POP BC
                ADD HL,BC
                RET
        PLOTRN: LD A,(DIRTY)            ; écart depuis le curseur : le nôtre, ou celui de la ROM
                OR A
                JR Z,PR1
                LD BC,(PX)
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                LD BC,(PY)
                ADD HL,BC
                JR PLOTN
        PR1:    PUSH HL
                PUSH DE
                CALL &BBC6              ; GRA ASK CURSOR
                POP BC
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                POP BC
                ADD HL,BC
        PLOTN:  LD A,(POK)
                DEC A
                JP NZ,PLSLOW
        PLGO:   LD (PX),DE              ; le curseur va au point (donné à la ROM plus tard)
                LD (PY),HL
                LD A,1
                LD (DIRTY),A
                LD A,(PNOK)
                OR A
                CALL Z,PPEN
                LD BC,(OY)              ; ligne de base = (y + origine) / 2, de 0 (en bas) à 199
                ADD HL,BC
                SRA H
                RR L
                LD A,H
                OR A
                RET NZ                  ; négative ou trop haute : dehors
                LD A,(WBB)
                LD C,A
                LD A,L
                CP C
                RET C                   ; sous la fenêtre
                LD C,A
                LD A,(WTB)
                CP C
                RET C                   ; au-dessus de la fenêtre
                EX DE,HL                ; point de base = (x + origine) / taille d'un point
                LD DE,(OX)
                ADD HL,DE
                BIT 7,H
                RET NZ
                LD A,(SHF)
                OR A
                JR Z,PX0
                LD B,A
        PXS:    SRL H
                RR L
                DJNZ PXS
        PX0:    LD DE,(WL)
                OR A
                SBC HL,DE
                RET C                   ; à gauche de la fenêtre
                ADD HL,DE
                EX DE,HL
                LD HL,(WR)
                OR A
                SBC HL,DE
                RET C                   ; à droite de la fenêtre
                LD A,(PPM)              ; DE = point : rang dans son octet, masque
                AND E
                LD B,A
                LD A,(LMASK)
                JR Z,PM1
        PM0:    SRL A
                DJNZ PM0
        PM1:    LD (PMSK),A
                LD A,(PSH)              ; octet dans la ligne
                LD B,A
        PB1:    SRL D
                RR E
                DJNZ PB1
                LD A,199                ; ligne de l'écran depuis le haut
                SUB C
                LD C,A
                AND &F8                 ; (ligne AND &F8) x 10 = (ligne / 8) x 80
                LD L,A
                LD H,0
                ADD HL,HL
                LD B,H
                LD A,L
                ADD HL,HL
                ADD HL,HL
                ADD A,L
                LD L,A
                LD A,B
                ADC A,H
                LD H,A
                ADD HL,DE
                LD A,C                  ; + &C000 + (ligne AND 7) x &800
                AND 7
                ADD A,A
                ADD A,A
                ADD A,A
                OR &C0
                ADD A,H
                LD H,A
                CALL &B903              ; KL U ROM DISABLE (pour lire l'écran) : A = état d'avant
                LD B,A
                LD A,(PMSK)
                LD C,A
                LD A,(PENC)
                XOR (HL)
                AND C
                XOR (HL)
                LD (HL),A
                LD A,B
                JP &B90C                ; KL ROM RESTORE
        PLSLOW: INC A                   ; POK = 0 : réglages à lire ; POK = 2 : la ROM
                JR NZ,PLROM
                PUSH HL
                PUSH DE
                CALL PREP
                POP DE
                POP HL
                LD A,(POK)
                DEC A
                JP Z,PLGO
        PLROM:  PUSH HL                 ; écran déplacé : la ROM s'en charge (curseur et stylo donnés d'abord)
                PUSH DE
                CALL FLUSH
                POP DE
                POP HL
                CALL &BBEA              ; (appel puis retour : le simulateur des tests ne suit pas un saut vers GRA PLOT)
                RET
                ; PLOT x,y,stylo : A = stylo, gardé ici (donné à la ROM plus tard)
        PENSET: LD (PVAL),A
                LD A,1
                LD (PENDIRTY),A
                XOR A
                LD (PNOK),A
                RET
                ; avant une instruction qui pourrait changer les réglages
        SYNC:   CALL FLUSH
                XOR A
                LD (POK),A
                LD (PNOK),A
                RET
                ; curseur et stylo gardés ici : donnés à la ROM
        FLUSH:  LD A,(PENDIRTY)
                OR A
                JR Z,FL1
                XOR A
                LD (PENDIRTY),A
                LD A,(PVAL)
                CALL &BBDE              ; GRA SET PEN
        FL1:    LD A,(DIRTY)
                OR A
                RET Z
                XOR A
                LD (DIRTY),A
                LD DE,(PX)
                LD HL,(PY)
                CALL &BBC0              ; GRA MOVE ABSOLUTE
                RET
                ; le stylo codé pour le MODE (gardé ou lu dans la ROM) ; DE, HL gardés
        PPEN:   PUSH HL
                PUSH DE
                LD A,(PENDIRTY)
                OR A
                LD A,(PVAL)
                CALL Z,&BBE1            ; GRA GET PEN
                LD C,A
                LD A,(PMODE)
                OR A
                JR Z,EN0
                DEC A
                JR Z,EN1
                LD A,C                  ; MODE 2 : bit 0 -> &FF
                RRA
                SBC A,A
                JR ENF
        EN1:    XOR A                   ; MODE 1 : bit 0 -> &F0, bit 1 -> &0F
                BIT 0,C
                JR Z,EN11
                OR &F0
        EN11:   BIT 1,C
                JR Z,ENF
                OR &0F
                JR ENF
        EN0:    XOR A                   ; MODE 0 : bits 0, 1, 2, 3 -> &C0, &0C, &30, &03
                BIT 0,C
                JR Z,EN01
                OR &C0
        EN01:   BIT 1,C
                JR Z,EN02
                OR &0C
        EN02:   BIT 2,C
                JR Z,EN03
                OR &30
        EN03:   BIT 3,C
                JR Z,ENF
                OR &03
        ENF:    LD (PENC),A
                LD A,1
                LD (PNOK),A
                POP DE
                POP HL
                RET
                ; les réglages du firmware, gardés
        PREP:   CALL &BC0B              ; SCR GET LOCATION : écran en &C000, sans décalage ?
                CP &C0
                JP NZ,PPROM
                LD A,H
                OR L
                JP NZ,PPROM
                CALL &BC11              ; SCR GET MODE
                AND 3
                CP 3
                JR NZ,PP0
                XOR A
        PP0:    LD (PMODE),A
                LD E,A
                LD D,0
                LD HL,MODES
                ADD HL,DE
                ADD HL,DE
                ADD HL,DE
                LD A,(HL)
                LD (SHF),A
                INC HL
                LD A,(HL)
                LD (LMASK),A
                INC HL
                LD A,(HL)
                LD (RMASK),A
                LD A,E                  ; points par octet : 2, 4, 8 (décalage 1, 2, 3 ; masque 1, 3, 7)
                INC A
                LD (PSH),A
                LD B,A
                XOR A
        PP1:    SCF
                RLA
                DJNZ PP1
                LD (PPM),A
                CALL &BBCC              ; GRA GET ORIGIN
                LD (OX),DE
                LD (OY),HL
                CALL &BBD5              ; GRA GET W WIDTH
                CALL XBASE
                LD (WR),HL
                EX DE,HL
                CALL XBASE
                LD (WL),HL
                CALL &BBD8              ; GRA GET W HEIGHT : en lignes de base (0 à 199)
                SRA H
                RR L
                LD A,L
                LD (WBB),A
                EX DE,HL
                SRA H
                RR L
                LD A,L
                LD (WTB),A
                XOR A
                LD (PNOK),A             ; le stylo dépend du MODE : recodé aussi
                INC A
                LD (POK),A
                RET
        PPROM:  LD A,2
                LD (POK),A
                RET
                ; par MODE : décalage x, masque du point de gauche, du point de droite
        MODES:  DEFB 2,&AA,&55,1,&88,&11,0,&80,&01
        ENDX:     DEFS 2
        ENDY:     DEFS 2
        OX:     DEFS 2
        OY:     DEFS 2
        X1:     DEFS 2
        Y1:     DEFS 2
        X2:     DEFS 2
        Y2:     DEFS 2
        WL:     DEFS 2
        WR:     DEFS 2
        WT:     DEFS 2
        WB:     DEFS 2
        DLX:     DEFS 2
        DLY:     DEFS 2
        CNT:    DEFS 2
        ERRS:    DEFS 2
        SGX:     DEFS 1
        SGY:     DEFS 1
        SHF:     DEFS 1
        LMASK:  DEFS 1
        RMASK:  DEFS 1
        ENC:    DEFS 1
        PPB1:   DEFS 1
        ROMS:   DEFS 1
        DIRECT: DEFS 1
        ILM:    DEFS 1
        IRM:    DEFS 1
        CNTL:   DEFS 1
        CNTH:   DEFS 1
        DMIN:   DEFS 2
        DMAJ:   DEFS 2
        NMAJ:   DEFS 2
        POK:    DEFS 1
        PNOK:   DEFS 1
        PENC:   DEFS 1
        PSH:    DEFS 1
        PPM:    DEFS 1
        PMODE:  DEFS 1
        PMSK:   DEFS 1
        WBB:    DEFS 1
        WTB:    DEFS 1
        DIRTY:  DEFS 1
        PENDIRTY: DEFS 1
        PVAL:   DEFS 1
        PX:     DEFS 2
        PY:     DEFS 2
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_ln_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module des traits : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
