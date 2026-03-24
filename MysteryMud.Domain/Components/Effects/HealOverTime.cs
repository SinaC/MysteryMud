namespace MysteryMud.Domain.Components.Effects;

public struct HealOverTime
{
    public int Heal;
    public long TickRate; // How many ticks between each heal
    public long NextTick; // The tick at which the next heal will occur
}
