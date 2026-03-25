using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public struct StatModifierDefinition
{
    // TODO: value formula ?
    public StatType Stat;
    public ModifierType Type;
    public int Value;
}
