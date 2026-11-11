// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class TimeTests
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
    public void Time_is_written_without_parentheses()
    {
        var call = Assert.IsType<CallExpression>(ValueOf("10 a = TIME"));

        Assert.Equal("TIME", call.Function);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void Time_can_be_used_in_calculations_and_conditions()
    {
        Assert.IsType<BinaryExpression>(ValueOf("10 a = TIME - t"));
        Assert.True(Compiler.CompileToDisk("10 t = TIME\n20 WHILE TIME - t < 600\n30 WEND", "test").Success);
    }

    [Fact]
    public void Randomize_time_is_accepted()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 RANDOMIZE TIME");

        Assert.False(analysis.HasErrors);
        var statement = Assert.IsType<RandomizeStatement>(Assert.Single(analysis.Program.Statements));
        Assert.IsType<CallExpression>(statement.Seed);
    }

    [Fact]
    public void Time_cannot_be_assigned_to_or_given_parentheses()
    {
        Assert.Equal(DiagnosticCode.UnexpectedToken, SingleError("10 TIME = 5"));
        Assert.Equal(DiagnosticCode.UnexpectedToken, SingleError("10 a = TIME(1)"));
    }

    [Fact]
    public void Time_is_a_single_call_of_the_firmware_without_any_module()
    {
        byte[] expected =
        [
            0xCD, 0x0D, 0xBD,          // CALL &BD0D (KL TIME PLEASE) : les 16 bits de poids faible sont dans HL
            0x22, 0x0C, 0x40,          // LD (A%),HL
            0xCD, 0x18, 0xBB,
            0xC3, 0x00, 0x00,
            0x00, 0x00
        ];
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 A% = TIME");
        Assert.False(analysis.HasErrors);
        GeneratedProgram generated = CodeGenerator.Generate(analysis.Program, 0x4000);

        Assert.Equal(expected, generated.Code);
        Assert.Empty(generated.Modules);
    }
}
