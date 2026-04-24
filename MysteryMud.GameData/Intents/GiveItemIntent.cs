using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct GiveItemIntent
{
    public EntityId Entity;
    public EntityId Item;
    public EntityId Target;
}
