using TinyECS;

namespace MysteryMud.GameData.Events;
public struct ItemDestroyedEvent
{
    public EntityId Entity;
    public EntityId Item;
}
