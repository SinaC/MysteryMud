using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class EquipmentCommand : ICommand
{
    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var equipment = ref actor.Get<Equipment>();

        executionContext.Msg.To(actor).Send("You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                executionContext.Msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                executionContext.Msg.To(actor).Send($"{slot}: nothing");
        }
    }
}
