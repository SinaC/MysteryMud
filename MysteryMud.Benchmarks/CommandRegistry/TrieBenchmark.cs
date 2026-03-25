using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace MysteryMud.Benchmarks.CommandRegistry;

// ------------------------
// Benchmark
// ------------------------
[MemoryDiagnoser]
public class TrieBenchmark
{
    private List<CommandDefinition> _commands;
    private OriginalCommandRegistry _original;
    private OptimizedCommandRegistry _optimized;
    private RomCommandRegistry _rom;
    private List<string> _testInputs;

    [GlobalSetup]
    public void Setup()
    {
        _commands = GenerateCommands(300).ToList();

        _original = new OriginalCommandRegistry();
        _original.RegisterCommands(_commands, new Assembly[0]);

        _optimized = new OptimizedCommandRegistry();
        _optimized.RegisterCommands(_commands, new Assembly[0]);

        _rom = new RomCommandRegistry();
        _rom.RegisterCommands(_commands, new Assembly[0]);

        _testInputs = _commands
            .Select(c => c.Name.Substring(0, Math.Min(3, c.Name.Length)))
            .ToList();
    }

    [Benchmark]
    public void OriginalTrieLookup()
    {
        foreach (var input in _testInputs)
        {
            _original.Find(CommandLevel.Player, Position.Standing, input.AsSpan());
        }
    }

    [Benchmark]
    public void OptimizedTrieLookup()
    {
        foreach (var input in _testInputs)
        {
            _optimized.Find(CommandLevel.Player, Position.Standing, input.AsSpan());
        }
    }

    [Benchmark]
    public void RomLookup()
    {
        foreach (var input in _testInputs)
        {
            _rom.Find(CommandLevel.Player, Position.Standing, input.AsSpan());
        }
    }

    private static IEnumerable<CommandDefinition> GenerateCommands(int count = 300)
    {
        var rnd = new Random(42);
        for (int i = 0; i < count; i++)
        {
            var name = $"cmd{i}";
            var aliases = new[] { $"a{i}", $"b{i}" };
            yield return new CommandDefinition
            {
                Name = name,
                Aliases = aliases,
                RequiredLevel = CommandLevel.Player,
                MinimumPosition = Position.Standing,
                Priority = rnd.Next(1, 10),
                AllowAbbreviation = rnd.NextDouble() < 0.95
            };
        }
    }
}
