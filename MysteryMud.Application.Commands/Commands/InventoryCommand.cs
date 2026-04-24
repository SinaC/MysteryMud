using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

// important note: even when worn item stays in inventory
public sealed class InventoryCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public InventoryCommand(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var inventory = ref _world.Get<Inventory>(actor);
        if (inventory.Items.Count == 0)
        {
            _msg.To(actor).Send("Your inventory is empty.");
        }
        else
        {
            _msg.To(actor).Send("You are carrying:");
            foreach (var item in inventory.Items)
            {
                if (!_world.Has<Equipped>(item))
                    _msg.To(actor).Act("- {0}").With(item);
            }
        }
    }
}