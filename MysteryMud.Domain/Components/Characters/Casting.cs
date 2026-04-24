using TinyECS;

namespace MysteryMud.Domain.Components.Characters;

public struct Casting // TODO: rename because it can also be used for skills
{
    public int AbilityId;

    public EntityId Source;
    public List<EntityId>? ResolvedTargets;

    public long ExecuteAt;
    public long LastUpdate; // tick of the last message send to inform about casting
}
