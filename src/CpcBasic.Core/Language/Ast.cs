// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

namespace CpcBasic.Core.Language;

/// <summary>Types de variables de J2 (décisions D026 et D033).</summary>
public enum VarType
{
    /// <summary>Entier signé 16 bits : A ou A%. Type par défaut, sans déclaration.</summary>
    Integer,
    /// <summary>Octet non signé 8 bits : variable déclarée avec BYTE.</summary>
    Byte,
    /// <summary>Texte : A$ (lot E, D062). Fiche de 3 octets comme le Locomotive : longueur, puis adresse des lettres.</summary>
    String,
    /// <summary>Nombre à virgule : A! (lot F1b, D068). 5 octets comme le Locomotive.</summary>
    Real
}

public static class VariableNames
{
    /// <summary>Tous les types existent depuis le lot F3 (tableaux de nombres à virgule, D070).</summary>
    public static bool IsSupported(string name) => name.Length > 0;

    /// <summary>Vrai pour une variable nombre à virgule (A!).</summary>
    public static bool IsReal(string name) => name[^1] == '!';

    /// <summary>Vrai pour une variable texte (A$).</summary>
    public static bool IsString(string name) => name[^1] == '$';

    /// <summary>Vrai si le nom se termine par un suffixe de type ($, % ou !).</summary>
    public static bool HasSuffix(string name) => name[^1] is '$' or '%' or '!';
}

/// <summary>Une instruction du programme. SourceLine est la ligne de l'éditeur (à partir de 1).</summary>
public abstract record Statement(int SourceLine);

/// <summary>PRINT "texte" : affiche un texte. NewLine est faux si l'instruction se termine par « ; ».</summary>
public sealed record PrintStatement(int SourceLine, string Text, bool NewLine) : Statement(SourceLine);

/// <summary>
/// PRINT expression : affiche un nombre. NewLine est faux s'il est suivi de « ; ».
/// NoLeadingSpace (WRITE, lot H2, D073) : Value est STR$(nombre), affiché sans l'espace du début.
/// </summary>
public sealed record PrintValueStatement(int SourceLine, Expression Value, bool NewLine, bool NoLeadingSpace = false)
    : Statement(SourceLine);

/// <summary>« , » dans PRINT : va au début de la zone suivante, ou à la ligne si elle ne tient pas (lot H2, D073).</summary>
public sealed record PrintZoneStatement(int SourceLine) : Statement(SourceLine);

/// <summary>TAB(colonne) ou SPC(nombre d'espaces) dans PRINT (lot H2, D073).</summary>
public sealed record PrintTabStatement(int SourceLine, Expression Value, bool Spc) : Statement(SourceLine);

/// <summary>
/// Emplacement de nombre de PRINT USING (lot H3, D074). Width : nombre de caractères de l'emplacement ; Before : places
/// avant le point (# , ** $$) ; Decimals : # après le point ; Flags : voir UsingFlags.
/// </summary>
public sealed record PrintUsingNumberStatement(int SourceLine, Expression Value, int Width, int Before, int Decimals, int Flags)
    : Statement(SourceLine);

/// <summary>Emplacement de texte de PRINT USING (D074) : Width 1 pour « ! », n pour « \   \ », 0 pour « & » (tout le texte).</summary>
public sealed record PrintUsingStringStatement(int SourceLine, Expression Value, int Width) : Statement(SourceLine);

/// <summary>Indicateurs d'un emplacement de nombre de PRINT USING (D074), lus aussi par le module PrintUsing.</summary>
public static class UsingFlags
{
    public const int PlusLead = 1;      // « + » devant : signe toujours écrit, collé au nombre
    public const int TrailMinus = 2;    // « - » derrière : « - » ou espace après le nombre
    public const int TrailPlus = 4;     // « + » derrière : « + » ou « - » après le nombre
    public const int Asterisk = 8;      // « ** » : espaces de gauche remplacés par des étoiles
    public const int Dollar = 16;       // « $$ » : « $ » collé au nombre
    public const int Comma = 32;        // « , » avant le point : chiffres groupés par trois
    public const int Exponent = 64;     // « ^^^^ » : écriture avec exposant (E+05)
    public const int Point = 128;       // « . » : point décimal écrit
}

