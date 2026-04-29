using DefaultEcs;

namespace MysteryMud.GameData.Definitions;

public struct CombatClaim
{
    public Entity Claimant;
    public Entity ClaimantGroup;
    public long JoinedAtTick;    // for ordering
    public bool Forfeited;
}
