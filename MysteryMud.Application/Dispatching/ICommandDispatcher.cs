using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Application.Dispatching;

public interface ICommandDispatcher
{
    void Dispatch(SystemContext systemContext, GameState gameState, Entity actor, ReadOnlySpan<char> input);
}
