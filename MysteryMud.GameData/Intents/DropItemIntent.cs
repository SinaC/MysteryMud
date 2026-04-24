using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct DropItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Room;
}
