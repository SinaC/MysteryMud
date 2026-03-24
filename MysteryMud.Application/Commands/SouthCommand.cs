using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Data.Enums;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Application.Systems;
using MysteryMud.Core.Command;

namespace MysteryMud.Application.Commands;

public class SouthCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.None;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get south exit
        var roomGraph = room.Get<RoomGraph>();
        var southExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Direction.South);
        if (southExit.Equals(default(Exit)) || southExit.TargetRoom == Entity.Null)
        {
            systemContext.MessageBus.Publish(actor, "Alas, you cannot go that way.");
            return;
        }

        systemContext.MessageBus.Publish(actor, $"You leaves south."); // TODO send message to current room: "{actor} leaves south."
        MovementSystem.Move(actor, southExit.TargetRoom);
        DisplayRoomSystem.DisplayRoom(systemContext, actor, southExit.TargetRoom);
        // TODO: send message to target room: "{actor} has arrived."
    }
}
