using TinyECS;

namespace MysteryMud.GameData.Intents;

public struct AutoSacrificeIntent
{
    public EntityId Actor;  // who gets the gold/reward
    public EntityId Corpse;   // Corpse to sacrifice
}
