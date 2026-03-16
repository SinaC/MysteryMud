using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Components.Effects;

namespace MysteryMud.ConsoleApp.Systems;

static class DotSystem
{
    static QueryDescription dotQuery =
    new QueryDescription().WithAll<DamageOverTime, Health>();

    public static void Run(World world, float dt, CommandBuffer cmd)
    {
        world.Query(dotQuery, (Entity e,
                               ref DamageOverTime dot,
                               ref Health hp) =>
        {
            dot.Timer += dt;
            dot.Duration -= dt;

            if (dot.Timer >= dot.TickInterval)
            {
                dot.Timer = 0;
                hp.Current -= dot.Damage;

                Console.WriteLine($"{e.Id} takes {dot.Damage} poison damage.");
            }

            if (dot.Duration <= 0)
            {
                cmd.Add(w => w.Remove<DamageOverTime>(e));
            }
        });
    }
}
