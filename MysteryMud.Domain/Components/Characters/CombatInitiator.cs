using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Components.Characters;

public struct CombatInitiator
{
    public List<CombatClaim> Claims; // ordered by join time
}
