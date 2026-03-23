using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Data.Definitions;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Effects;

namespace MysteryMud.ConsoleApp3.Domain.Factories;

public static class EffectFactory
{
    public static void ApplyEffect(SystemContext systemContext, GameState gameState, EffectTemplate effectTemplate, Entity source, Entity target)
    {
        ref var targetEffects = ref target.Get<CharacterEffects>();

        // if effect has a tag, check for an existing effect with the same tag and apply stacking rules if found
        int tagIndex = (int)effectTemplate.Tag;
        if (effectTemplate.Tag != EffectTagId.None && targetEffects.EffectsByTag[tagIndex] is Entity existing)
        {
            var handled = HandleStacking(systemContext, gameState, effectTemplate, existing, source);
            if (handled)
                return;
            // not handled: apply new effect
        }

        // no tag or no existing effect with the same tag, create a new one
        var effect = gameState.World.Create(new EffectInstance
        {
            Source = source,
            Target = target,
            StackCount = 1,
            Template = effectTemplate,
        });

        // add effect to target effect cache
        targetEffects.Effects.Add(effect);

        Logger.Logger.Factory.CreateEffect(source, target, effectTemplate);

        // add tag if applicable
        if (effectTemplate.Tag != EffectTagId.None)
        {
            // add EffectTag component to effect
            effect.Add(new EffectTag
            {
                Id = effectTemplate.Tag
            });
            targetEffects.EffectsByTag[tagIndex] = effect;
            targetEffects.ActiveTags |= 1UL << tagIndex;

            Logger.Logger.Factory.AddTagToEffect(effectTemplate.Tag);
        }

        // duration ?
        var duration = effectTemplate.DurationFunc?.Invoke(gameState.World, source, target);
        if (duration is not null)
        {
            // add Duration component to effect
            var expirationTick = gameState.CurrentTick + duration.Value;
            effect.Add(new Duration
            {
                StartTick = gameState.CurrentTick,
                ExpirationTick = expirationTick
            });

            // queue expiration event
            Logger.Logger.Factory.AddDurationToEffect(duration.Value, expirationTick);
            systemContext.Scheduler.Publish(effect, ScheduledEventType.EffectExpired, expirationTick);
        }

        // stat modifiers ?
        if (effectTemplate.StatModifiers is not null && effectTemplate.StatModifiers.Length > 0)
        {
            // add StatModifiers component to effect
            var modifiers = new List<StatModifier>();
            foreach (var modifierDefinition in effectTemplate.StatModifiers)
            {
                var modifier = new StatModifier
                {
                    Stat = modifierDefinition.Stat,
                    Type = modifierDefinition.Type,
                    Value = modifierDefinition.Value,
                };
                modifiers.Add(modifier);
            }
            effect.Add(new StatModifiers
            {
                Values = modifiers
            });
            // add dirty flag to character stats so we will recalculate them with the new modifiers
            if (!target.Has<DirtyStats>())
                target.Add<DirtyStats>();

            Logger.Logger.Factory.AddStatModifiersToEffect(modifiers);
        }

        // TODO: remove
        ref var casterStats = ref source.Get<EffectiveStats>();

        // dot ?
        if (effectTemplate.Dot is not null)
        {
            // add DamageOverTime component to effect
            var nextTick = gameState.CurrentTick + effectTemplate.Dot.Value.TickRate;
            var damage = effectTemplate.Dot.Value.DamageFunc.Invoke(gameState.World, source, target);
            effect.Add(new DamageOverTime
            {
                Damage = damage,
                DamageType = effectTemplate.Dot.Value.DamageType,
                TickRate = effectTemplate.Dot.Value.TickRate,
                NextTick = nextTick
            });

            // queue first tick event
            Logger.Logger.Factory.AddDotToEffect(damage, effectTemplate.Dot.Value.DamageType, effectTemplate.Dot.Value.TickRate, nextTick);
            systemContext.Scheduler.Publish(effect, ScheduledEventType.DotTick, nextTick);
        }

        // hot ?
        if (effectTemplate.Hot is not null)
        {
            // add HealOverTime component to effect
            var nextTick = gameState.CurrentTick + effectTemplate.Hot.Value.TickRate;
            var heal = effectTemplate.Hot.Value.HealFunc.Invoke(gameState.World, source, target);
            effect.Add(new HealOverTime
            {
                Heal = heal,
                TickRate = effectTemplate.Hot.Value.TickRate,
                NextTick = nextTick
            });
            // queue first tick event
            Logger.Logger.Factory.AddHotToEffect(heal, effectTemplate.Hot.Value.TickRate, nextTick);
            systemContext.Scheduler.Publish(effect, ScheduledEventType.HotTick, nextTick);
        }

        // apply message
        if (effectTemplate.ApplyMessage != null)
            systemContext.MessageBus.Publish(source, effectTemplate.ApplyMessage);
    }

    private static bool HandleStacking(SystemContext systemContext, GameState gameState, EffectTemplate effectTemplate, Entity effect, Entity source)
    {
        ref var effectInstance = ref gameState.World.Get<EffectInstance>(effect);

        if (source != effectInstance.Source)
        {
            // if the source is different, we will treat it as a new effect and not apply stacking rules
            return false; // not handled, allow new effect to be applied
        }

        ref var duration = ref effect.TryGetRef<Duration>(out var hasDuration);
        int idx = (int)effectTemplate.Tag;

        switch (effectTemplate.Stacking)
        {
            case StackingRule.None:
                // if the stacking rule is None, do not apply the new effect and do not refresh the duration
                return true; // handled
            case StackingRule.Refresh:
                if (hasDuration)
                {
                    // update Duration
                    var durationValue = effectTemplate.DurationFunc?.Invoke(gameState.World, effectInstance.Source, effectInstance.Target) ?? 0;
                    var expirationTick = gameState.CurrentTick + durationValue;
                    duration.LastRefreshTick = gameState.CurrentTick;
                    duration.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    Logger.Logger.Factory.RefreshEffect(source, effectInstance.Target, effectTemplate, durationValue, expirationTick);
                    systemContext.Scheduler.Publish(effect, ScheduledEventType.EffectExpired, expirationTick);
                }
                return true; // handled
            case StackingRule.Stack:
                if (effectInstance.StackCount < effectTemplate.MaxStacks)
                    effectInstance.StackCount++;
                if (hasDuration)
                {
                    // update Duration
                    var durationValue = effectTemplate.DurationFunc?.Invoke(gameState.World, effectInstance.Source, effectInstance.Target) ?? 0;
                    var expirationTick = gameState.CurrentTick + durationValue;
                    duration.LastRefreshTick = gameState.CurrentTick;
                    duration.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    Logger.Logger.Factory.StackEffect(source, effectInstance.Target, effectTemplate, durationValue, expirationTick, effectInstance.StackCount);
                    systemContext.Scheduler.Publish(effect, ScheduledEventType.EffectExpired, expirationTick);
                }
                return true; // handled
        }

        return true; // default to handled to prevent new effect application if stacking rule is not recognized
    }
}
