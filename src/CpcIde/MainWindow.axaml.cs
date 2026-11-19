// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using CpcBasic.Core;
using CpcBasic.Core.Editing;
using CpcBasic.Core.Libraries;

namespace CpcIde;

public partial class MainWindow : Window
{
    const string StartProgram =
        "10 REM Mon premier programme\r\n" +
        "20 PRINT \"HELLO CPC !\"\r\n" +
        "30 PRINT \"COMPILE DEPUIS MON EDITEUR\"\r\n" +
        "40 END\r\n";

    static readonly FilePickerFileType BasicFileType = new("Programmes BASIC")
    {
        Patterns = new[] { "*.bas" }
    };

    static IBrush Make(string hex) => new SolidColorBrush(Color.Parse(hex));

    // Une ligne du panneau de messages. Line vaut 0 quand le message n'est lié à aucune ligne.
    // Sorte de message (D114) : une erreur a un ✗ rouge devant, une réussite un ✓ vert, une information rien.
    enum MessageKind { Info, Success, Error }

    sealed class MessageItem
    {
        public MessageItem(string text, int line, MessageKind kind = MessageKind.Info, string? highlight = null)
        {
            Text = text;
            Line = line;
            Kind = kind;
            Highlight = highlight;
        }

        public string Text { get; }
        // Partie du texte mise en valeur (adresse d'une variable, D132), ou null
        public string? Highlight { get; }
        public int Line { get; }
        public MessageKind Kind { get; }

        // Ce que lit un lecteur d'écran : le mot dit ce que l'icône montre
        public override string ToString() => Kind switch
        {
            MessageKind.Error => "Erreur : " + Text,
            MessageKind.Success => "Réussi : " + Text,
            _ => Text
        };
    }

    readonly EditorSettings settings = EditorSettings.Load();
    readonly BasicColorizer colorizer = new();

    string? currentPath;
    bool modified;
    bool loading;
    int? autoNext;           // AUTO en cours (D116) : prochain numéro de ligne à écrire ; null sinon
    int autoStep;
    string savedText = "";   // texte au dernier Ouvrir / Enregistrer : une commande effacée ne compte pas comme une modification
    double charWidth;        // largeur d'un caractère de l'éditeur, en points
    int visibleColumns;      // nombre de colonnes de caractères visibles dans la fenêtre d'édition
    bool closeConfirmed;   // vrai quand on a déjà demandé quoi faire des modifications avant de fermer
    bool realByDefault;    // ce programme : variables sans signe à virgule, comme le Locomotive (D066)
    bool breakKey = true;  // ce programme : touche ESC comme le vrai BASIC (pause, « Break in N ») (D083)

