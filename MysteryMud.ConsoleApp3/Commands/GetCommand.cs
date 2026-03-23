using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Components.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class GetCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.Secondary.Name.IsEmpty)
        {
            // default: room
            var room = actor.Get<Location>().Room;
            var roomContents = room.Get<RoomContents>();
            foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
            {
                ItemMovementSystem.GetItemFromRoom(actor, room, item);
                systemContext.MessageBus.Publish(actor, $"You get {item.DisplayName}.");
            }
        }
        else
        {
            var container = FindContainer(actor, ctx.Secondary);
            if (container == default)
            {
                systemContext.MessageBus.Publish(actor, "You don't see that here.");
                return;
            }

            var containerContents = container.Get<ContainerContents>();
            foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, containerContents.Items))
            {
                ItemMovementSystem.GetItemFromContainer(actor, container, item);
                systemContext.MessageBus.Publish(actor, $"You get {item.DisplayName} from {container.DisplayName}.");
            }
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
