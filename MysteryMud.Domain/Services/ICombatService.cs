using DefaultEcs;
using MysteryMud.Core;

namespace MysteryMud.Domain.Services
{
    public interface ICombatService
    {
        void AddCombatClaim(Entity npc, Entity claimant, long currentTick);
        void EnterCombat(GameState state, Entity source, Entity target);
        void ForfeitAllClaims(World world, Entity claimant);
        void ForfeitClaim(Entity npc, Entity claimant);
        void RemoveFromAllCombat(GameState state, Entity character);
        void RemoveFromAllThreatTable(World world, Entity character);
        void RemoveFromCombat(GameState state, Entity character);
        bool TryDetermineLootOwner(Entity victim, Entity killer, out Entity looter);
    }
}