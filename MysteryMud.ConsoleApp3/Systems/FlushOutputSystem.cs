using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Characters.Players;

namespace MysteryMud.ConsoleApp3.Systems;

static class FlushOutputSystem
{
    public static void FlushOutputs(World world)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        world.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            Services.Services.Messages.Flush(player);
        });
    }
}
