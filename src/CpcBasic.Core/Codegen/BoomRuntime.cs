// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// BOOM son [, durée] (D214) : explosions et chocs, choisis à l'oreille par l'utilisateur (examples\boomref.bas,
/// 23 nov. 2026) : 1 voiture qui explose, 2 feu d'artifice, 3 pétard ou coup de fusil, 4 marteau qui frappe, 5 malus.
/// Durée en centièmes de seconde (0 : celle du son) : le volume s'éteint en 15 marches de durée / 15 ; pour le malus, la
/// descente du son dure aussi la durée (60 pas de durée / 60). Enveloppes posées juste avant chaque son, numéros pris
/// dans la réserve commune des effets (D218 ; avant : ENV 9 et 10, ENT 8 à 10).
/// </summary>
public static class BoomRuntime
{
    public const string EntryLabel = "rt_bm_BOOM";      // A = son - 1 (0 à 4), HL = durée (0 : celle du son)

    const string Source = """
                ; ---------- BOOM (D214) : A = son - 1, HL = durée (0 : celle du son) ----------
        BOOM:   LD (NUM),A
                LD A,H
                OR L
                JR NZ,BM0
                LD A,(NUM)              ; durée du son
                ADD A,A
                LD E,A
                LD D,0
                LD HL,DURS
                ADD HL,DE
                LD A,(HL)
                INC HL
                LD H,(HL)
                LD L,A
        BM0:    LD (DUR),HL
                LD DE,15                ; marches du volume : durée / 15
                CALL DIVP
                LD (EVDEC+3),A
                LD HL,(DUR)
                LD DE,60                ; pas de la descente du malus : durée / 60
                CALL DIVP
                LD (ETMAL+3),A
                LD HL,(DUR)             ; durée des parties principales
                LD (S1B+7),HL
                LD (S1C+7),HL
                LD (S2B+7),HL
                LD (S3+7),HL
                LD (S4A+7),HL
                LD (S4B+7),HL
                LD (S5A+7),HL
                LD (S5B+7),HL
                CALL NEWENV             ; l'enveloppe qui éteint le son, pour toutes les parties principales
                LD (S1B+1),A
                LD (S1C+1),A
                LD (S2B+1),A
                LD (S3+1),A
                LD (S4A+1),A
                LD (S4B+1),A
                LD (S5A+1),A
                LD (S5B+1),A
                LD HL,EVDEC
                CALL &BCBC              ; SOUND AMPL ENVELOPE
                LD A,(NUM)
                OR A
                JP Z,B1
                DEC A
                JP Z,B2
                DEC A
                JP Z,B3
                DEC A
                JP Z,B4
                CALL NEWENT             ; 5 : malus
                LD (S5A+2),A
                LD (S5B+2),A
                LD HL,ETMAL
                CALL &BCBF              ; SOUND TONE ENVELOPE
                LD HL,S5A
                CALL QUEUE
                LD HL,S5B
                JP QUEUE
        B1:     LD HL,S1A               ; 1 : voiture qui explose
                CALL QUEUE
                LD HL,S1B
                CALL QUEUE
                LD HL,S1C
                JP QUEUE
        B2:     CALL NEWENT             ; 2 : feu d'artifice (sifflement, boom, crépitement)
                LD (S2A+2),A
                LD HL,ETSIF
                CALL &BCBF
                CALL NEWENV
                LD (S2C+1),A
                LD HL,EVCRP
                CALL &BCBC
                LD HL,S2A
                CALL QUEUE
                LD HL,S2B
                CALL QUEUE
                LD HL,S2C
                JP QUEUE
        B3:     LD HL,S3                ; 3 : pétard, coup de fusil
                JP QUEUE
        B4:     CALL NEWENT             ; 4 : marteau qui frappe
                LD (S4A+2),A
                LD (S4B+2),A
                LD HL,ETMAR
                CALL &BCBF
                LD HL,S4A
                CALL QUEUE
                LD HL,S4B
                ; file du canal pleine : on attend une place, comme SOUND
        QUEUE:  PUSH HL
                CALL &BCAA              ; SOUND QUEUE
                POP HL
                JR NC,QUEUE
                RET
                ; A = HL / DE, de 1 à 255
        DIVP:   LD B,0
        DV1:    OR A
                SBC HL,DE
                JR C,DV2
                INC B
                JR NZ,DV1
                LD A,255
                RET
        DV2:    LD A,B
                OR A
                RET NZ
                INC A
                RET
        NUM:    DEFS 1
        DUR:    DEFS 2
        DURS:   DEFW 200,30,30,15,90
                ; enveloppes (16 octets : nombre de parties, puis pas, taille, pause)
        EVDEC:  DEFB 1,15,&FF,13
                DEFS 12
        EVCRP:  DEFB 3,5,&FD,3,5,3,3,5,&FD,3
                DEFS 6
        ETSIF:  DEFB 1,60,&FD,1
                DEFS 12
        ETMAR:  DEFB 1,20,8,1
                DEFS 12
        ETMAL:  DEFB 1,60,6,1
                DEFS 12
                ; sons (9 octets : canal, env. volume, env. ton, période, bruit, volume, durée)
        S1A:    DEFB 1,0,0
                DEFW 0
                DEFB 6,15
                DEFW 8
        S1B:    DEFB 1,9,0
                DEFW 0
                DEFB 31,15
                DEFW 200
        S1C:    DEFB 2,9,0
                DEFW 3000
                DEFB 31,15
                DEFW 200
        S2A:    DEFB 1,0,8
                DEFW 300
                DEFB 0,8
                DEFW 60
        S2B:    DEFB 1,9,0
                DEFW 0
                DEFB 15,15
                DEFW 30
        S2C:    DEFB 1,10,0
                DEFW 0
                DEFB 4,15
                DEFW 45
        S3:     DEFB 1,9,0
                DEFW 0
                DEFB 6,15
                DEFW 30
        S4A:    DEFB 1,9,9
                DEFW 180
                DEFB 10,15
                DEFW 15
        S4B:    DEFB 2,9,9
                DEFW 90
                DEFB 0,12
                DEFW 15
        S5A:    DEFB 1,9,10
                DEFW 200
                DEFB 0,13
                DEFW 90
        S5B:    DEFB 2,9,10
                DEFW 203
                DEFB 0,10
                DEFW 90
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_bm_", EffectsRuntime.Resolve);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module BOOM : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
