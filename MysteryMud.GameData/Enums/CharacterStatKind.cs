using MysteryMud.GameData.Attributes;

namespace MysteryMud.GameData.Enums;

public enum CharacterStatKind
{
    Strength = 0,
    Intelligence = 1,
    Wisdom = 2,
    Dexterity = 3,
    Constitution = 4,
    HitRoll = 5,
    DamRoll = 6,
    ArmorClass = 7,
    SavingThrow = 8,
    [EnumSentinel] Count, // always last, used for array sizing
}
