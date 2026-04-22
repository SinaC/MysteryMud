using MysteryMud.Core.Bus;
using System.Collections;

namespace MysteryMud.Tests.Infrastructure;

internal class TestEventBuffer<TEvent> : IEventBuffer<TEvent>, IEnumerable<TEvent>
    where TEvent : struct
{
    private TEvent[] _items = new TEvent[16];
    private int _count = 0;

    public ref TEvent Add()
    {
        if (_count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);

        _items[_count] = default;
        return ref _items[_count++];
    }

    public Span<TEvent> GetAll() => _items.AsSpan(0, _count);

    public void Clear() => _count = 0;

    // test helpers
    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public TEvent this[int index] => _items[index];
    public bool Any(Func<TEvent, bool> predicate) => GetAll().ToArray().Any(predicate);
    public void Add(TEvent evt)
    {
        if (_count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);
        _items[_count++] = evt;
    }

    // IEnumerable<TEvent> implementation
    public IEnumerator<TEvent> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _items[i];
        }
    }

    // Non-generic IEnumerable (required)
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}