/// <summary>A = expression (avec ou sans LET). Variable : nom en majuscules, suffixe compris.</summary>
public sealed record AssignStatement(int SourceLine, string Variable, Expression Value) : Statement(SourceLine);

/// <summary>BYTE a, b : déclare des variables octet (0 à 255). Ne produit aucun code.</summary>
public sealed record DeclareStatement(int SourceLine, IReadOnlyList<string> Names) : Statement(SourceLine);

/// <summary>
/// FOR variable = départ TO limite [STEP pas]. La limite et le pas sont des nombres écrits dans
/// le programme (décision D033). Le corps de la boucle se termine au NEXT correspondant.
/// Variable à virgule (FOR X!, D068) : RealLimit et RealStep donnent la limite et le pas ; Step vaut alors 1 ou -1.
/// </summary>
/// <remarks>
/// Limite et pas écrits en nombres : Limit, Step (RealLimit, RealStep pour FOR X!). Calculés (FOR I = 1 TO N, lot A,
/// D078) : LimitValue, StepValue ; ils sont calculés une seule fois, au début de la boucle, comme le Locomotive.
/// </remarks>
public sealed record ForStatement(int SourceLine, string Variable, Expression Start, int Limit, int Step,
                                  double RealLimit = 0, double RealStep = 0,
                                  Expression? LimitValue = null, Expression? StepValue = null)
    : Statement(SourceLine);

/// <summary>NEXT [variable] : referme la boucle FOR ouverte la plus récente.</summary>
public sealed record NextStatement(int SourceLine, string? Variable) : Statement(SourceLine);

/// <summary>Types de boucles.</summary>
public enum LoopKind { For, While, Repeat, Do }

/// <summary>WHILE condition ... WEND : test en haut, la boucle continue tant que la condition est vraie.</summary>
public sealed record WhileStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>WEND : referme le WHILE ouvert.</summary>
public sealed record WendStatement(int SourceLine) : Statement(SourceLine);

/// <summary>REPEAT ... UNTIL condition : test en bas, la boucle s'arrête quand la condition devient vraie.</summary>
public sealed record RepeatStatement(int SourceLine) : Statement(SourceLine);

public sealed record UntilStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>
/// DO [WHILE c | UNTIL c] ... LOOP [WHILE c | UNTIL c]. Une seule des deux condition est permise.
/// UntilForm est vrai pour UNTIL, faux pour WHILE. Condition est null s'il n'y en a pas.
/// </summary>
public sealed record DoStatement(int SourceLine, Condition? Condition, bool UntilForm) : Statement(SourceLine);

public sealed record LoopStatement(int SourceLine, Condition? Condition, bool UntilForm) : Statement(SourceLine);

/// <summary>EXIT [FOR] : sort de la boucle. Kind est null pour « EXIT » seul (boucle la plus proche).</summary>
public sealed record ExitStatement(int SourceLine, LoopKind? Kind) : Statement(SourceLine);

/// <summary>CONTINUE [FOR] : passe à l'itération suivante. Kind est null pour « CONTINUE » seul.</summary>
public sealed record ContinueStatement(int SourceLine, LoopKind? Kind) : Statement(SourceLine);

/// <summary>Comparaisons possibles dans une condition.</summary>
public enum CompareOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }

/// <summary>Une condition : une comparaison, ou plusieurs reliées par AND / OR (AND passe avant OR).</summary>
public abstract record Condition;

public sealed record ComparisonCondition(CompareOperator Operator, Expression Left, Expression Right) : Condition;

public sealed record AndCondition(Condition Left, Condition Right) : Condition;

public sealed record OrCondition(Condition Left, Condition Right) : Condition;

/// <summary>
/// IF condition THEN ... : début d'un test. Sur une ligne (IF c THEN a ELSE b), le parser
/// ajoute lui-même le EndIfStatement à la fin de la ligne ; en forme bloc, il vient de END IF.
/// </summary>
public sealed record IfStatement(int SourceLine, Condition Condition) : Statement(SourceLine);

/// <summary>ELSE : début de la partie « sinon » du IF ouvert.</summary>
public sealed record ElseStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Fin du IF ouvert (END IF, ENDIF, ou fin de ligne pour un IF sur une ligne).</summary>
public sealed record EndIfStatement(int SourceLine) : Statement(SourceLine);

