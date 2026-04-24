using MysteryMud.Domain.Components.Rooms;
using TinyECS;

namespace MysteryMud.Domain.Factories;

public static class RoomGraphFactory
{
    public static void BuildNeighborhood(World world, EntityId room)
    {
        ref var graph = ref world.Get<RoomGraph>(room);

        var neighbors1 = new List<EntityId>();
        var neighbors2 = new List<EntityId>();

        foreach (var exit in graph.Exits)
        {
            if (exit is null)
                continue;

            neighbors1.Add(exit!.Value.TargetRoom);

            ref var nextGraph = ref world.Get<RoomGraph>(exit!.Value.TargetRoom);

            foreach (var nextExit in nextGraph.Exits)
                neighbors2.Add(nextExit!.Value.TargetRoom);
        }

        world.Set(room, new RoomNeighborhood
        {
            Distance1 = neighbors1,
            Distance2 = neighbors2
        });
    }
}
