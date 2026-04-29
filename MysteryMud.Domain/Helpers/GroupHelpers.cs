using DefaultEcs;
using MysteryMud.Domain.Components.Groups;

namespace MysteryMud.Domain.Helpers;

public static class GroupHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive && !x.Has<DisbandedTag>());
    }
}
