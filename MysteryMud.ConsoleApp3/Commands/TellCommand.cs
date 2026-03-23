using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class TellCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetAndText;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        var message = ctx.Text;

        if (ctx.Primary.Name.IsEmpty)
        {
            MessageBus.Publish(actor, "Tell whom?");
            return;
        }

        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var target in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents))
        {  
            MessageBus.Publish(actor, $"You tell {target.DisplayName}: {message}");
            MessageBus.Publish(target, $"{actor.DisplayName} tells you: {message}");
        }
    }
}
