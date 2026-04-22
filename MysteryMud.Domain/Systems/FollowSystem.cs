using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public class FollowSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;

    public FollowSystem(IGameMessageService msg, IIntentContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Tick(GameState state)
    {
        var leaderMoves = BuildLeaderMoveMap();
        if (leaderMoves.Count == 0) return;

        var followersByLeader = BuildFollowersByLeader(state);
        var charmiesByMaster = BuildCharmiesByMaster(state);

        var visited = new HashSet<Entity>();
        var result = new List<(Entity follower, MoveIntent leaderIntent)>();

        foreach (var (leader, intent) in leaderMoves)
            VisitChildren(state, leader, intent, followersByLeader, charmiesByMaster, visited, result);

        foreach (var (follower, intent) in result)
            TryEmitFollowMove(state, follower, intent);
    }

    private Dictionary<Entity, MoveIntent> BuildLeaderMoveMap()
    {
        var map = new Dictionary<Entity, MoveIntent>();
        foreach (ref var intent in _intents.MoveSpan)
            map[intent.Actor] = intent;
        return map;
    }

    private Dictionary<Entity, List<Entity>> BuildFollowersByLeader(GameState state)
    {
        var map = new Dictionary<Entity, List<Entity>>();
        state.World.Query(new QueryDescription().WithAll<Following>(), (Entity follower, ref Following f) =>
        {
            if (!map.TryGetValue(f.Leader, out var list))
                map[f.Leader] = list = [];
            list.Add(follower);
        });
        return map;
    }

    private Dictionary<Entity, List<Entity>> BuildCharmiesByMaster(GameState state)
    {
        var map = new Dictionary<Entity, List<Entity>>();
        state.World.Query(new QueryDescription().WithAll<Charmed>(), (Entity charmie, ref Charmed c) =>
        {
            if (!map.TryGetValue(c.Master, out var list))
                map[c.Master] = list = [];
            list.Add(charmie);
        });
        return map;
    }

    // ------------------------------------------------------------------
    private void VisitChildren(
        GameState state,
        Entity leader,
        MoveIntent leaderIntent,
        Dictionary<Entity, List<Entity>> followersByLeader,
        Dictionary<Entity, List<Entity>> charmiesByMaster,
        HashSet<Entity> visited,
        List<(Entity, MoveIntent)> result)
    {
        if (followersByLeader.TryGetValue(leader, out var followers))
        {
            foreach (var follower in followers)
            {
                if (!visited.Add(follower)) continue;

                result.Add((follower, leaderIntent));

                if (WillBeAbleToFollow(state, follower, leaderIntent))
                {
                    var derived = leaderIntent with { Actor = follower };
                    VisitChildren(state, follower, derived, followersByLeader, charmiesByMaster, visited, result);
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

    private bool WillBeAbleToFollow(GameState state, Entity follower, MoveIntent leaderIntent)
    {
        if (follower.Has<CombatState>()) return false;
        // TODO
        //if (follower.Has<Stunned>()) return false;
        //if (follower.Has<Incapacitated>()) return false;
        //if (follower.Has<Sleeping>()) return false;

        ref readonly var loc = ref follower.Get<Location>();
        if (loc.Room != leaderIntent.FromRoom) return false;

        if (!MovementValidator.CanEnter(
                follower,
                leaderIntent.FromRoom, leaderIntent.ToRoom,
                leaderIntent.Direction, out _)) return false;

        return true;
    }

    // ------------------------------------------------------------------
    private void TryEmitFollowMove(GameState state, Entity follower, MoveIntent leaderIntent)
    {
        // Already queued their own move this tick
        foreach (ref var existing in _intents.MoveSpan)
            if (existing.Actor == follower) return;

        if (follower.Has<CombatState>()) return;
        // TODO
        //if (follower.Has<Stunned>()) return;
        //if (follower.Has<Incapacitated>()) return;
        //if (follower.Has<Sleeping>()) return;

        ref readonly var loc = ref follower.Get<Location>();
        if (loc.Room != leaderIntent.FromRoom) return;

        if (!MovementValidator.CanEnter(
                follower,
                leaderIntent.FromRoom, leaderIntent.ToRoom,
                leaderIntent.Direction, out var blockReason))
        {
            EmitCannotFollowMessage(follower, leaderIntent.Actor, blockReason);

            if (follower.Has<Charmed>())
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

    private void EmitCannotFollowMessage(Entity follower, Entity leader, string blockReason)
    {
        _msg.To(follower).Act("You cannot follow {0}: {1}.").With(leader, blockReason);
        _msg.To(leader).Act("{0} cannot follow you: {1}.").With(follower, blockReason);
    }

    private void HandleCharmieLeash(Entity charmie, MoveIntent leaderIntent)
    {
        ref var intent = ref _intents.Move.Add();
        intent.Actor = charmie;
        intent.FromRoom = leaderIntent.FromRoom;
        intent.ToRoom = leaderIntent.ToRoom;
        intent.Direction = leaderIntent.Direction;
        intent.AutoLook = true;
    }
}
