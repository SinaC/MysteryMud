using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class WearCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            if (!item.Has<Equipable>())
            {
                MessageBus.Publish(actor, "You can't wear that.");
                return;
            }

            if (!EquipmentSystem.Equip(actor, item))
            {
                MessageBus.Publish(actor, "Slot already used.");
                return;
            }

            MessageBus.Publish(actor, $"You wear {item.DisplayName}.");
            return;
        }
    }
}
