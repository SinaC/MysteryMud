using Arch.Core;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct GroupMember
{
    public Entity Group;            // back-reference
    public long JoinedAtTick;       // track when they joined
}
