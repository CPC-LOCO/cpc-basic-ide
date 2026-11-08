// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class VariablesLexerTests
{
    static List<Token> Lex(string source, out List<Diagnostic> diagnostics)
    {
        diagnostics = new List<Diagnostic>();
        return Lexer.Tokenize(source, diagnostics);
    }

    [Fact]
    public void Operators_and_parentheses_are_tokenized()
    {
        List<Token> tokens = Lex("10 A% = (5 + 3) - 1", out var diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] expected =
        [
            TokenKind.LineNumber, TokenKind.Word, TokenKind.Equals, TokenKind.LeftParen,
            TokenKind.Number, TokenKind.Plus, TokenKind.Number, TokenKind.RightParen,
            TokenKind.Minus, TokenKind.Number, TokenKind.EndOfLine, TokenKind.EndOfFile
        ];
        Assert.Equal(expected, tokens.Select(t => t.Kind).ToArray());
        Assert.Equal("A%", tokens[1].Text);
    }

    [Fact]
    public void Byte_declaration_is_tokenized_with_commas()
    {
        List<Token> tokens = Lex("10 BYTE a, b", out var diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] expected =
        [
            TokenKind.LineNumber, TokenKind.Word, TokenKind.Word, TokenKind.Comma,
            TokenKind.Word, TokenKind.EndOfLine, TokenKind.EndOfFile
        ];
        Assert.Equal(expected, tokens.Select(t => t.Kind).ToArray());
    }

    [Fact]
    public void Tilde_is_not_a_suffix_any_more()
    {
        _ = Lex("10 A~ = 1", out var diagnostics);

        Diagnostic d = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCode.UnexpectedCharacter, d.Code);
        Assert.Equal("~", d.Argument);
    }
}

