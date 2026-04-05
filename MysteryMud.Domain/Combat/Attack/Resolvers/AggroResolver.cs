using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Combat.Calculators;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Attack.Resolvers;

public class AggroResolver
{
    public void ResolveFromDamage(GameState state, Entity target, Entity source, int damageAmount, DamageKind damageKind)
    {
        var aggro = AggroCalculator.CalculateDamageAggro(target, source, damageAmount, damageKind);
        AddAggro(state, target, source, aggro);
    }

    public void ResolveFromHeal(GameState state, Entity target, Entity source, int healAmount)
    {
        var aggro = AggroCalculator.CalculateHealAggro(target, source, healAmount);
        AddAggro(state, target, source, aggro);
    }

    private static void AddAggro(GameState state, Entity target, Entity source, int amount)
    {
        if (!source.IsAlive())
            return;
        ref var threatTable = ref target.TryGetRef<ThreatTable>(out var hasThreat);
        if (!hasThreat)
            return;
        if (!threatTable.Threat.TryAdd(source, amount))
            threatTable.Threat[source] += amount;
        threatTable.LastUpdateTick = state.CurrentTick;

        if (!target.Has<ActiveThreatTag>()) // indicate to threat decay system this is an entity to check
            target.Add<ActiveThreatTag>();
    }
}
