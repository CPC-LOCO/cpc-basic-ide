// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class FunctionsParserTests
{
    static Expression ValueOf(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        var assign = Assert.IsType<AssignStatement>(Assert.Single(analysis.Program.Statements));
        return assign.Value;
    }

    static DiagnosticCode SingleError(string source)
    {
        CompileResult result = Compiler.CompileToDisk(source, "test");
        Assert.False(result.Success);
        return Assert.Single(result.Diagnostics).Code;
    }

    [Fact]
    public void One_value_function_is_recognized()
    {
        var call = Assert.IsType<CallExpression>(ValueOf("A = abs(-5)"));

        Assert.Equal("ABS", call.Function);
        Assert.Equal(new NumberExpression(-5), Assert.Single(call.Arguments));
    }

    [Fact]
    public void Min_with_more_than_two_values_is_split_into_calls_of_two_values()
    {
        var outer = Assert.IsType<CallExpression>(ValueOf("A = MIN(9, 4, 6)"));

        Assert.Equal("MIN", outer.Function);
        Assert.Equal(2, outer.Arguments.Count);
        var inner = Assert.IsType<CallExpression>(outer.Arguments[0]);
        Assert.Equal("MIN", inner.Function);
        Assert.Equal(new NumberExpression(9), inner.Arguments[0]);
        Assert.Equal(new NumberExpression(4), inner.Arguments[1]);
        Assert.Equal(new NumberExpression(6), outer.Arguments[1]);
    }

    [Fact]
    public void Clamp_keeps_its_three_values()
    {
        var call = Assert.IsType<CallExpression>(ValueOf("A = CLAMP(a, 9, 100)"));

        Assert.Equal("CLAMP", call.Function);
        Assert.Equal(3, call.Arguments.Count);
    }

    [Fact]
    public void Functions_can_be_nested_and_used_in_calculations()
    {
        Assert.IsType<BinaryExpression>(ValueOf("A = 2 * MAX(ABS(-3), MIN(10, 20)) + 1"));
    }

    [Fact]
    public void Functions_work_in_print_conditions_and_loop_headers()
    {
        Assert.True(Compiler.CompileToDisk("10 PRINT ABS(-5); MAX(1, 2)", "test").Success);
        Assert.True(Compiler.CompileToDisk("10 IF ABS(a) > 3 THEN PRINT 1", "test").Success);
        Assert.True(Compiler.CompileToDisk("10 FOR i = MIN(1, 2) TO 5\n20 NEXT", "test").Success);
    }

    [Fact]
    public void Wrong_number_of_values_is_refused()
    {
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 A = ABS(1, 2)"));
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 A = ABS()"));
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 A = MIN(1)"));
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 A = CLAMP(1, 2)"));
    }

    [Fact]
    public void Function_without_parentheses_is_refused_with_an_example()
    {
        CompileResult result = Compiler.CompileToDisk("10 A = ABS", "test");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ExpectedOpenParenthesis, d.Code);
        Assert.Contains("ABS(valeur)", d.Message);
    }

    [Fact]
    public void Missing_closing_parenthesis_is_reported()
    {
        Assert.Equal(DiagnosticCode.ExpectedClosingParenthesis, SingleError("10 A = ABS(1"));
    }

    [Fact]
    public void Function_names_cannot_be_used_as_variables()
    {
        Assert.Equal(DiagnosticCode.UnexpectedToken, SingleError("10 ABS = 5"));
    }

    [Fact]
    public void Variable_used_inside_a_function_cannot_be_declared_byte_afterwards()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("10 A = ABS(b)\n20 BYTE b"));
    }
}

public class FunctionsCodeGeneratorTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void Hibyte_keeps_the_high_byte_without_any_module()
    {
        byte[] expected =
        [
            0x21, 0x2C, 0x01,          // LD HL,300
            0x6C,                      // LD L,H
            0x26, 0x00,                // LD H,0
            0x22, 0x0F, 0x40,          // LD (A%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00
        ];
        GeneratedProgram generated = Generate("10 A% = HIBYTE(300)");

        Assert.Equal(expected, generated.Code);
        Assert.Empty(generated.Modules);
    }

    [Fact]
    public void Lobyte_keeps_the_low_byte_without_any_module()
    {
        byte[] expected =
        [
            0x21, 0x2C, 0x01,          // LD HL,300
            0x26, 0x00,                // LD H,0
            0x22, 0x0E, 0x40,          // LD (A%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00
        ];
        GeneratedProgram generated = Generate("10 A% = LOBYTE(300)");

        Assert.Equal(expected, generated.Code);
        Assert.Empty(generated.Modules);
    }

    [Fact]
    public void Each_function_links_only_its_own_module()
    {
        Assert.Equal(new[] { "Abs16" }, Generate("10 A = ABS(-3)").Modules);
        Assert.Equal(new[] { "Sgn16" }, Generate("10 A = SGN(-3)").Modules);
        Assert.Equal(new[] { "Min16" }, Generate("10 A = MIN(1, 2)").Modules);
        Assert.Equal(new[] { "Max16" }, Generate("10 A = MAX(1, 2)").Modules);
        Assert.Equal(new[] { "Min16" }, Generate("10 A = MIN(1, 2, 3)").Modules);
    }

    [Fact]
    public void Clamp_uses_max_then_min()
    {
        Assert.Equal(new[] { "Max16", "Min16" }, Generate("10 A = CLAMP(a, 9, 100)").Modules);
    }
}

internal static class ModuleRunner
{
    public static readonly int[] EdgeValues =
        [0, 1, -1, 2, -2, 7, -7, 100, 255, 256, -256, 12345, -123, 32767, -32767, -32768];

    public static MiniZ80 Run(string module, int hl, int de = 0)
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

    public static IEnumerable<(int A, int B)> Pairs()
    {
        foreach (int a in EdgeValues)
            foreach (int b in EdgeValues)
                yield return (a, b);

        var random = new Random(4242);
        for (int i = 0; i < 2000; i++)
            yield return (random.Next(-32768, 32768), i % 4 == 0 ? random.Next(-5, 5) : random.Next(-32768, 32768));
    }
}

public class FunctionsModuleTests
{
    [Fact]
    public void Min16_returns_the_smaller_signed_value()
    {
        foreach ((int a, int b) in ModuleRunner.Pairs())
        {
            MiniZ80 cpu = ModuleRunner.Run("Min16", a, b);

            Assert.True(unchecked((ushort)Math.Min(a, b)) == cpu.HL, $"MIN({a}, {b}) : obtenu {(short)cpu.HL}");
        }
    }

    [Fact]
    public void Max16_returns_the_larger_signed_value()
    {
        foreach ((int a, int b) in ModuleRunner.Pairs())
        {
            MiniZ80 cpu = ModuleRunner.Run("Max16", a, b);

            Assert.True(unchecked((ushort)Math.Max(a, b)) == cpu.HL, $"MAX({a}, {b}) : obtenu {(short)cpu.HL}");
        }
    }

    [Fact]
    public void Abs16_returns_the_absolute_value()
    {
        foreach ((int a, int _) in ModuleRunner.Pairs())
        {
            MiniZ80 cpu = ModuleRunner.Run("Abs16", a);

            Assert.True(unchecked((ushort)Math.Abs(a)) == cpu.HL, $"ABS({a}) : obtenu {(short)cpu.HL}");
        }
    }

    [Fact]
    public void Sgn16_returns_minus_one_zero_or_one()
    {
        foreach ((int a, int _) in ModuleRunner.Pairs())
        {
            MiniZ80 cpu = ModuleRunner.Run("Sgn16", a);

            Assert.True(unchecked((ushort)Math.Sign(a)) == cpu.HL, $"SGN({a}) : obtenu {(short)cpu.HL}");
        }
    }
}
