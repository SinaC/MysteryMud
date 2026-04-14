using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct ManaRegenModifier
{
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
