using DefaultEcs;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Domain.Action.Effect.Helpers;

public class EffectHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive && !x.Has<ExpiredTag>());
    }
}
