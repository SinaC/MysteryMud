using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct StatModifier
{
    public required StatTypes Stat { get; init; }
    public required ModifierTypes Type { get; init; }
    public required int Value { get; init; }
}
