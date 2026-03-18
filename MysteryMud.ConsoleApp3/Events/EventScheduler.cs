using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Events;

static class EventScheduler
{
    static readonly PriorityQueue<TimedEvent, (long time, EventType eventType)> queue = new();

    public static void Schedule(TimedEvent ev)
    {
        queue.Enqueue(ev, (ev.ExecuteAt, ev.Type));
    }

    public static void ProcessEvents(World world, long now)
    {
        // Process all events that are due to execute at or before the current time
        // priority is determined first by execution time, then by event type (to ensure consistent ordering of events scheduled for the same time)
        while (queue.TryPeek(out var ev, out var priority) && priority.time <= now)
        {
            queue.Dequeue();
            EventProcessor.Execute(world, ref ev);
        }
    }
}
