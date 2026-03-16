using Arch.Core;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class DotSystem
{
    public static void Update(World world)
    { 
        var query = new QueryDescription()
            .WithAll<DamageOverTime, Effect>();
        world.Query(in query, (Entity entity,
            ref DamageOverTime dot, ref Effect effect) =>
        {
            Console.WriteLine($"Processing DoT for Effect {entity.DisplayName} on Target {effect.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate}");

            if (TimeSystem.CurrentTick < dot.NextTick)
                return;

            Console.WriteLine($"Applying DoT damage for Effect {entity.DisplayName} on Target {effect.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate}");

            dot.NextTick += dot.TickRate;

            DamageSystem.ApplyDamage(world, effect.Target, dot.Damage, effect.Source);
        });
    }
}
