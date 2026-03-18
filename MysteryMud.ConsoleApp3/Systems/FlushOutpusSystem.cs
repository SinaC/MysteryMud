using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Networking;

namespace MysteryMud.ConsoleApp3.Systems;

static class FlushOutpusSystem
{
    public static void FlushOutputs(World world)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        world.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            conn.Value.Flush();  // signals SendLoop to send batched output
        });
    }
}
