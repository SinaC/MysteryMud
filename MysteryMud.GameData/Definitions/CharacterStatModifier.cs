using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct CharacterStatModifier
{
    public required CharacterStatKind Stat { get; init; }
    public required ModifierKind Modifier { get; init; }
    public required decimal Value { get; init; }
}
