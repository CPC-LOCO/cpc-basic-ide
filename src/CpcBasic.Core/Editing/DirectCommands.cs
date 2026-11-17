// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Editing;

/// <summary>Ce que fait une commande du mode direct tapée dans l'éditeur (D077, D115).</summary>
public enum DirectCommandKind
{
    New,      // efface le programme
    Edit,     // place le curseur sur une ligne
    Auto,     // numérotation automatique (étape suivante)
    List,     // (étape suivante)
    Renum,    // (étape suivante)
    Delete,   // (étape suivante)
    Explain   // affiche seulement ce que la commande devient en BASIC compilé
}

/// <summary>Une commande reconnue : son nom (en majuscules), le reste de la ligne, et l'explication éventuelle.</summary>
public sealed record DirectCommand(DirectCommandKind Kind, string Name, string Argument, string? Explanation);

/// <summary>
/// Commandes du mode direct du Locomotive BASIC, tapées seules sur une ligne de l'éditeur, sans numéro de ligne,
/// puis validées par Entrée (D077). Une ligne qui commence par un numéro est toujours une ligne du programme.
/// </summary>
public static class DirectCommands
{
    static readonly Dictionary<string, DirectCommandKind> Actions = new()
    {
        ["NEW"] = DirectCommandKind.New,
        ["EDIT"] = DirectCommandKind.Edit,
        ["AUTO"] = DirectCommandKind.Auto,
        ["LIST"] = DirectCommandKind.List,
        ["RENUM"] = DirectCommandKind.Renum,
        ["DELETE"] = DirectCommandKind.Delete
    };

    const string AmsdosText =
        "Les commandes AMSDOS (|DISC, |TAPE, |A, |B, |DIR, |ERA, |REN, |USER) marchent dans un programme, sur une ligne " +
        "avec un numéro. Tapées seules ici, elles n'ont rien à faire : c'est F5 qui crée la disquette.";

    static readonly Dictionary<string, string> Explanations = new()
    {
        ["RUN"] = "RUN : ici, c'est F5 qui compile le programme et crée la disquette ; dans l'émulateur, tapez RUN\"NOM\". " +
                  "Dans un programme : RUN recommence le programme, RUN \"nom\" lance un autre programme compilé.",
        ["CONT"] = "CONT : sur le CPC, il reprend le programme arrêté par STOP ou ESC. Un programme compilé ne se reprend pas " +
                   "ainsi : dans l'émulateur, relancez-le avec RUN\"NOM\".",
        ["CLEAR"] = "CLEAR : dans un programme, il efface toutes les variables (nombres à 0, chaînes vides) et les tableaux. " +
                    "Ici, le programme n'est pas lancé : il n'y a pas de variables à effacer.",
        ["MEMORY"] = "MEMORY : dans un programme, MEMORY adresse protège la mémoire au-dessus de l'adresse (code machine, écran...). " +
                     "Le compilateur vérifie que le programme tient en dessous.",
        ["LOAD"] = "LOAD : pour ouvrir un programme dans l'éditeur, faites Ctrl+O (menu Fichier). " +
                   "Dans un programme : LOAD \"nom\",adresse charge un fichier binaire (un écran...).",
        ["SAVE"] = "SAVE : pour enregistrer le programme, faites Ctrl+S (menu Fichier). " +
                   "Dans un programme : SAVE \"nom\",B,début,longueur enregistre une zone de la mémoire.",
        ["MERGE"] = "MERGE : il ajoute les lignes d'un autre programme BASIC, ce qui est impossible pour un programme compilé. " +
                    "Ici, copiez et collez les lignes (Ctrl+C, Ctrl+V).",
        ["CAT"] = "CAT : il affiche le contenu de la disquette ; dans un programme, CAT marche aussi. " +
                  "Pour voir les programmes du dossier sur le PC : Ctrl+O.",
        ["CHAIN"] = "CHAIN : il n'existe pas pour un programme compilé (les variables ne passent pas d'un programme à l'autre). " +
                    "Utilisez RUN \"nom\", et un fichier pour garder des valeurs.",
        ["|DISC"] = AmsdosText, ["|TAPE"] = AmsdosText, ["|A"] = AmsdosText, ["|B"] = AmsdosText,
        ["|DIR"] = AmsdosText, ["|ERA"] = AmsdosText, ["|REN"] = AmsdosText, ["|USER"] = AmsdosText
    };

