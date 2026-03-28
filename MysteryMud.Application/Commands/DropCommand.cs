using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class DropCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public DropCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Drop what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            ItemMovementSystem.DropItem(actor, room, item);
            systemContext.Msg.To(actor).Send($"You drop {item.DisplayName}.");
        }
    }
}
