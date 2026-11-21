// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Les programmes générés par le compilateur peuvent être publiés sous la licence
// de votre choix. Voir aussi le fichier LICENSE.

using System.Text;
using CpcBasic.Core.Assembly;
using CpcBasic.Core.Codegen;

namespace CpcBasic.Core.Output;

/// <summary>
/// Notre petit système pour la cartouche GX4000 (chemin A, D181 ; étape du texte, D182). Aucune ROM d'Amstrad :
/// la page 0 de la cartouche contient ce système, le jeu de caractères du projet et le programme compilé.
/// Au démarrage : écran préparé (CRTC, MODE 1, encres du CPC), nos routines branchées aux adresses de la ROM du CPC
/// (&amp;BB5A TXT OUTPUT, &amp;BB75 TXT SET CURSOR, &amp;BC0E SCR SET MODE...), le programme recopié en &amp;4000 et lancé.
/// Le programme compilé ne change pas : il appelle les mêmes adresses, nos routines répondent.
/// Les routines tournent depuis la cartouche (ROM basse, toujours branchée) ; leurs variables sont en &amp;BE00.
/// Manettes (D183) : JOY, et les touches de la table (tir 1 = espace, tir 2 = ENTRÉE, directions = flèches, plus les
/// touches d'INKEY placées par le compilateur). Pas encore faits : PEN, PAPER, les fenêtres, le son, la saisie
/// d'INPUT à la manette ; les autres adresses de la ROM répondent par un simple retour.
/// </summary>
public static class Gx4000System
{
    /// <summary>Adresses fixes des routines, pour les tests : TXT OUTPUT, SCR SET MODE, TXT SET CURSOR, TXT GET CURSOR.</summary>
    public const int TxtOutputEntry = 0x0040, SetModeEntry = 0x0043, SetCursorEntry = 0x0046, GetCursorEntry = 0x0049;

    /// <summary>Variables du système : colonne et ligne du curseur (à partir de 0), octets par caractère, largeur...</summary>
    public const int Variables = 0xBE00;

