using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Application.Commands;

// important note: even when worn item stays in inventory
public class InventoryCommand : ICommand
{
    private readonly IGameMessageService _msg;

    public InventoryCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var inventory = ref actor.Get<Inventory>();
        if (inventory.Items.Count == 0)
        {
            _msg.To(actor).Send("Your inventory is empty.");
        }
        else
        {
            _msg.To(actor).Send("You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!item.Has<Equipped>())
                    _msg.To(actor).Send($"- {item.DisplayName}");
            }
        }
    }
}