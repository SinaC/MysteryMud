using TinyECS;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct GroupMember
{
    public EntityId Group;            // back-reference
    public long JoinedAtTick;       // track when they joined
}
