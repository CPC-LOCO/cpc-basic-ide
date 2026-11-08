// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class LoopsParserTests
{
    static IReadOnlyList<Statement> Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return analysis.Program.Statements;
    }

    static DiagnosticCode SingleError(string source)
    {
        CompileResult result = Compiler.CompileToDisk(source, "test");
        Assert.False(result.Success);
        return Assert.Single(result.Diagnostics).Code;
    }

    [Fact]
    public void For_with_step_and_named_next()
    {
        IReadOnlyList<Statement> statements = Parse("10 FOR i = 1 TO 10 STEP 2\n20 NEXT i");

        Assert.Equal(new ForStatement(1, "I", new NumberExpression(1), 10, 2), statements[0]);
        Assert.Equal(new NextStatement(2, "I"), statements[1]);
    }

    [Fact]
    public void Step_defaults_to_one_and_next_may_have_no_name()
    {
        IReadOnlyList<Statement> statements = Parse("FOR i = 1 TO 3\nNEXT");

        Assert.Equal(new ForStatement(1, "I", new NumberExpression(1), 3, 1), statements[0]);
        Assert.Equal(new NextStatement(2, null), statements[1]);
    }

    [Fact]
    public void Limit_and_step_may_be_negative()
    {
        var loop = Assert.IsType<ForStatement>(Parse("FOR i = 10 TO -5 STEP -1\nNEXT")[0]);

        Assert.Equal(-5, loop.Limit);
        Assert.Equal(-1, loop.Step);
    }

    [Fact]
    public void Start_value_may_be_an_expression()
    {
        var loop = Assert.IsType<ForStatement>(Parse("FOR i = a + 1 TO 3\nNEXT")[0]);

        Assert.IsType<BinaryExpression>(loop.Start);
    }

    [Fact]
    public void Loops_may_be_nested_and_share_one_line()
    {
        Assert.False(BasicAnalyzer.Analyze("FOR i = 1 TO 2\nFOR j = 1 TO 2\nNEXT j\nNEXT i").HasErrors);
        Assert.False(BasicAnalyzer.Analyze("FOR i = 1 TO 2 : NEXT : PRINT").HasErrors);
    }

    [Fact]
    public void Exit_and_continue_are_recognized_with_or_without_for()
    {
        IReadOnlyList<Statement> statements = Parse("FOR i = 1 TO 3\nEXIT FOR\nCONTINUE\nNEXT");

        Assert.Equal(new ExitStatement(2, LoopKind.For), statements[1]);
        Assert.Equal(new ContinueStatement(3, null), statements[2]);
    }

    [Fact]
    public void Next_without_for_is_refused()
    {
        Assert.Equal(DiagnosticCode.NextWithoutFor, SingleError("10 NEXT"));
    }

    [Fact]
    public void For_without_next_is_refused()
    {
        Assert.Equal(DiagnosticCode.ForWithoutNext, SingleError("10 FOR i = 1 TO 3"));
    }

    [Fact]
    public void Next_with_another_variable_is_refused()
    {
        Assert.Equal(DiagnosticCode.NextNameMismatch, SingleError("10 FOR i = 1 TO 3\n20 NEXT j"));
    }

    [Fact]
    public void Exit_and_continue_outside_a_loop_are_refused()
    {
        Assert.Equal(DiagnosticCode.LoopControlWithoutLoop, SingleError("10 EXIT"));
        Assert.Equal(DiagnosticCode.LoopControlWithoutLoop, SingleError("10 CONTINUE FOR"));
        Assert.Equal(DiagnosticCode.LoopControlWithoutLoop,
            SingleError("10 FOR i = 1 TO 3\n20 NEXT\n30 EXIT FOR"));
    }

    [Fact]
    public void Zero_step_is_refused_without_a_second_error_for_next()
    {
        Assert.Equal(DiagnosticCode.StepIsZero, SingleError("10 FOR i = 1 TO 3 STEP 0\n20 NEXT"));
    }

    [Fact]
    public void Limit_must_be_a_number_written_in_the_program()
    {
        Assert.Equal(DiagnosticCode.ExpectedNumber, SingleError("10 FOR i = 1 TO x\n20 NEXT"));
    }

    [Fact]
    public void Missing_to_and_equals_are_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedTo, SingleError("10 FOR i = 1 3\n20 NEXT"));
        Assert.Equal(DiagnosticCode.ExpectedEquals, SingleError("10 FOR i 1 TO 3\n20 NEXT"));
    }

    [Fact]
    public void Limit_out_of_integer_range_is_refused()
    {
        Assert.Equal(DiagnosticCode.NumberOutOfRange, SingleError("10 FOR i = 1 TO 40000\n20 NEXT"));
    }

    [Fact]
    public void Byte_loop_limit_and_step_must_fit_in_a_byte()
    {
        Assert.Equal(DiagnosticCode.ByteOutOfRange, SingleError("10 BYTE b\n20 FOR b = 0 TO 300\n30 NEXT"));
        Assert.Equal(DiagnosticCode.ByteOutOfRange, SingleError("10 BYTE b\n20 FOR b = 9 TO 0 STEP -300\n30 NEXT"));
    }

    [Fact]
    public void Loop_variable_cannot_be_declared_byte_after_the_loop()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("10 FOR i = 1 TO 3\n20 NEXT\n30 BYTE i"));
    }

    [Fact]
    public void Print_of_a_loop_keyword_as_a_value_is_refused()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 PRINT TO"));
    }
}

public class LoopsCodeGeneratorTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    static bool Contains(byte[] code, params byte[] pattern) => code.AsSpan().IndexOf(pattern) >= 0;

    [Fact]
    public void Integer_loop_produces_the_expected_binary()
    {
        byte[] expected =
        [
            0x21, 0x01, 0x00,          // LD HL,1
            0x22, 0x29, 0x40,          // LD (I),HL
            0x2A, 0x29, 0x40,          // LD HL,(I)        <- début de NEXT
            0x11, 0x01, 0x00,          // LD DE,1          (pas)
            0xB7,                      // OR A
            0xED, 0x5A,                // ADC HL,DE
            0x22, 0x29, 0x40,          // LD (I),HL
            0xEA, 0x23, 0x40,          // JP PE,sortie     (débordement)
            0x7C,                      // LD A,H
            0xEE, 0x80,                // XOR &80
            0x67,                      // LD H,A
            0x11, 0x03, 0x80,          // LD DE,&8003      (limite 3, bit 15 retourné)
            0xEB,                      // EX DE,HL
            0xB7,                      // OR A
            0xED, 0x52,                // SBC HL,DE
            0xD2, 0x06, 0x40,          // JP NC,corps
            0xCD, 0x18, 0xBB,          // sortie : CALL &BB18
            0xC3, 0x00, 0x00,          // JP 0
            0x00, 0x00                 // la variable I
        ];
        Assert.Equal(expected, Generate("10 FOR i = 1 TO 3 : NEXT").Code);
    }

    [Fact]
    public void Byte_loop_has_no_overflow_test_and_uses_one_byte()
    {
        byte[] expected =
        [
            0x21, 0x01, 0x00,          // LD HL,1
            0x7D,                      // LD A,L
            0x32, 0x2B, 0x40,          // LD (B),A
            0x3A, 0x2B, 0x40,          // LD A,(B)         <- début de NEXT
            0x6F,                      // LD L,A
            0x26, 0x00,                // LD H,0
            0x11, 0x01, 0x00,          // LD DE,1
            0xB7,                      // OR A
            0xED, 0x5A,                // ADC HL,DE
            0x7D,                      // LD A,L
            0x32, 0x2B, 0x40,          // LD (B),A
            0x7C,                      // LD A,H           (pas de JP PE pour un octet)
            0xEE, 0x80,
            0x67,
            0x11, 0x03, 0x80,
            0xEB,
            0xB7,
            0xED, 0x52,
            0xD2, 0x07, 0x40,          // JP NC,corps
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00                       // la variable B : 1 octet
        ];
        Assert.Equal(expected, Generate("10 BYTE b\n20 FOR b = 1 TO 3\n30 NEXT").Code);
    }

    [Fact]
    public void Negative_step_compares_without_swapping()
    {
        byte[] code = Generate("10 FOR i = 5 TO 1 STEP -1 : NEXT").Code;

        Assert.True(Contains(code, 0x11, 0xFF, 0xFF, 0xB7, 0xED, 0x5A));   // LD DE,-1 ; OR A ; ADC HL,DE
        Assert.True(Contains(code, 0x11, 0x01, 0x80, 0xB7, 0xED, 0x52));   // LD DE,&8001 ; OR A ; SBC HL,DE
    }

    [Fact]
    public void Limit_32767_is_safe_because_the_comparison_is_signed_and_overflow_ends_the_loop()
    {
        byte[] code = Generate("10 FOR i = 32760 TO 32767 : NEXT").Code;

        Assert.True(Contains(code, 0x11, 0xFF, 0xFF, 0xEB));               // limite &7FFF devient &FFFF
        Assert.True(Contains(code, 0xED, 0x5A));                            // ADC HL,DE
    }

    [Fact]
    public void Exit_jumps_to_the_end_of_the_loop()
    {
        GeneratedProgram generated = Generate("10 FOR i = 1 TO 3 : EXIT FOR : NEXT");

        Assert.Equal(46, generated.Code.Length);
        Assert.Equal(new byte[] { 0xC3, 0x26, 0x40 }, generated.Code[6..9]);   // JP sortie
    }

    [Fact]
    public void Continue_jumps_to_the_increment()
    {
        GeneratedProgram generated = Generate("10 FOR i = 1 TO 3 : CONTINUE FOR : NEXT");

        Assert.Equal(46, generated.Code.Length);
        Assert.Equal(new byte[] { 0xC3, 0x09, 0x40 }, generated.Code[6..9]);   // JP début de NEXT
    }

    [Fact]
    public void Exit_without_a_kind_targets_the_innermost_loop()
    {
        GeneratedProgram nested = Generate("FOR i = 1 TO 2\nFOR j = 1 TO 2\nEXIT\nNEXT\nNEXT");

        Assert.NotEmpty(nested.Code);   // les étiquettes de chaque boucle sont distinctes : aucune exception
    }

    [Fact]
    public void Nested_loops_compile()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 FOR i = 1 TO 3\n20 FOR j = 1 TO 3\n30 PRINT j;\n40 NEXT j\n50 NEXT i", "nested");

        Assert.True(result.Success);
    }

    [Fact]
    public void Program_with_loop_and_print_links_the_modules()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 FOR i = 1 TO 3\n20 PRINT i;\n30 NEXT", "loop");

        Assert.True(result.Success);
        Assert.Equal(new[] { "PrintInt" }, result.Modules);
    }
}
