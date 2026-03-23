using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Systems;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Commands;

public class PutCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();

        var container = FindContainer(actor, ctx.Secondary);
        if (container == default)
        {
            systemContext.MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            ItemMovementSystem.PutItem(actor, container, item);

            systemContext.MessageBus.Publish(actor, $"You put {item.DisplayName} in {container.DisplayName}.");
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
