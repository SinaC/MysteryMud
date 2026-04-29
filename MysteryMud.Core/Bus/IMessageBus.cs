using DefaultEcs;

namespace MysteryMud.Core.Bus;

public interface IMessageBus
{
    void Publish(Entity entity, string message);
    public void Process(GameState state);
}
