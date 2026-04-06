using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters.Resources;

namespace MysteryMud.Domain.Systems;

public class ManaRegenSystem
{
    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<Mana, ManaRegen>(); // don't check UsesMana, mana regen even entity cannot use it for the moment
        state.World.Query(query, (Entity entity, ref Mana mana, ref ManaRegen regen) =>
        {
            if (mana.Current >= mana.Max)
                return;

            mana.Current = Math.Min(mana.Current + regen.AmountPerTick, mana.Max);
        });
    }
}
