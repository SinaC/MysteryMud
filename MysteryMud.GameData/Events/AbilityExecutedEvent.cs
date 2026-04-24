using TinyECS;

namespace MysteryMud.GameData.Events;

public struct AbilityExecutedEvent
{
    public int AbilityId;

    public EntityId Source;
    public List<EntityId> Targets;

    // TODO: success ?
}
