// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class ArithmeticLexerTests
{
    [Fact]
    public void Multiplication_and_division_symbols_are_tokenized()
    {
        var diagnostics = new List<Diagnostic>();
        List<Token> tokens = Lexer.Tokenize("10 a = b * c / d \\ e MOD f", diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] expected =
        [
            TokenKind.LineNumber, TokenKind.Word, TokenKind.Equals, TokenKind.Word, TokenKind.Star,
            TokenKind.Word, TokenKind.Slash, TokenKind.Word, TokenKind.Slash, TokenKind.Word,
            TokenKind.Word, TokenKind.Word, TokenKind.EndOfLine, TokenKind.EndOfFile
        ];
        Assert.Equal(expected, tokens.Select(t => t.Kind).ToArray());
        Assert.Equal("MOD", tokens[10].Text);
    }
}

public class ArithmeticParserTests
{
    static Expression ValueOf(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        var assign = Assert.IsType<AssignStatement>(Assert.Single(analysis.Program.Statements));
        return assign.Value;
    }

    static NumberExpression N(int value) => new(value);

    static BinaryExpression Op(BinaryOperator op, Expression left, Expression right) => new(op, left, right);

    static DiagnosticCode SingleError(string source)
    {
        CompileResult result = Compiler.CompileToDisk(source, "test");
        Assert.False(result.Success);
        return Assert.Single(result.Diagnostics).Code;
    }

    [Fact]
    public void Multiplication_binds_tighter_than_addition()
    {
        var expected = Op(BinaryOperator.Add, N(1), Op(BinaryOperator.Multiply, N(2), N(3)));

        Assert.Equal(expected, ValueOf("A = 1 + 2 * 3"));
    }

    [Fact]
    public void Multiplication_binds_tighter_than_subtraction_on_the_left_too()
    {
        var expected = Op(BinaryOperator.Subtract, Op(BinaryOperator.Multiply, N(2), N(3)), N(1));

        Assert.Equal(expected, ValueOf("A = 2 * 3 - 1"));
    }

    [Fact]
    public void Division_is_evaluated_from_left_to_right()
    {
        var expected = Op(BinaryOperator.Divide, Op(BinaryOperator.Divide, N(8), N(2)), N(2));

        Assert.Equal(expected, ValueOf("A = 8 / 2 / 2"));
    }

    [Fact]
    public void Backslash_is_the_same_as_slash()
    {
        Assert.Equal(ValueOf("A = 7 / 2"), ValueOf("A = 7 \\ 2"));
    }

    [Fact]
    public void Mod_is_recognized_and_has_the_priority_of_a_multiplication()
    {
        var expected = Op(BinaryOperator.Add, N(1), Op(BinaryOperator.Modulo, N(7), N(3)));

        Assert.Equal(expected, ValueOf("A = 1 + 7 MOD 3"));
    }

    [Fact]
    public void Parentheses_override_the_priority()
    {
        var expected = Op(BinaryOperator.Multiply, Op(BinaryOperator.Add, N(1), N(2)), N(3));

        Assert.Equal(expected, ValueOf("A = (1 + 2) * 3"));
    }

    [Fact]
    public void Negative_literals_and_unary_minus_bind_to_their_operand()
    {
        Assert.Equal(Op(BinaryOperator.Multiply, N(-2), N(3)), ValueOf("A = -2 * 3"));

        var expected = Op(BinaryOperator.Multiply,
            Op(BinaryOperator.Subtract, N(0), new VariableExpression("B")),
            new VariableExpression("C"));
        Assert.Equal(expected, ValueOf("A = -b * c"));
    }

    [Fact]
    public void Division_by_a_literal_zero_is_refused()
    {
        Assert.Equal(DiagnosticCode.DivisionByZero, SingleError("10 A = 5 / 0"));
        Assert.Equal(DiagnosticCode.DivisionByZero, SingleError("10 A = 5 \\ 0"));
        Assert.Equal(DiagnosticCode.DivisionByZero, SingleError("10 A = 5 MOD 0"));
    }

