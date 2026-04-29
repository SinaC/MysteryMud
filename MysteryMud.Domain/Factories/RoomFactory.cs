using DefaultEcs;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class RoomFactory
{
    public static Entity StartingRoomEntity; // TODO: remove these global variables and find a better way to reference important rooms (e.g. starting room, respawn room) without hardcoding their IDs everywhere
    public static Entity RespawnRoomEntity;

    public static Entity CreateRoom(World world, int id, string name, string description)
    {
        var room = world.CreateEntity();
        room.Set(new Room { Id = id });
        room.Set(new Name { Value = name });
        room.Set(new Description { Value = description });
        room.Set(new RoomGraph { Exits = new RoomExitValues() });
        room.Set(new RoomContents
        {
            Characters = [],
            Items = []
        });
        room.Set(new RoomNeighborhood
        {
            Distance1 = [],
            Distance2 = []
        });
        return room;
    }

    public static bool LinkRoom(World world, Entity sourceRoom, Entity targetRoom, DirectionKind direction)
    {
        ref var sourceRoomGraph = ref sourceRoom.Get<RoomGraph>();
        if (sourceRoomGraph.Exits[direction] is not null)
        {
            return false; // Exit already exists in this direction
        }

        sourceRoomGraph.Exits[direction] = new Exit { Direction = direction, TargetRoom = targetRoom }; // TODO: close + description
        return true;
    }
}
