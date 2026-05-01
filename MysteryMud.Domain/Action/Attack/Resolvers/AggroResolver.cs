using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class AggroResolver : IAggroResolver
{
    public void ResolveFromDamage(GameState state, Entity target, Entity source, decimal damageAmount, DamageKind damageKind)
    {
        var aggro = AggroCalculator.CalculateDamageAggro(target, source, damageAmount, damageKind);
        AddAggro(state, target, source, aggro);
    }

    public void ResolveFromHeal(GameState state, Entity target, Entity source, decimal healAmount)
    {
        var aggro = AggroCalculator.CalculateHealAggro(target, source, healAmount);
        AddAggro(state, target, source, aggro);
    }

    private static void AddAggro(GameState state, Entity target, Entity source, decimal amount)
    {
        if (!source.IsAlive)
            return;
        if (!target.Has<ThreatTable>())
            return;
        ref var threatTable = ref target.Get<ThreatTable>();
        if (!threatTable.Entries.TryAdd(source, amount))
            threatTable.Entries[source] += amount;
        threatTable.LastUpdateTick = state.CurrentTick;

        if (!target.Has<ActiveThreatTag>()) // indicate to threat decay system this is an entity to check
            target.Set<ActiveThreatTag>();
    }
}
