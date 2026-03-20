using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Calculators;

public static class HealCalculator
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

        MessageSystem.Send(source, $"%GYou heal %g{target.DisplayName} for %g{healAmount}%g health.%x");
        MessageSystem.Send(target, $"%G{source.DisplayName} heals you for %g{healAmount}%g health.%x");

        AggroCalculator.AddAggro(target, source, healAmount / 2); // healing generates some aggro, but less than damage

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
