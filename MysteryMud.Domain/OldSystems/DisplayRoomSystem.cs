using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.OldSystems;

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

        ctx.Msg.To(actor).Send($"{roomName.Value}");
        ctx.Msg.To(actor).Send($"{roomDescription.Value}");
        if (roomGraph.Exits.Count == 0)
        {
            ctx.Msg.To(actor).Send("No exits.");
        }
        else
        {
            ctx.Msg.To(actor).Send("Exits:");
            foreach (var exit in roomGraph.Exits)
            {
                ctx.Msg.To(actor).Send($"- {exit.Direction} - {exit.TargetRoom.DisplayName}");
            }
        }
        ctx.Msg.To(actor).Send("Characters here:");
        foreach (var c in roomCharacters)
        {
            if (c.Equals(actor)) continue; // skip self
            ctx.Msg.To(actor).Send($"- {c.DisplayName}");
        }

        ctx.Msg.To(actor).Send("Items here:");
        foreach (var item in roomItems)
        {
            ctx.Msg.To(actor).Send($"- {item.DisplayName}");
        }
    }
}
