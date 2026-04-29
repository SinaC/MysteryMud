using DefaultEcs;

namespace MysteryMud.Core.Bus;

public interface ICommandBus
{
    void Publish(Entity player, ReadOnlySpan<char> span);
    void Process(GameState state);
}
