using MysteryMud.Domain.Components.Rooms;
using TinyECS;

namespace MysteryMud.Domain.Services;

public static class Pathfinding
{
    public static List<EntityId> FindPath(World world, EntityId start, EntityId goal)
    {
        Queue<EntityId> open = new();
        Dictionary<EntityId, EntityId> cameFrom = [];

        open.Enqueue(start);

        while (open.Count > 0)
        {
            var room = open.Dequeue();

            if (room == goal)
                break;

            ref var roomGraph = ref world.Get<RoomGraph>(room);
            foreach (var exit in roomGraph.Exits)
            {
                if (exit is null)
                    continue;
                if (!cameFrom.ContainsKey(exit!.Value .TargetRoom))
                {
                    open.Enqueue(exit!.Value.TargetRoom);
                    cameFrom[exit!.Value.TargetRoom] = room;
                }
            }
        }

        return Reconstruct(cameFrom, goal);
    }

    private static List<EntityId> Reconstruct(Dictionary<EntityId, EntityId> cameFrom, EntityId current)
    {
        List<EntityId> path = [];
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Add(current);
            current = prev;
        }
        path.Reverse();
        return path;
    }
}
