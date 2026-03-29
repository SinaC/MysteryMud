using MysteryMud.Core.Intent;

namespace MysteryMud.Infrastructure.Intent;

public sealed class IntentWriter<T> : IIntentWriter<T>
    where T : struct
{
    private readonly StructBuffer<T> _buffer;

    public IntentWriter(StructBuffer<T> buffer)
    {
        _buffer = buffer;
    }

    public ref T Add() => ref _buffer.Add();
}
