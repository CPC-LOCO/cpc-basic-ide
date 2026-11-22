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

    const string Source = """
                ; ---------- DRAW et DRAWR rapides (D204) : nos traits à la place de GRA LINE de la ROM ----------
                ; Mêmes réglages que le firmware (origine, curseur, stylo, fenêtre graphique, mode d'écriture par SCR WRITE).
                ; La ROM garde les cas rares : un bout hors de la fenêtre graphique, écran déplacé (défilement du texte ou
                ; autre adresse que &C000). Points : même calcul que la cartouche (D202, D203).
                ; LINER : DE, HL = écart (repère de l'utilisateur) ; LINE : DE, HL = bout
        LINER:  PUSH HL
                PUSH DE
                CALL &BBC6              ; GRA ASK CURSOR
                POP BC
                EX DE,HL
                ADD HL,BC
                EX DE,HL
                POP BC
                ADD HL,BC
        LINE:   LD (ENDX),DE
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
        DY1:    PUSH BC
                PUSH HL
                CALL &BDE8              ; SCR WRITE
                POP HL
                POP BC
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
        DX1:    PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
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
                PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
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
                PUSH BC
                PUSH HL
                CALL &BDE8
                POP HL
                POP BC
                POP AF
                RET Z
                INC HL
                JR HZ3
        HZ4:    POP HL
                LD A,(LMASK)
                LD C,A
                JR HZ1
        HZ5:    LD C,D
                JP &BDE8
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
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_ln_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module des traits : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
