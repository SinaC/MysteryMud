using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat.Damage;
using MysteryMud.Domain.Combat.Heal;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Factories;

// TODO: handle damage/heal/... effect
public class EffectFactory
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intent;
    private readonly DamageResolver _damageResolver;
    private readonly HealResolver _healResolver;

    public EffectFactory(ILogger logger, IGameMessageService msg, IIntentWriterContainer intent, DamageResolver damageResolver, HealResolver healResolver)
    {
        _logger = logger;
        _msg = msg;
        _intent = intent;
        _damageResolver = damageResolver;
        _healResolver = healResolver;
    }

    public void RemoveEffect(GameState state, Entity effect)
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
        if (effectInstance.EffectRuntime != null)
        {
            if (effectInstance.EffectRuntime.Tag != EffectTagId.None)
            {
                int tagIndex = (int)effectInstance.EffectRuntime.Tag;
                var effectsByTag = characterEffects.EffectsByTag[tagIndex];
                if (effectsByTag != null)
                {
                    effectsByTag.Remove(effect);
                    if (effectsByTag.Count == 0)
                        characterEffects.ActiveTags &= ~(1UL << tagIndex); // remove tag from active tags when last effect on that tag is removed
                }
            }
        }

        // if effect has StatModifiers
        // flag the target's stats as dirty so they will be recalculated without this effect
        if (effect.Has<StatModifiers>() && !effectInstance.Target.Has<DirtyStats>())
            effectInstance.Target.Add<DirtyStats>();

        // if effect has ResourceModifiers
        // flag the target's resources as dirty so they will be recalculated without this effect
        if (effect.Has<ResourceModifiers<HealthModifier>>() && !effectInstance.Target.Has<DirtyHealth>())
            effectInstance.Target.Add<DirtyHealth>();
        if (effect.Has<ResourceModifiers<ManaModifier>>() && !effectInstance.Target.Has<DirtyMana>())
            effectInstance.Target.Add<DirtyMana>();
        if (effect.Has<ResourceModifiers<EnergyModifier>>() && !effectInstance.Target.Has<DirtyEnergy>())
            effectInstance.Target.Add<DirtyEnergy>();
        if (effect.Has<ResourceModifiers<RageModifier>>() && !effectInstance.Target.Has<DirtyRage>())
            effectInstance.Target.Add<DirtyRage>();

        // destroy effect
        state.World.Destroy(effect);
    }

    //private enum StackingResult
    //{
    //    Nop, // don't do anything
    //    NoExisting, // create new effect
    //    DifferentSource, // create new effect
    //    Refreshed, // don't do anything
    //    Stacked, // flag as Dirty
    //    Replaced, // create new effect
    //}
    public void ResolveEffect(GameState state, EffectRuntime effectRuntime, Entity source, Entity target)
    {
        // if duration, handle stacking rules + add expire intent and tick intent (if tick rate > 0)
        if (effectRuntime.DurationFunc != null)
        {
            ResolveDurationEffect(state, effectRuntime, source, target);
            return;
        }

        ResolveInstantEffect(state, effectRuntime, source, target);
    }

    private void ResolveDurationEffect(GameState state, EffectRuntime effectRuntime, Entity source, Entity target)
    {
        ref var targetEffects = ref target.Get<CharacterEffects>();
        var stackingResult = HandleStacking(state, effectRuntime, source, target, ref targetEffects, out var existingEffect, out var existingStackCount);

        if (stackingResult == StackingResult.Nop
            || stackingResult == StackingResult.Refreshed)
            return;

        if (stackingResult == StackingResult.Stacked)
        {
            // add dirty flag to character stats so we will recalculate them with the stack count
            if (!target.Has<DirtyStats>())
                target.Add<DirtyStats>();
            return;
        }

        // create effect
        var effect = state.World.Create(new EffectInstance
        {
            Source = source,
            Target = target,
            StackCount = 1,
            EffectRuntime = effectRuntime,
        });
        // add effect to target effect cache
        targetEffects.Effects.Add(effect);

        _logger.LogInformation(LogEvents.Factory, "Creating Effect from Template {effectTemplateName} Source {sourceName} Target {targetName}", effectRuntime.Name, source.DebugName, target.DebugName);

        // add tag if applicable
        if (effectRuntime.Tag != EffectTagId.None)
        {
            var tagIndex = (int)effectRuntime.Tag;
            // add EffectTag component to effect
            effect.Add(new EffectTag
            {
                Id = effectRuntime.Tag
            });
            // add effect to target's CharacterEffects
            if (targetEffects.EffectsByTag[tagIndex] == null)
                targetEffects.EffectsByTag[tagIndex] = [effect];
            else
                targetEffects.EffectsByTag[tagIndex]!.Add(effect);
            targetEffects.ActiveTags |= 1UL << tagIndex;

            _logger.LogInformation(LogEvents.Factory, " - add tag {tag}", effectRuntime.Tag);
        }

        // add TimedEffect component to effect
        var ctx = new EffectContext
        {
            Effect = effect, // non-instant effect
            Source = source,
            Target = target,

            IncomingDamage = 0,
            LastDamage = 0,

            StackCount = 1,

            State = state,
            Msg = _msg,
            DamageResolver = _damageResolver,
            HealResolver = _healResolver
        };

        var duration = effectRuntime.DurationFunc!.Invoke(ctx);
        var expirationTick = (int)(state.CurrentTick + duration);
        var nextTick = effectRuntime.TickOnApply
            ? state.CurrentTick
            : state.CurrentTick + effectRuntime.TickRate; // 0: means pure duration
        var tickRate = effectRuntime.TickRate; // 0: means pure duration
        effect.Add(new TimedEffect
        {
            StartTick = state.CurrentTick,
            ExpirationTick = expirationTick,
            NextTick = nextTick,
            TickRate = tickRate
        });

        // expire intent
        _logger.LogInformation(LogEvents.Factory, " - add duration {duration} ticks (expires at {expirationTick})", duration, expirationTick);
        ref var expireScheduleIntent = ref _intent.Schedule.Add();
        expireScheduleIntent.Effect = effect;
        expireScheduleIntent.Kind = ScheduledEventKind.Expire;
        expireScheduleIntent.ExecuteAt = expirationTick;

        // first tick intent (if periodic)
        if (tickRate > 0)
        {
            _logger.LogInformation(LogEvents.Factory, " - add tick rate {tickRate} (next tick {nextTick})", effectRuntime.TickRate, nextTick);
            ref var tickScheduleIntent = ref _intent.Schedule.Add();
            tickScheduleIntent.Effect = effect;
            tickScheduleIntent.Kind = ScheduledEventKind.Tick;
            tickScheduleIntent.ExecuteAt = nextTick;
        }

        // trigger onApply actions
        if (effectRuntime.OnApply.Length > 0)
        {
            foreach (var onApply in effectRuntime.OnApply)
            {
                if (CharacterHelpers.IsAlive(source, target))
                    onApply.Invoke(ctx);
            }
        }
    }

    private void ResolveInstantEffect(GameState state, EffectRuntime effectRuntime, Entity source, Entity target)
    {
        // trigger onApply actions
        if (effectRuntime.OnApply.Length > 0)
        {
            var ctx = new EffectContext
            {
                Effect = null, // instant effect
                Source = source,
                Target = target,

                IncomingDamage = 0,
                LastDamage = 0,

                StackCount = 1,

                State = state,
                Msg = _msg,
                DamageResolver = _damageResolver,
                HealResolver = _healResolver
            };

            foreach (var onApply in effectRuntime.OnApply)
            {
                if (CharacterHelpers.IsAlive(source, target))
                    onApply.Invoke(ctx);
            }
        }
    }

    // return true if a new effect has to be applied
    private enum StackingResult
    {
        Nop, // don't do anything
        NoExisting, // create new effect
        DifferentSource, // create new effect
        Refreshed, // don't do anything
        Stacked, // flag as Dirty
        Replaced, // create new effect
    }

    private StackingResult HandleStacking(GameState state, EffectRuntime effectRuntime, Entity source, Entity target, ref CharacterEffects targetEffects, out Entity? existingEffect, out int existingStackCount)
    {
        existingStackCount = 0;
        existingEffect = FindEffect(ref targetEffects, effectRuntime);
        if (existingEffect is null)
            return StackingResult.NoExisting;
        ref var instance = ref state.World.Get<EffectInstance>(existingEffect.Value);
        existingStackCount = instance.StackCount;
        if (source != instance.Source)
            return StackingResult.DifferentSource;

        // now we can check stacking rules
        var effect = existingEffect.Value;
        ref var timedEffect = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);

        switch (effectRuntime.Stacking)
        {
            case StackingRule.None:
                // if the stacking rule is None, do not apply the new effect and do not refresh the duration
                return StackingResult.Nop;
            case StackingRule.Refresh:
                if (isTimedEffect)
                {
                    var ctx = new EffectContext
                    {
                        Effect = effect,
                        Source = source,
                        Target = target,

                        IncomingDamage = 0,
                        LastDamage = 0,

                        StackCount = instance.StackCount,

                        State = state,
                        Msg = _msg,
                        DamageResolver = _damageResolver,
                        HealResolver = _healResolver
                    };

                    // update Duration
                    var durationValue = effectRuntime.DurationFunc?.Invoke(ctx) ?? 0;
                    var expirationTick = (int)(state.CurrentTick + durationValue);
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    _logger.LogInformation(LogEvents.Factory, "Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick}", effectRuntime.Name, source.DebugName, instance.Target.DebugName, durationValue, expirationTick);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref _intent.Schedule.Add();
                    expireScheduleIntent.Effect = effect;
                    expireScheduleIntent.Kind = ScheduledEventKind.Expire;
                    expireScheduleIntent.ExecuteAt = expirationTick;
                }
                return StackingResult.Refreshed;
            case StackingRule.Stack:
                bool stackCountModified = false;
                if (instance.StackCount < effectRuntime.MaxStacks)
                {
                    instance.StackCount++;
                    stackCountModified = true;
                }
                if (isTimedEffect)
                {
                    var ctx = new EffectContext
                    {
                        Effect = effect,
                        Source = source,
                        Target = target,

                        IncomingDamage = 0,
                        LastDamage = 0,

                        StackCount = instance.StackCount,

                        State = state,
                        Msg = _msg,
                        DamageResolver = _damageResolver,
                        HealResolver = _healResolver
                    };

                    // update Duration
                    var durationValue = effectRuntime.DurationFunc?.Invoke(ctx) ?? 0;
                    var expirationTick = (int)(state.CurrentTick + durationValue);
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    _logger.LogInformation(LogEvents.Factory, "Stacking/Refreshing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName} Duration {duration} Expiration {expirationTick} New Stack Count {newStackCount}", effectRuntime.Name, source.DebugName, instance.Target.DebugName, durationValue, expirationTick, instance.StackCount);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref _intent.Schedule.Add();
                    expireScheduleIntent.Effect = effect;
                    expireScheduleIntent.Kind = ScheduledEventKind.Expire;
                    expireScheduleIntent.ExecuteAt = expirationTick;
                }
                if (stackCountModified)
                    return StackingResult.Stacked;
                else
                    return StackingResult.Refreshed;
            case StackingRule.Replace:
                _logger.LogInformation(LogEvents.Factory, "Replacing Effect from Template {effectTemplateName} Source {sourceName} Target {targetName}", effectRuntime.Name, source.DebugName, instance.Target.DebugName);
                RemoveEffect(state, effect); // destroy current effect (no wear off message because it's a replacement)
                return StackingResult.Replaced;
        }

        return StackingResult.Nop;
    }

    public Entity? FindEffect(ref CharacterEffects characterEffects, EffectRuntime effectRuntime)
    {
        if (effectRuntime.Tag == EffectTagId.None)
        {
            foreach (var effect in characterEffects.Effects)
            {
                ref var effectInstance = ref effect.Get<EffectInstance>();
                if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                    return effect;
            }
            return null;
        }
        var tagIndex = (int)effectRuntime.Tag;
        ref var effectsByTag = ref characterEffects.EffectsByTag[tagIndex];
        if (effectsByTag == null)
            return null;
        foreach (var effectByTag in effectsByTag)
        {
            ref var effectInstance = ref effectByTag.Get<EffectInstance>();
            if (effectInstance.EffectRuntime.Name == effectRuntime.Name)
                return effectByTag;
        }
        return null;
    }
}
