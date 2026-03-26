using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Effects;

public struct DamageOverTime
{
    public int Damage;
    public DamageTypes DamageType;
    public long TickRate; // How many ticks between each damage application
    public long NextTick; // The tick at which the next damage application will occur
}
