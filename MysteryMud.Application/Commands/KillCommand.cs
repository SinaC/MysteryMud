using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class KillCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public KillCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Kill whom ?");
            return;
        }

        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        if (target.Equals(actor))
        {
            systemContext.Msg.To(actor).Send("You hit yourself. Ouch.");
            return;
        }

        // TODO: check if already in combat, if so, maybe switch targets? Or maybe not allow switching targets?

        // flag both as in combat with each other, with the target striking back after a delay
        actor.Add(new CombatState { Target = target, RoundDelay = 0 });
        target.Add(new CombatState { Target = actor, RoundDelay = 1 }); // strikes back
    }
}
