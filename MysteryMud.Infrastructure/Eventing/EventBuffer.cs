using MysteryMud.Core.Eventing;

namespace MysteryMud.Infrastructure.Eventing;

public sealed class EventBuffer<TEvent> : IEventBuffer<TEvent>
    where TEvent : struct
{
    private readonly StructBuffer<TEvent> _buffer;

    public EventBuffer(int capacity = 128)
    {
        _buffer = new StructBuffer<TEvent>(capacity);
    }

    public ref TEvent Add() => ref _buffer.Add();

    public Span<TEvent> GetAll() => _buffer.AsSpan();

    public void Clear() => _buffer.Clear();
}