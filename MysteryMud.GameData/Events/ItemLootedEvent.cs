using DefaultEcs;

namespace MysteryMud.GameData.Events;
public struct ItemLootedEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Corpse;
}
