using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Factories;

public static class RoomFactory
{
    public static EntityId StartingRoomEntity; // TODO: remove these global variables and find a better way to reference important rooms (e.g. starting room, respawn room) without hardcoding their IDs everywhere
    public static EntityId RespawnRoomEntity;

    public static EntityId CreateRoom(World world, int id, string name, string description)
    {
        return world.CreateEntity(
            new Room { Id = id },
            new Name { Value = name },
            new Description { Value = description },
            new RoomGraph { Exits = new RoomExitValues() },
            new RoomContents
            {
                Characters = [],
                Items = []
            },
            new RoomNeighborhood
            {
                Distance1 = [],
                Distance2 = []
            }
        );
    }

    public static bool LinkRoom(World world, EntityId sourceRoom, EntityId targetRoom, DirectionKind direction)
    {
        ref var sourceRoomGraph = ref world.Get<RoomGraph>(sourceRoom);
        if (sourceRoomGraph.Exits[direction] is not null)
        {
            return false; // Exit already exists in this direction
        }

        sourceRoomGraph.Exits[direction] = new Exit { Direction = direction, TargetRoom = targetRoom }; // TODO: close + description
        return true;
    }
}
