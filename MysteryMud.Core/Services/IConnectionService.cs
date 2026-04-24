using TinyECS;

namespace MysteryMud.Core.Services;

public interface IConnectionService
{
    EntityId CreatePlayer(int connectionId);

    bool TryGetEntity(int connectionId, out EntityId entity);
    bool TryGetConnection(EntityId entity, out int connectionId);

    bool Remove(int connectionId);

    void Disconnect(EntityId entity);
}