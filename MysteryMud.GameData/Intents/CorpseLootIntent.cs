using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct CorpseLootIntent
{
    public Entity Corpse;
    public Entity Killer;         // priority looter
    public Entity Group;          // Entity.Null if no group
}