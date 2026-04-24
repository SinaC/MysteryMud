using TinyECS;

namespace MysteryMud.GameData.Events;

public struct ItemPutEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Container;
}
