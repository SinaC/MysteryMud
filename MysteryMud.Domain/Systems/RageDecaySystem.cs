using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Resources;

namespace MysteryMud.Domain.Systems;

public class RageDecaySystem
{
    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<Rage, RageDecay, UsesRage>();
        state.World.Query(query, (Entity entity, ref Rage rage, ref RageDecay decay) =>
        {
            if (rage.Current <= 0)
                return;

            rage.Current = Math.Max(0, rage.Current - decay.AmountPerTick);
        });
    }
}
