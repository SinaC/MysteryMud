using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct GetItemIntent
{
    public Entity Entity;
    public Entity Item;
    public Entity RoomOrContainer;
}
