using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Systems;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Items;

namespace MysteryMud.ConsoleApp3.Commands;

public class DestroyCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                var equipped = item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            DestroySystem.DestroyItem(item);

            systemContext.MessageBus.Publish(actor, $"You destroy {item.DisplayName}.");
        }
    }
}