    [Fact]
    public void Division_by_a_calculation_that_gives_zero_is_not_detected()
    {
        Assert.True(Compiler.CompileToDisk("10 A = 5 / (2 - 2)", "test").Success);
    }

    [Fact]
    public void Missing_operand_after_the_operator_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 A = 5 *"));
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 A = 5 MOD"));
    }

    [Fact]
    public void Mod_cannot_be_used_as_a_variable_name()
    {
        Assert.Equal(DiagnosticCode.ExpectedExpression, SingleError("10 PRINT MOD"));
    }

    [Fact]
    public void Comparisons_accept_calculations()
    {
        Assert.True(Compiler.CompileToDisk("10 IF a * 2 > b MOD 3 THEN PRINT 1", "test").Success);
    }
}

public class ArithmeticCodeGeneratorTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void Multiplication_of_two_literals_calls_the_module()
    {
        byte[] expected =
        [
            0x21, 0x02, 0x00,          // LD HL,2
            0x11, 0x03, 0x00,          // LD DE,3
            0xCD, 0x12, 0x40,          // CALL Mul16
            0x22, 0x25, 0x40,          // LD (A%),HL
            0xCD, 0x18, 0xBB,          // CALL &BB18
            0xC3, 0x00, 0x00,          // JP 0
            // Le module Mul16
            0x44, 0x4D,                // LD B,H ; LD C,L
            0x21, 0x00, 0x00,          // LD HL,0
            0x3E, 0x10,                // LD A,16
            0x29,                      // ADD HL,HL        <- boucle
            0xCB, 0x23,                // SLA E
            0xCB, 0x12,                // RL D
            0x30, 0x01,                // JR NC,+1
            0x09,                      // ADD HL,BC
            0x3D,                      // DEC A
            0x20, 0xF5,                // JR NZ,boucle
            0xC9,                      // RET
            0x00, 0x00                 // la variable A%
        ];
        GeneratedProgram generated = Generate("10 A% = 2 * 3");

        Assert.Equal(expected, generated.Code);
        Assert.Equal(new[] { "Mul16" }, generated.Modules);
    }

    [Fact]
    public void Division_and_mod_link_the_same_module()
    {
        Assert.Equal(new[] { "Div16" }, Generate("10 A = 7 / 2").Modules);
        Assert.Equal(new[] { "Div16" }, Generate("10 A = 7 MOD 2").Modules);
        Assert.Equal(new[] { "Div16" }, Generate("10 A = 7 / 2 + 9 MOD 4").Modules);
    }

    [Fact]
    public void Multiplication_and_division_link_both_modules_once()
    {
        Assert.Equal(new[] { "Div16", "Mul16" }, Generate("10 A = 2 * 3 / 4 * 5").Modules);
    }

    [Fact]
    public void Program_without_multiplication_or_division_links_neither()
    {
        Assert.Empty(Generate("10 A = 1 + 2 - 3").Modules);
    }

    [Fact]
    public void Mod_takes_the_remainder_from_DE()
    {
        int division = Generate("10 A = b / 3").Code.Length;
        int modulo = Generate("10 A = b MOD 3").Code.Length;

        Assert.Equal(division + 1, modulo);           // un EX DE,HL de plus
    }

    [Fact]
    public void Operands_are_swapped_for_division_but_not_for_multiplication()
    {
        byte[] division = Generate("10 A = b / c").Code;
        byte[] multiplication = Generate("10 A = b * c").Code;

        Assert.True(division.AsSpan().IndexOf(new byte[] { 0xD1, 0xEB, 0xCD }) >= 0);     // POP DE ; EX DE,HL ; CALL
        Assert.True(multiplication.AsSpan().IndexOf(new byte[] { 0xD1, 0xCD }) >= 0);     // POP DE ; CALL
        Assert.True(multiplication.AsSpan().IndexOf(new byte[] { 0xD1, 0xEB }) < 0);
    }
}

