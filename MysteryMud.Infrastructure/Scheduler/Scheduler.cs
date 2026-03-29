using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;
using MysteryMud.GameData.Events;

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

    public void Process(GameState state, IEventBuffer<DotTriggeredEvent> dots, IEventBuffer<HotTriggeredEvent> hots, IEventBuffer<EffectExpiredEvent> expired)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (_queue.TryPeek(out var ev, out var priority) && priority.time <= state.CurrentTick)
        {
            _queue.Dequeue();
            Execute(dots, hots, expired, ref ev);
        }
    }

    private static void Execute(IEventBuffer<DotTriggeredEvent> dots, IEventBuffer<HotTriggeredEvent> hots, IEventBuffer<EffectExpiredEvent> expired, ref ScheduledEvent ev)
    {
        switch (ev.Type)
        {
            case ScheduledEventType.DotTick:
                // emit dot triggered event
                ref var dotEvt = ref dots.Add();
                dotEvt.Effect = ev.Target;
                break;

            case ScheduledEventType.HotTick:
                // emit hot triggered event
                ref var hotEvt = ref hots.Add();
                hotEvt.Effect = ev.Target;
                break;

            case ScheduledEventType.EffectExpired:
                // emit expired event
                ref var expiredEvt = ref expired.Add();
                expiredEvt.Effect = ev.Target;
                break;
        }
    }
}
