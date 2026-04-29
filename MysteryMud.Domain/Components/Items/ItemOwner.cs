using DefaultEcs;

namespace MysteryMud.Domain.Components.Items;

public struct ItemOwner
{
    public Entity Owner; // only this entity can loot this item
}
