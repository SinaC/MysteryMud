using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class EastCommand : ICommand
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
        var eastExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == DirectionKind.East);
        if (eastExit.Equals(default(Exit)) || eastExit.TargetRoom == Entity.Null)
        {
            _msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref _intents.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = eastExit.TargetRoom;
        moveIntent.Direction = DirectionKind.East;
        moveIntent.AutoLook = true;
    }
}
