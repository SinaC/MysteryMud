using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct DropItemIntent
{
    public Entity Entity;
    public Entity Item;
    public Entity Room;
}
