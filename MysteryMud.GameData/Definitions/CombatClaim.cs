using TinyECS;

namespace MysteryMud.GameData.Definitions;

public struct CombatClaim
{
    public EntityId Claimant;
    public EntityId ClaimantGroup;
    public long JoinedAtTick;    // for ordering
    public bool Forfeited;
}
