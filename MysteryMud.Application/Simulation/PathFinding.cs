using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Application.Simulation;

public static class Pathfinding
{
    public static List<Entity> FindPath(Entity start, Entity goal)
    {
        Queue<Entity> open = new();
        Dictionary<Entity, Entity> cameFrom = new();

        open.Enqueue(start);

        while (open.Count > 0)
        {
            var room = open.Dequeue();

            if (room == goal)
                break;

            foreach (var exit in room.Get<RoomGraph>().Exits)
            {
                if (!cameFrom.ContainsKey(exit.TargetRoom))
                {
                    open.Enqueue(exit.TargetRoom);
                    cameFrom[exit.TargetRoom] = room;
                }
            }
        }

        return Reconstruct(cameFrom, goal);
    }

    private static List<Entity> Reconstruct(Dictionary<Entity, Entity> cameFrom, Entity current)
    {
        List<Entity> path = new();
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Add(current);
            current = prev;
        }
        path.Reverse();
        return path;
    }
}
