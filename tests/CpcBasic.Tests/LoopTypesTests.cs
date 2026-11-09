// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class LoopTypesParserTests
{
    static IReadOnlyList<Statement> Parse(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return analysis.Program.Statements;
    }

    static List<DiagnosticCode> Errors(string source)
    {
        CompileResult result = Compiler.CompileToDisk(source, "test");
        Assert.False(result.Success);
        return result.Diagnostics.Select(d => d.Code).ToList();
    }

    static DiagnosticCode SingleError(string source) => Assert.Single(Errors(source));

    static readonly ComparisonCondition ALessThan3 =
        new(CompareOperator.Less, new VariableExpression("A"), new NumberExpression(3));

    [Fact]
    public void While_and_wend_are_recognized()
    {
        Statement[] expected = [new WhileStatement(1, ALessThan3), new WendStatement(2)];

        Assert.Equal(expected, Parse("WHILE a < 3\nWEND").ToArray());
    }

    [Fact]
    public void Repeat_and_until_are_recognized()
    {
        Statement[] expected = [new RepeatStatement(1), new UntilStatement(2, ALessThan3)];

        Assert.Equal(expected, Parse("REPEAT\nUNTIL a < 3").ToArray());
    }

    [Fact]
    public void Do_loop_without_condition()
    {
        Statement[] expected = [new DoStatement(1, null, false), new LoopStatement(2, null, false)];

        Assert.Equal(expected, Parse("DO\nLOOP").ToArray());
    }

    [Fact]
    public void Do_while_and_do_until_carry_their_condition()
    {
        Assert.Equal(new DoStatement(1, ALessThan3, false), Parse("DO WHILE a < 3\nLOOP")[0]);
        Assert.Equal(new DoStatement(1, ALessThan3, true), Parse("DO UNTIL a < 3\nLOOP")[0]);
    }

    [Fact]
    public void Loop_while_and_loop_until_carry_their_condition()
    {
        Assert.Equal(new LoopStatement(2, ALessThan3, false), Parse("DO\nLOOP WHILE a < 3")[1]);
        Assert.Equal(new LoopStatement(2, ALessThan3, true), Parse("DO\nLOOP UNTIL a < 3")[1]);
    }

    [Fact]
    public void Exit_and_continue_accept_every_loop_type()
    {
        IReadOnlyList<Statement> statements = Parse(
            "FOR i = 1 TO 2\nWHILE a = 1\nREPEAT\nDO\nEXIT FOR\nEXIT WHILE\nEXIT REPEAT\nEXIT DO\nCONTINUE DO\nLOOP\nUNTIL a = 1\nWEND\nNEXT");

        Assert.Equal(new ExitStatement(5, LoopKind.For), statements[4]);
        Assert.Equal(new ExitStatement(6, LoopKind.While), statements[5]);
        Assert.Equal(new ExitStatement(7, LoopKind.Repeat), statements[6]);
        Assert.Equal(new ExitStatement(8, LoopKind.Do), statements[7]);
        Assert.Equal(new ContinueStatement(9, LoopKind.Do), statements[8]);
    }

    [Fact]
    public void Exit_inside_a_nested_loop_may_target_an_outer_loop()
    {
        Assert.False(BasicAnalyzer.Analyze("FOR i = 1 TO 2\nWHILE a = 1\nEXIT FOR\nWEND\nNEXT").HasErrors);
    }

    [Fact]
    public void Wend_until_and_loop_without_an_opener_are_refused()
    {
        Assert.Equal(DiagnosticCode.ClosingWithoutOpening, SingleError("10 WEND"));
        Assert.Equal(DiagnosticCode.ClosingWithoutOpening, SingleError("10 UNTIL a = 1"));
        Assert.Equal(DiagnosticCode.ClosingWithoutOpening, SingleError("10 LOOP"));
    }

    [Fact]
    public void Unclosed_loops_are_reported_with_their_keyword()
    {
        CompileResult result = Compiler.CompileToDisk("10 WHILE a = 1", "test");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.UnclosedLoop, d.Code);
        Assert.Equal("WHILE", d.Argument);
        Assert.Contains("WEND", d.Message);

        Assert.Equal(DiagnosticCode.UnclosedLoop, SingleError("10 REPEAT"));
        Assert.Equal(DiagnosticCode.UnclosedLoop, SingleError("10 DO"));
    }

    [Fact]
    public void Do_cannot_have_a_condition_at_both_ends()
    {
        Assert.Equal(DiagnosticCode.DoConditionTwice, SingleError("DO WHILE a = 1\nLOOP UNTIL a = 2"));
    }

    [Fact]
    public void Loops_must_be_closed_by_their_own_keyword()
    {
        Assert.Equal(DiagnosticCode.BlocksNotNested, SingleError("WHILE a = 1\nNEXT\nWEND"));
        Assert.Contains(DiagnosticCode.BlocksNotNested, Errors("REPEAT\nWEND"));
        Assert.Contains(DiagnosticCode.BlocksNotNested, Errors("DO\nUNTIL a = 1\nLOOP"));
    }

    [Fact]
    public void Exit_of_a_missing_loop_type_is_refused()
    {
        CompileResult result = Compiler.CompileToDisk("FOR i = 1 TO 2\nEXIT DO\nNEXT", "test");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.LoopControlWithoutLoop, d.Code);
        Assert.Equal("EXIT DO", d.Argument);
    }

    [Fact]
    public void Missing_condition_is_reported_once()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("WHILE\nWEND"));
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("REPEAT\nUNTIL"));
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("DO WHILE\nLOOP"));
    }

    [Fact]
    public void Variable_used_in_a_loop_condition_cannot_be_declared_byte_afterwards()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("WHILE a = 1\nWEND\nBYTE a"));
    }

    [Fact]
    public void Loop_words_cannot_be_used_as_variables()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 PRINT DO"));
    }
}

