using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Queries;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public class FollowCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;

    public FollowCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // get existing Following if any
        ref var following = ref actor.TryGetRef<Following>(out var isFollowing);

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
            if (!isFollowing)
            {
                _msg.To(actor).Send("You already follow yourself.");
                return;
            }
            // inform leader
            _msg.To(following.Leader).Act("{0:N} stops following you.").With(actor);
            _msg.To(actor).Act("You stop following {0:N}.").With(following.Leader);

            // remove following
            actor.Remove<Following>();
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
        if (isFollowing)
        {
            // inform previous leader
            _msg.To(following.Leader).Act("{0:N} stops following you.").With(actor);
            _msg.To(actor).Act("You stop following {0:N}.").With(following.Leader);

            // change leader
            following.Leader = target.Value;
        }
        else
            actor.Add(new Following { Leader = target.Value });
        // inform new leader
        _msg.To(actor).Act("You start following {0:N}.").With(target.Value);
        _msg.To(target.Value).Act("{0:N} starts following you.").With(actor);
    }

    private Entity? GetLeader(Entity entity)
    {
        ref var following = ref entity.TryGetRef<Following>(out var isFollowing);
        if (!isFollowing)
            return null;
        return following.Leader;
    }
}
