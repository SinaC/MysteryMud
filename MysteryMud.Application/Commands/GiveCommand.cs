using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Application.Commands.Parser;
using MysteryMud.Application.Systems;

namespace MysteryMud.Application.Commands;

public class GiveCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        var room = actor.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        // Find target character in room
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == default)
        {
            systemContext.MessageBus.Publish(actor, "They are not here.");
            return;
        }

        // Move item
        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            ItemMovementSystem.GiveItem(actor, target, item);
            systemContext.MessageBus.Publish(actor, $"You give {item.DisplayName} to {target.DisplayName}.");
        }
    }
}
