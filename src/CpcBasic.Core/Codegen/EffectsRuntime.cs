// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// D218 : réserve commune des numéros d'enveloppes pour les effets sonores (VROOM, BOOM, SHOT, FALL, WAVE). Le compilateur
/// y range, au départ du programme, les numéros que le programme n'écrit pas lui-même dans ses ENV et ENT (du plus grand
/// au plus petit) ; chaque enveloppe d'un effet prend le numéro suivant, chacun son tour. Les numéros du programme ne
/// sont jamais touchés, et un son encore joué ou en attente garde son enveloppe tant que la réserve n'a pas fait le tour.
/// </summary>
public static class EffectsRuntime
{
    public const string PoolsLabel = "rt_fx_ENVP";      // 16 octets ENV (nombre, puis numéros), puis 16 octets ENT
    public const int PoolsSize = 32;

    /// <summary>Résout les appels des modules des effets vers cette réserve (NEWENV, NEWENT).</summary>
    public static string? Resolve(string name) => name is "NEWENV" or "NEWENT" ? "rt_fx_" + name : null;

    const string Source = """
                ; ---------- réserve des numéros d'enveloppes des effets (D218) : A = numéro suivant ; BC, DE, HL gardés ----------
        NEWENV: PUSH HL
                PUSH DE
                LD HL,ENVP
                LD DE,ENVI
                JR NEW1
        NEWENT: PUSH HL
                PUSH DE
                LD HL,ENTP
                LD DE,ENTI
        NEW1:   LD A,(DE)               ; rang du prochain numéro
                CP (HL)
                JR C,NEW2
                XOR A                   ; au bout de la liste : on recommence
        NEW2:   INC A
                LD (DE),A
                ADD A,L                 ; HL + rang + 1 : le numéro
                LD L,A
                JR NC,NEW3
                INC H
        NEW3:   LD A,(HL)
                POP DE
                POP HL
                RET
        ENVI:   DEFS 1
        ENTI:   DEFS 1
        ENVP:   DEFS 16
        ENTP:   DEFS 16
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_fx_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module des effets : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }
}