/// <summary>
/// RANDOMIZE [graine] : fixe le départ des nombres aléatoires (la même graine redonne la même suite).
/// Sans graine (Seed est null), le départ vient de l'horloge du CPC.
/// </summary>
public sealed record RandomizeStatement(int SourceLine, Expression? Seed) : Statement(SourceLine);

/// <summary>
/// Commandes d'écran : MODE, CLS, LOCATE, PEN, PAPER, INK, BORDER (décision D046), puis CLG, ORIGIN (D047) et BOX (D048).
/// Command est en majuscules ; Arguments sont les valeurs, dans l'ordre où elles sont écrites.
/// </summary>
public sealed record ScreenStatement(int SourceLine, string Command, IReadOnlyList<Expression> Arguments)
    : Statement(SourceLine);

/// <summary>
/// Choix de la fenêtre (flux #0 à #7) pour la commande qui suit (lot H1, D072). PRINT #1, "A" devient :
/// StreamStatement(1), PRINT "A", StreamStatement(null). Stream null : retour à la fenêtre #0, comme le Locomotive
/// après chaque commande.
/// </summary>
public sealed record StreamStatement(int SourceLine, Expression? Stream) : Statement(SourceLine);

/// <summary>
/// Une partie d'enveloppe (lot G1, D071) : nombre de pas, taille d'un pas, pause ; ou, avec « = » (Absolute),
/// ENV : forme et période de l'enveloppe matérielle, ENT : période du son et pause (C vaut alors null).
/// </summary>
public sealed record EnvelopeSection(bool Absolute, Expression A, Expression B, Expression? C);

/// <summary>ENV numéro, parties (volume) ou ENT numéro, parties (ton ; numéro négatif : l'enveloppe se répète).</summary>
public sealed record EnvelopeStatement(int SourceLine, bool Tone, Expression Number, IReadOnlyList<EnvelopeSection> Sections)
    : Statement(SourceLine);

/// <summary>
/// Tracé du Locomotive BASIC : MOVE, MOVER, PLOT, PLOTR, DRAW, DRAWR (décision D047).
/// X et Y sont des coordonnées (ou des écarts pour les commandes en « R »). Pen (le stylo) et Mode
/// (0 normal, 1 XOR, 2 AND, 3 OR) sont facultatifs : null s'ils sont omis.
/// </summary>
public sealed record DrawStatement(int SourceLine, string Command, Expression X, Expression Y,
                                   Expression? Pen, Expression? Mode) : Statement(SourceLine);

/// <summary>END : fin du programme.</summary>
public sealed record EndStatement(int SourceLine) : Statement(SourceLine);

/// <summary>Une expression entière. Son résultat est toujours calculé sur 16 bits.</summary>
public abstract record Expression;

public sealed record NumberExpression(int Value) : Expression;

/// <summary>3.14, .5, 1E5 ou 40000 : un nombre à virgule écrit dans le programme (lot F1b, D068).</summary>
public sealed record RealNumberExpression(double Value) : Expression;

public sealed record VariableExpression(string Name) : Expression;

/// <summary>
/// Appel d'une fonction : ABS, SGN, MIN, MAX, HIBYTE, LOBYTE, CLAMP. Function est en majuscules.
/// MIN et MAX avec plus de deux valeurs sont découpés par le parser en appels à deux valeurs.
/// </summary>
public sealed record CallExpression(string Function, IReadOnlyList<Expression> Arguments) : Expression;

public enum BinaryOperator
{
    Add, Subtract, Multiply, Divide, Modulo,
    /// <summary>AND, OR, XOR : bit à bit sur 16 bits, comme le Locomotive BASIC (lot D, D060).</summary>
    And, Or, Xor,
    /// <summary>^ : puissance entière (lot D, D060).</summary>
    Power
}

/// <summary>Comparaison utilisée comme valeur : -1 si elle est vraie, 0 sinon, comme le Locomotive (A = B > 3) (lot D, D060).</summary>
public sealed record CompareExpression(CompareOperator Operator, Expression Left, Expression Right) : Expression;

/// <summary>
/// @A, @T(3) : adresse en mémoire où la variable (ou la case de tableau) est rangée, comme le Locomotive (D061).
/// Target est une VariableExpression ou une ArrayElementExpression.
/// </summary>
public sealed record AddressOfExpression(Expression Target) : Expression;

