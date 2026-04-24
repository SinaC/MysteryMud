using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using TinyECS;

namespace MysteryMud.Domain.Services;

public class FollowService : IFollowService
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public FollowService(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Follow(EntityId follower, EntityId leader)
    {
        // stop following current leader first if already following
        if (_world.Has<Following>(follower))
            StopFollowing(follower);

        _world.Add(follower, new Following { Leader = leader });

        if (!_world.Has<Followers>(leader))
            _world.Add(leader, new Followers { Entities = [] });

        _world.Get<Followers>(leader).Entities.Add(follower);

        _msg.To(follower).Act("You start following {0:N}.").With(leader);
        _msg.To(leader).Act("{0} starts following you.").With(follower);
    }

    public void StopFollowing(EntityId follower)
    {
        if (!_world.Has<Following>(follower)) return;

        var leader = _world.Get<Following>(follower).Leader;
        _world.Remove<Following>(follower);

        if (CharacterHelpers.IsAlive(_world, leader) && _world.Has<Followers>(leader))
            _world.Get<Followers>(leader).Entities.Remove(follower);

        _msg.To(leader).Act("{0:N} stops following you.").With(follower);
        _msg.To(follower).Act("You stop following {0:N}.").With(leader);
    }

    public void StopAllFollowers(EntityId leader)
    {
        if (!_world.Has<Followers>(leader)) return;

        ref var followers = ref _world.Get<Followers>(leader);
        foreach (var follower in followers.Entities.ToArray())
        {
            if (!CharacterHelpers.IsAlive(_world, follower)) continue;
            if (_world.Has<Following>(follower))
                _world.Remove<Following>(follower);
            _msg.To(follower).Act("You stop following {0} as they have left.").With(leader);
        }

        followers.Entities.Clear();
        _world.Remove<Followers>(leader);
    }
}
