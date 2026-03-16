namespace MysteryMud.ConsoleApp.Events;

class CombatEventQueue
{
    public List<DamageEvent> DamageEvents = new();
    public List<HealEvent> HealEvents = new();

    public void Clear()
    {
        DamageEvents.Clear();
        HealEvents.Clear();
    }
}
