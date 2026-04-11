using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct AbilityUsedEvent
{
    public int AbilityId;

    public Entity Source;
}