    const string Template = """
                DI
                JP DEPART
                DEFS 52                 ; jusqu'à &0038
        ; &0038 : interruption (si le programme en autorise) : rien à faire
                EI
                RET
                DEFS 6                  ; jusqu'à &0040
        ; &0040 : entrées fixes (tests)
                JP TXTOUT
                JP SETMODE
                JP SETCUR
                JP GETCUR
        DEPART: IM 1
                LD SP,&BFF0
                LD BC,&7F89             ; Gate Array : MODE 1, cartouche gardée en &0000, ROM haute coupée
                OUT (C),C
        ; CRTC : écran de 40 x 25 caractères en &C000
                LD HL,CRTC
                LD D,0
        CRTCL:  LD B,&BC
                OUT (C),D
                LD A,(HL)
                INC HL
                LD B,&BD
                OUT (C),A
                INC D
                LD A,D
                CP 14
                JR NZ,CRTCL
        ; Encres du CPC au démarrage : stylos 0 à 3, puis la bordure (stylo 16)
                LD HL,ENCRES
                LD D,0
                LD B,&7F
        ENCRE:  OUT (C),D
                LD A,(HL)
                INC HL
                OUT (C),A
                INC D
                LD A,D
                CP 4
                JR NZ,ENCRE
                LD A,16
                OUT (C),A
                LD A,(HL)
                OUT (C),A
        ; Adresses de la ROM du CPC : un simple retour partout, puis nos routines là où elles existent
                LD HL,&BB00
                LD DE,&BB01
                LD BC,&02FF
                LD (HL),&C9
                LDIR
                LD HL,BRANCHE
        BRL:    LD E,(HL)
                INC HL
                LD D,(HL)
                INC HL
                LD A,D
                OR E
                JR Z,BRFIN
                LD A,&C3
                LD (DE),A
                INC DE
                LDI
                LDI
                JR BRL
        BRFIN:  LD HL,&BE00             ; variables du système à zéro
                LD DE,&BE01
                LD BC,15
                LD (HL),0
                LDIR
                LD A,1
                CALL SETMODE
        ; Le programme compilé, recopié en &4000, puis lancé
                LD HL,PROG
                LD DE,&4000
                LD BC,{0}
                LDIR
                CALL &4000
        FIN:    JR FIN                  ; programme fini : la console reste sur son écran

        ; ---------- SCR SET MODE (&BC0E) : A = mode ; écran effacé, curseur en haut à gauche ----------
        SETMODE: AND 3
                CP 3
                JR NZ,SETM1
                XOR A                   ; MODE 3 : comme le MODE 0
        SETM1:  LD (&BE06),A
                LD C,A
                OR &88
                LD B,&7F
                OUT (C),A
                LD A,C
                ADD A,A
                ADD A,A
                LD E,A
                LD D,0
                LD HL,MODES
                ADD HL,DE
                LD DE,&BE02
                LD BC,4
                LDIR
        ; ---------- TXT CLEAR WINDOW (&BB6C) : écran effacé, curseur en haut à gauche ----------
        CLS:    LD HL,&C000
                LD DE,&C001
                LD BC,&3FFF
                LD (HL),0
                LDIR
                XOR A
                LD (&BE00),A
                LD (&BE01),A
                RET
        ; par mode : octets par caractère, caractères par ligne, masque et décalage d'un octet d'écran
        MODES:  DEFB 4,20,&C0,2
                DEFB 2,40,&F0,4
                DEFB 1,80,&FF,0

        ; ---------- TXT OUTPUT (&BB5A) : A = caractère ou code de contrôle ; tous les registres gardés ----------
        TXTOUT: PUSH AF
                PUSH BC
                PUSH DE
                PUSH HL
                CP 32
                JR C,CTRL
                CP 127
                JR C,TXT1
                LD A,32                 ; pas encore de dessin au-delà de 126 : un espace
        TXT1:   LD C,A
                PUSH BC
                CALL WRAP
                POP BC
                CALL ADDR               ; HL = adresse dans l'écran
                LD A,C
                SUB 32
                LD E,A
                LD D,0
                EX DE,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD BC,POLICE
                ADD HL,BC
                EX DE,HL                ; DE = dessin du caractère, HL = écran
                LD B,8
        LIGNE:  PUSH BC
                PUSH HL
                LD A,(DE)
                INC DE
                LD C,A
                LD A,(&BE02)
                LD B,A                  ; octets par caractère
        OCTET:  LD A,(&BE04)
                AND C
                LD (HL),A
                INC HL
                LD A,(&BE05)
                OR A
                JR Z,OCTET2
        DECAL:  SLA C
                DEC A
                JR NZ,DECAL
        OCTET2: DJNZ OCTET
                POP HL
                LD A,H
                ADD A,8                 ; ligne de points suivante : +&800
                LD H,A
                POP BC
                DJNZ LIGNE
                LD HL,&BE00
                INC (HL)                ; colonne suivante
                JR TXTFIN
        CTRL:   CP 13
                JR NZ,CTRL1
                XOR A
                LD (&BE00),A            ; retour au début de la ligne
                JR TXTFIN
        CTRL1:  CP 10
                JR NZ,CTRL2
                CALL BAS                ; ligne suivante
                JR TXTFIN
        CTRL2:  CP 12
                JR NZ,CTRL3
                CALL CLS
                JR TXTFIN
        CTRL3:  CP 8
                JR NZ,CTRL4
                LD HL,&BE00             ; un caractère à gauche
                LD A,(HL)
                OR A
                JR Z,TXTFIN
                DEC (HL)
                JR TXTFIN
        CTRL4:  CP 9
                JR NZ,TXTFIN
                LD HL,&BE00             ; un caractère à droite
                INC (HL)
        TXTFIN: POP HL
                POP DE
                POP BC
                POP AF
                RET

        ; au bout de la ligne : début de la ligne suivante
        WRAP:   LD A,(&BE03)
                LD B,A
                LD A,(&BE00)
                CP B
                RET C
                XOR A
                LD (&BE00),A
        ; ligne suivante ; en bas de l'écran, tout remonte d'une ligne
        BAS:    LD HL,&BE01
                LD A,(HL)
                CP 24
                JR NC,MONTE
                INC (HL)
                RET
        MONTE:  LD HL,&C050
                LD DE,&C000
                LD A,8
        MONTE1: PUSH AF
                PUSH HL
                PUSH DE
                LD BC,1920
                LDIR
                LD H,D
                LD L,E
                INC DE
                LD (HL),0
                LD BC,79
                LDIR
                POP DE
                POP HL
                LD A,H
                ADD A,8
                LD H,A
                LD A,D
                ADD A,8
                LD D,A
                POP AF
                DEC A
                JR NZ,MONTE1
                RET

        ; HL = adresse dans l'écran du curseur : &C000 + ligne x 80 + colonne x octets par caractère
        ADDR:   LD A,(&BE01)
                LD L,A
                LD H,0
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD D,H
                LD E,L
                ADD HL,HL
                ADD HL,HL
                ADD HL,DE
                LD A,(&BE00)
                LD E,A
                LD D,0
                LD A,(&BE02)
        ADDR1:  ADD HL,DE
                DEC A
                JR NZ,ADDR1
                LD DE,&C000
                ADD HL,DE
                RET

        ; ---------- TXT SET CURSOR (&BB75) : H = colonne, L = ligne (à partir de 1) ----------
        SETCUR: PUSH HL
                DEC H
                DEC L
                LD A,H
                LD (&BE00),A
                LD A,L
                LD (&BE01),A
                POP HL
                RET
        ; ---------- TXT GET CURSOR (&BB78) : H = colonne, L = ligne (à partir de 1), A = 0 ----------
        GETCUR: LD A,(&BE00)
                INC A
                LD H,A
                LD A,(&BE01)
                INC A
                LD L,A
                XOR A
                RET
        ; ---------- TXT GET WINDOW (&BB69) : tout l'écran ----------
        FENETRE: LD A,(&BE03)
                DEC A
                LD D,A
                LD HL,0
                LD E,24
                OR A
                RET
        ; ---------- TXT VALIDATE (&BB87) : la position convient ----------
        VALIDE: SCF
                RET
        ; ---------- manettes (D183) : lues comme le CPC, par la grille du clavier (lignes 9 et 6) ----------
        ; &BE07 = manette 1, &BE08 = manette 2 : bit à 1 = appuyé (0 haut, 1 bas, 2 gauche, 3 droite, 4 tir 2, 5 tir 1)
        LIRE:   LD BC,&F40E
                OUT (C),C
                LD BC,&F6C0
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                LD BC,&F792
                OUT (C),C
                LD BC,&F649
                OUT (C),C
                LD B,&F4
                IN A,(C)
                CPL
                AND &3F
                LD (&BE07),A
                LD BC,&F646
                OUT (C),C
                LD B,&F4
                IN A,(C)
                CPL
                AND &3F
                LD (&BE08),A
                LD BC,&F782
                OUT (C),C
                LD BC,&F600
                OUT (C),C
                RET
        ; ---------- KM GET JOYSTICK (&BB24) : H = manette 1, L = manette 2, A = manette 1 ----------
        JOYST:  PUSH BC
                CALL LIRE
                POP BC
                LD A,(&BE08)
                LD L,A
                LD A,(&BE07)
                LD H,A
                RET
        ; ---------- KM TEST KEY (&BB1E) : A = touche ; Z faux si appuyée (table des touches), C = 0 ----------
        TESTKEY: PUSH DE
                PUSH BC
                LD E,A
                CALL LIRE
                LD HL,TOUCHES
        TK1:    LD A,(HL)
                CP &FF
                JR Z,TKNON
                CP E
                JR Z,TK2
                INC HL
                INC HL
                INC HL
                JR TK1
        TK2:    INC HL
                LD A,(HL)               ; 0 = manette 1, 1 = manette 2
                INC HL
                LD D,(HL)               ; boutons à appuyer ensemble
                OR A
                LD A,(&BE07)
                JR Z,TK3
                LD A,(&BE08)
        TK3:    AND D
                CP D
                JR NZ,TKNON
                POP BC
                POP DE
                LD C,0
                OR &FF
                RET
        TKNON:  POP BC
                POP DE
                LD C,0
                XOR A
                RET
        ; ---------- KM READ CHAR (&BB09) / KM READ KEY (&BB1B) : un nouvel appui de la manette 1 ----------
        ; tir 1 = espace, tir 2 = ENTRÉE (13), directions = flèches (&F0 à &F3) ; KEY : numéros de touches
        LITCAR: PUSH HL
                LD HL,CARS
                JR LIT
        LITCLE: PUSH HL
                LD HL,CLES
        LIT:    PUSH BC
                CALL LIRE
                LD A,(&BE09)
                CPL
                LD C,A
                LD A,(&BE07)
                LD (&BE09),A
                AND C                   ; boutons qui viennent d'être appuyés
                LD B,6
        LIT1:   RRCA
                JR C,LIT2
                INC HL
                DJNZ LIT1
                POP BC
                POP HL
                OR A                    ; pas de caractère
                RET
        LIT2:   LD A,(HL)
                POP BC
                POP HL
                SCF
                RET
        CARS:   DEFB &F0,&F1,&F2,&F3,13,32
        CLES:   DEFB 0,2,8,1,18,47
        ; ---------- KM WAIT CHAR (&BB06) / KM WAIT KEY (&BB18) : attend un appui ----------
        ATTCAR: CALL LITCAR
                JR NC,ATTCAR
                RET
        ATTCLE: CALL LITCLE
                JR NC,ATTCLE
                RET
        FLUX:   XOR A                   ; TXT STR SELECT : flux 0
                RET

        CRTC:   DEFB 63,40,46,&8E,38,0,25,30,0,7,0,0,&30,0
        ENCRES: DEFB &44,&4A,&53,&4C,&44  ; bleu, jaune vif, bleu ciel vif, rouge vif ; bordure : bleu
        BRANCHE: DEFW &BB5A,TXTOUT
                DEFW &BB6C,CLS
                DEFW &BB75,SETCUR
                DEFW &BB78,GETCUR
                DEFW &BB69,FENETRE
                DEFW &BB87,VALIDE
                DEFW &BB1E,TESTKEY
                DEFW &BB24,JOYST
                DEFW &BB09,LITCAR
                DEFW &BB1B,LITCLE
                DEFW &BB06,ATTCAR
                DEFW &BB18,ATTCLE
                DEFW &BBB4,FLUX
                DEFW &BC0E,SETMODE
                DEFW 0
        TOUCHES: {2}
        POLICE: {1}
        PROG:
        """;