/// <summary>"BONJOUR" dans un calcul : un texte écrit dans le programme (lot E, D062).</summary>
public sealed record StringLiteralExpression(string Text) : Expression;

/// <summary>A$ + B$ : les deux textes mis bout à bout (lot E, D062).</summary>
public sealed record ConcatExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// Fonction du programmeur, DEF FN (lot K7, D095). Name : le nom écrit après FN, avec son signe (DOUBLE, A$, R!) ;
/// Parameters : les noms cachés des valeurs (FNDOUBLE.X), propres à la fonction ; WrittenParameters : les noms écrits.
/// Body : le calcul (null tant que DEF FN n'a pas été lu). Ready : définie et sans appel à elle-même ; seul alors le
/// calcul est parcouru avec les expressions qui l'appellent.
/// </summary>
public sealed class UserFunction(string name)
{
    public string Name { get; } = name;
    public IReadOnlyList<string> Parameters { get; set; } = [];
    public IReadOnlyList<string> WrittenParameters { get; set; } = [];
    public Expression? Body { get; set; }
    public int Line { get; set; }
    public bool Ready { get; set; }
}

/// <summary>FNNOM(valeurs) : appel d'une fonction du programmeur (lot K7, D095).</summary>
public sealed record UserFunctionExpression(UserFunction Function, IReadOnlyList<Expression> Arguments) : Expression;

/// <summary>DEF FNNOM(valeurs)=calcul : rien à faire à l'exécution, le calcul est recopié à chaque appel (D095).</summary>
public sealed record DefFnStatement(int SourceLine, UserFunction Function) : Statement(SourceLine);

/// <summary>Le type d'une expression : texte ou nombre (lot E, D062).</summary>
public static class Types
{
    /// <summary>
    /// Vrai si l'expression donne un nombre à virgule (D068) : un nombre écrit avec une virgule, une variable A!,
    /// ou + - * / ^ dont un côté est à virgule. Entre deux entiers, le calcul reste entier.
    /// </summary>
    public static bool IsReal(Expression expression) => expression switch
    {
        RealNumberExpression => true,
        VariableExpression variable => VariableNames.IsReal(variable.Name),
        ArrayElementExpression element => VariableNames.IsReal(element.Name),      // T!(3) (D070)
        BinaryExpression binary => binary.Operator is BinaryOperator.Add or BinaryOperator.Subtract
                                       or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Power
                                   && (IsReal(binary.Left) || IsReal(binary.Right)),
        // Fonctions mathématiques (lot F2) : toujours à virgule, ou selon leur valeur (INT(5) reste entier)
        CallExpression call => call.Function switch
        {
            "SIN" or "COS" or "TAN" or "ATN" or "SQR" or "EXP" or "LOG" or "LOG10" or "PI" or "RND" or "CREAL" or "VAL" => true,
            "HIMEM" or "FRE" => true,
            "TIME!" => true,                            // TIME avec le réglage F8 : 32 bits (D097)                   // plus de 32767 : nombres à virgule, comme le Locomotive (D094)
            "INT" or "FIX" or "ABS" => IsReal(call.Arguments[0]),
            "ROUND" => call.Arguments.Count == 2 || IsReal(call.Arguments[0]),
            "MIN" or "MAX" => call.Arguments.Any(IsReal),
            _ => false
        },
        UserFunctionExpression user => VariableNames.IsReal(user.Function.Name),   // FNR! (D095)
        _ => false
    };

    /// <summary>Vrai si l'expression donne un texte.</summary>
    public static bool IsString(Expression expression) => expression switch
    {
        StringLiteralExpression or ConcatExpression => true,
        CallExpression call => call.Function.EndsWith('$'),   // LEFT$, MID$, CHR$... (D063)
        UserFunctionExpression user => VariableNames.IsString(user.Function.Name),   // FNA$ (D095)
        VariableExpression variable => VariableNames.IsString(variable.Name),
        ArrayElementExpression element => VariableNames.IsString(element.Name),
        _ => false
    };
}

/// <summary>NOT valeur : inverse tous les bits (NOT 0 = -1, NOT -1 = 0), comme le Locomotive (lot D, D060).</summary>
public sealed record NotExpression(Expression Operand) : Expression;

public sealed record BinaryExpression(BinaryOperator Operator, Expression Left, Expression Right) : Expression;

