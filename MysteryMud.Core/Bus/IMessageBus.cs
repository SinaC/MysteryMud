using TinyECS;

namespace MysteryMud.Core.Bus;

public interface IMessageBus
{
    void Publish(EntityId entity, string message);
    public void Process(GameState state);
}
