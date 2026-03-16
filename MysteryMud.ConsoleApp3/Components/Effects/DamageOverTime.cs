using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Components.Effects;

struct DamageOverTime
{
    public int Damage;
    public DamageType DamageType;
    public int TickRate;
    public int NextTick;
}