/// <summary>Le programme complet : la suite de ses instructions.</summary>
/// <summary>
/// Le programme complet : la suite de ses instructions, et la table des lignes numérotées (numéro de ligne BASIC
/// -> rang de la première instruction qui la suit), utile pour GOTO, GOSUB et ON (D054).
/// </summary>
/// <param name="Starts">Les instructions qui commencent une instruction BASIC (après « : » ou en début de ligne) ;
/// une instruction BASIC peut en donner plusieurs ici (PRINT "A"; B). D080 : RESUME NEXT, minuteries.</param>
public sealed record BasicProgram(IReadOnlyList<Statement> Statements, IReadOnlyDictionary<int, int>? Lines = null,
                                  IReadOnlySet<Statement>? Starts = null)
{
    /// <summary>Les lignes numérotées du programme (vide si aucune).</summary>
    public IReadOnlyDictionary<int, int> LineTable => Lines ?? new Dictionary<int, int>();

    /// <summary>Vrai si l'instruction commence une instruction BASIC (toujours vrai si l'information manque).</summary>
    public bool IsStart(Statement statement) => Starts is null || Starts.Contains(statement);
}

/// <summary>SAVE nom,B,début,longueur[,lancement] : enregistre une zone de mémoire (lot J2, D085).</summary>
public sealed record SaveStatement(int SourceLine, Expression Name, Expression Start, Expression Length, Expression? Entry)
    : Statement(SourceLine);

/// <summary>LOAD nom[,adresse] : charge un fichier binaire (lot J2, D085).</summary>
public sealed record LoadStatement(int SourceLine, Expression Name, Expression? Address) : Statement(SourceLine);

/// <summary>
/// |NOM [, valeurs] (commande RSX, Rsx = nom) ou CALL adresse, valeurs (lot K2, D089) : comme le firmware, A = nombre de
/// valeurs, IX = la liste (2 octets chacune, la dernière en premier). Un texte passe l'adresse de sa fiche (comme @a$).
/// </summary>
public sealed record ExternalCallStatement(int SourceLine, string? Rsx, Expression? Address, IReadOnlyList<Expression> Arguments)
    : Statement(SourceLine);

/// <summary>KEY n,texte : texte d'une touche de fonction (n : 0 à 31 ou 128 à 159) (lot K5, D092).</summary>
public sealed record KeyStatement(int SourceLine, Expression Number, Expression Text) : Statement(SourceLine);

/// <summary>KEY DEF touche, répétition [, normal [, shift [, ctrl]]] (lot K5, D092).</summary>
public sealed record KeyDefStatement(int SourceLine, IReadOnlyList<Expression> Values) : Statement(SourceLine);

/// <summary>SPEED INK a,b (clignotement des couleurs) ou SPEED KEY a,b (répétition des touches) (lot K4, D091).</summary>
public sealed record SpeedStatement(int SourceLine, string Kind, Expression First, Expression Second) : Statement(SourceLine);

/// <summary>CLEAR : toutes les variables à 0 (textes vides), tableaux effacés (lot K3, D090).</summary>
public sealed record ClearStatement(int SourceLine) : Statement(SourceLine);

/// <summary>RUN ou RUN ligne : le programme recommence, variables effacées, DATA au début (lot K3, D090).</summary>
public sealed record RestartStatement(int SourceLine, int? Target) : Statement(SourceLine);

/// <summary>TRON / TROFF : le numéro de chaque ligne exécutée s'affiche entre crochets (lot K1, D088).</summary>
public sealed record TraceStatement(int SourceLine, bool On) : Statement(SourceLine);

/// <summary>RUN "nom" : charge un autre programme compilé (nom.BIN) et le lance (lot J3, D086).</summary>
public sealed record RunFileStatement(int SourceLine, Expression Name) : Statement(SourceLine);

/// <summary>SPEED WRITE 0 ou 1 : vitesse d'écriture sur cassette (lot J2, D085).</summary>
public sealed record SpeedWriteStatement(int SourceLine, Expression Value) : Statement(SourceLine);

/// <summary>OPENOUT nom, OPENIN nom, CLOSEOUT, CLOSEIN : fichiers de données sur disquette ou cassette (lot J1, D084).</summary>
public sealed record FileStatement(int SourceLine, string Command, Expression? Name = null) : Statement(SourceLine);

