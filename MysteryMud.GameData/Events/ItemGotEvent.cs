using Arch.Core;

namespace MysteryMud.GameData.Events;
public struct ItemGotEvent
{
    public Entity Entity;
    public Entity Item;
    public Entity RoomOrContainer;
}
