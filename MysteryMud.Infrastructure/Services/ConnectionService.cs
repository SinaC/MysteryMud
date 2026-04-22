using Arch.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Factories;
using MysteryMud.Infrastructure.Network;

namespace MysteryMud.Infrastructure.Services;

public class ConnectionService : IConnectionService
{
    private readonly World _world;
    private readonly TelnetServer _telnet;   // ← inject this

    private readonly Dictionary<int, Entity> _connToEntity = new();
    private readonly Dictionary<Entity, int> _entityToConn = new();

    public ConnectionService(World world, TelnetServer telnetServer)
    {
        _world = world;
        _telnet = telnetServer;
    }

    public Entity CreatePlayer(int connectionId)
    {
        var player = PlayerFactory.CreateConnectingPlayer(_world, connectionId);

        _connToEntity[connectionId] = player;
        _entityToConn[player] = connectionId;

        return player;
    }

    public bool TryGetEntity(int connectionId, out Entity entity)
        => _connToEntity.TryGetValue(connectionId, out entity);

    public bool TryGetConnection(Entity entity, out int connectionId)
        => _entityToConn.TryGetValue(entity, out connectionId);

    public bool Remove(int connectionId)
    {
        if (!_connToEntity.TryGetValue(connectionId, out var entity))
            return false;
        _connToEntity.Remove(connectionId);
        _entityToConn.Remove(entity);
        return true;
    }

    public void Disconnect(Entity entity)
    {
        if (!_entityToConn.TryGetValue(entity, out var connectionId))
            return;

        // Closing the socket triggers TelnetSession.finally
        // → HandleDisconnected in TelnetServer
        // → OnDisconnected in GameServer
        // → GameServer.HandleDisconnected sets DisconnectedTag
        // But DisconnectedTag is already set by QuitCommand, so it's a no-op.
        _telnet.Disconnect(connectionId);
    }
}
