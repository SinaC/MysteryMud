using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Enums;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class EquipmentCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(Entity actor, CommandContext ctx)
    {
        ref var equipment = ref actor.Get<Equipment>();

        MessageSystem.SendMessage(actor, "You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
            {
                MessageSystem.SendMessage(actor, $"{slot}: {item.DisplayName}");
            }
            else
            {
                MessageSystem.SendMessage(actor, $"{slot}: nothing");
            }
        }
    }
}
