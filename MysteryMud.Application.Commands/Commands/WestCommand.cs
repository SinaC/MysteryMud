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

public sealed class WestCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public WestCommand(IGameMessageService msg, IIntentWriterContainer intents)
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

        // Get west exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var westExit = roomGraph.Exits[DirectionKind.West];
        if (westExit is null || westExit!.Value.TargetRoom == default)
        {
            _msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref _intents.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = westExit!.Value.TargetRoom;
        moveIntent.Direction = DirectionKind.West;
        moveIntent.AutoLook = true;
    }
}
