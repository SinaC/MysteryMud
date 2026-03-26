using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class DestroyCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public DestroyCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.Send(actor, "Destroy what ?");
            return;
        }

        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                var equipped = item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            DestroySystem.DestroyItem(item);

            systemContext.Msg.Send(actor, $"You destroy {item.DisplayName}.");
        }
    }
}
