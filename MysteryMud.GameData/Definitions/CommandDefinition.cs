using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct CommandDefinition
{
    public required string Name { get; init; }
    public required string[] Aliases { get; init; }
    public required CommandLevelKind RequiredLevel { get; init; }
    public required PositionKind MinimumPosition { get; init; }
    public required int Priority { get; init; }
    public required bool AllowAbbreviation { get; init; }
    public required string HelpText { get; init; }
    public required string[] Syntaxes { get; init; }
    public required string[] Categories { get; init; }
}
