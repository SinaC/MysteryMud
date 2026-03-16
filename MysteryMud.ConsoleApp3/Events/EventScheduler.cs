namespace MysteryMud.ConsoleApp3.Events;

class EventScheduler
{
    PriorityQueue<TimedEvent, long> queue = new();

    public void Schedule(TimedEvent ev)
    {
        queue.Enqueue(ev, ev.ExecuteAt);
    }

    public void Update(long now)
    {
        while (queue.TryPeek(out var ev, out var time) && time <= now)
        {
            queue.Dequeue();
            EventProcessor.Execute(ev);
        }
    }
}
