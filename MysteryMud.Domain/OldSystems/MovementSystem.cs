using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.OldSystems;

public static class MovementSystem
{
    public static void Move(SystemContext ctx, Entity entity, Entity newRoom, Directions direction)
    {
        ref var location = ref entity.TryGetRef<Location>(out var hasLocation);
        if (hasLocation) // entity can be an item in an inventory, so check if it has a location before trying to move it
        {
            var oldRoom = location.Room;
            var oldRoomContents = oldRoom.Get<RoomContents>();
            var newRoomContents = newRoom.Get<RoomContents>();

            if (entity.Has<ItemTag>()) // moving an item, so update room contents
            {
                oldRoomContents.Items.Remove(entity);
                newRoomContents.Items.Add(entity);
            }
            else // moving a character, so update room contents
            {
                oldRoomContents.Characters.Remove(entity);
                ctx.Msg.To(oldRoomContents.Characters).Act("{0} leaves {1}").With(entity, direction); // entity will not receive the msg, but the other characters in the room will
                ctx.Msg.To(newRoomContents.Characters).Act("{0} has arrived").With(entity); // entity will not receive the msg, but the other characters in the room will
                newRoomContents.Characters.Add(entity);
            }

            location.Room = newRoom;
        }
    }
}
