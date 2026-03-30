using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Characters;

public struct EffectiveStats
{
    public int Level;
    public long Experience;

    // instead of dictionary, fixed size array indexed by StatType, for better performance. would need to be careful to always keep the order of StatType enum values in sync with the order of values in the array
    public Dictionary<StatKind, int> Values;

    // TODO: add derived stats: calculated from base stats, skills, buffs, etc. that are used for combat calculations. could be stored here for easy access during combat, and updated whenever relevant stats/skills/buffs change
    public int AttackCount;
    public int Dodge;
    public int Parry;
    public int CounterAttack;
}
