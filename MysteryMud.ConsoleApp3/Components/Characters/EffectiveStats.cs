using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Components.Characters;

struct EffectiveStats
{
    // instead of dictionary, fixed size array indexed by StatType, for better performance. would need to be careful to always keep the order of StatType enum values in sync with the order of values in the array
    public Dictionary<StatType, int> Values;

    // TODO: add derived stats
}
