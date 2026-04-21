using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public class WearCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public WearCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Wear what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in CommandEntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            ref var equipable = ref item.TryGetRef<Equipable>(out var isEquipable);
            if (!isEquipable)
            {
                _msg.To(actor).Send("You can't wear that.");
                return;
            }

            ref var equipment = ref actor.Get<Equipment>();

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
