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

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            MessageBus.Publish(actor, "Your inventory is empty.");
        }
        else
        {
            MessageBus.Publish(actor, "You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    MessageBus.Publish(actor, $"- {item.DisplayName}");
            }
        }
    }
}