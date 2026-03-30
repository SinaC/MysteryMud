using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Damage.Resolvers;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class DotSystem
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly DamageResolver _damageResolver;
    private readonly IEventBuffer<DotTriggeredEvent> _dots;

    public DotSystem(ILogger logger, IScheduler scheduler, DamageResolver damageResolver, IEventBuffer<DotTriggeredEvent> dots)
    {
        _logger = logger;
        _scheduler = scheduler;
        _damageResolver = damageResolver;
        _dots = dots;
    }

    public void Tick(GameState state)
    {
        foreach (ref var dot in _dots.GetAll())
            ProcessOneEffect(state, dot.Effect);
    }

    public void ProcessOneEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            _logger.LogInformation(LogEvents.Dot,"Ticking DoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);
            return;
        }

        ref var dot = ref effect.Get<DamageOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (dot.NextTick >= duration.ExpirationTick)
        {
            _logger.LogInformation(LogEvents.Dot,"Ticking DoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, effectInstance.Target.DebugName, dot.TickRate);
            return;
        }

        // resolve damage
        var totalDamage = dot.Damage * effectInstance.StackCount;
        var damageAction = new DamageAction
        {
            Source = effectInstance.Source,
            Target = effectInstance.Target,
            Amount = totalDamage,
            DamageKind = dot.DamageType,
            SourceKind = DamageSourceKind.DoT
        };
        _logger.LogInformation(LogEvents.Dot, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageType} and tick rate {tickRate}", effect.DebugName, effectInstance.Target.DebugName, totalDamage, dot.DamageType, dot.TickRate);
        _damageResolver.Resolve(in damageAction);

        // calcule next tick
        dot.NextTick = state.CurrentTick + dot.TickRate;
        var remainingTicks = duration.ExpirationTick - state.CurrentTick;

        // queue next tick even if after expiration tick to handle effect refresh
        _logger.LogInformation(LogEvents.Dot,"Scheduling next DoT tick for Effect {effectName} on Target {targetName} and Remaining Tick {remainingTicks} at tick {nextTick}", effect.DebugName, effectInstance.Target.DebugName, remainingTicks, dot.NextTick);
        _scheduler.Schedule(effect, ScheduledEventType.DotTick, dot.NextTick);
    }
}
