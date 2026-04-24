using TinyECS;

namespace MysteryMud.GameData.Events;

public struct AggressedEvent
{
    public EntityId Source;
    public EntityId Target;
}
