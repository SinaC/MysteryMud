using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct CharacterIRVModifier
{
    public required FlagModifierKind Modifier { get; init; }
    public required IRVLocation Location { get; init; }
    public required ulong DamageKinds { get; init; }
}
