using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public static class Scheduler
{
    private static readonly PriorityQueue<ScheduledEvent, (long time, ScheduledEventType eventType)> _queue = new();

    public static void Publish(ScheduledEvent ev)
    {
        _queue.Enqueue(ev, (ev.ExecuteAt, ev.Type));
    }

    public static void Process(GameState state)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (_queue.TryPeek(out var ev, out var priority) && priority.time <= state.CurrentTick)
        {
            _queue.Dequeue();
            Execute(state, ref ev);
        }
    }

    private static void Execute(GameState state, ref ScheduledEvent ev)
    {
        switch (ev.Type)
        {
            case ScheduledEventType.DotTick:
                DotSystem.HandleTick(state, ev.Target);
                break;

            case ScheduledEventType.HotTick:
                HotSystem.HandleTick(state, ev.Target);
                break;

            case ScheduledEventType.EffectExpired:
                DurationSystem.HandleExpiration(state, ev.Target);
                break;
        }
    }
}
