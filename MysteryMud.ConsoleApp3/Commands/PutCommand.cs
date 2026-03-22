using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class PutCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();

        var container = FindContainer(actor, ctx.Secondary);
        if (container == default)
        {
            MessageSystem.Send(actor, "You don't see that here.");
            return;
        }

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            ItemMovementSystem.PutItem(actor, container, item);

            MessageSystem.Send(actor, $"You put {item.DisplayName} in {container.DisplayName}.");
        }
    }

    private Entity FindContainer(Entity actor, TargetSpec containerArg)
    {
        // Search in room first
        var room = actor.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        var container = TargetingSystem.SelectSingleTarget(actor, containerArg, roomContents.Items);
        if (container != default)
            return container;

        // Then inventory
        var inventory = actor.Get<Inventory>();
        container = TargetingSystem.SelectSingleTarget(actor, containerArg, inventory.Items);
        if (container != default)
            return container;

        return default;
    }
}
