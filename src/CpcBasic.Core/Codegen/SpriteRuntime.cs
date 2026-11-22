// Copyright (C) 2026 RetroPoke
// Ce module fait partie de CPC BASIC IDE.
// Il est publié sous la licence publique générale GNU, version 3, avec l'exception
// de liaison pour la bibliothèque d'exécution décrite dans docs/RUNTIME-EXCEPTION.md.
// Voir aussi le fichier LICENSE.

using CpcBasic.Core.Assembly;

namespace CpcBasic.Core.Codegen;

/// <summary>
/// Sprites (D198) : DEFSPR n, "ligne", ... donne l'image ; SPRITE n, x, y la montre (ou la déplace) en gardant le décor
/// dessous ; SPRITE n la cache et remet le décor. Coordonnées du MODE, coin en haut à gauche (comme BOX), au point près
/// (D199 : image décalée préparée une fois par décalage, puis gardée). Le compilateur prépare l'image des trois MODE
/// (Build) ; le module (Emit) est écrit en assembleur.
/// </summary>
public static class SpriteRuntime
{
    /// <summary>Numéros des sprites : de 1 à MaxNumber ; un dessin a au plus MaxSize points de large et de haut.</summary>
    public const int MaxNumber = 16, MaxSize = 16;

    public const string DefLabel = "rt_spr_DEF";        // A = numéro, HL = description
    public const string ShowLabel = "rt_spr_SHOW";      // rt_spr_SN, rt_spr_SX, rt_spr_SY
    public const string HideLabel = "rt_spr_HIDE";      // rt_spr_SN
    public const string NumberLabel = "rt_spr_SN";
    public const string XLabel = "rt_spr_SX";
    public const string YLabel = "rt_spr_SY";

