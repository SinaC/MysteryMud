using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class TimedEffectSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly IEffectExecutor _effectExecutor;
    private readonly IEventBuffer<TriggeredScheduledEvent> _triggeredScheduledEvents;
    private readonly IEventBuffer<EffectExpiredEvent> _effectExpiredEvents;
    private readonly IEventBuffer<EffectTickedEvent> _effectTickedEvents;

    public TimedEffectSystem(ILogger logger, IGameMessageService msg, IIntentContainer intentContainer, IEffectExecutor effectExecutor, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents, IEventBuffer<EffectExpiredEvent> effectExpiredEvents, IEventBuffer<EffectTickedEvent> effectTickedEvents)
    {
        _logger = logger;
        _msg = msg;
        _intentContainer = intentContainer;
        _effectExecutor = effectExecutor;
        _triggeredScheduledEvents = triggeredScheduledEvents;
        _effectExpiredEvents = effectExpiredEvents;
        _effectTickedEvents = effectTickedEvents;
    }

    public void Tick(GameState state)
    {
        foreach (var evt in _triggeredScheduledEvents.GetAll())
        {
            _logger.LogDebug("[{system}]", nameof(TimedEffectSystem));

            if (evt.Kind != ScheduledEventKind.Tick && evt.Kind != ScheduledEventKind.Expire)
            {
                _logger.LogDebug("[{system}]: invalid scheduled event kind {kind}", nameof(TimedEffectSystem), evt.Kind);
                continue;
            }

            var effect = evt.Effect;
            if (!EffectHelpers.IsAlive(effect))
            {
                _logger.LogDebug("[{system}]: effect {effectName} is not alive", nameof(TimedEffectSystem), effect.DebugName);
                continue;
            }

            ref var timed = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
            if (!isTimedEffect)
            {
                _logger.LogDebug("[{system}]: effect {effectName} without TimedEffect", nameof(TimedEffectSystem), effect.DebugName);
                continue;
            }

            // expire -> add expired tag if not rescheduled
            if (evt.Kind == ScheduledEventKind.Expire && !effect.Has<ExpiredTag>())
            {
                _logger.LogDebug("[{system}]: effect {effectName} EXPIRE {expirationTick}", nameof(TimedEffectSystem), effect.DebugName, timed.ExpirationTick);

                // TODO
                // rescheduled (stacking Refresh or Stack)
                if (timed.ExpirationTick != state.CurrentTick)
                {
                    _logger.LogDebug("[{system}]: effect {effectName} RESCHEDULED {lastRefresh} EXPIRE {expirationTick}", nameof(TimedEffectSystem), effect.DebugName, timed.LastRefreshTick, timed.ExpirationTick);

                    continue;
                }

                // flag as expired
                effect.Add<ExpiredTag>();

                ref var instance = ref effect.Get<EffectInstance>();
                ExpireEffect(state, effect, ref instance);

                // effect expired event
                ref var effectExpiredEvt = ref _effectExpiredEvents.Add();
                effectExpiredEvt.Effect = effect;
                continue;
            }

            // tick -> heal/damage/...
            if (evt.Kind == ScheduledEventKind.Tick)
            {
                // tick after expiration
                if (state.CurrentTick >= timed.ExpirationTick)
                {
                    _logger.LogDebug("[{system}]: effect {effectName} TICK after expiration tick {expirationTick}", nameof(TimedEffectSystem), effect.DebugName, timed.ExpirationTick);
                    continue;
                }

                ref var instance = ref effect.Get<EffectInstance>();
                TickEffect(state, effect, ref instance);

                // intent for next tick
                timed.NextTick = state.CurrentTick + timed.TickRate;

                _logger.LogDebug("[{system}]: effect {effectName} TICK {expirationTick} NEXT {nextTick}", nameof(TimedEffectSystem), effect.DebugName, timed.ExpirationTick, timed.NextTick);

                ref var scheduleIntent = ref _intentContainer.Schedule.Add();
                scheduleIntent.Effect = effect;
                scheduleIntent.Kind = ScheduledEventKind.Tick;
                scheduleIntent.ExecuteAt = timed.NextTick;

                // effect tick event
                ref var effectTickEvt = ref _effectTickedEvents.Add();
                effectTickEvt.Effect = effect;
            }
        }
    }

    private void ExpireEffect(GameState state, Entity effect, ref EffectInstance instance)
    {
        ref var effectRuntime = ref instance.EffectRuntime;
        if (effectRuntime != null && effectRuntime.OnExpire != null && effectRuntime.OnExpire.Length > 0)
        {
            var ctx = new EffectContext
            {
                Effect = effect,
                Source = instance.Source,
                Target = instance.Target,

                IncomingDamage = 0,
                EffectiveDamageAmount = 0,

                StackCount = instance.StackCount,

                State = state,
            };
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };
            foreach (var onExpire in effectRuntime.OnExpire)
            {
                if (CharacterHelpers.IsAlive(instance.Source, instance.Target))
                    onExpire(effectExecutionContext);
            }
        }
    }

    private void TickEffect(GameState state, Entity effect, ref EffectInstance instance)
    {
        ref var effectRuntime = ref instance.EffectRuntime;
        if (effectRuntime != null && effectRuntime.OnTick.Length > 0)
        {
            var ctx = new EffectContext
            {
                Effect = effect,
                Source = instance.Source,
                Target = instance.Target,

                IncomingDamage = 0,
                EffectiveDamageAmount = 0,

                StackCount = instance.StackCount,

                State = state,
            };
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };
            foreach (var onTick in effectRuntime.OnTick)
            {
                if (CharacterHelpers.IsAlive(instance.Source, instance.Target))
                    onTick(effectExecutionContext);
            }
        }
    }
}
