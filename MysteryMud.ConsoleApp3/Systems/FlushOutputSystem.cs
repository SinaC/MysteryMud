using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Core;

namespace MysteryMud.ConsoleApp3.Systems;

static class FlushOutputSystem
{
    public static void Process(GameState state)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        state.World.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            Services.Services.Messages.Flush(player);
        });
    }
}
