using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct StatModifier
{
    public required StatType Stat { get; init; }
    public required ModifierType Type { get; init; }
    public required int Value { get; init; }
}
