using Arch.Core;
using MysteryMud.ConsoleApp3.Commands.Dispatcher;
using System.Buffers;
using System.Collections.Concurrent;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public static class CommandBus
{
    private static readonly ConcurrentQueue<CommandEvent> _queue = new();

    public static void Publish(Entity player, ReadOnlySpan<char> span)
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

    public static void Process(GameState gameState)
    {
        // TOOD: we should process commands received since the last tick,
        // but with this loop, we will continue processing commands until the queue is empty, which could lead to starvation of other systems if a lot of commands are received.
        while (_queue.TryDequeue(out var cmd)) // TODO: consider processing a batch of commands instead of one at a time
        {
            var span = cmd.Buffer.AsSpan(0, cmd.Length);

            // TODO: check min position
            CommandDispatcher.Dispatch(gameState, cmd.Player, span);

            ArrayPool<char>.Shared.Return(cmd.Buffer);
        }
    }
}
