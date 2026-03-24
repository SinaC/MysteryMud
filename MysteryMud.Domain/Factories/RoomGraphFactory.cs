using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Factories;

public static class RoomGraphFactory
{
    public static void BuildNeighborhood(Entity room)
    {
        var graph = room.Get<RoomGraph>();

        var neighbors1 = new List<Entity>();
        var neighbors2 = new List<Entity>();

        foreach (var exit in graph.Exits)
        {
            neighbors1.Add(exit.TargetRoom);

            var nextGraph = exit.TargetRoom.Get<RoomGraph>();

            foreach (var nextExit in nextGraph.Exits)
                neighbors2.Add(nextExit.TargetRoom);
        }

        room.Set(new RoomNeighborhood
        {
            Distance1 = neighbors1,
            Distance2 = neighbors2
        });
    }
}
