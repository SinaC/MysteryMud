using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class GiveCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        var room = actor.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        // Find target character in room
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == default)
        {
            MessageSystem.Send(actor, "They are not here.");
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
            MessageSystem.Send(actor, $"You give {item.DisplayName} to {target.DisplayName}.");
        }
    }
}
