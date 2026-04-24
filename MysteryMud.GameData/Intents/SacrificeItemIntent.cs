using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct SacrificeItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Room;
}
