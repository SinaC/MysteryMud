using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Data;

public struct StatModifierDefinition
{
    // TODO: value formula ?
    public StatType Stat;
    public ModifierType Type;
    public int Value;
}
