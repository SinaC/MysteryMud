using Arch.Core;
using MysteryMud.ConsoleApp3.Commands.Dispatcher;
using MysteryMud.ConsoleApp3.Events;
using System.Buffers;
using System.Collections.Concurrent;

namespace MysteryMud.ConsoleApp3.Systems;

public static class CommandSystem
{
    private static readonly ConcurrentQueue<CommandEvent> _queue = new();

    public static void ProcessCommands(World world)
    {
        // TOOD: we should process commands received since the last tick,
        // but with this loop, we will continue processing commands until the queue is empty, which could lead to starvation of other systems if a lot of commands are received.
        while (TryDequeue(out var cmd)) // TODO: consider processing a batch of commands instead of one at a time
        {
            var span = cmd.Buffer.AsSpan(0, cmd.Length);

            // TODO: check min position
            CommandDispatcher.Dispatch(world, cmd.Player, span);

            ArrayPool<char>.Shared.Return(cmd.Buffer);
        }
    }

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

    private static bool TryDequeue(out CommandEvent cmd)
        => _queue.TryDequeue(out cmd);
}
