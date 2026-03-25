using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;
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

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.MessageBus.Publish(actor, "Kill whom ?");
            return;
        }

        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            systemContext.MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        if (target.Equals(actor))
        {
            systemContext.MessageBus.Publish(actor, "You hit yourself. Ouch.");
            return;
        }

        // TODO: check if already in combat, if so, maybe switch targets? Or maybe not allow switching targets?

        systemContext.MessageBus.Publish(actor, $"{actor.DisplayName} attacks {target.DisplayName}");
        actor.Add(new CombatState { Target = target, RoundDelay = 0 });
        target.Add(new CombatState { Target = actor, RoundDelay = 1 }); // strikes back
    }
}