    const string Source = """
                ; ---------- sprites (D198) : DEFSPR, SPRITE n, x, y ; SPRITE n ----------
                ; Chaque sprite (1 à 16) a 16 octets en TABLE + (n - 1) x 16 : +0 et +1 sa description (0 : pas défini),
                ; +2 montré (1) ou non (0), +3 et +4 adresse dans l'écran, +5 octets par ligne, +6 lignes, +8 à +11 x et y,
                ; +12 et +13 où est gardé le décor.
                ; Description (préparée par le compilateur) : largeur, hauteur (en points), adresse du décor gardé, adresse des
                ; images décalées (D199), MODE de ces images (&FF : aucun), images déjà prêtes (bit 0 : décalée d'un point...),
                ; puis pour les MODE 0, 1 et 2 : octets par ligne, puis pour chaque octet de chaque ligne, l'image et ce qui
                ; reste du décor.
                ; DEF : A = numéro, HL = description
        DEF:    PUSH IX
                PUSH HL
                CALL ENTRYA
                POP HL
                JR Z,DEFF
                LD (IX+0),L
                LD (IX+1),H
        DEFF:   POP IX
                RET
                ; IX = case du sprite A ; Z si A n'est pas de 1 à 16
        ENTRYA: DEC A
                CP 16
                JR NC,ENTNO
                LD L,A
                LD H,0
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                ADD HL,HL
                LD DE,TABLE
                ADD HL,DE
                PUSH HL
                POP IX
                OR 1
                RET
        ENTNO:  XOR A
                RET
                ; SPRITE SN, SX, SY : montré (ou déplacé) ; coin en haut à gauche en SX, SY (points du MODE)
        SHOW:   CALL &B903
                PUSH AF
                PUSH IX
                CALL SHOW1
                POP IX
                POP AF
                JP &B90C
                ; SPRITE SN : caché, le décor remis
        HIDE:   CALL &B903
                PUSH AF
                PUSH IX
                LD A,(SN)
                CALL ENTRYA
                CALL NZ,RESTORE
                POP IX
                POP AF
                JP &B90C
        SHOW1:  LD A,(SN)
                CALL ENTRYA
                RET Z
                LD L,(IX+0)
                LD H,(IX+1)
                LD A,H
                OR L
                RET Z                   ; pas défini
                CALL RESTORE            ; d'abord le décor remis à l'ancienne place
                LD L,(IX+0)
                LD H,(IX+1)
                LD (SPDESC),HL
                INC HL
                LD A,(HL)
                LD (SPH),A              ; hauteur
                INC HL
                LD E,(HL)
                INC HL
                LD D,(HL)
                INC HL
                LD (SPBG),DE            ; où garder le décor
                LD DE,4
                ADD HL,DE               ; les images décalées : voir SHPREP
                PUSH HL
                CALL &BC11              ; MODE : 0, 1 ou 2
                POP HL
                AND 3
                CP 3
                JR NZ,SH0
                XOR A
        SH0:    LD (SPMODE),A
                OR A
                JR Z,SH2
                LD B,A                  ; on saute les MODE d'avant
        SH1:    PUSH BC
                LD E,(HL)
                LD D,0
                PUSH HL
                LD HL,0
                LD A,(SPH)
                LD B,A
        SH1A:   ADD HL,DE
                DJNZ SH1A
                ADD HL,HL
                INC HL
                EX DE,HL
                POP HL
                ADD HL,DE
                POP BC
                DJNZ SH1
        SH2:    LD A,(HL)
                LD (SPWB),A             ; octets par ligne dans ce MODE
                INC HL
                LD (SPDATA),HL
                CALL SHPREP             ; x pas au début d'un octet : l'image décalée (D199)
                LD HL,(SX)              ; octet de départ : x / points par octet (dans l'écran, sinon pas montré)
                BIT 7,H
                RET NZ
                LD A,(SPMODE)
                INC A
                LD B,A
        SH3:    SRL H
                RR L
                DJNZ SH3
                LD A,H
                OR A
                RET NZ
                LD A,(SPWB)
                ADD A,L
                CP 81
                RET NC
                LD A,L
                LD (SPXB),A
                LD HL,(SY)
                LD A,H
                OR A
                RET NZ
                LD A,(SPH)
                ADD A,L
                RET C
                CP 201
                RET NC
                LD A,L                  ; adresse : &C000 + (y and 7) x &800 + (y / 8) x 80 + octet
                AND 7
                ADD A,A
                ADD A,A
                ADD A,A
                ADD A,&C0
                LD D,A
                LD E,0
                LD A,L
                AND &F8
                LD L,A
                LD H,0
                ADD HL,HL
                LD B,H
                LD C,L
                ADD HL,HL
                ADD HL,HL
                ADD HL,BC
                ADD HL,DE
                LD A,(SPXB)
                LD E,A
                LD D,0
                ADD HL,DE
                LD (IX+3),L             ; la case du sprite : où il est, sa taille, son décor
                LD (IX+4),H
                LD A,(SPWB)
                LD (IX+5),A
                LD A,(SPH)
                LD (IX+6),A
                LD DE,(SPBG)
                LD (IX+12),E
                LD (IX+13),D
                LD DE,(SX)
                LD (IX+8),E
                LD (IX+9),D
                LD DE,(SY)
                LD (IX+10),E
                LD (IX+11),D
                LD (IX+2),1
                LD DE,(SPBG)            ; décor gardé, puis l'image posée par-dessus
                LD IX,(SPDATA)
                LD A,(SPH)
                LD C,A
        SH4:    PUSH HL
                LD A,(SPWB)
                LD B,A
        SH5:    LD A,(HL)
                LD (DE),A
                AND (IX+1)
                OR (IX+0)
                LD (HL),A
                INC HL
                INC DE
                INC IX
                INC IX
                DJNZ SH5
                POP HL
                CALL NEXTL
                DEC C
                JR NZ,SH4
                RET
                ; ---- position au point près (D199) : x pas au début d'un octet -> l'image décalée de s points vers la droite,
                ; un octet de plus par ligne ; préparée une fois par décalage et gardée (place réservée par le compilateur) ----
        SHPREP: LD A,(SPMODE)
                LD E,A
                ADD A,A
                ADD A,E
                LD E,A
                LD D,0
                LD HL,SHTAB
                ADD HL,DE               ; points par octet - 1, points par octet, masque des points
                LD A,(SX)
                AND (HL)
                RET Z                   ; au début d'un octet : l'image telle quelle
                LD (SPS),A              ; s : décalage
                LD B,A
                INC HL
                LD A,(HL)
                SUB B
                LD (SPKS),A             ; points par octet - s
                INC HL
                LD C,(HL)
                LD A,&FF
        SHP1:   SRL A                   ; masque : la place des points venus de cet octet
                AND C
                DJNZ SHP1
                LD (SPM),A
                LD A,(SPWB)             ; taille d'une image décalée : (octets + 1) x hauteur x 2
                INC A
                LD E,A
                LD D,0
                LD HL,0
                LD A,(SPH)
                LD B,A
        SHP2:   ADD HL,DE
                DJNZ SHP2
                ADD HL,HL
                EX DE,HL                ; DE = taille
                LD HL,(SPDESC)
                LD BC,6
                ADD HL,BC
                LD A,(SPMODE)
                CP (HL)
                JR Z,SHP3
                LD (HL),A               ; autre MODE : les images gardées ne servent plus
                INC HL
                LD (HL),0
                DEC HL
        SHP3:   INC HL
                PUSH HL                 ; HL = images prêtes
                DEC HL
                DEC HL
                LD A,(HL)
                DEC HL
                LD L,(HL)
                LD H,A                  ; HL = début des images décalées
                LD A,(SPS)
                LD B,A
                LD C,1                  ; C = bit de ce décalage
                JR SHP5
        SHP4:   ADD HL,DE
                SLA C
        SHP5:   DJNZ SHP4
                LD (SPDATA2),HL
                POP HL
                LD A,(HL)
                AND C
                JR NZ,SHP6              ; déjà prête
                LD A,(HL)
                OR C
                LD (HL),A
                CALL SHIFT
        SHP6:   LD HL,(SPDATA2)
                LD (SPDATA),HL
                LD A,(SPWB)
                INC A
                LD (SPWB),A
                RET
                ; l'image (SPDATA, SPWB octets, SPH lignes) décalée de SPS points, rangée en SPDATA2
        SHIFT:  PUSH IX
                LD HL,(SPDATA)
                LD IX,(SPDATA2)
                LD A,(SPH)
                LD C,A
        SF1:    XOR A
                LD (SPPI),A
                DEC A
                LD (SPPK),A
                LD A,(SPWB)
                LD B,A
        SF2:    LD A,(SPPI)
                LD E,A
                LD A,(HL)
                LD (SPPI),A
                CALL COMB
                LD (IX+0),A
                INC HL
                LD A,(SPPK)
                LD E,A
                LD A,(HL)
                LD (SPPK),A
                CALL COMB
                LD (IX+1),A
                INC HL
                INC IX
                INC IX
                DJNZ SF2
                LD A,(SPPI)             ; un octet de plus : la fin de l'image
                LD E,A
                XOR A
                CALL COMB
                LD (IX+0),A
                LD A,(SPPK)
                LD E,A
                LD A,&FF
                CALL COMB
                LD (IX+1),A
                INC IX
                INC IX
                DEC C
                JR NZ,SF1
                POP IX
                RET
                ; A = ((A décalé de s) et M) ou ((E décalé de points par octet - s, vers la gauche) et non M) ; D, E changés
        COMB:   LD D,A
                LD A,(SPS)
        CB1:    SRL D
                DEC A
                JR NZ,CB1
                LD A,(SPKS)
        CB2:    SLA E
                DEC A
                JR NZ,CB2
                LD A,(SPM)
                AND D
                LD D,A
                LD A,(SPM)
                CPL
                AND E
                OR D
                RET
        SHTAB:  DEFB 1,2,&55,3,4,&77,7,8,&7F
                ; le décor du sprite IX remis s'il est montré
        RESTORE: LD A,(IX+2)
                OR A
                RET Z
                LD (IX+2),0
                LD L,(IX+3)
                LD H,(IX+4)
                LD E,(IX+12)
                LD D,(IX+13)
                LD C,(IX+6)
        RS1:    PUSH HL
                LD B,(IX+5)
        RS2:    LD A,(DE)
                LD (HL),A
                INC HL
                INC DE
                DJNZ RS2
                POP HL
                CALL NEXTL
                DEC C
                JR NZ,RS1
                RET
                ; HL = début de la ligne de points suivante
        NEXTL:  LD A,H
                ADD A,8
                LD H,A
                RET NC
                LD A,L
                ADD A,&50
                LD L,A
                LD A,H
                ADC A,&C0
                LD H,A
                RET
        SN:     DEFS 1
        SX:     DEFS 2
        SY:     DEFS 2
        SPH:    DEFS 1
        SPBG:   DEFS 2
        SPMODE: DEFS 1
        SPWB:   DEFS 1
        SPXB:   DEFS 1
        SPDATA: DEFS 2
        SPDATA2: DEFS 2
        SPDESC: DEFS 2
        SPS:    DEFS 1
        SPKS:   DEFS 1
        SPM:    DEFS 1
        SPPI:   DEFS 1
        SPPK:   DEFS 1
        TABLE:  DEFS 256
        """;

