// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// PAUSE [heure,][minute,] seconde et CLIC code (D222, idées de l'utilisateur notées en D049).
/// PAUSE : le compilateur range heures, minutes et secondes (rt_pz_PH, PM, PS), appelle START, puis appelle STEP
/// en boucle jusqu'à la retenue (entre deux appels : les minuteries EVERY / AFTER et la touche ESC, si le programme
/// s'en sert). Le temps vient de KL TIME PLEASE (1/300 de seconde, aussi sur la cartouche) ; chaque seconde est
/// comptée à partir du départ, sans dérive.
/// CLIC : le compilateur range le code (rt_pz_PK), appelle KSTART (touches déjà tapées oubliées), puis KEY en boucle
/// jusqu'à la retenue (KM READ CHAR, qui n'attend pas).
/// </summary>
public static class PauseRuntime
{
    public const string StartLabel = "rt_pz_START";
    public const string StepLabel = "rt_pz_STEP";       // retenue : fini
    public const string KeyStartLabel = "rt_pz_KSTART";
    public const string KeyLabel = "rt_pz_KEY";         // retenue : la touche attendue est arrivée
    public const string HoursLabel = "rt_pz_PH";
    public const string MinutesLabel = "rt_pz_PM";
    public const string SecondsLabel = "rt_pz_PS";
    public const string CodeLabel = "rt_pz_PK";

    const string Source = """
                ; ---------- PAUSE (D222) : nombre de secondes = (PH * 60 + PM) * 60 + PS, sur 32 bits ----------
        START:  LD HL,(PH)
                LD DE,0
                CALL M60
                LD BC,(PM)
                ADD HL,BC
                JR NC,S1
                INC DE
        S1:     CALL M60
                LD BC,(PS)
                ADD HL,BC
                JR NC,S2
                INC DE
        S2:     LD (PN),HL
                LD (PN+2),DE
                CALL &BD0D              ; KL TIME PLEASE : DEHL = maintenant
                LD (PT),HL
                LD (PT+2),DE
                RET
                ; DEHL = DEHL * 60 (pas d'EXX : les registres de l'autre jeu sont au firmware)
        M60:    LD (PX),HL
                LD (PX+2),DE
                LD HL,0
                LD DE,0
                LD B,60
        M1:     PUSH BC
                LD BC,(PX)
                ADD HL,BC
                EX DE,HL
                LD BC,(PX+2)
                ADC HL,BC
                EX DE,HL
                POP BC
                DJNZ M1
                RET
                ; un passage : retenue quand toutes les secondes sont passées
        STEP:   LD HL,(PN)
                LD DE,(PN+2)
                LD A,H
                OR L
                OR D
                OR E
                SCF
                RET Z                   ; plus de secondes : fini
                CALL &BD0D              ; DEHL = maintenant
                LD BC,(PT)              ; maintenant - départ de la seconde en cours
                OR A
                SBC HL,BC
                EX DE,HL
                LD BC,(PT+2)
                SBC HL,BC
                EX DE,HL                ; DEHL = écart
                LD BC,300               ; écart - 300 (une seconde)
                OR A
                SBC HL,BC
                EX DE,HL
                LD BC,0
                SBC HL,BC
                BIT 7,H
                JR Z,SEC
                OR A                    ; pas encore
                RET
        SEC:    LD HL,(PT)              ; une seconde de plus : départ + 300, une seconde de moins
                LD BC,300
                ADD HL,BC
                LD (PT),HL
                JR NC,SEC1
                LD HL,(PT+2)
                INC HL
                LD (PT+2),HL
        SEC1:   LD HL,(PN)
                LD BC,1
                OR A
                SBC HL,BC
                LD (PN),HL
                JR NC,STEP
                LD HL,(PN+2)
                DEC HL
                LD (PN+2),HL
                JR STEP
                ; ---------- CLIC (D222) ----------
        KSTART: CALL &BB09              ; KM READ CHAR : les touches déjà tapées sont oubliées
                JR C,KSTART
                RET
        KEY:    CALL &BB09
                RET NC                  ; pas de touche
                LD HL,PK
                CP (HL)
                SCF
                RET Z                   ; la touche attendue
                OR A
                RET
        PH:     DEFW 0
        PM:     DEFW 0
        PS:     DEFW 0
        PK:     DEFB 0
        PN:     DEFS 4
        PT:     DEFS 4
        PX:     DEFS 4
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_pz_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module PAUSE : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
