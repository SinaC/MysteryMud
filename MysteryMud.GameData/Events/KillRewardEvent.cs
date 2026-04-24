using TinyECS;

namespace MysteryMud.GameData.Events;

public struct KillRewardEvent
{
    public EntityId RewardOwner;          // priority reward
    public EntityId RewardOwnerGroup;     // Entity.Null if no group
    public EntityId Victim;
    public bool GrantXp;
}
