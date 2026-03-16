namespace MysteryMud.ConsoleApp.Events;

class EventBus
{
    Queue<object> events = new();

    public void Emit(object e) => events.Enqueue(e);

    public IEnumerable<object> Drain()
    {
        while (events.Count > 0)
            yield return events.Dequeue();
    }
}
