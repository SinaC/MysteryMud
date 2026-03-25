using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct StatModifierDefinition
{
    // TODO: value formula ?
    public required StatType Stat { get; init; }
    public required ModifierType Type { get; init; }
    public required int Value { get; init; }
}
