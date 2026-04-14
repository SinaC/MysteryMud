using Arch.Core;

namespace MysteryMud.Domain.Services;

public interface IExperienceService
{
    long CalculateCombatXp(Entity player, Entity victim);
    void GrantExperience(Entity player, long xpGained);
}