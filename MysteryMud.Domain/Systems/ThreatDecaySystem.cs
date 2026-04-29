using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Mobiles;

namespace MysteryMud.Domain.Systems;

public class ThreatDecaySystem
{
    private const int Timeout = 100; // 100 ticks
    private const int DecayPercentagePerTick = 98; // exponential decay

    private readonly EntitySet _hasThreatTableEntitySet;

    public ThreatDecaySystem(World world)
    {
        _hasThreatTableEntitySet = world
            .GetEntities()
            .With<ThreatTable>()
            .With<ActiveThreatTag>()
            .AsSet();
    }

    //Alternative: time-based decay
    //var dt = state.CurrentTick - entry.LastUpdateTick;
    //entry.Value *= MathF.Pow(DecayPercentagePerTick/100, dt);

    public void Tick(GameState state)
    {
        foreach(var entity in _hasThreatTableEntitySet.GetEntities())
        {
            var table = entity.Get<ThreatTable>();
            DecayThreatTable(state, table);
            if (table.Threat.Count == 0) // if no more entries, remove active threat tag
                entity.Remove<ActiveThreatTag>();
        }
    }

    private void DecayThreatTable(GameState state, ThreatTable table)
    {
        // no threat modification for a long time, clear
        if (state.CurrentTick - table.LastUpdateTick > Timeout)
        {
            table.Threat.Clear();
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
    }
}