    /// <summary>Une touche du CPC reliée à la manette : Joystick 0 ou 1, Buttons = boutons à appuyer ensemble.</summary>
    public sealed record KeyLink(int Key, int Joystick, int Buttons);

    /// <summary>
    /// Correspondance de base (D183) : tir 1 = espace (47), tir 2 = ENTRÉE (18 et 6), directions = flèches (0, 2, 8, 1),
    /// et les touches de la manette elles-mêmes (72 à 77 pour la manette 1, 48 à 53 pour la manette 2).
    /// </summary>
    public static readonly IReadOnlyList<KeyLink> BaseKeys =
    [
        new(47, 0, 0x20), new(18, 0, 0x10), new(6, 0, 0x10),
        new(0, 0, 0x01), new(2, 0, 0x02), new(8, 0, 0x04), new(1, 0, 0x08),
        new(72, 0, 0x01), new(73, 0, 0x02), new(74, 0, 0x04), new(75, 0, 0x08), new(76, 0, 0x10), new(77, 0, 0x20),
        new(48, 1, 0x01), new(49, 1, 0x02), new(50, 1, 0x04), new(51, 1, 0x08), new(52, 1, 0x10), new(53, 1, 0x20)
    ];

    /// <summary>
    /// Boutons donnés, dans l'ordre, aux autres touches d'INKEY(n) (idée de l'utilisateur, D183) : tir 2, tirs 1 et 2
    /// ensemble, puis la manette 2.
    /// </summary>
    public static readonly IReadOnlyList<(int Joystick, int Buttons, string Name)> FreeButtons =
    [
        (0, 0x10, "au tir 2 de la manette 1"),
        (0, 0x30, "aux tirs 1 et 2 de la manette 1 appuyés ensemble"),
        (1, 0x01, "au haut de la manette 2"), (1, 0x02, "au bas de la manette 2"),
        (1, 0x04, "à la gauche de la manette 2"), (1, 0x08, "à la droite de la manette 2"),
        (1, 0x20, "au tir 1 de la manette 2"), (1, 0x10, "au tir 2 de la manette 2"),
        (1, 0x30, "aux tirs 1 et 2 de la manette 2 appuyés ensemble")
    ];

