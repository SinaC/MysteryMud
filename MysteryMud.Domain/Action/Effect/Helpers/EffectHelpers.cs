using MysteryMud.Domain.Components.Effects;
using TinyECS;

namespace MysteryMud.Domain.Action.Effect.Helpers;

public static class EffectHelpers
{
    public static bool IsAlive(World world, params EntityId[] entities)
    {
        return entities.All(x => world.IsAlive(x) && !world.Has<ExpiredTag>(x));
    }
}
