using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Factories;

public class RoomFactory
{
    public static Entity StartingRoomEntity;
    public static Entity RespawnRoomEntity;

    public static Entity CreateRoom(World world, int id, string name, string description)
    {
        return world.Create(
            new Room { Id = id },
            new Name { Value = name },
            new Description { Value = description },
            new RoomGraph { Exits = [] },
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

    public static bool LinkRoom(World world, Entity sourceRoom, Entity targetRoom, Direction direction)
    {
        var sourceRoomGraph = sourceRoom.Get<RoomGraph>();
        if (sourceRoomGraph.Exits.Any(x => x.Direction == direction))
        {
            return false; // Exit already exists in this direction
        }

        sourceRoomGraph.Exits.Add(new Exit { Direction = direction, TargetRoom = targetRoom }); // TODO: close + description
        return true;
    }
}
