using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class TellCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetAndText;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        var message = ctx.Text;

        if (ctx.Primary.Name.IsEmpty)
        {
            MessageSystem.Send(actor, "Tell whom?");
            return;
        }

        var roomContents = actor.Get<Position>().Room.Get<RoomContents>().Characters;
        foreach (var target in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents))
        {  
            MessageSystem.Send(actor, $"You tell {target.DisplayName}: {message}");
            MessageSystem.Send(target, $"{actor.DisplayName} tells you: {message}");
        }
    }
}
