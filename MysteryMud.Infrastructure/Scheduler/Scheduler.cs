using Arch.Core;
using MysteryMud.Application.Systems;
using MysteryMud.Core;
using MysteryMud.Core.Scheduler;

namespace MysteryMud.Infrastructure.Scheduler;

public class Scheduler : IScheduler
{
    private readonly PriorityQueue<ScheduledEvent, (long time, ScheduledEventType eventType)> _queue = new();

    public void Schedule(Entity entity, ScheduledEventType type, long executedAt)
    {
        var scheduledEvent = new ScheduledEvent
        {
            Target = entity,
            Type = type,
            ExecuteAt = executedAt
        };
        _queue.Enqueue(scheduledEvent, (scheduledEvent.ExecuteAt, scheduledEvent.Type));
    }

    public void Process(SystemContext systemContext, GameState state)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (_queue.TryPeek(out var ev, out var priority) && priority.time <= state.CurrentTick)
        {
            _queue.Dequeue();
            Execute(systemContext, state, ref ev);
        }
    }

    private static void Execute(SystemContext systemContext, GameState state, ref ScheduledEvent ev)
    {
        switch (ev.Type)
        {
            case ScheduledEventType.DotTick:
                DotSystem.HandleTick(systemContext, state, ev.Target);
                break;

            case ScheduledEventType.HotTick:
                HotSystem.HandleTick(systemContext, state, ev.Target);
                break;

            case ScheduledEventType.EffectExpired:
                DurationSystem.HandleExpiration(systemContext, state, ev.Target);
                break;
        }
    }
}
