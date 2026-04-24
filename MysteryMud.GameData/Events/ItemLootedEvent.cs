using TinyECS;

namespace MysteryMud.GameData.Events;
public struct ItemLootedEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Corpse;
}
