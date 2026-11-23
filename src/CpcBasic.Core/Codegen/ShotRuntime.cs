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
/// avant chaque son : ENV 4 à 8, ENT 6 et 7 (VROOM et BOOM ont les leurs, D213, D214).
/// </summary>
public static class ShotRuntime
{
    public const string EntryLabel = "rt_sh_SHOT";      // A = son - 1 (0 à 3)

    const string Source = """
                ; ---------- SHOT (D215) : A = son - 1 ----------
        SHOT:   OR A
                JR Z,T1
                DEC A
                JR Z,T2
                DEC A
                JR Z,T3
                LD A,5                  ; 4 : vitre qui casse (fracas, puis tintements sur le canal B)
                LD HL,EVFRA
                CALL &BCBC              ; SOUND AMPL ENVELOPE
                LD A,4
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
        T1:     LD A,8                  ; 1 : tir de laser
                LD HL,EVLAS
                CALL &BCBC
                LD A,7
                LD HL,ETLAS
                CALL &BCBF              ; SOUND TONE ENVELOPE
                LD HL,S1
                JR QUEUE
        T2:     LD A,7                  ; 2 : missile (souffle qui enfle, son qui descend)
                LD HL,EVMIS
                CALL &BCBC
                LD A,6
                LD HL,ETMIS
                CALL &BCBF
                LD HL,S2A
                CALL QUEUE
                LD HL,S2B
                JR QUEUE
        T3:     LD A,6                  ; 3 : rafale (six tirs secs)
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
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_sh_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module SHOT : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
