using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct ItemPutEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity Container;
}
