// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// SHOT son (D215) : tirs et bris, choisis à l'oreille par l'utilisateur (examples\shotref.bas, 23 nov. 2026) :
/// 1 tir de laser, 2 missile, 3 rafale, 4 vitre qui casse. Pas de durée (choix de l'utilisateur). Enveloppes posées juste
/// avant chaque son, numéros pris dans la réserve commune des effets (D218 ; avant : ENV 4 à 8, ENT 6 et 7).
/// </summary>
public static class ShotRuntime
{
    public const string EntryLabel = "rt_sh_SHOT";      // A = son - 1 (0 à 3)

    const string Source = """
                ; ---------- SHOT (D215) : A = son - 1 ----------
        SHOT:   OR A
                JP Z,T1
                DEC A
                JP Z,T2
                DEC A
                JP Z,T3
                CALL NEWENV             ; 4 : vitre qui casse (fracas, puis tintements sur le canal B)
                LD (S4+1),A
                LD HL,EVFRA
                CALL &BCBC              ; SOUND AMPL ENVELOPE
                CALL NEWENV
                LD (S4T+1),A
                LD HL,EVTIN
                CALL &BCBC
                LD HL,S4
                CALL QUEUE
                LD HL,TINTS
                LD B,6
        T4:     PUSH BC
                PUSH HL
                LD E,(HL)
                INC HL
                LD D,(HL)
                LD (S4T+3),DE
                LD HL,S4T
                CALL QUEUE
                POP HL
                INC HL
                INC HL
                POP BC
                DJNZ T4
                RET
        T1:     CALL NEWENV             ; 1 : tir de laser
                LD (S1+1),A
                LD HL,EVLAS
                CALL &BCBC
                CALL NEWENT
                LD (S1+2),A
                LD HL,ETLAS
                CALL &BCBF              ; SOUND TONE ENVELOPE
                LD HL,S1
                JP QUEUE
        T2:     CALL NEWENV             ; 2 : missile (souffle qui enfle, son qui descend)
                LD (S2A+1),A
                LD (S2B+1),A
                LD HL,EVMIS
                CALL &BCBC
                CALL NEWENT
                LD (S2B+2),A
                LD HL,ETMIS
                CALL &BCBF
                LD HL,S2A
                CALL QUEUE
                LD HL,S2B
                JP QUEUE
        T3:     CALL NEWENV             ; 3 : rafale (six tirs secs)
                LD (S3+1),A
                LD HL,EVRAF
                CALL &BCBC
                LD B,6
        T31:    PUSH BC
                LD HL,S3
                CALL QUEUE
                POP BC
                DJNZ T31
                RET
                ; file du canal pleine : on attend une place, comme SOUND
        QUEUE:  PUSH HL
                CALL &BCAA              ; SOUND QUEUE
                POP HL
                JR NC,QUEUE
                RET
                ; enveloppes (16 octets : nombre de parties, puis pas, taille, pause)
        EVLAS:  DEFB 1,15,&FF,2
                DEFS 12
        ETLAS:  DEFB 1,30,9,1
                DEFS 12
        EVMIS:  DEFB 2,10,1,4,15,&FF,4
                DEFS 9
        ETMIS:  DEFB 1,90,3,1
                DEFS 12
        EVRAF:  DEFB 1,7,&FE,1
                DEFS 12
        EVFRA:  DEFB 1,15,&FF,4
                DEFS 12
        EVTIN:  DEFB 1,6,&FE,1
                DEFS 12
                ; sons (9 octets : canal, env. volume, env. ton, période, bruit, volume, durée)
        S1:     DEFB 1,8,7
                DEFW 30
                DEFB 0,15
                DEFW 30
        S2A:    DEFB 1,7,0
                DEFW 0
                DEFB 3,5
                DEFW 100
        S2B:    DEFB 2,7,6
                DEFW 100
                DEFB 0,5
                DEFW 100
        S3:     DEFB 1,6,0
                DEFW 0
                DEFB 5,14
                DEFW 9
        S4:     DEFB 1,5,0
                DEFW 0
                DEFB 1,15
                DEFW 60
        S4T:    DEFB 2,4,0
                DEFW 40
                DEFB 0,12
                DEFW 7
        TINTS:  DEFW 40,55,35,60,45,50
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_sh_", EffectsRuntime.Resolve);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module SHOT : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
