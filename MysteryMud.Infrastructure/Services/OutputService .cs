using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Extensions;
using MysteryMud.Infrastructure.Network;

namespace MysteryMud.Infrastructure.Services;

public class OutputService : IOutputService
{
    private readonly TelnetServer _telnet;

    public OutputService(TelnetServer telnet)
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

    public void FlushAll()
    {
        _telnet.FlushAll();
    }
}
