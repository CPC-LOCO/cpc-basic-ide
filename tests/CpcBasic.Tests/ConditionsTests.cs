// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class ConditionsLexerTests
{
    [Fact]
    public void Comparison_operators_are_tokenized()
    {
        var diagnostics = new List<Diagnostic>();
        List<Token> tokens = Lexer.Tokenize("10 IF a<b AND a<=b AND a<>b AND a>b AND a>=b AND a=b", diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] operators = tokens.Select(t => t.Kind).Where(k =>
            k is TokenKind.Less or TokenKind.LessEqual or TokenKind.NotEqual
              or TokenKind.Greater or TokenKind.GreaterEqual or TokenKind.Equals).ToArray();
        TokenKind[] expected =
        [
            TokenKind.Less, TokenKind.LessEqual, TokenKind.NotEqual,
            TokenKind.Greater, TokenKind.GreaterEqual, TokenKind.Equals
        ];
        Assert.Equal(expected, operators);
    }
}

public class ConditionsParserTests
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

    static ComparisonCondition Compare(string variable, CompareOperator op, int value) =>
        new(op, new VariableExpression(variable), new NumberExpression(value));

    [Theory]
    [InlineData("=", CompareOperator.Equal)]
    [InlineData("<>", CompareOperator.NotEqual)]
    [InlineData("<", CompareOperator.Less)]
    [InlineData("<=", CompareOperator.LessOrEqual)]
    [InlineData(">", CompareOperator.Greater)]
    [InlineData(">=", CompareOperator.GreaterOrEqual)]
    public void Each_comparison_operator_is_recognized(string symbol, CompareOperator expected)
    {
        var statement = Assert.IsType<IfStatement>(Parse($"IF a {symbol} 5 THEN\nEND IF")[0]);

        Assert.Equal(Compare("A", expected, 5), statement.Condition);
    }

    [Fact]
    public void Single_line_if_with_else_is_split_into_statements()
    {
        IReadOnlyList<Statement> statements = Parse("10 IF a = 1 THEN PRINT \"Y\" ELSE PRINT \"N\"");

        Statement[] expected =
        [
            new IfStatement(1, Compare("A", CompareOperator.Equal, 1)),
            new PrintStatement(1, "Y", true),
            new ElseStatement(1),
            new PrintStatement(1, "N", true),
            new EndIfStatement(1)
        ];
        Assert.Equal(expected, statements.ToArray());
    }

    [Fact]
    public void Print_with_trailing_semicolon_may_be_followed_by_else()
    {
        IReadOnlyList<Statement> statements = Parse("10 IF a = 1 THEN PRINT \"Y\"; ELSE PRINT \"N\"");

        Assert.Equal(new PrintStatement(1, "Y", false), statements[1]);
        Assert.IsType<ElseStatement>(statements[2]);
    }

    [Fact]
    public void Block_if_is_closed_by_end_if()
    {
        IReadOnlyList<Statement> statements = Parse("IF a > 1 THEN\nPRINT \"Y\"\nELSE\nPRINT \"N\"\nEND IF");

        Statement[] expected =
        [
            new IfStatement(1, Compare("A", CompareOperator.Greater, 1)),
            new PrintStatement(2, "Y", true),
            new ElseStatement(3),
            new PrintStatement(4, "N", true),
            new EndIfStatement(5)
        ];
        Assert.Equal(expected, statements.ToArray());
    }

    [Fact]
    public void Endif_written_as_one_word_is_accepted()
    {
        IReadOnlyList<Statement> statements = Parse("IF a = 1 THEN\nENDIF");

        Assert.IsType<EndIfStatement>(statements[1]);
    }

    [Fact]
    public void Colon_keeps_following_statements_in_the_then_part()
    {
        IReadOnlyList<Statement> statements = Parse("10 IF a = 1 THEN PRINT 1 : PRINT 2");

        Assert.Equal(4, statements.Count);
        Assert.IsType<PrintValueStatement>(statements[1]);
        Assert.IsType<PrintValueStatement>(statements[2]);
        Assert.IsType<EndIfStatement>(statements[3]);
    }

    [Fact]
    public void Else_belongs_to_the_nearest_single_line_if()
    {
        IReadOnlyList<Statement> statements =
            Parse("10 IF a = 1 THEN IF b = 2 THEN PRINT 1 ELSE PRINT 2");

        Assert.Equal(7, statements.Count);
        Assert.IsType<ElseStatement>(statements[3]);
        Assert.IsType<EndIfStatement>(statements[5]);
        Assert.IsType<EndIfStatement>(statements[6]);
    }

    [Fact]
    public void And_binds_tighter_than_or()
    {
        var statement = Assert.IsType<IfStatement>(Parse("IF a = 1 OR b = 2 AND c = 3 THEN\nEND IF")[0]);

        var expected = new OrCondition(
            Compare("A", CompareOperator.Equal, 1),
            new AndCondition(Compare("B", CompareOperator.Equal, 2), Compare("C", CompareOperator.Equal, 3)));
        Assert.Equal(expected, statement.Condition);
    }

    [Fact]
    public void Comparison_sides_may_be_expressions()
    {
        var statement = Assert.IsType<IfStatement>(Parse("IF a + 1 > (b - 2) THEN\nEND IF")[0]);

        var comparison = Assert.IsType<ComparisonCondition>(statement.Condition);
        Assert.IsType<BinaryExpression>(comparison.Left);
        Assert.IsType<BinaryExpression>(comparison.Right);
    }

    [Fact]
    public void Exit_inside_an_if_inside_a_loop_is_accepted()
    {
        Assert.False(BasicAnalyzer.Analyze("FOR i = 1 TO 3\nIF i = 2 THEN EXIT FOR\nNEXT").HasErrors);
    }

    [Fact]
    public void Missing_then_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedThen, SingleError("10 IF a = 1 PRINT 1\n20 END IF"));
    }

    [Fact]
    public void Condition_without_comparison_is_reported_once()
    {
        Assert.Equal(DiagnosticCode.ExpectedComparison, SingleError("10 IF a THEN\n20 END IF"));
        Assert.Equal(DiagnosticCode.ExpectedComparison, SingleError("10 IF a THEN PRINT 1"));
    }

    [Fact]
    public void Missing_right_side_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 IF a = THEN\n20 END IF"));
    }

    [Fact]
    public void Else_and_end_if_without_if_are_refused()
    {
        Assert.Equal(DiagnosticCode.ElseWithoutIf, SingleError("10 ELSE"));
        Assert.Equal(DiagnosticCode.EndIfWithoutIf, SingleError("10 END IF"));
    }

    [Fact]
    public void Second_else_is_refused()
    {
        Assert.Equal(DiagnosticCode.DuplicateElse, SingleError("IF a = 1 THEN\nELSE\nELSE\nEND IF"));
    }

    [Fact]
    public void Block_if_without_end_if_is_refused()
    {
        Assert.Equal(DiagnosticCode.IfWithoutEndIf, SingleError("10 IF a = 1 THEN\n20 PRINT 1"));
    }

    [Fact]
    public void Blocks_must_be_nested_properly()
    {
        Assert.Contains(DiagnosticCode.BlocksNotNested, Errors("FOR i = 1 TO 3\nIF i = 1 THEN\nNEXT\nEND IF"));
        Assert.Contains(DiagnosticCode.BlocksNotNested, Errors("IF a = 1 THEN\nFOR i = 1 TO 3\nEND IF\nNEXT"));
        Assert.Contains(DiagnosticCode.BlocksNotNested, Errors("IF a = 1 THEN\nFOR i = 1 TO 3\nELSE\nNEXT\nEND IF"));
    }

    [Fact]
    public void If_words_cannot_be_used_as_variables()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 PRINT AND"));
    }

    [Fact]
    public void Variable_used_in_a_condition_cannot_be_declared_byte_afterwards()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("IF a = 1 THEN\nEND IF\nBYTE a"));
    }
}

