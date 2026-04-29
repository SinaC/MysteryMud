using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct DeathEvent
{
    public Entity Killer;
    public Entity Victim;
}
