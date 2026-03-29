using Arch.Core;

namespace MysteryMud.GameData.Events;
public struct ItemSacrifiedEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Room;
}
