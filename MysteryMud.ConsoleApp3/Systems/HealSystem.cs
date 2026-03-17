using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Systems;

static class HealSystem
{
    public static void ApplyHeal(World word, Entity target, int healAmount, Entity source)
    {
        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (hasHealth)
        {
            if (health.Current < health.Max)
            {
                health.Current = Math.Min(health.Current + healAmount, health.Max);
                Console.WriteLine($"{target.DisplayName} healed for {healAmount} points. Current health: {health.Current}/{health.Max}");
                MessageSystem.Send(source, $"%GYou heal %g{target.DisplayName} for %g{healAmount}%g health.%x");
                MessageSystem.Send(target, $"%G{source.DisplayName} heals you for %g{healAmount}%g health.%x");
            }
        }
        else
        {
            Console.WriteLine($"{target.DisplayName} cannot be healed because it does not have a HealthComponent.");
        }
    }
}
