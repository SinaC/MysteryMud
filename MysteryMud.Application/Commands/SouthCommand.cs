using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class SouthCommand : ICommand
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

        // Get south exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var southExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == DirectionKind.South);
        if (southExit.Equals(default(Exit)) || southExit.TargetRoom == Entity.Null)
        {
            executionContext.Msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref executionContext.Intent.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = southExit.TargetRoom;
        moveIntent.Direction = DirectionKind.South;
        moveIntent.AutoLook = true;
    }
}
