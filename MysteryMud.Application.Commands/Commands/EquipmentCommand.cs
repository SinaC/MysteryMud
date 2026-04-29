using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands;

public sealed class EquipmentCommand : ICommand
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
                _msg.To(actor).Act("{0}: {1}").With(slot, item);
            else
                _msg.To(actor).Send($"{slot}: nothing");
        }
    }
}
