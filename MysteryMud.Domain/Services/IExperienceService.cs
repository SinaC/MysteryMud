using TinyECS;

namespace MysteryMud.Domain.Services;

public interface IExperienceService
{
    long CalculateCombatXp(EntityId player, EntityId victim);
    void GrantExperience(EntityId player, long xpGained);
}