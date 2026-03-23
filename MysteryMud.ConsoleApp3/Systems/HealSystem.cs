using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Simulation.Calculators;

namespace MysteryMud.ConsoleApp3.Systems;

public static class HealSystem
{
    public static ApplyHealResult ApplyHeal(Entity target, int healAmount, Entity source)
    {
        if (target.Has<Dead>())
            return ApplyHealResult.Dead; // can't heal something that's already dead

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return ApplyHealResult.CannotBeHealed; // can't heal something without health

        // if the target is already at full health, we can skip the healing process
        if (health.Current >= health.Max)
            return ApplyHealResult.FullHealth;

        // TODO: apply any healing modifiers here (buffs, debuffs, etc.)

        health.Current = Math.Min(health.Current + healAmount, health.Max);

        Logger.Logger.Heal.Apply(source, target, healAmount, ref health);

        MessageBus.Publish(source, $"%GYou heal %g{target.DisplayName} for %g{healAmount}%g health.%x");
        MessageBus.Publish(target, $"%G{source.DisplayName} heals you for %g{healAmount}%g health.%x");

        var aggro = AggroCalculator.CalculateHealAggro(target, source, healAmount);
        AggroSystem.AddAggro(target, source, aggro);

        return ApplyHealResult.Healed;
    }


    public enum ApplyHealResult
    {
        Dead,
        CannotBeHealed,
        FullHealth,
        Healed,
    }
}
