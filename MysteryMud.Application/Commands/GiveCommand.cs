using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class GiveCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public GiveCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount < 2)
        {
            systemContext.Msg.To(actor).Send("Give what to whom ?");
            return;
        }

        var inventory = actor.Get<Inventory>();
        var room = actor.Get<Location>().Room;
        var roomContents = room.Get<RoomContents>();

        // Find target character in room
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == default)
        {
            systemContext.Msg.To(actor).Send("They are not here.");
            return;
        }

        // Move item
        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // Unequip if necessary
            if (item.Has<Equipped>())
            {
                ref var equipped = ref item.Get<Equipped>();
                EquipmentSystem.Unequip(actor, equipped.Slot);
            }

            ItemMovementSystem.GiveItem(actor, target, item);
            systemContext.Msg.To(actor).Send($"You give {item.DisplayName} to {target.DisplayName}.");
        }
    }
}
