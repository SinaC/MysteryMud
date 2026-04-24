using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct DestroyItemIntent
{
    public EntityId Entity;
    public EntityId Item;
}
