using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Numerics;

namespace MysteryMud.Application.Commands;

public class ScoreCommand : ICommand
{
    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {

        // TODO: ref ?
        var (name, baseStats, effectiveStats, characterEffects) = actor.Get<Name, BaseStats, EffectiveStats, CharacterEffects>();
        executionContext.Msg.To(actor).Send($"Name: {name.Value}");
        DisplayLevelAndExperience(executionContext, actor);
        DisplayHealth(executionContext, actor);
        DisplayResource<Mana, ManaRegen, UsesMana>(executionContext, actor, ResourceKind.Mana, x => (x.Current, x.Max));
        DisplayResource<Energy, EnergyRegen, UsesEnergy>(executionContext, actor, ResourceKind.Energy, x => (x.Current, x.Max));
        DisplayResource<Rage, RageDecay, UsesRage>(executionContext, actor, ResourceKind.Rage, x => (x.Current, x.Max));
        foreach (var stat in Enum.GetValues<StatKind>())
        {
            executionContext.Msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref actor.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            executionContext.Msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");

        executionContext.Msg.To(actor).Send($"Active tags: {characterEffects.ActiveTags}");
        executionContext.Msg.To(actor).Send($"Effects:");
        foreach (var effect in characterEffects.Effects)
        {
            if (!effect.IsAlive() || effect.Has<ExpiredTag>())
                continue;
            ref var effectInstance = ref effect.Get<EffectInstance>();
            if (effectInstance.EffectRuntime != null)
            {
                // TODO: how could we display hot/dot
                var effectName = effectInstance.EffectRuntime.Name;
                var stackCount = effectInstance.StackCount;
                var sourceName = effectInstance.Source.DisplayName;

                ref var timedEffect = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
                if (isTimedEffect)
                {
                    var remainingTicks = timedEffect.ExpirationTick - state.CurrentTick;
                    if (timedEffect.TickRate > 0)
                        executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks} Tick rate: {timedEffect.TickRate}");
                    else
                        executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
                }
                else
                    executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");

                ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                if (hasStatModifiers)
                {
                    foreach (var modifier in statModifiers.Values)
                        executionContext.Msg.To(actor).Send($"  - {modifier.Modifier} {modifier.Value} {modifier.Stat}");
                }

                DisplayResourceModifier<HealthModifier>(executionContext, actor, effect, "Health", x => x.Modifier, x => x.Value);
                DisplayResourceModifier<ManaModifier>(executionContext, actor, effect, "Mana", x => x.Modifier, x => x.Value);
                DisplayResourceModifier<EnergyModifier>(executionContext, actor, effect, "Energy", x => x.Modifier, x => x.Value);
                DisplayResourceModifier<RageModifier>(executionContext, actor, effect, "Rage", x => x.Modifier, x => x.Value);
            }
        }
    }

    private void DisplayLevelAndExperience(CommandExecutionContext ctx, Entity actor)
    {
        ref var level = ref actor.TryGetRef<Level>(out var hasLevel);
        if (!hasLevel)
            return;
        ctx.Msg.To(actor).Send($"Level: {level.Value}");
        ref var progression = ref actor.TryGetRef<Progression>(out var hasProgression);
        if (hasProgression)
            ctx.Msg.To(actor).Send($"Experience: {progression.Experience} NextLevel: {progression.ExperienceToNextLevel - progression.Experience} XP");
    }

    private void DisplayHealth(CommandExecutionContext ctx, Entity actor)
    {
        var health = actor.Get<Health>();
        ctx.Msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
    }

    private void DisplayResource<TResource, TRegen, TUses>(CommandExecutionContext ctx, Entity actor, ResourceKind kind, Func<TResource, (int current, int max)> getCurrentMaxFunc)
        where TResource : struct
        where TRegen : struct
        where TUses : struct
    {
        var uses = actor.Has<TUses>();
        if (!uses)
            return;
        ref var resource = ref actor.TryGetRef<TResource>(out var hasResource);
        if (hasResource)
        {
            var (current, max) = getCurrentMaxFunc(resource);
            ref var resourceRegen = ref actor.TryGetRef<TRegen>(out var hasRegen);
            ctx.Msg.To(actor).Send($"{kind}: {current}/{max} CanUse: {uses}");
        }
    }

    private void DisplayResourceModifier<TResourceModifier>(CommandExecutionContext ctx, Entity actor, Entity effect, string resourceName, Func<TResourceModifier, ModifierKind> getModifierFunc, Func<TResourceModifier, decimal> getValueFunc)
        where TResourceModifier : struct
    {
        ref var resourceModifiers = ref effect.TryGetRef<ResourceModifiers<TResourceModifier>>(out var hasResourceModifiers);
        if (hasResourceModifiers)
        {
            foreach (var modifier in resourceModifiers.Values)
                ctx.Msg.To(actor).Send($"  - {getModifierFunc(modifier)} {getValueFunc(modifier)} {resourceName}");
        }
    }
}
