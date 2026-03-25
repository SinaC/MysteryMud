using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters.Mobiles;

namespace MysteryMud.Domain.Systems;

public static class AggroSystem
{
    public static void AddAggro(Entity target, Entity source, int amount)
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
