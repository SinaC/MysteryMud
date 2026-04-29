using DefaultEcs;

namespace MysteryMud.Core.Services;

public interface IConnectionService
{
    Entity CreatePlayer(int connectionId);

    bool TryGetEntity(int connectionId, out Entity entity);
    bool TryGetConnection(Entity entity, out int connectionId);

    bool Remove(int connectionId);

    void Disconnect(Entity entity);
}