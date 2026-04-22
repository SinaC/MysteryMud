using Arch.Core;
using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestExperienceService : IExperienceService
{
    private readonly Dictionary<Entity, long> _lastGranted = [];
    private long _lastGrantedGlobal;

    public long CalculateCombatXp(Entity player, Entity victim)
    {
        return 1;
    }

    public void GrantExperience(Entity player, long xpGained)
    {
        _lastGrantedGlobal = xpGained;
        _lastGranted[player] = xpGained;
    }

    public long LastGranted => _lastGrantedGlobal;
    public long GetLastGrantedFor(Entity player) => _lastGranted[player];
}
