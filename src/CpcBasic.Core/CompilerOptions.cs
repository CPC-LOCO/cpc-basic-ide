// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Libraries;

namespace CpcBasic.Core;

/// <summary>Machine pour laquelle le programme est compilé (D066).</summary>
public enum TargetMachine
{
    /// <summary>Tous les CPC : le programme reconnaît la machine au démarrage (si besoin). Choix par défaut.</summary>
    AllCpc,
    /// <summary>CPC 464 seulement (BASIC 1.0) : adresses du 464 écrites directement.</summary>
    Cpc464,
    /// <summary>CPC 664 et 6128 seulement (BASIC 1.1) : adresses du 664 / 6128 écrites directement.</summary>
    Cpc664And6128
}

/// <summary>
/// Réglages de la compilation, choisis dans l'éditeur (D066).
/// RealByDefault : une variable sans signe (A) est un nombre à virgule, comme le Locomotive ; sinon un entier (D026).
/// BreakKey : touche ESC comme le vrai BASIC (pause, puis « Break in N ») ; sinon ignorée, sauf ON BREAK (D083).
/// L'éditeur la met par défaut (F9) ; ici, faux par défaut pour garder les programmes de test tels quels.
/// Language : version du CPC, qui décide des codes des lettres accentuées dans les textes (D126) ; français par défaut.
/// Debug : réglage « Mise au point » (F2, D136) : les DEBUG du programme affichent les variables ; sinon ils sont ignorés.
/// PreferSize : réglage « Priorité : place » (F3, D141) : quand deux commandes peuvent partager une routine au prix de la
/// vitesse, on partage (le dessin reste exactement le même) ; sinon (par défaut) chaque commande garde sa routine rapide.
/// Aujourd'hui : CIRCLE passe par ELLIPSE quand le programme contient les deux.
/// Libraries : catalogue des bibliothèques (D148) ; null : aucune bibliothèque.
/// WithLoader : vrai (par défaut) : NOM.BAS charge NOM.BIN. Faux (D168) : NOM.BIN seul, lancé directement par RUN"NOM" ;
/// le firmware a été remis à zéro, le programme rebranche donc lui-même la disquette (AMSDOS) au départ.
/// </summary>
public sealed record CompilerOptions(TargetMachine Target = TargetMachine.AllCpc, bool RealByDefault = false,
                                     bool BreakKey = false, CpcLanguage Language = CpcLanguage.French, bool Debug = false,
                                     bool PreferSize = false, LibraryCatalog? Libraries = null,
                                     bool WithLoader = true)
{
    /// <summary>Les réglages par défaut : tous les CPC, variables sans signe entières.</summary>
    public static CompilerOptions Default { get; } = new();

    /// <summary>Nom du réglage de machine, tel qu'il est montré dans l'éditeur.</summary>
    public static string MachineName(TargetMachine target) => target switch
    {
        TargetMachine.Cpc464 => "CPC 464",
        TargetMachine.Cpc664And6128 => "CPC 664/6128",
        _ => "Tous les CPC"
    };
}
