using DefaultEcs;

namespace MysteryMud.GameData.Intents;

public struct AutoSacrificeIntent
{
    public Entity Actor;  // who gets the gold/reward
    public Entity Corpse;   // Corpse to sacrifice
}
