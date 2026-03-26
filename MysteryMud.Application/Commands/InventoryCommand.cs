using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.None;
    public CommandDefinition Definition { get; }

    public InventoryCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var inventory = actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            systemContext.Msg.Send(actor, "Your inventory is empty.");
        }
        else
        {
            systemContext.Msg.Send(actor, "You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    systemContext.Msg.Send(actor, $"- {item.DisplayName}");
            }
        }
    }
}