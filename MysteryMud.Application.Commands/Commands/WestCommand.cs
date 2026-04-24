using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class WestCommand : ICommand
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public WestCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (_world.Has<CombatState>(actor))
        {
            _msg.To(actor).Send("No way! You are still fighting!");
            return;
        }

        // Get room
        ref var room = ref _world.Get<Location>(actor).Room;

        // Get west exit
        ref var roomGraph = ref _world.Get<RoomGraph>(room);
        var westExit = roomGraph.Exits[DirectionKind.West];
        if (westExit is null || westExit!.Value.TargetRoom == EntityId.Invalid)
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