public class LoopTypesCodeGeneratorTests
{
    static byte[] Code(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000).Code;
    }

    [Fact]
    public void While_tests_at_the_top_and_jumps_back_at_wend()
    {
        byte[] expected =
        [
            0x2A, 0x19, 0x40,          // LD HL,(A)            <- début : test en haut
            0x7C, 0xEE, 0x80, 0x67,    // retourne le bit 15 de HL
            0x11, 0x03, 0x80,          // LD DE,&8003
            0xB7,                      // OR A
            0xED, 0x52,                // SBC HL,DE
            0xD2, 0x13, 0x40,          // JP NC,sortie
            0xC3, 0x00, 0x40,          // WEND : JP début
            0xCD, 0x18, 0xBB,          // sortie : CALL &BB18
            0xC3, 0x00, 0x00,
            0x00, 0x00                 // la variable A
        ];
        Assert.Equal(expected, Code("10 WHILE a < 3 : WEND"));
    }

    [Fact]
    public void Repeat_tests_at_the_bottom_and_jumps_back_while_the_condition_is_false()
    {
        byte[] expected =
        [
            0x2A, 0x12, 0x40,          // LD HL,(A)            <- début de UNTIL (le corps est vide)
            0x11, 0x01, 0x00,          // LD DE,1
            0xB7,
            0xED, 0x52,                // SBC HL,DE
            0xC2, 0x00, 0x40,          // JP NZ,début : on recommence tant que A <> 1
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00
        ];
        Assert.Equal(expected, Code("10 REPEAT : UNTIL a = 1"));
    }

    [Fact]
    public void Do_loop_without_condition_jumps_back_unconditionally()
    {
        byte[] expected = [0xC3, 0x00, 0x40, 0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00];

        Assert.Equal(expected, Code("10 DO : LOOP"));
    }

    [Fact]
    public void Continue_in_a_plain_do_loop_goes_back_to_the_top()
    {
        byte[] expected = [0xC3, 0x00, 0x40, 0xC3, 0x00, 0x40, 0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00];

        Assert.Equal(expected, Code("10 DO : CONTINUE : LOOP"));
    }

    [Fact]
    public void Exit_in_a_do_loop_jumps_after_the_loop()
    {
        byte[] expected = [0xC3, 0x06, 0x40, 0xC3, 0x00, 0x40, 0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00];

        Assert.Equal(expected, Code("10 DO : EXIT : LOOP"));
    }

    [Fact]
    public void Loop_while_returns_when_true_and_loop_until_returns_when_false()
    {
        byte[] loopWhile =
        [
            0x2A, 0x12, 0x40, 0x11, 0x01, 0x00, 0xB7, 0xED, 0x52,
            0xCA, 0x00, 0x40,          // JP Z,début : A = 1 est vrai, on recommence
            0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00, 0x00, 0x00
        ];
        Assert.Equal(loopWhile, Code("10 DO : LOOP WHILE a = 1"));

        byte[] loopUntil = (byte[])loopWhile.Clone();
        loopUntil[9] = 0xC2;           // JP NZ,début : on recommence tant que A <> 1
        Assert.Equal(loopUntil, Code("10 DO : LOOP UNTIL a = 1"));
    }

    [Fact]
    public void Do_while_exits_when_false_and_do_until_exits_when_true()
    {
        byte[] doWhile =
        [
            0x2A, 0x15, 0x40, 0x11, 0x01, 0x00, 0xB7, 0xED, 0x52,
            0xC2, 0x0F, 0x40,          // JP NZ,sortie : A = 1 est faux, on sort
            0xC3, 0x00, 0x40,          // LOOP : JP début
            0xCD, 0x18, 0xBB, 0xC3, 0x00, 0x00, 0x00, 0x00
        ];
        Assert.Equal(doWhile, Code("10 DO WHILE a = 1 : LOOP"));

        byte[] doUntil = (byte[])doWhile.Clone();
        doUntil[9] = 0xCA;             // JP Z,sortie : A = 1 est vrai, on sort
        Assert.Equal(doUntil, Code("10 DO UNTIL a = 1 : LOOP"));
    }

    [Fact]
    public void Continue_in_a_do_while_loop_goes_back_to_the_test()
    {
        byte[] code = Code("10 DO WHILE a = 1 : CONTINUE : LOOP");

        Assert.Equal(new byte[] { 0xC3, 0x00, 0x40 }, code[12..15]);   // CONTINUE : JP début (le test)
    }

    [Fact]
    public void Continue_in_a_repeat_loop_goes_to_the_until_test()
    {
        byte[] code = Code("10 REPEAT : CONTINUE : UNTIL a = 1");

        Assert.Equal(new byte[] { 0xC3, 0x03, 0x40 }, code[..3]);      // le test commence juste après
    }

    [Fact]
    public void Exit_for_from_inside_a_while_loop_compiles()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 FOR i = 1 TO 3\n20 WHILE a > 0\n30 EXIT FOR\n40 WEND\n50 NEXT", "exitfor");

        Assert.True(result.Success);
    }

    [Fact]
    public void Nested_loops_of_every_type_compile_with_distinct_labels()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 DO\n20 REPEAT\n30 WHILE a < 3\n40 a = a + 1\n50 WEND\n60 UNTIL a = 3\n70 LOOP WHILE a < 3", "mixed");

        Assert.True(result.Success);
    }

    [Fact]
    public void Existing_loop_tests_still_see_for_loops()
    {
        Assert.NotEmpty(Code("10 FOR i = 1 TO 3 : EXIT FOR : NEXT"));
    }
}
