using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems
{
    class MovementSystem
    {
        public static void Move(Entity entity, Entity newRoom)
        {
            ref var location = ref entity.TryGetRef<Location>(out var hasLocation);
            if (hasLocation) // entity can be an item in an inventory, so check if it has a location before trying to move it
            {
                var oldRoom = location.Room;

                if (entity.Has<Item>()) // moving an item, so update room contents
                {
                    oldRoom.Get<RoomContents>().Items.Remove(entity);
                    newRoom.Get<RoomContents>().Items.Add(entity);
                }
                else // moving a character, so update room contents
                {
                    oldRoom.Get<RoomContents>().Characters.Remove(entity);
                    newRoom.Get<RoomContents>().Characters.Add(entity);
                }

                location.Room = newRoom;
            }
        }
    }

}
