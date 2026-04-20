using Arch.Core;

namespace MysteryMud.Domain.Services;

public interface IFollowService
{
    void Follow(Entity follower, Entity leader);
    void StopFollowing(Entity follower);
    void StopAllFollowers(Entity leader);
}