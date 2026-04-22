using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Services;

public static class Pathfinding
{
    public static List<Entity> FindPath(Entity start, Entity goal)
    {
        Queue<Entity> open = new();
        Dictionary<Entity, Entity> cameFrom = [];

        open.Enqueue(start);

        while (open.Count > 0)
        {
            var room = open.Dequeue();

            if (room == goal)
                break;

            ref var roomGraph = ref room.Get<RoomGraph>();
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

    private static List<Entity> Reconstruct(Dictionary<Entity, Entity> cameFrom, Entity current)
    {
        List<Entity> path = [];
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Add(current);
            current = prev;
        }
        path.Reverse();
        return path;
    }
}
