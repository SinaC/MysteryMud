using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;

namespace MysteryMud.Application.Commands;

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            systemContext.MessageBus.Publish(actor, "Your inventory is empty.");
        }
        else
        {
            systemContext.MessageBus.Publish(actor, "You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    systemContext.MessageBus.Publish(actor, $"- {item.DisplayName}");
            }
        }
    }
}