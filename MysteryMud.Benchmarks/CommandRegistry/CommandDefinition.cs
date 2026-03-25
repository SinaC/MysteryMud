namespace MysteryMud.Benchmarks.CommandRegistry;

public enum CommandLevel { Player, Admin }
public enum Position { Standing, Sitting }

public readonly struct CommandDefinition
{
    public required string Name { get; init; }
    public required string[] Aliases { get; init; }
    public required CommandLevel RequiredLevel { get; init; }
    public required Position MinimumPosition { get; init; }
    public required int Priority { get; init; }
    public required bool AllowAbbreviation { get; init; }
}

public interface ICommand
{
    CommandDefinition Definition { get; }
}

public class DummyCommand : ICommand
{
    public CommandDefinition Definition { get; }
    public DummyCommand(CommandDefinition def) => Definition = def;
}
