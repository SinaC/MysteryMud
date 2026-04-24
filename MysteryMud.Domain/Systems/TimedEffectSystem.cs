using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public class TimedEffectSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly IEffectExecutor _effectExecutor;
    private readonly IEventBuffer<TriggeredScheduledEvent> _triggeredScheduledEvents;
    private readonly IEventBuffer<EffectExpiredEvent> _effectExpiredEvents;
    private readonly IEventBuffer<EffectTickedEvent> _effectTickedEvents;

    public TimedEffectSystem(World world, ILogger logger, IGameMessageService msg, IIntentContainer intentContainer, IEffectExecutor effectExecutor, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents, IEventBuffer<EffectExpiredEvent> effectExpiredEvents, IEventBuffer<EffectTickedEvent> effectTickedEvents)
    {
        _world = world;
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
        foreach (ref var evt in _triggeredScheduledEvents.GetAll())
            ProcessEvent(state, ref evt);
    }

    private void ProcessEvent(GameState state, ref TriggeredScheduledEvent evt)
    {
        _logger.LogDebug("[{system}]", nameof(TimedEffectSystem));

        if (evt.Kind != ScheduledEventKind.Tick && evt.Kind != ScheduledEventKind.Expire)
        {
            _logger.LogDebug("[{system}]: invalid scheduled event kind {kind}", nameof(TimedEffectSystem), evt.Kind);
            return;
        }

        var effect = evt.Effect;
        if (!EffectHelpers.IsAlive(_world, effect))
        {
            _logger.LogDebug("[{system}]: effect {effectName} is not alive", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect));
            return;
        }

        ref var timed = ref _world.TryGetRef<TimedEffect>(effect, out var isTimedEffect);
        if (!isTimedEffect)
        {
            _logger.LogDebug("[{system}]: effect {effectName} without TimedEffect", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect));
            return;
        }

        // expire -> add expired tag if not rescheduled
        if (evt.Kind == ScheduledEventKind.Expire && !_world.Has<ExpiredTag>(effect))
        {
            _logger.LogDebug("[{system}]: effect {effectName} EXPIRE {expirationTick}", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect), timed.ExpirationTick);

            // TODO
            // rescheduled (stacking Refresh or Stack)
            if (timed.ExpirationTick != state.CurrentTick)
            {
                _logger.LogDebug("[{system}]: effect {effectName} RESCHEDULED {lastRefresh} EXPIRE {expirationTick}", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect), timed.LastRefreshTick, timed.ExpirationTick);

                return;
            }

            // flag as expired
            if (!_world.Has<ExpiredTag>(effect))
                _world.Add<ExpiredTag>(effect);

            ref var instance = ref _world.Get<EffectInstance>(effect);
            ExpireEffect(state, effect, ref instance);

            // effect expired event
            ref var effectExpiredEvt = ref _effectExpiredEvents.Add();
            effectExpiredEvt.Effect = effect;
            return;
        }

        // tick -> heal/damage/...
        if (evt.Kind == ScheduledEventKind.Tick)
        {
            // tick after expiration
            if (state.CurrentTick >= timed.ExpirationTick)
            {
                _logger.LogDebug("[{system}]: effect {effectName} TICK after expiration tick {expirationTick}", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect), timed.ExpirationTick);
                return;
            }

            ref var instance = ref _world.Get<EffectInstance>(effect);
            TickEffect(state, effect, ref instance);

            // intent for next tick
            timed.NextTick = state.CurrentTick + timed.TickRate;

            _logger.LogDebug("[{system}]: effect {effectName} TICK {expirationTick} NEXT {nextTick}", nameof(TimedEffectSystem), EntityHelpers.DebugName(_world, effect), timed.ExpirationTick, timed.NextTick);

            ref var scheduleIntent = ref _intentContainer.Schedule.Add();
            scheduleIntent.Effect = effect;
            scheduleIntent.Kind = ScheduledEventKind.Tick;
            scheduleIntent.ExecuteAt = timed.NextTick;

            // effect tick event
            ref var effectTickEvt = ref _effectTickedEvents.Add();
            effectTickEvt.Effect = effect;
        }
    }

    private void ExpireEffect(GameState state, EntityId effect, ref EffectInstance instance)
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
                World = _world,
            };
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };
            foreach (var onExpire in effectRuntime.OnExpire)
            {
                if (CharacterHelpers.IsAlive(_world, instance.Source, instance.Target))
                    onExpire(effectExecutionContext);
            }
        }
    }

    private void TickEffect(GameState state, EntityId effect, ref EffectInstance instance)
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
                World = _world,
            };
            var effectExecutionContext = new EffectExecutionContext
            {
                Context = ctx,
                Executor = _effectExecutor,
                Msg = _msg
            };
            foreach (var onTick in effectRuntime.OnTick)
            {
                if (CharacterHelpers.IsAlive(_world, instance.Source, instance.Target))
                    onTick(effectExecutionContext);
            }
        }
    }
}
