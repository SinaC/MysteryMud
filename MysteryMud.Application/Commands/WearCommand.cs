using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
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

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Wear what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            if (!item.Has<Equipable>())
            {
                systemContext.Msg.To(actor).Send("You can't wear that.");
                return;
            }

            if (!EquipmentSystem.Equip(actor, item))
            {
                systemContext.Msg.To(actor).Send("Slot already used.");
                return;
            }

            systemContext.Msg.To(actor).Send($"You wear {item.DisplayName}.");
            return;
        }
    }
}
