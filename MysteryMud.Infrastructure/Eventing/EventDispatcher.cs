using MysteryMud.Core.Bus;

namespace MysteryMud.Infrastructure.Eventing;

public sealed class EventDispatcher<T> : IEventDispatcher<T>
    where T : struct
{
    private readonly List<Action<T>> _subs = new();

    public void Subscribe(Action<T> handler) => _subs.Add(handler);

    public void Dispatch(ReadOnlySpan<T> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var evt = events[i];
            foreach (var sub in _subs)
                sub(evt);
        }
    }
}
