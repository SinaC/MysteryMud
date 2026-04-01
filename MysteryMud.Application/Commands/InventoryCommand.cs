using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    public CommandDefinition Definition { get; }

    public InventoryCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var inventory = ref actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            systemContext.Msg.To(actor).Send("Your inventory is empty.");
        }
        else
        {
            systemContext.Msg.To(actor).Send("You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    systemContext.Msg.To(actor).Send($"- {item.DisplayName}");
            }
        }
    }
}