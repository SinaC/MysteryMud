using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Logging;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class EffectFactory
{
    public static void RemoveEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive()) // don't use helpers, effect with ExpiredTag should be removable
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive())
            return;

        // remove the effect from the target's CharacterEffects
        ref var characterEffects = ref effectInstance.Target.Get<CharacterEffects>();
        characterEffects.Effects.Remove(effect);
        // remove tag if applicable
        if (effectInstance.Definition.Tag != EffectTagId.None)
        {
            int tagIndex = (int)effectInstance.Definition.Tag;
            var effectsByTag = characterEffects.EffectsByTag[tagIndex];
            if (effectsByTag != null)
            {
                effectsByTag.Remove(effect);
                if (effectsByTag.Count == 0)
                    characterEffects.ActiveTags &= ~(1UL << tagIndex); // remove tag from active tags when last effect on that tag is removed
            }
        }

        // flag the target's stats as dirty so they will be recalculated without this effect
        ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
        if (hasStatModifiers && !effectInstance.Target.Has<DirtyStats>())
            effectInstance.Target.Add<DirtyStats>();

        state.World.Destroy(effect);
    }

    public static void ApplyEffect(SystemContext ctx, GameState state, EffectDefinition effectDefinition, Entity source, Entity target)
    {
        ref var targetEffects = ref target.Get<CharacterEffects>();

        // if effect has a tag, check for an existing effect with the same tag and apply stacking rules if found
        var existing = FindEffect(ref targetEffects, effectDefinition);
        if (existing is not null)
        {
            var handled = HandleStacking(ctx, state, effectDefinition, existing.Value, source);
            if (handled)
                return;
            // not handled: apply new effect
        }

        // no tag or no existing effect with the same tag, create a new one
        var effect = state.World.Create(new EffectInstance
        {
            Source = source,
            Target = target,
            StackCount = 1,
            Definition = effectDefinition,
        });

        // add effect to target effect cache
        targetEffects.Effects.Add(effect);

        ctx.Log.LogInformation(LogEvents.Factory, "Creating Effect from Template {effectTemplateName} Source {sourceName} Target {targetName}", effectDefinition.Id, source.DebugName, target.DebugName);

        // add tag if applicable
        if (effectDefinition.Tag != EffectTagId.None)
        {
            var tagIndex = (int)effectDefinition.Tag;
            // add EffectTag component to effect
            effect.Add(new EffectTag
            {
                Id = effectDefinition.Tag
            });
            // add effect to target's CharacterEffects
            if (targetEffects.EffectsByTag[tagIndex] == null)
                targetEffects.EffectsByTag[tagIndex] = [effect];
            else
                targetEffects.EffectsByTag[tagIndex]!.Add(effect);
            targetEffects.ActiveTags |= 1UL << tagIndex;

            ctx.Log.LogInformation(LogEvents.Factory, " - add tag {tag}", effectDefinition.Tag);
        }

        // timed effect ?
        var duration = effectDefinition.DurationFunc?.Invoke(state.World, source, target);
        if (duration is not null)
        {
            // add TimedEffect component to effect
            var expirationTick = state.CurrentTick + duration.Value;
            var nextTick = effectDefinition.TickOnApply
                ? state.CurrentTick
                : state.CurrentTick + effectDefinition.TickRate; // 0: means pure duration
            var tickRate = effectDefinition.TickRate; // 0: means pure duration
            effect.Add(new TimedEffect
            {
                StartTick = state.CurrentTick,
                ExpirationTick = expirationTick,
                NextTick = nextTick,
                TickRate = tickRate
            });

            // expiration intent
            ctx.Log.LogInformation(LogEvents.Factory, " - add duration {duration} ticks (expires at {expirationTick})", duration, expirationTick);
            ref var expireScheduleIntent = ref ctx.Intent.Schedule.Add();
            expireScheduleIntent.Effect = effect;
            expireScheduleIntent.Kind = ScheduledEventKind.Expire;
            expireScheduleIntent.ExecuteAt = expirationTick;

            // first tick intent (if periodic)
            if (tickRate > 0)
            {
                ctx.Log.LogInformation(LogEvents.Factory, " - add tick rate {tickRate} (next tick {nextTick})", effectDefinition.TickRate, nextTick);
                ref var tickScheduleIntent = ref ctx.Intent.Schedule.Add();
                tickScheduleIntent.Effect = effect;
                tickScheduleIntent.Kind |= ScheduledEventKind.Tick;
                tickScheduleIntent.ExecuteAt = nextTick;
            }
        }

        // stat modifiers ?
        if (effectDefinition.StatModifiers is not null && effectDefinition.StatModifiers.Length > 0)
        {
            // add StatModifiers component to effect
            var modifiers = new List<StatModifier>();
            foreach (var modifierDefinition in effectDefinition.StatModifiers)
            {
                var modifier = new StatModifier
                {
                    Stat = modifierDefinition.Stat,
                    Kind = modifierDefinition.Kind,
                    Value = modifierDefinition.Value,
                };
                modifiers.Add(modifier);

                ctx.Log.LogInformation(LogEvents.Factory, " - add stat modifier {stat} {value} ({type})", modifierDefinition.Stat, modifierDefinition.Value, modifierDefinition.Kind);
            }
            effect.Add(new StatModifiers
            {
                Values = modifiers
            });
            // add dirty flag to character stats so we will recalculate them with the new modifiers
            if (!target.Has<DirtyStats>())
                target.Add<DirtyStats>();

        }

        // TODO: remove
        ref var casterStats = ref source.Get<EffectiveStats>();

        // dot ?
        if (effectDefinition.Dot is not null)
        {
            // add DamageOverTime component to effect
            var damage = effectDefinition.Dot.Value.DamageFunc.Invoke(state.World, source, target);
            effect.Add(new DamageEffect
            {
                Damage = damage,
                DamageKind = effectDefinition.Dot.Value.DamageKind,
            });
        }

        // hot ?
        if (effectDefinition.Hot is not null)
        {
            // add HealOverTime component to effect
            var heal = effectDefinition.Hot.Value.HealFunc.Invoke(state.World, source, target);
            effect.Add(new HealEffect
            {
                Heal = heal,
            });
        }

        // apply message
        if (effectDefinition.ApplyMessage != null)
            ctx.Msg.To(source).Send(effectDefinition.ApplyMessage);
    }

    // return true if a new effect has to be applied
    private static bool HandleStacking(SystemContext ctx, GameState state, EffectDefinition effectDefinition, Entity effect, Entity source)
    {
        ref var instance = ref state.World.Get<EffectInstance>(effect);

        if (source != instance.Source)
        {
            // if the source is different, we will treat it as a new effect and not apply stacking rules
            return false; // not handled, allow new effect to be applied
        }

        ref var timedEffect = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
        int idx = (int)effectDefinition.Tag;

        switch (effectDefinition.Stacking)
        {
            case StackingRule.None:
                // if the stacking rule is None, do not apply the new effect and do not refresh the duration
                return true; // handled -> no new effect, existing modified
            case StackingRule.Refresh:
                if (isTimedEffect)
                {
                    // update Duration
                    var durationValue = effectDefinition.DurationFunc?.Invoke(state.World, instance.Source, instance.Target) ?? 0;
                    var expirationTick = state.CurrentTick + durationValue;
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    ctx.Log.LogInformation(LogEvents.Factory, "Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick}", effectDefinition.Id, source.DebugName, instance.Target.DebugName, timedEffect, expirationTick);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref ctx.Intent.Schedule.Add();
                    expireScheduleIntent.Effect = effect;
                    expireScheduleIntent.Kind = ScheduledEventKind.Tick;
                    expireScheduleIntent.ExecuteAt = expirationTick;
                }
                return true; // handled -> no new effect, existing modified
            case StackingRule.Stack:
                if (instance.StackCount < effectDefinition.MaxStacks)
                    instance.StackCount++;
                if (isTimedEffect)
                {
                    // update Duration
                    var durationValue = effectDefinition.DurationFunc?.Invoke(state.World, instance.Source, instance.Target) ?? 0;
                    var expirationTick = state.CurrentTick + durationValue;
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    ctx.Log.LogInformation(LogEvents.Factory, "Stacking/Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick} New Stack Count {newStackCount}", effectDefinition.Id, source.DebugName, instance.Target.DebugName, timedEffect, expirationTick, instance.StackCount);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref ctx.Intent.Schedule.Add();
                    expireScheduleIntent.Effect = effect;
                    expireScheduleIntent.Kind = ScheduledEventKind.Tick;
                    expireScheduleIntent.ExecuteAt = expirationTick;
                }
                return true; // handled -> no new effect, existing modified
            case StackingRule.Replace:
                ctx.Log.LogInformation(LogEvents.Factory, "Replacing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration}", effectDefinition.Id, source.DebugName, instance.Target.DebugName, timedEffect);
                RemoveEffect(state, effect); // destroy current effect (no wear off message because it's a replacement)
                return false; // no handled -> new effect will be added
        }

        return true; // default to handled to prevent new effect application if stacking rule is not recognized
    }

    public static Entity? FindEffect(ref CharacterEffects characterEffects, EffectDefinition effectDefinition)
    {
        if (effectDefinition.Tag == EffectTagId.None)
            return null;
        var tagIndex = (int)effectDefinition.Tag;
        ref var effectsByTag = ref characterEffects.EffectsByTag[tagIndex];
        if (effectsByTag == null)
            return null;
        foreach(var effectByTag in effectsByTag)
        {
            ref var effectInstance = ref effectByTag.Get<EffectInstance>();
            if (effectInstance.Definition.Id == effectDefinition.Id)
                return effectByTag;
        }
        return null;
    }
}
