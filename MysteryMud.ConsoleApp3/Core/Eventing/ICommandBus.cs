using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public interface ICommandBus
{
    void Publish(Entity player, ReadOnlySpan<char> span);
    void Process(SystemContext systemContext, GameState gameState);
}
