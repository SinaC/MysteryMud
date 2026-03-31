using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Heal;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

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
            if (evt.Kind != ScheduledEventKind.Tick && evt.Kind != ScheduledEventKind.Expire)
                continue;

            var effect = evt.Effect;
            if (!EffectHelpers.IsAlive(effect))
                continue;

            ref var timed = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
            if (!isTimedEffect)
                continue;

            // expire -> add expired tag
            if (evt.Kind == ScheduledEventKind.Expire && !effect.Has<ExpiredTag>())
            {
                // flag as expired
                effect.Add<ExpiredTag>();

                // display wear off message if any
                ref var instance = ref effect.Get<EffectInstance>();
                if (instance.Definition.WearOffMessage != null)
                {
                    // TODO: in room ?
                    _msg.To(instance.Target).Send(instance.Definition.WearOffMessage);
                }

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
                    continue;

                ref var instance = ref effect.Get<EffectInstance>();
                ApplyEffect(effect, ref instance);

                // intent for next tick
                timed.NextTick = state.CurrentTick + timed.TickRate;

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

    private void ApplyEffect(Entity effect, ref EffectInstance instance)
    {
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
            _healResolver.Resolve(healAction);
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
            _damageResolver.Resolve(in damageAction);
        }

        // TOOD: other
    }
}
