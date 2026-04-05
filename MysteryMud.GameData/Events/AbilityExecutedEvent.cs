using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct AbilityExecutedEvent
{
    public Entity Source;
    public List<Entity> Targets;

    public int AbilityId;
}
