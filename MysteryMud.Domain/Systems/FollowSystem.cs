using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Intents;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class FollowSystem
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;

    public FollowSystem(World world, IGameMessageService msg, IIntentContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Tick(GameState state)
    {
        var leaderMoves = BuildLeaderMoveMap();
        if (leaderMoves.Count == 0) return;


        var followersByLeader = BuildFollowersByLeader();
        var charmiesByMaster = BuildCharmiesByMaster();
        // TOOD: pets

        var visited = new HashSet<EntityId>();
        var result = new List<(EntityId follower, MoveIntent leaderIntent)>();

        foreach (var (leader, intent) in leaderMoves)
            VisitChildren(leader, intent, followersByLeader, charmiesByMaster, visited, result);

        foreach (var (follower, intent) in result)
            TryEmitFollowMove(follower, intent);
    }

    private Dictionary<EntityId, MoveIntent> BuildLeaderMoveMap()
    {
        var map = new Dictionary<EntityId, MoveIntent>();
        foreach (ref var intent in _intents.MoveSpan)
            map[intent.Actor] = intent;
        return map;
    }

    private static readonly QueryDescription _followingQueryDescr = new QueryDescription()
        .WithAll<Following>();

    private Dictionary<EntityId, List<EntityId>> BuildFollowersByLeader()
    {
        var map = new Dictionary<EntityId, List<EntityId>>();
        _world.Query(_followingQueryDescr, (EntityId follower,
            ref Following f) =>
        {
            if (!map.TryGetValue(f.Leader, out var list))
                map[f.Leader] = list = [];
            list.Add(follower);
        });
        return map;
    }

    private static readonly QueryDescription _charmedQueryDescr = new QueryDescription()
        .WithAll<Charmed>();

    private Dictionary<EntityId, List<EntityId>> BuildCharmiesByMaster()
    {
        var map = new Dictionary<EntityId, List<EntityId>>();
        _world.Query(_charmedQueryDescr, (EntityId charmie,
            ref Charmed c) =>
        {
            if (!map.TryGetValue(c.Master, out var list))
                map[c.Master] = list = [];
            list.Add(charmie);
        });
        return map;
    }

    // ------------------------------------------------------------------
    private void VisitChildren(
        EntityId leader,
        MoveIntent leaderIntent,
        Dictionary<EntityId, List<EntityId>> followersByLeader,
        Dictionary<EntityId, List<EntityId>> charmiesByMaster,
        HashSet<EntityId> visited,
        List<(EntityId, MoveIntent)> result)
    {
        if (followersByLeader.TryGetValue(leader, out var followers))
        {
            foreach (var follower in followers)
            {
                if (!visited.Add(follower)) continue;

                result.Add((follower, leaderIntent));

                if (WillBeAbleToFollow(follower, leaderIntent))
                {
                    var derived = leaderIntent with { Actor = follower };
                    VisitChildren(follower, derived, followersByLeader, charmiesByMaster, visited, result);
                }
            }
        }

        if (charmiesByMaster.TryGetValue(leader, out var charmies))
        {
            foreach (var charmie in charmies)
            {
                if (!visited.Add(charmie)) continue;
                result.Add((charmie, leaderIntent));
            }
        }
    }

    private bool WillBeAbleToFollow(EntityId follower, MoveIntent leaderIntent)
    {
        if (_world.Has<CombatState>(follower)) return false;
        // TODO
        //if (follower.Has<Stunned>()) return false;
        //if (follower.Has<Incapacitated>()) return false;
        //if (follower.Has<Sleeping>()) return false;

        ref readonly var loc = ref _world.Get<Location>(follower);
        if (loc.Room != leaderIntent.FromRoom) return false;

        if (!MovementValidator.CanEnter(
            _world,
            follower,
            leaderIntent.FromRoom, leaderIntent.ToRoom,
            leaderIntent.Direction, out _)) return false;

        return true;
    }

    // ------------------------------------------------------------------
    private void TryEmitFollowMove(EntityId follower, MoveIntent leaderIntent)
    {
        // Already queued their own move this tick
        foreach (ref var existing in _intents.MoveSpan)
            if (existing.Actor == follower) return;

        if (_world.Has<CombatState>(follower)) return;
        // TODO
        //if (_world.Has<Stunned>(follower)) return;
        //if (_world.Has<Incapacitated>(follower)) return;
        //if (_world.Has<Sleeping>(follower)) return;

        ref readonly var loc = ref _world.Get<Location>(follower);
        if (loc.Room != leaderIntent.FromRoom) return;

        if (!MovementValidator.CanEnter(
            _world,
            follower,
            leaderIntent.FromRoom, leaderIntent.ToRoom,
            leaderIntent.Direction, out var blockReason))
        {
            EmitCannotFollowMessage(follower, leaderIntent.Actor, blockReason);

            if (_world.Has<Charmed>(follower))
                HandleCharmieLeash(follower, leaderIntent);

            return;
        }

        ref var intent = ref _intents.Move.Add();
        intent.Actor = follower;
        intent.FromRoom = leaderIntent.FromRoom;
        intent.ToRoom = leaderIntent.ToRoom;
        intent.Direction = leaderIntent.Direction;
        intent.AutoLook = true;
    }

    private void EmitCannotFollowMessage(EntityId follower, EntityId leader, string blockReason)
    {
        _msg.To(follower).Act("You cannot follow {0}: {1}.").With(leader, blockReason);
        _msg.To(leader).Act("{0} cannot follow you: {1}.").With(follower, blockReason);
    }

    private void HandleCharmieLeash(EntityId charmie, MoveIntent leaderIntent)
    {
        ref var intent = ref _intents.Move.Add();
        intent.Actor = charmie;
        intent.FromRoom = leaderIntent.FromRoom;
        intent.ToRoom = leaderIntent.ToRoom;
        intent.Direction = leaderIntent.Direction;
        intent.AutoLook = true;
    }
}
