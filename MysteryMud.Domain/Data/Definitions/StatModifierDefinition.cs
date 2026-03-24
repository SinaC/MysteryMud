using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Data.Definitions;

public struct StatModifierDefinition
{
    // TODO: value formula ?
    public StatType Stat;
    public ModifierType Type;
    public int Value;
}
