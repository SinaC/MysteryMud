using TinyECS;

namespace MysteryMud.GameData.Events;

public struct ItemDroppedEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Room;
}
