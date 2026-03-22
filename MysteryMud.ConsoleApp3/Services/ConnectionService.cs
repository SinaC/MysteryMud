using Arch.Core;
using MysteryMud.ConsoleApp3.Factories;

namespace MysteryMud.ConsoleApp3.Services;

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
        var player = WorldFactory.CreateConnectingPlayer(_world, connectionId);

        _connToEntity[connectionId] = player;
        _entityToConn[player] = connectionId;

        return player;
    }

    public Entity GetEntity(int connectionId)
        => _connToEntity[connectionId];

    public int GetConnection(Entity entity)
        => _entityToConn[entity];
}