public class VariablesParserTests
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
    public void Assignment_with_and_without_LET_is_the_same()
    {
        Assert.Equal(Parse("A% = 5"), Parse("LET a% = 5"));
    }

    [Fact]
    public void Assignment_keeps_the_suffix_in_the_name()
    {
        var statement = Assert.IsType<AssignStatement>(Assert.Single(Parse("10 b% = 7")));

        Assert.Equal("B%", statement.Variable);
        Assert.Equal(new NumberExpression(7), statement.Value);
    }

    [Fact]
    public void Byte_declaration_lists_the_names_in_upper_case()
    {
        var declare = Assert.IsType<DeclareStatement>(Assert.Single(Parse("10 BYTE a, vie")));

        Assert.Equal(new[] { "A", "VIE" }, declare.Names);
        Assert.Equal(1, declare.SourceLine);
    }

    [Fact]
    public void Subtraction_is_evaluated_from_left_to_right()
    {
        var statement = Assert.IsType<AssignStatement>(Assert.Single(Parse("10 A = 1 - 2 - 3")));

        var expected = new BinaryExpression(BinaryOperator.Subtract,
            new BinaryExpression(BinaryOperator.Subtract, new NumberExpression(1), new NumberExpression(2)),
            new NumberExpression(3));
        Assert.Equal(expected, statement.Value);
    }

    [Fact]
    public void Parentheses_change_the_order()
    {
        var statement = Assert.IsType<AssignStatement>(Assert.Single(Parse("10 A = 1 - (2 - 3)")));

        var expected = new BinaryExpression(BinaryOperator.Subtract,
            new NumberExpression(1),
            new BinaryExpression(BinaryOperator.Subtract, new NumberExpression(2), new NumberExpression(3)));
        Assert.Equal(expected, statement.Value);
    }

    [Fact]
    public void Negative_literal_is_folded()
    {
        var statement = Assert.IsType<AssignStatement>(Assert.Single(Parse("10 A = -32768")));

        Assert.Equal(new NumberExpression(-32768), statement.Value);
    }

    [Fact]
    public void Print_with_text_and_value_gives_two_statements()
    {
        IReadOnlyList<Statement> statements = Parse("10 PRINT \"A=\"; A");

        Assert.Equal(2, statements.Count);
        var text = Assert.IsType<PrintStatement>(statements[0]);
        Assert.Equal("A=", text.Text);
        Assert.False(text.NewLine);
        var value = Assert.IsType<PrintValueStatement>(statements[1]);
        Assert.Equal(new VariableExpression("A"), value.Value);
        Assert.True(value.NewLine);
    }

    [Fact]
    public void Trailing_semicolon_removes_the_line_break_after_a_value()
    {
        var value = Assert.IsType<PrintValueStatement>(Assert.Single(Parse("10 PRINT 5;")));

        Assert.False(value.NewLine);
    }

    [Fact]
    public void Print_accepts_the_question_mark_shortcut_with_a_value()
    {
        Assert.IsType<PrintValueStatement>(Assert.Single(Parse("10 ?A")));
    }

    [Fact]
    public void Large_literal_is_fine_for_an_integer_variable()
    {
        Assert.False(BasicAnalyzer.Analyze("10 A = 300").HasErrors);
    }

    [Fact]
    public void Byte_literal_above_255_is_refused()
    {
        Assert.Equal(DiagnosticCode.ByteOutOfRange, SingleError("10 BYTE a\n20 a = 300"));
    }

    [Fact]
    public void Negative_byte_literal_is_refused()
    {
        Assert.Equal(DiagnosticCode.ByteOutOfRange, SingleError("10 BYTE a\n20 a = -1"));
    }

    [Fact]
    public void Byte_calculation_is_not_checked_because_it_wraps_at_run_time()
    {
        Assert.False(BasicAnalyzer.Analyze("10 BYTE a\n20 a = 200 + 100").HasErrors);
    }

    [Fact]
    public void Byte_declared_twice_is_refused()
    {
        Assert.Equal(DiagnosticCode.AlreadyDeclared, SingleError("10 BYTE a\n20 BYTE a"));
        Assert.Equal(DiagnosticCode.AlreadyDeclared, SingleError("10 BYTE a, a"));
    }

    [Fact]
    public void Declaration_after_first_use_is_refused()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("10 a = 1\n20 BYTE a"));
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("10 PRINT a\n20 BYTE a"));
    }

    [Fact]
    public void Declaration_with_a_suffix_is_refused()
    {
        Assert.Equal(DiagnosticCode.SuffixNotAllowedInDeclaration, SingleError("10 BYTE a%"));
    }

    [Fact]
    public void Declaration_without_a_name_is_refused()
    {
        Assert.Equal(DiagnosticCode.ExpectedVariableName, SingleError("10 BYTE"));
        Assert.Equal(DiagnosticCode.ExpectedVariableName, SingleError("10 BYTE 5"));
        Assert.Equal(DiagnosticCode.ExpectedVariableName, SingleError("10 BYTE a,"));
    }

    [Fact]
    public void Integer_literal_out_of_range_is_refused()
    {
        Assert.Equal(DiagnosticCode.NumberOutOfRange, SingleError("10 A% = 32768"));
        Assert.Equal(DiagnosticCode.NumberOutOfRange, SingleError("10 A% = -32769"));
        Assert.Equal(DiagnosticCode.NumberOutOfRange, SingleError("10 PRINT 40000"));
    }

    [Fact]
    public void String_and_real_variables_are_not_available_yet()
    {
        Assert.Equal(DiagnosticCode.UnsupportedVariableType, SingleError("10 A$ = 1"));
        Assert.Equal(DiagnosticCode.UnsupportedVariableType, SingleError("10 PRINT B!"));
    }

    [Fact]
    public void Missing_value_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 A% ="));
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 PRINT 1 +"));
    }

    [Fact]
    public void Missing_equals_sign_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedEquals, SingleError("10 LET A% 5"));
    }

    [Fact]
    public void Missing_closing_parenthesis_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedClosingParenthesis, SingleError("10 PRINT (1 + 2"));
    }

    [Fact]
    public void Print_of_a_number_is_no_longer_an_error()
    {
        Assert.True(Compiler.CompileToDisk("10 PRINT 5", "test").Success);
    }
}

public class VariablesCodeGeneratorTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void Integer_assignment_stores_two_bytes()
    {
        GeneratedProgram generated = Generate("10 A = 5");

        byte[] expected =
        [
            0x21, 0x05, 0x00,          // LD HL,5
            0x22, 0x0C, 0x40,          // LD (&400C),HL
            0xCD, 0x18, 0xBB,          // CALL &BB18
            0xC3, 0x00, 0x00,          // JP 0
            0x00, 0x00                 // la variable, à 0
        ];
        Assert.Equal(expected, generated.Code);
        Assert.Empty(generated.Modules);
    }

    [Fact]
    public void Byte_assignment_stores_one_byte()
    {
        byte[] expected =
        [
            0x21, 0x05, 0x00,          // LD HL,5
            0x7D,                      // LD A,L
            0x32, 0x0D, 0x40,          // LD (&400D),A
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00                       // la variable, 1 octet
        ];
        Assert.Equal(expected, Generate("10 BYTE a\n20 a = 5").Code);
    }

    [Fact]
    public void Declaration_alone_produces_no_code_and_no_variable()
    {
        GeneratedProgram generated = Generate("10 BYTE a, b");

        Assert.Equal(6, generated.Code.Length);              // seulement la fin de programme
        Assert.Empty(generated.Modules);
    }

    [Fact]
    public void Same_name_with_and_without_declaration_gives_different_sizes()
    {
        byte[] asInteger = Generate("10 a = 5").Code;
        byte[] asByte = Generate("10 BYTE a\n20 a = 5").Code;

        Assert.Equal(14, asInteger.Length);
        Assert.Equal(14, asByte.Length);                     // 1 octet de variable, mais LD A,L en plus
        Assert.Equal(0x22, asInteger[3]);                    // LD (var),HL
        Assert.Equal(0x7D, asByte[3]);                       // LD A,L
    }

    [Fact]
    public void Reading_a_byte_variable_extends_it_to_16_bits()
    {
        byte[] expected =
        [
            0x21, 0x05, 0x00,          // LD HL,5
            0x7D,                      // LD A,L
            0x32, 0x16, 0x40,          // LD (A),A
            0x3A, 0x16, 0x40,          // LD A,(A)
            0x6F,                      // LD L,A
            0x26, 0x00,                // LD H,0
            0x22, 0x17, 0x40,          // LD (B%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00,                      // A : 1 octet
            0x00, 0x00                 // B% : 2 octets
        ];
        Assert.Equal(expected, Generate("10 BYTE a\n20 a = 5 : b% = a").Code);
    }

    [Fact]
    public void Addition_with_a_literal_uses_DE_directly()
    {
        byte[] expected =
        [
            0x21, 0x01, 0x00,          // LD HL,1
            0x11, 0x02, 0x00,          // LD DE,2
            0x19,                      // ADD HL,DE
            0x22, 0x10, 0x40,          // LD (A%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00
        ];
        Assert.Equal(expected, Generate("10 A% = 1 + 2").Code);
    }

    [Fact]
    public void Subtraction_with_a_variable_swaps_the_operands()
    {
        byte[] expected =
        [
            0x21, 0x07, 0x00,          // LD HL,7
            0x22, 0x1B, 0x40,          // LD (A%),HL
            0x21, 0x0A, 0x00,          // LD HL,10
            0xE5,                      // PUSH HL
            0x2A, 0x1B, 0x40,          // LD HL,(A%)
            0xD1,                      // POP DE
            0xEB,                      // EX DE,HL
            0xB7,                      // OR A
            0xED, 0x52,                // SBC HL,DE
            0x22, 0x1D, 0x40,          // LD (B%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00,                // A%
            0x00, 0x00                 // B%
        ];
        Assert.Equal(expected, Generate("10 A% = 7 : B% = 10 - A%").Code);
    }

    [Fact]
    public void Negative_literals_are_written_in_16_bits()
    {
        byte[] minusFive = Generate("10 A% = -5").Code;
        Assert.Equal(new byte[] { 0x21, 0xFB, 0xFF }, minusFive[..3]);

        byte[] minimum = Generate("10 A% = -32768").Code;
        Assert.Equal(new byte[] { 0x21, 0x00, 0x80 }, minimum[..3]);
    }

    [Fact]
    public void Same_variable_is_reserved_only_once()
    {
        byte[] once = Generate("10 A% = 1").Code;
        byte[] twice = Generate("10 A% = 1 : A% = 2").Code;

        Assert.Equal(once.Length + 6, twice.Length);   // seulement 2 instructions de plus (3 + 3 octets)
    }

    [Fact]
    public void Print_of_a_number_links_PrintInt()
    {
        GeneratedProgram generated = Generate("10 PRINT 5;");

        Assert.Equal(new[] { "PrintInt" }, generated.Modules);
    }

    [Fact]
    public void Print_of_a_number_with_line_break_links_both_modules()
    {
        GeneratedProgram generated = Generate("10 PRINT 5");

        Assert.Equal(new[] { "PrintInt", "PrintString" }, generated.Modules);
        byte[] lineBreak = [0x0D, 0x0A, 0x00];
        Assert.Equal(lineBreak, generated.Code[^3..]);
    }

    [Fact]
    public void Program_with_variables_but_no_print_links_no_module()
    {
        Assert.Empty(Generate("10 BYTE b\n20 A% = 1 : b = 2").Modules);
    }

    [Fact]
    public void Compiler_accepts_a_program_with_variables()
    {
        CompileResult result = Compiler.CompileToDisk("10 A = 5\n20 PRINT \"A=\"; A", "vars");

        Assert.True(result.Success);
        Assert.Equal(new[] { "PrintInt", "PrintString" }, result.Modules);
    }
}
