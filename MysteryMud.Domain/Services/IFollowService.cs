using TinyECS;

namespace MysteryMud.Domain.Services;

public interface IFollowService
{
    void Follow(EntityId follower, EntityId leader);
    void StopFollowing(EntityId follower);
    void StopAllFollowers(EntityId leader);
}