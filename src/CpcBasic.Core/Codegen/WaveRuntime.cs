// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// WAVE son [, envers] (D219) : vagues et plouf, choisis à l'oreille par l'utilisateur (examples\waveref.bas,
/// 23 nov. 2026) : 1 petite vague, 2 vague moyenne, 3 grosse vague qui se brise, 4 gros plouf ; envers = 1 : le même son
/// joué de la fin vers le début. Pour une île : FOR A=1 TO 100:WAVE 1+INT(RND*3):NEXT. Numéros d'enveloppes pris dans
/// la réserve commune des effets (D218).
/// </summary>
public static class WaveRuntime
{
    public const string EntryLabel = "rt_wv_WAVE";      // A = son - 1, + 4 à l'envers (0 à 7)

    const string Source = """
                ; ---------- WAVE (D219) : A = son - 1 (+ 4 : à l'envers) ----------
        WAVE:   ADD A,A
                LD E,A
                LD D,0
                LD HL,WTAB
                ADD HL,DE
                LD A,(HL)
                INC HL
                LD H,(HL)
                LD L,A
                JP (HL)
        WTAB:   DEFW W1,W2,W3,W4,BK1,BK2,BK3,BK4
        W1:     CALL NEWENV              ; petite vague
                LD (W1A+1),A
                LD HL,EW1A
                CALL &BCBC
                LD HL,W1A
                CALL QUEUE
                RET
        W2:     CALL NEWENV              ; vague moyenne
                LD (W2A+1),A
                LD HL,EW2A
                CALL &BCBC
                LD HL,W2A
                CALL QUEUE
                RET
        W3:     CALL NEWENV              ; grosse vague qui se brise
                LD (W3A+1),A
                LD HL,EW3A
                CALL &BCBC
                LD HL,W3A
                CALL QUEUE
                CALL NEWENV
                LD (W3B+1),A
                LD HL,EW3B
                CALL &BCBC
                LD HL,W3B
                CALL QUEUE
                RET
        W4:     CALL NEWENV              ; gros plouf
                LD (W4A+1),A
                LD HL,EW4A
                CALL &BCBC
                CALL NEWENT
                LD (W4A+2),A
                LD HL,TW4A
                CALL &BCBF
                LD HL,W4A
                CALL QUEUE
                CALL NEWENV
                LD (W4B+1),A
                LD HL,EW4B
                CALL &BCBC
                LD HL,W4B
                CALL QUEUE
                RET
        BK1:     CALL NEWENV              ; petite vague à l'envers
                LD (BK1A+1),A
                LD HL,EBK1A
                CALL &BCBC
                LD HL,BK1A
                CALL QUEUE
                RET
        BK2:     CALL NEWENV              ; vague moyenne à l'envers
                LD (BK2A+1),A
                LD HL,EBK2A
                CALL &BCBC
                LD HL,BK2A
                CALL QUEUE
                RET
        BK3:     CALL NEWENV              ; grosse vague à l'envers
                LD (BK3A+1),A
                LD HL,EBK3A
                CALL &BCBC
                LD HL,BK3A
                CALL QUEUE
                CALL NEWENV
                LD (BK3B+1),A
                LD HL,EBK3B
                CALL &BCBC
                LD HL,BK3B
                CALL QUEUE
                RET
        BK4:     CALL NEWENV              ; gros plouf à l'envers
                LD (BK4A+1),A
                LD HL,EBK4A
                CALL &BCBC
                LD HL,BK4A
                CALL QUEUE
                LD HL,BK4B
                CALL QUEUE
                CALL NEWENV
                LD (BK4C+1),A
                LD HL,EBK4C
                CALL &BCBC
                CALL NEWENT
                LD (BK4C+2),A
                LD HL,TBK4C
                CALL &BCBF
                LD HL,BK4C
                CALL QUEUE
                RET
        EW1A:   DEFB 2,8,&01,4,12,&FF,6
                DEFS 9
        W1A:    DEFB 1,0,0
                DEFW 0
                DEFB 12,4
                DEFW 104
        EW2A:   DEFB 2,10,&01,5,15,&FF,8
                DEFS 9
        W2A:    DEFB 1,0,0
                DEFW 0
                DEFB 18,5
                DEFW 170
        EW3A:   DEFB 1,12,&01,4
                DEFS 12
        W3A:    DEFB 1,0,0
                DEFW 0
                DEFB 28,3
                DEFW 48
        EW3B:   DEFB 1,15,&FF,7
                DEFS 12
        W3B:    DEFB 1,0,0
                DEFW 0
                DEFB 16,15
                DEFW 105
        EW4A:   DEFB 1,7,&FE,2
                DEFS 12
        TW4A:   DEFB 1,15,&10,1
                DEFS 12
        W4A:    DEFB 1,0,0
                DEFW 60
                DEFB 0,14
                DEFW 15
        EW4B:   DEFB 1,15,&FF,3
                DEFS 12
        W4B:    DEFB 2,0,0
                DEFW 0
                DEFB 4,15
                DEFW 45
        EBK1A:   DEFB 2,12,&01,6,8,&FF,4
                DEFS 9
        BK1A:    DEFB 1,0,0
                DEFW 0
                DEFB 12,0
                DEFW 104
        EBK2A:   DEFB 2,15,&01,8,10,&FF,5
                DEFS 9
        BK2A:    DEFB 1,0,0
                DEFW 0
                DEFB 18,0
                DEFW 170
        EBK3A:   DEFB 1,15,&01,7
                DEFS 12
        BK3A:    DEFB 1,0,0
                DEFW 0
                DEFB 16,0
                DEFW 105
        EBK3B:   DEFB 1,12,&FF,4
                DEFS 12
        BK3B:    DEFB 1,0,0
                DEFW 0
                DEFB 28,15
                DEFW 48
        EBK4A:   DEFB 1,15,&01,3
                DEFS 12
        BK4A:    DEFB 2,0,0
                DEFW 0
                DEFB 4,0
                DEFW 45
        BK4B:    DEFB 1,0,0
                DEFW 0
                DEFB 0,0
                DEFW 30
        EBK4C:   DEFB 1,7,&02,2
                DEFS 12
        TBK4C:   DEFB 1,15,&F0,1
                DEFS 12
        BK4C:    DEFB 1,0,0
                DEFW 300
                DEFB 0,0
                DEFW 15
                ; file du canal pleine : on attend une place, comme SOUND
        QUEUE:  PUSH HL
                CALL &BCAA              ; SOUND QUEUE
                POP HL
                JR NC,QUEUE
                RET
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_wv_", EffectsRuntime.Resolve);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module WAVE : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
