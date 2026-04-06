using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct CommandDefinition
{
    public required int Id { get; init; } // generated
    public required string Name { get; init; }
    public required string[] Aliases { get; init; }
    public required bool CannotBeForced { get; init; }
    public required CommandLevelKind RequiredLevel { get; init; }
    public required PositionKind MinimumPosition { get; init; }
    public required int Priority { get; init; }
    public required bool DisallowAbbreviation { get; init; }
    public required string HelpText { get; init; }
    public required string[] Syntaxes { get; init; }
    public required string[] Categories { get; init; }
    public required CommandThrottlingCategories ThrottlingCategories { get; init; }
}
