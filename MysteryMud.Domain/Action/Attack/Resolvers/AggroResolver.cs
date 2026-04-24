using MysteryMud.Core;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class AggroResolver : IAggroResolver
{
    private readonly World _world;

    public AggroResolver(World world)
    {
        _world = world;
    }

    public void ResolveFromDamage(GameState state, EntityId target, EntityId source, int damageAmount, DamageKind damageKind)
    {
        var aggro = AggroCalculator.CalculateDamageAggro(target, source, damageAmount, damageKind);
        AddAggro(state, target, source, aggro);
    }

    public void ResolveFromHeal(GameState state, EntityId target, EntityId source, int healAmount)
    {
        var aggro = AggroCalculator.CalculateHealAggro(target, source, healAmount);
        AddAggro(state, target, source, aggro);
    }

    private void AddAggro(GameState state, EntityId target, EntityId source, int amount)
    {
        if (!CharacterHelpers.IsAlive(_world, source, target))
            return;
        ref var threatTable = ref _world.TryGetRef<ThreatTable>(target, out var hasThreat);
        if (!hasThreat)
            return;
        if (!threatTable.Threat.TryAdd(source, amount))
            threatTable.Threat[source] += amount;
        threatTable.LastUpdateTick = state.CurrentTick;

        if (!_world.Has<ActiveThreatTag>(target)) // indicate to threat decay system this is an entity to check
            _world.Add<ActiveThreatTag>(target);
    }
}
