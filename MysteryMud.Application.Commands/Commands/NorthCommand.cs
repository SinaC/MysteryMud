using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands.Commands;

public class NorthCommand : ICommand
{
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public NorthCommand(IGameMessageService msg, IIntentWriterContainer intents)
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

        // Get North exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == DirectionKind.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            _msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref _intents.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = northExit.TargetRoom;
        moveIntent.Direction = DirectionKind.North;
        moveIntent.AutoLook = true;
    }
}
