using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Network;

namespace MysteryMud.ConsoleApp3.Services;

public class MessageService : IMessageService
{
    private readonly TelnetServer _telnet;

    public MessageService(TelnetServer telnet)
    {
        _telnet = telnet;
    }

    public void Send(Entity entity, string message)
    {
        if (!entity.IsAlive())
            return;

        ref var connection = ref entity.TryGetRef<Connection>(out var hasConnection);
        if (!hasConnection)
        {
            Console.WriteLine($"[{entity.DebugName}]: {message}");
            return;
        }

        _telnet.Write(connection.ConnectionId, message);
        _telnet.Write(connection.ConnectionId, "\r\n");
    }

    public void Flush(Entity entity)
    {
        ref var connection = ref entity.TryGetRef<Connection>(out var hasConnection);
        if (!hasConnection)
            return;

        _telnet.Flush(connection.ConnectionId);
    }
}