/// <summary>Ce que fait la touche ESC (D083) : rien (CONT), pause puis « Break in N » (STOP), pause puis appel (GOSUB).</summary>
public enum BreakMode { Cont = 0, Stop = 1, Gosub = 2 }

/// <summary>ON BREAK CONT, ON BREAK STOP, ON BREAK GOSUB ligne (lot I3b, D083).</summary>
public sealed record OnBreakStatement(int SourceLine, BreakMode Mode, int Target = 0) : Statement(SourceLine);

/// <summary>ON SQ(canal) GOSUB ligne : appel quand la file du canal (1, 2 ou 4) a une place libre (lot I3a, D082).</summary>
public sealed record OnSqStatement(int SourceLine, Expression Channel, int Target) : Statement(SourceLine);

/// <summary>ON ERROR GOTO ligne ; Target = 0 : ON ERROR GOTO 0, plus de traitement d'erreur (lot I2, D080).</summary>
public sealed record OnErrorStatement(int SourceLine, int Target) : Statement(SourceLine);

/// <summary>Les trois RESUME (D080) : refaire l'instruction en erreur, passer à la suivante, ou aller à une ligne.</summary>
public enum ResumeKind { Same, Next, Line }

/// <summary>RESUME, RESUME NEXT, RESUME ligne (lot I2, D080).</summary>
public sealed record ResumeStatement(int SourceLine, ResumeKind Kind, int Target = 0) : Statement(SourceLine);

/// <summary>ERROR numéro : provoque l'erreur de ce numéro (1 à 255) (lot I2, D080).</summary>
public sealed record ErrorStatement(int SourceLine, Expression Code) : Statement(SourceLine);

/// <summary>GOTO ligne : saut à une ligne numérotée. Target est le numéro de ligne BASIC (D054).</summary>
public sealed record GotoStatement(int SourceLine, int Target) : Statement(SourceLine);

/// <summary>GOSUB ligne : appel d'un sous-programme qui commence à une ligne numérotée (D054).</summary>
public sealed record GosubStatement(int SourceLine, int Target) : Statement(SourceLine);

/// <summary>
/// AFTER durée [, minuterie] GOSUB ligne (Every faux) ou EVERY durée [, minuterie] GOSUB ligne (lot I1, D079).
/// Durée en cinquantièmes de seconde ; minuterie de 0 à 3 (0 si elle est omise ; 3 est la plus prioritaire).
/// </summary>
public sealed record TimerStatement(int SourceLine, bool Every, Expression Time, Expression Timer, int Target)
    : Statement(SourceLine);

/// <summary>RETURN : fin d'un sous-programme appelé par GOSUB (D054).</summary>
public sealed record ReturnStatement(int SourceLine) : Statement(SourceLine);

/// <summary>STOP : arrête le programme, comme END (D054).</summary>
public sealed record StopStatement(int SourceLine) : Statement(SourceLine);

/// <summary>ON valeur GOTO / GOSUB ligne1, ligne2... : saut ou appel selon la valeur (1 = première ligne). Hors de 1 à N, on continue (D054).</summary>
public sealed record OnStatement(int SourceLine, Expression Selector, IReadOnlyList<int> Targets, bool IsGosub) : Statement(SourceLine);

/// <summary>CLEAR INPUT : vide les touches déjà tapées mais pas encore lues (lot B, D055).</summary>
public sealed record ClearInputStatement(int SourceLine) : Statement(SourceLine);

/// <summary>
/// INPUT [;] ["texte" ; ou ,] variable [, variable ...] : saisie de nombres entiers au clavier (lot B, D055).
/// Question : afficher « ? » après le texte (point-virgule, ou pas de texte). NoNewline : le « ; » juste après INPUT.
/// </summary>
public sealed record InputStatement(int SourceLine, string? Prompt, bool Question, bool NoNewline,
                                    IReadOnlyList<string> Variables,
                                    IReadOnlyList<IReadOnlyList<Expression>?>? Indexes = null) : Statement(SourceLine)
{
    /// <summary>
    /// Ce que reçoit chaque nombre tapé : une variable, ou une case de tableau (INPUT T(I), D059).
    /// Indexes[i] donne les numéros de case quand la i-ème cible est un tableau, sinon null.
    /// </summary>
    public IEnumerable<Expression> Targets => Variables.Select((name, i) =>
        Indexes?[i] is { } indexes ? new ArrayElementExpression(name, indexes) : (Expression)new VariableExpression(name));
}

