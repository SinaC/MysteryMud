using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Application.Systems;
using MysteryMud.Core.Command;

namespace MysteryMud.Application.Commands;

public class NorthCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get North exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Direction.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            systemContext.MessageBus.Publish(actor, "Alas, you cannot go that way.");
            return;
        }

        systemContext.MessageBus.Publish(actor, $"You leaves north."); // TODO send message to current room: "{actor} leaves North."
        MovementSystem.Move(actor, northExit.TargetRoom);
        DisplayRoomSystem.DisplayRoom(systemContext, actor, northExit.TargetRoom);
        // TODO: send message to target room: "{actor} has arrived."
    }
}
