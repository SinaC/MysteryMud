namespace MysteryMud.Infrastructure;

public sealed class StructBuffer<T>
    where T : struct
{
    private T[] _items;
    private int _count;

    public StructBuffer(int capacity = 128)
    {
        _items = new T[capacity];
        _count = 0;
    }

    public int Count => _count;
    public int Capacity => _items.Length;

    public ref T Add()
    {
        if (_count >= _items.Length)
            Grow();

        return ref _items[_count++];
    }

    public Span<T> AsSpan() => _items.AsSpan(0, _count);

    public void Clear() => _count = 0;

    private void Grow()
    {
        var newArray = new T[_items.Length * 2];
        Array.Copy(_items, newArray, _items.Length);
        _items = newArray;
    }
}
