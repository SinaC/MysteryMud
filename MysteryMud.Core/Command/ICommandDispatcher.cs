using Arch.Core;

namespace MysteryMud.Core.Command;

public interface ICommandDispatcher
{
    void Dispatch(SystemContext systemContext, GameState gameState, Entity actor, ReadOnlySpan<char> input);
}
