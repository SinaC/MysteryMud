using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Time;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Action.Effect;

public partial class EffectApplicationManager : IEffectApplicationManager
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentWriterContainer _intent;
    private readonly IEffectExecutor _effectExecutor;
    private readonly IEffectLifecycleManager _effectLifecycleManager;

    public EffectApplicationManager(World world, ILogger logger, IGameMessageService msg, IDirtyTracker dirtyTracker, IIntentWriterContainer intent, IEffectExecutor effectExecutor, IEffectLifecycleManager effectLifecycleManager)
    {
        _world = world;
        _logger = logger;
        _msg = msg;
        _dirtyTracker = dirtyTracker;
        _intent = intent;
        _effectExecutor = effectExecutor;
        _effectLifecycleManager = effectLifecycleManager;
    }

    public void CreateEffect(GameState state, EffectRuntime effectRuntime, ref EffectData effectData)
    {
        var target = effectData.Target;
        var actualKind = target.Has<CharacterEffects>() ? EffectTargetKind.Character : EffectTargetKind.Item;

        if ((effectRuntime.SupportedTargets & actualKind) == 0)
        {
            _logger.LogWarning(LogEvents.Factory,
                "Effect '{name}' does not support target kind {kind} — skipped.",
                effectRuntime.Name, actualKind);
            return;
        }

        // if duration, handle stacking rules + add expire intent and tick intent (if tick rate > 0)
        if (effectRuntime.DurationFunc != null)
        {
            CreateDurationEffect(state, effectRuntime, ref effectData);
            return;
        }

        CreateInstantEffect(state, effectRuntime, ref effectData);
    }

    // TODO: this will only work for character
    // TODO: what if no source ?
    private void CreateDurationEffect(GameState state, EffectRuntime effectRuntime, ref EffectData effectData)
    {
        var source = effectData.Source;
        var target = effectData.Target;

        var isTargetCharacter = target.Has<CharacterEffects>();
        // Resolve which host to use — single branch point
        IEffectHost host = isTargetCharacter
            ? new CharacterEffectHost(_dirtyTracker, target)
            : new ItemEffectHost(_dirtyTracker, target);

        var existingEffect = host.FindEffect(effectRuntime);

        // check stacking rules if a similar effect already exists
        if (existingEffect is not null)
        {
            var stackingResult = HandleStacking(state, effectRuntime, source, target, existingEffect.Value);

            if (stackingResult is StackingResult.Nop or StackingResult.Refreshed)
                return;

            if (stackingResult == StackingResult.Stacked)
            {
                host.MarkAsDirtyIfNeeded(existingEffect.Value);
                return;
            }
        }

        // Snapshot — host provides target-side values, caller provides source-side
        var snapshot = host.CreateSnapshot();
        snapshot.SourceLevel = source.Get<Level>().Value;
        snapshot.SourceStats = source.Get<EffectiveStats>().Values;

        // create effect with snapshotted values
        var effect = _world.CreateEntity();
        effect.Set(new EffectInstance
        {
            Source = source,
            Target = target,
            StackCount = 1,
            EffectRuntime = effectRuntime,
        });
        effect.Set(snapshot);

        // add effect to target effect cache
        host.RegisterEffect(effect, effectRuntime);

        _logger.LogInformation(LogEvents.Factory, "Creating Effect {name} Source {source} Target {target}", effectRuntime.Name, source.DebugName, target.DebugName);

        // --- Everything below is target-agnostic ---

        var ctx = new EffectContext
        {
            Effect = effect, // non-instant effect
            Source = source,
            Target = target,

            IncomingDamage = 0,
            EffectiveDamageAmount = 0,

            StackCount = 1,

            State = state,
        };

        // add TimedEffect component to effect
        var duration = Math.Max(1, effectRuntime.DurationFunc!.Invoke(ctx));
        var expirationTick = state.CurrentTick + TimeConversion.SecondsToTicks(duration);
        var tickRate = effectRuntime.TickRate; // 0: means pure duration
        var nextTick = effectRuntime.TickOnApply
            ? state.CurrentTick
            : state.CurrentTick + tickRate; // 0: means pure duration
        effect.Set(new TimedEffect
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
        if (isTargetCharacter && effectRuntime.OnApply.Length > 0)
        {
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };

            foreach (var onApply in effectRuntime.OnApply)
            {
                if (CharacterHelpers.IsAlive(source, target))
                    onApply.Invoke(effectExecutionContext);
            }
        }
    }

    private void CreateInstantEffect(GameState state, EffectRuntime effectRuntime, ref EffectData effectData)
    {
        // trigger onApply actions
        if (effectRuntime.OnApply.Length > 0)
        {
            var source = effectData.Source;
            var target = effectData.Target;
            var effectiveDamageAmount = effectData.EffectiveDamageAmount;

            var ctx = new EffectContext
            {
                Effect = null, // instant effect
                Source = source,
                Target = target,

                IncomingDamage = 0,
                EffectiveDamageAmount = effectiveDamageAmount,

                StackCount = 1,

                State = state,
            };
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };

            foreach (var onApply in effectRuntime.OnApply)
            {
                if (CharacterHelpers.IsAlive(source, target))
                    onApply.Invoke(effectExecutionContext);
            }
        }
    }

    private StackingResult HandleStacking(GameState state, EffectRuntime effectRuntime, Entity source, Entity target, Entity existingEffect)
    {
        ref var instance = ref existingEffect.Get<EffectInstance>();
        if (source != instance.Source)
            return StackingResult.DifferentSource;

        // now we can check stacking rules
        var isTimedEffect = existingEffect.Has<TimedEffect>();
        ref var timedEffect = ref isTimedEffect
            ? ref existingEffect.Get<TimedEffect>()
            : ref Unsafe.NullRef<TimedEffect>();

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
                        Effect = existingEffect,
                        Source = source,
                        Target = target,

                        IncomingDamage = 0,
                        EffectiveDamageAmount = 0,

                        StackCount = instance.StackCount,

                        State = state,
                    };

                    // update Duration
                    var duration = Math.Max(1, effectRuntime.DurationFunc?.Invoke(ctx) ?? 1);
                    var expirationTick = state.CurrentTick + TimeConversion.SecondsToTicks(duration);
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    _logger.LogInformation(LogEvents.Factory, "Refreshing Effect {name} Source {source} Target {target} Duration {duration} Expiration {expirationTick}", effectRuntime.Name, source.DebugName, instance.Target.DebugName, duration, expirationTick);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref _intent.Schedule.Add();
                    expireScheduleIntent.Effect = existingEffect;
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
                        Effect = existingEffect,
                        Source = source,
                        Target = target,

                        IncomingDamage = 0,
                        EffectiveDamageAmount = 0,

                        StackCount = instance.StackCount,

                        State = state,
                    };

                    // update Duration
                    var duration = Math.Max(1, effectRuntime.DurationFunc?.Invoke(ctx) ?? 1);
                    var expirationTick = state.CurrentTick + TimeConversion.SecondsToTicks(duration);
                    timedEffect.LastRefreshTick = state.CurrentTick;
                    timedEffect.ExpirationTick = expirationTick;

                    // schedule a new expiration event (don't remove the old one, just add a new one with the new expiration tick - when the old one executes it will check the current expiration tick and do nothing if it's different)
                    _logger.LogInformation(LogEvents.Factory, "Stacking/Refreshing Effect {name} Source {source} Target {target} Duration {duration} Expiration {expirationTick} New Stack Count {newStackCount}", effectRuntime.Name, source.DebugName, instance.Target.DebugName, duration, expirationTick, instance.StackCount);

                    // expire schedule intent
                    ref var expireScheduleIntent = ref _intent.Schedule.Add();
                    expireScheduleIntent.Effect = existingEffect;
                    expireScheduleIntent.Kind = ScheduledEventKind.Expire;
                    expireScheduleIntent.ExecuteAt = expirationTick;
                }
                if (stackCountModified)
                    return StackingResult.Stacked;
                else
                    return StackingResult.Refreshed;
            case StackingRule.Replace:
                _logger.LogInformation(LogEvents.Factory, "Replacing Effect {name} Source {source} Target {target}", effectRuntime.Name, source.DebugName, instance.Target.DebugName);
                _effectLifecycleManager.RemoveEffect(existingEffect); // destroy current effect (no wear off message because it's a replacement)
                return StackingResult.Replaced;
        }

        return StackingResult.Nop;
    }
}
