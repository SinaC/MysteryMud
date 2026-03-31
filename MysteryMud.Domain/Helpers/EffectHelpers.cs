using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Effects;

namespace MysteryMud.Domain.Helpers;

public class EffectHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive() && !x.Has<ExpiredTag>());
    }
}