    /// <summary>Écrit le module dans le programme.</summary>
    public static void Emit(Z80Emitter emitter)
    {
        IReadOnlyList<AsmError> errors = Z80Assembler.Assemble(Source, emitter, "rt_spr_", _ => null);
        if (errors.Count > 0)
            throw new InvalidOperationException("Module des sprites : " + string.Join(" / ", errors.Select(e => e.ToString())));
    }

    /// <summary>
    /// L'image préparée pour les MODE 0, 1 et 2 : pour chacun, les octets par ligne, puis pour chaque octet de chaque
    /// ligne, l'image (les stylos placés comme le CPC les range) et ce qui reste du décor (bits à 1). Un « . » laisse
    /// voir le décor ; 0 à 9 et A à F : le stylo (gardé selon le MODE : 16, 4 ou 2 encres). BackgroundSize : place pour
    /// garder le décor (le MODE le plus large, plus un octet pour la position au point près). CacheSize : place pour les
    /// images décalées d'un MODE (D199 : 1, 3 ou 7 décalages, d'un octet de plus par ligne).
    /// </summary>
    public static (byte[] Blocks, int BackgroundSize, int CacheSize) Build(IReadOnlyList<string> rows)
    {
        int width = rows[0].Length, height = rows.Count, widest = 0, cache = 0;
        var blocks = new List<byte>();
        for (int mode = 0; mode < 3; mode++)
        {
            int perByte = 2 << mode;
            int bytes = (width + perByte - 1) / perByte;
            widest = Math.Max(widest, bytes);
            cache = Math.Max(cache, (perByte - 1) * (bytes + 1) * height * 2);
            blocks.Add((byte)bytes);
            foreach (string row in rows)
            {
                for (int b = 0; b < bytes; b++)
                {
                    int image = 0, keep = 0xFF;
                    for (int i = 0; i < perByte; i++)
                    {
                        int x = b * perByte + i;
                        if (x >= width || row[x] == '.')
                            continue;
                        int pen = Convert.ToInt32(row[x].ToString(), 16);
                        image |= Encode(mode, pen, i);
                        keep &= ~PixelMask(mode, i);
                    }
                    blocks.Add((byte)image);
                    blocks.Add((byte)(keep & 0xFF));
                }
            }
        }
        return (blocks.ToArray(), (widest + 1) * height, cache);
    }

    /// <summary>Les bits du point i (0 = à gauche) dans un octet de l'écran.</summary>
    public static int PixelMask(int mode, int i) => mode switch
    {
        2 => 0x80 >> i,
        1 => (0x80 >> i) | (0x08 >> i),
        _ => (0x80 >> i) | (0x20 >> i) | (0x08 >> i) | (0x02 >> i)
    };

    /// <summary>Le stylo pen pour le point i : bits du stylo entrelacés comme le CPC les range.</summary>
    public static int Encode(int mode, int pen, int i) => mode switch
    {
        2 => (pen & 1) << (7 - i),
        1 => ((pen & 1) << (7 - i)) | (((pen >> 1) & 1) << (3 - i)),
        _ => ((pen & 1) << (7 - i)) | (((pen >> 1) & 1) << (3 - i)) | (((pen >> 2) & 1) << (5 - i)) | (((pen >> 3) & 1) << (1 - i))
    };
}
