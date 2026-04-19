using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Services;

public class FollowService : IFollowService
{
    private readonly IGameMessageService _msg;

    public FollowService(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void StartFollowing(Entity follower, Entity leader)
    {
        ref var following = ref follower.TryGetRef<Following>(out var isFollowing);
        if (isFollowing)
        {
            // inform previous leader
            _msg.To(following.Leader).Act("{0} stops following you.").With(follower);
            _msg.To(follower).Act("You stop following {0:N}.").With(following.Leader);

            // change leader
            following.Leader = leader;
        }
        else
            follower.Add(new Following { Leader = leader });
        // inform new leader
        _msg.To(follower).Act("You start following {0:N}.").With(leader);
        _msg.To(leader).Act("{0} starts following you.").With(follower);
    }

    public void StopFollowing(Entity follower)
    {
        ref var following = ref follower.TryGetRef<Following>(out var isFollowing);
        if (isFollowing)
        {
            // inform leader
            _msg.To(following.Leader).Act("{0:N} stops following you.").With(follower);
            _msg.To(follower).Act("You stop following {0:N}.").With(following.Leader);

            // remove following
            follower.Remove<Following>();
        }
    }
}
