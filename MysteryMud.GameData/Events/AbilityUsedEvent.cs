using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct AbilityUsedEvent
{
    public Entity Source;
    public List<Entity> Targets;

    public int AbilityId;
}
