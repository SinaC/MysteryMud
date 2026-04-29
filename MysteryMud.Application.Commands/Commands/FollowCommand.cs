using DefaultEcs;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class FollowCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IFollowService _followService;

    public FollowCommand(IGameMessageService msg, IFollowService followService)
    {
        _msg = msg;
        _followService = followService;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // get leader
        var leader = GetLeader(actor);

        // no argument: display leader
        if (ctx.TargetCount == 0)
        {
            if (leader is not null)
                _msg.To(actor).Act("You are following {0:N}.").With(leader.Value);
            else
                _msg.To(actor).Send("You are not following anyone.");
            return;
        }

        // search target
        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary.Kind, ctx.Primary.Index, ctx.Primary.Name, people);
        if (target == null)
        {
            _msg.To(actor).Send("They are not here.");
            return;
        }

        // follow ourself -> cancel follow
        if (target.Value == actor)
        {
            if (leader is null)
            {
                _msg.To(actor).Send("You already follow yourself.");
                return;
            }
            _followService.StopFollowing(actor);
            return;
        }

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

    private static Entity? GetLeader(Entity entity)
    {
        if (!entity.Has<Following>())
            return null;
        ref var following = ref entity.Get<Following>();
        return following.Leader;
    }
}
