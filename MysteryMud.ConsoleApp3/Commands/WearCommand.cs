using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class WearCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            if (!item.Has<Equipable>())
            {
                MessageSystem.Send(actor, "You can't wear that.");
                return;
            }

            if (!EquipmentSystem.Equip(actor, item))
            {
                MessageSystem.Send(actor, "Slot already used.");
                return;
            }

            MessageSystem.Send(actor, $"You wear {item.DisplayName}.");
            return;
        }
    }
}