public class ArithmeticModuleTests
{
    static readonly int[] EdgeValues =
        [0, 1, -1, 2, -2, 3, 7, -7, 10, 100, 255, 256, -256, 1000, 12345, -123, 32767, -32767, -32768];

    static MiniZ80 Run(string module, int hl, int de = 0)
    {
        var emitter = new Z80Emitter(0x4000);
        RuntimeLibrary.Get(module).Emit(emitter);

        var cpu = new MiniZ80();
        cpu.Load(emitter.ToBytes(), 0x4000);
        cpu.HL = unchecked((ushort)hl);
        cpu.DE = unchecked((ushort)de);
        cpu.Call(0x4000);
        return cpu;
    }

    static IEnumerable<(int A, int B)> Pairs(bool allowZeroSecond)
    {
        foreach (int a in EdgeValues)
            foreach (int b in EdgeValues)
                if (allowZeroSecond || b != 0)
                    yield return (a, b);

        var random = new Random(12345);
        for (int i = 0; i < 3000; i++)
        {
            int a = random.Next(-32768, 32768);
            int b = i % 3 == 0 ? random.Next(-300, 300) : random.Next(-32768, 32768);
            if (allowZeroSecond || b != 0)
                yield return (a, b);
        }
    }

    [Fact]
    public void Mul16_gives_the_low_16_bits_of_the_product()
    {
        foreach ((int a, int b) in Pairs(allowZeroSecond: true))
        {
            MiniZ80 cpu = Run("Mul16", a, b);

            Assert.True(unchecked((ushort)(a * b)) == cpu.HL, $"{a} * {b} : obtenu {(short)cpu.HL}");
        }
    }

    [Fact]
    public void Div16_gives_the_truncated_quotient_and_the_remainder_with_the_sign_of_the_dividend()
    {
        foreach ((int a, int b) in Pairs(allowZeroSecond: false))
        {
            MiniZ80 cpu = Run("Div16", a, b);

            Assert.True(unchecked((ushort)(a / b)) == cpu.HL, $"{a} / {b} : quotient obtenu {(short)cpu.HL}, attendu {a / b}");
            Assert.True(unchecked((ushort)(a % b)) == cpu.DE, $"{a} MOD {b} : reste obtenu {(short)cpu.DE}, attendu {a % b}");
        }
    }

    [Theory]
    [InlineData(7, 2, 3, 1)]
    [InlineData(-7, 2, -3, -1)]
    [InlineData(7, -2, -3, 1)]
    [InlineData(-7, -2, 3, -1)]
    [InlineData(100, 7, 14, 2)]
    [InlineData(-100, 7, -14, -2)]
    [InlineData(0, 5, 0, 0)]
    public void Div16_known_cases(int dividend, int divisor, int quotient, int remainder)
    {
        MiniZ80 cpu = Run("Div16", dividend, divisor);

        Assert.Equal(quotient, (short)cpu.HL);
        Assert.Equal(remainder, (short)cpu.DE);
    }

    [Fact]
    public void Div16_by_zero_gives_zero_and_leaves_the_dividend_as_remainder()
    {
        MiniZ80 cpu = Run("Div16", -5, 0);

        Assert.Equal(0, cpu.HL);
        Assert.Equal(-5, (short)cpu.DE);
    }

    [Fact]
    public void PrintInt_displays_numbers_like_the_cpc()
    {
        var values = new List<int>(EdgeValues);
        var random = new Random(777);
        for (int i = 0; i < 500; i++)
            values.Add(random.Next(-32768, 32768));

        foreach (int value in values)
        {
            MiniZ80 cpu = Run("PrintInt", value);

            string expected = (value < 0 ? "-" : " ") + Math.Abs(value) + " ";
            Assert.True(expected == cpu.Output.ToString(), $"{value} : obtenu « {cpu.Output} », attendu « {expected} »");
        }
    }
}
