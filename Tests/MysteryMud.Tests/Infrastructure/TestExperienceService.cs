using TinyECS;
using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestExperienceService : IExperienceService
{
    private readonly Dictionary<EntityId, long> _lastGranted = [];
    private long _lastGrantedGlobal;

    public long CalculateCombatXp(EntityId player, EntityId victim)
    {
        return 1;
    }

    public void GrantExperience(EntityId player, long xpGained)
    {
        _lastGrantedGlobal = xpGained;
        _lastGranted[player] = xpGained;
    }

    public long LastGranted => _lastGrantedGlobal;
    public long GetLastGrantedFor(EntityId player) => _lastGranted[player];
}
