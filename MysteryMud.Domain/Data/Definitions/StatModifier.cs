using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Data.Definitions;

public struct StatModifier
{
    public StatType Stat;
    public ModifierType Type;
    public int Value;
}
