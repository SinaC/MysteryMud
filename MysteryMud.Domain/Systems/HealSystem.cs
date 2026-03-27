using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Calculators;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Systems;

public static class HealSystem
{
    public static ApplyHealResult ApplyHeal(SystemContext ctx, Entity target, int healAmount, Entity source)
    {
        if (target.Has<Dead>())
            return ApplyHealResult.Dead; // can't heal something that's already dead

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return ApplyHealResult.CannotBeHealed; // can't heal something without health

        // if the target is already at full health, we can skip the healing process
        if (health.Current >= health.Max)
            return ApplyHealResult.FullHealth;

        // apply heal modifiers
        var modifiedHeal = HealCalculator.ModifyDamage(target, healAmount, source);

        return ApplyHeal(ctx, target, ref health, modifiedHeal, source);
    }

    private static ApplyHealResult ApplyHeal(SystemContext ctx, Entity target, ref Health health, int healAmount, Entity source)
    {
        // apply heal and cap at max health
        health.Current = Math.Min(health.Current + healAmount, health.Max);

        ctx.Log.LogInformation(LogEvents.Heal,"Applying heal from {sourceName} to {targetName} with amount {heal}. Current health: {health.Current}/{health.Max}", source.DebugName, target.DebugName, healAmount, health.Current, health.Max);

        ctx.Msg.ToRoom(source).Act("%G{0} heal %g{1} for %g{2}%g health.%x").With(source, target, healAmount);

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
