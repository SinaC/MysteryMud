using Microsoft.Extensions.Logging;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Helpers;
using MysteryMud.Infrastructure.Network;
using TinyECS;

namespace MysteryMud.Infrastructure.Services;

public class OutputService : IOutputService
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly TelnetServer _telnet;

    public OutputService(World world, ILogger logger, TelnetServer telnet)
    {
        _world = world;
        _logger = logger;
        _telnet = telnet;
    }

    public void Send(EntityId entity, string message)
    {
        if (!_world.IsAlive(entity))
            return;

        ref var connection = ref _world.TryGetRef<Connection>(entity, out var hasConnection);
        if (!hasConnection)
        {
            _logger.LogInformation("[{entityName}]: {message}", EntityHelpers.DebugName(_world, entity), message);
            return;
        }

        _logger.LogInformation("[{entityName}]: {message}", EntityHelpers.DebugName(_world, entity), message);
        _telnet.Write(connection.ConnectionId, message);
        _telnet.Write(connection.ConnectionId, "\r\n");
    }

    public void FlushAll()
    {
        _telnet.FlushAll();
    }
}
