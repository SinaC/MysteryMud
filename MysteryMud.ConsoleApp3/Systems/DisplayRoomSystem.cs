using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

public class DisplayRoomSystem
{
    public static void DisplayRoom(Entity actor, Entity room)
    {
        // Get room name, description and contents and graph
        ref var roomName = ref room.Get<Name>();
        ref var roomDescription = ref room.Get<Description>();
        ref var roomContents = ref room.Get<RoomContents>();
        ref var roomGraph = ref room.Get<RoomGraph>();
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        MessageSystem.Send(actor, $"{roomName.Value}");
        MessageSystem.Send(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            MessageSystem.Send(actor, "No exits.");
        }
        else
        {
            MessageSystem.Send(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                MessageSystem.Send(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        MessageSystem.Send(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            MessageSystem.Send(actor, $"- {c.DisplayName}");
        }

        MessageSystem.Send(actor, "Items here:");
        foreach (var item in roomItems)
        {
            MessageSystem.Send(actor, $"- {item.DisplayName}");
        }
    }
}
