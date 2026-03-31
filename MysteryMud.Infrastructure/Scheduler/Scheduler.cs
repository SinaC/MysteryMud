using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Infrastructure.Scheduler;

public class Scheduler : IScheduler
{
    private readonly PriorityQueue<ScheduledEvent, (long time, ScheduledEventKind eventKind)> _queue = new();

    public void Schedule(Entity entity, ScheduledEventKind kind, long executedAt)
    {
        var scheduledEvent = new ScheduledEvent
        {
            Target = entity,
            Kind = kind,
            ExecuteAt = executedAt
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
            Execute(triggeredScheduledEvents, ref ev);
        }
    }

    private static void Execute(IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents, ref ScheduledEvent ev)
    {
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
