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
/// Pas encore faits : la manette (KM WAIT KEY attend sans fin, KM TEST KEY répond « pas appuyée »), PEN, PAPER,
/// les fenêtres, le son ; les autres adresses de la ROM répondent par un simple retour.
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
        BRFIN:  LD A,1
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
        ; ---------- clavier : pas encore de manette ----------
        TESTKEY: XOR A                  ; KM TEST KEY : pas appuyée
                RET
        LITCAR: OR A                    ; KM READ CHAR : pas de caractère
                RET
        ATTEND: JR ATTEND               ; KM WAIT KEY, KM WAIT CHAR : attente sans fin
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
                DEFW &BB09,LITCAR
                DEFW &BB06,ATTEND
                DEFW &BB18,ATTEND
                DEFW &BBB4,FLUX
                DEFW &BC0E,SETMODE
                DEFW 0
        POLICE: {1}
        PROG:
        """;

    /// <summary>Le système seul, pour un programme de la longueur donnée (sans le programme).</summary>
    public static byte[] SystemCode(int programLength)
    {
        var font = new StringBuilder();
        byte[] glyphs = Gx4000Font.Bytes();
        for (int i = 0; i < glyphs.Length; i += 8)
        {
            if (i > 0)
                font.Append("\n        ");
            font.Append("DEFB ").Append(string.Join(",", glyphs.Skip(i).Take(8).Select(b => $"&{b:X2}")));
        }
        string source = Template.Replace("{0}", programLength.ToString()).Replace("{1}", font.ToString());
        var emitter = new Z80Emitter(0);
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(source, emitter, "gx_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Système de la cartouche : " + string.Join(" / ", errors.Select(e => e.ToString())));
        return emitter.ToBytes();
    }

    /// <summary>La cartouche : notre système, le jeu de caractères, puis le programme compilé (chargé en &amp;4000).</summary>
    public static CartridgeImage Build(byte[] program)
    {
        byte[] system = SystemCode(program.Length);
        if (system.Length + program.Length > CartridgeImage.PageSize)
            throw new InvalidOperationException(
                $"Le programme fait {program.Length} octets : pour l'instant, une cartouche en accepte {CartridgeImage.PageSize - system.Length} au plus.");
        var cartridge = new CartridgeImage();
        cartridge.AddPage([.. system, .. program]);
        return cartridge;
    }
}
