using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

public class DisplayRoomSystem
{
    public static void DisplayRoom(SystemContext systemContext, Entity actor, Entity room)
    {
        // Get room name, description and contents and graph
        ref var roomName = ref room.Get<Name>();
        ref var roomDescription = ref room.Get<Description>();
        ref var roomContents = ref room.Get<RoomContents>();
        ref var roomGraph = ref room.Get<RoomGraph>();
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        systemContext.MessageBus.Publish(actor, $"{roomName.Value}");
        systemContext.MessageBus.Publish(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            systemContext.MessageBus.Publish(actor, "No exits.");
        }
        else
        {
            systemContext.MessageBus.Publish(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                systemContext.MessageBus.Publish(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        systemContext.MessageBus.Publish(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            systemContext.MessageBus.Publish(actor, $"- {c.DisplayName}");
        }

        systemContext.MessageBus.Publish(actor, "Items here:");
        foreach (var item in roomItems)
        {
            systemContext.MessageBus.Publish(actor, $"- {item.DisplayName}");
        }
    }
}
