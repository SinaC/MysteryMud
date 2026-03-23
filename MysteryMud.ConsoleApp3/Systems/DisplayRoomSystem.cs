using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core.Eventing;
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

        MessageBus.Publish(actor, $"{roomName.Value}");
        MessageBus.Publish(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            MessageBus.Publish(actor, "No exits.");
        }
        else
        {
            MessageBus.Publish(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                MessageBus.Publish(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        MessageBus.Publish(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            MessageBus.Publish(actor, $"- {c.DisplayName}");
        }

        MessageBus.Publish(actor, "Items here:");
        foreach (var item in roomItems)
        {
            MessageBus.Publish(actor, $"- {item.DisplayName}");
        }
    }
}
