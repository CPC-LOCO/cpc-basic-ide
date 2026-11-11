// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class RandomParserTests
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
    public void Randomize_alone_has_no_seed()
    {
        Assert.Equal(new RandomizeStatement(1, null), Parse("10 RANDOMIZE")[0]);
    }

    [Fact]
    public void Randomize_with_a_number_or_a_calculation()
    {
        Assert.Equal(new RandomizeStatement(1, new NumberExpression(123)), Parse("10 RANDOMIZE 123")[0]);

        var statement = Assert.IsType<RandomizeStatement>(Parse("10 RANDOMIZE a + 1")[0]);
        Assert.IsType<BinaryExpression>(statement.Seed);
    }

    [Fact]
    public void Randomize_may_be_followed_by_else_or_a_colon()
    {
        IReadOnlyList<Statement> statements = Parse("10 IF a = 1 THEN RANDOMIZE ELSE PRINT 2");

        Assert.Equal(5, statements.Count);
        Assert.Equal(new RandomizeStatement(1, null), statements[1]);
        Assert.IsType<ElseStatement>(statements[2]);

        Assert.Equal(2, Parse("10 RANDOMIZE : PRINT 1").Count);
    }

    [Fact]
    public void Random_is_a_function_with_two_values()
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze("10 a = RANDOM(1, 6)");

        Assert.False(analysis.HasErrors);
        var assign = Assert.IsType<AssignStatement>(Assert.Single(analysis.Program.Statements));
        var call = Assert.IsType<CallExpression>(assign.Value);
        Assert.Equal("RANDOM", call.Function);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Random_with_the_wrong_number_of_values_is_refused()
    {
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 a = RANDOM(5)"));
        Assert.Equal(DiagnosticCode.WrongArgumentCount, SingleError("10 a = RANDOM(1, 2, 3)"));
    }

    [Fact]
    public void Random_words_cannot_be_used_as_variables()
    {
        Assert.Equal(DiagnosticCode.UnexpectedToken, SingleError("10 RANDOM = 5"));
    }

    [Fact]
    public void Variable_used_in_a_seed_cannot_be_declared_byte_afterwards()
    {
        Assert.Equal(DiagnosticCode.DeclaredAfterUse, SingleError("10 RANDOMIZE b\n20 BYTE b"));
    }
}

public class RandomCodeGeneratorTests
{
    static GeneratedProgram Generate(string source)
    {
        AnalysisResult analysis = BasicAnalyzer.Analyze(source);
        Assert.False(analysis.HasErrors);
        return CodeGenerator.Generate(analysis.Program, 0x4000);
    }

    [Fact]
    public void Random_links_the_generator_the_division_and_the_range_module()
    {
        Assert.Equal(new[] { "Div16", "Rand16", "RandomRange" }, Generate("10 a = RANDOM(1, 6)").Modules);
    }

    [Fact]
    public void Randomize_links_only_what_it_needs()
    {
        Assert.Equal(new[] { "Rand16", "Randomize" }, Generate("10 RANDOMIZE 5").Modules);
        Assert.Equal(new[] { "Rand16", "Randomize" }, Generate("10 RANDOMIZE").Modules);
    }

    [Fact]
    public void Programs_that_use_random_compile_to_a_disk()
    {
        CompileResult result = Compiler.CompileToDisk(
            "10 RANDOMIZE 123\n20 FOR i = 1 TO 8\n30 PRINT RANDOM(1, 6);\n40 NEXT", "random");

        Assert.True(result.Success);
    }
}

/// <summary>Modules liés ensemble, exécutés dans le mini-émulateur.</summary>
internal sealed class LinkedModules
{
    readonly Z80Emitter emitter = new(0x4000);

    public MiniZ80 Cpu { get; } = new();

    public LinkedModules(params string[] required)
    {
        foreach (RuntimeModule module in Linker.Resolve(required))
            module.Emit(emitter);
        Cpu.Load(emitter.ToBytes(), 0x4000);
    }

    public ushort State
    {
        get
        {
            int at = emitter.AddressOf(RuntimeLibrary.RandStateLabel);
            return (ushort)(Cpu.Memory[at] | (Cpu.Memory[at + 1] << 8));
        }
        set
        {
            int at = emitter.AddressOf(RuntimeLibrary.RandStateLabel);
            Cpu.Memory[at] = (byte)(value & 0xFF);
            Cpu.Memory[at + 1] = (byte)(value >> 8);
        }
    }

    public ushort Call(string label, int hl = 0, int de = 0)
    {
        Cpu.HL = unchecked((ushort)hl);
        Cpu.DE = unchecked((ushort)de);
        Cpu.Call(emitter.AddressOf(label));
        return Cpu.HL;
    }
}

