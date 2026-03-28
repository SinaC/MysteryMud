using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct ItemDroppedEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Room;
}