/// <summary>
/// Un élément de DATA (D064). Un nombre garde aussi son écriture (READ A$ la rend telle quelle : « 012 ») ; un texte
/// n'a pas de valeur (READ A sur un texte : « Type mismatch »). Un élément vide est le nombre 0, écrit « ».
/// </summary>
/// <summary>Élément de DATA. IsReal : nombre à virgule (3.14, 1E5, 70000), valeur dans Real (lot F3, D070).</summary>
public sealed record DataItem(bool IsText, int Value, string Text, bool IsReal = false, double Real = 0);

/// <summary>DATA 10, "BONJOUR", -5 : éléments rangés dans le programme, lus par READ (lot C, D059 ; textes : D064).</summary>
public sealed record DataStatement(int SourceLine, IReadOnlyList<DataItem> Items) : Statement(SourceLine)
{
    /// <summary>Les nombres seulement, sur 16 bits.</summary>
    public IReadOnlyList<int> Values => Items.Where(item => !item.IsText && !item.IsReal).Select(item => item.Value).ToList();
}

/// <summary>LINE INPUT [;] ["texte" ;] A$ : toute la ligne tapée va dans A$, virgules comprises (D064).</summary>
/// <param name="Question">« ? » après le texte, comme INPUT : sans texte ou avec « ; » (SugarBox, 16 nov. 2026, D093).</param>
public sealed record LineInputStatement(int SourceLine, string? Prompt, bool NoNewline, string Variable,
                                        IReadOnlyList<Expression>? Indexes = null, bool Question = true) : Statement(SourceLine)
{
    /// <summary>La variable A$, ou la case N$(I) (D065).</summary>
    public Expression Target => Indexes is null ? new VariableExpression(Variable) : new ArrayElementExpression(Variable, Indexes);
}

/// <summary>
/// MID$(A$, début [, longueur]) = B$ : remplace des lettres de A$ sans changer sa longueur (D065).
/// Target est une variable texte ou une case d'un tableau de textes. Length est null s'il est omis.
/// </summary>
public sealed record MidAssignStatement(int SourceLine, Expression Target, Expression Start, Expression? Length, Expression Value)
    : Statement(SourceLine);

/// <summary>
/// READ A, T(I) : lit les nombres suivants des DATA (lot C, D059). Chaque cible est une VariableExpression
/// ou une ArrayElementExpression.
/// </summary>
public sealed record ReadStatement(int SourceLine, IReadOnlyList<Expression> Targets) : Statement(SourceLine);

/// <summary>RESTORE [ligne] : la lecture repart au premier DATA (ou au premier DATA de cette ligne ou après) (D059).</summary>
public sealed record RestoreStatement(int SourceLine, int? Line) : Statement(SourceLine);

/// <summary>Un tableau déclaré par DIM : son nom et la plus grande case de chaque dimension (DIM A(10, 5)) (lot C, D058).</summary>
public sealed record ArrayDeclaration(string Name, IReadOnlyList<Expression> Sizes);

/// <summary>DIM A(10) [, B(5, 5) ...] : crée des tableaux d'entiers, cases de 0 à la valeur écrite (lot C, D058).</summary>
public sealed record DimStatement(int SourceLine, IReadOnlyList<ArrayDeclaration> Arrays) : Statement(SourceLine);

/// <summary>ERASE T, N$ : efface des tableaux ; leur place est rendue (lot K8, D096).</summary>
public sealed record EraseStatement(int SourceLine, IReadOnlyList<string> Names) : Statement(SourceLine);

/// <summary>Une case de tableau lue dans un calcul : A(3), T(I, J) (lot C, D058). Name est le nom sans parenthèses.</summary>
public sealed record ArrayElementExpression(string Name, IReadOnlyList<Expression> Indexes) : Expression;

/// <summary>A(3) = valeur : écrit dans une case de tableau (lot C, D058).</summary>
public sealed record ArrayAssignStatement(int SourceLine, string Name, IReadOnlyList<Expression> Indexes, Expression Value)
    : Statement(SourceLine);

