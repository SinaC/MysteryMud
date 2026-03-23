using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Domain.Components.Effects;

struct DamageOverTime
{
    public int Damage;
    public DamageType DamageType;
    public long TickRate; // How many ticks between each damage application
    public long NextTick; // The tick at which the next damage application will occur
}
