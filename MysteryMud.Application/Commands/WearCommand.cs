using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;

namespace MysteryMud.Application.Commands;

public class WearCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            executionContext.Msg.To(actor).Send("Wear what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            ref var equipable = ref item.TryGetRef<Equipable>(out var isEquipable);
            if (!isEquipable)
            {
                executionContext.Msg.To(actor).Send("You can't wear that.");
                return;
            }

            ref var equipment = ref actor.Get<Equipment>();

            var slot = equipable.Slot;

            if (equipment.Slots.ContainsKey(slot))
            {
                executionContext.Msg.To(actor).Send("You already wear.");
                return;
            }

            // intent to wear item
            ref var wearItemIntent = ref executionContext.Intent.WearItem.Add();
            wearItemIntent.Actor = actor;
            wearItemIntent.Item = item;
            wearItemIntent.Slot = slot;
        }
    }
}
