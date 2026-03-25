using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Systems;

namespace MysteryMud.Application.Commands;

public class DropCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            ItemMovementSystem.DropItem(actor, room, item);
            systemContext.MessageBus.Publish(actor, $"You drop {item.DisplayName}.");
        }
    }
}
