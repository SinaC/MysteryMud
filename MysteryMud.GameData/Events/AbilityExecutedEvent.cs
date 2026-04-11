using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct AbilityExecutedEvent
{
    public int AbilityId;

    public Entity Source;
    public List<Entity> Targets;

    // TODO: success ?
}
