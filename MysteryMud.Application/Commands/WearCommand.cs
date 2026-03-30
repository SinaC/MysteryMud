using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class WearCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public WearCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Wear what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            ref var equipable = ref item.TryGetRef<Equipable>(out var isEquipable);
            if (!isEquipable)
            {
                systemContext.Msg.To(actor).Send("You can't wear that.");
                return;
            }

            ref var equipment = ref actor.Get<Equipment>();

            var slot = equipable.Slot;

            if (equipment.Slots.ContainsKey(slot))
            {
                systemContext.Msg.To(actor).Send("You already wear.");
                return;
            }

            // intent to wear item
            ref var wearItemIntent = ref systemContext.Intent.WearItem.Add();
            wearItemIntent.Actor = actor;
            wearItemIntent.Item = item;
            wearItemIntent.Slot = slot;
        }
    }
}
