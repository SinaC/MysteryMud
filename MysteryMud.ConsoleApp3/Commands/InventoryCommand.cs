using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            MessageSystem.SendMessage(actor, "Your inventory is empty.");
        }
        else
        {
            MessageSystem.SendMessage(actor, "You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    MessageSystem.SendMessage(actor, $"- {item.DisplayName}");
            }
        }
    }
}