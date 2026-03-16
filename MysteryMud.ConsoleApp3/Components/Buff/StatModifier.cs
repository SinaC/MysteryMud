using MysteryMud.ConsoleApp3.Enums;

namespace MysteryMud.ConsoleApp3.Components.Buff;

struct StatModifier
{
    public StatType Stat;
    public int Value;
    public ModifierType Type;
    public int Duration;
}
