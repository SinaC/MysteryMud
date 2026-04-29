using DefaultEcs;

namespace MysteryMud.GameData.Intents;

public struct ExecuteAbilityIntent
{
    public int AbilityId;

    public Entity Source;
    // Resolved, validated target list.
    //
    // Single-target: always exactly 0 or 1 entry (0 only for AoE that found nobody).
    // AoE: 0..N entries; empty list is valid — ability still executes, costs already paid.
    public List<Entity> Targets;
}