public class RandomModuleTests
{
    // La même suite, écrite en C# : 16 pas du registre à décalage (polynôme &B400) par nombre
    static ushort Next(ushort x)
    {
        for (int i = 0; i < 16; i++)
        {
            bool lowBit = (x & 1) != 0;
            x >>= 1;
            if (lowBit)
                x ^= 0xB400;
        }
        return x;
    }

    [Fact]
    public void The_shift_register_polynomial_gives_the_longest_possible_period()
    {
        // 65535 pas d'un bit : l'état doit revenir au départ, sans jamais repasser par lui avant
        ushort x = 1;
        int steps = 0;
        do
        {
            bool lowBit = (x & 1) != 0;
            x >>= 1;
            if (lowBit)
                x ^= 0xB400;
            steps++;
        }
        while (x != 1);

        Assert.Equal(65535, steps);
    }

    [Fact]
    public void Rand16_follows_the_reference_sequence_and_keeps_its_state()
    {
        var modules = new LinkedModules("Rand16");
        modules.State = 0x1234;
        ushort expected = 0x1234;

        for (int i = 0; i < 200; i++)
        {
            expected = Next(expected);
            ushort result = modules.Call(RuntimeLibrary.Rand16Label);

            Assert.True(expected == result, $"appel {i} : obtenu {result}, attendu {expected}");
            Assert.Equal(expected, modules.State);
        }
    }

    [Fact]
    public void Rand16_starts_from_the_clock_when_the_state_is_zero()
    {
        var modules = new LinkedModules("Rand16");
        modules.State = 0;
        modules.Cpu.Clock = 0x12345678;                 // DE = &1234, HL = &5678

        ushort result = modules.Call(RuntimeLibrary.Rand16Label);

        Assert.Equal(Next(0x5678 ^ 0x1234), result);
    }

    [Fact]
    public void Rand16_never_starts_from_a_zero_state()
    {
        var modules = new LinkedModules("Rand16");
        modules.State = 0;
        modules.Cpu.Clock = 0x00010001;                 // HL xor DE = 0

        ushort result = modules.Call(RuntimeLibrary.Rand16Label);

        Assert.Equal(Next(1), result);
    }

    [Fact]
    public void Randomize_sets_the_state_and_turns_zero_into_one()
    {
        var modules = new LinkedModules("Randomize");

        modules.Call(RuntimeLibrary.RandomizeLabel, 123);
        Assert.Equal(123, modules.State);

        modules.Call(RuntimeLibrary.RandomizeLabel, 0);
        Assert.Equal(1, modules.State);

        modules.Call(RuntimeLibrary.RandomizeLabel, -2);
        Assert.Equal(0xFFFE, modules.State);
    }

    [Fact]
    public void Randomize_alone_resets_the_state_so_that_the_clock_is_used_again()
    {
        var modules = new LinkedModules("Randomize");
        modules.State = 55;

        modules.Call(RuntimeLibrary.RandomizeClockLabel);

        Assert.Equal(0, modules.State);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(0, 255)]
    [InlineData(-5, 5)]
    [InlineData(100, 100)]
    [InlineData(-100, 32666)]        // écart 32767 : le plus grand permis
    [InlineData(-32768, -32000)]
    public void RandomRange_follows_the_reference_and_stays_between_min_and_max(int min, int max)
    {
        var modules = new LinkedModules("RandomRange");
        modules.State = 4321;
        ushort state = 4321;
        int range = max - min + 1;

        for (int i = 0; i < 300; i++)
        {
            state = Next(state);
            int expected = min + ((state & 0x7FFF) % range);

            short result = (short)modules.Call(RuntimeLibrary.RandomRangeLabel, min, max);

            Assert.True(expected == result, $"RANDOM({min}, {max}) appel {i} : obtenu {result}, attendu {expected}");
            Assert.InRange(result, min, max);
        }
    }

    [Theory]
    [InlineData(5, 1)]               // max plus petit que min
    [InlineData(0, 32767)]           // écart 32768 : trop grand
    [InlineData(-20000, 20000)]      // écart trop grand
    public void RandomRange_gives_min_when_the_range_is_not_valid(int min, int max)
    {
        var modules = new LinkedModules("RandomRange");
        modules.State = 999;

        short result = (short)modules.Call(RuntimeLibrary.RandomRangeLabel, min, max);

        Assert.Equal(min, result);
        Assert.Equal(999, modules.State);               // aucun nombre n'a été consommé
    }

    [Fact]
    public void RandomRange_spreads_the_values_evenly_for_a_die()
    {
        var modules = new LinkedModules("RandomRange");
        modules.State = 2024;
        var counts = new int[7];

        for (int i = 0; i < 6000; i++)
            counts[(short)modules.Call(RuntimeLibrary.RandomRangeLabel, 1, 6)]++;

        Assert.Equal(0, counts[0]);
        for (int face = 1; face <= 6; face++)
            Assert.InRange(counts[face], 800, 1200);
    }
}
