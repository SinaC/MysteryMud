using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class SouthCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.None;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get south exit
        var roomGraph = room.Get<RoomGraph>();
        var southExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Direction.South);
        if (southExit.Equals(default(Exit)) || southExit.TargetRoom == Entity.Null)
        {
            MessageSystem.Send(actor, "Alas, you cannot go that way.");
            return;
        }

        MessageSystem.Send(actor, $"You leaves south."); // TODO send message to current room: "{actor} leaves south."
        MovementSystem.Move(actor, southExit.TargetRoom);
        DisplayRoomSystem.DisplayRoom(actor, southExit.TargetRoom);
        // TODO: send message to target room: "{actor} has arrived."
    }
}
