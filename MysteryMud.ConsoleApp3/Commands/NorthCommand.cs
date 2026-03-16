using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Enums;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class NorthCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(Entity actor, CommandContext ctx)
    {
        // Get room
        var position = actor.Get<Position>();
        var room = position.Room;

        // Get North exit
        var roomGraph = room.Get<RoomGraph>();
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Direction.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            MessageSystem.SendMessage(actor, "Alas, you cannot go that way.");
            return;
        }

        MessageSystem.SendMessage(actor, $"You leaves north."); // TODO send message to current room: "{actor} leaves North."
        MovementSystem.Move(actor, northExit.TargetRoom);
        DisplayRoomSystem.DisplayRoom(actor, northExit.TargetRoom);
        // TODO: send message to target room: "{actor} has arrived."
    }
}
