using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class DropCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Position>().Room;

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            ItemMovementSystem.DropItem(actor, room, item);
            MessageSystem.Send(actor, $"You drop {item.DisplayName}.");
        }
    }
}
