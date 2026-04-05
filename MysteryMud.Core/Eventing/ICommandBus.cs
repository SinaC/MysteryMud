using Arch.Core;
using MysteryMud.Core.Commands;

namespace MysteryMud.Core.Eventing;

public interface ICommandBus
{
    void Publish(Entity player, ReadOnlySpan<char> span);
    void Process(CommandExecutionContext ctx, GameState state);
}
