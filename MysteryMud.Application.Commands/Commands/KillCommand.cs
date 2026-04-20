using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public class KillCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;

    public KillCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Kill whom ?");
            return;
        }

        ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, people);

        if (target == null)
        {
            _msg.To(actor).Send("They aren't here.");
            return;
        }

        if (target.Equals(actor))
        {
            _msg.To(actor).Send("You hit yourself. Ouch.");
            return;
        }

        // TODO: check if already in combat, if so, maybe switch targets? Or maybe not allow switching targets?
        if (actor.Has<CombatState>())
        {
            _msg.To(actor).Send("You do the best you can!");
            return;
        }

        // TODO: check if target is already fighting

        // flag both as in combat with each other, with the target striking back after a delay
        CombatHelpers.EnterCombat(state, actor, target.Value);
    }
}
