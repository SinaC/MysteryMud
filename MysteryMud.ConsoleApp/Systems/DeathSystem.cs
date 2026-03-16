using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Events;

namespace MysteryMud.ConsoleApp.Systems;

static class DeathSystem
{
    public static void Run(World world, EventBus events)
    {
        var query = new QueryDescription()
            .WithAll<DeadTag>();
        world.Query(query, (Entity e) =>
        {
            events.Emit(new DeathEvent(e));
            world.Destroy(e);
        });
    }
}
