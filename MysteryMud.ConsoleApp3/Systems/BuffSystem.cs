using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Buff;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;

namespace MysteryMud.ConsoleApp3.Systems;

public static class BuffSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
                .WithAll<Duration, BuffTarget>()
                .WithNone<DeadTag, DestroyedTag>();
        world.Query(query, (Entity buff, ref Duration duration, ref BuffTarget target) =>
        {
            duration.RemainingTicks--;

            if (duration.RemainingTicks <= 0)
            {
                if (!target.Target.Has<DirtyStats>())
                    target.Target.Add<DirtyStats>();
                world.Destroy(buff);
            }
        });
    }
}
