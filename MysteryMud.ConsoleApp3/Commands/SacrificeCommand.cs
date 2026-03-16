using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class SacrificeCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        // search in room
        ref var room = ref actor.Get<Position>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            DestroySystem.DestroyItem(item);

            MessageSystem.Send(actor, $"You sacrifice the {item.DisplayName}.");
        }
    }
}
