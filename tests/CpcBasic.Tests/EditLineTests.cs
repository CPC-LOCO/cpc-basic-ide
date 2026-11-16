// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot H5 (D076) : édition de la ligne tapée pendant INPUT et LINE INPUT, comme le vrai BASIC (essais dans SugarBox,
/// 15 nov. 2026 : BONJOUR, 3 x gauche, X -> BONJXOUR ; CLR -> BONJUR ; DEL -> BONOUR ; COPY recopie l'écran).
/// Codes des touches : gauche &amp;F2, droite &amp;F3, haut &amp;F0, bas &amp;F1, SHIFT + flèches &amp;F4 à &amp;F7,
/// CTRL + gauche / droite &amp;FA / &amp;FB, COPY &amp;E0, CLR &amp;10, DEL &amp;7F.
/// </summary>
public class EditLineTests
{
    const string Left = "ò", Right = "ó", Up = "ð", Down = "ñ";
    const string ShiftUp = "ô", ShiftDown = "õ", ShiftLeft = "ö", ShiftRight = "÷";
    const string CtrlLeft = "ú", CtrlRight = "û", Copy = "à", Clr = "\u0010", Del = "\u007F";

    static MiniZ80 Run(string typed, MiniZ80? cpu = null)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 LINE INPUT A$\n20 PRINT \"[\"; A$; \"]\"");
        Assert.False(analysis.HasErrors);
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        cpu ??= new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in typed)
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);
        return cpu;
    }

    // Ce que PRINT a affiché : la dernière ligne
    static string Read(MiniZ80 cpu)
    {
        string output = cpu.Output.ToString();
        int start = output.LastIndexOf('[');
        return output[(start + 1)..output.LastIndexOf(']')].Replace("\r\n", "");   // fenêtre étroite : PRINT passe à la ligne
    }

    [Theory]
    [InlineData("BONJOUR", "BONJOUR")]
    [InlineData("BONJOUR" + Left + Left + Left + "X", "BONJXOUR")]          // une lettre s'insère
    [InlineData("BONJOUR" + Left + Left + Left + Clr, "BONJUR")]            // CLR : sous le curseur
    [InlineData("BONJOUR" + Left + Left + Left + Del, "BONOUR")]            // DEL : à gauche
    [InlineData("BONJOUR" + CtrlLeft + "A", "ABONJOUR")]                    // CTRL + gauche : au début
    [InlineData("BONJOUR" + CtrlLeft + CtrlRight + "!", "BONJOUR!")]        // CTRL + droite : à la fin
    [InlineData("AB" + Left + Left + Left + Del + "X", "XAB")]              // rien avant le début
    [InlineData("AB" + Right + Clr + "C", "ABC")]                           // rien après la fin
    [InlineData("ABC" + Left + Right + "D", "ABCD")]
    [InlineData("ABC" + Up + "X", "ABCX")]                                  // pas une largeur de ligne au-dessus
    [InlineData("ABC" + CtrlLeft + Down + "X", "XABC")]                     // ni au-dessous
    [InlineData(Copy + "A", "A")]                                           // COPY sans curseur de copie : rien
    public void Edition_de_la_ligne(string typed, string read)
    {
        Assert.Equal(read, Read(Run(typed + "\r")));
    }

    [Fact]
    public void Haut_et_bas_d_une_largeur_d_ecran()
    {
        var cpu = new MiniZ80 { WindowWidth = 10 };
        // 12 lettres dans une fenêtre de 10 colonnes : haut revient 10 places en arrière
        Assert.Equal("AXBCDEFGHIJKL", Read(Run("ABCDEFGHIJKL" + Left + Up + "X\r", cpu)));
    }

    [Fact]
    public void COPY_recopie_l_ecran_sous_le_curseur_de_copie()
    {
        var cpu = new MiniZ80();
        cpu.ScreenChars[(3, 2)] = (byte)'O';   // « ? » avant la saisie : colonne 3 (D093)
        cpu.ScreenChars[(4, 2)] = (byte)'K';
        // le curseur de copie part de la position du curseur (colonne 3, après « ? »), descend d'une ligne, puis COPY 2 fois
        Assert.Equal("OK", Read(Run(ShiftDown + Copy + Copy + "\r", cpu)));
    }

    [Fact]
    public void COPY_puis_frappe_et_curseur_de_copie_deplace()
    {
        var cpu = new MiniZ80();
        cpu.ScreenChars[(4, 3)] = (byte)'Z';
        Assert.Equal("AZ", Read(Run("A" + ShiftDown + ShiftDown + Copy + "\r", cpu)));   // part de la colonne 4 (après « ? A »)
        cpu = new MiniZ80();
        cpu.ScreenChars[(3, 1)] = (byte)'Q';
        Assert.Equal("Q", Read(Run(ShiftDown + ShiftUp + ShiftRight + ShiftLeft + Copy + "\r", cpu)));
    }

    [Fact]
    public void COPY_sur_une_case_vide_ne_fait_rien()
    {
        Assert.Equal("", Read(Run(ShiftUp + Copy + "\r")));
    }

    [Fact]
    public void INPUT_d_un_nombre_avec_edition()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 INPUT A\n20 PRINT A");
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, 0x4000);
        var cpu = new MiniZ80();
        cpu.Load(program.Code, 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        foreach (char key in "15" + Left + "2\r")
            cpu.KeyQueue.Enqueue((byte)key);
        cpu.Call(0x4000);

        Assert.EndsWith(" 125 \r\n", cpu.Output.ToString());
    }
}
