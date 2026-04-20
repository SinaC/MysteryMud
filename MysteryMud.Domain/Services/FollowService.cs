using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.Services;

public class FollowService : IFollowService
{
    private readonly IGameMessageService _msg;

    public FollowService(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Follow(Entity follower, Entity leader)
    {
        // stop following current leader first if already following
        if (follower.Has<Following>())
            StopFollowing(follower);

        follower.Add(new Following { Leader = leader });

        if (!leader.Has<Followers>())
            leader.Add(new Followers { Entities = [] });

        leader.Get<Followers>().Entities.Add(follower);

        _msg.To(follower).Act("You start following {0:N}.").With(leader);
        _msg.To(leader).Act("{0} starts following you.").With(follower);
    }

    public void StopFollowing(Entity follower)
    {
        if (!follower.Has<Following>()) return;

        var leader = follower.Get<Following>().Leader;
        follower.Remove<Following>();

        if (leader.IsAlive() && leader.Has<Followers>())
            leader.Get<Followers>().Entities.Remove(follower);

        _msg.To(leader).Act("{0:N} stops following you.").With(follower);
        _msg.To(follower).Act("You stop following {0:N}.").With(leader);
    }

    public void StopAllFollowers(Entity leader)
    {
        if (!leader.Has<Followers>()) return;

        ref var followers = ref leader.Get<Followers>();
        foreach (var follower in followers.Entities.ToArray())
        {
            if (!follower.IsAlive()) continue;
            if (follower.Has<Following>())
                follower.Remove<Following>();
            _msg.To(follower).Act("You stop following {0} as they have left.").With(leader);
        }

        followers.Entities.Clear();
        leader.Remove<Followers>();
    }
}
