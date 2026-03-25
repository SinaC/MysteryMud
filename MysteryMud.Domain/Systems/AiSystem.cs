using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Systems;

public static class AiSystem
{
    public static void Think(Entity npc)
    {
        if (!npc.Has<NpcTag>())
            return;

        var room = npc.Get<Location>().Room;
        var contents = room.Get<RoomContents>();

        foreach (var e in contents.Characters)
        {
            if (e.Has<PlayerTag>())
            {
                npc.Get<CombatState>().Target = e;
                break;
            }
        }
    }

    // neighborhood detection example
    //var neighborhood = npcRoom.Get<RoomNeighborhood>();
    //foreach (var room in neighborhood.Distance1)
    //{
    //    foreach (var player in room.Get<RoomContents>().Characters)
    //    {
    //        // detect player
    //    }
    //}
}
