using DefaultEcs;

namespace MysteryMud.GameData.Events;
public struct ItemDestroyedEvent
{
    public Entity Entity;
    public Entity Item;
}
