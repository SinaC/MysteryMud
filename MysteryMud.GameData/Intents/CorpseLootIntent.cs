using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct CorpseLootIntent
{
    public Entity Corpse;
    public Entity LootOwner;        // priority looter
    public Entity LootOwnerGroup;   // Entity.Null if no group
}