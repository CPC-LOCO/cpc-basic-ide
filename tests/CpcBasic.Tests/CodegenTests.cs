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

public class Z80EmitterTests
{
    [Fact]
    public void Absolute_label_is_resolved_with_the_origin()
    {
        var e = new Z80Emitter(0x4000);
        e.LdHlLabel("data");
        e.Label("data");
        e.Bytes(0xAA);

        byte[] expected = [0x21, 0x03, 0x40, 0xAA];   // LD HL,&4003
        Assert.Equal(expected, e.ToBytes());
    }

    [Fact]
    public void Backward_jump_has_a_negative_offset()
    {
        var e = new Z80Emitter(0x4000);
        e.Label("loop");
        e.IncHl();
        e.JrLabel("loop");

        byte[] expected = [0x23, 0x18, 0xFD];         // INC HL ; JR -3
        Assert.Equal(expected, e.ToBytes());
    }

    [Fact]
    public void Forward_jump_has_a_positive_offset()
    {
        var e = new Z80Emitter(0x4000);
        e.JrLabel("end");
        e.IncHl();
        e.Label("end");

        byte[] expected = [0x18, 0x01, 0x23];         // JR +1 ; INC HL
        Assert.Equal(expected, e.ToBytes());
    }

    [Fact]
    public void Unknown_label_is_refused()
    {
        var e = new Z80Emitter(0x4000);
        e.JpLabel("nowhere");

        Assert.Throws<InvalidOperationException>(() => e.ToBytes());
    }

    [Fact]
    public void Duplicate_label_is_refused()
    {
        var e = new Z80Emitter(0x4000);
        e.Label("a");

        Assert.Throws<InvalidOperationException>(() => e.Label("a"));
    }

    [Fact]
    public void Relative_jump_that_is_too_far_is_refused()
    {
        var e = new Z80Emitter(0x4000);
        e.JrLabel("far");
        e.Data(new byte[200]);
        e.Label("far");

        Assert.Throws<InvalidOperationException>(() => e.ToBytes());
    }
}

public class CodeGeneratorTests
{
    static ushort Word(byte[] bytes, int index) => (ushort)(bytes[index] | (bytes[index + 1] << 8));

    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void Print_produces_the_expected_binary()
    {
        byte[] code = Generate("10 PRINT \"HI\"").Code;

        Assert.Equal(26, code.Length);                       // 12 + 9 (module) + 5 (texte)

        Assert.Equal(0x21, code[0]);                         // LD HL,texte
        Assert.Equal(0x4015, Word(code, 1));
        Assert.Equal(0xCD, code[3]);                         // CALL rt_print_string
        Assert.Equal(0x400C, Word(code, 4));

        byte[] waitKey = [0xCD, 0x18, 0xBB];                 // CALL &BB18 (KM WAIT KEY)
        Assert.Equal(waitKey, code[6..9]);
        byte[] restart = [0xC3, 0x00, 0x00];                 // JP 0
        Assert.Equal(restart, code[9..12]);

        byte[] module = [0x7E, 0xB7, 0xC8, 0xCD, 0x5A, 0xBB, 0x23, 0x18, 0xF7];
        Assert.Equal(module, code[12..21]);                  // le module PrintString

        byte[] text = [(byte)'H', (byte)'I', 0x0D, 0x0A, 0x00];
        Assert.Equal(text, code[21..26]);
    }

    [Fact]
    public void Program_without_print_links_no_module()
    {
        GeneratedProgram generated = Generate("10 END");

        Assert.Empty(generated.Modules);
        byte[] expected = [0xC3, 0x03, 0x40, 0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00];
        Assert.Equal(expected, generated.Code);
    }

    [Fact]
    public void Module_is_linked_only_once_for_several_prints()
    {
        GeneratedProgram generated = Generate("10 PRINT \"A\"\n20 PRINT \"B\"\n30 PRINT \"C\"");

        string module = Assert.Single(generated.Modules);
        Assert.Equal("PrintString", module);
    }

    [Fact]
    public void Print_alone_writes_a_line_break()
    {
        byte[] code = Generate("10 PRINT").Code;

        Assert.Equal(24, code.Length);
        byte[] lineBreak = [0x0D, 0x0A, 0x00];
        Assert.Equal(lineBreak, code[^3..]);
    }

    [Fact]
    public void Empty_print_without_line_break_produces_nothing()
    {
        GeneratedProgram generated = Generate("10 PRINT \"\";");

        Assert.Empty(generated.Modules);
        Assert.Equal(6, generated.Code.Length);              // seulement la fin de programme
    }

    [Fact]
    public void Characters_outside_ascii_become_question_marks()
    {
        byte[] code = Generate("10 PRINT \"\u00e9\";").Code;

        byte[] text = [(byte)'?', 0x00];
        Assert.Equal(text, code[^2..]);
    }
}

public class CompilerTests
{
    static string AsText(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    [Fact]
    public void Valid_program_produces_a_disk_with_loader_and_binary()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT \"HELLO\"", "hello");

        Assert.True(result.Success);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(29, result.BinarySize);                 // 12 + 9 (module) + 8 (texte)
        Assert.Equal(new[] { "PrintString" }, result.Modules);

        string disk = AsText(result.Disk!.ToDskBytes());
        Assert.Contains("HELLO   BAS", disk);                // chargeur BASIC
        Assert.Contains("HELLO   BIN", disk);                // programme Z80
        Assert.Contains("HELLO.BIN", disk);                  // le chargeur charge ce fichier
    }

    [Fact]
    public void Program_with_errors_produces_no_disk()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRNT \"A\"", "test");

        Assert.False(result.Success);
        Assert.Null(result.Disk);
        Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void Program_name_longer_than_eight_characters_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Compiler.CompileToDisk("10 END", "TOOLONGNAME"));
    }
}
