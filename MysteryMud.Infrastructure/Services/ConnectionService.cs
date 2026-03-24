using Arch.Core;
using MysteryMud.Domain.Factories;

namespace MysteryMud.Infrastructure.Services;

public class ConnectionService
{
    private readonly World _world;

    private readonly Dictionary<int, Entity> _connToEntity = new();
    private readonly Dictionary<Entity, int> _entityToConn = new();

    public ConnectionService(World world)
    {
        _world = world;
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
}
