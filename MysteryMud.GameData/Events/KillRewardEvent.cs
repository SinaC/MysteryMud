using DefaultEcs;

namespace MysteryMud.GameData.Events;

public struct KillRewardEvent
{
    public Entity RewardOwner;          // priority reward
    public Entity RewardOwnerGroup;     // Entity.Null if no group
    public Entity Victim;
    public bool GrantXp;
}