    public MainWindow()
    {
        InitializeComponent();

        Editor.TextArea.TextView.LineTransformers.Add(colorizer);
        UseLibraries(LibraryFolders.Load());    // commandes des bibliothèques colorées dès l'ouverture (D154)

        // Messages (D113) : une longue phrase passe à la ligne au lieu d'être coupée au bord du panneau
        // Une icône (D114) devant les erreurs (✗ rouge) et les réussites (✓ vert)
        MessageList.ItemTemplate = new FuncDataTemplate<MessageItem>((item, _) =>
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("28,*") };
            var icon = new TextBlock { FontWeight = FontWeight.Bold };
            if (item?.Kind == MessageKind.Error)
            {
                icon.Text = "✗";
                icon.Foreground = Make("#FF6060");
            }
            else if (item?.Kind == MessageKind.Success)
            {
                icon.Text = "✓";
                icon.Foreground = Make("#40E040");
            }
            var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (item?.Highlight is string highlight && item.Text.IndexOf(highlight, StringComparison.Ordinal) is int at and >= 0)
            {
                // Adresse d'une variable (D132) : en gras, jaune sur bleu comme l'écran du CPC (noir sur blanc en contraste
                // élevé), lisible aussi sur la ligne choisie
                bool high = settings.HighContrast;
                text.Inlines = new InlineCollection
                {
                    new Run(item.Text[..at]),
                    new Run(" " + highlight + " ")
                    {
                        FontWeight = FontWeight.Bold,
                        Foreground = Make(high ? "#000000" : "#FFFF00"),
                        Background = Make(high ? "#FFFFFF" : "#000080")
                    },
                    new Run(item.Text[(at + highlight.Length)..])
                };
            }
            else
                text.Text = item?.Text ?? "";
            Grid.SetColumn(text, 1);
            row.Children.Add(icon);
            row.Children.Add(text);
            return row;
        });

        // Un espace entre la colonne des numéros de l'éditeur et le texte du programme :
        // sans lui, « 1 » (éditeur) et « 10 » (numéro BASIC) se lisent comme « 110 ».
        Editor.TextArea.LeftMargins.Add(new Border { Width = 14 });

        // La largeur en colonnes change quand la fenêtre d'édition change de taille
        Editor.TextArea.TextView.PropertyChanged += (_, e) =>
        {
            if (e.Property == Avalonia.Visual.BoundsProperty)
                ComputeColumns();
        };
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatus();
        Editor.TextArea.TextEntering += OnTextEntering;
        Editor.TextChanged += (_, _) =>
        {
            if (loading) return;
            modified = true;
            UpdateTitle();
        };

        // « Tunnel » : la fenêtre voit les touches et la molette avant l'éditeur,
        // donc Ctrl+molette et Ctrl+plus ne sont pas pris par l'éditeur.
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnWindowWheel, RoutingStrategies.Tunnel);

        // Au démarrage, le curseur est dans l'éditeur : on peut taper tout de suite.
        Opened += (_, _) =>
        {
            Editor.TextArea.Focus();
            ComputeColumns();
        };

        SetText(StartProgram);
        UpdateTitle();
        ApplyAppearance();
    }

    // ---------- Boutons et raccourcis ----------

    async void OnOpenClick(object? sender, RoutedEventArgs e) => await OpenFileAsync();
    async void OnSaveClick(object? sender, RoutedEventArgs e) => await SaveAsync();
    async void OnSaveAsClick(object? sender, RoutedEventArgs e) => await SaveAsAsync();
    async void OnCompileClick(object? sender, RoutedEventArgs e) => await CompileAsync();
    void OnVerifyClick(object? sender, RoutedEventArgs e) => Verify();
    void OnSmallerClick(object? sender, RoutedEventArgs e) => ChangeFontSize(-2);
    void OnBiggerClick(object? sender, RoutedEventArgs e) => ChangeFontSize(+2);
    void OnContrastClick(object? sender, RoutedEventArgs e) => ToggleContrast();
    void OnRealClick(object? sender, RoutedEventArgs e) => ToggleRealByDefault();
    void OnBreakClick(object? sender, RoutedEventArgs e) => ToggleBreakKey();
    void OnTextClick(object? sender, RoutedEventArgs e) => ToggleTextOnDisk();
    void OnVersionClick(object? sender, RoutedEventArgs e) => ToggleCpcVersion();
    void OnVariablesClick(object? sender, RoutedEventArgs e) => ShowVariables();
    void OnLibrariesClick(object? sender, RoutedEventArgs e) => ShowLibraries();
    void OnDebugClick(object? sender, RoutedEventArgs e) => ToggleDebug();
    void OnPriorityClick(object? sender, RoutedEventArgs e) => TogglePriority();
    void OnWrapClick(object? sender, RoutedEventArgs e)
    {
        settings.WordWrap = !settings.WordWrap;
        settings.Save();
        ApplyAppearance();
        ShowMessage(settings.WordWrap
            ? "Lignes longues : elles continuent sur la ligne suivante de l'écran, comme sur le CPC (le numéro n'est affiché qu'une fois)."
            : "Lignes longues : sur une seule ligne, avec une barre de défilement en bas.");
    }

    // Menu Édition (D121) : l'action, puis le clavier revient dans l'éditeur
    void OnUndoClick(object? sender, RoutedEventArgs e) => EditAction(() => Editor.Undo());
    void OnRedoClick(object? sender, RoutedEventArgs e) => EditAction(() => Editor.Redo());
    void OnCutClick(object? sender, RoutedEventArgs e) => EditAction(Editor.Cut);
    void OnCopyClick(object? sender, RoutedEventArgs e) => EditAction(Editor.Copy);
    void OnPasteClick(object? sender, RoutedEventArgs e) => EditAction(Editor.Paste);
    void OnSelectAllClick(object? sender, RoutedEventArgs e) => EditAction(Editor.SelectAll);

    void EditAction(Action action)
    {
        action();
        Editor.TextArea.Focus();
    }

    void OnQuitClick(object? sender, RoutedEventArgs e) => Close();
    void OnNormalSizeClick(object? sender, RoutedEventArgs e) => SetFontSize(EditorSettings.DefaultFontSize);
    void OnShortcutsClick(object? sender, RoutedEventArgs e) => ShowShortcuts();
    void OnAboutClick(object? sender, RoutedEventArgs e) => ShowAbout();

    // Aide (D112) : les raccourcis, affichés dans le panneau des messages (lisibles par un lecteur d'écran)
    void ShowShortcuts()
    {
        ShowMessages(new[]
        {
            new MessageItem("Raccourcis clavier :", 0),
            new MessageItem("Ctrl+O : ouvrir un programme.   Ctrl+S : enregistrer.   Ctrl+Maj+S : enregistrer sous un autre nom.", 0),
            new MessageItem("F5 : compiler.   F4 : liste des variables.   Maj+F4 : liste des bibliothèques et de leurs commandes.", 0),
            new MessageItem("Maj+F5 : vérifier sans enregistrer (rien n'est écrit : ni le programme, ni la disquette).", 0),
            new MessageItem("F6 : passer de l'éditeur aux messages, et inversement.   Entrée sur un message d'erreur : aller à la ligne.", 0),
            new MessageItem("F7 : version du CPC, française (AZERTY, é è à ç ù) ou anglaise (QWERTY).", 0),
            new MessageItem("F8 : variables sans signe entières ou à virgule.   F9 : touche ESC comme le CPC ou ignorée.", 0),
            new MessageItem("F11 : texte du programme sur la disquette (pour le vrai BASIC : LOAD\"NOM.TXT\" puis RUN).", 0),
            new MessageItem("F2 : mise au point : les DEBUG du programme affichent les variables (sinon ils sont ignorés).", 0),
            new MessageItem("F3 : priorité vitesse ou place (le dessin reste le même ; place : CIRCLE passe par ELLIPSE si le programme a les deux).", 0),
            new MessageItem("Ctrl et + / Ctrl et - / Ctrl et 0 : taille du texte.   Ctrl+H : contraste élevé.", 0),
            new MessageItem("Alt : ouvrir la barre de menus (les lettres soulignées choisissent un menu).   F1 : cette aide.", 0)
        });
    }

    void ShowAbout()
    {
        string version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";
        ShowMessages(new[]
        {
            new MessageItem($"CPC BASIC IDE, version {version} : éditeur et compilateur de BASIC pour Amstrad CPC.", 0),
            new MessageItem("Logiciel libre (licence GPL 3), par RetroPoke : https://github.com/CPC-LOCO/cpc-basic-ide", 0)
        });
    }

    async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Entrée sur une commande du mode direct tapée sans numéro (NEW, EDIT 20, CLEAR...) : D077, D115
        // AUTO en cours : ESC l'arrête (D116)
        if (e.Key == Key.Escape && autoNext is not null && Editor.TextArea.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            StopAuto();
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && Editor.TextArea.IsKeyboardFocusWithin)
        {
            var line = Editor.Document.GetLineByNumber(Editor.TextArea.Caret.Line);
            if (autoNext is not null)
            {
                e.Handled = true;
                try
                {
                    ContinueAuto(line);
                }
                catch (Exception ex)
                {
                    // Jamais de fermeture de l'éditeur pour une commande : AUTO s'arrête et le problème est affiché
                    autoNext = null;
                    ShowError($"AUTO arrêté à cause d'un problème interne : {ex.Message}");
                }
                return;
            }
            string lineText = Editor.Document.GetText(line);
            var command = DirectCommands.Recognize(lineText);
            if (command is null
                && DirectCommands.LeadingNumber(lineText, out bool numberOnly) is int number
                && (numberOnly || DirectCommands.FindLine(Editor.Text, number, line.LineNumber) is not null))
            {
                // Numéro seul (effacer une ligne, comme le CPC) ou numéro déjà pris par une autre ligne (D118)
                e.Handled = true;
                try
                {
                    await HandleExistingNumberAsync(line, number, numberOnly);
                }
                catch (Exception ex)
                {
                    ShowError($"Ligne {number} : problème interne, rien n'a été fait ({ex.Message}).");
                }
                return;
            }
            if (command is not null)
            {
                e.Handled = true;
                try
                {
                    RemoveCommandLine(line);
                    await RunDirectCommandAsync(command);
                }
                catch (Exception ex)
                {
                    autoNext = null;
                    ShowError($"{command.Name} : problème interne, la commande n'a pas été faite ({ex.Message}).");
                }
            }
            return;
        }

        if (e.Key == Key.F5 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            Verify();
        }
        else if (e.Key == Key.F5)
        {
            e.Handled = true;
            await CompileAsync();
        }
        else if (e.Key == Key.F6)
        {
            // F6 : passer de l'éditeur au panneau de messages, et inversement
            e.Handled = true;
            if (MessageList.IsKeyboardFocusWithin)
                Editor.TextArea.Focus();
            else
            {
                // Rien de choisi : on prend la première erreur liée à une ligne (sinon le premier message)
                if (MessageList.SelectedItem is null
                    && MessageList.ItemsSource is List<MessageItem> messages
                    && messages.Count > 0)
                {
                    int first = messages.FindIndex(m => m.Line > 0);
                    MessageList.SelectedIndex = first >= 0 ? first : 0;
                }

                // Le clavier se pose sur un message précis (comme après un clic), pas sur la liste entière
                Control? container = MessageList.SelectedIndex >= 0
                    ? MessageList.ContainerFromIndex(MessageList.SelectedIndex)
                    : null;
                if (container is not null)
                    container.Focus(NavigationMethod.Tab);
                else
                    MessageList.Focus();
            }
        }
        else if (e.Key == Key.Enter && MessageList.IsKeyboardFocusWithin)
        {
            // Entrée sur un message : la liste la garde pour elle, donc on la prend ici, avant la liste
            e.Handled = true;
            GoToSelectedMessage(true);
            Editor.TextArea.Focus();   // même pour un message qui n'est lié à aucune ligne
        }
        else if (ctrl && e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            await SaveAsAsync();
        }
        else if (ctrl && e.Key == Key.S)
        {
            e.Handled = true;
            await SaveAsync();
        }
        else if (ctrl && e.Key == Key.O)
        {
            e.Handled = true;
            await OpenFileAsync();
        }
        else if (ctrl && e.Key is Key.OemPlus or Key.Add)
        {
            e.Handled = true;
            ChangeFontSize(+2);
        }
        else if (ctrl && e.Key is Key.OemMinus or Key.Subtract)
        {
            e.Handled = true;
            ChangeFontSize(-2);
        }
        else if (ctrl && e.Key is Key.D0 or Key.NumPad0)
        {
            e.Handled = true;
            SetFontSize(EditorSettings.DefaultFontSize);
        }
        else if (ctrl && e.Key == Key.H)
        {
            e.Handled = true;
            ToggleContrast();
        }
        else if (e.Key == Key.F4 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            ShowLibraries();
        }
        else if (e.Key == Key.F4)
        {
            e.Handled = true;
            ShowVariables();
        }
        else if (e.Key == Key.F7)
        {
            e.Handled = true;
            ToggleCpcVersion();
        }
        else if (e.Key == Key.F8)
        {
            e.Handled = true;
            ToggleRealByDefault();
        }
        else if (e.Key == Key.F9)
        {
            e.Handled = true;
            ToggleBreakKey();
        }
        else if (e.Key == Key.F1)
        {
            e.Handled = true;
            ShowShortcuts();
        }
        else if (e.Key == Key.F2)
        {
            e.Handled = true;
            ToggleDebug();
        }
        else if (e.Key == Key.F3)
        {
            e.Handled = true;
            TogglePriority();
        }
        else if (e.Key == Key.F11)
        {
            e.Handled = true;
            ToggleTextOnDisk();
        }
    }

    // Ctrl + molette : taille du texte
    void OnWindowWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        e.Handled = true;
        if (e.Delta.Y > 0)
            ChangeFontSize(+2);
        else if (e.Delta.Y < 0)
            ChangeFontSize(-2);
    }

    // ---------- Apparence : taille du texte et contraste ----------

    void ChangeFontSize(int delta) => SetFontSize(settings.FontSize + delta);

    void SetFontSize(int size)
    {
        size = Math.Clamp(size, EditorSettings.MinFontSize, EditorSettings.MaxFontSize);
        if (size == settings.FontSize)
            return;

        settings.FontSize = size;
        settings.Save();
        ApplyAppearance();
    }

    // F8 : variables sans signe entières, ou à virgule comme le Locomotive ; mémorisé pour ce programme (D066)
    void ToggleRealByDefault()
    {
        realByDefault = !realByDefault;
        if (currentPath is not null)
        {
            settings.SetRealByDefault(currentPath, realByDefault);
            settings.Save();
        }
        UpdateOptionButtons();
        ShowMessage(realByDefault
            ? "Variables sans signe (A, B...) : nombres à virgule, comme le Locomotive BASIC. A% reste un entier."
            : "Variables sans signe (A, B...) : nombres entiers, plus rapides. A! sera un nombre à virgule.");
    }

    // F9 : touche ESC comme le vrai BASIC, ou ignorée ; mémorisé pour ce programme (D083)
    void ToggleBreakKey()
    {
        breakKey = !breakKey;
        if (currentPath is not null)
        {
            settings.SetBreakIgnored(currentPath, !breakKey);
            settings.Save();
        }
        UpdateOptionButtons();
        ShowMessage(breakKey
            ? "Touche ESC comme le vrai BASIC : une fois = pause, deux fois = « Break in … » et arrêt (sauf ON BREAK)."
            : "Touche ESC ignorée (sauf ON BREAK dans le programme) : programme un peu plus petit et plus rapide.");
    }

    // F4 : liste des variables du programme (D131, solution A) : type, lignes où elles reçoivent une valeur et où elles
    // sont utilisées ; ⚠ devant celles à vérifier ; adresse dans la mémoire du CPC (D132, solution B).
    // Entrée sur une ligne : le curseur va à sa première ligne.
    void ShowVariables()
    {
        UseLibraries(LibraryFolders.Load());
        var options = new CompilerOptions(TargetMachine.AllCpc, realByDefault, breakKey, settings.CpcVersion, Libraries: libraries);
        IReadOnlyList<VariableLine> variables = VariableList.Build(Editor.Text, options);
        int warnings = variables.Count(v => v.Warning);
        var items = new List<MessageItem>
        {
            new(variables.Count == 0
                ? "Aucune variable dans ce programme."
                : $"Variables du programme : {variables.Count}{(warnings > 0 ? $", dont {warnings} à vérifier (⚠)" : "")}. "
                  + "F6 puis Entrée sur une ligne : aller à la variable dans le programme.", 0)
        };
        if (CpcBasic.Core.Language.BasicAnalyzer.Analyze(Editor.Text, options).HasErrors)
            items.Add(new MessageItem("Le programme contient des erreurs (F5 pour les voir) : la liste peut être incomplète, et les adresses ne sont pas données.", 0, MessageKind.Error));
        items.AddRange(variables.Select(v => new MessageItem(v.Text, v.Line, MessageKind.Info, v.Address)));
        ShowMessages(items);
    }

    // F7 : version du CPC, française ou anglaise ; pour tous les programmes (D126). Les textes entre guillemets du
    // programme sont convertis : chaque caractère garde son code et prend le dessin de la nouvelle version.
    void ToggleCpcVersion()
    {
        CpcLanguage from = settings.CpcVersion;
        CpcLanguage to = from == CpcLanguage.French ? CpcLanguage.English : CpcLanguage.French;
        settings.CpcVersion = to;
        settings.Save();
        ConvertDocument(from, to);
        UpdateOptionButtons();
        ShowMessage(to == CpcLanguage.French
            ? "Version du CPC : français (AZERTY). Le programme montre ce qu'affiche un CPC français : é è à ç ù °, à pour @, ù pour | (commandes RSX)."
            : "Version du CPC : anglais (QWERTY). Le programme montre ce qu'affiche un CPC anglais : { pour é, } pour è, @ pour à, \\ pour ç, | pour ù, ↑ pour ^.");
    }

    // Remplace seulement les caractères qui changent (même longueur) : le curseur reste à sa place, Ctrl+Z annule tout
    void ConvertDocument(CpcLanguage from, CpcLanguage to)
    {
        string text = Editor.Text;
        string converted = CharacterSet.ConvertProgram(text, from, to);
        if (converted == text)
            return;
        var document = Editor.Document;
        using (document.RunUpdate())
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != converted[i])
                    document.Replace(i, 1, converted[i].ToString());
            }
        }
    }

    // Une touche tapée donne, partout dans le programme, le caractère que le CPC choisi affichera (D126, D133) :
    // sur le CPC français, « { » devient é et @ devient à ; sur le CPC anglais, é devient « { »
    void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (e.Text is not { Length: 1 } typed)
            return;
        char shown = CharacterSet.Shown(typed[0], settings.CpcVersion);
        if (shown == typed[0])
            return;
        e.Handled = true;
        Editor.TextArea.Selection.ReplaceSelectionWithText(shown.ToString());
    }

    // F2 : mise au point (D136) : les DEBUG du programme affichent les variables ; sinon ignorés ; pour tous les programmes
    void ToggleDebug()
    {
        settings.DebugMode = !settings.DebugMode;
        settings.Save();
        UpdateOptionButtons();
        ShowMessage(settings.DebugMode
            ? "Mise au point : oui. Chaque DEBUG du programme affiche les variables ; ESPACE : suite, S : stop (plus aucun DEBUG)."
            : "Mise au point : non. Les DEBUG du programme sont ignorés : le programme garde sa taille normale.");
    }

    // F3 : priorité vitesse ou place (D141) ; ne change jamais le dessin ; pour tous les programmes
    void TogglePriority()
    {
        settings.PreferSize = !settings.PreferSize;
        settings.Save();
        UpdateOptionButtons();
        ShowMessage(settings.PreferSize
            ? "Priorité : place. Avec CIRCLE et ELLIPSE dans le programme, les ronds passent par ELLIPSE : environ 750 octets de moins, ronds environ 3 fois plus lents ; mêmes points."
            : "Priorité : vitesse. Chaque commande garde sa routine la plus rapide (CIRCLE garde la sienne, même avec ELLIPSE).");
    }

    // Le programme contient-il CIRCLE et ELLIPSE (D141) ? Seul cas où la priorité F3 change quelque chose aujourd'hui.
    bool UsesCircleAndEllipse()
    {
        var statements = CpcBasic.Core.Language.BasicAnalyzer.Analyze(Editor.Text,
            new CompilerOptions(Language: settings.CpcVersion, Libraries: libraries)).Program.Statements;
        return statements.Any(s => s is CpcBasic.Core.Language.ScreenStatement { Command: "CIRCLE" })
               && statements.Any(s => s is CpcBasic.Core.Language.EllipseStatement);
    }

    // Nombre de DEBUG du programme (D136), pour le rappeler quand ils sont ignorés
    int CountDebug() => CpcBasic.Core.Language.BasicAnalyzer.Analyze(Editor.Text,
            new CompilerOptions(Language: settings.CpcVersion, Libraries: libraries)).Program.Statements.OfType<CpcBasic.Core.Language.DebugStatement>().Count();

    // F11 : le texte du programme aussi sur la disquette (NOM.TXT), pour le vrai BASIC ; pour tous les programmes (D101)
    void ToggleTextOnDisk()
    {
        settings.TextOnDisk = !settings.TextOnDisk;
        settings.Save();
        UpdateOptionButtons();
        ShowMessage(settings.TextOnDisk
            ? "Texte sur la disquette : oui. F5 ajoute NOM.TXT ; dans l'émulateur : LOAD\"NOM.TXT\" puis RUN pour le vrai BASIC."
            : "Texte sur la disquette : non. F5 ne met que le programme compilé.");
    }

    void UpdateOptionButtons()
    {
        // Menu Options (D112) : coché quand le réglage est actif
        TextMenu.IsChecked = settings.TextOnDisk;
        DebugMenu.IsChecked = settings.DebugMode;
        PriorityMenu.Header = settings.PreferSize ? "_Priorité : place" : "_Priorité : vitesse";
        VersionMenu.Header = settings.CpcVersion == CpcLanguage.French
            ? "Version du _CPC : français (AZERTY)"
            : "Version du _CPC : anglais (QWERTY)";
        BreakMenu.IsChecked = breakKey;
        RealMenu.Header = realByDefault
            ? "_Variables sans signe (A, X...) : à virgule"
            : "_Variables sans signe (A, X...) : entières";
        ContrastMenu.IsChecked = settings.HighContrast;
        WrapMenu.IsChecked = settings.WordWrap;
        UpdateStatus();
    }

    void ToggleContrast()
    {
        settings.HighContrast = !settings.HighContrast;
        settings.Save();
        ApplyAppearance();
    }

    // Applique les réglages à l'éditeur, au panneau de messages et à la barre d'état
    void ApplyAppearance()
    {
        bool high = settings.HighContrast;

        Editor.FontSize = settings.FontSize;
        // Lignes longues (D113) : à la ligne comme le CPC, ou une seule ligne avec défilement horizontal
        Editor.WordWrap = settings.WordWrap;
        Editor.HorizontalScrollBarVisibility = settings.WordWrap
            ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        Editor.Background = Make(high ? "#000000" : "#000080");
        Editor.Foreground = Make(high ? "#FFFFFF" : "#FFFF00");
        // Numéros de l'éditeur en gris : jamais de la couleur des numéros BASIC (violet ou jaune)
        Editor.LineNumbersForeground = Make(high ? "#C0C0C0" : "#B0B0B0");

        colorizer.HighContrast = high;
        Editor.TextArea.TextView.Redraw();

        // Le panneau de messages et la barre d'état restent lisibles : au moins 16 et 14 points
        MessageList.FontSize = Math.Max(16, settings.FontSize);
        MessageList.Background = Make(high ? "#000000" : "#101010");
        MessageList.Foreground = Make(high ? "#FFFFFF" : "#E0E0E0");

        // Barre d'état (D113) : claire comme sous Windows ; en contraste élevé, blanc sur noir
        StatusText.FontSize = StatusRight.FontSize = Math.Max(14, settings.FontSize - 2);
        StatusText.Foreground = StatusRight.Foreground = Make(high ? "#FFFFFF" : "#000000");
        StatusBar.Background = Make(high ? "#000000" : "#F0F0F0");
        StatusBar.BorderBrush = Make(high ? "#FFFFFF" : "#C8C8C8");
        MessageSplitter.Background = Make(high ? "#FFFFFF" : "#808080");

        UpdateOptionButtons();

        UpdateColumns();
    }

    // Mesure la largeur d'un caractère de l'éditeur (la police est à chasse fixe), puis la largeur en colonnes
    void UpdateColumns()
    {
        var sample = new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                       new Typeface(Editor.FontFamily), Editor.FontSize, Brushes.Black);
        charWidth = sample.Width;
        ComputeColumns();
    }

    void ComputeColumns()
    {
        double width = Editor.TextArea.TextView.Bounds.Width;
        visibleColumns = charWidth > 0 ? (int)Math.Floor(width / charWidth) : 0;
        UpdateStatus();
    }

    // ---------- Ouvrir et enregistrer ----------

    // Si le programme a des modifications non enregistrées, demande quoi faire (Enregistrer, Ne pas enregistrer, Annuler).
    // Renvoie vrai si on peut continuer (enregistré ou abandonné), faux si la personne a annulé.
    async Task<bool> ConfirmDiscardOrSaveAsync()
    {
        if (!modified)
            return true;

        string file = currentPath is null ? "sans titre" : Path.GetFileName(currentPath);
        var dialog = new SaveChangesDialog(file);
        SaveChangesDialog.Choice choice = await dialog.ShowDialog<SaveChangesDialog.Choice>(this);

        return choice switch
        {
            SaveChangesDialog.Choice.Save => await SaveAsync(),   // faux si l'enregistrement échoue ou est annulé
            SaveChangesDialog.Choice.Discard => true,
            _ => false
        };
    }

    // Fermeture de la fenêtre : si le programme est modifié, on la retient et on demande d'abord quoi faire
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (closeConfirmed || !modified)
            return;

        e.Cancel = true;
        _ = CloseAfterConfirmAsync();
    }

    async Task CloseAfterConfirmAsync()
    {
        if (await ConfirmDiscardOrSaveAsync())
        {
            closeConfirmed = true;
            Close();
        }
    }

    async Task OpenFileAsync()
    {
        if (!await ConfirmDiscardOrSaveAsync())
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Ouvrir un programme BASIC",
            AllowMultiple = false,
            FileTypeFilter = new[] { BasicFileType, FilePickerFileTypes.All }
        });
        if (files.Count == 0)
            return;

        string? path = files[0].TryGetLocalPath();
        if (path is null)
        {
            ShowError("Impossible de lire ce fichier : son emplacement n'est pas accessible.");
            return;
        }

        try
        {
            // Textes montrés comme le CPC choisi les affiche (D126) ; les codes ne changent pas
            string text = CharacterSet.ConvertProgram(await File.ReadAllTextAsync(path), settings.CpcVersion, settings.CpcVersion);
            SetText(text);
            currentPath = path;
            realByDefault = settings.IsRealByDefault(path);   // réglage mémorisé pour ce programme (D066)
            breakKey = !settings.IsBreakIgnored(path);        // D083
            UpdateOptionButtons();
            UpdateTitle();
            ShowMessage($"Fichier ouvert : {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Impossible de lire le fichier : {ex.Message}");
        }
    }

    // Enregistre le programme ; demande un nom si le fichier n'en a pas encore. Renvoie faux si annulé ou en échec.
    async Task<bool> SaveAsync()
    {
        if (currentPath is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer le programme",
                SuggestedFileName = "jeu.bas",
                DefaultExtension = "bas",
                FileTypeChoices = new[] { BasicFileType }
            });
            if (file is null)
                return false;

            string? chosen = file.TryGetLocalPath();
            if (chosen is null)
            {
                ShowError("Impossible d'enregistrer ici : emplacement non accessible.");
                return false;
            }
            currentPath = chosen;
            if (realByDefault || !breakKey)
            {
                settings.SetRealByDefault(chosen, realByDefault);   // les réglages suivent le programme (D066, D083)
                settings.SetBreakIgnored(chosen, !breakKey);
                settings.Save();
            }
        }

        try
        {
            await File.WriteAllTextAsync(currentPath, Editor.Text);
            savedText = Editor.Text;
            modified = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Impossible d'enregistrer le fichier : {ex.Message}");
            return false;
        }
    }

    // Ctrl+Maj+S (D135, remarque de l'utilisateur) : enregistrer sous un autre nom. L'ancien fichier reste tel qu'il était ;
    // l'éditeur travaille ensuite sur le nouveau, qui garde les réglages F8 et F9 du programme.
    async Task<bool> SaveAsAsync()
    {
        IStorageFolder? start = null;
        if (currentPath is not null && Path.GetDirectoryName(currentPath) is string folder)
            start = await StorageProvider.TryGetFolderFromPathAsync(folder);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le programme sous un autre nom",
            SuggestedFileName = currentPath is null ? "jeu.bas" : Path.GetFileName(currentPath),
            SuggestedStartLocation = start,
            DefaultExtension = "bas",
            FileTypeChoices = new[] { BasicFileType }
        });
        if (file is null)
            return false;

        string? chosen = file.TryGetLocalPath();
        if (chosen is null)
        {
            ShowError("Impossible d'enregistrer ici : emplacement non accessible.");
            return false;
        }
        string stem = Path.GetFileNameWithoutExtension(chosen);
        if (stem.Length is 0 or > 8)
        {
            ShowError($"Le nom « {stem} » est trop long : une disquette CPC accepte 8 caractères au plus. " +
                      "Rien n'a été enregistré ; choisissez un nom plus court (par exemple « essai.bas »).");
            return false;
        }

        string? old = currentPath;
        currentPath = chosen;
        settings.SetRealByDefault(chosen, realByDefault);   // les réglages suivent le programme (D066, D083)
        settings.SetBreakIgnored(chosen, !breakKey);
        settings.Save();
        if (!await SaveAsync())
        {
            currentPath = old;
            return false;
        }
        ShowMessage(old is null
            ? $"Programme enregistré : {chosen}"
            : $"Programme enregistré sous un autre nom : {chosen}. L'ancien fichier ({Path.GetFileName(old)}) n'a pas changé.");
        return true;
    }

    // ---------- Compilation ----------

    async Task CompileAsync()
    {
        if (!await SaveAsync())
            return;

        string path = currentPath!;
        string name = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        if (name.Length is 0 or > 8)
        {
            ShowError($"Le nom « {name} » ne convient pas : une disquette CPC accepte 8 caractères au plus. " +
                        "Renommez le fichier (par exemple « jeu.bas ») puis recompilez.");
            return;
        }

        UseLibraries(LibraryFolders.Load());      // bibliothèques relues à chaque compilation (D149)
        CompileResult result;
        try
        {
            // RUN "nom" écrit en clair : nom.BAS du même dossier est compilé aussi, sur la même disquette (D087)
            string folder = Path.GetDirectoryName(path) ?? ".";
            ProgramSource? Find(string program)
            {
                string? file = Directory.EnumerateFiles(folder, "*.bas")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(program, StringComparison.OrdinalIgnoreCase));
                if (file is null)
                    return null;
                return new ProgramSource(File.ReadAllText(file),
                                         new CompilerOptions(TargetMachine.AllCpc, settings.IsRealByDefault(file), !settings.IsBreakIgnored(file),
                                                             settings.CpcVersion, settings.DebugMode, settings.PreferSize, libraries));
            }
            result = Compiler.CompileWithLinked(Editor.Text, name,
                                                new CompilerOptions(TargetMachine.AllCpc, realByDefault, breakKey, settings.CpcVersion, settings.DebugMode, settings.PreferSize, libraries), Find);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ShowError($"Compilation impossible : {ex.Message}");
            return;
        }

        if (!result.Success)
        {
            // une erreur dans un autre programme (RUN "nom", D087) ne mène pas à une ligne de celui-ci
            var items = result.Diagnostics.Select(d => new MessageItem(d.Message, d.File is null ? d.Line : 0, MessageKind.Error)).ToList();
            items.Add(new MessageItem($"{result.Diagnostics.Count} erreur(s). Aucun fichier n'a été créé.", 0));
            items.AddRange(LibraryProblems());
            ShowMessages(items);
            return;
        }

        string dskPath = Path.ChangeExtension(path, ".dsk");
        string? textNote = null;
        if (settings.TextOnDisk)
        {
            try
            {
                result.Disk!.AddTextFile(name, "TXT", Editor.Text, settings.CpcVersion);   // pour le vrai BASIC (D101, D126)
                textNote = $"Texte du programme aussi sur la disquette : {name}.TXT (dans l'émulateur : LOAD\"{name}.TXT\" puis RUN).";
            }
            catch (InvalidOperationException ex)
            {
                textNote = $"Le texte du programme n'a pas pu être mis sur la disquette : {ex.Message}";
            }
        }
        try
        {
            result.Disk!.Save(dskPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessages(new[]
            {
                new MessageItem("Impossible d'écrire le fichier .dsk : il est sans doute ouvert dans votre émulateur.", 0, MessageKind.Error),
                new MessageItem("Fermez l'émulateur (ou éjectez la disquette), puis compilez de nouveau.", 0),
                new MessageItem($"Fichier : {dskPath}", 0),
                new MessageItem($"Détail de Windows : {ex.Message}", 0)
            });
            return;
        }

        // « GRAPHIQUE:STAR » : commande de bibliothèque (D148) ; les autres sont les modules du compilateur
        List<string> ownModules = result.Modules.Where(m => !m.Contains(':')).ToList();
        string modules = ownModules.Count == 0 ? "aucun" : string.Join(", ", ownModules);
        var done = new List<MessageItem>
        {
            new MessageItem($"Compilation réussie : {dskPath}", 0, MessageKind.Success),
            new MessageItem($"Programme Z80 : {result.BinarySize} octets. Modules liés : {modules}.", 0),
            new MessageItem("Machine : tous les CPC (le programme reconnaît la machine au démarrage). " +
                            $"Variables sans signe : {(realByDefault ? "à virgule" : "entières")}. " +
                            $"Touche ESC : {(breakKey ? "comme le CPC" : "ignorée")}.", 0)
        };
        IEnumerable<string> libraryModules = result.Modules.Where(m => m.Contains(':'));
        if (libraryModules.Any())              // D149 : bibliothèques utilisées, avec leurs commandes
            done.Add(new MessageItem("Bibliothèques utilisées : " + string.Join(" ; ", libraryModules
                .GroupBy(m => m[..m.IndexOf(':')])
                .Select(g => $"{g.Key} ({string.Join(", ", g.Select(m => m[(m.IndexOf(':') + 1)..]))})")) + ".", 0));
        if (textNote is not null)
            done.Add(new MessageItem(textNote, 0));
        foreach (LinkedProgram linked in result.Linked)   // D087
            done.Add(new MessageItem($"Aussi compilé et mis sur la disquette (RUN \"{linked.Name}\") : {linked.Name}.BAS, {linked.BinarySize} octets.", 0));
        foreach (string absent in result.Missing)
            done.Add(new MessageItem($"RUN \"{absent}\" : {absent}.BAS n'est pas dans le dossier ; ce programme n'est pas sur la disquette.", 0));
        int debugs = CountDebug();
        if (debugs > 0)
            done.Add(new MessageItem(settings.DebugMode
                ? $"Mise au point : {debugs} DEBUG actif(s) (ESPACE : suite, S : stop). F2 pour les couper."
                : $"{debugs} DEBUG ignoré(s) (mise au point coupée, F2).", 0));
        if (UsesCircleAndEllipse())            // D141 : rappel de la priorité, seulement quand elle change quelque chose
            done.Add(new MessageItem(settings.PreferSize
                ? "Priorité place (F3) : les CIRCLE passent par ELLIPSE (environ 750 octets de moins, ronds plus lents, mêmes points)."
                : "Priorité vitesse (F3) : CIRCLE garde sa routine rapide (environ 750 octets de plus qu'avec la priorité place).", 0));
        done.Add(new MessageItem($"Dans l'émulateur : insérez le disque dans le lecteur A, puis tapez RUN\"{name}\"", 0));
        done.Add(new MessageItem("Avant de recompiler le même programme, fermez l'émulateur : il garde le fichier .dsk ouvert.", 0));
        done.AddRange(LibraryProblems());
        ShowMessages(done);
    }

    // Maj+F5 (D134, idée de l'utilisateur) : compile en mémoire, comme F5, sans enregistrer le programme ni écrire de
    // disquette ; l'étoile du titre reste si le programme n'est pas enregistré
    void Verify()
    {
        string name = currentPath is null ? "VERIF" : Path.GetFileNameWithoutExtension(currentPath).ToUpperInvariant();
        if (name.Length is 0 or > 8)
            name = "VERIF";
        string? folder = currentPath is null ? null : Path.GetDirectoryName(currentPath);
        UseLibraries(LibraryFolders.Load());      // D149
        CompileResult result;
        try
        {
            ProgramSource? Find(string program)        // RUN "nom" : comme F5, les programmes du même dossier (lus seulement)
            {
                if (folder is null)
                    return null;
                string? file = Directory.EnumerateFiles(folder, "*.bas")
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(program, StringComparison.OrdinalIgnoreCase));
                return file is null ? null
                    : new ProgramSource(File.ReadAllText(file),
                                        new CompilerOptions(TargetMachine.AllCpc, settings.IsRealByDefault(file), !settings.IsBreakIgnored(file),
                                                            settings.CpcVersion, settings.DebugMode, settings.PreferSize, libraries));
            }
            result = Compiler.CompileWithLinked(Editor.Text, name,
                                                new CompilerOptions(TargetMachine.AllCpc, realByDefault, breakKey, settings.CpcVersion, settings.DebugMode, settings.PreferSize, libraries), Find);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            ShowError($"Vérification impossible : {ex.Message}");
            return;
        }

        if (!result.Success)
        {
            var items = result.Diagnostics.Select(d => new MessageItem(d.Message, d.File is null ? d.Line : 0, MessageKind.Error)).ToList();
            items.Add(new MessageItem($"{result.Diagnostics.Count} erreur(s). Rien n'a été enregistré.", 0));
            items.AddRange(LibraryProblems());
            ShowMessages(items);
            return;
        }
        ShowMessages(new[]
        {
            new MessageItem($"Vérification réussie : aucune erreur. Programme Z80 : {result.BinarySize} octets. Rien n'a été enregistré.", 0, MessageKind.Success),
            new MessageItem(modified ? "Le programme n'est pas encore enregistré (étoile dans le titre) : Ctrl+S ou F5 pour l'enregistrer."
                                     : "F5 pour créer la disquette.", 0)
        });
    }

    // ---------- Bibliothèques (D149) ----------

    // Catalogue lu à la dernière compilation (F5, Maj+F5 ou F4)
    LibraryCatalog libraries = LibraryCatalog.Empty;

    // Maj+F4 : fenêtre des bibliothèques (D156, idée de l'utilisateur ; remplace la liste dans les messages de D155) ;
    // une seule fenêtre : redemandée, elle est remise à jour et passe devant
    LibraryWindow? libraryWindow;

    void ShowLibraries()
    {
        UseLibraries(LibraryFolders.Load());
        if (libraryWindow is null)
        {
            libraryWindow = new LibraryWindow(settings.LibraryFontSize, size =>
            {
                settings.LibraryFontSize = size;
                settings.Save();
            });
            libraryWindow.Closed += (_, _) => libraryWindow = null;
            libraryWindow.Fill(libraries);
            libraryWindow.Show(this);
        }
        else
        {
            libraryWindow.Fill(libraries);
            libraryWindow.Activate();
        }
    }

    // Nouveau catalogue : gardé pour la compilation, et ses commandes et fonctions colorées dans l'éditeur (D154)
    void UseLibraries(LibraryCatalog catalog)
    {
        libraries = catalog;
        colorizer.LibraryWords = new HashSet<string>(
            catalog.All.Where(c => c.Kind != LibraryKind.Routine).Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        Editor.TextArea.TextView.Redraw();
    }

    // Fichiers de commande refusés par le catalogue : signalés après le compte rendu, pour pouvoir les corriger
    IEnumerable<MessageItem> LibraryProblems() =>
        libraries.Errors.Select(e => new MessageItem($"Bibliothèque : fichier refusé, {e}", 0, MessageKind.Error));

    // ---------- Panneau de messages ----------

    void ShowMessage(string text) => ShowMessages(new[] { new MessageItem(text, 0) });
    void ShowError(string text) => ShowMessages(new[] { new MessageItem(text, 0, MessageKind.Error) });

    void ShowMessages(IEnumerable<MessageItem> items)
    {
        MessageList.ItemsSource = items.ToList();
        MessageList.SelectedItem = null;
    }

    // Place le curseur de l'éditeur sur la ligne du message choisi.
    // Avec focusEditor, le clavier passe aussi dans l'éditeur.
    void GoToSelectedMessage(bool focusEditor)
    {
        if (MessageList.SelectedItem is MessageItem { Line: > 0 } item)
        {
            int line = Math.Min(item.Line, Editor.Document.LineCount);
            Editor.TextArea.Caret.Position = new TextViewPosition(line, 1);
            Editor.ScrollToLine(line);
            if (focusEditor)
                Editor.TextArea.Focus();
        }
    }

    // Flèches haut/bas dans la liste : le curseur de l'éditeur suit, mais on reste dans la liste
    void OnMessageSelected(object? sender, SelectionChangedEventArgs e) => GoToSelectedMessage(false);

    // Clic sur un message : on va dans l'éditeur, sur la ligne concernée
    void OnMessageTapped(object? sender, TappedEventArgs e) => GoToSelectedMessage(true);

    // Entrée sur un message : on va dans l'éditeur, sur la ligne concernée s'il y en a une.
    // Échap : retour dans l'éditeur, sans changer de ligne.
    void OnMessageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            GoToSelectedMessage(true);
            Editor.TextArea.Focus();   // même pour un message qui n'est lié à aucune ligne
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Editor.TextArea.Focus();
        }
    }

    // ---------- Titre et barre d'état ----------

    // ---------- Commandes du mode direct (D077, D115) ----------

    // La ligne de la commande disparaît du programme ; si le reste est comme au dernier enregistrement, il n'est plus « modifié »
    void RemoveCommandLine(AvaloniaEdit.Document.DocumentLine line)
    {
        Editor.Document.Remove(line.Offset, line.TotalLength);
        modified = Editor.Text != savedText;
        UpdateTitle();
    }

    async Task RunDirectCommandAsync(DirectCommand command)
    {
        switch (command.Kind)
        {
            case DirectCommandKind.New:
                if (!await ConfirmDiscardOrSaveAsync())
                {
                    ShowMessage("NEW annulé : le programme est gardé.");
                    return;
                }
                SetText("");
                currentPath = null;
                UpdateTitle();
                ShowMessage("NEW : nouveau programme, sans titre. Ctrl+S l'enregistre sous un nom.");
                break;

            case DirectCommandKind.Edit:
                if (!int.TryParse(command.Argument, out int number) || number < 1 || number > 65535)
                {
                    ShowError("EDIT : il faut le numéro de la ligne, par exemple EDIT 20.");
                    return;
                }
                int? target = DirectCommands.FindLine(Editor.Text, number);
                if (target is null)
                {
                    ShowError($"EDIT {number} : cette ligne n'existe pas (Line does not exist).");
                    return;
                }
                var found = Editor.Document.GetLineByNumber(target.Value);
                Editor.TextArea.Caret.Offset = found.EndOffset;
                Editor.TextArea.Caret.BringCaretToView();
                ShowMessage($"EDIT {number} : le curseur est à la fin de la ligne {number}.");
                break;

            case DirectCommandKind.Auto:
                if (!DirectCommands.TryParseAuto(command.Argument, out int start, out int step))
                {
                    ShowError("AUTO : écrivez AUTO, AUTO début ou AUTO début,pas (numéros de 1 à 65535), par exemple AUTO 100,5.");
                    return;
                }
                autoStep = step;
                StartAutoLine(start, newLine: false);
                break;

            case DirectCommandKind.Renum:
                if (!Renumber.TryParse(command.Argument, out int newStart, out int? oldStart, out int renumStep))
                {
                    ShowError("RENUM : écrivez RENUM, ou RENUM nouveau,ancien,pas (numéros de 1 à 65535), par exemple RENUM 100,,5.");
                    return;
                }
                var renumbered = Renumber.Apply(Editor.Text, newStart, oldStart, renumStep);
                if (renumbered.Error is not null)
                {
                    ShowDuplicateError(renumbered.Error, renumbered.Duplicate);
                    return;
                }
                // Remplacement par le document : Ctrl+Z peut annuler RENUM
                int caretLine = Editor.TextArea.Caret.Line;
                Editor.Document.Replace(0, Editor.Document.TextLength, renumbered.Text!);
                Editor.TextArea.Caret.Line = Math.Min(caretLine, Editor.Document.LineCount);
                var renumMessages = new List<MessageItem>
                {
                    new($"RENUM : {renumbered.LineCount} ligne(s) renumérotée(s), avec les GOTO, GOSUB, THEN... corrigés. " +
                        "Ctrl+Z annule RENUM.", 0, MessageKind.Success)
                };
                renumMessages.AddRange(renumbered.Warnings.Select(w => new MessageItem(w, 0, MessageKind.Error)));
                ShowMessages(renumMessages);
                break;

            case DirectCommandKind.List:
                ListProgram(command.Argument);
                break;

            case DirectCommandKind.Delete:
                await DeleteLinesAsync(command.Argument);
                break;

            case DirectCommandKind.Explain:
                ShowMessage(command.Explanation!);
                break;

            default:
                ShowMessage($"{command.Name} : cette commande de l'éditeur arrive à une prochaine étape.");
                break;
        }
    }

    // Problème imprévu (D119), appelé par App : l'éditeur reste ouvert, le détail est dans erreurs.txt
    public void ReportInternalError(Exception ex, string? logPath)
    {
        autoNext = null;
        var items = new List<MessageItem>
        {
            new($"Problème interne : {ex.Message}", 0, MessageKind.Error),
            new("L'éditeur reste ouvert et votre programme n'est pas perdu ; pensez à l'enregistrer (Ctrl+S).", 0)
        };
        if (logPath is not null)
            items.Add(new($"Le détail est noté dans le fichier {logPath} : envoyez-le pour que le problème soit corrigé.", 0));
        ShowMessages(items);
    }

    // DELETE plage (D120) : toujours une question avant d'effacer ; Ctrl+Z remet tout d'un coup
    async Task DeleteLinesAsync(string argument)
    {
        if (!DirectCommands.TryParseRange(argument, out int from, out int to))
        {
            ShowError("DELETE : écrivez DELETE 30, DELETE 10-50, DELETE -50 (du début à 50), DELETE 100- (de 100 à la fin), " +
                      "ou DELETE seul (tout le programme).");
            return;
        }
        var lines = DirectCommands.LinesInRange(Editor.Text, from, to);
        if (lines.Count == 0)
        {
            ShowError(from == to
                ? $"DELETE {from} : la ligne {from} n'existe pas, rien n'est effacé."
                : "DELETE : aucune ligne dans cette plage, rien n'est effacé.");
            return;
        }

        var document = Editor.Document;
        string first = document.GetText(document.GetLineByNumber(lines[0])).Trim();
        string last = document.GetText(document.GetLineByNumber(lines[^1])).Trim();
        string what = argument.Trim().Length == 0 ? "tout le programme" : $"la plage {argument.Trim()}";
        string question = lines.Count == 1
            ? $"Effacer cette ligne ?\n\n{first}"
            : $"Effacer {lines.Count} lignes ({what}) ?\n\nPremière : {Shorten(first)}\nDernière : {Shorten(last)}";
        bool erase = await new ConfirmDialog("Effacer des lignes", question,
                lines.Count == 1 ? "Effacer la ligne" : $"Effacer les {lines.Count} lignes", "Ne rien effacer")
            .ShowDialog<bool>(this);
        if (!erase)
        {
            ShowMessage("DELETE annulé : rien n'est effacé.");
            return;
        }

        var toRemove = lines.Select(n => document.GetLineByNumber(n)).ToList();
        using (document.RunUpdate())
        {
            foreach (var line in Enumerable.Reverse(toRemove))
                document.Remove(line.Offset, line.TotalLength);
        }
        modified = Editor.Text != savedText;
        UpdateTitle();
        ShowMessages(new[] { new MessageItem($"DELETE : {lines.Count} ligne(s) effacée(s). Ctrl+Z les remet.", 0, MessageKind.Success) });
    }

    // LIST [plage] (D122) : range les lignes, les écrit comme le CPC, puis curseur sur la première ligne de la plage
    void ListProgram(string argument)
    {
        if (!DirectCommands.TryParseRange(argument, out int from, out int to))
        {
            ShowError("LIST : écrivez LIST, LIST 30, LIST 10-50, LIST -50 ou LIST 100-.");
            return;
        }
        var listed = Lister.Apply(Editor.Text);
        if (listed.Error is not null)
        {
            ShowDuplicateError(listed.Error, listed.Duplicate);
            return;
        }

        if (listed.Text != Editor.Text)
            Editor.Document.Replace(0, Editor.Document.TextLength, listed.Text!);   // Ctrl+Z annule LIST

        var inRange = DirectCommands.LinesInRange(Editor.Text, from, to);
        int target = inRange.Count > 0 ? inRange[0] : 1;
        Editor.TextArea.Caret.Offset = Editor.Document.GetLineByNumber(target).Offset;
        Editor.TextArea.Caret.BringCaretToView();

        var items = new List<MessageItem>
        {
            new($"LIST : {listed.LineCount} ligne(s) rangée(s) par numéro et écrite(s) comme le CPC. Ctrl+Z annule LIST.",
                0, MessageKind.Success)
        };
        if (argument.Trim().Length > 0 && inRange.Count == 0)
            items.Add(new("Aucune ligne dans cette plage : le curseur est au début du programme.", 0, MessageKind.Error));
        items.AddRange(listed.Warnings.Select(w => new MessageItem(w, 0, MessageKind.Error)));
        ShowMessages(items);
    }

    // Numéro en double (D122) : le message dit où sont les lignes ; Entrée ou clic sur l'une d'elles y amène le curseur
    void ShowDuplicateError(string error, int? number)
    {
        var items = new List<MessageItem> { new(error, 0, MessageKind.Error) };
        if (number is int n)
            foreach (int line in DirectCommands.LinesInRange(Editor.Text, n, n))
                items.Add(new($"Ligne {line} de l'éditeur : {Shorten(Editor.Document.GetText(Editor.Document.GetLineByNumber(line)).Trim())}", line));
        ShowMessages(items);
    }

    static string Shorten(string text) => text.Length <= 60 ? text : text[..60] + "...";

    // ---------- Numéro tapé seul, ou numéro déjà pris (D118) ----------

    async Task HandleExistingNumberAsync(AvaloniaEdit.Document.DocumentLine line, int number, bool numberOnly)
    {
        int? otherNumber = DirectCommands.FindLine(Editor.Text, number, line.LineNumber);
        var document = Editor.Document;

        if (numberOnly && otherNumber is null)
        {
            RemoveCommandLine(line);
            ShowMessage($"{number} : aucune autre ligne ne porte ce numéro ; le numéro seul est retiré, rien d'autre n'est effacé.");
            return;
        }

        var other = document.GetLineByNumber(otherNumber!.Value);
        string otherText = document.GetText(other).Trim();

        if (numberOnly)
        {
            bool erase = await new ConfirmDialog("Effacer une ligne",
                    $"Effacer la ligne {number} ?\n\n{otherText}", "Effacer la ligne", "Garder la ligne")
                .ShowDialog<bool>(this);
            // RunUpdate : un seul Ctrl+Z pour les deux effacements, et le groupe se ferme toujours, même en cas de problème
            using (document.RunUpdate())
            {
                if (erase)
                    document.Remove(other.Offset, other.TotalLength);
                document.Remove(line.Offset, line.TotalLength);   // le numéro tapé seul disparaît dans les deux cas
            }
            modified = Editor.Text != savedText;
            UpdateTitle();
            if (erase)
                ShowMessages(new[] { new MessageItem($"Ligne {number} effacée. Ctrl+Z la remet.", 0, MessageKind.Success) });
            else
                ShowMessage($"Ligne {number} gardée.");
            return;
        }

        string newText = document.GetText(line).Trim();
        bool replace = await new ConfirmDialog("Ligne déjà existante",
                $"La ligne {number} existe déjà :\n\n{otherText}\n\nLa remplacer par la nouvelle ?\n\n{newText}",
                "Remplacer l'ancienne ligne", "Garder les deux")
            .ShowDialog<bool>(this);
        if (replace)
        {
            document.Remove(other.Offset, other.TotalLength);
            Editor.TextArea.Caret.Offset = line.EndOffset;
            ShowMessages(new[] { new MessageItem($"L'ancienne ligne {number} est remplacée par la nouvelle. Ctrl+Z la remet.", 0, MessageKind.Success) });
        }
        else
            ShowError($"Deux lignes ont maintenant le numéro {number} : changez le numéro de l'une des deux avant de compiler.");
    }

    // ---------- AUTO (D116) ----------

    // Écrit le numéro de la ligne suivante (sur une nouvelle ligne après le curseur, ou à la place de la ligne AUTO).
    // Si ce numéro existe déjà, AUTO s'arrête au lieu d'écrire une deuxième ligne du même numéro.
    void StartAutoLine(int number, bool newLine)
    {
        if (number > 65535)
        {
            autoNext = null;
            ShowMessage("AUTO s'arrête : 65535 est le plus grand numéro de ligne.");
            return;
        }
        if (DirectCommands.FindLine(Editor.Text, number) is not null)
        {
            autoNext = null;
            ShowMessage($"AUTO s'arrête : la ligne {number} existe déjà (EDIT {number} pour y aller).");
            return;
        }

        var caret = Editor.TextArea.Caret;
        string text = $"{number} ";
        if (newLine)
        {
            // Le numéro de la nouvelle ligne est pris AVANT l'ajout : le curseur peut déjà avoir suivi le texte ajouté
            var current = Editor.Document.GetLineByNumber(caret.Line);
            int newLineNumber = current.LineNumber + 1;
            Editor.Document.Insert(current.EndOffset, Environment.NewLine + text);
            caret.Offset = Editor.Document.GetLineByNumber(newLineNumber).EndOffset;
            autoNext = number + autoStep;
            return;
        }
        else
        {
            // La ligne de la commande a été effacée : on écrit le numéro à sa place, sur une ligne à lui
            int offset = caret.Offset;
            var current = Editor.Document.GetLineByNumber(caret.Line);
            bool emptyLine = current.Length == 0;
            Editor.Document.Insert(offset, emptyLine ? text : text + Environment.NewLine);
            caret.Offset = offset + text.Length;
            autoNext = number + autoStep;
            ShowMessage($"AUTO : numéros {number}, {number + autoStep}, {number + 2 * autoStep}... " +
                        "Entrée passe à la ligne suivante ; ESC, ou Entrée sur une ligne sans rien après le numéro, arrête AUTO.");
            return;
        }
    }

    void ContinueAuto(AvaloniaEdit.Document.DocumentLine line)
    {
        string text = Editor.Document.GetText(line).Trim();
        // Rien après le numéro : AUTO s'arrête et le numéro seul disparaît (comme sur le CPC, la ligne n'est pas créée)
        if (text.Length == 0 || text.All(char.IsDigit))
        {
            StopAuto();
            return;
        }
        StartAutoLine(autoNext!.Value, newLine: true);
    }

    void StopAuto()
    {
        autoNext = null;
        var line = Editor.Document.GetLineByNumber(Editor.TextArea.Caret.Line);
        string text = Editor.Document.GetText(line).Trim();
        if (text.Length > 0 && text.All(char.IsDigit))
            Editor.Document.Replace(line.Offset, line.Length, "");
        ShowMessage("AUTO arrêté.");
    }

    void SetText(string text)
    {
        loading = true;
        autoNext = null;
        // Un document neuf (D119) : l'historique de Ctrl+Z repart de zéro sans dépendre de l'état de l'ancien document
        // (Editor.Text = ... refusait d'ouvrir un fichier : « No undo group should be open at this point »)
        Editor.Document = new AvaloniaEdit.Document.TextDocument(text);
        loading = false;
        savedText = text;
        modified = false;
    }

    void UpdateTitle()
    {
        string file = currentPath is null ? "sans titre" : Path.GetFileName(currentPath);
        Title = $"CPC BASIC IDE - {file}{(modified ? " *" : "")}";
    }

    void UpdateStatus()
    {
        var caret = Editor.TextArea.Caret;
        StatusText.Text = $"Ligne {caret.Line}, colonne {caret.Column}";
        StatusRight.Text = $"CPC {CharacterSet.Name(settings.CpcVersion)}   Largeur visible : {visibleColumns} colonnes";
    }
}
