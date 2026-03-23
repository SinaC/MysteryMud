namespace MysteryMud.ConsoleApp3.Domain.Components.Effects;

struct HealOverTime
{
    public int Heal;
    public long TickRate; // How many ticks between each heal
    public long NextTick; // The tick at which the next heal will occur
}
