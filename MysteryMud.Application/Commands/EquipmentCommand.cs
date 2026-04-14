using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class EquipmentCommand : ICommand
{
    private readonly IGameMessageService _msg;

    public EquipmentCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var equipment = ref actor.Get<Equipment>();

        _msg.To(actor).Send("You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                _msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                _msg.To(actor).Send($"{slot}: nothing");
        }
    }
}
