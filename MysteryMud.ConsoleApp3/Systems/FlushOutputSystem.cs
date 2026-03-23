using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Infrastructure.Services;

namespace MysteryMud.ConsoleApp3.Systems;

static class FlushOutputSystem
{
    public static void Process(IMessageService messageService, GameState state)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        state.World.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            messageService.Flush(player);
        });
    }
}
