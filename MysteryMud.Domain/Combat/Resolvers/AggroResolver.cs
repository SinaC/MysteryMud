using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Calculators;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Resolvers;

public class AggroResolver
{
    public void ResolveFromDamage(Entity target, Entity source, int damageAmount, DamageTypes damageType)
    {
        var aggro = AggroCalculator.CalculateDamageAggro(target, source, damageAmount, damageType);
        AddAggro(target, source, aggro);
    }

    public void ResolveFromHeal(Entity target, Entity source, int healAmount)
    {
        var aggro = AggroCalculator.CalculateHealAggro(target, source, healAmount);
        AddAggro(target, source, aggro);
    }

    private static void AddAggro(Entity target, Entity source, int amount)
    {
        if (!source.IsAlive())
            return;
        ref var threatTable = ref target.TryGetRef<ThreatTable>(out var hasThreat);
        if (!hasThreat)
            return;
        if (!threatTable.Threat.TryAdd(source, amount))
            threatTable.Threat[source] += amount;
    }
}
