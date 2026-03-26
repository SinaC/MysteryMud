using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Systems;

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

        ctx.Msg.Send(actor, $"{roomName.Value}");
        ctx.Msg.Send(actor, $"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            ctx.Msg.Send(actor, "No exits.");
        }
        else
        {
            ctx.Msg.Send(actor, "Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                ctx.Msg.Send(actor, $"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        ctx.Msg.Send(actor, "Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            ctx.Msg.Send(actor, $"- {c.DisplayName}");
        }

        ctx.Msg.Send(actor, "Items here:");
        foreach (var item in roomItems)
        {
            ctx.Msg.Send(actor, $"- {item.DisplayName}");
        }
    }
}
