using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct StatModifierDefinition
{
    // TODO: value formula ?
    public required StatKind Stat { get; init; }
    public required ModifierKind Kind { get; init; }
    public required int Value { get; init; }
}
