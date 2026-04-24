using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class FollowCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IFollowService _followService;

    public FollowCommand(World world, IGameMessageService msg, IFollowService followService)
    {
        _world = world;
        _msg = msg;
        _followService = followService;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // get existing Following if any
        ref var following = ref _world.TryGetRef<Following>(actor, out var isFollowing);

        // no argument: display leader
        if (ctx.TargetCount == 0)
        {
            if (isFollowing)
                _msg.To(actor).Act("You are following {0:N}.").With(following.Leader);
            else
                _msg.To(actor).Send("You are not following anyone.");
            return;
        }

        // search target
        var room = _world.Get<Location>(actor).Room;
        var people = _world.Get<RoomContents>(room).Characters;
        var target = EntityFinder.SelectSingleTarget(_world, actor, ctx.Primary.Kind, ctx.Primary.Index, ctx.Primary.Name, people);
        if (target == null)
        {
            _msg.To(actor).Send("They are not here.");
            return;
        }

        // follow ourself -> cancel follow
        if (target.Value == actor)
        {
            if (!isFollowing)
            {
                _msg.To(actor).Send("You already follow yourself.");
                return;
            }
            _followService.StopFollowing(actor);
            return;
        }

        var leader = GetLeader(actor);
        if (leader == target.Value)
        {
            _msg.To(actor).Act("You are already following {0:N}.").With(target.Value);
            return;
        }

        // check cycle
        var next = GetLeader(target.Value);
        while (next != null)
        {
            if (next.Value == actor)
            {
                _msg.To(actor).Act("You can't follow {0:N}.").With(target.Value);
                return;
            }
            next = GetLeader(next.Value);
        }

        // no cycle -> follow target
        _followService.Follow(actor, target.Value);
    }

    private EntityId? GetLeader(EntityId entity)
    {
        ref var following = ref _world.TryGetRef<Following>(entity, out var isFollowing);
        if (!isFollowing)
            return null;
        return following.Leader;
    }
}
