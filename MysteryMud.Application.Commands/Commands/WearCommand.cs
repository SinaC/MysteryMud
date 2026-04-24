using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class WearCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public WearCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Wear what ?");
            return;
        }

        ref var inventory = ref _world.Get<Inventory>(actor);
        ref var equipment = ref _world.Get<Equipment>(actor);

        foreach (var item in CommandEntityFinder.SelectTargets(_world,  actor, ctx.Primary, inventory.Items))
        {
            ref var equipable = ref _world.TryGetRef<Equipable>(item, out var isEquipable);
            if (!isEquipable)
            {
                _msg.To(actor).Send("You can't wear that.");
                return;
            }

            var slot = equipable.Slot;
            if (equipment.Slots.ContainsKey(slot))
            {
                _msg.To(actor).Send("You already wear.");
                return;
            }

            // intent to wear item
            ref var wearItemIntent = ref _intents.WearItem.Add();
            wearItemIntent.Entity = actor;
            wearItemIntent.Item = item;
            wearItemIntent.Slot = slot;
        }
    }
}
