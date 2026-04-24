using MysteryMud.Application.Dispatching;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using System.Buffers;
using System.Collections.Concurrent;
using TinyECS;

namespace MysteryMud.Infrastructure.Eventing;

public class CommandBus : ICommandBus
{
    private readonly ICommandDispatcher _dispatcher;

    private readonly ConcurrentQueue<CommandEvent> _queue = new();

    public CommandBus(ICommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Publish(EntityId player, ReadOnlySpan<char> span)
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

    public void Process(GameState state)
    {
        int maxCommands = 100; // Prevent global starvation
        while (maxCommands-- > 0 && _queue.TryDequeue(out var cmd))
        {
            var span = cmd.Buffer.AsSpan(0, cmd.Length);

            // TODO: check min position
            _dispatcher.Dispatch(cmd.Player, span);

            ArrayPool<char>.Shared.Return(cmd.Buffer);
        }
    }
}
