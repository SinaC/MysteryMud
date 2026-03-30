using Arch.Core;

namespace MysteryMud.Core.Eventing;

public interface ICommandBus
{
    void Publish(Entity player, ReadOnlySpan<char> span);
    void Process(SystemContext ctx, GameState state);
}
