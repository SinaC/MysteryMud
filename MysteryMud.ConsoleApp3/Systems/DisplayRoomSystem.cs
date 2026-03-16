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

        MessageSystem.SendMessage(actor, $"{roomName.Value}");
        MessageSystem.SendMessage(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            MessageSystem.SendMessage(actor, "No exits.");
        }
        else
        {
            MessageSystem.SendMessage(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                MessageSystem.SendMessage(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        MessageSystem.SendMessage(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            MessageSystem.SendMessage(actor, $"- {c.DisplayName}");
        }

        MessageSystem.SendMessage(actor, "Items here:");
        foreach (var item in roomItems)
        {
            MessageSystem.SendMessage(actor, $"- {item.DisplayName}");
        }
    }
}
