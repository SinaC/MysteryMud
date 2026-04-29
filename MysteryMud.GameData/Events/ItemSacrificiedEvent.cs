using DefaultEcs;

namespace MysteryMud.GameData.Events;
public struct ItemSacrificiedEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Room;
}
