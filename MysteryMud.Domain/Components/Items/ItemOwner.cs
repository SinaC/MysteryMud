using TinyECS;

namespace MysteryMud.Domain.Components.Items;

public struct ItemOwner
{
    public EntityId Owner; // only this entity can loot this item
}
