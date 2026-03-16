namespace MysteryMud.ConsoleApp2.ECS.Components.Effects;

public struct DamageOverTime
{
    public int Damage;
    public int TickRate;
    public int NextTick;
}
