using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class KillCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        if (target.Equals(actor))
        {
            MessageBus.Publish(actor, "You hit yourself. Ouch.");
            return;
        }

        // TODO: check if already in combat, if so, maybe switch targets? Or maybe not allow switching targets?

        MessageBus.Publish(actor, $"{actor.DisplayName} attacks {target.DisplayName}");
        actor.Add(new CombatState { Target = target, RoundDelay = 0 });
        target.Add(new CombatState { Target = actor, RoundDelay = 1 }); // strikes back
    }
}
