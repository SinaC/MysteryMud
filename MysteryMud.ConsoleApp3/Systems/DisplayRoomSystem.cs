using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Components.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

public class DisplayRoomSystem
{
    public static void DisplayRoom(SystemContext ctx, Entity actor, Entity room)
    {
        // Get room name, description and contents and graph
        ref var roomName = ref room.Get<Name>();
        ref var roomDescription = ref room.Get<Description>();
        ref var roomContents = ref room.Get<RoomContents>();
        ref var roomGraph = ref room.Get<RoomGraph>();
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        ctx.MessageBus.Publish(actor, $"{roomName.Value}");
        ctx.MessageBus.Publish(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            ctx.MessageBus.Publish(actor, "No exits.");
        }
        else
        {
            ctx.MessageBus.Publish(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                ctx.MessageBus.Publish(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        ctx.MessageBus.Publish(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            ctx.MessageBus.Publish(actor, $"- {c.DisplayName}");
        }

        ctx.MessageBus.Publish(actor, "Items here:");
        foreach (var item in roomItems)
        {
            ctx.MessageBus.Publish(actor, $"- {item.DisplayName}");
        }
    }
}
