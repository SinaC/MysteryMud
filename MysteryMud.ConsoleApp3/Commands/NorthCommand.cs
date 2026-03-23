using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

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
