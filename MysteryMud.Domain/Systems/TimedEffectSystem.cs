using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Damage;
using MysteryMud.Domain.Effect;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Heal;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Systems;

public class TimedEffectSystem
{
    private readonly ILogger _logger;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly DamageResolver _damageResolver;
    private readonly HealResolver _healResolver;
    private readonly IEventBuffer<TriggeredScheduledEvent> _triggeredScheduledEvents;
    private readonly IEventBuffer<EffectExpiredEvent> _effectExpiredEvents;
    private readonly IEventBuffer<EffectTickedEvent> _effectTickedEvents;

    public TimedEffectSystem(ILogger logger, IGameMessageService msg, IIntentContainer intentContainer, DamageResolver damageResolver, HealResolver healResolver, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents, IEventBuffer<EffectExpiredEvent> effectExpiredEvents, IEventBuffer<EffectTickedEvent> effectTickedEvents)
    {
        _logger = logger;
        _msg = msg;
        _intentContainer = intentContainer;
        _damageResolver = damageResolver;
        _healResolver = healResolver;
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
                ApplyEffect(state, effect, ref instance);

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
        if (instance.EffectRuntime != null && instance.EffectRuntime.OnExpire != null && instance.EffectRuntime.OnExpire.Length > 0)
        {
            var ctx = new EffectContext
            {
                Effect = effect,
                Source = instance.Source,
                Target = instance.Target,

                IncomingDamage = 0,
                LastDamage = 0,

                StackCount = instance.StackCount,

                State = state,
                Msg = _msg,
                DamageResolver = _damageResolver,
                HealResolver = _healResolver,
            };
            foreach (var onExpire in instance.EffectRuntime.OnExpire)
            {
                if (CharacterHelpers.IsAlive(instance.Source, instance.Target))
                    onExpire(ctx);
            }
        }

        // display wear off message if any
        if (instance.Definition?.WearOffMessage != null)
        {
            // TODO: in room ?
            _msg.To(instance.Target).Send(instance.Definition.WearOffMessage);
        }
    }

    private void ApplyEffect(GameState state, Entity effect, ref EffectInstance instance)
    {
        if (instance.EffectRuntime != null && instance.EffectRuntime.OnTick.Length > 0)
        {
            var ctx = new EffectContext
            {
                Effect = effect,
                Source = instance.Source,
                Target = instance.Target,

                IncomingDamage = 0,
                LastDamage = 0,

                StackCount = instance.StackCount,

                State = state,
                Msg = _msg,
                DamageResolver = _damageResolver,
                HealResolver = _healResolver,
            };
            foreach (var onTick in instance.EffectRuntime.OnTick)
            {
                if (CharacterHelpers.IsAlive(instance.Source, instance.Target))
                    onTick(ctx);
            }
        }

        ref var healEffect = ref effect.TryGetRef<HealEffect>(out var hasHealEffect);
        if (hasHealEffect)
        {
            var totalHeal = healEffect.Heal * instance.StackCount;
            var healAction = new HealAction
            {
                Source = instance.Source,
                Target = instance.Target,
                Amount = totalHeal,
                SourceKind = HealSourceKind.HoT
            };
            _logger.LogInformation(LogEvents.Hot, "Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal}", effect.DebugName, instance.Target.DebugName, totalHeal);
            _healResolver.Resolve(state, healAction);
        }

        ref var damageEffect = ref effect.TryGetRef<DamageEffect>(out var hasDamageEffect);
        if (hasDamageEffect)
        {
            var totalDamage = damageEffect.Damage * instance.StackCount;
            var damageAction = new DamageAction
            {
                Source = instance.Source,
                Target = instance.Target,
                Amount = totalDamage,
                DamageKind = damageEffect.DamageKind,
                SourceKind = DamageSourceKind.DoT
            };
            _logger.LogInformation(LogEvents.Dot, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageKind}", effect.DebugName, instance.Target.DebugName, totalDamage, damageEffect.DamageKind);
            _damageResolver.Resolve(state, damageAction);
        }

        // TOOD: other
    }
}