    /// <summary>
    /// Reconnaît une commande du mode direct sur une ligne de l'éditeur ; null si la ligne n'en est pas une
    /// (ligne vide, ligne qui commence par un numéro, autre mot).
    /// </summary>
    public static DirectCommand? Recognize(string line)
    {
        string text = line.Trim();
        if (text.Length == 0 || char.IsDigit(text[0]))
            return null;

        int start = text[0] == '|' ? 1 : 0;
        int end = start;
        while (end < text.Length && char.IsAsciiLetter(text[end]))
            end++;
        if (end == start)
            return null;
        // Le mot doit être entier : NEWS ou LIST$ ne sont pas des commandes
        if (end < text.Length && (char.IsAsciiLetterOrDigit(text[end]) || text[end] is '$' or '%' or '!' or '.'))
            return null;

        string name = text[..end].ToUpperInvariant();
        string argument = text[end..].Trim();

        if (Actions.TryGetValue(name, out var kind))
            return new DirectCommand(kind, name, argument, null);
        if (Explanations.TryGetValue(name, out var explanation))
            return new DirectCommand(DirectCommandKind.Explain, name, argument, explanation);
        return null;
    }

    /// <summary>
    /// Valeurs de AUTO [début][,pas] (D116) : 10 et 10 si elles manquent, comme le CPC. Faux si elles ne conviennent pas
    /// (numéro hors de 1 à 65535, pas nul, autre chose que des nombres).
    /// </summary>
    public static bool TryParseAuto(string argument, out int start, out int step)
    {
        start = 10;
        step = 10;
        string[] parts = argument.Split(',');
        if (parts.Length > 2)
            return false;
        if (parts[0].Trim().Length > 0 && !TryLineNumber(parts[0], out start))
            return false;
        if (parts.Length == 2 && !TryLineNumber(parts[1], out step))
            return false;
        return true;
    }

    static bool TryLineNumber(string text, out int value) =>
        int.TryParse(text.Trim(), System.Globalization.NumberStyles.None, null, out value) && value is >= 1 and <= 65535;

    /// <summary>
    /// Plage de DELETE (et plus tard LIST), comme le CPC (D120) : rien = tout ; 30 ; 10-50 ; -50 (du début à 50) ;
    /// 100- (de 100 à la fin). Faux si elle ne convient pas.
    /// </summary>
    public static bool TryParseRange(string argument, out int from, out int to)
    {
        from = 1;
        to = 65535;
        string a = argument.Trim();
        if (a.Length == 0)
            return true;
        int dash = a.IndexOf('-');
        if (dash < 0)
        {
            if (!TryLineNumber(a, out from))
                return false;
            to = from;
            return true;
        }
        string left = a[..dash].Trim(), right = a[(dash + 1)..].Trim();
        if (left.Length == 0 && right.Length == 0)
            return false;
        if (left.Length > 0 && !TryLineNumber(left, out from))
            return false;
        if (right.Length > 0 && !TryLineNumber(right, out to))
            return false;
        return from <= to;
    }

    /// <summary>Lignes de l'éditeur (comptées à partir de 1) dont le numéro BASIC est dans la plage, dans l'ordre.</summary>
    public static List<int> LinesInRange(string text, int from, int to)
    {
        var result = new List<int>();
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            if (LeadingNumber(lines[i], out _) is int n && n >= from && n <= to)
                result.Add(i + 1);
        return result;
    }

    /// <summary>
    /// Numéro BASIC au début d'une ligne de l'éditeur (D118) ; null s'il n'y en a pas (ou plus de 65535).
    /// numberOnly : rien d'autre que le numéro sur la ligne (sur le CPC, cela efface la ligne).
    /// </summary>
    public static int? LeadingNumber(string line, out bool numberOnly)
    {
        string s = line.Trim();
        int n = 0;
        while (n < s.Length && char.IsAsciiDigit(s[n]))
            n++;
        numberOnly = n > 0 && n == s.Length;
        if (n == 0 || n > 5)
            return null;
        int value = int.Parse(s[..n]);
        return value is >= 1 and <= 65535 ? value : null;
    }

    /// <summary>
    /// Ligne de l'éditeur (comptée à partir de 1) qui porte le numéro BASIC demandé ; null si aucune ligne ne l'a.
    /// </summary>
    public static int? FindLine(string text, int number, int exceptLine = 0)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i + 1 == exceptLine)
                continue;
            string s = lines[i].TrimStart();
            int n = 0;
            while (n < s.Length && char.IsDigit(s[n]))
                n++;
            if (n > 0 && n <= 5 && int.Parse(s[..n]) == number)
                return i + 1;
        }
        return null;
    }
}
