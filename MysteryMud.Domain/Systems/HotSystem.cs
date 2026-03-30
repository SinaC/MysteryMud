using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Heal;
using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class HotSystem
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly HealResolver _healResolver;
    private readonly IEventBuffer<HotTriggeredEvent> _hots;

    public HotSystem(ILogger logger, IScheduler scheduler, HealResolver healResolver, IEventBuffer<HotTriggeredEvent> hots)
    {
        _logger = logger;
        _scheduler = scheduler;
        _healResolver = healResolver;
        _hots = hots;
    }

    public void Tick(GameState state)
    {
        foreach (ref var hot in _hots.GetAll())
            ProcessOneEffect(state, hot.Effect);
    }

    public void ProcessOneEffect(GameState state, Entity effect)
    {
        if (!effect.IsAlive())
            return;

        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (!effectInstance.Target.IsAlive() || effectInstance.Target.Has<Dead>())
        {
            _logger.LogInformation(LogEvents.Hot,"Ticking HoT for Effect {effectName} on DEAD Target {targetName}", effect.DebugName, effectInstance.Target.DebugName);
            return;
        }

        ref var hot = ref effect.Get<HealOverTime>();
        ref var duration = ref effect.Get<Duration>();

        // too late
        if (hot.NextTick >= duration.ExpirationTick)
        {
            _logger.LogInformation(LogEvents.Hot,"Ticking HoT for Effect {effectName} on Target {targetName} and tick rate {tickRate} on EXPIRED effect", effect.DebugName, effectInstance.Target.DebugName, hot.TickRate);
            return;
        }

        // resolve heal
        var totalHeal = hot.Heal * effectInstance.StackCount;
        var healAction = new HealAction
        {
            Source = effectInstance.Source,
            Target = effectInstance.Target,
            Amount = totalHeal,
            SourceKind = HealSourceKind.HoT
        };
        _logger.LogInformation(LogEvents.Hot, "Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal} and tick rate {tickRate}", effect.DebugName, effectInstance.Target.DebugName, totalHeal, hot.TickRate);
        _healResolver.Resolve(healAction);

        // calcule next tick
        hot.NextTick = state.CurrentTick + hot.TickRate;
        var remainingTicks = duration.ExpirationTick - state.CurrentTick;

        // queue next tick even if after expiration tick to handle effect refresh
        _logger.LogInformation(LogEvents.Hot, "Scheduling next HoT tick for Effect {effectName} on Target {targetName} and Remaining Tick {remainingTicks} at tick {nextTick}", effect.DebugName, effectInstance.Target.DebugName, remainingTicks, hot.NextTick);
        _scheduler.Schedule(effect, ScheduledEventType.HotTick, hot.NextTick);
    }
}
