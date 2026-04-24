using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct PutItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Container;
}
