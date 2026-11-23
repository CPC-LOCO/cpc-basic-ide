// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// FALL son (D217) : chutes, choisies à l'oreille par l'utilisateur (examples\fallref.bas, 23 nov. 2026, sons 2 et 4) :
/// 1 chute dans le vide puis le sol, 2 vie perdue. Pas de durée. Enveloppes posées juste avant chaque son : ENV 2 et 3,
/// ENT 4 et 5 (les autres effets ont les leurs, D216).
/// </summary>
public static class FallRuntime
{
    public const string EntryLabel = "rt_fa_FALL";      // A = son - 1 (0 ou 1)

    const string Source = """
                ; ---------- FALL (D217) : A = son - 1 ----------
        FALL:   OR A
                JR Z,F1
                LD A,3                  ; 2 : vie perdue (quatre notes qui descendent, la dernière tremble et s'éteint)
                LD HL,EVFIN
                CALL &BCBC              ; SOUND AMPL ENVELOPE
                LD A,4
                LD HL,ETVIB
                CALL &BCBF              ; SOUND TONE ENVELOPE
                LD HL,S2A
                CALL QUEUE
                LD HL,S2B
                CALL QUEUE
                LD HL,S2C
                CALL QUEUE
                LD HL,S2D
                JR QUEUE
        F1:     LD A,2                  ; 1 : chute dans le vide, puis le sol
                LD HL,EVSOL
                CALL &BCBC
                LD A,5
                LD HL,ETCHU
                CALL &BCBF
                LD HL,S1A
                CALL QUEUE
                LD HL,S1B
                ; file du canal pleine : on attend une place, comme SOUND
        QUEUE:  PUSH HL
                CALL &BCAA              ; SOUND QUEUE
                POP HL
                JR NC,QUEUE
                RET
                ; enveloppes (16 octets : nombre de parties, puis pas, taille, pause)
        EVSOL:  DEFB 1,15,&FF,1
                DEFS 12
        ETCHU:  DEFB 1,100,6,1
                DEFS 12
        EVFIN:  DEFB 1,12,&FF,5
                DEFS 12
        ETVIB:  DEFB &82,2,2,1,2,&FE,1
                DEFS 9
                ; sons (9 octets : canal, env. volume, env. ton, période, bruit, volume, durée)
        S1A:    DEFB 1,0,5
                DEFW 100
                DEFB 0,12
                DEFW 100
        S1B:    DEFB 1,2,0
                DEFW 2000
                DEFB 25,15
                DEFW 15
        S2A:    DEFB 1,0,0
                DEFW 239
                DEFB 0,12
                DEFW 25
        S2B:    DEFB 1,0,0
                DEFW 253
                DEFB 0,12
                DEFW 25
        S2C:    DEFB 1,0,0
                DEFW 268
                DEFB 0,12
                DEFW 25
        S2D:    DEFB 1,3,4
                DEFW 284
                DEFB 0,12
                DEFW 60
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_fa_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module FALL : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
