using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;

namespace MysteryMud.Application.Commands;

public class PutCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.TargetPair;

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
