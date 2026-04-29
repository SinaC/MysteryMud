using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands;

public sealed class EastCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public EastCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (actor.Has<CombatState>())
        {
            _msg.To(actor).Send("No way! You are still fighting!");
            return;
        }

        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get east exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var eastExit = roomGraph.Exits[DirectionKind.East];
        if (eastExit is null || eastExit!.Value.TargetRoom == default)
        {
            _msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref _intents.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = eastExit!.Value.TargetRoom;
        moveIntent.Direction = DirectionKind.East;
        moveIntent.AutoLook = true;
    }
}
