using Arch.Core;

namespace MysteryMud.Core.Eventing;

public interface IMessageBus
{
    void Publish(Entity entity, string message);
    public void Process(GameState state);
}
