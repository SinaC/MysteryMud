using Arch.Core;
using System.Buffers;
using System.Collections.Concurrent;

namespace MysteryMud.ConsoleApp3.Commands;

public static class CommandQueue
{
    private static readonly ConcurrentQueue<CommandEvent> _queue = new();

    public static void Enqueue(Entity player, ReadOnlySpan<char> span)
    {
        var buffer = ArrayPool<char>.Shared.Rent(span.Length);

        span.CopyTo(buffer);

        _queue.Enqueue(new CommandEvent
        {
            Player = player,
            Buffer = buffer,
            Length = span.Length
        });
    }

    public static bool TryDequeue(out CommandEvent cmd)
        => _queue.TryDequeue(out cmd);
}
