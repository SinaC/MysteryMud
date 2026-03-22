using Arch.Core;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters.Players;

namespace MysteryMud.ConsoleApp3.Systems;

public static class RoomBroadcastSystem
{
    public static void Broadcast(World world, Entity room, string message)
    {
        var query = new QueryDescription()
                .WithAll<Position, Connection>();
        world.Query(query, (Entity player,
                     ref Position pos,
                     ref Connection conn) =>
        {
            if (pos.Room == room)
            {
                // Append message to player's output buffer
                conn.Value.Write(message + "\r\n");
            }
        });
    }
}
