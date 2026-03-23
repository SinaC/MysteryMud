using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public interface IMessageBus
{
    void Publish(Entity entity, string message);
    public void Process(SystemContext systemContext, GameState gameState);
}
