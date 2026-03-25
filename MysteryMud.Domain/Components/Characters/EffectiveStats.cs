using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Characters;

public struct EffectiveStats
{
    public int Level;
    public long Experience;

    // instead of dictionary, fixed size array indexed by StatType, for better performance. would need to be careful to always keep the order of StatType enum values in sync with the order of values in the array
    public Dictionary<StatType, int> Values;

    // TODO: add derived stats
}
