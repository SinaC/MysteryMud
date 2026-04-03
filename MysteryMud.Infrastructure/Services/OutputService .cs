using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Extensions;
using MysteryMud.Infrastructure.Network;

namespace MysteryMud.Infrastructure.Services;

public class OutputService : IOutputService
{
    private readonly ILogger _logger;
    private readonly TelnetServer _telnet;

    public OutputService(ILogger logger, TelnetServer telnet)
    {
        _logger = logger;
        _telnet = telnet;
    }

    public void Send(Entity entity, string message)
    {
        if (!entity.IsAlive())
            return;

        ref var connection = ref entity.TryGetRef<Connection>(out var hasConnection);
        if (!hasConnection)
        {
            _logger.LogInformation("[{entityName}]: {message}", entity.DebugName, message);
            return;
        }

        _logger.LogInformation("[{entityName}]: {message}", entity.DebugName, message);
        _telnet.Write(connection.ConnectionId, message);
        _telnet.Write(connection.ConnectionId, "\r\n");
    }

    public void FlushAll()
    {
        _telnet.FlushAll();
    }
}
