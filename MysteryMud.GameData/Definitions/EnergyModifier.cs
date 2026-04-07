using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct EnergyModifier
{
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
