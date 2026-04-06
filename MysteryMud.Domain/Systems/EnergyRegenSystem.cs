using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Resources;

namespace MysteryMud.Domain.Systems;

public class EnergyRegenSystem
{
    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<Energy, EnergyRegen, UsesEnergy>();
        state.World.Query(query, (Entity entity, ref Energy energy, ref EnergyRegen regen) =>
        {
            if (energy.Current >= energy.Max)
                return;

            energy.Current = Math.Min(energy.Current + regen.AmountPerTick, energy.Max);
        });
    }
}
