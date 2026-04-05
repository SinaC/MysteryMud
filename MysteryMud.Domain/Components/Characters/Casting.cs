using Arch.Core;

namespace MysteryMud.Domain.Components.Characters;

public struct Casting
{
    public Entity Source;
    public List<Entity> Targets;
    public int AbilityId;

    public long ExecuteAt;
}
