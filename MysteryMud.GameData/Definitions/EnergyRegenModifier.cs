using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct EnergyRegenModifier
{
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
