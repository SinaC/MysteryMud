using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class NorthCommand : ICommand
{
    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (actor.Has<CombatState>())
        {
            executionContext.Msg.To(actor).Send("No way! You are still fighting!");
            return;
        }

        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get North exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == DirectionKind.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            executionContext.Msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref executionContext.Intent.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = northExit.TargetRoom;
        moveIntent.Direction = DirectionKind.North;
        moveIntent.AutoLook = true;
    }
}
