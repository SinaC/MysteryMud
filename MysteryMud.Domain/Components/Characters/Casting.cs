using Arch.Core;

namespace MysteryMud.Domain.Components.Characters;

public struct Casting // TODO: rename because it can also be used for skills
{
    public Entity Source;
    public List<Entity> Targets;
    public int AbilityId;

    public long ExecuteAt;
    public long LastUpdate; // tick of the last message send to inform about casting
}
