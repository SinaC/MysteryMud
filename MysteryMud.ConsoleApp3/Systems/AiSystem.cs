using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Mobiles;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems
{
    class AiSystem
    {
        public static void Think(Entity npc)
        {
            if (!npc.Has<NpcTag>())
                return;

            var room = npc.Get<Position>().Room;
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
}
