using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Application.Commands;

public class KillCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            executionContext.Msg.To(actor).Send("Kill whom ?");
            return;
        }

        ref var people = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, people);

        if (target == default)
        {
            executionContext.Msg.To(actor).Send("They aren't here.");
            return;
        }

        if (target.Equals(actor))
        {
            executionContext.Msg.To(actor).Send("You hit yourself. Ouch.");
            return;
        }

        // TODO: check if already in combat, if so, maybe switch targets? Or maybe not allow switching targets?
        if (actor.Has<CombatState>())
        {
            executionContext.Msg.To(actor).Send("You do the best you can!");
            return;
        }    

        // TODO: check if target is already fighting

        // flag both as in combat with each other, with the target striking back after a delay
        actor.Add(new CombatState { Target = target, RoundDelay = 0 });
        if (!target.Has<CombatState>()) // TODO: initiator, last target, ...
            target.Add(new CombatState { Target = actor, RoundDelay = 1 }); // strikes back
    }
}
