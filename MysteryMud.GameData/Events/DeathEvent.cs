using TinyECS;

namespace MysteryMud.GameData.Events;

public struct DeathEvent
{
    public EntityId Killer;
    public EntityId Victim;
}
