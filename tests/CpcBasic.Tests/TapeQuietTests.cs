// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using System.Text;
using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// D224 : « ! » devant un nom de fichier (RUN"!NOM", LOAD"!IMAGE", SAVE, OPENIN, OPENOUT), comme le vrai CPC : pas de
/// messages de la cassette (CAS NOISY), et le « ! » ne fait pas partie du nom. examples\silence.bas.
/// </summary>
public class TapeQuietTests
{
    static byte[] Compile(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors, string.Join(" / ", analysis.Diagnostics.Select(d => d.Message)));
        return CodeGenerator.Generate(analysis.Program, 0x4000).Code;
    }

    static MiniZ80 Run(string source, Dictionary<string, byte[]>? disk = null)
    {
        var cpu = new MiniZ80 { WindowWidth = 80 };
        foreach ((string name, byte[] code) in disk ?? new())
        {
            cpu.Files[name] = code.ToList();
            cpu.FileHeaders[name] = (2, 0x4000);
        }
        cpu.Load(Compile(source), 0x4000);
        cpu.Memory[0xBB18] = 0xC9;
        cpu.Call(0x4000);
        return cpu;
    }

    [Fact]
    public void RUN_avec_un_point_d_exclamation_lance_le_programme_sans_messages()
    {
        var disk = new Dictionary<string, byte[]> { ["PARTIE2.BIN"] = Compile("10 PRINT \"PARTIE 2\"") };

        MiniZ80 cpu = Run("10 RUN \"!PARTIE2\"", disk);

        Assert.Equal("PARTIE 2\r\n", cpu.Output.ToString());
        Assert.Equal((byte)1, cpu.Noisy[0]);
    }

    [Fact]
    public void SAVE_et_LOAD_avec_un_point_d_exclamation()
    {
        MiniZ80 cpu = Run("10 POKE &C000,7:SAVE \"!E\",B,&C000,1:POKE &C000,0:LOAD \"!E\":PRINT PEEK(&C000)");

        Assert.Equal(" 7 \r\n", cpu.Output.ToString());
        Assert.Contains("E", cpu.Files.Keys);
        Assert.DoesNotContain("!E", cpu.Files.Keys);
        Assert.NotEmpty(cpu.Noisy);
        Assert.All(cpu.Noisy, n => Assert.Equal((byte)1, n));
    }

    [Fact]
    public void Sans_point_d_exclamation_les_messages_reviennent()
    {
        MiniZ80 cpu = Run("10 SAVE \"!E\",B,&C000,1:SAVE \"F\",B,&C000,1");

        Assert.Equal((byte)1, cpu.Noisy[0]);
        Assert.Equal((byte)0, cpu.Noisy[^1]);
        Assert.Contains("F", cpu.Files.Keys);
    }

    [Fact]
    public void OPENOUT_et_un_nom_calcule()
    {
        MiniZ80 cpu = Run("10 N$=\"!T\":OPENOUT N$:PRINT #9,\"A\":CLOSEOUT");

        Assert.Contains("T", cpu.Files.Keys);
        Assert.Equal((byte)1, cpu.Noisy[0]);
    }

    [Fact]
    public void Le_programme_lance_par_RUN_point_d_exclamation_est_compile_aussi()
    {
        BasicProgram program = BasicAnalyzer.Analyze("10 RUN \"!SUITE\":RUN \"!FIN.BIN\"").Program;

        Assert.Equal(new[] { "SUITE", "FIN" }, Compiler.LaunchedPrograms(program).ToArray());
    }

    [Fact]
    public void Le_fichier_de_LOAD_point_d_exclamation_est_trouve_et_range_sans_le_point_d_exclamation()
    {
        static Func<string, DataFile?> Folder(string name, byte[] content) =>
            wanted => wanted.Equals(name, StringComparison.OrdinalIgnoreCase) ? new DataFile(name, content) : null;

        CompileResult result = Compiler.CompileWithLinked("10 LOAD \"!IMAGE\",&C000", "CHARGE", null, _ => null,
                                                          Folder("IMAGE.BIN", new byte[100]));

        Assert.True(result.Success);
        Assert.Empty(result.MissingData);
        Assert.Contains("IMAGE.BIN", result.Disk!.FileNames);
        Assert.Equal(new[] { "CHARGE", "CHARGE.BIN", "IMAGE" }, result.Tape!.FileNames);
    }

    [Fact]
    public void Sur_la_cartouche_le_point_d_exclamation_est_accepte()
    {
        CartridgeResult result = Compiler.CompileToCartridge("10 RUN \"!SUITE\"", CompilerOptions.Default, "DEBUT",
            n => n == "SUITE" ? new ProgramSource("10 PRINT \"OK\"", CompilerOptions.Default) : null, null);

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Cartridge);
        Assert.Empty(result.Missing);
    }
}
