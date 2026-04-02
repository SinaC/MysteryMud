using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Mobiles;

namespace MysteryMud.Domain.Systems;

public class ThreatDecaySystem
{
    private const int Timeout = 100; // 100 ticks
    private const int DecayPercentagePerTick = 98; // exponential decay

    //Alternative: time-based decay
    //var dt = state.CurrentTick - entry.LastUpdateTick;
    //entry.Value *= MathF.Pow(DecayPercentagePerTick/100, dt);

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<ThreatTable, ActiveThreatTag>();
        state.World.Query(query, (Entity entity, ref ThreatTable table, ref ActiveThreatTag tag) =>
        {
            // no threat modification for a long time, clear
            if (state.CurrentTick - table.LastUpdateTick > Timeout)
            {
                table.Threat.Clear();
                entity.Remove<ActiveThreatTag>();
                return;
            }

            foreach (var key in table.Threat.Keys.ToArray())
            {
                var decayed = (table.Threat[key] * DecayPercentagePerTick) / 100;
                if (decayed == 0) // remove entry if threat reaches 0
                    table.Threat.Remove(key);
                else
                    table.Threat[key] = decayed;
            }
            if (table.Threat.Count == 0) // if no more entries, remove active threat tag
                entity.Remove<ActiveThreatTag>();
        });
    }
}
