using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Characters.Players;

namespace MysteryMud.ConsoleApp3.Systems;

public static class BroadcastSystem
{
    public static void Broadcast(World world, string message)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        world.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            // Append message to player's output buffer
            conn.Value.Write(message + "\r\n");
        });
    }
}
