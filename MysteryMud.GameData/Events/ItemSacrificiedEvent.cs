using TinyECS;

namespace MysteryMud.GameData.Events;
public struct ItemSacrificiedEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Room;
}
