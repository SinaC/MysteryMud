using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Core.Command;

namespace MysteryMud.Application.Commands;

public class EquipmentCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var equipment = ref actor.Get<Equipment>();

        systemContext.MessageBus.Publish(actor, "You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                systemContext.MessageBus.Publish(actor, $"{slot}: {item.DisplayName}");
            else
                systemContext.MessageBus.Publish(actor, $"{slot}: nothing");
        }
    }
}
