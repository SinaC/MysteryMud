using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class EquipmentCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public EquipmentCommand(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        ref var equipment = ref _world.Get<Equipment>(actor);

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
