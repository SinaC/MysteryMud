using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class GetCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public GetCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Get what ?");
            return;
        }

        if (ctx.Secondary.Name.IsEmpty)
        {
            // default: room
            var room = actor.Get<Location>().Room;
            var roomContents = room.Get<RoomContents>();
            foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
            {
                ItemMovementSystem.GetItemFromRoom(actor, room, item);
                systemContext.Msg.To(actor).Send($"You get {item.DisplayName}.");
            }
        }
        else
        {
            var container = FindContainer(actor, ctx.Secondary);
            if (container == default)
            {
                systemContext.Msg.To(actor).Send("You don't see that here.");
                return;
            }

            var containerContents = container.Get<ContainerContents>();
            foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, containerContents.Items))
            {
                ItemMovementSystem.GetItemFromContainer(actor, container, item);
                systemContext.Msg.To(actor).Send($"You get {item.DisplayName} from {container.DisplayName}.");
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
