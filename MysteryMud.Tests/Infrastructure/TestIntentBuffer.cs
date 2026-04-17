using MysteryMud.Core.Contracts;

namespace MysteryMud.Tests.Infrastructure;

internal class TestIntentBuffer<TIntent> : IIntentWriter<TIntent> where TIntent : struct
{
    private TIntent[] _items = new TIntent[16];
    private int _count = 0;

    public ref TIntent Add()
    {
        if (_count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);
        _items[_count] = default;
        return ref _items[_count++];
    }

    public Span<TIntent> Span => _items.AsSpan(0, _count);
    public int Count => _count;
    public ref TIntent ByIndex(int i) => ref _items[i];
    public void Clear() => _count = 0;

    // test assertion helpers
    public bool IsEmpty => _count == 0;
    public bool Any(Func<TIntent, bool> predicate) => Span.ToArray().Any(predicate);
    public TIntent First(Func<TIntent, bool> predicate) => Span.ToArray().First(predicate);
}