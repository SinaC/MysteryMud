using TinyECS;

namespace MysteryMud.Core.Bus;

public interface ICommandBus
{
    void Publish(EntityId player, ReadOnlySpan<char> span);
    void Process(GameState state);
}