    /// <summary>Le système seul, pour un programme de la longueur donnée (sans le programme).</summary>
    public static byte[] SystemCode(int programLength, IEnumerable<KeyLink>? extraKeys = null)
    {
        string keys = string.Join(",", BaseKeys.Concat(extraKeys ?? []).Select(k => $"{k.Key},{k.Joystick},&{k.Buttons:X2}")) + ",&FF";
        var font = new StringBuilder();
        byte[] glyphs = Gx4000Font.Bytes();
        for (int i = 0; i < glyphs.Length; i += 8)
        {
            if (i > 0)
                font.Append("\n        ");
            font.Append("DEFB ").Append(string.Join(",", glyphs.Skip(i).Take(8).Select(b => $"&{b:X2}")));
        }
        string source = Template.Replace("{0}", programLength.ToString()).Replace("{1}", font.ToString())
                                .Replace("{2}", "DEFB " + keys);
        var emitter = new Z80Emitter(0);
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(source, emitter, "gx_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Système de la cartouche : " + string.Join(" / ", errors.Select(e => e.ToString())));
        return emitter.ToBytes();
    }

    /// <summary>La cartouche : notre système, le jeu de caractères, puis le programme compilé (chargé en &amp;4000).</summary>
    public static CartridgeImage Build(byte[] program, IEnumerable<KeyLink>? extraKeys = null)
    {
        byte[] system = SystemCode(program.Length, extraKeys);
        if (system.Length + program.Length > CartridgeImage.PageSize)
            throw new InvalidOperationException(
                $"Le programme fait {program.Length} octets : pour l'instant, une cartouche en accepte {CartridgeImage.PageSize - system.Length} au plus.");
        var cartridge = new CartridgeImage();
        cartridge.AddPage([.. system, .. program]);
        return cartridge;
    }
}
