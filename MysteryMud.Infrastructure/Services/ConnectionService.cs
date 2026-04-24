using MysteryMud.Core.Services;
using MysteryMud.Domain.Factories;
using MysteryMud.Infrastructure.Network;
using TinyECS;

namespace MysteryMud.Infrastructure.Services;

public class ConnectionService : IConnectionService
{
    private readonly World _world;
    private readonly TelnetServer _telnet;   // ← inject this

    private readonly Dictionary<int, EntityId> _connToEntity = [];
    private readonly Dictionary<EntityId, int> _entityToConn = [];

    public ConnectionService(World world, TelnetServer telnetServer)
    {
        _world = world;
        _telnet = telnetServer;
    }

    public EntityId CreatePlayer(int connectionId)
    {
        var player = PlayerFactory.CreateConnectingPlayer(_world, connectionId);

        _connToEntity[connectionId] = player;
        _entityToConn[player] = connectionId;

        return player;
    }

    public bool TryGetEntity(int connectionId, out EntityId entity)
        => _connToEntity.TryGetValue(connectionId, out entity);

    public bool TryGetConnection(EntityId entity, out int connectionId)
        => _entityToConn.TryGetValue(entity, out connectionId);

    public bool Remove(int connectionId)
    {
        if (!_connToEntity.TryGetValue(connectionId, out var entity))
            return false;
        _connToEntity.Remove(connectionId);
        _entityToConn.Remove(entity);
        return true;
    }

    public void Disconnect(EntityId entity)
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
