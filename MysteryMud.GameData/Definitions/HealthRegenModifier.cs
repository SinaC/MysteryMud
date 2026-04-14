using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct HealthRegenModifier
{
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
