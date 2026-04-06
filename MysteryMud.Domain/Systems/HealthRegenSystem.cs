using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Systems;

public class HealthRegenSystem
{
    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<Health, HealthRegen>();
        state.World.Query(query, (Entity entity, ref Health health, ref HealthRegen regen) =>
        {
            if (health.Current >= health.Max)
                return;

            health.Current = Math.Min(health.Current + regen.AmountPerTick, health.Max);
        });
    }
}
