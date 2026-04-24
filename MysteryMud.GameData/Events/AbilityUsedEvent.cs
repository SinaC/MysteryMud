using TinyECS;

namespace MysteryMud.GameData.Events;

public struct AbilityUsedEvent
{
    public int AbilityId;

    public EntityId Source;
}
