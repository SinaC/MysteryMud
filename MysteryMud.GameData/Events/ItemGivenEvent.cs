using TinyECS;

namespace MysteryMud.GameData.Events;

public struct ItemGivenEvent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Target;
}
