using Arch.Core;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Infrastructure.Scheduler;

public class Scheduler : IScheduler
{
    private readonly ILogger _logger;

    private readonly PriorityQueue<ScheduledEvent, (long time, ScheduledEventKind eventKind)> _queue = new();

    public Scheduler(ILogger logger)
    {
        _logger = logger;
    }

    public void Schedule(GameState state, Entity entity, ScheduledEventKind kind, long executeAt)
    {
        _logger.LogDebug("[{system}]: schedule {effectName} kind {kind} execute at {executeAt}", nameof(Scheduler), entity.DebugName, kind, executeAt);

        var scheduledEvent = new ScheduledEvent
        {
            Target = entity,
            Kind = kind,
            ExecuteAt = executeAt
        };
        _queue.Enqueue(scheduledEvent, (scheduledEvent.ExecuteAt, scheduledEvent.Kind));
    }

    public void Process(GameState state, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (_queue.TryPeek(out var ev, out var priority) && priority.time <= state.CurrentTick)
        {
            _queue.Dequeue();
            Execute(state, triggeredScheduledEvents, ref ev);
        }
    }

    private void Execute(GameState state, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents, ref ScheduledEvent ev)
    {
        _logger.LogDebug("[{system}]: execute {effectName} kind {kind} execute at {executeAt}", nameof(Scheduler), ev.Target.DebugName, ev.Kind, ev.ExecuteAt);

        switch (ev.Kind)
        {
            case ScheduledEventKind.Tick:
                // emit triggered scheduled event
                ref var tickEvt = ref triggeredScheduledEvents.Add();
                tickEvt.Effect = ev.Target;
                tickEvt.Kind = ScheduledEventKind.Tick;
                break;

            case ScheduledEventKind.Expire:
                // emit triggered scheduled event
                ref var expiredEvt = ref triggeredScheduledEvents.Add();
                expiredEvt.Effect = ev.Target;
                expiredEvt.Kind = ScheduledEventKind.Expire;
                break;
        }
    }
}
