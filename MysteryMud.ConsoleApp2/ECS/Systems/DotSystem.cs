using Arch.Core;
using MysteryMud.ConsoleApp2.ECS.Components.Effects;

namespace MysteryMud.ConsoleApp2.ECS.Systems;

public static class DotSystem
{
    public static void Update(World world)
    {
        var query = new QueryDescription()
            .WithAll<DamageOverTime, EffectTarget>();
        world.Query(in query, (ref DamageOverTime dot, ref EffectTarget target) =>
        {
            if (GameTime.Tick < dot.NextTick)
                return;

            dot.NextTick += dot.TickRate;

            ApplyDamage(target.Target, dot.Damage);
        });
    }

    private static void ApplyDamage(Entity target, int damage)
    {
        // TODO
    }
}
