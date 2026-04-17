using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Helpers;

public static class CharacterHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive() && !x.Has<Dead>());
    }

    public static bool SameRoom(Entity character1, Entity character2)
        => character1.Get<Location>().Room == character2.Get<Location>().Room;
}
