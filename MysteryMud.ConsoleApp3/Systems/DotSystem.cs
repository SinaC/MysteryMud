using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class DotSystem
{
    public static void Update(World world)
    { 
        var query = new QueryDescription()
            .WithAll<EffectInstance, DamageOverTime>();
        world.Query(in query, (Entity entity,
            ref EffectInstance effectInstance, ref DamageOverTime dot) =>
        {
            //Console.WriteLine($"Processing DoT for Effect {entity.DisplayName} on Target {effectInstance.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate}");

            if (effectInstance.Target.Has<DeadTag>())
            {
                Console.WriteLine($"Processing DoT for Effect {entity.DisplayName} on DEAD Target");
                return;
            }

            if (TimeSystem.CurrentTick < dot.NextTick)
                return;

            Console.WriteLine($"Applying DoT damage for Effect {entity.DisplayName} on Target {effectInstance.Target.DisplayName} with damage {dot.Damage} and tick rate {dot.TickRate}");

            dot.NextTick += dot.TickRate;

            var damage = dot.Damage * effectInstance.StackCount;
            DamageSystem.ApplyDamage(world, effectInstance.Target, damage, effectInstance.Source);
        });
    }
}
