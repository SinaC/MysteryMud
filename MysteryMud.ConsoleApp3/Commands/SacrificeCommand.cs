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

public class SacrificeCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        // search in room
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            DestroySystem.DestroyItem(item);

            MessageBus.Publish(actor, $"You sacrifice the {item.DisplayName}.");
        }
    }
}
