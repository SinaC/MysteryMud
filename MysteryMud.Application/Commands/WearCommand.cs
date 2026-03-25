using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Systems;

namespace MysteryMud.Application.Commands;

public class WearCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            if (!item.Has<Equipable>())
            {
                systemContext.MessageBus.Publish(actor, "You can't wear that.");
                return;
            }

            if (!EquipmentSystem.Equip(actor, item))
            {
                systemContext.MessageBus.Publish(actor, "Slot already used.");
                return;
            }

            systemContext.MessageBus.Publish(actor, $"You wear {item.DisplayName}.");
            return;
        }
    }
}