/// <summary>Parcourt les expressions d'une instruction (pour les vérifications sur les tableaux, D058).</summary>
public static class AstWalker
{
    /// <summary>L'expression et toutes celles qu'elle contient.</summary>
    public static IEnumerable<Expression> Flatten(Expression expression)
    {
        yield return expression;
        IEnumerable<Expression> inner = expression switch
        {
            BinaryExpression binary => [binary.Left, binary.Right],
            CallExpression call => call.Arguments,
            ArrayElementExpression element => element.Indexes,
            CompareExpression compare => [compare.Left, compare.Right],
            NotExpression negation => [negation.Operand],
            AddressOfExpression address => [address.Target],
            ConcatExpression concat => [concat.Left, concat.Right],
            UserFunctionExpression { Function: { Ready: true, Body: not null } } user =>
                user.Arguments.Append(user.Function.Body),   // le calcul de FN est recopié à l'appel (D095)
            UserFunctionExpression user => user.Arguments,
            _ => []
        };
        foreach (Expression child in inner)
            foreach (Expression e in Flatten(child))
                yield return e;
    }

    /// <summary>Les expressions d'une condition.</summary>
    public static IEnumerable<Expression> Of(Condition? condition) => condition switch
    {
        ComparisonCondition c => [c.Left, c.Right],
        AndCondition a => Of(a.Left).Concat(Of(a.Right)),
        OrCondition o => Of(o.Left).Concat(Of(o.Right)),
        _ => []
    };

    /// <summary>Les expressions écrites directement dans l'instruction (sans descendre dedans).</summary>
    public static IEnumerable<Expression> TopLevel(Statement statement) => statement switch
    {
        PrintValueStatement p => [p.Value],
        AssignStatement a => [a.Value],
        ArrayAssignStatement a => a.Indexes.Append(a.Value),
        DimStatement d => d.Arrays.SelectMany(x => x.Sizes),
        ForStatement f => new[] { f.Start, f.LimitValue, f.StepValue }.OfType<Expression>(),
        TimerStatement t => [t.Time, t.Timer],
        ErrorStatement e => [e.Code],
        OnSqStatement q => [q.Channel],
        FileStatement { Name: not null } f => [f.Name],
        SaveStatement sv => new[] { sv.Name, sv.Start, sv.Length, sv.Entry }.OfType<Expression>(),
        LoadStatement ld => new[] { ld.Name, ld.Address }.OfType<Expression>(),
        SpeedWriteStatement sw => [sw.Value],
        RunFileStatement rf => [rf.Name],
        SpeedStatement sp => [sp.First, sp.Second],
        KeyStatement ks => [ks.Number, ks.Text],
        KeyDefStatement kd => kd.Values,
        ExternalCallStatement ec => ec.Arguments.Prepend(ec.Address).OfType<Expression>(),
        WhileStatement w => Of(w.Condition),
        UntilStatement u => Of(u.Condition),
        DoStatement d => Of(d.Condition),
        LoopStatement l => Of(l.Condition),
        IfStatement i => Of(i.Condition),
        RandomizeStatement r when r.Seed is not null => [r.Seed],
        ScreenStatement s => s.Arguments,
        StreamStatement st when st.Stream is not null => [st.Stream],
        PrintTabStatement t => [t.Value],
        PrintUsingNumberStatement u => [u.Value],
        PrintUsingStringStatement u => [u.Value],
        EnvelopeStatement env => env.Sections.SelectMany(x => new[] { x.A, x.B, x.C }).OfType<Expression>().Prepend(env.Number),
        DrawStatement d => new[] { d.X, d.Y, d.Pen, d.Mode }.OfType<Expression>(),
        OnStatement o => [o.Selector],
        ReadStatement r => r.Targets,
        InputStatement i => i.Targets,
        LineInputStatement l => [l.Target],
        MidAssignStatement m => new[] { m.Target, m.Start, m.Length, m.Value }.OfType<Expression>(),
        _ => []
    };

    /// <summary>Toutes les expressions de l'instruction, y compris celles qui sont à l'intérieur d'autres.</summary>
    public static IEnumerable<Expression> All(Statement statement) => TopLevel(statement).SelectMany(Flatten);

    /// <summary>Vrai si l'instruction crée, lit ou écrit un tableau.</summary>
    public static bool UsesArrays(Statement statement) =>
        statement is DimStatement or ArrayAssignStatement or EraseStatement || All(statement).Any(e => e is ArrayElementExpression);
}
