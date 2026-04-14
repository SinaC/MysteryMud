using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Application.Dispatching;

public interface ICommandDispatcher
{
    void Dispatch(GameState state, Entity actor, ReadOnlySpan<char> input);
}
