using Arch.Core;

namespace MysteryMud.Domain.Services;

public interface IFollowService
{
    void StartFollowing(Entity follower, Entity leader);
    void StopFollowing(Entity follower);
}