public class ConditionsCodeGeneratorTests
{
    static byte[] Code(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000).Code;
    }

    static bool Contains(byte[] code, params byte[] pattern) => code.AsSpan().IndexOf(pattern) >= 0;

    static int Count(byte[] code, params byte[] pattern)
    {
        int count = 0;
        for (int i = 0; i + pattern.Length <= code.Length; i++)
            if (code.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                count++;
        return count;
    }

    [Fact]
    public void Equality_with_a_literal_produces_the_expected_binary()
    {
        byte[] expected =
        [
            0x2A, 0x15, 0x40,          // LD HL,(A)
            0x11, 0x01, 0x00,          // LD DE,1
            0xB7,                      // OR A
            0xED, 0x52,                // SBC HL,DE
            0xC2, 0x0F, 0x40,          // JP NZ,sinon
            0xC3, 0x0F, 0x40,          // END : JP fin de programme
            0xCD, 0x18, 0xBB,          // sinon = fin de programme : CALL &BB18
            0xC3, 0x00, 0x00,          // JP 0
            0x00, 0x00                 // la variable A
        ];
        Assert.Equal(expected, Code("10 IF a = 1 THEN END"));
    }

    [Fact]
    public void Literal_comparisons_flip_bit_15_and_use_the_carry()
    {
        byte[] bias = [0x7C, 0xEE, 0x80, 0x67];                              // LD A,H ; XOR &80 ; LD H,A

        Assert.True(Contains(Code("IF a < 5 THEN END"), [.. bias, 0x11, 0x05, 0x80, 0xB7, 0xED, 0x52, 0xD2]));
        Assert.True(Contains(Code("IF a >= 5 THEN END"), [.. bias, 0x11, 0x05, 0x80, 0xB7, 0xED, 0x52, 0xDA]));
    }

    [Fact]
    public void Greater_and_less_or_equal_swap_the_operands()
    {
        byte[] bias = [0x7C, 0xEE, 0x80, 0x67];

        Assert.True(Contains(Code("IF a > 5 THEN END"), [.. bias, 0x11, 0x05, 0x80, 0xEB, 0xB7, 0xED, 0x52, 0xD2]));
        Assert.True(Contains(Code("IF a <= 5 THEN END"), [.. bias, 0x11, 0x05, 0x80, 0xEB, 0xB7, 0xED, 0x52, 0xDA]));
    }

    [Fact]
    public void Not_equal_jumps_when_the_zero_flag_is_set()
    {
        Assert.True(Contains(Code("IF a <> 5 THEN END"), 0x11, 0x05, 0x00, 0xB7, 0xED, 0x52, 0xCA));
    }

    [Fact]
    public void Comparison_with_a_variable_on_the_right_uses_the_stack()
    {
        Assert.True(Contains(Code("IF a = b THEN END"), 0xD1, 0xEB, 0xB7, 0xED, 0x52, 0xC2));

        byte[] lessThan = [0xD1, 0xEB, 0x7C, 0xEE, 0x80, 0x67, 0x7A, 0xEE, 0x80, 0x57, 0xB7, 0xED, 0x52, 0xD2];
        Assert.True(Contains(Code("IF a < b THEN END"), lessThan));
    }

    [Fact]
    public void And_tests_both_comparisons_and_jumps_to_else_on_the_first_failure()
    {
        byte[] code = Code("IF a = 1 AND b = 2 THEN END");

        Assert.Equal(2, Count(code, 0xED, 0x52, 0xC2));      // deux JP NZ
    }

    [Fact]
    public void Or_jumps_into_the_then_part_as_soon_as_one_comparison_is_true()
    {
        byte[] code = Code("IF a = 1 OR b = 2 THEN END");

        Assert.True(Contains(code, 0xED, 0x52, 0xCA));        // JP Z vers la partie « alors »
        Assert.True(Contains(code, 0xED, 0x52, 0xC2));        // JP NZ vers « sinon »
    }

    [Fact]
    public void Else_part_is_skipped_by_a_jump_at_the_end_of_the_then_part()
    {
        byte[] withElse = Code("IF a = 1 THEN PRINT \"Y\" ELSE PRINT \"N\"");
        byte[] withoutElse = Code("IF a = 1 THEN PRINT \"Y\"");

        Assert.Equal(withoutElse.Length + 3 + 3 + 3 + 4, withElse.Length);   // JP fin + LD HL + CALL + texte N (4 octets)
    }

    [Fact]
    public void Nested_and_block_ifs_compile_with_distinct_labels()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 IF a = 1 THEN\n20 IF b = 2 THEN\n30 PRINT \"x\"\n40 ELSE\n50 PRINT \"y\"\n60 END IF\n70 END IF", "nested");

        Assert.True(result.Success);
    }

    [Fact]
    public void If_inside_a_loop_with_exit_and_continue_compiles()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 FOR i = 1 TO 6\n20 IF i = 3 THEN CONTINUE FOR\n30 IF i = 5 THEN EXIT FOR\n40 PRINT i;\n50 NEXT", "loopif");

        Assert.True(result.Success);
    }

    [Fact]
    public void Byte_variables_can_be_compared()
    {
        CompileResult result = Compiler.CompileToDisk("10 BYTE b\n20 b = 200\n30 IF b >= 200 THEN PRINT \"ok\"", "bytecmp");

        Assert.True(result.Success);
    }
}
