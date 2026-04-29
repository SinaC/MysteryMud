using DefaultEcs;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Factories;

public static class RoomGraphFactory
{
    public static void BuildNeighborhood(Entity room)
    {
        ref var graph = ref room.Get<RoomGraph>();

        var neighbors1 = new List<Entity>();
        var neighbors2 = new List<Entity>();

        foreach (var exit in graph.Exits)
        {
            if (exit is null)
                continue;

            neighbors1.Add(exit!.Value.TargetRoom);

            ref var nextGraph = ref exit!.Value.TargetRoom.Get<RoomGraph>();

            foreach (var nextExit in nextGraph.Exits)
                neighbors2.Add(nextExit!.Value.TargetRoom);
        }

        room.Set(new RoomNeighborhood
        {
            Distance1 = neighbors1,
            Distance2 = neighbors2
        });
    }
}
