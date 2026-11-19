// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using CpcBasic.Core.Libraries;

namespace CpcBasic.Tests;

/// <summary>
/// STAR, première commande de la bibliothèque GRAPHIQUE du projet (D150, D151 ; intérieur D152), lue dans le dossier bibliotheques du dépôt.
/// Les traits attendus viennent du simulateur Python du texte assembleur (essais\star\sim.py) ; ils sont aussi comparés
/// au calcul exact avec virgule (moins d'un point d'écart).
/// </summary>
public class StarTests
{
    const int Origin = 0x4000;

    // dossier « bibliotheques » du dépôt, en remontant depuis le dossier des tests
    static string ProjectLibraries()
    {
        for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            string candidate = Path.Combine(folder.FullName, "bibliotheques");
            if (Directory.Exists(Path.Combine(candidate, "GRAPHIQUE")))
                return candidate;
        }
        throw new DirectoryNotFoundException("dossier bibliotheques introuvable");
    }

    static readonly LibraryCatalog Catalog = LibraryCatalog.Load(ProjectLibraries(), null);

    static MiniZ80 Run(string source, byte mode)
    {
        var options = new CompilerOptions(Libraries: Catalog);
        AnalysisResult analysis = BasicAnalyzer.Analyze(source, options);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        GeneratedProgram program = CodeGenerator.Generate(analysis.Program, Origin, options);
        var cpu = new MiniZ80 { ScreenMode = mode };
        cpu.Load(program.Code, Origin);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(Origin);
        return cpu;
    }

    static string Lines(MiniZ80 cpu) => string.Join(" ", cpu.Lines.Select(l => $"{l.X1},{l.Y1},{l.X2},{l.Y2},{l.Pen}"));

    [Fact]
    public void STAR_est_au_catalogue_sans_erreur()
    {
        Assert.Empty(Catalog.Errors);
        LibraryCommand star = Catalog.Find("STAR")!;
        Assert.Equal("GRAPHIQUE", star.Library);
        Assert.True(star.Protected);
        Assert.Equal(new[] { "X", "Y", "R1", "R2", "POINTES", "STYLO", "REMPLISSAGE", "ANGLE" }, star.Parameters.Select(p => p.Name));
        Assert.Equal(16, star.Parameters[4].Max);                // 16 pointes au plus (D153)
        Assert.Equal(3, star.Parameters.Count(p => p.IsOptional));
    }

    [Theory]
    [InlineData("10 STAR 160,100,30,80,5", 1,
        "320,358,282,246,1 282,246,156,248,1 156,248,258,180,1 258,180,218,68,1 218,68,320,138,1 320,138,422,68,1 " +
        "422,68,382,180,1 382,180,484,248,1 484,248,358,246,1 358,246,320,358,1")]
    [InlineData("10 STAR 80,100,40,99,7,3,,33", 0,
        "204,364,248,240,3 248,240,108,218,3 108,218,240,170,3 240,170,172,56,3 172,56,292,122,3 292,122,348,2,3 " +
        "348,2,368,130,3 368,130,504,96,3 504,96,404,190,3 404,190,520,266,3 520,266,380,256,3 380,256,388,386,3 " +
        "388,386,308,278,3 308,278,204,364,3")]
    [InlineData("10 STAR 320,100,50,90,6,,,-725", 2,
        "337,378,274,288,1 274,288,161,302,1 161,302,213,206,1 213,206,144,122,1 144,122,258,116,1 258,116,303,18,1 " +
        "303,18,366,108,1 366,108,479,94,1 479,94,427,190,1 427,190,496,274,1 496,274,382,280,1 382,280,337,378,1")]
    public void STAR_trace_les_memes_traits_que_le_simulateur(string source, byte mode, string expected)
    {
        MiniZ80 cpu = Run(source, mode);

        Assert.Equal(expected, Lines(cpu));
        Assert.Equal(1, cpu.GraphicsPen);                      // stylo du tracé remis comme avant
        Assert.Equal((short)0, cpu.GraphicsX);                 // position du tracé remise comme avant
        Assert.Equal((short)0, cpu.GraphicsY);
    }

    [Theory]
    [InlineData(5, 30, 80, 0, 1)]
    [InlineData(7, 40, 99, 33, 0)]
    [InlineData(12, 60, 70, 200, 2)]
    [InlineData(3, 0, 150, -90, 1)]
    public void STAR_est_a_moins_d_un_point_du_calcul_exact(int points, int r1, int r2, int angle, byte mode)
    {
        MiniZ80 cpu = Run($"10 STAR 160,100,{r1},{r2},{points},,,{angle}", mode);

        Assert.Equal(2 * points, cpu.Lines.Count);
        double k = new[] { 69.0 / 128, 69.0 / 64, 69.0 / 32 }[mode];
        int scale = new[] { 4, 2, 1 }[mode];
        for (int i = 0; i < 2 * points; i++)
        {
            double r = i % 2 == 0 ? r2 : r1;
            double t = (90 + angle + i * 180.0 / points) * Math.PI / 180;
            double x = 160 + r * Math.Cos(t) * k, y = 100 - r * Math.Sin(t);
            var line = cpu.Lines[i];
            Assert.InRange(line.X1 / (double)scale - x, -1, 1);
            Assert.InRange(199 - line.Y1 / 2.0 - y, -1, 1);
        }
        Assert.Equal((cpu.Lines[^1].X2, cpu.Lines[^1].Y2), (cpu.Lines[0].X1, cpu.Lines[0].Y1));   // l'étoile est refermée
    }

    [Fact]
    public void STAR_avec_des_valeurs_calculees_hors_limites_ne_dessine_rien()
    {
        Assert.Empty(Run("10 N=1:STAR 160,100,30,80,N,,2:N=33:STAR 160,100,30,80,N,,2:R=1001:STAR 160,100,R,80,5,,2", 1).Lines);
    }

    static long FloorDiv(long n, long d) => n >= 0 ? n / d : -((-n + d - 1) / d);

    // Intérieur (D152) : comparé à la règle écrite ici en nombres exacts, à partir des sommets du contour :
    // ligne y, bords avec y1 <= y < y2, croisements rangés, de l'entier au-dessus du 1er à l'entier au-dessous du 2e...
    [Theory]
    [InlineData(5, 30, 80, 0, 1, 100)]
    [InlineData(7, 40, 99, 33, 0, 100)]
    [InlineData(6, 35, 40, 0, 2, 100)]
    [InlineData(5, 30, 80, 17, 1, -20)]
    [InlineData(3, 10, 120, 200, 1, 190)]
    public void STAR_remplit_l_interieur_comme_le_calcul_exact(int points, int r1, int r2, int angle, byte mode, int y)
    {
        MiniZ80 cpu = Run($"10 STAR 160,{y},{r1},{r2},{points},1,2,{angle}", mode);
        int scale = new[] { 4, 2, 1 }[mode];
        var contour = cpu.Lines.Where(l => l.Pen == 1).ToList();
        Assert.Equal(2 * points, contour.Count);
        var vertices = contour.Select(l => (X: (long)(l.X1 / scale), Y: (long)(199 - l.Y1 / 2))).ToList();

        var expected = new List<string>();
        for (long row = Math.Max(0, vertices.Min(v => v.Y)); row <= Math.Min(199, vertices.Max(v => v.Y)); row++)
        {
            var crossings = new List<(long Num, long Den)>();
            for (int i = 0; i < vertices.Count; i++)
            {
                var (x1, y1) = vertices[i];
                var (x2, y2) = vertices[(i + 1) % vertices.Count];
                if (y1 == y2)
                    continue;
                if (y1 > y2)
                    (x1, y1, x2, y2) = (x2, y2, x1, y1);
                if (y1 <= row && row < y2)
                    crossings.Add((x1 * (y2 - y1) + (row - y1) * (x2 - x1), y2 - y1));
            }
            crossings.Sort((a, b) => (a.Num * b.Den).CompareTo(b.Num * a.Den));
            for (int k = 0; k + 1 < crossings.Count; k += 2)
            {
                long left = -FloorDiv(-crossings[k].Num, crossings[k].Den);
                long right = FloorDiv(crossings[k + 1].Num, crossings[k + 1].Den);
                if (left <= right)
                    expected.Add($"{left}-{right}@{row}");
            }
        }
        var filled = cpu.Lines.Where(l => l.Pen == 2).Select(l => $"{l.X1 / scale}-{l.X2 / scale}@{199 - l.Y1 / 2}").ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, filled);
        Assert.Equal(1, cpu.GraphicsPen);
    }

    [Theory]
    [InlineData("10 STAR 160,100,30,80,1", "STAR, valeur pointes")]
    [InlineData("10 STAR 160,100,30,80,5,16", "STAR, valeur stylo")]
    [InlineData("10 STAR 160,100,30,80", "STAR x, y, r1, r2, pointes [, stylo] [, remplissage] [, angle]")]
    public void STAR_verifie_ses_valeurs(string source, string word)
    {
        var errors = BasicAnalyzer.Analyze(source, new CompilerOptions(Libraries: Catalog)).Diagnostics;

        Assert.Contains(word, Assert.Single(errors).Message);
    }
}
