// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Linq;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Output;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D126 : table des caractères du CPC français (par défaut) et du CPC anglais, relevée dans les ROM OS6128.ROM et
/// OS6128-AZERTY.rom (18 nov. 2026). Vérifié dans WinAPE, ROM AZERTY : PRINT CHR$(123) affiche « é ».
/// </summary>
public class CharacterSetTests
{
    [Theory]
    [InlineData(64, 'à', '@')]
    [InlineData(92, 'ç', '\\')]
    [InlineData(94, '^', '↑')]
    [InlineData(123, 'é', '{')]
    [InlineData(124, 'ù', '|')]
    [InlineData(125, 'è', '}')]
    [InlineData(162, '°', '¨')]
    [InlineData(65, 'A', 'A')]
    [InlineData(163, '£', '£')]
    [InlineData(243, '→', '→')]
    public void Les_codes_affichent_le_caractere_de_la_ROM(int code, char french, char english)
    {
        Assert.Equal(french, CharacterSet.Glyph(code, CpcLanguage.French));
        Assert.Equal(english, CharacterSet.Glyph(code, CpcLanguage.English));
    }

    [Theory]
    [InlineData('é', CpcLanguage.French, 123)]
    [InlineData('é', CpcLanguage.English, 123)]    // n'existe pas sur le CPC anglais : le code du CPC français (« { »)
    [InlineData('{', CpcLanguage.French, 123)]     // une touche du clavier garde son code
    [InlineData('à', CpcLanguage.French, 64)]
    [InlineData('°', CpcLanguage.French, 162)]
    [InlineData('¨', CpcLanguage.English, 162)]
    [InlineData('↑', CpcLanguage.English, 94)]
    [InlineData('↑', CpcLanguage.French, 240)]
    [InlineData('£', CpcLanguage.French, 163)]
    [InlineData('½', CpcLanguage.English, 169)]
    public void Code_d_un_caractere(char c, CpcLanguage language, int code)
    {
        Assert.Equal(code, CharacterSet.CodeOf(c, language));
    }

    [Theory]
    [InlineData('ê')]
    [InlineData('â')]
    [InlineData('€')]
    public void Un_caractere_absent_de_tous_les_CPC_n_a_pas_de_code(char c)
    {
        Assert.Null(CharacterSet.CodeOf(c, CpcLanguage.French));
        Assert.Null(CharacterSet.CodeOf(c, CpcLanguage.English));
    }

    static bool Contains(byte[] code, byte[] part) =>
        Enumerable.Range(0, code.Length - part.Length + 1).Any(i => code.Skip(i).Take(part.Length).SequenceEqual(part));

    [Theory]
    [InlineData(CpcLanguage.French)]
    [InlineData(CpcLanguage.English)]
    public void Un_texte_accentue_est_compile_avec_les_codes_du_CPC(CpcLanguage language)
    {
        var options = new CompilerOptions(Language: language);
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 PRINT \"été à 20°\"", options);
        Assert.False(analysis.HasErrors);

        byte[] code = CodeGenerator.Generate(analysis.Program, 0x4000, options).Code;

        Assert.True(Contains(code, [123, (byte)'t', 123, (byte)' ', 64, (byte)' ', (byte)'2', (byte)'0', 162, 13, 10, 0]));
    }

    [Theory]
    [InlineData("10 PRINT \"fête\"", "ê", "e")]
    [InlineData("10 DATA \"rêve\",1", "ê", "e")]
    [InlineData("10 A$=\"5 €\"", "€", "")]
    public void Un_caractere_absent_de_tous_les_CPC_est_signale(string source, string c, string plain)
    {
        Diagnostic error = Assert.Single(BasicAnalyzer.Analyze(source).Diagnostics);

        Assert.Equal(DiagnosticCode.UnknownCharacter, error.Code);
        Assert.Contains($"« {c} » n'existe pas sur le CPC français", error.Message);
        if (plain.Length > 0)
            Assert.Contains($"Écrivez par exemple « {plain} »", error.Message);
    }

    [Fact]
    public void Le_message_nomme_la_version_choisie()
    {
        Diagnostic error = Assert.Single(BasicAnalyzer.Analyze("10 PRINT \"ê\"", new CompilerOptions(Language: CpcLanguage.English)).Diagnostics);

        Assert.Contains("n'existe pas sur le CPC anglais", error.Message);
    }

    // ---------- Éditeur (D126, partie 2) : touche F7 et caractères tapés ----------

    [Theory]
    [InlineData('{', CpcLanguage.French, 'é')]
    [InlineData('@', CpcLanguage.French, 'à')]
    [InlineData('é', CpcLanguage.French, 'é')]
    [InlineData('é', CpcLanguage.English, '{')]
    [InlineData('^', CpcLanguage.English, '↑')]
    [InlineData('°', CpcLanguage.English, '¨')]
    [InlineData('£', CpcLanguage.English, '£')]
    [InlineData('ê', CpcLanguage.French, 'ê')]     // absent de tous les CPC : reste tel quel (la compilation le signale)
    [InlineData('A', CpcLanguage.French, 'A')]
    public void Un_caractere_tape_montre_ce_qu_affiche_le_CPC(char typed, CpcLanguage language, char shown)
    {
        Assert.Equal(shown, CharacterSet.Shown(typed, language));
    }

    [Fact]
    public void F7_convertit_seulement_les_textes_entre_guillemets()
    {
        const string french = "10 PRINT \"été à 20°\";A$:|DISC\r\n20 A$=\"ça\":B=2^3 ' é hors texte";
        const string english = "10 PRINT \"{t{ @ 20¨\";A$:|DISC\r\n20 A$=\"\\a\":B=2^3 ' é hors texte";

        Assert.Equal(english, CharacterSet.ConvertProgram(french, CpcLanguage.French, CpcLanguage.English));
        Assert.Equal(french, CharacterSet.ConvertProgram(english, CpcLanguage.English, CpcLanguage.French));
    }

    [Fact]
    public void F7_aller_et_retour_rend_le_meme_programme()
    {
        // (une flèche ↑ tapée sur le CPC français, code 240, reviendrait en ^ : les deux s'affichent ↑ sur le CPC anglais)
        const string source = "10 PRINT \"^ é à ç ù è ° £ ê\"";
        string back = CharacterSet.ConvertProgram(
            CharacterSet.ConvertProgram(source, CpcLanguage.French, CpcLanguage.English), CpcLanguage.English, CpcLanguage.French);

        Assert.Equal(source, back);
    }

    [Theory]
    [InlineData("10 PRINT \"abc", 12, true)]
    [InlineData("10 PRINT \"abc\";", 15, false)]
    [InlineData("10 PRINT ", 9, false)]
    public void Le_curseur_est_il_dans_un_texte(string line, int position, bool inside)
    {
        Assert.Equal(inside, CharacterSet.IsInString(line, position));
    }

    [Fact]
    public void Le_texte_pour_le_vrai_BASIC_suit_la_version()
    {
        var disk = new DiskImage();
        disk.AddTextFile("essai", "txt", "10 PRINT \"ù£ê\"", CpcLanguage.English);

        string names = string.Join(",", disk.FileNames);
        Assert.Equal("ESSAI.TXT", names);
        Assert.Equal(new byte[] { 124, 163, (byte)'?' }, CharacterSet.Encode("ù£ê", CpcLanguage.English));
    }
}
