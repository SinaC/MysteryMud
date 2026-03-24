using Arch.Core;
using MysteryMud.Application.Commands.Dispatcher;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using System.Buffers;
using System.Collections.Concurrent;

namespace MysteryMud.Infrastructure.Eventing;

public class CommandBus : ICommandBus
{
    private readonly ConcurrentQueue<CommandEvent> _queue = new();

    public void Publish(Entity player, ReadOnlySpan<char> span)
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

    public void Process(SystemContext ctx, GameState gameState)
    {
        // TOOD: we should process commands received since the last tick,
        // but with this loop, we will continue processing commands until the queue is empty, which could lead to starvation of other systems if a lot of commands are received.
        while (_queue.TryDequeue(out var cmd)) // TODO: consider processing a batch of commands instead of one at a time
        {
            var span = cmd.Buffer.AsSpan(0, cmd.Length);

            // TODO: check min position
            CommandDispatcher.Dispatch(ctx, gameState, cmd.Player, span);

            ArrayPool<char>.Shared.Return(cmd.Buffer);
        }
    }
}
