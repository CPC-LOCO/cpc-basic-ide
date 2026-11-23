// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// VROOM son (D213) : bruits de moteur, choisis à l'oreille par l'utilisateur (examples\vroomref.bas, 23 nov. 2026) :
/// 1 moteur qui monte, 2 ralenti, 3 moteur qui descend (en 3 morceaux de plus en plus faibles). Sur le canal A, en
/// arrière-plan comme SOUND. Les enveloppes sont posées juste avant chaque son, avec des numéros pris dans la réserve
/// commune des effets (D218 ; avant : ENV 12 à 15 et ENT 11 à 15).
/// </summary>
public static class VroomRuntime
{
    public const string EntryLabel = "rt_vr_VROOM";     // A = son - 1 (0, 1 ou 2)

    const string Source = """
                ; ---------- VROOM (D213) : A = 0 monte, 1 ralenti, 2 descend ----------
        VROOM:  OR A
                JP Z,V1
                DEC A
                JP Z,V2
                CALL NEWENV             ; 3 : moteur qui descend, en 3 morceaux (volume 12, 8, 4)
                LD (SD1+1),A
                LD HL,EV12
                CALL &BCBC              ; SOUND AMPL ENVELOPE
                CALL NEWENV
                LD (SD2+1),A
                LD HL,EV8
                CALL &BCBC
                CALL NEWENV
                LD (SD3+1),A
                LD HL,EV4
                CALL &BCBC
                CALL NEWENT
                LD (SD1+2),A
                LD HL,ET33
                CALL &BCBF              ; SOUND TONE ENVELOPE
                CALL NEWENT
                LD (SD2+2),A
                LD HL,ET33
                CALL &BCBF
                CALL NEWENT
                LD (SD3+2),A
                LD HL,ET34
                CALL &BCBF
                LD HL,SD1
                CALL QUEUE
                LD HL,SD2
                CALL QUEUE
                LD HL,SD3
                JP QUEUE
        V1:     CALL NEWENV             ; 1 : moteur qui monte
                LD (SUP+1),A
                LD HL,EV12
                CALL &BCBC
                CALL NEWENT
                LD (SUP+2),A
                LD HL,ETUP
                CALL &BCBF
                LD HL,SUP
                JP QUEUE
        V2:     CALL NEWENV             ; 2 : ralenti
                LD (SRAL+1),A
                LD HL,EVRAL
                CALL &BCBC
                CALL NEWENT
                LD (SRAL+2),A
                LD HL,ETRAL
                CALL &BCBF
                LD HL,SRAL
                ; file du canal pleine : on attend une place, comme SOUND
        QUEUE:  PUSH HL
                CALL &BCAA              ; SOUND QUEUE
                POP HL
                JR NC,QUEUE
                RET
                ; enveloppes (16 octets : nombre de parties, puis pas, taille, pause)
        EV12:   DEFB 2,2,&FA,2,2,6,2
                DEFS 9
        EV8:    DEFB 2,2,&FC,2,2,4,2
                DEFS 9
        EV4:    DEFB 2,2,&FE,2,2,2,2
                DEFS 9
        EVRAL:  DEFB 2,3,&FC,4,3,4,4
                DEFS 9
        ETUP:   DEFB 1,100,&FB,1
                DEFS 12
        ETRAL:  DEFB &82,5,8,1,5,&F8,1
                DEFS 9
        ET33:   DEFB 1,33,5,1
                DEFS 12
        ET34:   DEFB 1,34,5,1
                DEFS 12
                ; sons (9 octets : canal, env. volume, env. ton, période, bruit, volume, durée ; durée < 0 : enveloppe répétée)
        SUP:    DEFB 1,15,15
                DEFW 900
                DEFB 25,12
                DEFW &FFF3
        SRAL:   DEFB 1,14,14
                DEFW 900
                DEFB 25,12
                DEFW &FFFA
        SD1:    DEFB 1,15,13
                DEFW 400
                DEFB 25,12
                DEFW &FFFC
        SD2:    DEFB 1,13,12
                DEFW 565
                DEFB 25,8
                DEFW &FFFC
        SD3:    DEFB 1,12,11
                DEFW 730
                DEFB 25,4
                DEFW &FFFB
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_vr_", EffectsRuntime.Resolve);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module VROOM : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
