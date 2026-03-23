using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Systems;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Commands;

public class TellCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetAndText;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var message = ctx.Text;

        if (ctx.Primary.Name.IsEmpty)
        {
            systemContext.MessageBus.Publish(actor, "Tell whom?");
            return;
        }

        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var target in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents))
        {  
            systemContext.MessageBus.Publish(actor, $"You tell {target.DisplayName}: {message}");
            systemContext.MessageBus.Publish(target, $"{actor.DisplayName} tells you: {message}");
        }
    }
}
