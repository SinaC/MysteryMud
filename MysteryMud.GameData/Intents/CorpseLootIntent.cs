using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct CorpseLootIntent
{
    public EntityId Corpse;
    public EntityId LootOwner;        // priority looter
    public EntityId LootOwnerGroup;   // Entity.Null if no group
}