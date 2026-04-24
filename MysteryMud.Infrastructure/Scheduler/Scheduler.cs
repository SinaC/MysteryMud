using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using TinyECS;

namespace MysteryMud.Infrastructure.Scheduler;

public class Scheduler : IScheduler
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IEventBuffer<TriggeredScheduledEvent> _triggeredScheduledEvents;

    private readonly PriorityQueue<ScheduledEvent, (long time, ScheduledEventKind eventKind)> _queue = new();

    public Scheduler(World world, ILogger logger, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents)
    {
        _world = world;
        _logger = logger;
        _triggeredScheduledEvents = triggeredScheduledEvents;
    }

    public void Schedule(GameState state, EntityId entity, ScheduledEventKind kind, long executeAt)
    {
        _logger.LogDebug("[{system}]: schedule {effectName} kind {kind} execute at {executeAt}", nameof(Scheduler), EntityHelpers.DebugName(_world, entity), kind, executeAt);

        var scheduledEvent = new ScheduledEvent
        {
            Target = entity,
            Kind = kind,
            ExecuteAt = executeAt
        };
        _queue.Enqueue(scheduledEvent, (scheduledEvent.ExecuteAt, scheduledEvent.Kind));
    }

    public void Process(GameState state)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (_queue.TryPeek(out var ev, out var priority) && priority.time <= state.CurrentTick)
        {
            _queue.Dequeue();
            Execute(ref ev);
        }
    }

    private void Execute(ref ScheduledEvent ev)
    {
        _logger.LogDebug("[{system}]: execute {effectName} kind {kind} execute at {executeAt}", nameof(Scheduler), EntityHelpers.DebugName(_world, ev.Target), ev.Kind, ev.ExecuteAt);

        switch (ev.Kind)
        {
            case ScheduledEventKind.Tick:
                // emit triggered scheduled event
                ref var tickEvt = ref _triggeredScheduledEvents.Add();
                tickEvt.Effect = ev.Target;
                tickEvt.Kind = ScheduledEventKind.Tick;
                break;

            case ScheduledEventKind.Expire:
                // emit triggered scheduled event
                ref var expiredEvt = ref _triggeredScheduledEvents.Add();
                expiredEvt.Effect = ev.Target;
                expiredEvt.Kind = ScheduledEventKind.Expire;
                break;
        }
    }
}
