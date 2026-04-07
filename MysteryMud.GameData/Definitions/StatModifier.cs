using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct StatModifier
{
    public required StatKind Stat { get; init; }
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
