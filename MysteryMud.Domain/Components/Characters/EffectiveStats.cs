using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Components.Characters;

public struct EffectiveStats
{
    public CharacterStatValues Values;

    // TODO: add derived stats: calculated from base stats, skills, buffs, etc. that are used for combat calculations. could be stored here for easy access during combat, and updated whenever relevant stats/skills/buffs change
    public int AttackCount;
    public int Dodge;
    public int Parry;
    public int CounterAttack;
}
